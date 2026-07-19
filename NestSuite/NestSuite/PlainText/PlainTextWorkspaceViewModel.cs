using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NestSuite.PlainText;

/// <summary>
/// v2.19.0 SH-43: PlainTextWorkspace（`.txt`）の最小 ViewModel。
///
/// <para><b>責務</b><br/>
/// 本文（<see cref="Text"/>）・未保存状態（<see cref="IsDirty"/>）・読込時に判定した
/// 文字コード／改行コードの保持のみ。Shell 固有のダイアログ・ファイル選択は持ち込まない
/// （<see cref="NestSuiteShellWindow"/> 側の Save/SaveAs 経路が <see cref="PlainTextFileService"/>
/// を直接呼ぶ。他 Workspace の Try*ToPath パターンと対称）。</para>
/// </summary>
public sealed class PlainTextWorkspaceViewModel : INotifyPropertyChanged, IDisposable
{
    private string _text = string.Empty;
    private bool _isDirty;
    private bool _isLoading;
    private bool _disposed;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>本文。TextBox と TwoWay バインドする。</summary>
    public string Text
    {
        get => _text;
        set
        {
            if (_text == value) return;
            _text = value;
            OnPropertyChanged();
            // v2.19.0 SH-43: 読込直後の初期反映（LoadContent 内）では dirty にしない。
            // それ以外の変更は、同一内容へ戻した場合でも dirty を解除しない
            // （既存 Workspace の内容比較によらない dirty 契約に合わせる）。
            if (!_isLoading) IsDirty = true;
        }
    }

    /// <summary>未保存の変更があるかどうか。保存成功時のみ <see cref="MarkSaved"/> で解除する。</summary>
    public bool IsDirty
    {
        get => _isDirty;
        private set { if (_isDirty == value) return; _isDirty = value; OnPropertyChanged(); }
    }

    /// <summary>読込時（または新規作成時）に確定した文字コード。保存時もこの形式を維持する。</summary>
    public PlainTextEncodingKind EncodingKind { get; private set; } = PlainTextEncodingKind.Utf8NoBom;

    /// <summary>読込時に判定した改行コード構成。単一種類の場合のみ保存時に揃える。</summary>
    public PlainTextNewlineKind NewlineKind { get; private set; } = PlainTextNewlineKind.None;

    /// <summary>
    /// 読込結果を反映する。<see cref="IsDirty"/> は false のまま（読込直後は未保存変更なし）。
    /// </summary>
    public void LoadContent(PlainTextLoadResult result)
    {
        _isLoading = true;
        try
        {
            Text = result.Text;
        }
        finally
        {
            _isLoading = false;
        }
        EncodingKind = result.EncodingKind;
        NewlineKind = result.NewlineKind;
        IsDirty = false;
    }

    /// <summary>
    /// 新規（無題）タブの初期状態にする。内容は空・文字コードは UTF-8 BOM なし・
    /// 改行コードは None（保存時は編集コントロールの内容をそのまま書き込む）。<see cref="IsDirty"/> は false。
    /// </summary>
    public void InitializeAsNew()
    {
        _isLoading = true;
        try
        {
            Text = string.Empty;
        }
        finally
        {
            _isLoading = false;
        }
        EncodingKind = PlainTextEncodingKind.Utf8NoBom;
        NewlineKind = PlainTextNewlineKind.None;
        IsDirty = false;
    }

    /// <summary>保存成功後に呼ぶ。保存失敗時は呼ばない（dirty を維持するため）。</summary>
    public void MarkSaved() => IsDirty = false;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        PropertyChanged = null;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
