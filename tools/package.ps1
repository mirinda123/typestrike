param(
    [string]$OutputDirectory = (Join-Path (Split-Path -Parent $PSScriptRoot) 'artifacts/packages'),
    [string]$GameDirectory,
    [string]$SoundtrackPath,
    [switch]$SkipBuild,
    [switch]$NoSource
)
. (Join-Path $PSScriptRoot 'Release.Common.ps1')
$version = Get-ProjectVersion
$kind = if ($GameDirectory) { 'full-game' } else { 'mod' }
$packageName = if ($GameDirectory) { "TypeGameWASD-$version" } else { "TypeGameWASD-$version-mod" }
$runtime = Join-Path ([IO.Path]::GetFullPath($OutputDirectory)) $packageName
if (!$SkipBuild) { & (Join-Path $projectRoot 'build.ps1') -Test }
New-ReleaseDirectory $runtime
Copy-ReleaseFile (Join-Path $projectRoot 'artifacts/bin/Release/TypeGameWASD.exe') (Join-Path $runtime 'TypeGameWASD.exe')
foreach ($directory in @('compat', 'audio/weapons')) {
    Get-ChildItem -LiteralPath (Join-Path $projectRoot $directory) -File -Recurse | ForEach-Object {
        $relative = $_.FullName.Substring($projectRoot.Length + 1)
        Copy-ReleaseFile $_.FullName (Join-Path $runtime $relative)
    }
}
foreach ($file in @('LICENSE.txt', 'THIRD_PARTY.md', 'CHANGELOG.md', 'audio/README.txt')) {
    Copy-ReleaseFile (Join-Path $projectRoot $file) (Join-Path $runtime $file)
}
Copy-ReleaseFile (Join-Path $projectRoot 'docs/RUNNING.md') (Join-Path $runtime 'README.md')
Write-Utf8 (Join-Path $runtime 'Settings.cmd') "@echo off`r`nstart `"`" `"%~dp0TypeGameWASD.exe`" --launcher`r`n"
if ($SoundtrackPath) {
    Copy-ReleaseFile ([IO.Path]::GetFullPath($SoundtrackPath)) (Join-Path $runtime 'audio/MOON-Paris.m4a')
}
if ($GameDirectory) {
    $GameDirectory = [IO.Path]::GetFullPath($GameDirectory)
    $identity = Get-Content -LiteralPath (Join-Path $projectRoot 'src/TypeGameMod/Installation/GameIdentity.cs') -Encoding UTF8 -Raw
    foreach ($entry in @(@('ExecutableHash', 'TypeGame.exe'), @('ResourceHash', 'game/GameRes.dll'))) {
        $expected = [regex]::Match($identity, ($entry[0] + '\s*=\s*"([a-f0-9]{64})"')).Groups[1].Value
        $file = Join-Path $GameDirectory $entry[1]
        if (!$expected -or !(Test-Path -LiteralPath $file) -or
            (Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash.ToLowerInvariant() -ne $expected) {
            throw "Unsupported game file: $file"
        }
    }
    $policy = Get-Content -LiteralPath (Join-Path $projectRoot 'build/game-files.json') -Encoding UTF8 -Raw | ConvertFrom-Json
    foreach ($relative in $policy.files) {
        Copy-ReleaseFile (Join-Path $GameDirectory $relative) (Join-Path $runtime "Classic/$relative")
    }
    foreach ($directory in $policy.directories) {
        Get-ChildItem -LiteralPath (Join-Path $GameDirectory $directory) -Recurse -Force | ForEach-Object {
            if (($_.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw "Game input contains a link: $($_.FullName)" }
            if (!$_.PSIsContainer) {
                $relative = $_.FullName.Substring($GameDirectory.Length + 1)
                Copy-ReleaseFile $_.FullName (Join-Path $runtime "Classic/$relative")
            }
        }
    }
    foreach ($relative in $policy.emptyFiles) {
        $target = Join-Path $runtime "Classic/$relative"
        [IO.Directory]::CreateDirectory((Split-Path -Parent $target)) | Out-Null
        [IO.File]::WriteAllBytes($target, [byte[]]@())
    }
    foreach ($file in @('dciman32.dll', 'DDrawCompat.ini', 'TypeGame.exe.manifest', 'Microsoft.VC80.CRT.manifest')) {
        Copy-ReleaseFile (Join-Path $projectRoot "compat/$file") (Join-Path $runtime "Classic/$file")
    }
}
# Runtime dependencies only: symbols, source and historical validation are separate.
Write-ReleaseManifest $runtime $kind
& (Join-Path $PSScriptRoot 'check-release.ps1') -Directory $runtime -Kind $kind
Compress-Release $runtime
Write-Output "Player ZIP: $runtime.zip"
if (!$NoSource) { & (Join-Path $PSScriptRoot 'export-source.ps1') -OutputDirectory $OutputDirectory }
