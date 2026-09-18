$ErrorActionPreference = 'Stop'
$taskRepo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$taskSource = [IO.File]::ReadAllText((Join-Path $taskRepo 'medick_TerribleTooltips/src/TooltipRecolor.cs'))
$taskCompiler = 'C:/Program Files/dotnet/sdk/10.0.401/Roslyn/bincore/csc.dll'
$taskRefs = 'C:/Program Files/dotnet/packs/Microsoft.NETCore.App.Ref/6.0.36/ref/net6.0'
$taskMutations = @{
    MissingComparison = $taskSource.Replace('AddRoot(ui.compareContent?.transform);', '// MUTATION: comparison panel omitted')
    WholeScene = $taskSource.Replace('root.GetComponentsInChildren<TextMeshProUGUI>(true)', 'UnityEngine.Object.FindObjectsOfType<TextMeshProUGUI>()')
}
foreach ($taskName in $taskMutations.Keys) {
    if ($taskMutations[$taskName] -ceq $taskSource) { throw "Mutation did not apply: $taskName" }
    $taskDir = Join-Path $PSScriptRoot ('mutations/' + $taskName)
    New-Item -ItemType Directory -Force -Path $taskDir | Out-Null
    $taskMutated = Join-Path $taskDir 'TooltipRecolor.cs'
    [IO.File]::WriteAllText($taskMutated, $taskMutations[$taskName])
    $taskOutput = Join-Path $taskDir 'RegressionHarness.dll'
    $taskArgs = @('-nologo', '-target:exe', '-langversion:latest', '-nullable:disable', ('-out:' + $taskOutput))
    $taskArgs += Get-ChildItem -LiteralPath $taskRefs -Filter '*.dll' | ForEach-Object { '-r:' + $_.FullName }
    $taskArgs += Join-Path $PSScriptRoot 'RegressionHarness.cs'
    $taskArgs += Join-Path $PSScriptRoot 'ScopedRegression.cs'
    $taskArgs += $taskMutated
    $taskArgs += Join-Path $taskRepo 'medick_TerribleTooltips/src/FilterRuleTooltip.cs'
    $taskArgs += Join-Path $taskRepo 'medick_TerribleTooltips/src/Colors.cs'
    & dotnet $taskCompiler @taskArgs
    if ($LASTEXITCODE -ne 0) { throw "Mutation failed to compile (not a valid kill): $taskName" }
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'RegressionHarness.runtimeconfig.json') -Destination $taskDir
    & dotnet $taskOutput > (Join-Path $taskDir 'output.txt') 2>&1
    $taskExit = $LASTEXITCODE
    $taskLog = [IO.File]::ReadAllText((Join-Path $taskDir 'output.txt'))
    if ($taskExit -eq 0 -or !$taskLog.Contains('Unhandled exception. System.Exception:')) {
        throw "Mutation survived or failed without an assertion: $taskName (exit $taskExit)"
    }
    $taskFailure = ($taskLog -split '\r?\n' | Where-Object { $_.Contains('Unhandled exception.') }) -join ' '
    "KILLED $taskName (exit $taskExit): $taskFailure"
}
