param([ValidateSet('None','Divider','Box','Whitespace','Marker','IgnoreOff','StaleBox')][string]$Mutation = 'None')
$ErrorActionPreference = 'Stop'
$taskRepo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$taskSrc = Join-Path $taskRepo 'medick_TerribleTooltips/src'
$taskOld = Join-Path $taskRepo 'docs/build-2026-09-18'
$taskOut = Join-Path $PSScriptRoot "generated/$Mutation"
New-Item -ItemType Directory -Force -Path $taskOut | Out-Null
# Reuse all 76 beta3 assertions; replace preference doubles with real Prefs.cs.
$taskHarness = Get-Content (Join-Path $taskOld 'RegressionHarness.cs') -Raw
$taskStart = $taskHarness.IndexOf('    public class Setting<T>')
$taskEnd = $taskHarness.IndexOf('    public static class GroundLabels', $taskStart)
$taskHarness = $taskHarness.Remove($taskStart, $taskEnd - $taskStart)
$taskStart = $taskHarness.IndexOf('    public static class Dbg')
$taskEnd = $taskHarness.IndexOf("`n}", $taskStart)
$taskHarness = $taskHarness.Remove($taskStart, $taskEnd - $taskStart)
$taskHarness = $taskHarness.Replace("        ScopedRegression();", "        ScopedRegression();`n        SignalRegression();")
$taskHarness = $taskHarness.Replace('        Prefs.DebugLog.Value = false;', '        if (Prefs.Category == null) Prefs.Init();' + "`n        Prefs.DebugLog.Value = false;")
$taskHarness = $taskHarness.Replace('        public bool activeInHierarchy = true;', "        public bool activeInHierarchy = true;`n        public bool activeSelf = true;`n        public void SetActive(bool value) { activeSelf = value; }")
Set-Content (Join-Path $taskOut 'RegressionHarness.cs') $taskHarness

$taskComposer = Get-Content (Join-Path $taskSrc 'TooltipRecolor.cs') -Raw
$taskReplacement = switch ($Mutation) {
    'Divider' { 'if (!Prefs.ShowSignal.Value) return name + " | ";' }
    'Box' { 'if (!Prefs.ShowSignal.Value) return "<link=\"ttu\"></link>" + name;' }
    'Whitespace' { 'if (!Prefs.ShowSignal.Value) return name + " ";' }
    'Marker' { 'if (!Prefs.ShowSignal.Value) return name + "\u200B";' }
    'IgnoreOff' { 'if (false) return name;' }
    default { 'if (!Prefs.ShowSignal.Value) return name;' }
}
$taskComposer = $taskComposer.Replace('if (!Prefs.ShowSignal.Value) return name;', $taskReplacement)
Set-Content (Join-Path $taskOut 'TooltipRecolor.cs') $taskComposer
# Compare against the frozen beta3 composer, with only class names renamed.
$taskBaseline = (& git -C $taskRepo show 'd9caa94:medick_TerribleTooltips/medick_TerribleTooltips/src/TooltipRecolor.cs') -join "`n"
if ($LASTEXITCODE -ne 0) { throw 'Cannot load beta3 baseline' }
$taskBaseline = $taskBaseline.Replace('TooltipRecolor', 'BaselineTooltipRecolor').Replace('TooltipPerf', 'BaselineTooltipPerf')
Set-Content (Join-Path $taskOut 'BaselineTooltipRecolor.cs') $taskBaseline
# Extract exact production border eligibility/count/hide/LateUpdate methods.
# Layout/Unity geometry is deliberately stubbed; this tests retirement, not pixels.
$taskBorder = Get-Content (Join-Path $taskSrc 'UnitBorder.cs') -Raw
$taskMethods = @()
foreach ($taskPair in @(
    @('    internal static void OnLateUpdate()', '    private static void Run('),
    @('    private static int CountUnitBoxes(', '    private static bool GateEnabled()'),
    @('    private static void Hide(RowState row)', '    private static void DestroyRows(')
)) {
    $taskStart = $taskBorder.IndexOf($taskPair[0]); $taskEnd = $taskBorder.IndexOf($taskPair[1], $taskStart)
    if ($taskStart -lt 0 -or $taskEnd -lt 0) { throw 'Border extraction anchor missing' }
    $taskMethods += $taskBorder.Substring($taskStart, $taskEnd - $taskStart)
}
$taskBorderProbe = "internal static partial class BorderProbe {`n" + ($taskMethods -join "`n") + "`n}"
if ($Mutation -eq 'StaleBox') { $taskBorderProbe = $taskBorderProbe.Replace('Hide(row);', '// deliberately leave stale border visible') }
Set-Content (Join-Path $taskOut 'BorderProbe.Generated.cs') $taskBorderProbe
$taskCompiler = 'C:/Program Files/dotnet/sdk/10.0.401/Roslyn/bincore/csc.dll'
$taskRefs = 'C:/Program Files/dotnet/packs/Microsoft.NETCore.App.Ref/6.0.36/ref/net6.0'
$taskArgs = @('-nologo', '-target:exe', '-langversion:latest', '-nullable:disable', ('-out:' + (Join-Path $taskOut 'RegressionHarness.dll')))
$taskArgs += Get-ChildItem $taskRefs -Filter '*.dll' | ForEach-Object { '-r:' + $_.FullName }
$taskArgs += Get-ChildItem $taskOut -Filter '*.cs' | ForEach-Object { $_.FullName }
$taskArgs += Join-Path $taskOld 'ScopedRegression.cs'
$taskArgs += Join-Path $PSScriptRoot 'SignalRegression.cs'
$taskArgs += Join-Path $PSScriptRoot 'SettingsStubs.cs'
foreach ($taskFile in @('FilterRuleTooltip.cs','Colors.cs','Prefs.cs','SettingsUi.cs')) { $taskArgs += Join-Path $taskSrc $taskFile }
& dotnet $taskCompiler @taskArgs
if ($LASTEXITCODE -ne 0) { throw 'Regression compilation failed' }
'{"runtimeOptions":{"tfm":"net6.0","framework":{"name":"Microsoft.NETCore.App","version":"6.0.36"}}}' | Set-Content (Join-Path $taskOut 'RegressionHarness.runtimeconfig.json')
$env:TT_TEST_SOURCE = $taskSrc
& dotnet (Join-Path $taskOut 'RegressionHarness.dll')
if ($LASTEXITCODE -ne 0) { throw "Regression failed ($Mutation)" }
