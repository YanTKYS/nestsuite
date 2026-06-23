using System.Windows.Input;

namespace NestSuite.ChatNest;

/// <summary>
/// ChatNest Workspace 固有ショートカットの判定を集約する。
/// Shell 共通の Shift+Left/Right タブ切り替えを妨げないため、
/// ChatNest は Ctrl+Left/Right と Ctrl+Enter のみを処理対象にする。
/// </summary>
public static class ChatNestShortcutPolicy
{
    public static bool IsSpeakerSwitchShortcut(Key key, ModifierKeys modifiers)
        => (key == Key.Left || key == Key.Right) && modifiers == ModifierKeys.Control;

    public static bool IsSendShortcut(Key key, ModifierKeys modifiers)
        => key == Key.Enter && modifiers == ModifierKeys.Control;
}
