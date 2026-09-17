# Draft reply to Trunks1981 — ANDREW POSTS THIS. Claude never posts to Nexus.

**Thread:** Nexus bug report "Stutter Over Affixed Shards" (filed 2026-09-12 vs v3.0.2)
**Link to give him:** https://github.com/medick51o/LastEpoch-Mods/releases/tag/terrible-tooltips-v3.1.1-beta1

---

Thanks for the report, and for attaching a debug log — that is exactly how to file one, and it
made this a lot faster.

I found two separate every-frame loops in the tooltip code, either of which can do what you're
describing:

1. When the game recycles a tooltip text field for plain crafting text, the mod loses track of
   it and ends up re-scanning the entire scene every frame. Gear text recovers on its own;
   shard text never does, so it just keeps scanning for as long as you hover.
2. The loot filter rule number needs somewhere to write itself — on gear that's the
   requirements line. A crafting shard doesn't have one, so the mod kept hunting for it every
   single frame, forever.

Both are fixed in a beta build here:
https://github.com/medick51o/LastEpoch-Mods/releases/tag/terrible-tooltips-v3.1.1-beta1

**I want to be straight with you: I have not been able to test this myself.** I couldn't get
in front of the game to reproduce your exact setup, so what you're getting is a fix aimed at
two problems I can prove exist in the code, not a fix I've watched work. That's exactly why
it's a beta and why it's on GitHub instead of the main Nexus file — I'm not pushing an
unverified build at everyone who uses this mod. If you'd rather wait until I can confirm it
myself, that's a completely fair call and no hard feelings.

**If you are up for testing it:**
1. Back up your current `medick_Terrible_Tooltips.dll` out of your Mods folder.
2. Drop the beta DLL in its place.
3. With the game CLOSED, open `UserData/MelonPreferences.cfg` and set `DebugLog = true` in the
   Terrible Tooltips section. (The game rewrites that file when it quits, so editing it while
   the game is running won't stick.)
4. Play normally, then hover Affix Shards the way you did when you filed this.
5. Send the log back.

This beta prints a `[perf]` summary line every 5 seconds while DebugLog is on — it counts scans,
rule lookups that found nothing, and stale entries cleaned up. That tells me directly whether
these two fixes actually caught your case, instead of us both guessing from "feels smoother."
It's one line per 5 seconds, and it costs nothing at all when DebugLog is off.

**If the stutter is STILL there, here's what would let me fix it properly:**

- The full `Last Epoch\MelonLoader\Latest.log` (the whole file, not a snippet) from a session
  where it happened, with `DebugLog = true`.
- Roughly what time you hovered the shards, so I can find that part of the log.
- Your `UserData/MelonPreferences.cfg` — I need to see which of my settings were switched on.
- Your full mod list with versions (the MelonLoader console prints it at startup) and a
  screenshot of your Mods folder.
- Where exactly you were hovering: inventory, stash tab, or the crafting panel? A screenshot of
  the shard with the tooltip open helps more than you'd think.
- Two quick A/B tests, if you don't mind:
  (a) In my mod's settings, set **"TT - Rule Display" to Off**, then hover the same shards. Does
      the stutter go away? That single answer tells me which of the two loops is yours.
  (b) Temporarily rename `FallenStar's Improved Tooltips` DLL so it doesn't load, and hover
      again. That tells me whether the two mods are fighting over the same tooltip.
- Frame time or FPS numbers before and during the hover, if you have a counter up.

Any one of those helps; all of them and I can almost certainly land it. Thanks for sticking with
the mod — reports like yours are how it gets better.

— medick
