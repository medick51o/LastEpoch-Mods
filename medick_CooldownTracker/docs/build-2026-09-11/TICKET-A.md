# TICKET-A — Terrible Cooldowns Tier A → v1.0.1

## TASK (the boss's words, verbatim)
"go thru all my release mods we havent went over tonight that have been release on nexusmods. have the council go through each one and do the extensive review i want astra doing the same work as we did on terrible tool tips and terrible inventory. just the released ones on nexusmods not the private or github one exclusives. go in automode for the night and prepare all the changes for me to review in the morning. if the astra seat runs out wait the 5 hours and pick up where you last left off."

## CONTEXT
Repo `C:\Users\andre\Downloads\LastEpoch-Mods\medick_CooldownTracker` (source `src\*.cs`, `src\UI\*.cs`, build `medick_CooldownTracker.csproj`). MelonLoader + HarmonyX mod for Last Epoch 1.4.7, IL2CPP interop (`Il2Cpp` namespace), net6.0, Unity 6000.0.42f1. Council findings this ticket implements: `docs\council-2026-09-11\SYNTHESIS.md` (read for mechanism, not instructions; finding numbers below refer to its agreement table). Baselines + md5: `docs\build-2026-09-11\baseline\` (source copies carry a `.baseline` suffix so the compile glob ignores them; `MD5SUMS.txt` lists the originals). The pre-ticket build of this exact source is green: 0 warnings, 0 errors. The csproj currently has NO copy-to-Mods target and NO `<Version>` element — verify that yourself first (item A7a) before touching anything else.

## ITEMS (all required)
A1 — `InputBlocker.Apply` (src\InputBlocker.cs ~27-42): set `_applied = true` ONLY when this mod actually flipped `forceDisableInput` from false to true (inside the `if (!mgr.forceDisableInput)` branch). Do not reset `_applied` when the flag was already true. Keep the re-assert and the release-once logic otherwise unchanged. Rewrite the header comment (lines ~5-12) so it describes the real semantics: re-asserted every update while wanted, released once on close, and only if this mod set it. Drop the "v4.x shipped seven / v5 ships none" narration. (Finding 1: Astra, Kimi, GLM, Gemini)
A2 — `Prefs.Init` (src\Prefs.cs ~39-63): after the entries are created, add a `Sanitize()` step: for Alpha, Size, OffsetX, OffsetY, MenuScale, PanelX, PanelY — if the loaded value is NaN or infinite (`float.IsFinite` false), reset it to the entry's default; clamp InputMode to 0..3 and CtrlLayout to 0..2. Log ONE `MelonLogger.Warning` naming every key repaired (nothing when none). Do not clamp finite floats (the sliders and renderer already clamp at use) and do not touch labels or SlotEnabled. (Finding 3: Astra, Kimi, GLM)
A3 — `OverheadRenderer.Draw` (src\UI\OverheadRenderer.cs ~24): resolve `Camera.main` at most once per frame — cache it with the `Time.frameCount` it was resolved on, wrapped in try/catch (a throw → null → draw nothing that frame). (Finding 4: Astra, Kimi, GLM, Gemini)
A4 — `SlotRegistry.Register` (src\SlotRegistry.cs ~63-67): before the `RemoveAll`, detect whether an existing entry with the same `SlotIndex` has a Source that is still alive (Unity-null check) AND is a different instance from `icon`. If so: `Dbg.Log` every time, and `MelonLogger.Warning` ONCE per session (static flag) with text like "slot #N re-registered from a different live AbilityBarIcon — a second action bar may exist; report this line". Keep last-wins behaviour unchanged. Compare instances with `ReferenceEquals` on the managed wrappers OR `.Pointer` equality if you judge wrappers may differ for the same native object — say which you used and why. (Finding 2: Astra, Gemini, GLM; Kimi NOT PROVEN — this is the evidence step)
A5 — Comment-only: retcon or strip the v4.x/v5 narration at src\InputDetection.cs ~13 and ~144-148, src\UI\OverheadRenderer.cs ~9-10, src\UI\Theme.cs ~254-255. Keep the technical explanation (why textureRect, why no Input patches, why no fallback anchor); drop the version-numbered history or phrase it as "earlier releases". No code changes in these edits. (Finding 5: all four)
A6 — README.md Installation step 1: "Install MelonLoader 0.7.x (tested on 0.7.2 Open-Beta, game 1.4.7)". Leave the rest of README alone. (Finding 6: Astra, GLM)
A7 — Mechanical: (a) csproj: confirm there is no copy/deploy target; add `<Version>1.0.1</Version>` to the first PropertyGroup; add an ItemGroup with `<Compile Remove="docs\**\*.cs" />` (the SDK glob otherwise compiles review baselines under docs\ — the sibling mods carry the same line); do NOT add any copy-to-Mods target. (b) `BuildInfo.Version` → "1.0.1". (c) CHANGELOG.md: new top section "## v1.0.1 — council pass (2026-09-11)" with one honest line per item A1-A6, plus a short "Legacy config keys" note listing the 12 inert v4.x keys (ShowWindow, ShowOverhead, WindowScale, WindowX, WindowY, IconAlpha, IconSize, OverheadY, EnabledKeys, Controller, SlotLabel0-5) as ignored and safe to delete by hand; NO code deletes them. (Findings 7, 8)

## EXPECTED OUTCOME
1. `dotnet build -c Release --nologo -v q -p:DeployToMods=false` (run from the repo root) → Build succeeded, 0 warnings, 0 errors.
2. NOTHING copied into `C:\Program Files (x86)\Steam\steamapps\common\Last Epoch\Mods\`: `medick_CooldownTracker.dll` there must remain 40,448 bytes dated 2026-07-01 20:58 (md5 c4210da03fef2976a49e0f7bad636b2a). Check it and print the result.
3. Each item present, each with a one-line comment naming the council finding it closes (e.g. `// council 2026-09-11 #1`).
4. Report: status word first (DONE / DONE_WITH_CONCERNS / BLOCKED); build command + tail; per-item one-liner with file:line; the Mods-folder check; anything not done and why.

## CONSTRAINTS
Minimum change per item; no refactors; no new files; keep comment style; do not touch files outside the WRITE SET; no game run; no commit; no deploy; `--no-restore` is acceptable if NuGet config is unreadable in the sandbox.

## MUST NOT
No spawns, no sub-agents, no edits outside the WRITE SET, no commits, no copying anything into the game's Mods folder, no running the game.

## OUTPUT FORMAT
Status word first, then the four report parts above. Under 60 lines.

## WRITE SET
medick_CooldownTracker.csproj · src\BuildInfo.cs · src\InputBlocker.cs · src\Prefs.cs · src\SlotRegistry.cs · src\InputDetection.cs · src\UI\OverheadRenderer.cs · src\UI\Theme.cs · README.md · CHANGELOG.md

## LAWS
"'I could not tell what you meant' is a good outcome. Propose, don't guess."
