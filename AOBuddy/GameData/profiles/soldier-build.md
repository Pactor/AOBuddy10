# Soldier — Character Build Reference

Profession ID **1** (`Profession.Soldier = 1` in the local enum). The pure weapon profession: ranged damage plus reflect/absorb shields.

> Sourcing: every skill cost factor is read from `reference/skillcaps.json` (Soldier = index 12 of `_professionOrder`), every nano id/name/requirement from `soldier-nanos.json` (177 castable nanos out of `nanos.ocp`), every weapon count/stat from `items.ocp` read with `tools/mp-weapon-extractor --prof 1`, and every id verified against `itemnames.sql`. Perk and research organisation comes from wiki.aodb.us and ao-universe.com, cross-checked against `AOSharp.Common/GameData/PerkHash.cs`. Anything that could not be verified is in the last section.

## Role & Playstyle

The Soldier trains **one ranged weapon line to the cap**, feeds it **Burst / Full Auto / Fling Shot / Aimed Shot**, and tanks with **layered reflect and absorb shields** rather than with armour and taunts alone. There is no pet, no heal worth the name (two drain-heal nanos) and no crowd control.

The skill table says the same thing. Soldier cost factors from `skillcaps.json`:

| Cost | Skills |
|---|---|
| **1.0** | Assault Rifle, Heavy Weapons, Pistol, Ranged Energy, **Ranged Init.**, **Fling Shot** (+ Vehicle Ground, Tutoring) |
| **1.1** | Body Dev. |
| **1.5** | **Burst**, **Full Auto**, MG/SMG, Shotgun, Dodge-Rng, Psychology, Weapon Smithing, Adventuring, Swimming |
| 1.6–1.8 | Grenade, Sharp Objects, Map Nav, Vehicle Air/Water · **Aimed Shot** 1.8, Duck-Exp 1.8 |
| 2.0 | Rifle, Multi Ranged, Evade-ClsC, Treatment, First Aid, Computer Literacy, Nano Pool, Run Speed, **PM**, **MM**, most melee |
| 2.2–2.5 | Nano Resist 2.2, Melee Energy 2.2, **BM/SI 2.4**, Bow 2.4, 1hB/2hB/2hE/Piercing 2.5, **Matter Creation 2.5** |
| 3.0–4.0 | Sneak Attack 3.0, **Time & Space 3.2**, Dimach 4.0, **Nano C. Init 4.0** |

**Strengths**
- **Best reflects in the game** — Total Mirror Shield Mk I–X (ids 70300–70309) and Augmented Mirror Shield MK I–V (223229 / 223181 / 223183 / 223185 / 273400). AO-Universe: total mirror shield gives *"75% reflect for a definite amount of time"*.
- **Team reflect auras** — 18 `ShadowlandReflectBase` nanos, Empowered Minor Deflection Shield (L0) → **Pre-Nullity Sphere** (233033, L220); each casts a self version and a team version.
- **Absorb / AC layer** — 14 `ArmorBuff` nanos to Heavy Assault Absorption Shield (75401, **+530 all ACs**) and team **Phalanx** (29245 / 162357, +350 all ACs).
- **Weapon buffs** — Art of War (275027, +200 AR), Improved Total Focus (270806, +125 to ten ranged weapon skills), Improved Soldier Clip Junkie (273402, +180 Full Auto), Riot Control (29251, +110 Burst), Improved Ranged Energy Weapon Mastery (275905, +120 RE).
- **Offense/utility buffs** — Full Automatic Targeting (270248, +74 AddAllOff), Offensive Steamroller (29240, +133 all four inits), Improved Precognition (275844, +110 all three evades).
- **Team damage** — seven `SoldierDamageBase` "Fight (Team)" nanos (223187–223199) and the Improved Augmentation Cloud line, topping out at Improved Semi-Sentient Augmentation Cloud (222838, +40 to all eight damage types).
- **HP** — Total Combat Survival (273398, **+1200 Max Health**, +35 Heal Delta) plus the Colossal Health perk line (+3180 Max Health).
- **Weapon pool** — of 10,730 weapon templates in `items.ocp`, **9,757 are usable by a Soldier**, including **1,545 Assault Rifles** (78 Soldier-only).

**Weaknesses**
- AOWiki on froob Soldiers: *"A profession that can be replaced by a single cast of a False Profession nano is a disadvantaged profession."* (The False Profession nanos are in the data: 32038 / 117227 / 117216.)
- **No heal, no CC** — the entire heal kit is two `DrainHeal` nanos (Don't Fear the Reaper 29241, Adrenaline Rush 301897) plus the Special Forces perk's Field Bandage.
- The defence is gated behind the **two most expensive nano schools**: Augmented Mirror Shield MK IV needs **Time & Space 1419 + Matter Creation 1454**; MK V needs 1490 / 1527 — at cost factors 3.2 and 2.5.
- **Single-target taunts only** (Insult line 229090–229104, Victim/Me lines 223201–223221); add management is weaker than an Enforcer's.
- Reflects are a **timed burst defence** — when TMS drops you are a plain armour-and-HP target.
- **Nano C. Init 4.0** — the Soldier's most expensive skill, so the kit is deliberately built from long-duration buffs.

## Breed — recommended: **Solitus**

Both AO-Universe Soldier guides pick Solitus. Tepamina: Solitus is *"the preferred choice of many soldiers because of nice health combined with nice intelligence, which especially makes it much easier to equip endgame symbiants compared to the Atrox."* Alezander: Solitus *"can do anything an Atrox can do, just very slightly less effectively."* `reference/breeds.json` agrees — no trained cap below 472, and 480 on Agility/Stamina/Int/Sense/Psychic, which is exactly the spread Assault Rifle (10% Str / 30% Agi / 40% Sta / 20% Int), Ranged Init. (10/10/60/20) and Aimed Shot (100% Sense) want.

| Breed | Verdict |
|---|---|
| **Solitus** | **Recommended.** Balanced caps, easiest symbiant/implant requirements, least buff-dependent. |
| Atrox | "Pretty equal" to Solitus per Tepamina. Most HP (4 HP/Body Dev point), cheapest Str/Sta, Mongo Rage for PvP. Lowest Int/Psy → harder nanos and symbiants. |
| Opifex | *"Master of evades and speed… higher agility and sense (which are not the usual stats for a soldier)."* Agi 544 / Sense 512, but Str 464 / Psy 448 and less HP. Evade/PvP pick. |
| Nanomage | *"Not the best choice for soldiers"* — casts the same nanos earlier but has the least HP, and the Soldier's value is weapon damage. |

## Perks

### Shadowlands
- **Special Forces** (10) — +200 Full Auto, +180 Burst, +180 Fling Shot, +150 Dodge Ranged, +50 Evade Close, +50 Duck Explosions; Field Bandage, Tracer, Contained Burst, Violence, Guardian. AOWiki: *"A must have… one of the best perklines Soldiers have."* **(high)**
- **Heavy Ranged** (7+) — +200 Assault Rifle, +200 Heavy Weapons; Laser Paint Target, Weapon Bash, Triangulate Target, Napalm Spray. *"The staple for Assault Rifle soldiers."* **(high)**
- **Power Up** (10) — +200 Ranged Energy, +50 all damage types; Energize, Power Blast/Volley/Shock/Combo. **(high)**
- **Colossal Health** (10) — Max Health +3180; Blessing of Life, Lifeblood, Draw Blood. **(high)**
- **SMG Mastery** (10) — +200 MG/SMG, +20 Burst; Reinforce Slugs, Jarring Burst, Solid Slug, Neutronium Slug. *(MG/SMG builds only)*
- **Shotgun Mastery** (6) — +75 Shotgun, +21 all damage types; Leg Shot. *(Shotgun/SMG builds)*
- **Mountaineer** (5) — +35 Str, +35 Sta, +215 Max Health, +90 Adventuring. *(filler)*
- **Infantry** (7) — +191 Max Health, +63 Rifle, +10 Fling Shot. AOWiki: *"Not really useable by Soldiers. This is more of an Agent perkline."* *(skip)*

### Alien / AI
- **Power in Numbers** — for **Suppressive Horde**; AO-Universe: at least 1 point, *"though some keep this line maxed for its nice mods like max health and full auto."*
- **Champion of Heavy Artillery** — grants **Fuzz**; *"recommended if you're using AR or RE."*
- **Champion of Light Artillery** — grants **Collapser**; Alezander's SMG+Shotgun layout takes 10.
- **Champion of Nano Combat** — grants **Nano Feast** (which takes a damage bonus from Fuzz).
- **Breed perks** (e.g. Atrox **Mongo Rage**) — the alternative to 10× Champion of Nano Combat.

### LE / Research proc perks (all 12 verified in `PerkHash.cs`, lines from AOWiki)

| Proc perk | Research line | Enum |
|---|---|---|
| Target Acquired | Sweep and Clear | `LEProcSoldierTargetAcquired` |
| On The Double | Sweep and Clear | `LEProcSoldierOnTheDouble` |
| Graze Jugular Vein | Strategic Planning | `LEProcSoldierGrazeJugularVein` |
| Furious Ammunition | Strategic Planning | `LEProcSoldierFuriousAmmunition` |
| Shoot Artery | Combat Sense | `LEProcSoldierShootArtery` |
| Deep Six Initiative | Combat Sense | `LEProcSoldierDeepSixInitiative` |
| Emergency Bandages | Marksmanship | `LEProcSoldierEmergencyBandages` |
| Concussive Shot | Forward Observer | `LEProcSoldierConcussiveShot` |
| Gear Assault Absorption | Forward Observer | `LEProcSoldierGearAssaultAbsorption` |
| Fuse Body Armor | Forward Observer | `LEProcSoldierFuseBodyArmor` |
| Successful Targeting | Classified Ops | `LEProcSoldierSuccessfulTargeting` |
| Reconditioned | Force Recon | `LEProcSoldierReconditioned` |

## Research — seven personal LE lines (AOWiki)

- **Marksmanship** — Assault Rifle, Full Auto, Burst, MG/SMG, Ranged Init.; → Emergency Bandages. AO-Universe: *"a very good line to research at any time of a soldier's career."*
- **Sweep and Clear** — Body Development, Max Health, Stamina, Agility; → Target Acquired, On The Double. The survivability line.
- **Strategic Planning** — Max NCU, Comp Lit, **Time & Space**, **PM**, **Matter Creation**, Add All Off; → Graze Jugular Vein, Furious Ammunition. One of the two reflect-casting lines.
- **Combat Sense** — Strength, Stamina, **Time & Space**, **Matter Creation**, Agility; → Shoot Artery, Deep Six Initiative. The other reflect-casting line.
- **Forward Observer** — Int, Sense, **MM**, Ranged Init., **Full Auto**, **Burst**; → Concussive Shot, Gear Assault Absorption, Fuse Body Armor (three procs).
- **Classified Ops** — PM, Str, Sense, Burst, MM, Add All Off; → Successful Targeting.
- **Force Recon** — Fling Shot, Pistol, Ranged Energy, Multi Ranged, Ranged Init.; → Reconditioned. The Pistol/RE/dual-wield line.

## Skill / IP priorities

**Max**
- **Assault Rifle** (1.0) — *"Our main attack skill. Most of our good weapons are Assault Rifles."* 1,545 usable ARs; 1,136 carry Burst, 1,041 Full Auto.
- **Ranged Init.** (1.0) — *"All Soldier guns rely on this initiative."*
- **Full Auto** (1.5) — *"Our ultimate PvM special."*
- **Burst** (1.5) — the special that fires most often.
- **Body Dev.** (1.1) — cheaper for a Soldier than for anyone but an Enforcer.

**High**
- **Fling Shot** (1.0) · **Aimed Shot** (1.8, Rifle/PvP builds — only 190 of 1,545 ARs carry it, versus 672 of 852 Rifles)
- **Matter Creation** (2.5) — required on **102 of 177** Soldier nanos, to 1,527
- **Time & Space** (3.2) — required on **64**, to 1,490 — the reflect line
- **Psychological Modification** (2.0) and **Sensory Improvement** (2.4) — every weapon/special buff needs both (1,342 each on the top ones)
- **Treatment** (2.0), **Computer Literacy** (2.0), **Dodge-Rng** (1.5), **Duck-Exp** (1.8), **Evade-ClsC** (2.0)

**Medium / as-needed** — Matter Metamorphosis (2.0, absorbs + Phalanx, to 1,649), Biological Metamorphosis (2.4, Total Combat Survival 1,243), Nano Pool, Run Speed, First Aid; alternate primaries Ranged Energy / Pistol / Heavy Weapons (1.0), MG/SMG & Shotgun (1.5).

**Avoid** — Rifle (2.0 for a Soldier), all melee (2.0–2.5; *"Soldiers are ranged, not melee."*), Sneak Attack 3.0, Dimach 4.0, **Nano C. Init 4.0**, and per Alezander's guide, Deflect and Nano Resist.

### Cap the specials to the weapon, not to a round number

AO-Universe's formulas, applied to real `items.ocp` values:

| Weapon (id) | Recharge | Cycle | Cap skill |
|---|---|---|---|
| Superior Rebuilt Perennium Blaster (260706) | 1.0 s | Burst 2500 | **900 Burst** |
| Superior Rebuilt Perennium Blaster (260706) | 1.0 s | FA 5000 | **1,975 Full Auto** |
| Ofab Shark Mk 6 (265103) | 1.4 s | FA 4500 | **2,250 Full Auto** |

- Burst cap = `[(recharge s × 20) + (BurstCycle/100) − 9] × 25`
- Full Auto cap = `[(recharge s × 40) + (FullAutoCycle/100) − 11] × 25`
- Fling Shot cap = `[(attack s × 16) − 7] × 100`
- Aimed Shot cap = `[(recharge s × 40) − 11] × 100 / 3`

The Ofab Shark figure (2,250) independently matches AOWiki's *"approximately 2300 Full Auto"* note for that gun — which is the cross-check that the unit interpretation is right.

## Unverified / caveats
- AI perk tier counts and per-rank values (AOWiki's Soldier:Perks lists no AI detail; its Perk_Chains page says the AI chains are incomplete).
- Mongo Rage as an Atrox breed perk: reported by AO-Universe, mechanics not verified here.
- Per-proc trigger chance/magnitude for the 12 LE procs (line assignment and enum names *are* verified).
- The `NanoFocusLevel` bit-64 gate on 7 top nanos is read from `nanos.ocp`; its in-game meaning is not verified.
- **No attack-rating / add-damage / initiative cap numbers are claimed** — no fetched source states them and they are not in the local data.
- The computed special caps assume `items.ocp` attack/recharge are hundredths of a second and that `BurstRecharge`/`FullAutoRecharge` are the formulas' "cycle" values.
- AOWiki's Soldier:Breed_and_Skills page gives no breed recommendation; the Solitus pick comes from the AO-Universe guides plus `breeds.json`.
- Drop/acquisition locations are out of scope here (that belongs in `soldier-weapons.json`).

## Sources
- AOWiki — [Soldier](http://wiki.aodb.us/wiki/Soldier), [Breed and Skills](http://wiki.aodb.us/wiki/Soldier:Breed_and_Skills), [Perks](http://wiki.aodb.us/wiki/Soldier:Perks), [Research Lines](http://wiki.aodb.us/wiki/Soldier:Research_Lines), [Weapons](http://wiki.aodb.us/wiki/Soldier:Weapons), [Perk Chains](http://wiki.aodb.us/wiki/Perk_Chains)
- AO-Universe — [Tepamina's Soldier Guide 1/3](https://www.ao-universe.com/guides/classic-ao/profession-guides/tepaminas-soldier-guide-13), [2/3](https://www.ao-universe.com/guides/classic-ao/profession-guides/tepaminas-soldier-guide-23), [3/3](https://www.ao-universe.com/guides/classic-ao/profession-guides/tepaminas-soldier-guide-33), [Alezander's Soldier Guide](https://www.ao-universe.com/guides/classic-ao/profession-guides/alezanders-soldier-guide)
- Local data — `reference/skillcaps.json`, `reference/breeds.json`, `soldier-nanos.json`, `items.ocp` via `tools/mp-weapon-extractor --prof 1`, `itemnames.sql`, `AOSharp.Common/GameData/PerkHash.cs`, `Profession.cs`
