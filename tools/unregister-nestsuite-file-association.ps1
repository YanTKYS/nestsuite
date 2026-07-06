#Requires -Version 5.1
<#
.SYNOPSIS
    NoteNest が登録したファイル関連付けを解除します（ユーザー単位）。

.DESCRIPTION
    HKCU\Software\Classes から NoteNest が作成した ProgId エントリのみを削除します。
    他アプリや OS 全体の設定は変更しません。管理者権限は不要です。
    v1.18.0 以降はアプリ内の「ヘルプ → ファイル関連付けの設定」から解除することを推奨します。
    このスクリプトは IT 担当者向けの補助手段です。

.EXAMPLE
    .\unregister-nestsuite-file-association.ps1
#>
[CmdletBinding(SupportsShouldProcess)]
param()

# v2.14.6 FM-3: FileAssociationService.cs の Targets と 3 箇所同期必須
# （FileAssociationService.cs / register-nestsuite-file-association.ps1 / unregister-nestsuite-file-association.ps1）
$entries = @(
    @{ Ext = ".nestsuite"; ProgId = "NoteNest.nestsuite" },
    @{ Ext = ".notenest"; ProgId = "NoteNest.notenest" },
    @{ Ext = ".chatnest"; ProgId = "NoteNest.chatnest" },
    @{ Ext = ".ideanest"; ProgId = "NoteNest.ideanest" }
)

$anyRemoved = $false

foreach ($entry in $entries) {
    $progIdPath = "HKCU:\Software\Classes\$($entry.ProgId)"
    $extPath    = "HKCU:\Software\Classes\$($entry.Ext)"

    if ($PSCmdlet.ShouldProcess($entry.Ext, "ファイル関連付けを解除")) {
        # 拡張子キーは ProgId が一致する場合のみ削除する
        if (Test-Path $extPath) {
            $current = (Get-ItemProperty -Path $extPath -ErrorAction SilentlyContinue)."(default)"
            if ($current -ieq $entry.ProgId) {
                Remove-Item -Path $extPath -Recurse -Force
                Write-Host "削除: $extPath"
                $anyRemoved = $true
            } else {
                Write-Host "スキップ: $extPath（別のアプリが登録済みのため変更しません）"
            }
        }

        # ProgId キーは存在すれば削除する
        if (Test-Path $progIdPath) {
            Remove-Item -Path $progIdPath -Recurse -Force
            Write-Host "削除: $progIdPath"
            $anyRemoved = $true
        }
    }
}

if ($anyRemoved) {
    Write-Host ""
    Write-Host "解除が完了しました。エクスプローラーを再起動するか、一度ログアウト・ログインしてください。"
} else {
    Write-Host "NoteNest が作成した関連付けが見つかりませんでした。"
}
