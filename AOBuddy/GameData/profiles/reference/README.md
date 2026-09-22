# AOBuddy10 Reference Data

Canonical Anarchy Online reference data for the character-planning web app. Every value is sourced from local client enums or reputable AO community references; each file carries a `_sources` array. Values that could not be fully confirmed are marked `unverified` (in strings, `(unverified)`, or `"unverified": true`).

Local enums are the authority for names/ids so the app can join by id:
- Professions: `AOSharp.Common/GameData/Profession.cs`
- Skills / abilities / ACs: `AOSharp.Common/GameData/Stat.cs`
- Implant slots: `AOSharp.Common/GameData/EquipSlot.cs`

## breeds.json
The 4 breeds (Solitus=0, Opifex=1, Nanomage=2, Atrox=3).

- `_abilityOrder`, `_abilityStatIds`, `_breedStatValues` — join keys.
- `breeds[]`:
  - `id`, `name`
  - `creationBase` — level-1 starting ability values (Str/Agi/Sta/Int/Sen/Psy).
  - `trainedAbilityCap` — max each ability can be IP-trained to (the breed "cap" planners use; level-200 values, agreed by AO-Universe and aodb.us).
  - `maxAbility220Unverified` — AO-Universe's higher second figure per ability (interpreted as a level-220 absolute max incl. external buffs; interpretation not fully confirmed).
  - `hpPerBodyDev`, `nanoPerNanoPool` — Max Health per Body Dev point, Max Nano per Nano Pool point.
  - `baseHealthDelta`, `baseNanoDelta`, `nanoCostReductionCapPct`.
  - `traits[]` — short breed perk/trait notes.

## professions.json
The 14 playable professions (Monster=13 excluded).

- `_breedAvailability` — key finding: **no breed restrictions**; all 4 breeds can roll every profession. Keeper/Shade are expansion-gated (Shadowlands), not breed-gated.
- `professions[]`: `id`, `name`, `role` (one-line), `allowedBreeds` (always all 4), `recommendedBreeds` (community synergy preference), `froob` (free-to-play selectable), `expansion`.
- `_notPlayable[]`: Monster (id 13).

## skillcaps.json
Per-profession skill **cost factors** (the canonical AO skill-cost/colour table). Cost factor = IP-per-point multiplier; lower = cheaper = higher attainable cap.

- `_professionOrder` / `_professionIdOrder` — the 14-element column order every `cost` array is aligned to.
- `skills[]`: `category`, `name`, `stat` (Stat.cs enum name, or null for legacy skills), `statId`, `dependency` (ability synergy %, best-effort), `cost[14]` (aligned to `_professionOrder`).
- Cost factors are the load-bearing, cross-checked data. `dependency` strings are best-effort (the extractor was inconsistent on a few rows — notably Body Dev. and Nano Pool); rows marked `(unverified)` should be confirmed against client data.
- This file gives cost factors, **not** a precomputed level-220 cap table. Computing exact caps needs the AO title-level trainable-cap curve combined with these factors plus ability contribution — see `_capComputationNote`. That curve is intentionally not reproduced here (would require its own verified source); flagged as future work.

## implants.json
The implant system.

- `_grades` — Shiny/Bright/Faded bonus tiers and their minimum-QL-% rules (86/84/82).
- `_clusterInvariant` — each cluster has exactly one legal (slot, grade) home.
- `equipRequirementModel` — Treatment required `trunc(4.723717064*QL + 6.767295257)` (~`QL*4.7+11`); governing Ability required `QL*2+4`; plus the laddering concept.
- `clusterBuildRequirement` — Nano Programming to assemble a cluster (Shiny `2*mult*QL`, Bright `1.5*mult*QL`, Faded `1*mult*QL`).
- `slots[]` — 13 implant slots (`slot` = EquipSlot.cs Imp_* name, `slotId`, `label`) each with `shiny` / `bright` / `faded` cluster lists.
- `_clusterStatMap` — joins each cluster display name to its Stat.cs `{stat, id}`. `sourceName` records the original source label where it differed; `unverified: true` flags old/ambiguous names whose modern mapping is inferred (e.g. ThrownGrapplingWeapons→HeavyWeapons, MaterialLocation→SpaceTime, Climb→Adventuring, AttackSpeed, Nano AC).

## Confidence & known gaps
- **High:** breed caps, HP/nano modifiers, profession list/ids/breed-availability, skill cost factors, implant cluster-to-slot mapping, implant equip formulas.
- **Marked unverified:** Nanomage `creationBase` Sense (4 vs 6 across sources) and Atrox Psychic base; the `maxAbility220Unverified` interpretation; several skill `dependency` percentages; a handful of legacy implant cluster→stat mappings; and the absence of a precomputed 220 skill-cap table (cost factors provided instead).
- To harden the unverified items, cross-check against local client data (`items.dat` for exact implant requirements; client character-creation data for exact breed base abilities).
