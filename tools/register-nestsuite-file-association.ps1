#Requires -Version 5.1
<#
.SYNOPSIS
    .notenest / .chatnest / .ideanest を NoteNest.exe に関連付けます（ユーザー単位）。

.DESCRIPTION
    HKCU\Software\Classes にファイル関連付けを登録します。管理者権限は不要です。
    v1.18.0 以降はアプリ内の「ヘルプ → ファイル関連付けの設定」から登録することを推奨します。
    このスクリプトは IT 担当者向けの補助手段です。

.PARAMETER ExePath
    NoteNest.exe のフルパス。省略時はスクリプトと同じディレクトリの NoteNest.exe を使用。

.EXAMPLE
    .\register-nestsuite-file-association.ps1
    .\register-nestsuite-file-association.ps1 -ExePath "C:\Apps\NoteNest\NoteNest.exe"

.NOTES
    - 登録後はエクスプローラーを再起動するか、一度ログアウト・ログインしてください。
    - NoteNest.exe を別の場所に移動した場合は再登録が必要です。
    - 関連付けが反映されない場合は Windows の「設定 → 既定のアプリ」を確認してください。
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$ExePath = (Join-Path $PSScriptRoot "..\NoteNest\bin\Release\net8.0-windows\NoteNest.exe")
)

$resolved = Resolve-Path $ExePath -ErrorAction SilentlyContinue
if ($resolved) { $ExePath = $resolved.Path }
if (-not $ExePath -or -not (Test-Path $ExePath)) {
    Write-Error "NoteNest.exe が見つかりません。-ExePath でフルパスを指定してください。"
    exit 1
}

$entries = @(
    @{ Ext = ".notenest"; ProgId = "NoteNest.notenest"; Desc = "NoteNest Document" },
    @{ Ext = ".chatnest"; ProgId = "NoteNest.chatnest"; Desc = "ChatNest Document" },
    @{ Ext = ".ideanest"; ProgId = "NoteNest.ideanest"; Desc = "IdeaNest Document" }
)

foreach ($entry in $entries) {
    $progIdPath = "HKCU:\Software\Classes\$($entry.ProgId)"
    $cmdPath    = "$progIdPath\shell\open\command"
    $extPath    = "HKCU:\Software\Classes\$($entry.Ext)"

    if ($PSCmdlet.ShouldProcess($entry.Ext, "ファイル関連付けを登録")) {
        New-Item -Path $progIdPath -Force | Out-Null
        Set-ItemProperty -Path $progIdPath -Name "(Default)" -Value $entry.Desc

        New-Item -Path $cmdPath -Force | Out-Null
        Set-ItemProperty -Path $cmdPath -Name "(Default)" -Value "`"$ExePath`" `"%1`""

        New-Item -Path $extPath -Force | Out-Null
        Set-ItemProperty -Path $extPath -Name "(Default)" -Value $entry.ProgId

        Write-Host "登録完了: $($entry.Ext) -> $ExePath"
    }
}

Write-Host ""
Write-Host "登録が完了しました。エクスプローラーを再起動するか、一度ログアウト・ログインしてください。"
