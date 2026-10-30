param([string]$OutputDirectory = (Join-Path (Split-Path -Parent $PSScriptRoot) 'artifacts/packages'))
. (Join-Path $PSScriptRoot 'Release.Common.ps1')
$destination = Join-Path ([IO.Path]::GetFullPath($OutputDirectory)) "TypeGameMod-$(Get-ProjectVersion)-src"
New-ReleaseDirectory $destination

# No local Git history, experiments, original game or personal soundtrack.
foreach ($directory in @('src', 'tests', 'native', 'assets', 'audio/weapons', 'compat', '.github')) {
    Get-ChildItem -LiteralPath (Join-Path $projectRoot $directory) -File -Recurse -Force |
        Where-Object { $_.FullName -notmatch '[\\/](obj|bin|__pycache__)[\\/]' } |
        ForEach-Object {
            Copy-ReleaseFile $_.FullName (Join-Path $destination $_.FullName.Substring($projectRoot.Length + 1))
        }
}
$files = @('TypeGameMod.sln', 'build.ps1', 'README.md', 'CHANGELOG.md', 'CONTRIBUTING.md',
    'LICENSE.txt', '.editorconfig', '.clang-format', '.gitignore', '.gitattributes',
    'audio/README.txt', 'build/common.props', 'build/game-files.json',
    'docs/ARCHITECTURE.md', 'docs/DEVELOPMENT.md', 'docs/RUNNING.md', 'docs/RELEASING.md',
    'tools/Release.Common.ps1', 'tools/package.ps1', 'tools/export-source.ps1',
    'tools/check-release.ps1', 'tools/build_native.py', 'tools/build_map_cover.py', 'tools/verify_refactor.py',
    'tools/format.ps1', 'tools/requirements-native.txt', 'tools/requirements-format.txt')
foreach ($file in $files) { Copy-ReleaseFile (Join-Path $projectRoot $file) (Join-Path $destination $file) }
Write-ReleaseManifest $destination 'github-source'
& (Join-Path $PSScriptRoot 'check-release.ps1') -Directory $destination -Kind 'github-source'
Compress-Release $destination
Write-Output "GitHub project: $destination"
