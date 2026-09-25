# Bureaucrat — Cluster Implant Build

Profession ID **8**. Crowd-control-first layout. **Script-validated** against `reference/implants.json`: **39 of 39** placements (13 slots × 3 grades) legal for their slot *and* grade, **0** illegal, **0** clusters duplicated within a grade. 33 distinct clusters used; **52 of the 85** clusters in the table get nothing.

> `shiny` = biggest bonus · `bright` ≈ 60% · `faded` ≈ 40%. Each cluster has **exactly one** (slot, grade) home in the whole game, so spending a home on one skill permanently denies it to every other skill whose last home is there. That constraint, not preference, is what shapes this build.

**No implant in AO carries a profession lock.** All 44,060 ItemClass-3 implant templates in `items.ocp` have zero `Profession(60)` requirements, so an automated "what can a Bureaucrat equip" sweep returns *every implant in the game* and tells you nothing. This file is derived from the cluster table + the Crat's own IP cost factors + its nanos' cast requirements, and asserts no restriction the client data does not contain.

## The school contest, counted

Across **all 343** Bureaucrat-castable nanos in `bureaucrat-nanos.json`:

| School | Nanos requiring it | Highest requirement | Crat IP cost |
|---|---|---|---|
| **Psychological Modification** | **234 / 343** | 2001 — Means Test Pet (258222) | 1.0 |
| **Time & Space** | **207 / 343** | 1641 — Puissant Void Inertia (224129) | 1.6 |
| **Sensory Improvement** | **153 / 343** | 2001 — Means Test Pet (258222) | 1.0 |
| Matter Creation | 100 / 343 | 1470 — CEO Guardian (273300) | 1.6 |
| Biological Metamorphosis | 83 / 343 | 2001 — Means Test Pet (258222) | 1.0 |
| Matter Metamorphosis | **1 / 343** | 800 — Fill Inbox (266303) | 1.6 |

Pairings that decide the build: **PM + SI together on 124 nanos** at near-identical values (Empowered Divided Ego 224143 = PM 2001 / SI 1997). **MC + T&S together on 79** — every robot pet, at *equal* values (Corporate Guardian 235386 = MC 1227 / T&S 1227). **T&S without MC on 128**; MC without T&S on only 21.

## The build

| Slot | Shiny | Bright | Faded | QL |
|---|---|---|---|---|
| **Head** | **Psychological Modifications** | Nano Pool | Sense | 200 |
| **Eye** | Tutoring *(cheapest filler, 1.0)* | **Sensory Improvement** | **Matter Creation** | 200 |
| **Ear** | Perception *(filler)* | **Psychology** | **Psychological Modifications** | 200 |
| **Right Arm** | Break & Entry | Chemical AC *(filler)* | Mechanical Engineering | any |
| **Chest** | **Nano Pool** | **Biological Metamorphosis** | **Nano C. Init** | 200 |
| **Left Arm** | Brawling *(filler)* | Strength | Matter Metamorphosis *(buys 1 nano)* | any |
| **Right Wrist** | **Pistol** | Burst | Fling Shot | 200 |
| **Waist** | Cold AC *(filler)* | Body Dev. | **Biological Metamorphosis** | 200 |
| **Left Wrist** | Multi Ranged | Attack Speed *(filler)* | Rifle *(filler)* | any |
| **Right Hand** | Trap Disarming | **Time & Space** | **Treatment** | 200 |
| **Legs** | Dodge-Rng | Evade-ClsC | Body Dev. | 200 |
| **Left Hand** | Fast Attack *(filler)* | Fire AC *(filler)* | **First Aid** | 200 |
| **Feet** | Evade-ClsC | Dodge-Rng | Duck-Exp | 200 |

## Coverage — X of Y, stated honestly

**33 of the 85 clusters** in the game's table are used. **52 get nothing.** The ones that matter:

| Skill | Grades obtained |
|---|---|
| **Psychological Modifications** | Head shiny + Ear faded — **two** (bright home Eye given to SI) |
| **Sensory Improvement** | Eye bright only |
| **Time & Space** | Right Hand bright only |
| Matter Creation | Eye faded only |
| **Biological Metamorphosis** | Chest bright + Waist faded — two |
| Matter Metamorphosis | Left Arm faded only — and it gates **one** nano |
| **Nano C. Init** | Chest faded only |
| **Nano Pool** | Chest shiny + Head bright — two |
| **Psychology** | Ear bright only |
| **Treatment** | Right Hand faded only |
| Body Dev. | Waist bright + Legs faded (shiny home spent on Nano Pool) |
| Pistol kit | Pistol (RW shiny) · Burst (RW bright) · Fling Shot (RW faded) · Multi Ranged (LW shiny) |
| Evades | Evade-ClsC (Feet shiny + Legs bright) · Dodge-Rng (Legs shiny + Feet bright) · Duck-Exp (Feet faded) |
| First Aid | Left Hand faded — the Crat's **only** self-heal |
| Abilities | Sense (Head faded) · Strength (Left Arm bright) — nothing else |
| **No cluster at all** | **Computer Literacy · Intelligence · Psychic · Max Nano Energy** · Max Health · Nano Programming · Pharma Tech · Stamina · Agility · Ranged Init. · Concealment · Martial Arts · Melee Init. + 39 more. **Nano Resist** gets nothing because no Nano Resist cluster exists anywhere in the game. |

## What loses a contested home, and why

**Head shiny → PM, so 9 skills lose their only shiny.** Computer Literacy, Intelligence, Psychic, Max Nano Energy, Nano Programming, Pharma Tech, Treatment, Nano C. Init and Psychology all have Head shiny as a home. PM wins on the count above (234/343) at cost factor 1.0.

**Eye bright → SI, not PM.** PM does *not* take all three grades, unlike BM in the Doctor build. PM and SI are joint requirements at near-equal values on 124 nanos — The Voice of God (231010) PM/SI/BM 1500 each, Dead Man Walking (275826) PM 1913 / SI 1913, Greater Corporate Insurance Policy (267605) PM 1901 / SI 1901 — so raising PM past SI unlocks nothing. This costs **Nano C. Init, Treatment, Computer Literacy, Intelligence, Nano Programming and Pharma Tech** their bright home.

**Matter Creation and Time & Space share all three homes** (Head shiny, Right Hand bright, Eye faded), so a Crat can never implant both at the same grade — and all **65 robot pets require them at equal values**. T&S takes the bright because it is on 128 nanos where MC is absent (every AoE, root, snare, root-breaker, speech, pet buff); MC takes the faded. *A pet-first Crat should swap them.*

**Right Hand faded → Treatment, so Computer Literacy gets nothing.** Treatment and Computer Literacy have **identical home sets** (Head shiny / Eye bright / Right Hand faded), so a Bureaucrat gets exactly one of the two. Treatment wins: it gates the QL of every implant and Control symbiant, and it costs a Crat **2.0** (twice a Doctor's 1.0), while Computer Literacy is **1.0** — the cheapest tier to simply buy with IP — and also arrives on armour (Chosen/Faithful Bureaucrat Pants 219024 / 218946 = Computer Literacy **+30** at QL300). Both AOWiki and AO-Universe tell a Crat to max Computer Literacy; this build says buy it with IP and gear, not with a cluster.

**Intelligence and Psychic get nothing**, and for this class that is the sharpest cost. Read from `items.ocp`, they are the *only* requirements on the Crat's own endgame armour: **Ofab Bureaucrat QL300 = Intelligence 1045 / Psychic 855**; **Chosen/Faithful Bureaucrat QL300 = Intelligence 1100 / Psychic 900**. Their homes are Head shiny (PM), Eye bright (SI), Chest bright (BM) and Ear faded (PM). It has to come from symbiants, the armour itself (Penultimate Ofab Headgear 264500 = Int +32; OFAB Protective Gear 267932 = Psychic +25; Chosen Cloak 219050 = Psychic +20) and the Genius perk line.

**Max Nano Energy gets nothing** — the opposite of the Doctor build. Homes: Head shiny (PM), Chest faded (Nano C. Init), Waist bright (Body Dev.). Defensible here because the Crat's own vests carry it in bulk: Penultimate Ofab Vest (264518) **+900**, Chosen Vest (219030) **+800**, plain Ofab Vest (264522) +750, Special Edition Ofab Headgear (267364) +500.

**Matter Metamorphosis keeps a cluster that buys one nano.** Fill Inbox (266303, MM 800 / BM 800) is the *only* MM nano in 343. Left Arm faded is MM's last home; taking Chemical AC or Physic. Init there instead costs a Crat nothing measurable.

## Where a Bureaucrat differs from the Doctor and Soldier templates

- **Head shiny = Psychological Modifications** — Soldier spends it on Matter Creation, Doctor on Biological Metamorphosis. Neither is close for a Crat.
- **PM takes only two grades**, deliberately, because SI is its co-requirement at the same value. The Doctor gives BM all three because BM's number is always the *higher* of its pair.
- **Chest shiny = Nano Pool, not Body Dev.** — the only class file so far to do this. Crat Body Dev. is cost **2.4** (Soldier 1.1, Enforcer 1.0, Doctor 2.0) and Nano Pool is **1.4**; AOWiki calls Nano Pool the "buffer for when your Calms go awry" and says Body Development "doesn't necessarily have to be maxed out".
- **Legs bright = Evade-ClsC, not Stamina.** The Doctor takes Stamina there because Ofab/Chosen Doctor QL300 needs Stamina 1099. The same read for a Crat says the opposite: **no Stamina requirement at all** on Ofab or Chosen/Faithful Bureaucrat. All three Crat evades also cost exactly **2.4** — unusual; Doctor's Evade-ClsC is 3.2, Soldier's is 2.0 — so the evade picks fall out of which home pairs are free.
- **Right Arm shiny = Break & Entry, not Fling Shot.** Every ranged special is cost **4.0** for a Bureaucrat (Doctor: Fling Shot 2.4 / Burst 3.0; Soldier: Fling Shot 1.0 / Full Auto 1.5). The specials that *are* kept (Burst, Fling Shot at Right Wrist) are kept precisely because IP cannot buy them.
- **Left Wrist shiny = Multi Ranged is a real pick**, not filler. The MP and Doctor files write this slot off; AO-Universe's Bureaucrat guide names it directly — "QL200 implants for Multiranged and Burst skills" — because dual pistols is a standard Crat setup.
- **Ear bright = Psychology as a real skill**, where the Doctor and Soldier files both call it "the cheapest filler in a useless list". For a Crat it is cost **1.0**, AOWiki says "Max this out if you plan on using Charms extensively", and the Bureaucrat is the **only** profession with a self-cast Psychology buff: Authority Figure (30057) **+50**, Improved Authority Figure (207284) **+52**, plus **+60** from the Chosen/Faithful Cloak.
- **Eye shiny = Tutoring** (cost 1.0) where the Doctor takes Electrical Engineering — EE is 1.6 for a Doctor but **2.4** for a Crat. Same logic at Right Hand shiny: Trap Disarming (2.4) over Weapon Smithing (2.5), which the Doctor takes at 1.5.
- **First Aid is not a convenience cluster here — it is the whole self-heal.** The Bureaucrat has **no heal nano** in its 343; the three "Pet Heals / Repair" entries (Droid Repair 30072, Droid Overhaul 30071, Thorough Overhaul 116798) repair the robot.

## Ladder

Treatment required per implant QL = `trunc(4.723717064 × QL + 6.767295257)` (≈ **947 at QL200**). Governing ability = `QL×2+4` (≈ 404 at QL200). Treatment costs a Crat **2.0**, so most of that number must come from gear and buffs.

A Bureaucrat brings **almost nothing to its own ladder**: there is no self Treatment buff and no self ability buff anywhere in its 343 nanos (verified). Treatment comes from a Doctor (Superior First Aid 28675, +80), stims and ladder items; abilities from a Trader wrangle, an Enforcer Essence, a Composite Attribute Boost and an MP's Composite Mochams. Equip the throwaway slots first (Left Wrist, Left Arm, Right Arm, Ear shiny), then swap the core five — Head, Eye, Chest, Waist, Right Hand — one QL step at a time.

Then plan to throw most of it away: AO-Universe moves a Crat onto symbiants at level **58** ("especially for your Eye, Head and Right hand") and names **Eye, Head, Ear, Chest and Waist** as the slots that matter most. At endgame all 13 become Xan **Control**-unit symbiants (see `bureaucrat-symbiants.json`), which carry Nano C. Init **+275**.

## Variants

- **Pet-first:** Right Hand bright = Matter Creation, Eye faded = Time & Space. Do this if the robot, not the calm, is what caps out.
- **No-gun Crat:** Right Wrist's three grades (Pistol / Burst / Fling Shot) and Left Wrist shiny are all droppable; nothing else in the build depends on them.
- **Never-cast-Fill-Inbox:** Left Arm faded = Chemical AC or Physic. Init instead of Matter Metamorphosis.

## Flagged / unverified

- The ~QL200 target is the conventional top-tier regular-implant figure, not read from item data. **No Bureaucrat-specific per-slot implant sheet exists on a reachable source**: AO-Universe gives only level milestones and two named skills; AOWiki gives per-skill advice but no shiny/bright/faded layout; auno.org / aoitems.com / anarchyonline.fandom.com block automation, and nothing was relayed from them. The layout is **derived**, not copied.
- Cluster bonus magnitudes per grade are not in `reference/implants.json` beyond the ≈60% / ≈40% ratios, so **no numeric skill gain is claimed** anywhere.
- `reference/implants.json` flags three cluster→stat mappings this build touches: **Attack Speed** (taken as least-useless filler, no bonus claimed), **Nano AC** (deliberately **avoided** — no base stat exists in `Stat.cs`; Rifle taken instead) and **Time & Space** (`sourceName` = `MaterialLocation`, load-bearing here).
- Psychology is described as the charm skill on AOWiki's authority. **No Psychology requirement appears in the 37 charm nanos' own `castReqs`** — they check PM / SI / BM. If the charm role is a server-side check rather than a cast gate, the cluster is justified by the buff line and Cloak bonus alone.
- The Waist-shiny elemental AC (Cold AC) is content-dependent and an arbitrary default.
- Laddering buffs from other professions (Superior First Aid, wrangle, Essence, Composite Mochams, Genius perks) are general AO knowledge, not re-verified in this pass. The claim that the **Crat has no self Treatment/ability buff is verified** from its own nano data.

## Sources

Local: `reference/implants.json` (legality + formulas) · `reference/skillcaps.json` (Bureaucrat = `_professionOrder` index **2**) · `bureaucrat-nanos.json` (all 343 nanos, every requirement quoted) · `items.ocp` via `tools/eng-gear-extractor --prof 8 --match "Bureaucrat"` (120 wearable named matches; Ofab/Chosen/Faithful requirements and bonuses) · `bureaucrat-symbiants.json` (Control unit) · `CLASS-PROFILE-PLAYBOOK.md` §9b (no implant profession lock).

Web: [wiki.aodb.us — Bureaucrat:Breed_and_Skills](http://wiki.aodb.us/wiki/Bureaucrat:Breed_and_Skills) · [wiki.aodb.us — Implant](http://wiki.aodb.us/wiki/Implant) · [AO-Universe — Bureaucrat Guide v1.2](https://www.ao-universe.com/guides/classic-ao/profession-guides/bureaucrat-guide---version-12).

All 33 item/nano ids cited in `bureaucrat-implants.json` were checked against `itemnames.sql`: **33 resolved, 0 not-in-db, 0 mismatches.**
