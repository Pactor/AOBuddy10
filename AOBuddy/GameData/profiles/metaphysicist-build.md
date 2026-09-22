# Meta-Physicist (Metaphysicist) — Character Build Reference

Profession ID **12** (`Profession.Metaphysicist = 12`, `Profession.MetaPhysicist = Bit12` in the local enum). Pet-based nuker/support hybrid.

> Sourcing: all guidance below is traced to reputable AO sources — the official Funcom-forums MP guide, wiki.aodb.us (AOWiki), and the Fandom AO Knowledge Base — plus the local `PerkHash.cs`/`Profession.cs` enums. Unverified items are flagged in the last section.

## Role & Playstyle

The Meta-Physicist commands **up to three floating AI pets** — an **attacker**, a **healer**, and a **mezz/crowd-control** pet — and supplements them with nukes, debuffs, and the game's second-best nanoskill buffs. You keep the right pets up, keep them and the team topped, and control the fight with mezz and debuffs. You are not a weapon platform; the pets do the tanking and most of the damage.

**Strengths**
- Three simultaneous pets → very versatile solo and in teams.
- Second-best nanoskill buffs in the game — Mocham's Gifts and Composite Infuses give **+140 nanoskills** (only Trader's +147 is higher).
- Strong nuke + debuff kit (Mind Quake debuff line, Trader-like debuffs).
- Pets are now persistent (no time-out), so wrangles let you cast pets above your self-cast level.

**Weaknesses**
- Low, expensive weapon skills (1HB, 2HE, Bow, Pistol) → weak personal weapon damage.
- Squishy — depends on pets to tank; fragile if caught without them.
- Pet-management overhead (positioning, re-casting on death, over-equip / body-pool concerns).
- Ancient Knowledge (AI) pet effects are bugged.

## Breed — recommended: **Nanomage**

Nanomage has the highest **Intelligence and Psychic**, which most directly raise the nanoskills an MP lives on. That means higher-tier pets, nukes and buffs sooner, for less IP, plus the biggest nano pool for sustained casting. The Funcom-forums MP guide calls Nanomage optimal: *"Nanomage have most Int and Psy… Int ability will boost your nano skills most"* and *"You will need a lot of MC and TS all the time 1-220."*

| Breed | Verdict |
|---|---|
| **Nanomage** | **Recommended.** Best Int/Psy → best nanoskills, nukes, buffs, and nano pool. Fragile HP/AC. |
| Solitus | Balanced, forgiving IP spread, better HP/AC. Safe alternative. |
| Atrox | Most HP / best Body Dev, but low nano pool and Int/Psy → nanoskills cost more IP. Survivability/froob pick. Nano Pool is "especially important for the Atrox breed." |
| Opifex | Best evades / run speed for kiting; lower Int/Psy. Niche. |

## Perks

### Shadowlands (SL) perk lines
- **Soothing Spirit** (10 tiers, L30–203) — **top priority, max first.** ~50% bonus to heal-pet healing at max, plus *Spirit of Blessing* (team heal ~1,300 HP) and *Spirit of Purity* (team nano ~1,300). "The best line an MP can specialize in."
- **Channel Rage** (10 tiers, L10–210) — attack-pet buff: +30 AddAllOff and +300 all damage types at max; +210 HP and resists to the MP. Value debated. (med)
- **Starfall** (10 tiers) — +400 max nano, +160 health, and a 4-step damage special chain (Dazzle with Lights → Combust → Thermal Detonation → Supernova). (med)
- **Blunt Mastery** (rank 4 @ L80, shared with Enforcer) — +45 1HB and Quick Bash; only for a 1HB MP. (low)

### Alien / AI perks
- **Ranger** (rank 6) — +75 Bow, +38 Bow Special, Clearshot/Popshot; only for a bow MP. (low)
- **Ancient Knowledge** — intended pet buff but **bugged**; skip until fixed. (low)

### LE / Research proc perks (from `PerkHash.cs`)
The 12 `LEProcMetaPhysicist*` procs map to the personal research lines below. Offensive procs deal damage on fire; defensive/sustain procs raise evasion, restore nano/focus, or mitigate.

| Proc perk (enum) | Research line | Type |
|---|---|---|
| Ego Strike | Foresight | offense |
| Anticipated Evasion | Foresight | defense (evasion) |
| Super-Ego Strike | Sympathy | offense |
| Suppress Fury | Sympathy | defense |
| Mind Wail | Perseverances | offense |
| Regain Focus | Perseverances | sustain (nano/focus) |
| Nanobot Contingent Arrest | Perseverances | control |
| Diffuse Rage | Spatial Awareness | defense |
| Economic Nanobot Use | Spatial Awareness | sustain (nano cost) |
| Sow Doubt | Angst / Jealousy / Trauma *(line unverified)* | debuff |
| Sow Despair | Angst / Jealousy / Trauma *(line unverified)* | debuff |
| Thoughtful Means | Angst / Jealousy / Trauma *(line unverified)* | utility |

Typical setup: one offensive proc (Ego/Super-Ego Strike or Mind Wail) + one defensive/sustain proc (Anticipated Evasion or Regain Focus).

## Research — seven **personal** LE lines (AOWiki)

All MP profession research lines are **personal** (10 levels each, funded by redirecting a % of XP), not global. Standard cross-profession global lines apply as for any profession.

- **Foresight** — Int/Quantum-Physics scaling; procs **Ego Strike** + **Anticipated Evasion**. Good balanced offense/defense pick.
- **Perseverances** — Health/defense scaling; procs **Regain Focus**, **Mind Wail**, **Nanobot Contingent Arrest**. Survivability + offense proc.
- **Sympathy** — Nano-programming/Int scaling; procs **Suppress Fury** + **Super-Ego Strike**. Nanoskill support + strong offense proc.
- **Spatial Awareness** — Evasion/Stamina; procs **Diffuse Rage** + **Economic Nanobot Use**. Defensive/sustain.
- **Angst** — Strength + nano-skill progression to psychic abilities.
- **Jealousy** — Melee/exploit-weakness, nano-cost reduction. Low priority for a caster.
- **Trauma** — Ranged/bow (Aimed Shot, Bow proficiency, initiative). Only for a bow MP.

## Skill / IP priorities

**Max (dark-blue core nano schools — these gate every pet, nuke and buff):**
- **Matter Metamorphosis (MM)** — "always max if possible"
- **Biological Metamorphosis (BM)** — "always max"
- **Matter Creation (MC)** — "always keep maxed"; heavy use 1–220
- **Time & Space (TS)** — "always max"; needed all levels

**High:**
- **Psychological Modification (PM)** and **Sensory Improvement (SI)** — buffs/mezz/debuffs; max when possible
- **Nano Pool** — fuel for constant casting (critical at low levels / for Atrox)
- **Nano C. Init** — faster cast/recast
- **Computer Literacy** — "max this out," needed for NCU/decks/tradeskills
- **Treatment** — "max this out," heal & nano kits, implants
- **Evades** (Dodge Ranged, Evade Close Combat, Duck Explosives) — keep high/max whenever possible

**As-needed:**
- **First Aid** — emergency self-stims
- **Body Development** — raise if dying a lot (more value on Atrox)
- **Run Speed** — spare IP, positioning/kiting
- **Weapon skills** — expensive and secondary to pets; only for a specific weapon plan (e.g. bow + Ranger/Trauma)

**Headline:** max the four core nano schools (MM/BM/MC/TS), then PM/SI + Nano Pool + Nano Init, then Comp Lit/Treatment and evades; weapons last.

## Unverified / caveats
- Research-line assignment of **Sow Doubt / Sow Despair / Thoughtful Means** is inferred (Angst/Jealousy/Trauma), not confirmed in fetched sources.
- Per-proc offense/defense labels are inferred from names/effects; `aoitems.com/perk/prof/meta-physicist` returned unrelated content and could not confirm details.
- The Fandom `Meta-Physicist:Perks` page returned HTTP 402; perk detail relies on wiki.aodb.us + the Funcom-forums guide.
- Exact per-tier values beyond the summary figures quoted from AOWiki are not verified.
- The single recommended breed (Nanomage over Solitus) reflects the Funcom-forums guide + community consensus; AOWiki's Breed and Skills page did not state one explicit breed.

## Sources
- Funcom forums — [MP Meta-physicist guide](https://forums.funcom.com/t/mp-meta-physicist-guide/74897)
- AOWiki — [Meta-Physicist](http://wiki.aodb.us/wiki/Meta-Physicist), [Breed and Skills](http://wiki.aodb.us/wiki/Meta-Physicist:Breed_and_Skills), [Perks](http://wiki.aodb.us/wiki/Meta-Physicist:Perks), [Research Lines](http://wiki.aodb.us/wiki/Meta-Physicist:Research_Lines)
- Fandom — [Meta-Physicist Knowledge Base](https://anarchyonline.fandom.com/wiki/Meta-Physicist)
- Local enums — `E:/Funcom/AOBuddy10/AOSharp.Common/GameData/Profession.cs`, `PerkHash.cs`
