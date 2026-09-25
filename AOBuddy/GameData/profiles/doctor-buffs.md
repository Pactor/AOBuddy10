# Doctor — Buffs Reference

Profession ID **10**. The Doctor is the heal class, so the load-bearing section here is **what it gives**.

> Every id, name, nanoLine and cast requirement is read from `doctor-nanos.json` (nanos.ocp, client 18.8.50_EP1) and re-verified against `itemnames.sql`. Cross-profession buff ids are `null` — they belong to other professions and are not in the local Doctor data.

## A finding from the local data: the "Heals - Team" category is two different lines

`doctor-nanos.json` lumps **54** nanos under *Heals - Team*. They split cleanly on their cast requirements:

| Split | Count | Cast requirement | What it is |
|---|---|---|---|
| **Team HP buffs** | **16** | BM + MM + **SpaceTime** | wiki nanoline 184 — *Enhance Team Health … Superior Omni-Med Enhancement*. Raises the team's max health and heals the amount raised. Lasts ~1h. |
| **Team heals** | **38** | BM + MM only | The team-heal ladder proper. |

Both families cast via `TeamCastNano` children, which is why the extractor could not tell them apart. **This is the reason a Doctor cannot skip Time & Space** — no other line in its 240-nano kit requires it.

## What a Doctor gives (the important half)

### The complete heals — nothing else in the game has these
| Nano | Id | Requirement | Note |
|---|---|---|---|
| **Complete Healing** | 28650 | BM 794 / MM 722 | AOWiki: complete heals *"always heal for 10001 health, ignoring heal modifying buffs"* |
| **Improved Complete Healing** | 270747 | BM 2153 / MM 2153 + NanoFocusLevel BitAnd 64 | Endgame |
| **Alpha and Omega** | 42409 | BM 873 / MM 786 | The **team** complete heal |

### The ladders (full enumerations are in the JSON)
- **Team heals — 38 rungs**, Weak Team Heal (42408, BM 29) → Superior Team Health Plan (273312, BM 2153). AOWiki rates **Distributed Care (43892)** as *"the best team heal when rated at amount healed per second"* — the highest requirement is **not** automatically the best heal-per-second.
- **Team HP buffs — 16 rungs**, Enhance Team Health (42397, BM 18) → Superior Omni-Med Enhancement (95709, BM 852 / MM 769 / SpaceTime 769). Who_Buffs_What: +26 to +920 Max Health.
- **Single HP buffs — 14 rungs**, Health Augmentation (96248, +57) → **Life Channeler (96247, +1344)**. Needs **Matter Creation** as well (MC 781). Lasts ~8m20s. Team version: Team Improved Life Channeler (275011).
- **HoTs — 18 rungs**, topped by Deathless Blessing (43852, 277-323/tick, BM 864). Team HoT: **Team-Enhanced Deathless Blessing (269455)**, Shadowlands only.
- **Single heals — 58 rungs**, topped by Bodily Invigoration (223299, **7215-8385**, level 220) and Cellular Rebirth (223297, 6001-7639, level 218). The 2232xx family are SL level-gated (Specialization BitAnd 8 + Expansion BitAnd 2); the 438xx family are the unrestricted Rubi-Ka heals a froob runs.

### The standing team buffs
| Nano | Id | Effect |
|---|---|---|
| **Iron Circle** | 42400 | Strength +20, Stamina +20 (lesser: Enlarge 28658, +10/+10) |
| **Improved Instinctive Control** | 222856 | **Nano C. Init +350**, Nano Resist +75, Max Nano +300 — the Doctor's biggest non-healing gift, handed to every caster in the raid |
| **Improved Nano Repulsor** | 222823 | Nano Resist +200 |
| **Superior First Aid** | 28675 | First Aid +80, **Treatment +80** — makes a Doctor a walking implant ladder for its whole org |
| **Continuous Reconstruction** | 222824 | Heal Delta +65 |
| **Vaccine of Divestiture** | 204431 | Resists the Trader drain strains 26%/15% (level 195; 3 lesser tiers below) |

### The debuffs that are really team buffs
| Nano | Id | Requirement | Effect |
|---|---|---|---|
| **Uncontrollable Body Tremors (UBT)** | 99577 | BM 861 / PM 861 | **Unbreakable** — *"lasts the full duration of the nano or until the target dies, regardless of damage dealt"*. A raid-wide damage reduction dressed as a debuff. |
| Rapid Palsy | 301845 | BM/PM 569 | Also unbreakable |
| Induce Muscle Spasms / Muscle Atrophy / Tired Limbs | 99583 / 99582 / 99578 | BM/PM 389 / 93 / 29 | The lower ladder; Tired Limbs scales to −2500 on all four initiatives |
| **Wrack and Ruin** | 28640 | — | DoT that also strips **100** from every base attack skill |
| Misdiagnosis | 266306 | Lost Eden, BM/MM 799 | **Heal Multiplier −50** — the anti-healer PvP nano (shared with Adventurers) |

## What a Doctor should ask for

| From | Buff | Why |
|---|---|---|
| **Meta-Physicist** | **Composite Mochams (+140 all six nano skills)** | The #1 ask. +140 BM/MM is often exactly one rung up the heal ladder. |
| **Trader** | Wrangle / team wrangle | The equip window for the next symbiant QL, plus a heal tier you can't normally cast. |
| **Enforcer** | Essence of Behemoth (Str/Sta + Max Health) | **Your own armor is Stamina-gated at 1099.** Different nanoline from Iron Circle, so they stack. |
| **Fixer** | NCU, team run speed, HoT | NCU is your binding constraint. |
| **Keeper** | Reflect and heal auras | Reduces the damage you have to out-heal — a force multiplier, not a stat stick. |
| **Agent** | Feline Grace (+25 Agi), Enhanced Senses (+15 Sense), Chemistry/Pharma | Gear requirements and tradeskills. |
| **Adventurer** | Robust Treatment, Eagle Eye, run speed | Treatment stacks on top of your own Superior First Aid. |

⚠️ **Trap:** Who_Buffs_What puts the Enforcer Essence line **and** the Soldier HP buffs both on nanoline **151** — they do **not** stack. Take whichever is bigger.

## Bot cast order

**Pre-fight:** team HP buff → Iron Circle → Improved Instinctive Control (self + every caster) → Continuous Reconstruction → Improved Nano Repulsor + best Vaccine if needed → Life Channeler on self and tank immediately before the pull (it only lasts ~8m20s).

**In fight:** UBT on the mob → HoT on the tank → spot-heal → team-heal when 2+ are hurt → **hold** the complete heals for spikes → DoTs only when nobody needs healing.

## Nanoline stacking traps (a bot must not double-cast these)

`InitiativeBuffs` (28669 ↔ 222856) · `NanoResistanceBuffs` (28671 ↔ 222823) · `StrengthBuff` (42400 ↔ 28658) · `FirstAidAndTreatmentBuff` (28657 / 28674 / 28675) · `HealDeltaBuff` (28663 / 28651 / 222855 / 222824) · `Ransack_DepriveResistBuff` (all four Vaccines) · `DoctorShortHPBuffs` (all 14).

## Froob ceiling

A free account loses: Improved Instinctive Control (222856), Team-Enhanced Deathless Blessing (269455), every level-gated 2232xx single heal, Misdiagnosis (266306), and everything gated on `NanoFocusLevel BitAnd 64` (270747, 273315, 273312, 275011, 275701) — plus all research procs and alien perks.

It **keeps**: Complete Healing, Alpha and Omega, 36 of 38 team heals, all 16 team HP buffs, all 14 short HP buffs, the full HoT ladder, all four heal-delta buffs, Iron Circle, Instinctive Control, Nano Repulsor, Superior First Aid, Epsilon Purge and **UBT**. Still the best healing kit in the game.

## Flagged / unverified

- **Heal amounts for the 38 team heals and 16 team HP buffs are not in this file** — each parent nano only carries a `TeamCastNano` pointer to a child (e.g. 95709 → 95673) and the children are not in the Doctor's castable extraction. Who_Buffs_What's ranges are quoted as web values.
- *"10001 health"* is AOWiki's figure. The local effect summary for 28650/270747 is `Hit[27,100,0,0]`, which does not obviously encode it — the argument encoding was not decoded.
- Which effects nano strains **6/7/8/9/582** (Epsilon Purge 28659) and **883** (Tjernberg Soothing Adrenaline 279372) correspond to.
- The Enforcer/Soldier nanoline-151 collision comes from a single reading of Who_Buffs_What.
- Misdiagnosis (266306) and Fill Inbox (266303) each carry **two** `Profession EqualTo` requirements in the flattened data; the OR/AND grouping was not verified from the raw action tree.
- Durations are AOWiki's; no duration field was read from the local nano data.
- DoT tick counts and totals were not derived, so no DoT DPS comparison is attempted.
