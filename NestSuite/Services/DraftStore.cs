using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using NestSuite.ChatNest;

namespace NestSuite.Services;

public static class DraftStore
{
    public const string CurrentDraftFormatVersion = "1.0";
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static string DefaultRootDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NoteNest", "drafts");

    public static bool TryGetTabId(string draftFilePath, out string tabId)
    {
        tabId = string.Empty;
        if (string.IsNullOrWhiteSpace(draftFilePath)) return false;
        var name = Path.GetFileName(draftFilePath);
        if (name.Contains("..", StringComparison.Ordinal)) return false;
        if (!name.StartsWith("draft-", StringComparison.OrdinalIgnoreCase) ||
            !name.EndsWith(NestSuiteWorkspaceEnvelope.FileExtension, StringComparison.OrdinalIgnoreCase)) return false;
        if (name.Contains(".state.json", StringComparison.OrdinalIgnoreCase) ||
            name.Contains(".tmp", StringComparison.OrdinalIgnoreCase) ||
            name.Contains(".corrupt-", StringComparison.OrdinalIgnoreCase)) return false;
        var guidPart = name[6..^NestSuiteWorkspaceEnvelope.FileExtension.Length];
        if (!Guid.TryParseExact(guidPart, "N", out var guid)) return false;
        tabId = guid.ToString("N");
        return true;
    }

    public static IReadOnlyList<string> ListDraftFiles(string? rootDirectory = null)
    {
        var root = ResolveRoot(rootDirectory);
        if (!Directory.Exists(root)) return Array.Empty<string>();
        return Directory.EnumerateFiles(root)
            .Where(p => TryGetTabId(Path.GetFileName(p), out _))
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToList();
    }

    public static void WriteWorkspaceDraft(string tabId, string wrappedJson, ChatNestTransientDraftState? transientState = null, string? rootDirectory = null)
    {
        tabId = ValidateTabId(tabId);
        if (string.IsNullOrWhiteSpace(wrappedJson)) throw new ArgumentException("wrappedJson is required.", nameof(wrappedJson));
        var root = ResolveRoot(rootDirectory);
        Directory.CreateDirectory(root);
        var draftPath = WorkspacePath(root, tabId);
        var sidecarPath = StatePath(root, tabId);
        var bytes = Utf8NoBom.GetBytes(wrappedJson);
        AtomicFileWriter.WriteAllText(draftPath, wrappedJson, Utf8NoBom);
        if (transientState is { IsEmpty: false })
        {
            var sidecar = new SidecarDto
            {
                DraftFormatVersion = CurrentDraftFormatVersion,
                WorkspaceKind = NestSuiteWorkspaceEnvelope.KindChatNest,
                WorkspaceFileSha256 = Sha256Hex(bytes),
                TransientState = new TransientStateDto
                {
                    InputText = transientState.InputText,
                    SelectedSpeaker = transientState.SelectedSpeaker,
                    EditingMessageId = transientState.EditingMessageId,
                    EditingText = transientState.EditingText,
                }
            };
            AtomicFileWriter.WriteAllText(sidecarPath, JsonSerializer.Serialize(sidecar, JsonOptions), Utf8NoBom);
        }
        else if (File.Exists(sidecarPath))
        {
            File.Delete(sidecarPath);
        }
    }

    public static TransientDraftReadResult ReadTransientState(string draftFilePath, string? rootDirectory = null)
    {
        try
        {
            if (!TryGetTabId(Path.GetFileName(draftFilePath), out var tabId))
                return new(TransientDraftReadStatus.InvalidFormat, null, "Invalid draft file name.");
            var root = rootDirectory == null ? Path.GetDirectoryName(Path.GetFullPath(draftFilePath)) ?? ResolveRoot(null) : ResolveRoot(rootDirectory);
            var draftPath = WorkspacePath(root, tabId);
            var sidecarPath = StatePath(root, tabId);
            if (!File.Exists(sidecarPath)) return new(TransientDraftReadStatus.NotPresent, null);
            SidecarDto? sidecar;
            try { sidecar = JsonSerializer.Deserialize<SidecarDto>(File.ReadAllText(sidecarPath, Utf8NoBom), JsonOptions); }
            catch (JsonException ex) { return new(TransientDraftReadStatus.InvalidFormat, null, ex.Message); }
            if (sidecar?.TransientState == null || string.IsNullOrWhiteSpace(sidecar.DraftFormatVersion) ||
                string.IsNullOrWhiteSpace(sidecar.WorkspaceKind) || string.IsNullOrWhiteSpace(sidecar.WorkspaceFileSha256))
                return new(TransientDraftReadStatus.InvalidFormat, null, "Missing required sidecar fields.");
            if (!string.Equals(sidecar.DraftFormatVersion, CurrentDraftFormatVersion, StringComparison.Ordinal))
                return new(TransientDraftReadStatus.UnsupportedVersion, null, sidecar.DraftFormatVersion);
            if (!string.Equals(sidecar.WorkspaceKind, NestSuiteWorkspaceEnvelope.KindChatNest, StringComparison.Ordinal))
                return new(TransientDraftReadStatus.InvalidFormat, null, "Unsupported workspace kind.");
            var actualHash = Sha256Hex(File.ReadAllBytes(draftPath));
            if (!string.Equals(actualHash, sidecar.WorkspaceFileSha256, StringComparison.OrdinalIgnoreCase))
                return new(TransientDraftReadStatus.HashMismatch, null);
            var state = sidecar.TransientState;
            var selectedSpeaker = NormalizeSelectedSpeaker(state.SelectedSpeaker);
            return new(TransientDraftReadStatus.Loaded, new ChatNestTransientDraftState(
                state.InputText ?? string.Empty,
                selectedSpeaker,
                state.EditingMessageId,
                state.EditingText ?? string.Empty));
        }
        catch (IOException ex) { return new(TransientDraftReadStatus.IoError, null, ex.Message); }
        catch (UnauthorizedAccessException ex) { return new(TransientDraftReadStatus.IoError, null, ex.Message); }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException) { return new(TransientDraftReadStatus.InvalidFormat, null, ex.Message); }
    }

    public static string QuarantineWorkspaceDraft(string draftFilePath)
    {
        if (!TryGetTabId(Path.GetFileName(draftFilePath), out var tabId)) throw new ArgumentException("Invalid draft path.", nameof(draftFilePath));
        var root = Path.GetDirectoryName(Path.GetFullPath(draftFilePath)) ?? ResolveRoot(null);
        var stamp = NowProvider().ToString("yyyyMMdd-HHmmss");
        var target = MoveUnique(WorkspacePath(root, tabId), stamp);
        var sidecar = StatePath(root, tabId);
        if (File.Exists(sidecar)) MoveUnique(sidecar, stamp);
        return target;
    }

    public static string? QuarantineTransientState(string draftFilePath)
    {
        if (!TryGetTabId(Path.GetFileName(draftFilePath), out var tabId)) throw new ArgumentException("Invalid draft path.", nameof(draftFilePath));
        var root = Path.GetDirectoryName(Path.GetFullPath(draftFilePath)) ?? ResolveRoot(null);
        var sidecar = StatePath(root, tabId);
        return File.Exists(sidecar) ? MoveUnique(sidecar, NowProvider().ToString("yyyyMMdd-HHmmss")) : null;
    }

    public static void Delete(string tabId, string? rootDirectory = null)
    {
        tabId = ValidateTabId(tabId);
        var root = ResolveRoot(rootDirectory);
        var draft = WorkspacePath(root, tabId); if (File.Exists(draft)) File.Delete(draft);
        var sidecar = StatePath(root, tabId); if (File.Exists(sidecar)) File.Delete(sidecar);
    }

    private static string ResolveRoot(string? rootDirectory) => Path.GetFullPath(string.IsNullOrWhiteSpace(rootDirectory) ? DefaultRootDirectory : rootDirectory);
    private static string ValidateTabId(string tabId) => Guid.TryParseExact(tabId, "N", out var guid) ? guid.ToString("N") : throw new ArgumentException("tabId must be GUID-N.", nameof(tabId));
    private static string WorkspacePath(string root, string tabId) => Path.Combine(root, $"draft-{tabId}.nestsuite");
    private static string StatePath(string root, string tabId) => Path.Combine(root, $"draft-{tabId}.state.json");
    internal static Func<DateTime> NowProvider { get; set; } = () => DateTime.Now;

    private static string NormalizeSelectedSpeaker(string? selectedSpeaker) =>
        selectedSpeaker switch
        {
            nameof(Speaker.自分) => nameof(Speaker.自分),
            nameof(Speaker.反論) => nameof(Speaker.反論),
            nameof(Speaker.補足) => nameof(Speaker.補足),
            nameof(Speaker.結論) => nameof(Speaker.結論),
            _ => nameof(Speaker.自分),
        };

    private static string Sha256Hex(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    private static string MoveUnique(string path, string stamp)
    {
        var baseTarget = path + ".corrupt-" + stamp;
        var target = baseTarget;
        for (var i = 1; File.Exists(target); i++) target = baseTarget + "-" + i.ToString(System.Globalization.CultureInfo.InvariantCulture);
        File.Move(path, target);
        return target;
    }
    private sealed class SidecarDto { public string DraftFormatVersion { get; set; } = ""; public string WorkspaceKind { get; set; } = ""; public string WorkspaceFileSha256 { get; set; } = ""; public TransientStateDto? TransientState { get; set; } }
    private sealed class TransientStateDto { public string? InputText { get; set; } public string? SelectedSpeaker { get; set; } public Guid? EditingMessageId { get; set; } public string? EditingText { get; set; } }
}
