param([string]$ClangFormat = 'clang-format', [switch]$Check)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$files = Get-ChildItem (Join-Path $root 'src'), (Join-Path $root 'tests') -Filter '*.cs' -Recurse |
    Where-Object { $_.Name -notlike '*.g.cs' -and $_.FullName -notmatch '[\\/](obj|bin)[\\/]' } |
    ForEach-Object { $_.FullName }
if ($Check) { & $ClangFormat --dry-run --Werror @files }
else { & $ClangFormat -i @files }
if ($LASTEXITCODE -ne 0) { throw 'C# formatting check failed.' }
