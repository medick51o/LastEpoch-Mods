# Independent Cross-Vendor Code Review — v3.1.1-beta1 Patch

**Reviewer:** Google Seat (Antigravity Assistant)  
**Target Patch:** `docs/DELTA-311beta.patch` (553 lines)  
**Target Repository:** `medick_TerribleTooltips` (Last Epoch 1.4.7 / Unity 6000.0.42f1 / MelonLoader IL2CPP)  
**Write Set Scope:** `src/TooltipRecolor.cs`, `src/FilterRuleTooltip.cs`, `src/BuildInfo.cs`, `CHANGELOG.md`  

---

## Executive Summary & Verdict

**VERDICT: APPROVE**

The patch for v3.1.1-beta1 successfully resolves both confirmed per-frame defect loops identified in the 3.0.2/3.1.0 codebase while fully preserving all protected invariants, restoration paths, and performance boundaries.

- **Defect A (Filter-Rule Retries):** Correctly bounded to at most 12 frames or 0.5s per item content generation. Repeated same-content setter calls do not reset the budget. Missing targets settle gracefully without per-frame matching or descendant tree walks.
- **Defect B (Stale Original Retirement):** `RetireStaleOriginals()` safely prunes active, markerless originals that were not composed during a completed scan pass. Composed/marked originals and inactive background originals are preserved, ensuring Alt range deep-view and master-off vanilla restorations remain 100% reliable.
- **Defect C (Rate-Limited Telemetry):** `TooltipPerf` telemetry is strictly caller-guarded by `if (TooltipPerf.Enabled)`. When `DebugLog` is `false`, zero clock reads, zero allocations, and zero string formatting occur. When enabled, summaries emit at most once per ~5s.

---

## Criteria Adjudication

### 1. Rule-Retry Bounding & Late Target Discovery (`FilterRuleTooltip.cs`)
* **Status:** PASS
* **Findings:** None
* **Technical Evaluation:**
  - `Capture()` uses serialized `item.id` byte comparison (with a stable `(itemType, subType, rarity, uniqueID, individualID)` fallback for ID-less items) to detect identical content. Re-entrancy with identical content bypasses budget resets.
  - `ResolveRuleOnce()` sets `s_ruleResolved = true` prior to executing `TryGetMatchedRule()`. Loot filter matching runs at most once per capture generation, even if filter evaluation throws an exception.
  - Target discovery via `GetComponentsInChildren<TextMeshProUGUI>()` is limited to 1 attempt per frame and terminates when `frame > s_startFrame + 12` or `Time.unscaledTime > s_deadline` (0.5s).
  - If a `requires` component activates 1–12 frames late, `MonitorUpdate()` finds the active target, uses the cached rule resolution, and successfully injects `Rule#`.
  - When a target is destroyed or detached mid-hover, `BeginReplacement()` opens exactly one bounded discovery window for a replacement without re-running rule matching. Subsequent failed attempts lock via `GiveUp()`.
  - Tooltip closure or target change clears all pending state via `ClearPending()`.

### 2. Original-Retirement Safety & Restoration Invariants (`TooltipRecolor.cs`)
* **Status:** PASS
* **Findings:** None
* **Technical Evaluation:**
  - `RetireStaleOriginals()` executes at the end of `RunScan()`, after Pass 2 composition completes and before native relayout runs.
  - An entry in `s_originals` is retired ONLY if `tmp != null`, `tmp.gameObject.activeInHierarchy` is `true`, AND `(tmp.text ?? "").Contains(Marker)` is `false`.
  - Composed TMPs (which contain `Marker`) are NOT retired.
  - Inactive TMPs (`!activeInHierarchy`) are NOT retired, preserving background tab/panel restoration data.
  - Master-off (`RestoreVanillaOnMasterOff()`) and Alt deep-view (`ReRenderFromOriginals()`) iterate `s_originals` and rely on active marked entries. Because marked entries are never retired, vanilla text restoration and Alt toggles function with 100% fidelity.

### 3. Telemetry Performance & Zero-Cost Gate (`TooltipPerf`)
* **Status:** PASS
* **Findings:** None
* **Technical Evaluation:**
  - `TooltipPerf.Enabled` evaluates `Prefs.DebugLog != null && Prefs.DebugLog.Value`.
  - Every event method call in `FilterRuleTooltip.cs` and `TooltipRecolor.cs` is explicitly guarded by `if (TooltipPerf.Enabled)` at the invocation site.
  - When `DebugLog` is `false`, no event methods are called, and `TooltipPerf.Tick()` returns on line 1030 immediately without evaluating `Time.unscaledTime`.
  - When `DebugLog` is `true`, `Tick()` checks `elapsed < 5f` and rate-limits summary outputs to `MelonLogger.Msg()` to at most once per 5 seconds.

### 4. Protected Invariants
* **Status:** PASS
* **Findings:** None
* **Technical Evaluation:**
  - `BuildInfo.Name` remains `"Terrible Tooltips"` (FallenStar ABI compatibility maintained).
  - Two-stat/sealed affixes, ground labels (`GroundLabels.Marker`), unit border (`UnitBorder.cs`), and native range switch (`DriveNativeRangeSwitch()`) remain untouched and fully operational.
  - No new Harmony hooks on `OpenItemTooltip` or `UpdatePrefixAndSuffixesText`.
  - No new configuration keys added to `Prefs.cs`.

### 5. Containment & File Integrity
* **Status:** PASS
* **Findings:** None
* **Technical Evaluation:**
  - Source changes strictly match the 4 allowed write-set files:
    - `src/TooltipRecolor.cs` (MD5: `c313593345614b7fb699c3ddfb7d5a18`)
    - `src/FilterRuleTooltip.cs` (MD5: `534b032a29abd918c16a81f4b8b5e5b5`)
    - `src/BuildInfo.cs` (MD5: `77b1e6c9a9c5a7d564dccea893241a33`)
    - `CHANGELOG.md` (MD5: `dc6103c3e55fbb880b11c07e865241be`)

---

## Detailed Findings List

| Rank | File : Line | Finding Description | Failure Mechanism & Repro Path |
| :--- | :--- | :--- | :--- |
| **BLOCKER** | None | None | N/A |
| **MATERIAL** | None | None | N/A |
| **MINOR** | None | None | N/A |
| **NOT PROVEN** | None | None | N/A |

---

## Explicit List of What Could Not Be Verified

1. **In-game performance/FPS in the reporter's specific environment:** The Nexus reporter's (Trunks1981) exact system configuration and crafting shard hover stutter (17 FPS on v3.0.2) was not tested in live gameplay. This release is an explicitly intended feedback beta.
2. **Live Il2Cpp / Unity runtime assembly execution:** Verification was performed via source inspection, MD5 checksum validation, deterministic C# Release build (`dotnet build -c Release`), and stub harness execution (53 assertions PASS). Live Unity engine execution was not performed.

---

*Effective Model:* Gemini 3.6 Flash
