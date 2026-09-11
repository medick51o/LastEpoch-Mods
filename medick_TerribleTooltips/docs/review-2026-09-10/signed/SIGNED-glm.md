# 🟣➤🔷 GLM (glm-5.2-high via Cursor pool, 💸 CREDITS 50636 in / 3201 out) — top-to-bottom review (2026-09-10, read-only, settings / config / break-experience lens)
sessionId b3132856-ce7a-4eee-9b62-1651cd4bcfb8 · manifest hashes UNAVAILABLE (seat substituted line counts; not fabricated)

DONE_WITH_CONCERNS

| Rank | Claim | Failure mechanism | Verify-by / repair | Cite |
|---|---|---|---|---|
| BLOCKER | Master toggle off does not restore default tooltips (tier colours) | OnLateUpdate ends with an UNCONDITIONAL re-apply of s_tierColorCache, running even when EnableTooltips=false; cache never cleared on master-off. | Master off, hover an item with a standalone Tier widget → still TT-tinted. Gate the loop by EnableTooltips and clear the cache on off. | TooltipRecolor.cs:204-208 |
| BLOCKER | Master off leaves suppressed Range rows blank | s_suppressedRanges enforcement is gated by EnableTooltips && !DeepRange; when master goes off the block stops running, rows already emptied to Marker stay blank. | Master on + ranges off, hover a unique so a Range row is suppressed; master off → row stays blank. Restore originals on master-off; clear the dicts. | TooltipRecolor.cs:177-200 |
| MATERIAL | cfg `GroundLabels = "FilterOnly"` is an orphan key | No such MelonPreferences entry (real key GroundLabelFilterOnly). Hand-editing changes nothing, silently. | Rename/remove in the shipped cfg; document. | cfg:5 vs Prefs.cs:104 |
| MATERIAL | cfg `SignalText = "Spelled"` is an orphan key | No SignalText entry; SignalStyle is Badge/PlainText only. | Drop the line or ship the feature. | cfg:30 vs Prefs.cs:66 |
| MATERIAL | Preference change while a tooltip is open does not update it | Settings toggles call Prefs.Save only; none call MarkDirty. | Callbacks call TooltipRecolor.MarkDirty(). | SettingsUi.cs:43,57,67 vs TooltipRecolor.cs:81 |
| MATERIAL | Ground-label pref change leaves existing labels stale | Postfix fires only on SetGroundTooltipText; Marker guard blocks reprocessing. | Document "re-drop to refresh" or rescan on pref change. | GroundLabels.cs:74-77,101 |
| MATERIAL | Enabling Hold-Alt mid-session is inconsistent | Labels rendered in always-visible mode were never added to s_altCache; they keep brackets without Alt. | On toggle-on, walk existing labels and repopulate. | GroundLabels.cs:128-139 vs 142-145 |
| MATERIAL | README "the log tells you exactly what degraded" is FALSE for the tooltip pair | TooltipRecolor depends on AffixInjector's brackets; if Patch_AffixFormatter is disabled, UpdateLayout still "succeeds" and silently no-ops. | RunScan detects "zero brackets across N scans" → one-shot warning. | TerribleTooltipsMod.cs:58-65; TooltipRecolor.cs:330-498 |
| MATERIAL | Prefs.Save failure is silent | try{}catch{} — a locked/invalid cfg write is swallowed; value persists in-memory, reverts on restart. | MelonLogger.Warning in the catch. | Prefs.cs:122-127 |
| MINOR | WarnDegradedOnce reports the first broken row only | _degradedWarned latches globally. | Latch per detail. | NativeSettings.cs:36-46 |
| MINOR | _keepAlive grows per settings-panel open | Delegate sets added each Awake; never pruned. | Clear at the top of a successful Build. | NativeSettings.cs:38-39 |
| MINOR | Stale cfg comment above the orphan key | — | Delete orphan + comment. | cfg:4-5 |
| NOT PROVEN | Empty loot filter breaks ground labels | FilterOnly + empty filter → CheckFilter false → yield break before SetText, native label intact. Correct behaviour. | No repair. | GroundLabels.cs:108-109 |

README claim per feature: Ground labels TRUE · Filter rule # TRUE · Settings panel TRUE (tonight's log proves it) · Aaron's House TRUE · Tooltip colours FALSE (coupled pipeline; incomplete log story).
Silent player-visible failures: orphan cfg keys; Prefs.Save failure; master-off leaving tier colours stamped and Range rows blank; layout/style/AltKey changes not refreshing open tooltips or dropped labels; TooltipRecolor becoming a no-op after an upstream patch is disabled.

🟣➤🔷 GLM
