param(
    [Parameter(Mandatory = $true)][string]$Directory,
    [ValidateSet('mod', 'full-game', 'github-source')][string]$Kind = 'full-game'
)
$ErrorActionPreference = 'Stop'
$Directory = [IO.Path]::GetFullPath($Directory)
$manifest = Get-Content -LiteralPath (Join-Path $Directory 'manifest.json') -Encoding UTF8 -Raw | ConvertFrom-Json
if ($manifest.kind -ne $Kind) { throw 'Release kind differs from its manifest.' }
foreach ($entry in $manifest.files) {
    $file = Join-Path $Directory $entry.path
    if ((Get-Item -LiteralPath $file).Length -ne $entry.bytes -or
        (Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash.ToLowerInvariant() -ne $entry.sha256) {
        throw "Release file differs from manifest: $($entry.path)"
    }
}
$longestPath = ''
foreach ($file in Get-ChildItem -LiteralPath $Directory -File -Recurse -Force) {
    if ($file.FullName.Length -gt $longestPath.Length) { $longestPath = $file.FullName }
    if ($file.FullName.Length -ge 260) {
        throw "Windows Explorer extraction path is too long. Use a shorter OutputDirectory: $($file.FullName)"
    }
    $relative = $file.FullName.Substring($Directory.Length + 1).Replace('\', '/')
    if ($relative -match '(^|/)(\.git|obj|bin|validation|__pycache__|logs)(/|$)' -or
        $file.Extension -in @('.pdb', '.log', '.idb', '.i64', '.id0', '.id1', '.id2', '.pyc') -or
        $file.Name -in @('owned-processes.json', 'TypeGameWASD.prepared', '.env')) {
        throw "Local-only file in release: $relative"
    }
    if ($Kind -eq 'github-source' -and ($relative -like 'Classic/*' -or $file.Name -eq 'MOON-Paris.m4a')) {
        throw "Private game/music input in source release: $relative"
    }
}
if ($Kind -eq 'full-game') {
    foreach ($relative in @('TypeGameWASD.exe', 'Classic/TypeGame.exe', 'Classic/game/GameRes.dll',
                            'Classic/game/GameSnd.dll', 'Classic/TypeGame.exe.manifest', 'Classic/License.txt')) {
        if (!(Test-Path -LiteralPath (Join-Path $Directory $relative))) { throw "Missing player dependency: $relative" }
    }
}
Write-Output "PASS release manifest and layout: $Kind, $($manifest.files.Count) files"
Write-Output "PASS Windows extraction path: longest $($longestPath.Length) characters"
