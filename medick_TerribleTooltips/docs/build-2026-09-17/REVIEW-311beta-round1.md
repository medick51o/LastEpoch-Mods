# Native same-lineage self-check — round 1

Reviewer: `/root/review_beta`, native OpenAI agent. Read-only. Not independent approval.
Verdict: NEEDS REPAIR.

Reviewed MD5s:

- TooltipRecolor.cs: `c14a38bce884aafbc9f29ed594be14ba`
- FilterRuleTooltip.cs: `2e96970d31ee6329b7866cd8aa1cefed`
- BuildInfo.cs: `77b1e6c9a9c5a7d564dccea893241a33`
- CHANGELOG.md: `c99f59cb56483bb9754aa933ccb73106`
- DELTA-311beta.patch: `f464853b2fcf42d77762e26f26209e60`

BLOCKER: persisted marker on a fresh capture set completion without resolving a rule; a later same-content rewrite could never get Rule# back. ACCEPT. Repair separates known-destination repair from exhausted discovery, resolves the matching rule once when needed. Regression harness covers persisted-marker reuse after the original deadline.

BLOCKER: same-content rewrite while master/display off left completion latched, preventing restoration upon reenable. ACCEPT. Known-destination repair remains pending across disabled preferences and resumes without reopening descendant discovery. Harness covers both master and display switches without a further setter.

NOT PROVEN: ID-less items sharing owner/target/type might represent different content. ACCEPT as a runtime limitation. Added stable type/subtype/rarity/unique/individual fields to the ID-less fallback, retaining the bound for equal wrappers; harness covers different material subtypes. Distinct ID-less items with equal fallback fields remain indistinguishable until ownership changes; serialized-ID availability in the reporter's path remains unobserved.

Retirement ordering, bounded matching/discovery, and gated telemetry had no additional findings. Rendering/native timing remains untested. A fresh second self-check is required after repair.
