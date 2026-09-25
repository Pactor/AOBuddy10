# Soldier — Cluster Implant Build

Weapon-first implant layout for profession **1 (Soldier)**, Assault Rifle template. Companion to `soldier-implants.json`.

> Every placement below is **legal for that slot and grade** — each was validated against `reference/implants.json`, where each cluster has exactly one (slot, grade) home. The *choice* of which legal cluster to take comes from the Soldier cost factors in `reference/skillcaps.json` and the nano cast requirements in `soldier-nanos.json`. No per-slot Soldier implant sheet could be fetched from a reputable source (auno.org / aoitems.com block automation), so the layout is derived, not copied — see Unverified.

## The build

| Slot | Shiny | Bright | Faded | QL |
|---|---|---|---|---|
| Head/Brain | **Matter Creation** | **Ranged Init.** | Sense | 200 |
| Eye/Ocular | Aimed Shot | **Treatment** | **Assault Rifle** | 200 |
| Ear | Perception *(filler)* | Psychology *(filler)* | **Psychological Modifications** | 200 |
| Right Arm | **Assault Rifle** | Physic. Init *(filler)* | Radiation AC *(filler)* | 200 |
| Chest/Body | **Body Dev.** | **Matter Metamorphosis** | **Sensory Improvement** | 200 |
| Left Arm | Brawling *(filler)* | Strength | **Matter Metamorphosis** | 200 |
| Right Wrist | **Ranged Init.** | **Full Auto** | **Fling Shot** | 200 |
| Waist | Cold AC *(filler)* | **Body Dev.** | **Full Auto** | 200 |
| Left Wrist | Multi Ranged *(filler)* | Attack Speed *(filler)* | Rifle *(filler)* | any |
| Right Hand | Weapon Smithing *(filler)* | **Assault Rifle** | **Burst** | 200 |
| Legs/Thigh | **Dodge-Rng** | **Evade-ClsC** | **Body Dev.** | 200 |
| Left Hand | Fast Attack *(filler)* | Fire AC *(filler)* | **First Aid** | 200 |
| Feet | **Evade-ClsC** | **Dodge-Rng** | **Duck-Exp** | 200 |

## What that adds up to

| Skill | Where it comes from |
|---|---|
| **Assault Rifle** (weapon skill) | Right Arm shiny + Right Hand bright + Eye faded — **all three grades** |
| **Ranged Init.** | Right Wrist shiny + Head bright |
| **Full Auto** | Right Wrist bright + Waist faded |
| **Burst** | Right Hand faded |
| **Fling Shot** | Right Wrist faded |
| **Aimed Shot** | Eye shiny |
| **Body Dev.** | Chest shiny + Waist bright + Legs faded — **all three grades** |
| **Treatment** | Eye bright |
| **Matter Creation** | Head shiny |
| **Matter Metamorphosis** | Chest bright + Left Arm faded |
| **PM / SI** | Ear faded / Chest faded |
| **Evades** | Evade-ClsC (Feet shiny + Legs bright), Dodge-Rng (Legs shiny + Feet bright), Duck-Exp (Feet faded) |
| **Time & Space** | **none** — see deviations |
| Nano Pool / Max Nano Energy / Nano C. Init | **none, deliberately** |

## Why it looks nothing like the Meta-Physicist build

1. **The wrists and Right Arm are the best implants a Soldier owns.** The MP file writes them off ("no caster clusters available in this slot; lowest priority"). For a Soldier: Right Arm shiny = Assault Rifle, Right Hand bright = Assault Rifle, Right Wrist shiny = Ranged Init., bright = Full Auto, faded = Fling Shot, Right Hand faded = Burst.
2. **No nano-pool clusters at all.** The MP spends Head bright on Nano Pool and Waist bright on Max Nano Energy. The Soldier spends them on **Ranged Init.** and **Body Dev.** — its nanos are long-duration pre-fight buffs, and Nano C. Init is its most expensive skill (4.0 vs the MP's 1.0).
3. **Head shiny = Matter Creation, not Treatment.** Matter Creation is a cast requirement on **102 of 177** Soldier nanos (to 1,527) and is the cheaper of the two reflect schools (2.5 vs Time & Space 3.2). Treatment falls back to Eye bright.
4. **Time & Space gets no cluster.** Its only homes — Head shiny, Right Hand bright, Eye faded — are exactly the ones Matter Creation and the weapon skill need. Since Augmented Mirror Shield MK IV (223185) wants **Time & Space 1419**, a reflect-first Soldier should run the *variant* rather than pretend it is covered: **Head shiny = Time & Space, Eye faded = Matter Creation**, Right Hand bright stays Assault Rifle — paying for it with the Eye-faded weapon-skill cluster.
5. **Chest bright = Matter Metamorphosis**, not the MP's Biological Metamorphosis: MM gates the absorb shields (Heavy Assault Absorption Shield 75401 needs MM 760 + MC 679), Phalanx and the top team-damage nanos, and is on 36 Soldier nanos versus BM's 14.
6. **Body Dev. in all three grades.** Cost factor **1.1** for a Soldier versus 2.4 for an MP — the cheapest HP in the game for this profession.

## Other weapon templates

Change only the weapon slots:

- **Ranged Energy** — its homes are Head shiny, Eye bright and Left Hand faded, so it competes with Matter Creation and Treatment, not with the Right Arm (which then has no weapon cluster worth taking).
- **MG/SMG** — Right Arm shiny + Right Hand bright + Chest faded.
- **Shotgun** — Right Arm shiny + Right Hand bright + Waist faded.
- **Pistol** — Right Wrist shiny + Right Hand bright + Eye faded, and Left Wrist shiny (Multi Ranged) stops being filler.
- AO-Universe's guide puts **Full Auto** in Right Arm shiny at level 100–110 ("right arm for full auto") instead of the weapon skill — use that once gear and buffs already cap the weapon skill.

## Ladder

Treatment gates every implant. `reference/implants.json` gives the exact requirement as `trunc(4.723717064 × QL + 6.767295257)` = **951 Treatment at QL 200** (its approximate note says ~947), plus the governing ability at `QL × 2 + 4` = **404**. Treatment costs a Soldier 2.0, so most of that has to come from gear and buffs.

Ladder order: equip the low-priority slots first, stack Treatment and ability buffs (Trader wrangles, First Aid / Bio Met stims, ability buffs), then step the four weapon slots — Right Arm, Right Hand, Right Wrist, Eye — up one QL at a time, since those carry the wield requirement you are twinking for. The Soldier's own buffs ladder too: Art of War (+200 Assault Rifle) and Improved Total Focus (+125 to ten ranged weapon skills) let you equip a higher-requirement gun once the implants are in. Use Auno Implant Designer / TinkerPlants / AOLadderer to solve the exact sequence.

## Endgame

This is the leveling/twink layout. AO-Universe moves brain and eye to symbiants around level 80, puts QL 200 weapon-skill implants in at 100–110, and is in a mostly-symbiant setup by 130–150. The Soldier's symbiant line is **Artillery** (see `soldier-symbiants.json`).

## Unverified
- No fetched source gives a per-slot Soldier cluster sheet; the layout is derived from local cost factors, nano requirements and the cluster mapping.
- QL 200 is the usual top-tier regular-implant target, not a Soldier-specific sourced value.
- Which slots an endgame Soldier keeps as implants rather than symbiants is only qualitatively sourced (AO-Universe mentions an ear implant with +7% exp and a left-wrist implant with +10 add energy damage; neither item was identified in `itemnames.sql`, so neither is asserted).
- Cluster bonus magnitudes per grade beyond the ~60% / ~40% ratios are not in `reference/implants.json`, so no numeric skill gains are claimed.

## Sources
- Local — `reference/implants.json` (cluster mapping, equip formulas), `reference/skillcaps.json` (Soldier cost factors), `soldier-nanos.json` (cast requirements), `soldier-symbiants.json` (Artillery line), `items.ocp` via `tools/mp-weapon-extractor --prof 1` (special distribution per weapon skill)
- AOWiki — [Soldier:Breed and Skills](http://wiki.aodb.us/wiki/Soldier:Breed_and_Skills), [Implant](http://wiki.aodb.us/wiki/Implant)
- AO-Universe — [Tepamina's Soldier Guide 2/3](https://www.ao-universe.com/guides/classic-ao/profession-guides/tepaminas-soldier-guide-23) (implant/symbiant milestones)
