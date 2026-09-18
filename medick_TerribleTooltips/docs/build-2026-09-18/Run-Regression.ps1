$ErrorActionPreference = 'Stop'
# PSScriptRoot = repo/docs/build-2026-09-18; resolve source paths explicitly.
$taskRepo = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$taskCompiler = 'C:/Program Files/dotnet/sdk/10.0.401/Roslyn/bincore/csc.dll'
$taskRefs = 'C:/Program Files/dotnet/packs/Microsoft.NETCore.App.Ref/6.0.36/ref/net6.0'
$taskOutput = Join-Path $PSScriptRoot 'RegressionHarness.dll'
$taskArgs = @('-nologo', '-target:exe', '-langversion:latest', '-nullable:disable', ('-out:' + $taskOutput))
$taskArgs += Get-ChildItem -LiteralPath $taskRefs -Filter '*.dll' | ForEach-Object { '-r:' + $_.FullName }
$taskArgs += Join-Path $PSScriptRoot 'RegressionHarness.cs'
$taskArgs += Join-Path $PSScriptRoot 'ScopedRegression.cs'
$taskArgs += Join-Path $taskRepo 'medick_TerribleTooltips/src/TooltipRecolor.cs'
$taskArgs += Join-Path $taskRepo 'medick_TerribleTooltips/src/FilterRuleTooltip.cs'
$taskArgs += Join-Path $taskRepo 'medick_TerribleTooltips/src/Colors.cs'
& dotnet $taskCompiler @taskArgs
if ($LASTEXITCODE -ne 0) { throw 'Regression harness compilation failed' }
'{"runtimeOptions":{"tfm":"net6.0","framework":{"name":"Microsoft.NETCore.App","version":"6.0.36"}}}' |
    Set-Content -LiteralPath (Join-Path $PSScriptRoot 'RegressionHarness.runtimeconfig.json') -Encoding utf8
& dotnet $taskOutput
if ($LASTEXITCODE -ne 0) { throw 'Regression harness failed' }
