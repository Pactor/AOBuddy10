# Bureaucrat — Character Build Reference

Profession ID **8** (`Profession.Bureaucrat = 8` in `AOSharp.Common/GameData/Profession.cs`). The game's crowd-control and pet class.

> **Sourcing.** Every perk line, rank, level requirement, item id and stat bonus is read out of `items.ocp` (client 18.8.50_EP1) by `tools/eng-gear-extractor --prof 8 --slug bureaucrat` piped through `tools/perk-extractor/build-perks.py` — **not** from web prose. Every IP cost factor is quoted from `reference/skillcaps.json` at `_professionOrder` index **2** (Bureaucrat). Every nano id and cast requirement comes from `bureaucrat-nanos.json` (343 nanos, `nanos.ocp`). Breed numbers come from `reference/breeds.json`. Web sourcing is limited to ao-universe.com and wiki.aodb.us, and only for *what a thing is for* — never for an id, a number or a level. Everything that could not be verified is listed at the end.

---

## Coverage — state it honestly

| What | Coverage |
|---|---|
| Perk lines available to profession 8 | **68 of 68** enumerated (474 rank templates) |
| Bureaucrat-only lines | **13 of 13**, **125 of 125 ranks**, every rank with id + level + exact bonus |
| Restricted-but-shared lines given full rank tables | 4 (Careful in Battle, Nano Doctorate, Pistol Mastery, Starfall) — **40 more ranks** |
| Total ranks with a full `rankTable` | **165** |
| Fully unlocked lines | **51**, each with rank count, level span and summed line bonuses |
| Shadowbreed perk lines | **37 of 37**, enumerated separately (see note below) |
| LE research lines | **7 of 7**, all 10 ranks each |
| LE research proc perks | **12 of 12** named from `PerkHash.cs`, **12 of 12** mapped to a research line |
| Nanos behind the skill targets | all **343** castable Bureaucrat nanos |
| **Not** covered | perk **action** effects, perk point costs, proc numeric effects — none of these exist in the client item templates and none are published on an allowed source |

**Why Shadowbreed lines are in their own block.** `build-perks.py` isolates perk templates by the client's own perk bit (`can` flag `0x10000000`). The 37 Shadowbreed templates (Adumbrated 215392, Black 215394, Balanced 230903, …) carry `can = 0xa00005` **without** that bit, so the extractor excludes them. `shade-build.json` counts 84 "general" lines because it included them; this file returns 68 from the extractor and lists the 37 separately, with their Side (faction) and level-205/210/215 gates shown, rather than silently losing them.

---

## Role & playstyle

A pistol-carrying pet-and-crowd-control caster. It summons a bot, charms a second pet, and takes the rest of the room out of the fight with calms, roots and snares while debuffing what is left. **Its power is gated by whether the crowd control lands, not by a damage or healing ladder.**

The shape of `bureaucrat-nanos.json` *is* the class: 65 summonable bots, 37 `CharmOther` charms + 2 short charms, 25 calms/mezzes, 17 roots, 10 snares, 27 area casts, 13 Demotivational Speeches, 12 Motivational Speeches, 17 nukes, 17 debuffs.

**Strengths**
- 114 of its 343 nanos need nothing but the three **cost-1.0** schools (Psy Mod / Sense Imp / Bio Met).
- 25 calms on strain **147**, from Distracted Gaze (30065, Psy Mod 58) to Empowered Divided Ego (224143, Psy Mod 2001 / Sense Imp 1997, level 219).
- 17 roots on strain **146** to Void Inertia (55993, Psy Mod 859); 10 snares on strain **145** driven by Time & Space (Shackles of Obedience 82463, T&S 741).
- Two pets at once — a bot plus a charmed mob. The bot ladder is Matter Creation and Time & Space in **lockstep**: Basic Worker-Droid 46397 (MC 7 / T&S 7) → Director-Grade Bodyguard 46391 (787 / 787) → Corporate Guardian 235386 (1227 / 1227) → CEO Guardian 273300 (1470 / 1470).
- 37 charms to The Voice of God (231010, Psy Mod 1500 / Sense Imp 1500 / Bio Met 1500, level 220).
- Team buffs nobody else gives: 12 Motivational Speeches on strain **233**, topping out at Improved Heroic Measures (270783, Psy Mod 1913 / Sense Imp 1913 / T&S 1471).
- **Froob-playable**: 292 of 343 nanos carry no `Expansion(389)` requirement at all.

**Weaknesses**
- All 13 Demotivational Speeches share **one** nano strain (238) — the line is a ladder, not a stack; only one can ever be on a target.
- Both pet schools are the **expensive** ones: Matter Creation and Time & Space cost **1.6** while the three CC schools cost **1.0**, and **229 of 343** nanos need one of the 1.6 schools.
- All three evades cost **2.4**, Body Dev. **2.4**, and there is no heal, reflect or absorb.
- Pistol is 1.6 but **Fling Shot, Multi Ranged and Ranged Init. all cost 4.0** — the maximum in the table.
- Every melee weapon skill costs 3.2–4.0 and Melee Init. 4.0. There is no melee Bureaucrat.
- Exactly **one** nano uses Matter Metamorphosis (Fill Inbox 266303, MM 800), so the common "a Crat needs no MM at all" is *almost* right — recorded as almost right.

---

## Breed — **Nanomage**

Derived from the numbers, then checked against the guides. Counting what actually gates the class: **Psy Mod gates 234 of 343 nanos, Time & Space 207, Sense Imp 153, Matter Creation 100, Bio Met 83.** `reference/skillcaps.json` gives those schools' ability dependencies as Psy Mod and Bio Met 80% Int / 20% Psy, and Sense Imp / Matter Creation / Time & Space 20% Int / 80% Psy — the whole class runs on **Intelligence and Psychic and nothing else**.

Nanomage is the only breed capping **both** at 512 (Solitus 480/480, Opifex 464/448, Atrox 400/400), gains **4** Max Nano per Nano Pool point (others 3 or 2) and has the highest nano-cost-reduction cap at **55%**. The Crat has no melee dimension to trade that away for. The price is 2 HP per Body Dev. point on a class already paying 2.4 for Body Dev. with no heal — which is exactly why Enhance Health (+1540 HP), Bureaucratic Shuffle (+200), Starfall (+160) and Market Awareness (+150) are on the perk list.

| Alternative | Why |
|---|---|
| **Solitus** | Int 480 / Psy 480 — ~32 points less headroom in each ability the class lives on, but 3 HP per Body Dev. ao-universe calls it the reliable all-rounder. The safe pick. |
| **Opifex** | Agi 544 / Sen 512 is wasted on nano schools, but Pistol is 60% Agi / 40% Sen at cost 1.6 and evades are 50% Agi. Psychic caps at **448**, the lowest of the four. A PvP/pistol pick. |
| **Atrox** | Int / Sen / Psy all hard-capped at **400** — 112 below Nanomage on every ability the class uses — and 2 Max Nano per Nano Pool point on a chain-caster. Buys 4 HP per Body Dev. Worst fit for the CC ladder. |

**Honest note:** ao-universe's Bureaucrat guide deliberately does *not* name a best breed — it says breed matters less than playstyle and that any breed can solo Inferno bosses and PvP with the right gear. Nanomage here is the IP/ability-cap argument, not a claim that the guides mandate it. `reference/professions.json` confirms AO has **no** breed restriction on any profession.

---

## Skill priorities (cost factors are exact, from `skillcaps.json` index 2)

| Skill | Cost | Target | Why |
|---|---|---|---|
| Psychological Modifications | **1.0** | max → 2001 | Cast requirement on **234 of 343** nanos — more than any other school. Momentary Daze (43368, PM 5) → Empowered Divided Ego (224143, PM 2001). |
| Sensory Improvement | **1.0** | max → 2001 | Requirement on 153 nanos, almost always paired 1:1 with Psy Mod. Malaise of Zeal (275824) needs PM 1913 **and** SI 1913. |
| Biological Metamorphosis | **1.0** | max → 2001 | The **charm** school. The Voice of God (231010) and The Voice of One (231008) each need BM 1500; The Choir Fantastic (275009) needs 1550. |
| Time & Space | 1.6 | max → 1641 | Requirement on 207 nanos: half of every bot summon, the whole snare line, and the speeches. Top requirement in the class is Puissant Void Inertia (224129, T&S 1641). |
| Matter Creation | 1.6 | max → 1470 | 100 nanos. Always **equals** the T&S number on a bot. Also the nuke school (Pink Slip 273307, MC 1470 / PM 1634) and the taunt school (Improved Rule of One 270250, MC 799). |
| Nano Pool | 1.4 | max | Chain-casting is the whole fight. AOWiki: keep it high for the spell buffer. |
| Nano C. Init. | **1.0** | max | AOWiki: *"an extra second or two shaved off that Calm"* is life vs reclaim. Loophole adds +51 and −4 nano cost. |
| Computer Literacy | **1.0** | max | NCU is the real limit — speeches **plus** pet buffs **plus** self-buffs. NCU Extensions gives Max NCU +140 / Comp Lit +110. |
| Psychology | **1.0** | max if you charm | **The Bureaucrat is the only profession paying 1.0 for Psychology.** AOWiki: max it if you charm extensively. Perks give +480 total. See caveat below. |
| Tutoring | **1.0** | to 1000 | AOWiki: QL200 Tutoring Devices, *"no bonus for having more than the required skill"*. |
| Nano Resist | 1.6 | high | No heal, no reflect, no absorb — this is the only defence the class owns against casters. |
| Body Dev. | 2.4 | **not** max | AOWiki is explicit: *a Bureaucrat won't be in the thick of things usually.* Buy HP from perks (+1540 / +200 / +160 / +150). |
| Pistol | 1.6 | to your gear | The only weapon skill worth IP. Buffed by Gunslinger 30077 (PM 79) → Skilled Gunslinger 263251 / Greater Gunslinger 263250 (PM 583 / SI 583, both Shadowlands). |
| Evade-ClsC / Dodge-Rng / Duck-Exp | 2.4 | **from perks** | Careful in Battle +300 AAD / +80 crit resist, Bureaucratic Shuffle +40 each, Market Awareness +50 AAD. More than 2.4-cost IP will ever buy. |
| Treatment | 2.0 | to implant QL only | A Doctor pays 1.0; a Crat pays 2.0 and **no Crat nano requires it**. |
| First Aid | 2.0 | low | Team Building alone gives +60 (261055 + 261062). |
| Nano Programming | 1.6 | situational | Cheap for this class, but gates nothing it casts. |
| Matter Metamorphosis | 1.6 | **skip** | Exactly **one** nano: Fill Inbox (266303, MM 800). |
| Fling Shot / Multi Ranged / Ranged Init. | **4.0** | never | Take from perks only: Hostile Negotiations (+90/+30/+30), Executive Decisions (+25/+50), Pistol Mastery (+20), Market Awareness (+15). |
| every melee skill, Rifle/AR/Bow/Shotgun/MG-SMG/RE/Grenade/HW | 2.8–4.0 | skip entirely | Pistol (1.6) is the only affordable weapon skill in the game for this class. |
| Vehicle Water | 2.4 | never | AOWiki: *"Do not raise this."* |

---

## Perks — from the client, every rank

**13 Bureaucrat-only lines, 125 ranks.** Seven are LE research (the 1/50/75/100/125/150/175/190/190/200 ladder), five are Shadowlands, and exactly one — **Loophole** — is an Alien Invasion line, identified from its 15/15/55/55/55/105/105/145/205/210 doubled-level ladder.

| Line | Type | Ranks | Levels | Full-line bonus (summed from items.ocp) |
|---|---|---|---|---|
| **Commanding Presence** | SL | 10 | 10–205 | **No stat modifiers at all.** Pure perk actions (ids 210931–210940). |
| **Directorship** | SL | 10 | 30–208 | Psychology +120, Bio Met +36, Sense Imp +36, Psy Mod +36 |
| **Persuader** | SL | 10 | 50–**219** | Psychology +200, Psy Mod +54, Sense Imp +54, Bio Met +14 |
| **Bureaucratic Shuffle** | SL | 10 | 50–206 | Max Health +200, Duck-Exp +40, Dodge-Rng +40, Evade-ClsC +40 |
| **Insurance Agent** | SL | **5** | 80–209 | Psychology +80, Psy Mod +18, Sense Imp +18, Bio Met +18 |
| **Loophole** | AI | 10 | 15–210 | Sense Imp +51, Psy Mod +51, Nano C. Init +51, Nano Cost −4 |
| **Hostile Negotiations** | LE | 10 | 1–200 | Pistol +145, Fling Shot +90, Ranged Init +30, Multi Ranged +30 |
| **Process Theory** | LE | 10 | 1–200 | Sense Imp +50, Psychic +40, Psy Mod +25, Bio Met +25, AAD +15, Sense +10, Nano Delta +10 |
| **Human Resources** | LE | 10 | 1–200 | Int +40, Psy Mod +25, Bio Met +25, Sense +20, Sense Imp +15, Comp Lit +10, Max NCU +4 |
| **Professional Development** | LE | 10 | 1–200 | T&S +55, Psy Mod +50, Bio Met +50, Matter Creation +40, XP +1% |
| **Team Building** | LE | 10 | 1–200 | First Aid +60, Sense Imp +50, Comp Lit +25, T&S +15, Psychology +15, Body Dev +15, Stamina +15 |
| **Market Awareness** | LE | 10 | 1–200 | Max Health +150, Matter Creation +60, AAD +50, Stamina +25, Psychology +15, Body Dev +15, Dodge-Rng +15, Ranged Init +15 |
| **Executive Decisions** | LE | 10 | 1–200 | Psychology +65, T&S +60, Multi Ranged +50, Fling Shot +25, Stamina +20, Pistol +15, Int +10 |

**Restricted but shared** (full rank tables also in the JSON):

| Line | Locked to | Full-line bonus |
|---|---|---|
| **Careful in Battle** | MA / Fixer / **Crat** / Shade | Add All Def +300, Critical Resistance +80 |
| **Nano Doctorate** | caster professions (the line carries two different lock sets across its ranks; a Crat is on both) | +100 to **all six** nano schools |
| **Starfall** | **Crat** / Doc / NT / MP | Max Nano +400, Max Health +160, +15 to all six schools |
| **Pistol Mastery** | Engi / Adv / **Crat** / Doc / MP | Pistol +200, +60 to all eight damage-type modifiers, Fling Shot +20 |

> **Commanding Presence is the trap in the data.** All ten ranks modify **not one stat** in `items.ocp`, so a data-only reading produces an empty effect string — and ao-universe calls this the line to *"max at all times"* for its team AR/AAD. Its value is entirely in perk **actions**, which the client item template does not encode. Recorded with an explicit `_dataNote` so the empty table is not misread.

> **`PerkLine.cs` is missing "Persuader".** The 272-entry enum does not contain it, although the client has all ten ranks (211957–211966) as ordinary perk templates carrying the perk bit and a Bureaucrat lock. The Shade pass found the same gap for Apotheosis, Improved Notum Propellant and Notum Fiber Armor; Persuader is a fourth.

---

## LE research and its procs

The proc-to-line mapping comes from `wiki.aodb.us/wiki/Bureaucrat:Research_Lines`. **Before trusting it, its per-rank stat rows were checked against `items.ocp` — 9 of 9 checked rows match exactly** (Executive Decisions r6 Fling Shot 25 and r10 T&S 60; Hostile Negotiations r8 Pistol 50 and r10 Pistol 60; Human Resources r1 Max NCU 4; Market Awareness r8 AAD 50 and r10 MC 60; Process Theory r9 Sense Imp 50; Professional Development r1 XP +1%).

| Research line | Level | Proc |
|---|---|---|
| Market Awareness | 1 | Papercut |
| Process Theory | 1 | Inflation Adjustment |
| Process Theory | 2 | Wait In That Queue |
| Executive Decisions | 3 | Deflation |
| Professional Development | 3 | Next Window Over |
| Professional Development | 4 | Lost Paperwork |
| Professional Development | 10 | Tariffs *(see conflict)* |
| Hostile Negotiations | 5 | Social Services |
| Human Resources | 6 | Forms in Triplicate |
| Human Resources | 8 | Wrong Window |
| Team Building | 7 | Tax Audit |
| Team Building | 10 | Please Hold |

**Unresolved name conflict — recorded, not smoothed.** `itemnames.sql` holds **both** `Tariffs` (263435) and `Mobility Embargo` (263432 / 263433). `PerkHash.cs` names twelve `LEProcBureaucrat*` perks including **Mobility Embargo** and **not** Tariffs; the wiki names **Tariffs** and never mentions Mobility Embargo. They are probably the same perk under two names — Tariffs 263435 has no paired Nano row while Mobility Embargo 263433 is a Nano with no third Item row, whereas the other eleven procs all have a full Item/Nano/Item triple — but that is an inference and is flagged, not asserted.

The **numeric effect, proc chance, trigger and stacking type** of all twelve procs are unknown: proc perks are action items with no `ToWear` template, so the perk extractor cannot reach them, and no allowed source publishes them.

---

## How the Bureaucrat differs from the Doctor and Shade templates

Full text in `_deviations` (12 entries). The load-bearing ones:

1. **Pet class whose pet schools are the expensive ones.** MC and T&S cost 1.6 while the three CC schools cost 1.0. 229 of 343 nanos need a 1.6 school; only 114 live entirely inside the 1.0 ones. The budgeting problem is *pets vs control* — neither template class has it.
2. **Gated by whether CC lands, not by a ladder rung.** A Doctor's 58-step heal ladder always makes the next rung better. A Crat's does not: ao-universe says the RK calm wipes a hate list but is *"murder to land"* on SL mobs, while the SL calm lands reliably and only sometimes pacifies. The best nano you can cast is frequently the wrong one.
3. **Psychology is cost 1.0 here and nowhere else** (Doctor 2.0, Shade 2.4). The Crat's seven 1.0 skills are Bio Met, Comp Lit, Nano C. Init., Psy Mod, Psychology, Sense Imp, Tutoring. **Treatment is 2.0 (Doctor 1.0) and Nano Pool is 1.4 (Doctor 1.0)** — so the Doctor file's "max Treatment every level" rule does *not* carry over.
4. **Breed is Nanomage, and for the opposite reason to the Doctor's Solitus.** The Doctor file argues its healing schools are *already* 1.0 so extra nano skill buys less. The Crat's two most-used schools are its *expensive* ones, so extra Int/Psychic buys **more**.
5. **Cheap weapon skill, most expensive specials in the table.** Pistol 1.6 but Fling Shot / Multi Ranged / Ranged Init. 4.0 — unlike the Shade, whose Sneak Attack (1.0) and Fast Attack (1.4) are cheap. A Crat can never buy its weapon specials with IP.
6. **One perk line with zero stats** (Commanding Presence) — the client-data-first method that made `shade-build.json` better than the MP's web prose produces an *empty* effect string here.
7. **No heal, no reflect, no absorb, evades at 2.4** — the same place the Doctor ends up by a different route: the Crat's answer is neither IP nor healing but **perks**.
8. **Froob-playable in a way neither template class is.** The Shade cannot exist on a free account at all; 292 of the Crat's 343 nanos have no expansion gate.

---

## Verified vs unknown

**Verified from local client data:** all 68 perk lines, all 474 rank templates, every rank's id / level / stat bonus; all 69 IP cost factors; all 343 nano cast requirements, nano strains and expansion gates; all four breeds' caps and per-point modifiers; the twelve `LEProcBureaucrat*` names and hashes; the thirteen proc item/nano ids.

**Cross-checked against the web and confirmed:** eleven skill cost factors (wiki quotes the same numbers this file reads from `skillcaps.json`), nine per-rank research bonuses, the Starfall profession lock (`wiki.aodb.us/wiki/Perk_Chains` says "Crat/doc/MP/NT"; the client lock is exactly Bureaucrat/Doctor/NanoTechnician/Metaphysicist).

**Corrected while writing this file:** both wiki.aodb.us and ao-universe state a Bureaucrat has *no* nanos requiring Matter Metamorphosis. `bureaucrat-nanos.json` has exactly one — Fill Inbox (266303, MM 800). The advice survives; the claim does not.

**Unknown / flagged (14 entries in `_unverified`).** The big ones: Psychology's actual role in charm is **not** provable from the local data (no Crat nano lists it as a cast requirement); the `Psychic < 551` term on the top charms could be a caster check or a target check — the extractor flattens the requirement `Target` field away; the `Pets op66 n` / `Specialization op22 n` / `NanoFocusLevel op22 64` operator bits are not decoded; Tariffs vs Mobility Embargo; every perk **action** effect; perk point costs. `wiki.aodb.us/wiki/Bureaucrat:Perks` is a 404 and `Perk_Chains` documents only 1 of the 11 Bureaucrat-relevant lines asked of it.

**Deliberate exclusions.** No weapon-QL, implant-QL or symbiant number is quoted here: no `bureaucrat-weapons.json` or `bureaucrat-implants.json` existed when this file was built, and estimating one would have been invention. The 51 unlocked general perk lines are given rank counts, level spans and summed bonuses but not per-rank tables — they are not Bureaucrat-specific and the same 51 lines belong to every profession.

---

## Verification run

```
python tools/verify-profile-ids.py AOBuddy/GameData/profiles/bureaucrat-build.json
ok   bureaucrat-build.json   pairs 277   exact 277   summary 0   mismatch 0   not-in-db 0
```

A second pass over every `Name (id)` reference written inline in the prose strings (44 unique references) also resolved 44/44 against `itemnames.sql`.
