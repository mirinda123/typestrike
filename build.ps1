param(
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Release',
    [switch]$Test,
    [string]$BaselineExecutable,
    [string]$ReferenceAssembliesPath,
    [switch]$Rebuild
)
$ErrorActionPreference = 'Stop'
$msbuild = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\MSBuild.exe'
if (!(Test-Path -LiteralPath $msbuild)) {
    throw 'MSBuild for .NET Framework 4.x is required. You can also open TypeGameMod.sln in Visual Studio.'
}
$buildArguments = @('/nologo', '/verbosity:minimal', "/p:Configuration=$Configuration", '/p:Platform=x86')
if ($ReferenceAssembliesPath) {
    $referenceRoot = [IO.Path]::GetFullPath($ReferenceAssembliesPath) + [IO.Path]::DirectorySeparatorChar
    $buildArguments += "/p:TargetFrameworkRootPath=$referenceRoot"
}
if ($Rebuild) { $buildArguments += '/t:Rebuild' }
& $msbuild (Join-Path $PSScriptRoot 'TypeGameMod.sln') @buildArguments
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
if ($Test) {
    $testArgs = @((Join-Path $PSScriptRoot 'audio\weapons'))
    if ($BaselineExecutable) { $testArgs += [IO.Path]::GetFullPath($BaselineExecutable) }
    & (Join-Path $PSScriptRoot "artifacts\tests\$Configuration\TypeGameMod.Tests.exe") @testArgs
    if ($LASTEXITCODE -ne 0) { throw 'Regression tests failed.' }
}
Write-Output (Join-Path $PSScriptRoot "artifacts\bin\$Configuration\TypeGameWASD.exe")
