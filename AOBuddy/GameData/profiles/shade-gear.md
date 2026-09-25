# Shade - Gear

What a Shade actually wears. Two things make this list different from every other class: (1) the implant slots are NOT here - they take Spirits, see shade-symbiants.json and shade-implants.json; and (2) the Shade has its own armour vocabulary - TATTOOS, BODY WRAPS, HAND/FOOT COVERS and a back-slot SUPPORT SYSTEM - because the class was originally built as one that "can't wear any armor, but have nano-tech enhanced tattoos grafted into their skin" (ao-universe). Patch 18.7 relaxed that: ao-universe states a Shade can now wear all items open to other professions, including token boards, helmets and back items. The client data agrees - 51,143 of the 56,349 wearable templates in items.ocp are usable by profession 15, of which 6,158 are ItemClass-2 gear (the rest are implants a Shade cannot use and Spirits). 370 templates in 139 named models are explicitly Shade-locked. The spine of a Shade gear plan is the Shade TIER line (Jobe Suit Vest -> First Tier -> Second Tier -> Chosen for Omni / Faithful for Clan, upgraded with Pandemonium glyphs), the Shade-locked OFAB line bought with Victory Points at the Tower Shop Shade booth in three grades (Ofab -> Improved -> Penultimate), and the tattoo lines (Ectoplasm neutral, Entophagous Clan, Sombreous Omni, plus the Eric Miller and Min-Li Jiu sets from Redeemed/Unredeemed Shadowlands mobs).

## Armour sets

| Set | Slots | QL in data | Level / source | Priority |
|---|---|---|---|---|
| First Tier Shade armour (the Shade tier line, step 1) | Body, Feet, Hands, LeftArm, Legs, RightArm | 100-160 | Upgraded from the Jobe Suit; the tier level is raised by right-clicking the piece. Cited: ao-universe Shade guide, wiki.aodb.us Shade:Armor. | high |
| Second Tier Shade armour (the Shade tier line, step 2) | Body, Feet, Hands, Head, LeftArm, LeftShoulder, Legs, RightArm, RightShoulder | 160-220 | Upgraded from First Tier. Cited: ao-universe Shade guide. | high |
| Chosen Shade armour (Omni-Tek endgame tier) | Back, Body, Feet, Hands, Head, LeftArm, LeftShoulder, Legs, RightArm, RightShoulder | 220-300 | Omni-Tek side of the tier line; upgraded with glyphs found inactive in Pandemonium. Cited: ao-universe Shade guide. | high |
| Faithful Shade armour (Clan endgame tier) | Back, Body, Feet, Hands, Head, LeftArm, LeftShoulder, Legs, RightArm, RightShoulder | 220-300 | Clan side of the tier line; upgraded with glyphs found inactive in Pandemonium. Cited: ao-universe Shade guide. | high |
| Ofab Shade armour (Alien Invasion / victory-point line) | Body, Feet, Hands, Head, LeftArm, Legs, RightArm | 1-300 | Bought with Victory Points at the Tower Shop Shade booth and upgraded with Kyr'Ozch Bio-Material Type 935. Cited: ao-universe Shade guide. | high |
| Improved Ofab Shade armour | Body, Feet, Hands, Head, LeftArm, Legs, RightArm | 1-300 | Upgrade grade of the Ofab Shade set. Cited: ao-universe Shade guide (Ofab upgraded with bio-material). | med |
| Penultimate Ofab Shade armour | Body, Feet, Hands, Head, LeftArm, Legs, RightArm | 1-300 | Top upgrade grade of the Ofab Shade set. Cited: ao-universe Shade guide. | high |
| Tattoos of Eric Miller (Shade-locked tattoo set) | Body, Feet, Hands, LeftArm, Legs, RightArm | 80-120 | ao-universe Shade guide: the Eric Miller and Min-Li Jiu tattoos drop from Redeemed/Unredeemed mobs in Shadowlands. | med |
| Tattoos of Min-Li Jiu (Shade-locked tattoo set) | Body, Feet, Hands, LeftArm, Legs, RightArm | 80-120 | ao-universe Shade guide: drops from Redeemed/Unredeemed mobs in Shadowlands. | med |
| Ectoplasm Tattoos (shared Shade / Meta-Physicist) | Body, Feet, Hands, LeftArm, Legs, RightArm | 1-300 | ao-universe Shade guide names Ectoplasm Torso Tattoos as the easy entry tattoo that buffs Piercing and Sneak Attack; they drop off Shadowlands monsters. | high |
| Entophagous Tattoos (Clan-only, shared Shade / Meta-Physicist) | Body, Feet, Hands, LeftArm, Legs, RightArm | 1-300 | ao-universe Shade guide: Entophagous is the Clan-only tattoo line. | med |
| Sombreous Tattoos (Omni-only, shared Shade / Meta-Physicist) | Body, Feet, Hands, LeftArm, Legs, RightArm | 1-300 | ao-universe Shade guide: Sombreous is the Omni-Tek-only tattoo line. | med |
| Infantry set (shared with MA / Adventurer / Enforcer / Keeper) | Back, Body, Feet, Hands, Head, LeftArm, Legs, RightArm | 25-300 | unknown - no fetched guide names this set for a Shade; it is included because items.ocp locks it to a group that INCLUDES Shade. | med |
| Outsider's Armor (shared with Martial Artist) | Body, Feet, Hands, Legs, RightArm | 1-300 | unknown - included from the client-data lock (MartialArtist/Shade). | low |

### First Tier Shade armour (the Shade tier line, step 1)

Step 1 of the Shade-only tier line (Jobe Suit Vest / Jobe armour -> First Tier -> Second Tier -> Chosen for Omni / Faithful for Clan). FIVE pieces only: there is NO "First Tier Shade Headgear" in items.ocp, and the ao-universe guide agrees - headgear and shoulderpads first appear at Second Tier. wiki.aodb.us calls the set the "First Tier Shade Jobe Suit" and "Body Wraps" is the body-slot piece inside it, which is why the two sources look like they disagree. WARNING from the ao-universe guide: you can level tier armour past your own skills and lock yourself out of wearing it.

| id | Piece | QL | Slots | Requirements | Key bonuses |
|---|---|---|---|---|---|
| 222926 | First Tier Shade Body Wraps | 160 | Body | Agility 585, Sense 480, Expansion 2 | MeleeAC +1200, EnergyAC +900, ProjectileAC +1200, FireAC +900, ColdAC +900, PoisonAC +1200, ChemicalAC +1200, RadiationAC +1200 |
| 222928 | First Tier Shade Arm Tattoos | 160 | RightArm, LeftArm | Agility 585, Sense 480, Expansion 2 | MeleeAC +300, EnergyAC +225, ProjectileAC +300, FireAC +225, ColdAC +225, PoisonAC +300, ChemicalAC +300, RadiationAC +300 |
| 222930 | First Tier Shade Hand Wraps | 160 | Hands | Agility 585, Sense 480, Expansion 2 | Piercing +20, MeleeAC +250, EnergyAC +190, ProjectileAC +250, FireAC +190, ColdAC +190, PoisonAC +250, ChemicalAC +250, RadiationAC +250 |
| 222922 | First Tier Shade Leg Covers | 160 | Legs | Agility 585, Sense 480, Expansion 2 | MaxHealth +100, MeleeAC +755, EnergyAC +570, ProjectileAC +755, FireAC +570, ColdAC +570, PoisonAC +755, ChemicalAC +755, RadiationAC +755 |
| 222924 | First Tier Shade Foot Covers | 160 | Feet | Agility 585, Sense 480, Expansion 2 | RunSpeed +20, MeleeAC +455, EnergyAC +345, ProjectileAC +455, FireAC +345, ColdAC +345, PoisonAC +455, ChemicalAC +455, RadiationAC +455 |

### Second Tier Shade armour (the Shade tier line, step 2)

Adds the shoulderpad and headgear slots over First Tier. NEUTRAL Shades stop here - the ao-universe guide states neutrals cannot wear the Chosen/Faithful step.

| id | Piece | QL | Slots | Requirements | Key bonuses |
|---|---|---|---|---|---|
| 222912 | Second Tier Shade Headgear | 220 | Head | Agility 805, Sense 660, Expansion 2 | Intelligence +10, MeleeAC +1125, EnergyAC +900, ProjectileAC +1125, FireAC +900, ColdAC +900, PoisonAC +1125, ChemicalAC +1125, RadiationAC +1125 |
| 222902 | Second Tier Shade Body Wraps | 220 | Body | Agility 805, Sense 660, Expansion 2 | MeleeAC +1500, EnergyAC +900, ProjectileAC +1500, FireAC +1200, ColdAC +1200, PoisonAC +1500, ChemicalAC +1500, RadiationAC +1500 |
| 222904 | Second Tier Shade Arm Tattoos | 220 | RightArm, LeftArm | Agility 805, Sense 660, Expansion 2 | MeleeInit +12, MeleeAC +375, EnergyAC +225, ProjectileAC +375, FireAC +300, ColdAC +300, PoisonAC +375, ChemicalAC +375, RadiationAC +375 |
| 222908 | Second Tier Shade Hand Wraps | 220 | Hands | Agility 805, Sense 660, Expansion 2 | Piercing +40, Dimach +30, MeleeAC +310, EnergyAC +190, ProjectileAC +310, FireAC +250, ColdAC +250, PoisonAC +310, ChemicalAC +310, RadiationAC +310 |
| 222898 | Second Tier Shade Leg Covers | 220 | Legs | Agility 805, Sense 660, Expansion 2 | EvadeClsC +20, MaxHealth +225, MeleeAC +940, EnergyAC +570, ProjectileAC +940, FireAC +755, ColdAC +755, PoisonAC +940, ChemicalAC +940, RadiationAC +940 |
| 222900 | Second Tier Shade Foot Covers | 220 | Feet | Agility 805, Sense 660, Expansion 2 | DuckExp +20, RunSpeed +30, MeleeAC +565, EnergyAC +345, ProjectileAC +565, FireAC +455, ColdAC +455, PoisonAC +565, ChemicalAC +565, RadiationAC +565 |
| 222914 | Second Tier Shade Shoulder Tattoos | 220 | RightShoulder, LeftShoulder | Agility 805, Sense 660, Expansion 2 | Parry +10, MeleeAC +175, EnergyAC +125, ProjectileAC +175, FireAC +125, ColdAC +125, PoisonAC +175, ChemicalAC +175, RadiationAC +175 |

### Chosen Shade armour (Omni-Tek endgame tier)

The OMNI-TEK endgame tier set. Same 8 pieces as Faithful; side-locked. Includes the Back-slot "Support System", which is the Shade tier line's back item.

| id | Piece | QL | Slots | Requirements | Key bonuses |
|---|---|---|---|---|---|
| 222678 | Chosen Shade Headgear | 300 | Head | Agility 1100, Sense 900, Expansion 2, Side 2 | Concealment +60, MaxHealth +200, Intelligence +20, SpaceTime +24, MeleeAC +1350, EnergyAC +900, ProjectileAC +1350, FireAC +1350, ColdAC +1125, PoisonAC +1350,  |
| 222668 | Chosen Shade Body Wraps | 300 | Body | Agility 1100, Sense 900, Expansion 2, Side 2 | Stamina +20, MaterialCreation +24, MaxNCU +24, MeleeAC +1800, EnergyAC +1200, ProjectileAC +1800, FireAC +1800, ColdAC +1500, PoisonAC +1800, ChemicalAC +1800,  |
| 222670 | Chosen Shade Arm Tattoos | 300 | RightArm, LeftArm | Agility 1100, Sense 900, Expansion 2, Side 2 | MultiMelee +20, MeleeInit +24, MaxHealth +250, MaterialMetamorphosis +12, MeleeAC +450, EnergyAC +300, ProjectileAC +450, FireAC +450, ColdAC +375, PoisonAC +45 |
| 222674 | Chosen Shade Hand Wraps | 300 | Hands | Agility 1100, Sense 900, Expansion 2, Side 2 | Piercing +60, Dimach +40, MaxHealth +250, Sense +30, PsychologicalModification +24, MeleeAC +375, EnergyAC +250, ProjectileAC +375, FireAC +375, ColdAC +310, Po |
| 222662 | Chosen Shade Leg Covers | 300 | Legs | Agility 1100, Sense 900, Expansion 2, Side 2 | EvadeClsC +40, MaxHealth +550, Agility +30, MaxNCU +16, MeleeAC +1125, EnergyAC +755, ProjectileAC +1125, FireAC +1125, ColdAC +940, PoisonAC +1125, ChemicalAC  |
| 222664 | Chosen Shade Foot Covers | 300 | Feet | Agility 1100, Sense 900, Expansion 2, Side 2 | DodgeRanged +40, DuckExp +40, RunSpeed +50, MaxHealth +250, SensoryImprovement +24, MeleeAC +675, EnergyAC +455, ProjectileAC +675, FireAC +675, ColdAC +565, Po |
| 222682 | Chosen Shade Shoulder Tattoos | 300 | RightShoulder, LeftShoulder | Agility 1100, Sense 900, Expansion 2, Side 2 | Parry +20, MaxHealth +400, Strength +10, BiologicalMetamorphosis +12, MeleeAC +225, EnergyAC +125, ProjectileAC +225, FireAC +225, ColdAC +175, PoisonAC +225, C |
| 222684 | Chosen Shade Support System | 300 | Back | Agility 1100, Sense 900, Expansion 2, Side 2 | SneakAttack +50, Psychic +20, MaxNanoEnergy +550, MeleeAC +1800, EnergyAC +900, ProjectileAC +1800, FireAC +1800, ColdAC +1500, PoisonAC +1800, ChemicalAC +1800 |

### Faithful Shade armour (Clan endgame tier)

The CLAN endgame tier set - the mirror of Chosen.

| id | Piece | QL | Slots | Requirements | Key bonuses |
|---|---|---|---|---|---|
| 216680 | Faithful Shade Headgear | 300 | Head | Agility 1100, Sense 900, Expansion 2, Side 1 | Concealment +60, MaxHealth +200, Intelligence +20, SpaceTime +24, MeleeAC +1350, EnergyAC +900, ProjectileAC +1350, FireAC +1125, ColdAC +1350, PoisonAC +1350,  |
| 216686 | Faithful Shade Body Wraps | 300 | Body | Agility 1100, Sense 900, Expansion 2, Side 1 | Stamina +20, MaterialCreation +24, MaxNCU +24, MeleeAC +1800, EnergyAC +1200, ProjectileAC +1800, FireAC +1500, ColdAC +1800, PoisonAC +1800, ChemicalAC +1800,  |
| 216684 | Faithful Shade Arm Tattoos | 300 | RightArm, LeftArm | Agility 1100, Sense 900, Expansion 2, Side 1 | MultiMelee +20, MeleeInit +24, MaxHealth +250, MaterialMetamorphosis +12, MeleeAC +450, EnergyAC +300, ProjectileAC +450, FireAC +375, ColdAC +450, PoisonAC +45 |
| 216682 | Faithful Shade Hand Wraps | 300 | Hands | Agility 1100, Sense 900, Expansion 2, Side 1 | Piercing +60, Dimach +40, MaxHealth +250, Sense +30, PsychologicalModification +24, MeleeAC +375, EnergyAC +250, ProjectileAC +375, FireAC +310, ColdAC +375, Po |
| 216690 | Faithful Shade Leg Covers | 300 | Legs | Agility 1100, Sense 900, Expansion 2, Side 1 | EvadeClsC +40, MaxHealth +550, Agility +30, MaxNCU +16, MeleeAC +1125, EnergyAC +755, ProjectileAC +1125, FireAC +940, ColdAC +1125, PoisonAC +1125, ChemicalAC  |
| 216688 | Faithful Shade Foot Covers | 300 | Feet | Agility 1100, Sense 900, Expansion 2, Side 1 | DodgeRanged +40, DuckExp +40, RunSpeed +50, MaxHealth +250, SensoryImprovement +24, MeleeAC +675, EnergyAC +455, ProjectileAC +675, FireAC +565, ColdAC +675, Po |
| 216678 | Faithful Shade Shoulder Tattoos | 300 | RightShoulder, LeftShoulder | Agility 1100, Sense 900, Expansion 2, Side 1 | Parry +20, MaxHealth +400, Strength +10, BiologicalMetamorphosis +12, MeleeAC +225, EnergyAC +125, ProjectileAC +225, FireAC +175, ColdAC +225, PoisonAC +225, C |
| 216676 | Faithful Shade Support System | 300 | Back | Agility 1100, Sense 900, Expansion 2, Side 1 | SneakAttack +50, Psychic +20, MaxNanoEnergy +550, MeleeAC +1800, EnergyAC +900, ProjectileAC +1800, FireAC +1500, ColdAC +1800, PoisonAC +1800, ChemicalAC +1800 |

### Ofab Shade armour (Alien Invasion / victory-point line)

The Shade-locked Ofab set. Three upgrade grades exist in the client data for the same six pieces: Ofab -> Improved Ofab -> Penultimate Ofab. The Back and Shoulder pieces are separate QL300-only items (OFAB Shade Protective Gear id 267934, OFAB Shade Shoulder Wear id 268005) and there is a Special Edition Ofab Shade Headgear (id 267377/267378).

| id | Piece | QL | Slots | Requirements | Key bonuses |
|---|---|---|---|---|---|
| 264540 | Ofab Shade Headgear | 300 | Head | Agility 1045, Sense 855, Expansion 32 | AddAllOff +20, Concealment +60, MaxHealth +265, Intelligence +20, MeleeAC +1418, EnergyAC +1418, ProjectileAC +1418, FireAC +1418, ColdAC +1418, PoisonAC +1418, |
| 264558 | Ofab Shade Body Armor | 300 | Body | Agility 1045, Sense 855, Expansion 32 | AddAllOff +15, Stamina +18, MaterialCreation +20, MaxNCU +20, ComputerLiteracy +50, MeleeAC +1890, EnergyAC +1890, ProjectileAC +1890, FireAC +1890, ColdAC +189 |
| 264552 | Ofab Shade Sleeves | 300 | RightArm, LeftArm | Agility 1045, Sense 855, Expansion 32 | MultiMelee +20, MeleeInit +25, MaxHealth +265, MaterialMetamorphosis +12, MeleeAC +473, EnergyAC +473, ProjectileAC +473, FireAC +473, ColdAC +473, PoisonAC +47 |
| 264546 | Ofab Shade Gloves | 300 | Hands | Agility 1045, Sense 855, Expansion 32 | Piercing +60, Dimach +30, MaxHealth +265, Sense +25, PsychologicalModification +25, MeleeAC +394, EnergyAC +394, ProjectileAC +394, FireAC +394, ColdAC +394, Po |
| 264570 | Ofab Shade Pants | 300 | Legs | Agility 1045, Sense 855, Expansion 32 | EvadeClsC +50, MaxHealth +400, Agility +25, MaxNCU +15, MeleeAC +1182, EnergyAC +1182, ProjectileAC +1182, FireAC +1182, ColdAC +1182, PoisonAC +1182, ChemicalA |
| 264564 | Ofab Shade Boots | 300 | Feet | Agility 1045, Sense 855, Expansion 32 | DodgeRanged +40, DuckExp +40, RunSpeed +40, MaxHealth +265, SensoryImprovement +25, MeleeAC +709, EnergyAC +709, ProjectileAC +709, FireAC +709, ColdAC +709, Po |

### Improved Ofab Shade armour

Middle grade of the three Ofab Shade grades in the client data.

| id | Piece | QL | Slots | Requirements | Key bonuses |
|---|---|---|---|---|---|
| 264538 | Improved Ofab Shade Headgear | 300 | Head | Agility 1045, Sense 855, Expansion 32 | AddAllOff +25, Concealment +65, MaxHealth +300, Intelligence +22, MeleeAC +1418, EnergyAC +1418, ProjectileAC +1418, FireAC +1418, ColdAC +1418, PoisonAC +1418, |
| 264556 | Improved Ofab Shade Body Armor | 300 | Body | Agility 1045, Sense 855, Expansion 32 | AddAllOff +20, Stamina +20, MaterialCreation +25, MaxNCU +25, ComputerLiteracy +55, MeleeAC +1890, EnergyAC +1890, ProjectileAC +1890, FireAC +473, ColdAC +1890 |
| 264550 | Improved Ofab Shade Sleeves | 300 | RightArm, LeftArm | Agility 1045, Sense 855, Expansion 32 | MultiMelee +25, MeleeInit +30, MaxHealth +300, MaterialMetamorphosis +14, MeleeAC +473, EnergyAC +473, ProjectileAC +473, FireAC +473, ColdAC +473, PoisonAC +47 |
| 264544 | Improved Ofab Shade Gloves | 300 | Hands | Agility 1045, Sense 855, Expansion 32 | Piercing +65, Dimach +35, MaxHealth +300, Sense +27, PsychologicalModification +28, MeleeAC +394, EnergyAC +394, ProjectileAC +394, FireAC +394, ColdAC +394, Po |
| 264568 | Improved Ofab Shade Pants | 300 | Legs | Agility 1045, Sense 855, Expansion 32 | EvadeClsC +55, MaxHealth +500, Agility +27, MaxNCU +20, MeleeAC +1182, EnergyAC +1182, ProjectileAC +1182, FireAC +1182, ColdAC +1182, PoisonAC +1182, ChemicalA |
| 264562 | Improved Ofab Shade Boots | 300 | Feet | Agility 1045, Sense 855, Expansion 32 | DodgeRanged +45, DuckExp +45, RunSpeed +45, MaxHealth +300, SensoryImprovement +30, MeleeAC +709, EnergyAC +709, ProjectileAC +709, FireAC +178, ColdAC +709, Po |

### Penultimate Ofab Shade armour

Top grade of the three Ofab Shade grades in the client data.

| id | Piece | QL | Slots | Requirements | Key bonuses |
|---|---|---|---|---|---|
| 264536 | Penultimate Ofab Shade Headgear | 300 | Head | Agility 1045, Sense 855, Expansion 32 | AddAllOff +35, Concealment +75, MaxHealth +350, Intelligence +25, MeleeAC +1418, EnergyAC +1418, ProjectileAC +1418, FireAC +1418, ColdAC +1418, PoisonAC +1418, |
| 264554 | Penultimate Ofab Shade Body Armor | 300 | Body | Agility 1045, Sense 855, Expansion 32 | AddAllOff +25, Stamina +25, MaterialCreation +30, MaxNCU +30, ComputerLiteracy +55, MeleeAC +1890, EnergyAC +1890, ProjectileAC +1890, FireAC +1890, ColdAC +189 |
| 264548 | Penultimate Ofab Shade Sleeves | 300 | RightArm, LeftArm | Agility 1045, Sense 855, Expansion 32 | MultiMelee +30, MeleeInit +26, MaxHealth +350, MaterialMetamorphosis +20, MeleeAC +473, EnergyAC +473, ProjectileAC +473, FireAC +473, ColdAC +473, PoisonAC +47 |
| 264542 | Penultimate Ofab Shade Gloves | 300 | Hands | Agility 1045, Sense 855, Expansion 32 | Piercing +75, Dimach +55, MaxHealth +350, Sense +32, PsychologicalModification +32, MeleeAC +394, EnergyAC +394, ProjectileAC +394, FireAC +394, ColdAC +394, Po |
| 264566 | Penultimate Ofab Shade Pants | 300 | Legs | Agility 1045, Sense 855, Expansion 32 | EvadeClsC +65, MaxHealth +650, Agility +35, MaxNCU +25, ComputerLiteracy +25, MeleeAC +1182, EnergyAC +1182, ProjectileAC +1182, FireAC +1182, ColdAC +1182, Poi |
| 264560 | Penultimate Ofab Shade Boots | 300 | Feet | Agility 1045, Sense 855, Expansion 32 | DodgeRanged +55, DuckExp +55, RunSpeed +55, MaxHealth +350, SensoryImprovement +35, MeleeAC +709, EnergyAC +709, ProjectileAC +709, FireAC +709, ColdAC +709, Po |

### Tattoos of Eric Miller (Shade-locked tattoo set)

One of the two named mid-level Shade tattoo sets.

| id | Piece | QL | Slots | Requirements | Key bonuses |
|---|---|---|---|---|---|
| 223990 | Arm Tattoo's of Eric Miller | 120 | RightArm, LeftArm | Agility 450, Sense 450, Side 2, Expansion 2 | MaxHealth +40, MaxNanoEnergy +25, MeleeAC +225, EnergyAC +125, ProjectileAC +125, FireAC +125, ColdAC +125, PoisonAC +125, ChemicalAC +125, RadiationAC +125 |
| 223992 | Hand Tattoo's of Eric Miller | 120 | Hands | Agility 450, Sense 450, Side 2, Expansion 2 | MaxHealth +35, MaxNanoEnergy +25, MeleeAC +225, EnergyAC +125, ProjectileAC +125, FireAC +125, ColdAC +125, PoisonAC +125, ChemicalAC +125, RadiationAC +125 |
| 223986 | Leg Tattoo's of Eric Miller | 120 | Legs | Agility 450, Sense 450, Side 2, Expansion 2 | MaxHealth +110, MaxNanoEnergy +75, MeleeAC +550, EnergyAC +350, ProjectileAC +350, FireAC +350, ColdAC +350, PoisonAC +350, ChemicalAC +350, RadiationAC +350 |
| 223984 | Foot Tattoo's of Eric Miller | 120 | Feet | Agility 450, Sense 450, Side 2, Expansion 2 | MaxHealth +75, MaxNanoEnergy +50, MeleeAC +300, EnergyAC +200, ProjectileAC +200, FireAC +200, ColdAC +200, PoisonAC +200, ChemicalAC +200, RadiationAC +200 |
| 223988 | Torso Tattoo's of Eric Miller | 120 | Body | Agility 450, Sense 450, Side 2, Expansion 2 | MaxHealth +150, MaxNanoEnergy +100, MeleeAC +900, EnergyAC +475, ProjectileAC +475, FireAC +475, ColdAC +475, PoisonAC +475, ChemicalAC +475, RadiationAC +475 |

### Tattoos of Min-Li Jiu (Shade-locked tattoo set)

The mirror of the Eric Miller set.

| id | Piece | QL | Slots | Requirements | Key bonuses |
|---|---|---|---|---|---|
| 223975 | Arm Tattoo's of Min-Li Jiu | 120 | RightArm, LeftArm | Agility 450, Sense 450, Side 1, Expansion 2 | MaxHealth +40, MaxNanoEnergy +25, MeleeAC +225, EnergyAC +125, ProjectileAC +125, FireAC +125, ColdAC +125, PoisonAC +125, ChemicalAC +125, RadiationAC +125 |
| 223980 | Hand Tattoo's of Min-Li Jiu | 120 | Hands | Agility 450, Sense 450, Side 1, Expansion 2 | MaxHealth +35, MaxNanoEnergy +25, MeleeAC +225, EnergyAC +125, ProjectileAC +125, FireAC +125, ColdAC +125, PoisonAC +125, ChemicalAC +125, RadiationAC +125 |
| 223956 | Leg Tattoo's of Min-Li Jiu | 120 | Legs | Agility 450, Sense 450, Side 1, Expansion 2 | MaxHealth +110, MaxNanoEnergy +75, MeleeAC +550, EnergyAC +350, ProjectileAC +350, FireAC +350, ColdAC +350, PoisonAC +350, ChemicalAC +350, RadiationAC +350 |
| 223954 | Foot Tattoo's of Min-Li Jiu | 120 | Feet | Agility 450, Sense 450, Side 1, Expansion 2 | MaxHealth +75, MaxNanoEnergy +50, MeleeAC +300, EnergyAC +200, ProjectileAC +200, FireAC +200, ColdAC +200, PoisonAC +200, ChemicalAC +200, RadiationAC +200 |
| 223972 | Torso Tattoo's of Min-Li Jiu | 120 | Body | Agility 450, Sense 450, Side 1, Expansion 2 | MaxHealth +150, MaxNanoEnergy +100, MeleeAC +900, EnergyAC +475, ProjectileAC +475, FireAC +475, ColdAC +475, PoisonAC +475, ChemicalAC +475, RadiationAC +475 |

### Ectoplasm Tattoos (shared Shade / Meta-Physicist)

Faction-NEUTRAL tattoo line and the easiest one to start on. Locked to Metaphysicist + Shade only, and it interpolates QL1-300, so it stays relevant the whole way up.

| id | Piece | QL | Slots | Requirements | Key bonuses |
|---|---|---|---|---|---|
| 226704 | Ectoplasm Arm Tattoo's | 300 | RightArm, LeftArm | level 200, Expansion 2, Agility 900, Sense 900 | Piercing +8, SneakAttack +8, FastAttack +8, DuckExp +8, Concealment +8, NanoResist +8, BiologicalMetamorphosis +4, MeleeAC +300, EnergyAC +300, ProjectileAC +30 |
| 226702 | Ectoplasm Hand Tattoo's | 300 | Hands | level 200, Expansion 2, Agility 900, Sense 900 | Piercing +8, SneakAttack +8, FastAttack +8, DuckExp +8, Concealment +8, NanoResist +8, BiologicalMetamorphosis +4, MeleeAC +300, EnergyAC +300, ProjectileAC +30 |
| 226708 | Ectoplasm Leg Tattoo's | 300 | Legs | level 200, Expansion 2, Agility 900, Sense 900 | Piercing +8, SneakAttack +8, FastAttack +8, DuckExp +8, Concealment +8, NanoResist +8, BiologicalMetamorphosis +4, MeleeAC +900, EnergyAC +900, ProjectileAC +90 |
| 226710 | Ectoplasm Foot Tattoo's | 300 | Feet | level 200, Expansion 2, Agility 900, Sense 900 | Piercing +8, SneakAttack +8, FastAttack +8, DuckExp +8, Concealment +8, NanoResist +8, BiologicalMetamorphosis +4, MeleeAC +600, EnergyAC +600, ProjectileAC +60 |
| 226706 | Ectoplasm Torso Tattoo's | 300 | Body | level 200, Expansion 2, Agility 900, Sense 900 | Piercing +8, SneakAttack +8, FastAttack +8, DuckExp +8, Concealment +8, NanoResist +8, BiologicalMetamorphosis +4, MeleeAC +1500, EnergyAC +1500, ProjectileAC + |

### Entophagous Tattoos (Clan-only, shared Shade / Meta-Physicist)

Clan side. Same slots and QL span as Ectoplasm.

| id | Piece | QL | Slots | Requirements | Key bonuses |
|---|---|---|---|---|---|
| 226696 | Entophagous Arm Tattoo's | 300 | RightArm, LeftArm | level 200, Expansion 2, Side 1, Agility 900, Sense 900 | Piercing +10, SneakAttack +10, FastAttack +10, DuckExp +10, Concealment +10, NanoResist +10, MaxHealth +50, BiologicalMetamorphosis +5, MeleeAC +450, EnergyAC + |
| 226698 | Entophagous Hand Tattoo's | 300 | Hands | level 200, Expansion 2, Side 1, Agility 900, Sense 900 | Piercing +10, SneakAttack +10, FastAttack +10, DuckExp +10, Concealment +10, NanoResist +10, MaxHealth +50, BiologicalMetamorphosis +5, MeleeAC +450, EnergyAC + |
| 226692 | Entophagous Leg Tattoo's | 300 | Legs | level 200, Expansion 2, Side 1, Agility 900, Sense 900 | Piercing +10, SneakAttack +10, FastAttack +10, DuckExp +10, Concealment +10, NanoResist +10, MaxHealth +50, BiologicalMetamorphosis +5, MeleeAC +1125, EnergyAC  |
| 226690 | Entophagous Foot Tattoo's | 300 | Feet | level 200, Expansion 2, Side 1, Agility 900, Sense 900 | Piercing +10, SneakAttack +10, FastAttack +10, DuckExp +10, Concealment +10, NanoResist +10, MaxHealth +50, BiologicalMetamorphosis +5, MeleeAC +675, EnergyAC + |
| 226694 | Entophagous Torso Tattoo's | 300 | Body | level 200, Expansion 2, Side 1, Agility 900, Sense 900 | Piercing +10, SneakAttack +10, FastAttack +10, DuckExp +10, Concealment +10, NanoResist +10, MaxHealth +50, BiologicalMetamorphosis +5, MeleeAC +1800, EnergyAC  |

### Sombreous Tattoos (Omni-only, shared Shade / Meta-Physicist)

Omni-Tek side. Same slots and QL span as Ectoplasm.

| id | Piece | QL | Slots | Requirements | Key bonuses |
|---|---|---|---|---|---|
| 226675 | Sombreous Arm Tattoo's | 300 | RightArm, LeftArm | level 200, Expansion 2, Side 2, Agility 900, Sense 900 | Piercing +10, SneakAttack +10, FastAttack +10, DuckExp +10, Concealment +10, NanoResist +10, BiologicalMetamorphosis +5, MeleeAC +500, EnergyAC +150, Projectile |
| 226677 | Sombreous Hands Tattoo's | 300 | Hands | level 200, Expansion 2, Side 2, Agility 900, Sense 900 | Piercing +10, SneakAttack +10, FastAttack +10, DuckExp +10, Concealment +10, NanoResist +10, BiologicalMetamorphosis +5, MeleeAC +500, EnergyAC +150, Projectile |
| 226671 | Sombreous Leg Tattoo's | 300 | Legs | level 200, Expansion 2, Side 2, Agility 900, Sense 900 | Piercing +10, SneakAttack +10, FastAttack +10, DuckExp +10, Concealment +10, NanoResist +10, BiologicalMetamorphosis +5, MeleeAC +1175, EnergyAC +450, Projectil |
| 226591 | Sombreous Feet Tattoo's | 300 | Feet | level 200, Expansion 2, Side 2, Agility 900, Sense 900 | Piercing +10, SneakAttack +10, FastAttack +10, DuckExp +10, Concealment +10, NanoResist +10, BiologicalMetamorphosis +5, MeleeAC +725, EnergyAC +300, Projectile |
| 226673 | Sombreous Torso Tattoo's | 300 | Body | level 200, Expansion 2, Side 2, Agility 900, Sense 900 | Piercing +10, SneakAttack +10, FastAttack +10, DuckExp +10, Concealment +10, NanoResist +10, BiologicalMetamorphosis +5, MeleeAC +1850, EnergyAC +750, Projectil |

### Infantry set (shared with MA / Adventurer / Enforcer / Keeper)

Not a Shade-only set; the ToWear lock is MartialArtist/Adventurer/Enforcer/Keeper/Shade.

| id | Piece | QL | Slots | Requirements | Key bonuses |
|---|---|---|---|---|---|
| 215614 | Infantry Helmet | 300 | Head | Expansion 2, Strength 1000, Stamina 1000 | MeleeInit +15, EvadeClsC +15, MaxHealth +75, MeleeAC +1300, EnergyAC +1200, ProjectileAC +1200, FireAC +1200, ColdAC +1200, PoisonAC +900, ChemicalAC +900, Radi |
| 215620 | Infantry Vest | 300 | Body | Expansion 2, Strength 1000, Stamina 1000 | MeleeInit +20, EvadeClsC +20, MaxHealth +100, MeleeAC +1700, EnergyAC +1500, ProjectileAC +1500, FireAC +1500, ColdAC +1500, PoisonAC +1100, ChemicalAC +1100, R |
| 215618 | Infantry Sleeves | 300 | RightArm, LeftArm | Expansion 2, Strength 1000, Stamina 1000 | MeleeInit +10, EvadeClsC +10, MaxHealth +50, MeleeAC +400, EnergyAC +300, ProjectileAC +325, FireAC +300, ColdAC +300, PoisonAC +225, ChemicalAC +225, Radiation |
| 215616 | Infantry Gloves | 300 | Hands | Expansion 2, Strength 1000, Stamina 1000 | MeleeInit +10, EvadeClsC +10, MaxHealth +50, MeleeAC +375, EnergyAC +325, ProjectileAC +325, FireAC +325, ColdAC +325, PoisonAC +200, ChemicalAC +200, Radiation |
| 215624 | Infantry Pants | 300 | Legs | Expansion 2, Strength 1000, Stamina 1000 | MeleeInit +10, EvadeClsC +10, MaxHealth +75, MeleeAC +1000, EnergyAC +850, ProjectileAC +850, FireAC +850, ColdAC +850, PoisonAC +600, ChemicalAC +600, Radiatio |
| 215622 | Infantry Boots | 300 | Feet | Expansion 2, Strength 1000, Stamina 1000 | MeleeInit +10, EvadeClsC +10, MaxHealth +50, MeleeAC +550, EnergyAC +450, ProjectileAC +450, FireAC +450, ColdAC +450, PoisonAC +300, ChemicalAC +300, Radiation |
| 215461 | Infantry Chest Cover | 300 | Back | Expansion 2, Strength 1000, Stamina 1000 | MeleeInit +30, EvadeClsC +30, MaxHealth +225, MeleeAC +1800, EnergyAC +1600, ProjectileAC +1600, FireAC +1600, ColdAC +1600, PoisonAC +1200, ChemicalAC +1200, R |

### Outsider's Armor (shared with Martial Artist)

Interpolates QL1-300.

| id | Piece | QL | Slots | Requirements | Key bonuses |
|---|---|---|---|---|---|
| 226970 | Outsider's Body Armor | 300 | Body | Stamina 900, Agility 900, Expansion 2, level 200 | PhysicalInit +25, AddAllOff +5, EvadeClsC +20, MeleeAC +1800, EnergyAC +1200, ProjectileAC +1200, FireAC +1200, ColdAC +1200, PoisonAC +1200, ChemicalAC +1200,  |
| 226972 | Outsider's Armor Sleeve | 300 | RightArm | Stamina 900, Agility 900, Expansion 2, level 200 | PhysicalInit +25, AddAllOff +5, EvadeClsC +20, MeleeAC +450, EnergyAC +300, ProjectileAC +300, FireAC +300, ColdAC +300, PoisonAC +300, ChemicalAC +300, Radiati |
| 226974 | Outsider's Armor Gloves | 300 | Hands | Stamina 900, Agility 900, Expansion 2, level 200 | PhysicalInit +25, AddAllOff +5, EvadeClsC +20, MeleeAC +375, EnergyAC +300, ProjectileAC +300, FireAC +300, ColdAC +300, PoisonAC +300, ChemicalAC +300, Radiati |
| 226968 | Outsider's Armor Trousers | 300 | Legs | Stamina 900, Agility 900, Expansion 2, level 200 | PhysicalInit +25, AddAllOff +5, EvadeClsC +20, MeleeAC +1125, EnergyAC +750, ProjectileAC +750, FireAC +750, ColdAC +750, PoisonAC +750, ChemicalAC +750, Radiat |
| 226966 | Outsider's Armor Boots | 300 | Feet | Stamina 900, Agility 900, Expansion 2, level 200 | PhysicalInit +25, AddAllOff +5, EvadeClsC +20, MeleeAC +675, EnergyAC +450, ProjectileAC +450, FireAC +450, ColdAC +450, PoisonAC +450, ChemicalAC +450, Radiati |

## Every Shade-locked wearable in the client (122 models, 213 templates)

| Model | id | QL | Clothing slots | Lock | NODROP | Key bonuses at top QL |
|---|---|---|---|---|---|---|
| Achromic Ring for the Infantry Unit | 226068 | 1 | RightFinger | MartialArtist/Enforcer/Keeper/Shade | yes | Piercing +3, MultiMelee +3, SneakAttack +3, FastAttack +3, Dimach +3, Riposte +3, Parry +3, MartialArts +3, MeleeInit +3, PhysicalInit +3, EvadeClsC + |
| Ancient Container | 224110 | 150 | Back | Soldier/MartialArtist/Fixer/Agent/Adventurer/Trader/Enforcer/Keeper/Shade |  | MeleeInit +50, NanoResist +25, MaxHealth +250, MeleeAC +1000, EnergyAC +850, ProjectileAC +1000, FireAC +850, ColdAC +425, PoisonAC +850, ChemicalAC + |
| Anun Tooth Trophy | 248918 | 125-200 | Neck | Shade | yes | MaxHealth +150 |
| Arm Tattoo's of Eric Miller | 223990 | 80-120 | RightArm/LeftArm | Shade |  | MaxHealth +40, MaxNanoEnergy +25, MeleeAC +225, EnergyAC +125, ProjectileAC +125, FireAC +125, ColdAC +125, PoisonAC +125, ChemicalAC +125, RadiationA |
| Arm Tattoo's of Min-Li Jiu | 223975 | 80-120 | RightArm/LeftArm | Shade |  | MaxHealth +40, MaxNanoEnergy +25, MeleeAC +225, EnergyAC +125, ProjectileAC +125, FireAC +125, ColdAC +125, PoisonAC +125, ChemicalAC +125, RadiationA |
| Aurea's Heated Cloak | 223705 | 20-80 | Back | MartialArtist/Fixer/Agent/Adventurer/Shade | yes | EvadeClsC +16, DodgeRanged +16, DuckExp +16, RunSpeed +16, Concealment +16, MeleeAC +400, EnergyAC +200, ProjectileAC +400, FireAC +200, ColdAC +600,  |
| Bigot's Armband of Sacrifice | 245773 | 250 | RightArm/LeftArm | Agent/Shade |  | ChemicalDamageModifier +15, MaxHealth +200, MeleeAC +300, EnergyAC +375, ProjectileAC +375, FireAC +375, ColdAC +375, PoisonAC +375, ChemicalAC +450,  |
| Blood-Soaked Cloak of Dishonour | 246085 | 250 | Back | Shade |  | EvadeClsC +75, DodgeRanged +75, DuckExp +75, RunSpeed +75, MeleeAC +1000, EnergyAC +1000, ProjectileAC +1000, FireAC +1000, ColdAC +1000, PoisonAC +10 |
| Brother's Brass Knuckles | 246223 | 100 | Hands | MartialArtist/Adventurer/Enforcer/Keeper/Shade |  | Dimach +10, Riposte +10, Parry +10, MartialArts +10, ChemicalDamageModifier +10, RunSpeed +10, ChemicalAC +300 |
| Caliginous Ring for the Infantry Unit | 226126 | 1 | RightFinger | MartialArtist/Enforcer/Keeper/Shade | yes | Piercing +2, MultiMelee +2, SneakAttack +2, FastAttack +2, Dimach +2, Riposte +2, Parry +2, MartialArts +2, MeleeInit +2, PhysicalInit +2, EvadeClsC + |
| Chosen Shade Arm Tattoos | 222670 | 220-300 | RightArm/LeftArm | Shade | yes | MultiMelee +20, MeleeInit +24, MaxHealth +250, MaterialMetamorphosis +12, MeleeAC +450, EnergyAC +300, ProjectileAC +450, FireAC +450, ColdAC +375, Po |
| Chosen Shade Body Wraps | 222668 | 220-300 | Body | Shade | yes | Stamina +20, MaterialCreation +24, MaxNCU +24, MeleeAC +1800, EnergyAC +1200, ProjectileAC +1800, FireAC +1800, ColdAC +1500, PoisonAC +1800, Chemical |
| Chosen Shade Foot Covers | 222664 | 220-300 | Feet | Shade | yes | DodgeRanged +40, DuckExp +40, RunSpeed +50, MaxHealth +250, SensoryImprovement +24, MeleeAC +675, EnergyAC +455, ProjectileAC +675, FireAC +675, ColdA |
| Chosen Shade Hand Wraps | 222674 | 220-300 | Hands | Shade | yes | Piercing +60, Dimach +40, MaxHealth +250, Sense +30, PsychologicalModification +24, MeleeAC +375, EnergyAC +250, ProjectileAC +375, FireAC +375, ColdA |
| Chosen Shade Headgear | 222678 | 220-300 | Head | Shade | yes | Concealment +60, MaxHealth +200, Intelligence +20, SpaceTime +24, MeleeAC +1350, EnergyAC +900, ProjectileAC +1350, FireAC +1350, ColdAC +1125, Poison |
| Chosen Shade Leg Covers | 222662 | 220-300 | Legs | Shade | yes | EvadeClsC +40, MaxHealth +550, Agility +30, MaxNCU +16, MeleeAC +1125, EnergyAC +755, ProjectileAC +1125, FireAC +1125, ColdAC +940, PoisonAC +1125, C |
| Chosen Shade Shoulder Tattoos | 222682 | 220-300 | RightShoulder/LeftShoulder | Shade | yes | Parry +20, MaxHealth +400, Strength +10, BiologicalMetamorphosis +12, MeleeAC +225, EnergyAC +125, ProjectileAC +225, FireAC +225, ColdAC +175, Poison |
| Chosen Shade Support System | 222684 | 220-300 | Back | Shade | yes | SneakAttack +50, Psychic +20, MaxNanoEnergy +550, MeleeAC +1800, EnergyAC +900, ProjectileAC +1800, FireAC +1800, ColdAC +1500, PoisonAC +1800, Chemic |
| Doubleknit Pants | 245372 | 150 | Legs | MartialArtist/Shade |  | Dimach +25, Parry +25, Concealment +25, MaxHealth +100, MaxNanoEnergy +100, MeleeAC +500, EnergyAC +500, ProjectileAC +500, FireAC +250, ColdAC +250,  |
| Ectoplasm Arm Tattoo's | 226704 | 1-300 | RightArm/LeftArm | Metaphysicist/Shade |  | Piercing +8, SneakAttack +8, FastAttack +8, DuckExp +8, Concealment +8, NanoResist +8, BiologicalMetamorphosis +4, MeleeAC +300, EnergyAC +300, Projec |
| Ectoplasm Foot Tattoo's | 226710 | 1-300 | Feet | Metaphysicist/Shade |  | Piercing +8, SneakAttack +8, FastAttack +8, DuckExp +8, Concealment +8, NanoResist +8, BiologicalMetamorphosis +4, MeleeAC +600, EnergyAC +600, Projec |
| Ectoplasm Hand Tattoo's | 226702 | 1-300 | Hands | Metaphysicist/Shade |  | Piercing +8, SneakAttack +8, FastAttack +8, DuckExp +8, Concealment +8, NanoResist +8, BiologicalMetamorphosis +4, MeleeAC +300, EnergyAC +300, Projec |
| Ectoplasm Leg Tattoo's | 226708 | 1-300 | Legs | Metaphysicist/Shade |  | Piercing +8, SneakAttack +8, FastAttack +8, DuckExp +8, Concealment +8, NanoResist +8, BiologicalMetamorphosis +4, MeleeAC +900, EnergyAC +900, Projec |
| Ectoplasm Torso Tattoo's | 226706 | 1-300 | Body | Metaphysicist/Shade |  | Piercing +8, SneakAttack +8, FastAttack +8, DuckExp +8, Concealment +8, NanoResist +8, BiologicalMetamorphosis +4, MeleeAC +1500, EnergyAC +1500, Proj |
| Enel Gil's Raging Spirit Tattoo | 223722 | 50 | RightArm/LeftArm | Adventurer/Enforcer/Shade |  | SneakAttack +5, MeleeInit +10, EvadeClsC +10, Agility +5, Strength +5, MeleeAC +175, EnergyAC +125, ProjectileAC +175, FireAC +125, ColdAC +125, Poiso |
| Entophagous Arm Tattoo's | 226696 | 1-300 | RightArm/LeftArm | Metaphysicist/Shade |  | Piercing +10, SneakAttack +10, FastAttack +10, DuckExp +10, Concealment +10, NanoResist +10, MaxHealth +50, BiologicalMetamorphosis +5, MeleeAC +450,  |
| Entophagous Foot Tattoo's | 226690 | 1-300 | Feet | Metaphysicist/Shade |  | Piercing +10, SneakAttack +10, FastAttack +10, DuckExp +10, Concealment +10, NanoResist +10, MaxHealth +50, BiologicalMetamorphosis +5, MeleeAC +675,  |
| Entophagous Hand Tattoo's | 226698 | 1-300 | Hands | Metaphysicist/Shade |  | Piercing +10, SneakAttack +10, FastAttack +10, DuckExp +10, Concealment +10, NanoResist +10, MaxHealth +50, BiologicalMetamorphosis +5, MeleeAC +450,  |
| Entophagous Leg Tattoo's | 226692 | 1-300 | Legs | Metaphysicist/Shade |  | Piercing +10, SneakAttack +10, FastAttack +10, DuckExp +10, Concealment +10, NanoResist +10, MaxHealth +50, BiologicalMetamorphosis +5, MeleeAC +1125, |
| Entophagous Torso Tattoo's | 226694 | 1-300 | Body | Metaphysicist/Shade |  | Piercing +10, SneakAttack +10, FastAttack +10, DuckExp +10, Concealment +10, NanoResist +10, MaxHealth +50, BiologicalMetamorphosis +5, MeleeAC +1800, |
| Eva Pourais' Snakeskin Patch | 224093 | 150 | Neck/RightShoulder/LeftShoulder | MartialArtist/Fixer/Agent/Shade | yes | AddAllOff +6, EvadeClsC +12, DodgeRanged +12, DuckExp +6, Concealment +6, Agility +4, Sense +4 |
| Faithful Shade Arm Tattoos | 216684 | 220-300 | RightArm/LeftArm | Shade | yes | MultiMelee +20, MeleeInit +24, MaxHealth +250, MaterialMetamorphosis +12, MeleeAC +450, EnergyAC +300, ProjectileAC +450, FireAC +375, ColdAC +450, Po |
| Faithful Shade Body Wraps | 216686 | 220-300 | Body | Shade | yes | Stamina +20, MaterialCreation +24, MaxNCU +24, MeleeAC +1800, EnergyAC +1200, ProjectileAC +1800, FireAC +1500, ColdAC +1800, PoisonAC +1800, Chemical |
| Faithful Shade Foot Covers | 216688 | 220-300 | Feet | Shade | yes | DodgeRanged +40, DuckExp +40, RunSpeed +50, MaxHealth +250, SensoryImprovement +24, MeleeAC +675, EnergyAC +455, ProjectileAC +675, FireAC +565, ColdA |
| Faithful Shade Hand Wraps | 216682 | 220-300 | Hands | Shade | yes | Piercing +60, Dimach +40, MaxHealth +250, Sense +30, PsychologicalModification +24, MeleeAC +375, EnergyAC +250, ProjectileAC +375, FireAC +310, ColdA |
| Faithful Shade Headgear | 216680 | 220-300 | Head | Shade | yes | Concealment +60, MaxHealth +200, Intelligence +20, SpaceTime +24, MeleeAC +1350, EnergyAC +900, ProjectileAC +1350, FireAC +1125, ColdAC +1350, Poison |
| Faithful Shade Leg Covers | 216690 | 220-300 | Legs | Shade | yes | EvadeClsC +40, MaxHealth +550, Agility +30, MaxNCU +16, MeleeAC +1125, EnergyAC +755, ProjectileAC +1125, FireAC +940, ColdAC +1125, PoisonAC +1125, C |
| Faithful Shade Shoulder Tattoos | 216678 | 220-300 | RightShoulder/LeftShoulder | Shade | yes | Parry +20, MaxHealth +400, Strength +10, BiologicalMetamorphosis +12, MeleeAC +225, EnergyAC +125, ProjectileAC +225, FireAC +175, ColdAC +225, Poison |
| Faithful Shade Support System | 216676 | 220-300 | Back | Shade | yes | SneakAttack +50, Psychic +20, MaxNanoEnergy +550, MeleeAC +1800, EnergyAC +900, ProjectileAC +1800, FireAC +1500, ColdAC +1800, PoisonAC +1800, Chemic |
| First Tier Shade Arm Tattoos | 222928 | 100-160 | RightArm/LeftArm | Shade | yes | MeleeAC +300, EnergyAC +225, ProjectileAC +300, FireAC +225, ColdAC +225, PoisonAC +300, ChemicalAC +300, RadiationAC +300 |
| First Tier Shade Body Wraps | 222926 | 100-160 | Body | Shade | yes | MeleeAC +1200, EnergyAC +900, ProjectileAC +1200, FireAC +900, ColdAC +900, PoisonAC +1200, ChemicalAC +1200, RadiationAC +1200 |
| First Tier Shade Foot Covers | 222924 | 100-160 | Feet | Shade | yes | RunSpeed +20, MeleeAC +455, EnergyAC +345, ProjectileAC +455, FireAC +345, ColdAC +345, PoisonAC +455, ChemicalAC +455, RadiationAC +455 |
| First Tier Shade Hand Wraps | 222930 | 100-160 | Hands | Shade | yes | Piercing +20, MeleeAC +250, EnergyAC +190, ProjectileAC +250, FireAC +190, ColdAC +190, PoisonAC +250, ChemicalAC +250, RadiationAC +250 |
| First Tier Shade Leg Covers | 222922 | 100-160 | Legs | Shade | yes | MaxHealth +100, MeleeAC +755, EnergyAC +570, ProjectileAC +755, FireAC +570, ColdAC +570, PoisonAC +755, ChemicalAC +755, RadiationAC +755 |
| Foot Tattoo's of Eric Miller | 223984 | 80-120 | Feet | Shade |  | MaxHealth +75, MaxNanoEnergy +50, MeleeAC +300, EnergyAC +200, ProjectileAC +200, FireAC +200, ColdAC +200, PoisonAC +200, ChemicalAC +200, RadiationA |
| Foot Tattoo's of Min-Li Jiu | 223954 | 80-120 | Feet | Shade |  | MaxHealth +75, MaxNanoEnergy +50, MeleeAC +300, EnergyAC +200, ProjectileAC +200, FireAC +200, ColdAC +200, PoisonAC +200, ChemicalAC +200, RadiationA |
| Great Grandfather's Flesh Sleeves | 233469 | 110 | RightArm/LeftArm | MartialArtist/Enforcer/Shade |  | Piercing +7, MaxHealth +75, Sense +7, MeleeAC +200, EnergyAC +200, ProjectileAC +200, FireAC +200, ColdAC +200, PoisonAC +200, ChemicalAC +200, Radiat |
| Hand Tattoo's of Eric Miller | 223992 | 80-120 | Hands | Shade |  | MaxHealth +35, MaxNanoEnergy +25, MeleeAC +225, EnergyAC +125, ProjectileAC +125, FireAC +125, ColdAC +125, PoisonAC +125, ChemicalAC +125, RadiationA |
| Hand Tattoo's of Min-Li Jiu | 223980 | 80-120 | Hands | Shade |  | MaxHealth +35, MaxNanoEnergy +25, MeleeAC +225, EnergyAC +125, ProjectileAC +125, FireAC +125, ColdAC +125, PoisonAC +125, ChemicalAC +125, RadiationA |
| Improved Ofab Shade Body Armor | 264556 | 1-300 | Body | Shade | yes | AddAllOff +20, Stamina +20, MaterialCreation +25, MaxNCU +25, ComputerLiteracy +55, MeleeAC +1890, EnergyAC +1890, ProjectileAC +1890, FireAC +473, Co |
| Improved Ofab Shade Boots | 264562 | 1-300 | Feet | Shade | yes | DodgeRanged +45, DuckExp +45, RunSpeed +45, MaxHealth +300, SensoryImprovement +30, MeleeAC +709, EnergyAC +709, ProjectileAC +709, FireAC +178, ColdA |
| Improved Ofab Shade Gloves | 264544 | 1-300 | Hands | Shade | yes | Piercing +65, Dimach +35, MaxHealth +300, Sense +27, PsychologicalModification +28, MeleeAC +394, EnergyAC +394, ProjectileAC +394, FireAC +394, ColdA |
| Improved Ofab Shade Headgear | 264538 | 1-300 | Head | Shade | yes | AddAllOff +25, Concealment +65, MaxHealth +300, Intelligence +22, MeleeAC +1418, EnergyAC +1418, ProjectileAC +1418, FireAC +1418, ColdAC +1418, Poiso |
| Improved Ofab Shade Pants | 264568 | 1-300 | Legs | Shade | yes | EvadeClsC +55, MaxHealth +500, Agility +27, MaxNCU +20, MeleeAC +1182, EnergyAC +1182, ProjectileAC +1182, FireAC +1182, ColdAC +1182, PoisonAC +1182, |
| Improved Ofab Shade Sleeves | 264550 | 1-300 | RightArm/LeftArm | Shade | yes | MultiMelee +25, MeleeInit +30, MaxHealth +300, MaterialMetamorphosis +14, MeleeAC +473, EnergyAC +473, ProjectileAC +473, FireAC +473, ColdAC +473, Po |
| Infantry Boots | 215622 | 25-300 | Feet | MartialArtist/Adventurer/Enforcer/Keeper/Shade |  | MeleeInit +10, EvadeClsC +10, MaxHealth +50, MeleeAC +550, EnergyAC +450, ProjectileAC +450, FireAC +450, ColdAC +450, PoisonAC +300, ChemicalAC +300, |
| Infantry Chest Cover | 215461 | 75-300 | Back | MartialArtist/Adventurer/Enforcer/Keeper/Shade |  | MeleeInit +30, EvadeClsC +30, MaxHealth +225, MeleeAC +1800, EnergyAC +1600, ProjectileAC +1600, FireAC +1600, ColdAC +1600, PoisonAC +1200, ChemicalA |
| Infantry Gloves | 215616 | 25-300 | Hands | MartialArtist/Adventurer/Enforcer/Keeper/Shade |  | MeleeInit +10, EvadeClsC +10, MaxHealth +50, MeleeAC +375, EnergyAC +325, ProjectileAC +325, FireAC +325, ColdAC +325, PoisonAC +200, ChemicalAC +200, |
| Infantry Helmet | 215614 | 75-300 | Head | MartialArtist/Adventurer/Enforcer/Keeper/Shade |  | MeleeInit +15, EvadeClsC +15, MaxHealth +75, MeleeAC +1300, EnergyAC +1200, ProjectileAC +1200, FireAC +1200, ColdAC +1200, PoisonAC +900, ChemicalAC  |
| Infantry Pants | 215624 | 25-300 | Legs | MartialArtist/Adventurer/Enforcer/Keeper/Shade |  | MeleeInit +10, EvadeClsC +10, MaxHealth +75, MeleeAC +1000, EnergyAC +850, ProjectileAC +850, FireAC +850, ColdAC +850, PoisonAC +600, ChemicalAC +600 |
| Infantry Sleeves | 215618 | 25-300 | RightArm/LeftArm | MartialArtist/Adventurer/Enforcer/Keeper/Shade |  | MeleeInit +10, EvadeClsC +10, MaxHealth +50, MeleeAC +400, EnergyAC +300, ProjectileAC +325, FireAC +300, ColdAC +300, PoisonAC +225, ChemicalAC +225, |
| Infantry Vest | 215620 | 25-300 | Body | MartialArtist/Adventurer/Enforcer/Keeper/Shade |  | MeleeInit +20, EvadeClsC +20, MaxHealth +100, MeleeAC +1700, EnergyAC +1500, ProjectileAC +1500, FireAC +1500, ColdAC +1500, PoisonAC +1100, ChemicalA |
| Insipid Ring for the Infantry Unit | 225760 | 1 | RightFinger | MartialArtist/Enforcer/Keeper/Shade | yes | Piercing +1, MultiMelee +1, SneakAttack +1, FastAttack +1, Dimach +1, Riposte +1, Parry +1, MartialArts +1, MeleeInit +1, PhysicalInit +1, EvadeClsC + |
| Jobe Shade Support System | 248380 | 220 | Back | Shade | yes | NanoResist +24, MaxHealth +100, MeleeAC +1100, EnergyAC +1100, ProjectileAC +1100, FireAC +1100, ColdAC +1100, PoisonAC +1100, ChemicalAC +1100, Radia |
| Joshua Khan's Catskin Patch | 224092 | 150 | Neck/RightShoulder/LeftShoulder | MartialArtist/Fixer/Agent/Shade | yes | AddAllDef +6, EvadeClsC +12, DodgeRanged +12, DuckExp +6, Concealment +6, Agility +4, Sense +4 |
| Leg Tattoo's of Eric Miller | 223986 | 80-120 | Legs | Shade |  | MaxHealth +110, MaxNanoEnergy +75, MeleeAC +550, EnergyAC +350, ProjectileAC +350, FireAC +350, ColdAC +350, PoisonAC +350, ChemicalAC +350, Radiation |
| Leg Tattoo's of Min-Li Jiu | 223956 | 80-120 | Legs | Shade |  | MaxHealth +110, MaxNanoEnergy +75, MeleeAC +550, EnergyAC +350, ProjectileAC +350, FireAC +350, ColdAC +350, PoisonAC +350, ChemicalAC +350, Radiation |
| Nar Shere's Temperamental Spirit Tattoo | 223725 | 50 | RightArm/LeftArm | Adventurer/Enforcer/Shade |  | SneakAttack +5, MeleeInit +10, EvadeClsC +10, Agility +5, Strength +5, MeleeAC +175, EnergyAC +125, ProjectileAC +175, FireAC +125, ColdAC +125, Poiso |
| Necklace of Alacrity | 263290 | 125-150 | Neck | Shade | yes | MeleeInit +100, AddAllDef +100 |
| Net of Adonis | 233427 | 150 | Back | MartialArtist/Fixer/Agent/Shade |  | Concealment +20, Perception +20, MaxHealth +200, MeleeAC +700, EnergyAC +675, ProjectileAC +700, FireAC +675, ColdAC +675, PoisonAC +675, ChemicalAC + |
| OFAB Shade Protective Gear | 267934 | 300 | Back | Shade |  | SneakAttack +50, MeleeInit +50, AddAllOff +25, MaxHealth +500, Stamina +15, Psychic +25, MaxNCU +25, MaxNanoEnergy +700, MeleeAC +2000, EnergyAC +2000 |
| OFAB Shade Shoulder Wear | 268005 | 300 | RightShoulder/LeftShoulder | Shade |  | Parry +25, AddAllDef +10, MaxHealth +500, Strength +12, Stamina +12, BiologicalMetamorphosis +15, MeleeAC +250, EnergyAC +250, ProjectileAC +250, Fire |
| Ofab Shade Body Armor | 264558 | 1-300 | Body | Shade |  | AddAllOff +15, Stamina +18, MaterialCreation +20, MaxNCU +20, ComputerLiteracy +50, MeleeAC +1890, EnergyAC +1890, ProjectileAC +1890, FireAC +1890, C |
| Ofab Shade Boots | 264564 | 1-300 | Feet | Shade |  | DodgeRanged +40, DuckExp +40, RunSpeed +40, MaxHealth +265, SensoryImprovement +25, MeleeAC +709, EnergyAC +709, ProjectileAC +709, FireAC +709, ColdA |
| Ofab Shade Gloves | 264546 | 1-300 | Hands | Shade |  | Piercing +60, Dimach +30, MaxHealth +265, Sense +25, PsychologicalModification +25, MeleeAC +394, EnergyAC +394, ProjectileAC +394, FireAC +394, ColdA |
| Ofab Shade Headgear | 264540 | 1-300 | Head | Shade |  | AddAllOff +20, Concealment +60, MaxHealth +265, Intelligence +20, MeleeAC +1418, EnergyAC +1418, ProjectileAC +1418, FireAC +1418, ColdAC +1418, Poiso |
| Ofab Shade Pants | 264570 | 1-300 | Legs | Shade |  | EvadeClsC +50, MaxHealth +400, Agility +25, MaxNCU +15, MeleeAC +1182, EnergyAC +1182, ProjectileAC +1182, FireAC +1182, ColdAC +1182, PoisonAC +1182, |
| Ofab Shade Sleeves | 264552 | 1-300 | RightArm/LeftArm | Shade |  | MultiMelee +20, MeleeInit +25, MaxHealth +265, MaterialMetamorphosis +12, MeleeAC +473, EnergyAC +473, ProjectileAC +473, FireAC +473, ColdAC +473, Po |
| Outsider's Armor Boots | 226966 | 1-300 | Feet | MartialArtist/Shade |  | PhysicalInit +25, AddAllOff +5, EvadeClsC +20, MeleeAC +675, EnergyAC +450, ProjectileAC +450, FireAC +450, ColdAC +450, PoisonAC +450, ChemicalAC +45 |
| Outsider's Armor Gloves | 226974 | 1-300 | Hands | MartialArtist/Shade |  | PhysicalInit +25, AddAllOff +5, EvadeClsC +20, MeleeAC +375, EnergyAC +300, ProjectileAC +300, FireAC +300, ColdAC +300, PoisonAC +300, ChemicalAC +30 |
| Outsider's Armor Sleeve | 226972 | 1-300 | RightArm | MartialArtist/Shade |  | PhysicalInit +25, AddAllOff +5, EvadeClsC +20, MeleeAC +450, EnergyAC +300, ProjectileAC +300, FireAC +300, ColdAC +300, PoisonAC +300, ChemicalAC +30 |
| Outsider's Armor Trousers | 226968 | 1-300 | Legs | MartialArtist/Shade |  | PhysicalInit +25, AddAllOff +5, EvadeClsC +20, MeleeAC +1125, EnergyAC +750, ProjectileAC +750, FireAC +750, ColdAC +750, PoisonAC +750, ChemicalAC +7 |
| Outsider's Body Armor | 226970 | 1-300 | Body | MartialArtist/Shade |  | PhysicalInit +25, AddAllOff +5, EvadeClsC +20, MeleeAC +1800, EnergyAC +1200, ProjectileAC +1200, FireAC +1200, ColdAC +1200, PoisonAC +1200, Chemical |
| Penultimate Ofab Shade Body Armor | 264554 | 1-300 | Body | Shade | yes | AddAllOff +25, Stamina +25, MaterialCreation +30, MaxNCU +30, ComputerLiteracy +55, MeleeAC +1890, EnergyAC +1890, ProjectileAC +1890, FireAC +1890, C |
| Penultimate Ofab Shade Boots | 264560 | 1-300 | Feet | Shade | yes | DodgeRanged +55, DuckExp +55, RunSpeed +55, MaxHealth +350, SensoryImprovement +35, MeleeAC +709, EnergyAC +709, ProjectileAC +709, FireAC +709, ColdA |
| Penultimate Ofab Shade Gloves | 264542 | 1-300 | Hands | Shade | yes | Piercing +75, Dimach +55, MaxHealth +350, Sense +32, PsychologicalModification +32, MeleeAC +394, EnergyAC +394, ProjectileAC +394, FireAC +394, ColdA |
| Penultimate Ofab Shade Headgear | 264536 | 1-300 | Head | Shade | yes | AddAllOff +35, Concealment +75, MaxHealth +350, Intelligence +25, MeleeAC +1418, EnergyAC +1418, ProjectileAC +1418, FireAC +1418, ColdAC +1418, Poiso |
| Penultimate Ofab Shade Pants | 264566 | 1-300 | Legs | Shade | yes | EvadeClsC +65, MaxHealth +650, Agility +35, MaxNCU +25, ComputerLiteracy +25, MeleeAC +1182, EnergyAC +1182, ProjectileAC +1182, FireAC +1182, ColdAC  |
| Penultimate Ofab Shade Sleeves | 264548 | 1-300 | RightArm/LeftArm | Shade | yes | MultiMelee +30, MeleeInit +26, MaxHealth +350, MaterialMetamorphosis +20, MeleeAC +473, EnergyAC +473, ProjectileAC +473, FireAC +473, ColdAC +473, Po |
| Pure Novictum Ring for the Infantry Unit | 226307 | 1 | RightFinger | MartialArtist/Enforcer/Keeper/Shade | yes | Piercing +10, MultiMelee +10, SneakAttack +10, FastAttack +10, Dimach +10, Riposte +10, Parry +10, MartialArts +10, MeleeInit +10, PhysicalInit +10, E |
| Replica Armor Set: Penultimate Shade OFAB | 289092 | 1 | Back/Body | Shade | yes |  |
| Replica Armor Set: Penultimate Shade OFAB with Helmet | 289078 | 1 | Back/Body | Shade | yes |  |
| Replica Armor Set: Standard Shade OFAB | 289063 | 1 | Back/Body | Shade | yes |  |
| Replica Armor Set: Standard Shade OFAB with Helmet | 289049 | 1 | Back/Body | Shade | yes |  |
| Rimy Ring for the Infantry Unit | 226191 | 1 | RightFinger | MartialArtist/Enforcer/Keeper/Shade | yes | Piercing +5, MultiMelee +5, SneakAttack +5, FastAttack +5, Dimach +5, Riposte +5, Parry +5, MartialArts +5, MeleeInit +5, PhysicalInit +5, EvadeClsC + |
| Sanguine Ring for the Infantry Unit | 226295 | 1 | RightFinger | MartialArtist/Enforcer/Keeper/Shade | yes | Piercing +7, MultiMelee +7, SneakAttack +7, FastAttack +7, Dimach +7, Riposte +7, Parry +7, MartialArts +7, MeleeInit +7, PhysicalInit +7, EvadeClsC + |
| Second Tier Shade Arm Tattoos | 222904 | 160-220 | RightArm/LeftArm | Shade | yes | MeleeInit +12, MeleeAC +375, EnergyAC +225, ProjectileAC +375, FireAC +300, ColdAC +300, PoisonAC +375, ChemicalAC +375, RadiationAC +375 |
| Second Tier Shade Body Wraps | 222902 | 160-220 | Body | Shade | yes | MeleeAC +1500, EnergyAC +900, ProjectileAC +1500, FireAC +1200, ColdAC +1200, PoisonAC +1500, ChemicalAC +1500, RadiationAC +1500 |
| Second Tier Shade Foot Covers | 222900 | 160-220 | Feet | Shade | yes | DuckExp +20, RunSpeed +30, MeleeAC +565, EnergyAC +345, ProjectileAC +565, FireAC +455, ColdAC +455, PoisonAC +565, ChemicalAC +565, RadiationAC +565 |
| Second Tier Shade Hand Wraps | 222908 | 160-220 | Hands | Shade | yes | Piercing +40, Dimach +30, MeleeAC +310, EnergyAC +190, ProjectileAC +310, FireAC +250, ColdAC +250, PoisonAC +310, ChemicalAC +310, RadiationAC +310 |
| Second Tier Shade Headgear | 222912 | 160-220 | Head | Shade | yes | Intelligence +10, MeleeAC +1125, EnergyAC +900, ProjectileAC +1125, FireAC +900, ColdAC +900, PoisonAC +1125, ChemicalAC +1125, RadiationAC +1125 |
| Second Tier Shade Leg Covers | 222898 | 160-220 | Legs | Shade | yes | EvadeClsC +20, MaxHealth +225, MeleeAC +940, EnergyAC +570, ProjectileAC +940, FireAC +755, ColdAC +755, PoisonAC +940, ChemicalAC +940, RadiationAC + |
| Second Tier Shade Shoulder Tattoos | 222914 | 160-220 | RightShoulder/LeftShoulder | Shade | yes | Parry +10, MeleeAC +175, EnergyAC +125, ProjectileAC +175, FireAC +125, ColdAC +125, PoisonAC +175, ChemicalAC +175, RadiationAC +175 |
| Shades' Ring of Shadows | 267577 | 1-300 | RightFinger/LeftFinger | Shade |  | Piercing +20, MultiMelee +20, AddAllOff +20, AddAllDef +20, Concealment +20, MaxHealth +150 |
| Sharl's Cybernetic Tattoo | 269511 | 200 | RightArm/LeftArm | Shade | yes | SneakAttack +13, FastAttack +13, MeleeInit +30, DuckExp +13, Concealment +13, NanoResist +13, ComputerLiteracy +50, XPModifier +7, MeleeAC +295, Energ |
| Shoulder Marking of Auraka | 304619 | 150 | LeftShoulder | Shade |  | Parry +20, EvadeClsC +24, DodgeRanged +12, DuckExp +12, Concealment +15, Strength +8, Stamina +8, MeleeAC +30, EnergyAC +25, ProjectileAC +15, FireAC  |
| Shuffling Finger | 246289 | 100 | RightFinger/LeftFinger | Adventurer/Enforcer/Shade |  | MultiMelee +10 |
| Slippers of Screaming | 245890 | 250 | Feet | MartialArtist/Shade |  | EvadeClsC +60, DodgeRanged +60, DuckExp +60, RunSpeed +60, MeleeAC +600, EnergyAC +600, ProjectileAC +600, FireAC +600, ColdAC +600, PoisonAC +600, Ch |
| Sombreous Arm Tattoo's | 226675 | 1-300 | RightArm/LeftArm | Metaphysicist/Shade |  | Piercing +10, SneakAttack +10, FastAttack +10, DuckExp +10, Concealment +10, NanoResist +10, BiologicalMetamorphosis +5, MeleeAC +500, EnergyAC +150,  |
| Sombreous Feet Tattoo's | 226591 | 1-300 | Feet | Metaphysicist/Shade |  | Piercing +10, SneakAttack +10, FastAttack +10, DuckExp +10, Concealment +10, NanoResist +10, BiologicalMetamorphosis +5, MeleeAC +725, EnergyAC +300,  |
| Sombreous Hands Tattoo's | 226677 | 1-300 | Hands | Metaphysicist/Shade |  | Piercing +10, SneakAttack +10, FastAttack +10, DuckExp +10, Concealment +10, NanoResist +10, BiologicalMetamorphosis +5, MeleeAC +500, EnergyAC +150,  |
| Sombreous Leg Tattoo's | 226671 | 1-300 | Legs | Metaphysicist/Shade |  | Piercing +10, SneakAttack +10, FastAttack +10, DuckExp +10, Concealment +10, NanoResist +10, BiologicalMetamorphosis +5, MeleeAC +1175, EnergyAC +450, |
| Sombreous Torso Tattoo's | 226673 | 1-300 | Body | Metaphysicist/Shade |  | Piercing +10, SneakAttack +10, FastAttack +10, DuckExp +10, Concealment +10, NanoResist +10, BiologicalMetamorphosis +5, MeleeAC +1850, EnergyAC +750, |
| Special Edition Ofab Shade Headgear | 267378 | 1-300 | Head | Shade |  | AddAllOff +50, Concealment +150, MaxHealth +500, Agility +25, Stamina +25, MeleeAC +1600, EnergyAC +1600, ProjectileAC +1600, FireAC +1600, ColdAC +16 |
| Spider Web Tattoo | 245303 | 150 | RightArm/LeftArm | Shade |  | Parry +20, PoisonDamageModifier +20, MaxHealth +200, MeleeAC +600, ProjectileAC +200 |
| Spirit's Signet of The Apocalypse | 281206 | 1 | RightFinger/LeftFinger | Shade | yes | RunSpeed +100, NanoResist +700, FireAC +200 |
| Stinging Louse | 260679 | 250 | Head | Enforcer/Shade | yes | Piercing +12, MultiMelee +12, SneakAttack +12, Parry +12, MeleeInit +12, PhysicalInit +12, AddAllOff +12, AddAllDef +12, EvadeClsC +12, DodgeRanged +1 |
| Tattoo of Inner Peace | 223765 | 1 | RightArm/LeftArm | Shade | yes | ProjectileAC +20 |
| Titan Viper Tattoo | 165202 | 200 | RightArm/LeftArm | MartialArtist/Shade | yes | Piercing +8, MartialArts +8, Agility +8, Strength +8, MeleeAC +400, ProjectileAC +300, PoisonAC +400 |
| Torso Tattoo's of Eric Miller | 223988 | 80-120 | Body | Shade |  | MaxHealth +150, MaxNanoEnergy +100, MeleeAC +900, EnergyAC +475, ProjectileAC +475, FireAC +475, ColdAC +475, PoisonAC +475, ChemicalAC +475, Radiatio |
| Torso Tattoo's of Min-Li Jiu | 223972 | 80-120 | Body | Shade |  | MaxHealth +150, MaxNanoEnergy +100, MeleeAC +900, EnergyAC +475, ProjectileAC +475, FireAC +475, ColdAC +475, PoisonAC +475, ChemicalAC +475, Radiatio |
| Yuan Chi's Air-cooled Cloak | 223715 | 20-80 | Back | MartialArtist/Fixer/Agent/Adventurer/Shade | yes | EvadeClsC +16, DodgeRanged +16, DuckExp +16, RunSpeed +16, Concealment +16, MeleeAC +400, EnergyAC +200, ProjectileAC +400, FireAC +600, ColdAC +200,  |

## Usable gear pool per slot

The complete ItemClass-2 wearable pool a Shade can equip, counted per clothing slot, with the 40 highest Shade-relevance models per slot listed. shadeScore is a STATED ranking formula, not a community tier list: offensive Shade skills x4, evades/Add All Def/Agility/Sense/Concealment/Run Speed/Nano Resist/Heal Delta x2, other tracked stats x0.5, Max Health x0.2, AC x0.05.

| Slot | Templates usable | Distinct models | Shade-locked models |
|---|---|---|---|
| Neck | 977 | 845 | 4 |
| Head | 628 | 470 | 9 |
| Back | 386 | 326 | 14 |
| RightShoulder | 156 | 129 | 4 |
| Body | 551 | 409 | 14 |
| LeftShoulder | 42 | 36 | 1 |
| RightArm | 485 | 356 | 22 |
| Hands | 471 | 343 | 15 |
| LeftArm | 4 | 4 | 0 |
| RightWrist | 439 | 255 | 0 |
| Legs | 481 | 355 | 15 |
| LeftWrist | 322 | 170 | 0 |
| RightFinger | 229 | 144 | 9 |
| Feet | 484 | 348 | 15 |

## Reading the slot bitmask

The client stores an item's slot as a bitmask in stat 298, where the bit index is (EquipSlot & 0x0F) from AOSharp.Common/GameData/EquipSlot.cs. The PAGE (weapon 0x0n, clothing 0x1n, implant 0x2n) is NOT in the mask, so bit 5 means Cloth_Body on the clothing page AND Weap_Utils3 on the weapon page. Both readings are printed per row.

Item stat 88 "DefaultPos" holds the default slot's low nibble and confirms the clothing reading on every Shade piece sampled: Chosen Shade Headgear DefaultPos 2 = Cloth_Head 0x12; Chosen Shade Foot Covers 14 = Cloth_Feet 0x1E; Chosen Shade Support System 3 = Cloth_Back 0x13; Ofab Shade Body Armor 5 = Cloth_Body 0x15; Necklace of Alacrity 1 = Cloth_Neck 0x11; Shades' Ring of Shadows 13 = Cloth_RightFinger 0x1D.

**Trap.** tools/eng-gear-extractor prints BOTH decodings for every item, which makes Spirits (ItemClass 5, whose mask is on the IMPLANT page) look like clothing - e.g. "Bitter Spirit of Feet Defense" appears as RightFinger/Deck5. This file excludes ItemClass 3 and 5 entirely for that reason.

## Where the Shade breaks the class-profile template

- THE IMPLANT SLOTS ARE NOT IN THIS FILE. For every other class the gear picture and the implant picture partly overlap (symbiants vs clusters in the same 13 slots). A Shade's 13 implant slots hold Spirits, which are a completely separate item class (ItemClass 5) with their own catalogue file. Gear here means clothing-page armour, jewellery, HUD, utility, deck and belt only.
- THE ARMOUR VOCABULARY IS DIFFERENT. Searching a Shade gear list for "armor", "helmet" or "boots" misses most of it. The Shade-locked pieces are called Body Wraps, Arm Tattoos, Shoulder Tattoos, Hand Wraps, Leg Covers, Foot Covers, Headgear and Support System. Any generic armour-set matcher written for the other classes will return almost nothing for this one.
- THREE OFAB GRADES, NOT ONE. Other professions have a single Ofab set. The client data holds Ofab Shade, Improved Ofab Shade and Penultimate Ofab Shade as three separate six-piece Shade-locked sets, plus two QL300-only extras (OFAB Shade Protective Gear id 267934 back, OFAB Shade Shoulder Wear id 268005), a Special Edition Ofab Shade Headgear (ids 267377/267378), and four "Replica Armor Set" containers (ids 289049, 289063, 289078, 289092).
- THE TIER LINE IS SIDE-LOCKED AND NEUTRALS ARE CUT OFF. Chosen (Omni) and Faithful (Clan) are the same eight pieces with different names. ao-universe states neutral Shades cannot wear either and stop at Second Tier - a faction constraint no other class profile in this project has had to record.
- TATTOOS ARE SHARED WITH THE META-PHYSICIST, NOT WITH THE MELEE CLASSES. Ectoplasm / Entophagous / Sombreous are locked to {Metaphysicist, Shade} in items.ocp - so the Shade's nearest gear neighbour is a pet caster, not another melee profession.
- THE SLOT BITMASK IS AMBIGUOUS AND THE GEAR EXTRACTOR PRINTS BOTH READINGS. See _slotDecoding. This bit the previous pass on this class: Spirits showed up in the gear dump as rings and deck items.

## Deliberately unverified

- Drop locations for individual non-set pieces are mostly unknown. The client data has no drop table, and auno.org / aoitems.com / anarchyonline.fandom.com block automation, so only what ao-universe and wiki.aodb.us actually state is recorded as a source.
- The shadeScore ranking is a stated formula invented for this file to order a 6,158-template pool. It is a sorting aid, NOT a best-in-slot claim, and it ignores set bonuses, stacking, NCU cost and faction/side locks.
- The Infantry and Outsider's sets are included because their items.ocp ToWear lock includes Shade; no fetched guide recommends them for a Shade.
- Which exact QL step of the tier line a given character should be on, and the exact glyph items used to upgrade Chosen/Faithful in Pandemonium, were not resolved to item ids.
- The clothing-page reading of the slot bitmask is confirmed by stat 88 for the sampled Shade pieces but was not verified item-by-item for all 6,158 ItemClass-2 templates.

## Sources

- LOCAL E:/Funcom/OmniCell/OmniCell/Datafiles/items.ocp (client 18.8.50_EP1) via tools/eng-gear-extractor --prof 15: 120,842 templates read, 56,349 wearable, 51,143 usable by profession 15; every id, name, QL, ToWear requirement, worn bonus, slot bitmask, profession lock and NODROP flag in this file.
- LOCAL E:/Funcom/attic/extracted-client-data/itemnames.sql: id -> name verification for every row.
- LOCAL E:/Funcom/AOBuddy10/AOSharp.Common/GameData/EquipSlot.cs: the slot-bit decoding in _slotDecoding.
- LOCAL items.ocp stat 88 "DefaultPos" on the sampled Shade pieces: the clothing-page cross-check.
- https://www.ao-universe.com/guides/shadowlands/professions-guides/comprehensive-shade-guide
- http://wiki.aodb.us/wiki/Shade:Armor
- http://wiki.aodb.us/wiki/Shade
