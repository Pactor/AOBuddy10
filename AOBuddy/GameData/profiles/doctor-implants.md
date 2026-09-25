# Doctor — Cluster Implant Build

Profession ID **10**. Heal-first layout. **Script-validated** against `reference/implants.json`: 39 placements (13 slots × 3 grades), every one legal for its slot *and* grade, no cluster duplicated within a grade.

> `shiny` = biggest bonus · `bright` ≈ 60% · `faded` ≈ 40%. Each cluster has **exactly one** (slot, grade) home in the whole game, so spending a home on one skill permanently denies it to another. That constraint, not preference, is what shapes this build.

## The build

| Slot | Shiny | Bright | Faded | QL |
|---|---|---|---|---|
| **Head** | **Biological Metamorphosis** | Nano Pool | Sense | 200 |
| **Eye** | Electrical Engineering *(filler)* | **Treatment** | **Matter Creation** | 200 |
| **Ear** | Perception *(filler)* | Psychology | **Psychological Modifications** | 200 |
| **Right Arm** | Fling Shot | Physic. Init *(filler)* | Mechanical Engineering | any |
| **Chest** | **Body Dev.** | **Biological Metamorphosis** | **Nano C. Init** | 200 |
| **Left Arm** | Brawling *(filler)* | Strength | **Matter Metamorphosis** | 200 |
| **Right Wrist** | Pistol | Burst | Fling Shot | 200 |
| **Waist** | Cold AC *(filler)* | Max Nano Energy | **Biological Metamorphosis** | 200 |
| **Left Wrist** | Multi Ranged *(filler)* | Attack Speed *(filler)* | Rifle *(filler)* | any |
| **Right Hand** | Weapon Smithing | **Time & Space** | **Treatment** | 200 |
| **Legs** | Dodge-Rng | **Stamina** | Body Dev. | 200 |
| **Left Hand** | Fast Attack *(filler)* | Fire AC *(filler)* | First Aid | 200 |
| **Feet** | Evade-ClsC | Dodge-Rng | Duck-Exp | 200 |

## Coverage

| Skill | Grades obtained |
|---|---|
| **Biological Metamorphosis** | Head shiny + Chest bright + Waist faded — **all three** |
| Matter Metamorphosis | Left Arm faded **only** (see below) |
| Treatment | Eye bright + Right Hand faded |
| Nano C. Init | Chest faded only |
| Nano Pool | Head bright only |
| Max Nano Energy | Waist bright only |
| Time & Space | Right Hand bright only |
| Matter Creation | Eye faded only |
| Psychological Modifications | Ear faded only |
| First Aid | Left Hand faded only |
| Body Dev. | Chest shiny + Legs faded |
| Evades | Evade-ClsC (Feet shiny) · Dodge-Rng (Legs shiny + Feet bright) · Duck-Exp (Feet faded) |
| Pistol kit | Pistol (RW shiny) · Fling Shot (RA shiny + RW faded) · Burst (RW bright) |
| **No cluster at all** | Sensory Improvement · Computer Literacy · Intelligence · Psychic · Pharma Tech · Nano Programming · Max Health · Nano Resist (no cluster exists) |

## Why it looks like this

**BM takes all three of its homes.** Biological Metamorphosis is the primary cast requirement on every one of the Doctor's 138 heal/HP-buff nanos, from Weak Team Heal (BM 29) to Superior Team Health Plan (BM 2153). Its three homes are Head shiny, Chest bright and Waist faded — all three go to BM. Nothing else a Doctor can reach returns more.

**Which is why MM only gets one cluster, unavoidably.** Matter Metamorphosis's three homes are Head shiny, Chest bright and Left Arm faded — it shares *two of three* with BM. Since every heal requires both and BM's number is always the higher (Superior Omni-Med Enhancement: BM 852 vs MM 769), BM wins both contested homes and MM takes the leftover. Make it up with symbiants, the Ofab/Chosen helmet (+24 MM at QL300) and an MP's Mocham's Gift: MatMet.

**Right Hand bright = Time & Space** — a Doctor-only call. Read from the nano data: the **16 team HP buffs are the only Doctor line carrying a SpaceTime requirement** (95709 = BM 852 / MM 769 / SpaceTime 769). Without this one cluster a Doctor has no Time & Space from implants at all.

**Legs bright = Stamina, not Evade-ClsC.** Read from items.ocp: Ofab Doctor QL300 and Chosen/Faithful Doctor QL300 both require **Stamina 1099**. Stamina is what lets a Doctor wear its own endgame armor. Evade-ClsC is cost 3.2 and unbuffable by anything in the kit — it keeps its Feet shiny and that's it.

**Sensory Improvement and Computer Literacy get nothing.** SI's homes are Head shiny / Eye bright / Chest faded — all spent on BM, Treatment and Nano C. Init. Computer Literacy's are Head shiny / Eye bright / Right Hand faded — all spent on BM and Treatment. CompLit has to come from armor (Ofab Doctor Pants +24 at QL300) and SI from symbiants. This is a genuine cost of the build and it is stated, not hidden.

## Ladder

Treatment required per implant QL = `trunc(4.723717064 × QL + 6.767295257)` (~QL×4.7+11 → **~947 at QL200**). Governing ability = `QL×2+4` (~404 at QL200).

A Doctor has an unusual advantage: **it buffs its own Treatment.** Superior First Aid (28675) is +80 Treatment and +80 First Aid; Specialist Treatment (28674) +35/+20; Enhanced First Aid (28657) +10/+15. The Nano Surgeon perk line adds +60 Treatment / +45 First Aid at max and Assault Force Medic another +40/+25. Stack a Trader wrangle, an MP's Composite Mochams (+140 all nano skills), an Enforcer Essence and a Composite Attribute Boost on top. Equip the throwaway slots (Left Wrist, Left Hand, Ear, Right Arm) first, then swap the core five (Head, Eye, Chest, Waist, Right Hand) up one QL step at a time.

At endgame most slots are replaced by **Support-unit symbiants** — see `doctor-symbiants.json`.

## Variants

- **Sensory Improvement variant:** Chest faded = SI instead of Nano C. Init. You lose Nano C. Init entirely (its other two homes are already spent).
- **Pistol-first variant:** Right Hand bright → Pistol and Eye faded → Pistol gives Pistol all three grades, at the cost of the Doctor's only Time & Space *and* only Matter Creation clusters. PvP/twink only.
- **Martial Arts variant:** Right Hand shiny = Martial Arts (cost 2.0) and Left Hand faded = Martial Arts (displacing First Aid). Right Arm shiny reverts to Strength.

## Where a Doctor differs from the MP and Soldier templates

- **Head shiny = BM**, where both the MP and Soldier builds spend it on Matter Creation.
- **Legs bright = Stamina**, where both of them take Evade-ClsC.
- **Right Hand bright = Time & Space**, which neither of them takes anywhere.
- **Waist bright = Max Nano Energy**, the opposite of the Soldier (which puts Body Dev. in all three grades) — a Soldier's nanos are pre-fight buffs, a Doctor's are the whole fight.
- **The weapon slots are real but cheap** — Pistol (1.6) and Fling Shot (2.4) are genuine picks, unlike the MP's "no caster clusters, lowest priority", but they are not the point of the build like the Soldier's Assault Rifle slots are.
- Unlike the MP file, **every grade names a real cluster** rather than `(filler)`, so the whole build validates by script.

## Flagged / unverified

- The ~QL200 target is the conventional figure, not read from item data. No published Doctor implant sheet was located (auno.org and aoitems.com are blocked to automation).
- `reference/implants.json` flags three cluster→stat mappings used here: **Attack Speed**, **Nano AC** (avoided — no base stat exists in `Stat.cs`, Rifle taken instead) and **Time & Space** (sourceName `MaterialLocation`). The Time & Space mapping is load-bearing.
- The Waist-shiny elemental AC choice is content-dependent; Cold AC is an arbitrary default.
- "Alien/Combined armor assembly is Psychology-gated" (the reason Ear bright takes Psychology over the cheaper Tutoring) is general AO knowledge, not a page fetched in this pass.
