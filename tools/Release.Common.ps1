$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot

function Get-ProjectVersion {
    $source = Get-Content -LiteralPath (Join-Path $projectRoot 'src/TypeGameMod/ProjectInfo.cs') -Encoding UTF8 -Raw
    $version = [regex]::Match($source, 'const string Version = "([0-9.]+)"').Groups[1].Value
    if (!$version) { throw 'ProjectInfo.Version is missing.' }
    return $version
}

function New-ReleaseDirectory([string]$Path) {
    if ((Test-Path -LiteralPath $Path) -or (Test-Path -LiteralPath "$Path.zip")) {
        throw "Release target already exists: $Path"
    }
    [IO.Directory]::CreateDirectory($Path) | Out-Null
}

function Write-Utf8([string]$Path, [string]$Text) {
    [IO.File]::WriteAllText($Path, $Text, (New-Object Text.UTF8Encoding $false))
}

function Copy-ReleaseFile([string]$From, [string]$To) {
    [IO.Directory]::CreateDirectory((Split-Path -Parent $To)) | Out-Null
    Copy-Item -LiteralPath $From -Destination $To
}

function Write-ReleaseManifest([string]$Directory, [string]$Kind) {
    $files = @(Get-ChildItem -LiteralPath $Directory -File -Recurse -Force |
        Sort-Object FullName | ForEach-Object {
            [ordered]@{
                path = $_.FullName.Substring($Directory.Length + 1).Replace('\', '/')
                bytes = $_.Length
                sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
            }
        })
    Write-Utf8 (Join-Path $Directory 'manifest.json') (
        [ordered]@{ version = (Get-ProjectVersion); kind = $Kind; files = $files } | ConvertTo-Json -Depth 5)
}

function Compress-Release([string]$Directory) {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [IO.Compression.ZipFile]::CreateFromDirectory(
        $Directory, "$Directory.zip", [IO.Compression.CompressionLevel]::Optimal, $false)
    $hash = (Get-FileHash -LiteralPath "$Directory.zip" -Algorithm SHA256).Hash.ToLowerInvariant()
    Write-Utf8 "$Directory.zip.sha256" "$hash  $([IO.Path]::GetFileName($Directory)).zip`n"
}
