# Doctor — Character Build Reference

Profession ID **10** (`Profession.Doctor = 10` in `AOSharp.Common/GameData/Profession.cs`). The game's primary healer.

> **Sourcing:** every cost factor is quoted verbatim from `reference/skillcaps.json` at `_professionOrder` index 3 (Doctor). Every nano id and cast requirement comes from `doctor-nanos.json` (nanos.ocp, client 18.8.50_EP1). Every perk-line name and tier count was confirmed as real client items in `itemnames.sql`. Web sourcing is wiki.aodb.us, ao-universe.com and forums.funcom.com. Unverified items are flagged at the end.

## Role & Playstyle

The Doctor's whole kit is in the local data: **240 castable nanos**, of which **138 are heals or heal support** (3 Complete Healing, 58 single-target heals, 38 team heals, 16 team HP buffs, 14 single HP buffs, 18 HoTs, 1 team HoT, 4 heal-delta, 3 Treatment/First-Aid, 2 cures) and **58 are DoTs** across three simultaneously-stacking strains.

It is the only profession with **Complete Healing** (28650) and its team version **Alpha and Omega** (42409), and one of very few with an **unbreakable** initiative debuff — **Uncontrollable Body Tremors / UBT** (99577).

The loop: team HP buff and Iron Circle up before the pull → UBT on the mob → a HoT rolling on the tank → spot-heals → hold the complete heals for spikes → stack one DoT from each strain in the gaps.

**Strengths**
- Complete Healing / Alpha and Omega — AOWiki: complete heals *"will always heal for 10001 health, ignoring heal modifying buffs"*.
- A heal ladder with a correct rung at every skill level, from Weak Team Heal (42408, BM 29) to Superior Team Health Plan (273312, BM 2153).
- A 16-step team HP buff line that raises the team's max health *and heals the amount raised*.
- Unbreakable init debuffs — AOWiki: they *"last the full duration of the nano or until the target dies, regardless of damage dealt"*.
- Three DoT lines that run at once, so it kills without a good weapon. Wrack and Ruin (28640) also strips **100** from every base attack skill.
- **Nine skills at cost factor 1.0** — BM, MM, Treatment, First Aid, Nano Pool, Nano C. Init, Computer Literacy, Tutoring, Pharma Tech.
- Never turned away from a team, at any level.

**Weaknesses**
- No burst. AOWiki: must *"gradually wear down opponent defenses rather than burst damage"*.
- Pistol (1.6) is the only cheap weapon; Rifle/AR/Ranged Energy/Bow are all **4.0**.
- Evade-ClsC is **3.2** — its worst skill — and there is no evade, reflect or absorb nano in the kit at all.
- Healing generates aggro and there is no taunt-dump.
- NCU pressure: it runs more standing buffs than anyone.

## Breed

**Recommended: Solitus.** Its healing schools are *already* cost 1.0, so extra nano skill buys less than it does for a Meta-Physicist — while the Doctor is the aggro magnet and its own armor is **Stamina-gated** (Ofab Doctor QL300 needs Stamina **1099**, Psychic **854**; Chosen/Faithful QL300 needs Stamina **1099**, Psychic **899** — read from items.ocp). AO-Universe lists Solitus as *"a prefered breed among Adventurers, Doctors and Soldiers"*.

| Alternative | Why |
|---|---|
| **Nanomage** | Also explicitly named for Doctors by AO-Universe (*"preferred among Metaphysicists, Nanotechnicians and Doctors"*). Biggest nano pool and best nano-cost. Pays in HP/AC on the class that gets hit. |
| **Atrox** | The "troxdoc". Most HP and Body Dev — answers the real failure mode. Costs nano pool and Int/Psychic. |
| **Opifex** | Best evades — but Evade-ClsC is 3.2 for a Doctor whatever the breed, so breed can't fix it. |

## Skill priorities (cost factors are exact, from skillcaps.json)

| Skill | Cost | Target | Why |
|---|---|---|---|
| Biological Metamorphosis | **1.0** | max | Primary requirement on all 138 heal/HP-buff nanos. AOWiki: "Always max" |
| Matter Metamorphosis | **1.0** | max | Second requirement on every heal. "Always max" |
| Treatment | **1.0** | max | Gates every implant/symbiant QL. "Always max" |
| Psychological Modification | 1.6 | max | UBT needs BM 861 **and** PM 861. "Always max, even after you can cast UBT" |
| Nano Pool | **1.0** | max | "Always Max" — the nano bar is the fight timer |
| Body Dev. | 2.0 | max | "Always Max" — the only mitigation a Doctor owns |
| Nano C. Init. | **1.0** | max | Cast/recharge speed = healing throughput |
| Computer Literacy | **1.0** | max | NCU is the binding constraint |
| Matter Creation | 1.6 | high | Short HP buffs (Life Channeler MC 781), heal-delta (MC 619), DoTs. AOWiki: *"cannot forego Matter Creation"* |
| Time & Space | 1.6 | high | **The 16 team HP buffs are the only Doctor line requiring it** (95709 = SpaceTime 769) |
| Sensory Improvement | 1.6 | high | Epsilon Purge (696), Superior First Aid (517), Improved Instinctive Control (1033) |
| First Aid | **1.0** | high | Stim QL — the one instant heal that isn't a cast |
| Nano Resist | 1.2 | high | Unusually cheap; stops drains and debuffs mid-heal |
| Pistol | 1.6 | as-needed | The only cheap weapon; 17 of 23 Doctor-locked weapons are pistols |
| Fling Shot | 2.4 | as-needed | Cheapest pistol special (Burst 3.0, Aimed Shot 3.2, Full Auto 4.0) |
| Rifle / AR / Ranged Energy / Bow / Multi Ranged / Full Auto / Aimed Shot | **4.0** | skip | The most expensive tier in the table |

## Perks — all names and tier counts confirmed in `itemnames.sql`

**Profession (SL):** Assault Force Medic (10) · Specialist Healer (10, ids 212039-212048 — the client items carry **HealMultiplier +2/+3**) · Nano Surgeon (6)
**Group (SL):** Nano Doctorate (10) · Starfall (10) · Alchemist (6) · Pistol Mastery (5) · Power Up (8)
**General (SL):** Genius (3) · Kung-Fu Master (5)
**Alien (AI):** **Champion of Nano Combat (10 — +100 to all six nano skills)** · **Embrace (10 — +50 MM, +1000 Max Nano, −4% nano cost)** · The Unknown Factor (8)

Maxed, the profession + Nano Doctorate + Champion of Nano Combat lines are worth roughly **+285 BM and +285 MM, +100 Treatment, +70 First Aid and +21% healing** — permanent, no drop RNG.

## Research (Lost Eden) — 7 personal lines, all 10 levels, all confirmed in itemnames.sql

| Line | Gives | Procs |
|---|---|---|
| **Bedside Manner** | Pistol skill | Healing Care, Antiseptic |
| **Internship** | NCU + Pharma Tech | Blood Transfusion, Pathogen, Massive Vitae Plan |
| **Rehabilitation** | BM/MM | Inflammation, Astringent |
| **Diagnosis** | Health + Stamina | Anesthetic |
| **Toxicology** | Matter Creation + nano capacity | Anatomic Blight, Dangerous Culture |
| **Underground Doctor** | **5% healing efficiency** (perk item 261175 carries HealMultiplier **+5**) | Restrictive Bandaging |
| **Aggressive Surgery** | Ranged combat | see the Muscle Memory / Muscular Malaise note |

All twelve proc names match the twelve `LEProcDoctor*` entries in `AOSharp.Common/GameData/PerkHash.cs`.

## Where a Doctor differs from the MP template (`_deviations`)

- **Breed flips to Solitus.** The MP wants Nanomage because its power scales with nano skill; the Doctor's healing schools are already 1.0 and its armor is Stamina-gated.
- **No pet dimension at all** — the build is organised around heal-ladder rungs (BM 29 → 2153), not pet tiers.
- **Treatment and First Aid are 1.0 for a Doctor and 2.0 for an MP**, which is why "max Treatment every level" is a hard rule here.
- **Weapon skills matter slightly more** — Pistol 1.6 vs 2.4, and 23 Doctor-locked weapon models exist.
- **The alien perk lines are worth maxing** (Champion of Nano Combat, Embrace), where the MP's alien entry is "Ancient Knowledge — bugged, avoid".
- **Research→proc mapping is fully sourced**, unlike the MP file's inferred Sow Doubt / Sow Despair / Thoughtful Means.
- **Evades are treated as a lost cause**, not a priority (MP evades are 1.6/1.6/2.4; Doctor's are 2.4/2.4/3.2).

## Flagged / unverified

- Which of **Muscle Memory** (PerkHash `LEProcDoctorMuscleMemory`; itemnames 263491/263514/267034) and **Muscular Malaise** (266812, named by the wiki as the Aggressive Surgery proc) is the perk and which is the nano it fires.
- Per-tier perk values and unlock levels — only **max-rank** totals are sourced. `recommendedAtLevel` is priority guidance, not a gate.
- The Solitus recommendation is a judgement: AO-Universe names *both* Solitus and Nanomage for Doctors, and wiki.aodb.us `Doctor:Breed_and_Skills` names **no** recommended breed.
- Specialist Healer's "+21% healing" is the wiki's number; the client data confirms only HealMultiplier +2/+3 per item.
- *"Over 95% of all team deaths are due to a failure on the doc's part"* is from a **satirical** Funcom-forums guide — recorded as team expectation, not mechanic.

See `doctor-build.json` for the machine-readable version with every id and citation.
