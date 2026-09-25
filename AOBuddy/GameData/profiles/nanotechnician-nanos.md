# Nano-Technician (profession 11) — Castable Nano Reference

Generated 2026-09-23T06:46:29Z for the AOBuddy10 bot. **392 nanos.**

## Method / provenance

- **Nano data (ground truth):** `E:\Funcom\OmniCell\OmniCell\Datafiles\nanos.ocp` — OmniCell OMNICELL-CONTENT v3 pack (client 18.8.50_EP1 extraction), loaded through `OmniCell.Core` `NanoLoader.CacheAllNanos` (net10 DLL). 10965 nano formulas in the pack.
- **Names:** joined by nano id against `itemnames.sql` (`itemnames` table).
- **Enums:** stat ids and nano-line names from `AOSharp.Common/GameData/Stat.cs` and `NanoEnums.cs`.
- **Extractor source:** `E:\Funcom\AOBuddy10\tools\mp-nano-extractor` (re-runnable).
- **No web data was used for any id, name or level.**

## NT-castability criterion

A nano is included when one of its cast `Actions` (`ActionType.ToUse` = 3) has an `EqualTo` requirement on **`Profession`(stat 60) == 11** or **`VisualProfession`(stat 368) == 11**. VisualProfession is how most older profession nanos are locked. Split: 117 via Profession(60), 275 via VisualProfession(368). Every match uses the EqualTo operator.

## Category counts

| Category | Count |
| --- | ---: |
| Self / Team Buffs | 41 |
| Nukes / Damage | 195 |
| Debuffs | 20 |
| Mezz / Calm | 16 |
| Heals / HoT | 8 |
| Travel / Summon Utility | 2 |
| Misc / Utility | 42 |
| Detaunt | 1 |
| Evade Buffs | 2 |
| Health Buffs | 1 |
| Reflect / Absorb Shields | 12 |
| Root | 16 |
| Taunt / Aggro | 36 |
| **Total** | **392** |

## Self / Team Buffs (41)

| id | name | nano line (strain) | minLvl | cast reqs | effect |
| ---: | --- | --- | ---: | --- | --- |
| 201521 | Enfraam's Toolkit | NanoProgrammingBuff (210) | 75 | Profession == 11; PsychologicalModification > 493; SensoryImprovement > 493; Level > 74 | Modify NanoProgramming +75 |
| 220347 | Enfraam's Cortex Accelerator | Psy_IntBuff (576) | 145 | BiologicalMetamorphosis > 969; PsychologicalModification > 969; Profession == 11; Level > 144; Specialization op22 2; Expansion op22 2 | Modify Intelligence +48; Modify Psychic +48; Modify MaterialCreation +4 |
| 201937 | Lick of the Pest | NOSTACKING (0) | 185 | Profession == 11; Level > 184; MaterialCreation > 1025; BiologicalMetamorphosis > 1025; MaterialMetamorphosis > 1025 | Modify ChemicalAC -5000 |
| 220349 | Izgimmer's Hippocampal Augmentor | Psy_IntBuff (576) | 195 | BiologicalMetamorphosis > 1240; PsychologicalModification > 1240; Profession == 11; Level > 194; Expansion op22 2; Specialization op22 4 | Modify Intelligence +83; Modify Psychic +83; Modify MaterialCreation +15 |
| 302274 | Izgimmer's Blessing | MatCreaBuff (159) | 201 | PsychologicalModification > 964; SensoryImprovement > 964; Profession == 11; Level > 200 | Modify MaterialCreation +180 |
| 117218 | Assume Profession: Nanotechnician | FalseProfession (218) | - | VisualProfession == 5; VisualProfession == 11; Profession == 5; PsychologicalModification > 558; SensoryImprovement > 558; BiologicalMetamorphosis > 558 | RemoveNanoStrain[680]; Skill[318,70]; ModifyPercentage PsychologicalModification -15; ModifyPercentage SensoryImprovement -15; ModifyPercentage MaterialMetamorphosis -15; ModifyPercentage MaterialCreation -15; ModifyPercentage SpaceTime -15; ModifyPercentage BiologicalMetamorphosis -15; ChangeVariable[368,11] |
| 95444 | Coherent Nano Pathway | NFRangeBuff (181) | - | SpaceTime > 202; MaterialMetamorphosis > 202; SensoryImprovement > 202; VisualProfession == 11 | Skill[381,50] |
| 95445 | Cohesion Amplifier | NFRangeBuff (181) | - | SpaceTime > 303; MaterialMetamorphosis > 303; SensoryImprovement > 303; VisualProfession == 11 | Skill[381,57] |
| 95407 | CrunchCom Code Sieve | NPCostBuff (148) | - | SensoryImprovement > 63; MaterialMetamorphosis > 63; PsychologicalModification > 63; VisualProfession == 11 | Skill[318,-6]; Skill[381,5] |
| 95415 | CrunchCom Nano Compressor | NPCostBuff (148) | - | SensoryImprovement > 232; MaterialMetamorphosis > 232; PsychologicalModification > 232; VisualProfession == 11 | Skill[318,-11]; Skill[381,13] |
| 95412 | CrunchCom Nano Compressor Pro | NPCostBuff (148) | - | SensoryImprovement > 566; MaterialMetamorphosis > 566; PsychologicalModification > 566; VisualProfession == 11 | Skill[318,-18]; Skill[381,20] |
| 150622 | Enfraam's Augmented Fortification | Fortify (224) | - | MaterialCreation > 714; MaterialMetamorphosis > 714; BiologicalMetamorphosis > 714; VisualProfession == 11 | Modify PoisonAC +200; Modify ColdAC +200; Modify FireAC +200; Modify ChemicalAC +200; Modify RadiationAC +200; Modify ProjectileAC +200; Modify EnergyAC +200; Modify MeleeAC +200; Modify MaxHealth +281; Modify NanoResist +122 |
| 150628 | Enfraam's Fortification | Fortify (224) | - | MaterialCreation > 219; MaterialMetamorphosis > 219; BiologicalMetamorphosis > 219; VisualProfession == 11 | Modify PoisonAC +61; Modify ColdAC +61; Modify FireAC +61; Modify ChemicalAC +61; Modify RadiationAC +61; Modify ProjectileAC +61; Modify EnergyAC +61; Modify MeleeAC +61; Modify MaxHealth +92; Modify NanoResist +51 |
| 150623 | Enfraam's Glorious Fortification | Fortify (224) | - | MaterialCreation > 638; MaterialMetamorphosis > 638; BiologicalMetamorphosis > 638; VisualProfession == 11 | Modify PoisonAC +167; Modify ColdAC +167; Modify FireAC +167; Modify ChemicalAC +167; Modify RadiationAC +167; Modify ProjectileAC +167; Modify EnergyAC +167; Modify MeleeAC +167; Modify MaxHealth +246; Modify NanoResist +111 |
| 150625 | Enfraam's Greater Fortification | Fortify (224) | - | MaterialCreation > 430; MaterialMetamorphosis > 430; BiologicalMetamorphosis > 430; VisualProfession == 11 | Modify PoisonAC +116; Modify ColdAC +116; Modify FireAC +116; Modify ChemicalAC +116; Modify RadiationAC +116; Modify ProjectileAC +116; Modify EnergyAC +116; Modify MeleeAC +116; Modify MaxHealth +177; Modify NanoResist +85 |
| 150629 | Enfraam's Lesser Fortification | Fortify (224) | - | MaterialCreation > 178; MaterialMetamorphosis > 178; BiologicalMetamorphosis > 178; VisualProfession == 11 | Modify PoisonAC +47; Modify ColdAC +47; Modify FireAC +47; Modify ChemicalAC +47; Modify RadiationAC +47; Modify ProjectileAC +47; Modify EnergyAC +47; Modify MeleeAC +47; Modify MaxHealth +75; Modify NanoResist +44 |
| 150626 | Enfraam's Major Fortification | Fortify (224) | - | MaterialCreation > 370; MaterialMetamorphosis > 370; BiologicalMetamorphosis > 370; VisualProfession == 11 | Modify PoisonAC +96; Modify ColdAC +96; Modify FireAC +96; Modify ChemicalAC +96; Modify RadiationAC +96; Modify ProjectileAC +96; Modify EnergyAC +96; Modify MeleeAC +96; Modify MaxHealth +150; Modify NanoResist +74 |
| 150630 | Enfraam's Minor Fortification | Fortify (224) | - | MaterialCreation > 122; MaterialMetamorphosis > 122; BiologicalMetamorphosis > 122; VisualProfession == 11 | Modify PoisonAC +31; Modify ColdAC +31; Modify FireAC +31; Modify ChemicalAC +31; Modify RadiationAC +31; Modify ProjectileAC +31; Modify EnergyAC +31; Modify MeleeAC +31; Modify MaxHealth +50; Modify NanoResist +32 |
| 150621 | Enfraam's Perfected Fortification | Fortify (224) | - | MaterialCreation > 762; MaterialMetamorphosis > 762; BiologicalMetamorphosis > 762; VisualProfession == 11 | Modify PoisonAC +230; Modify ColdAC +230; Modify FireAC +230; Modify ChemicalAC +230; Modify RadiationAC +230; Modify ProjectileAC +230; Modify EnergyAC +230; Modify MeleeAC +230; Modify MaxHealth +307; Modify NanoResist +129 |
| 150627 | Enfraam's Superior Fortification | Fortify (224) | - | MaterialCreation > 290; MaterialMetamorphosis > 290; BiologicalMetamorphosis > 290; VisualProfession == 11 | Modify PoisonAC +79; Modify ColdAC +79; Modify FireAC +79; Modify ChemicalAC +79; Modify RadiationAC +79; Modify ProjectileAC +79; Modify EnergyAC +79; Modify MeleeAC +79; Modify MaxHealth +120; Modify NanoResist +62 |
| 150624 | Enfraam's Supreme Fortification | Fortify (224) | - | MaterialCreation > 518; MaterialMetamorphosis > 518; BiologicalMetamorphosis > 518; VisualProfession == 11 | Modify PoisonAC +138; Modify ColdAC +138; Modify FireAC +138; Modify ChemicalAC +138; Modify RadiationAC +138; Modify ProjectileAC +138; Modify EnergyAC +138; Modify MeleeAC +138; Modify MaxHealth +207; Modify NanoResist +96 |
| 150620 | Enfraam's Trivial Fortification | Fortify (224) | - | MaterialCreation > 68; MaterialMetamorphosis > 68; BiologicalMetamorphosis > 68; VisualProfession == 11 | Modify PoisonAC +17; Modify ColdAC +17; Modify FireAC +17; Modify ChemicalAC +17; Modify RadiationAC +17; Modify ProjectileAC +17; Modify EnergyAC +17; Modify MeleeAC +17; Modify MaxHealth +27; Modify NanoResist +18 |
| 95447 | Enhance Nano Cohesion | NFRangeBuff (181) | - | SpaceTime > 108; MaterialMetamorphosis > 108; SensoryImprovement > 108; VisualProfession == 11 | Skill[381,42] |
| 95448 | Enhance Nano Communication | NFRangeBuff (181) | - | SpaceTime > 29; MaterialMetamorphosis > 29; SensoryImprovement > 29; VisualProfession == 11 | Skill[381,36] |
| 32037 | False Profession: Nanotechnician | FalseProfession (218) | - | VisualProfession == 5; VisualProfession == 11; Profession == 5; PsychologicalModification > 191; SensoryImprovement > 191; BiologicalMetamorphosis > 191 | RemoveNanoStrain[680]; Skill[318,150]; ModifyPercentage PsychologicalModification -25; ModifyPercentage SensoryImprovement -25; ModifyPercentage MaterialMetamorphosis -25; ModifyPercentage MaterialCreation -25; ModifyPercentage SpaceTime -25; ModifyPercentage BiologicalMetamorphosis -25; ChangeVariable[368,11] |
| 150631 | Izgimmer's Mockery | Fortify (224) | - | MaterialCreation > 841; MaterialMetamorphosis > 841; BiologicalMetamorphosis > 841; VisualProfession == 11 | Modify FireAC +331; Modify ChemicalAC +331; Modify RadiationAC +331; Modify ProjectileAC +331; Modify EnergyAC +331; Modify MeleeAC +331; Modify PoisonAC +331; Modify ColdAC +331; Modify MaxHealth +350; Modify NanoResist +140 |
| 95417 | Izgimmer's Obfuscated Recompiler | NPCostBuff (148) | - | SensoryImprovement > 826; MaterialMetamorphosis > 826; PsychologicalModification > 826; VisualProfession == 11 | Skill[318,-28] |
| 95413 | Jobe Nano Libraries | NPCostBuff (148) | - | SensoryImprovement > 707; MaterialMetamorphosis > 707; PsychologicalModification > 707; VisualProfession == 11 | Skill[318,-22] |
| 29294 | MatCrea Mastery | MatCreaBuff (159) | - | VisualProfession == 12; VisualProfession == 11; PsychologicalModification > 207; SensoryImprovement > 207 | Modify MaterialCreation +50 |
| 117207 | Mimic Profession: Nanotechnician | FalseProfession (218) | - | VisualProfession == 5; VisualProfession == 11; Profession == 5; PsychologicalModification > 776; SensoryImprovement > 776; BiologicalMetamorphosis > 776 | RemoveNanoStrain[680]; Skill[318,15]; ModifyPercentage PsychologicalModification -5; ModifyPercentage SensoryImprovement -5; ModifyPercentage MaterialMetamorphosis -5; ModifyPercentage MaterialCreation -5; ModifyPercentage SpaceTime -5; ModifyPercentage BiologicalMetamorphosis -5; ChangeVariable[368,11] |
| 95442 | Nano Cloud Supplement | NFRangeBuff (181) | - | SpaceTime > 697; MaterialMetamorphosis > 697; SensoryImprovement > 697; VisualProfession == 11 | Skill[318,2]; Skill[381,86] |
| 220345 | Neuronal Stimulator | Psy_IntBuff (576) | - | Profession == 11; Profession == 12; Profession == 8; BiologicalMetamorphosis > 193; PsychologicalModification > 193; Specialization op22 1; Expansion op22 2; Expansion op22 2 | Modify Intelligence +23; Modify Psychic +23 |
| 263266 | Notum Overflow Injector | NanoDamageMultiplierBuffs (1062) | - | Expansion op22 2; MaterialMetamorphosis > 597; MaterialCreation > 597; Profession == 11 | Modify NanoDamageMultiplier +7 |
| 95443 | Notum Overload | NFRangeBuff (181) | - | SpaceTime > 815; MaterialMetamorphosis > 815; SensoryImprovement > 815; VisualProfession == 11 | Skill[318,4]; Skill[381,104] |
| 150501 | Nullity Sphere | NullitySphereNano (824) | - | MaterialMetamorphosis > 417; MaterialCreation > 417; VisualProfession == 11; Flags op101 266315 | Modify ReflectProjectileAC +100; Modify ReflectMeleeAC +100; Modify ReflectEnergyAC +100; Modify ReflectChemicalAC +100; Modify ReflectRadiationAC +100; Modify ReflectColdAC +100; Modify ReflectFireAC +100; Modify ReflectPoisonAC +100; Modify RunSpeed -2500; Modify Concealment -2000; CastNano[266314] |
| 150502 | Nullity Sphere MK II | NullitySphereNano (824) | - | MaterialMetamorphosis > 741; MaterialCreation > 741; VisualProfession == 11; Flags op101 266315 | Modify ReflectProjectileAC +100; Modify ReflectMeleeAC +100; Modify ReflectEnergyAC +100; Modify ReflectChemicalAC +100; Modify ReflectRadiationAC +100; Modify ReflectColdAC +100; Modify ReflectFireAC +100; Modify ReflectPoisonAC +100; Modify RunSpeed -2500; Modify Concealment -2000; CastNano[266314] |
| 95414 | On-The-Fly Compression | NPCostBuff (148) | - | SensoryImprovement > 349; MaterialMetamorphosis > 349; PsychologicalModification > 349; VisualProfession == 11 | Skill[318,-13] |
| 95416 | Run-Time Recompiler | NPCostBuff (148) | - | SensoryImprovement > 162; MaterialMetamorphosis > 162; PsychologicalModification > 162; VisualProfession == 11 | Skill[318,-9] |
| 95446 | Superior Nano Command | NFRangeBuff (181) | - | SpaceTime > 439; MaterialMetamorphosis > 439; SensoryImprovement > 439; VisualProfession == 11 | Skill[381,67] |
| 260758 | Superior Notum Overflow Injector | NanoDamageMultiplierBuffs (1062) | - | Expansion op22 2; MaterialMetamorphosis > 1499; MaterialCreation > 1499; Profession == 11 | Modify NanoDamageMultiplier +10 |
| 151765 | Teachings of Material Creation | MatCreaBuff (159) | - | VisualProfession == 12; VisualProfession == 11; PsychologicalModification > 107; SensoryImprovement > 107 | Modify MaterialCreation +25 |

## Nukes / Damage (195)

| id | name | nano line (strain) | minLvl | cast reqs | effect |
| ---: | --- | --- | ---: | --- | --- |
| 218096 | Caring Needle | NOSTACKING (0) | 50 | Profession == 11; Level > 49; MaterialCreation > 400; Expansion op22 2; Specialization op22 1; NanoFocusLevel op22 1 | Hit[27,-409,-748,96]; Hit[27,-7,-7,92]; Hit[27,-11,-11,96]; Hit[27,-15,-15,95]; Hit[27,-23,-23,97]; Hit[27,-30,-30,94] |
| 218094 | Chilled Touch | NOSTACKING (0) | 50 | Profession == 11; Level > 49; MaterialCreation > 359; Expansion op22 2; Specialization op22 1; NanoFocusLevel op22 1 | Hit[27,-345,-632,97]; Hit[27,-5,-5,92]; Hit[27,-10,-10,96]; Hit[27,-13,-13,95]; Hit[27,-19,-19,97]; Hit[27,-25,-25,94] |
| 218092 | Spark Shower | NOSTACKING (0) | 50 | Profession == 11; Level > 49; MaterialCreation > 319; Expansion op22 2; Specialization op22 1; NanoFocusLevel op22 1 | Hit[27,-277,-527,92]; Hit[27,-4,-4,92]; Hit[27,-7,-7,96]; Hit[27,-10,-10,95]; Hit[27,-16,-16,97]; Hit[27,-21,-21,94] |
| 218102 | Ariu's Neutron Annihilator | NOSTACKING (0) | 75 | Profession == 11; Level > 74; MaterialCreation > 643; Expansion op22 2; Specialization op22 1; NanoFocusLevel op22 1 | Hit[27,-791,-1549,94]; Hit[27,-13,-13,92]; Hit[27,-21,-21,96]; Hit[27,-30,-30,95]; Hit[27,-45,-45,97]; Hit[27,-61,-61,94] |
| 218098 | Astinus's Stellar Pulse | NOSTACKING (0) | 75 | Profession == 11; Level > 74; MaterialCreation > 514; Expansion op22 2; Specialization op22 1; NanoFocusLevel op22 1 | Hit[27,-583,-1077,94]; Hit[27,-9,-9,92]; Hit[27,-15,-15,96]; Hit[27,-22,-22,95]; Hit[27,-32,-32,97]; Hit[27,-43,-43,94] |
| 218110 | Conator's Collapsing Hadron String | NOSTACKING (0) | 75 | Profession == 11; Level > 74; MaterialCreation > 625; Expansion op22 2; Specialization op22 1; NanoFocusLevel op22 2 | Hit[27,-774,-1379,94]; Hit[27,-12,-12,92]; Hit[27,-20,-20,96]; Hit[27,-28,-28,95]; Hit[27,-41,-41,97]; Hit[27,-55,-55,94] |
| 266286 | Explosive Double | DOTNanotechnicianStrainB (10) | 75 | Expansion op22 32; Profession == 11; MaterialCreation > 562; Level > 74; NanoFocusLevel op22 1; Flags op101 266314 | CastNano[266275]; Hit[27,-375,-630,94]; CastNano[266315] |
| 266292 | Future Vision | DOTNanotechnicianStrainB (10) | 75 | Expansion op22 32; Profession == 11; MaterialCreation > 562; Level > 74; NanoFocusLevel op22 1; Flags op101 266314 | CastNano[266275]; Hit[27,-645,-1042,94]; CastNano[266315] |
| 218108 | Positronic Fluctuation | NOSTACKING (0) | 75 | Profession == 11; Level > 74; MaterialCreation > 564; Expansion op22 2; Specialization op22 1; NanoFocusLevel op22 2 | Hit[27,-632,-1216,96]; Hit[27,-10,-10,92]; Hit[27,-16,-16,96]; Hit[27,-23,-23,95]; Hit[27,-35,-35,97]; Hit[27,-48,-48,94] |
| 218106 | Vital Corruptor | NOSTACKING (0) | 75 | Profession == 11; Level > 74; MaterialCreation > 514; Expansion op22 2; Specialization op22 1; NanoFocusLevel op22 2 | Hit[27,-534,-1014,96]; Hit[27,-9,-9,92]; Hit[27,-14,-14,96]; Hit[27,-20,-20,95]; Hit[27,-30,-30,97]; Hit[27,-40,-40,94] |
| 218100 | Ziana's Energy Wave | NOSTACKING (0) | 75 | Profession == 11; Level > 74; MaterialCreation > 558; Expansion op22 2; Specialization op22 1; NanoFocusLevel op22 1 | Hit[27,-649,-1170,92]; Hit[27,-10,-10,92]; Hit[27,-16,-16,96]; Hit[27,-23,-23,95]; Hit[27,-35,-35,97]; Hit[27,-47,-47,94] |
| 218104 | Blood of Hephaestos | NOSTACKING (0) | 100 | Profession == 11; Level > 99; MaterialCreation > 687; Expansion op22 2; Specialization op22 1; NanoFocusLevel op22 1 | Hit[27,-877,-1699,97]; Hit[27,-15,-15,92]; Hit[27,-24,-24,96]; Hit[27,-34,-34,95]; Hit[27,-50,-50,97]; Hit[27,-66,-66,94] |
| 218112 | Combustive Envelopment | NOSTACKING (0) | 100 | Profession == 11; Level > 99; MaterialCreation > 701; Expansion op22 2; Specialization op22 2; NanoFocusLevel op22 2 | Hit[27,-891,-1590,97]; Hit[27,-14,-14,92]; Hit[27,-22,-22,96]; Hit[27,-32,-32,95]; Hit[27,-47,-47,97]; Hit[27,-64,-64,94] |
| 218114 | Searing Dioxin Shower | NOSTACKING (0) | 100 | Profession == 11; Level > 99; MaterialCreation > 773; Expansion op22 2; Specialization op22 2; NanoFocusLevel op22 2 | Hit[27,-1033,-1660,93]; Hit[27,-15,-15,92]; Hit[27,-25,-25,96]; Hit[27,-35,-35,95]; Hit[27,-52,-52,97]; Hit[27,-70,-70,94] |
| 218116 | The Spider's Secret | NOSTACKING (0) | 100 | Profession == 11; Level > 99; MaterialCreation > 845; Expansion op22 2; Specialization op22 2; NanoFocusLevel op22 2 | Hit[27,-1171,-2089,96]; Hit[27,-18,-18,92]; Hit[27,-30,-30,96]; Hit[27,-42,-42,95]; Hit[27,-63,-63,97]; Hit[27,-84,-84,94] |
| 266291 | Calculated Detonation | DOTNanotechnicianStrainB (10) | 120 | Expansion op22 32; Profession == 11; MaterialCreation > 824; Level > 119; NanoFocusLevel op22 2; Flags op101 266314 | CastNano[266275]; Hit[27,-1018,-1430,94]; CastNano[266315] |
| 266285 | Earthshattering Double | DOTNanotechnicianStrainB (10) | 120 | Expansion op22 32; Profession == 11; MaterialCreation > 824; Level > 119; NanoFocusLevel op22 2; Flags op101 266314 | CastNano[266275]; Hit[27,-775,-1290,94]; CastNano[266315] |
| 218118 | Acidic Droplets | NOSTACKING (0) | 125 | Profession == 11; Level > 124; MaterialCreation > 900; Expansion op22 2; Specialization op22 2; NanoFocusLevel op22 2 | Hit[27,-1311,-2342,93]; Hit[27,-21,-21,92]; Hit[27,-34,-34,96]; Hit[27,-47,-47,95]; Hit[27,-71,-71,97]; Hit[27,-94,-94,94] |
| 218126 | Enfraam's Blistering Blast | NOSTACKING (0) | 125 | Profession == 11; Level > 124; MaterialCreation > 885; Expansion op22 2; Specialization op22 2; NanoFocusLevel op22 4 | Hit[27,-1194,-2232,93]; Hit[27,-20,-20,92]; Hit[27,-32,-32,96]; Hit[27,-44,-44,95]; Hit[27,-66,-66,97]; Hit[27,-89,-89,94] |
| 218128 | Pestilential Stream | NOSTACKING (0) | 135 | Profession == 11; Level > 134; MaterialCreation > 927; Expansion op22 2; Specialization op22 2; NanoFocusLevel op22 2 | Hit[27,-1343,-2745,96]; Hit[27,-23,-23,92]; Hit[27,-38,-38,96]; Hit[27,-53,-53,95]; Hit[27,-79,-79,97]; Hit[27,-106,-106,94] |
| 218120 | Slithering Flames | NOSTACKING (0) | 135 | Profession == 11; Level > 134; MaterialCreation > 930; Expansion op22 2; Specialization op22 2; NanoFocusLevel op22 2 | Hit[27,-1459,-2602,97]; Hit[27,-23,-23,92]; Hit[27,-37,-37,96]; Hit[27,-52,-52,95]; Hit[27,-79,-79,97]; Hit[27,-105,-105,94] |
| 218122 | Chilling Presence | NOSTACKING (0) | 145 | Profession == 11; Level > 144; MaterialCreation > 959; Expansion op22 2; Specialization op22 2; NanoFocusLevel op22 2 | Hit[27,-1602,-2861,95]; Hit[27,-25,-25,92]; Hit[27,-41,-41,96]; Hit[27,-57,-57,95]; Hit[27,-86,-86,97]; Hit[27,-115,-115,94] |
| 218130 | Enfraam's Glacial Encasement | NOSTACKING (0) | 145 | Profession == 11; Level > 144; MaterialCreation > 965; Expansion op22 2; Specialization op22 2; NanoFocusLevel op22 4 | Hit[27,-1503,-3136,95]; Hit[27,-23,-23,92]; Hit[27,-38,-38,96]; Hit[27,-53,-53,95]; Hit[27,-79,-79,97]; Hit[27,-106,-106,94] |
| 218132 | Biomolecular Corrosion | NOSTACKING (0) | 155 | Profession == 11; Level > 154; MaterialCreation > 1002; Expansion op22 2; Specialization op22 4; NanoFocusLevel op22 4 | Hit[27,-1802,-3765,93]; Hit[27,-32,-32,92]; Hit[27,-52,-52,96]; Hit[27,-72,-72,95]; Hit[27,-108,-108,97]; Hit[27,-144,-144,94] |
| 218124 | Entropy's Advance | NOSTACKING (0) | 155 | Profession == 11; Level > 154; MaterialCreation > 985; Expansion op22 2; Specialization op22 2; NanoFocusLevel op22 2 | Hit[27,-1804,-3234,95]; Hit[27,-29,-29,92]; Hit[27,-47,-47,96]; Hit[27,-65,-65,95]; Hit[27,-98,-98,97]; Hit[27,-130,-130,94] |
| 266290 | Delayed Cellular Collapse | DOTNanotechnicianStrainB (10) | 165 | Expansion op22 32; Profession == 11; MaterialCreation > 1114; Level > 164; NanoFocusLevel op22 2; Flags op101 266314 | CastNano[266275]; Hit[27,-1629,-2656,94]; CastNano[266315] |
| 266284 | Notum Double | DOTNanotechnicianStrainB (10) | 165 | Expansion op22 32; Profession == 11; MaterialCreation > 1114; Level > 164; NanoFocusLevel op22 2; Flags op101 266314 | CastNano[266275]; Hit[27,-1500,-2500,94]; CastNano[266315] |
| 218134 | Touch of the Pyre | NOSTACKING (0) | 165 | Profession == 11; Level > 164; MaterialCreation > 1053; Expansion op22 2; Specialization op22 4; NanoFocusLevel op22 4 | Hit[27,-2150,-4367,97]; Hit[27,-37,-37,92]; Hit[27,-60,-60,96]; Hit[27,-79,-79,95]; Hit[27,-126,-126,97]; Hit[27,-168,-168,94] |
| 201933 | Corruption of The Pest | NOSTACKING (0) | 185 | Profession == 11; Level > 184; MaterialCreation > 1017 | Hit[27,-2270,-5033,93] |
| 218136 | Enfraam's Ultimate Destroyer | NOSTACKING (0) | 185 | Profession == 11; Level > 184; MaterialCreation > 1102; Expansion op22 2; Specialization op22 4; NanoFocusLevel op22 4 | Hit[27,-2567,-4899,94]; Hit[27,-43,-43,92]; Hit[27,-69,-69,96]; Hit[27,-96,-96,95]; Hit[27,-144,-144,97]; Hit[27,-193,-193,94] |
| 202262 | Candycane | NOSTACKING (0) | 195 | Profession == 11; Level > 194; MaterialCreation > 1031 | Hit[27,-540,-540,91]; Hit[27,-540,-540,90]; Hit[27,-540,-540,92]; Hit[27,-540,-540,97]; Hit[27,-540,-540,95]; Hit[27,-540,-540,93]; Hit[27,-540,-540,96]; Hit[27,-540,-540,94] |
| 218138 | Implacability of the Second Law | NOSTACKING (0) | 195 | Profession == 11; Level > 194; MaterialCreation > 1140; Expansion op22 2; Specialization op22 4; NanoFocusLevel op22 4 | Hit[27,-2876,-5503,94]; Hit[27,-48,-48,92]; Hit[27,-78,-78,96]; Hit[27,-108,-108,95]; Hit[27,-162,-162,97]; Hit[27,-217,-217,94] |
| 266289 | Impending Demise | DOTNanotechnicianStrainB (10) | 200 | Expansion op22 32; Profession == 11; MaterialCreation > 1307; Level > 199; NanoFocusLevel op22 4; Flags op101 266314 | CastNano[266275]; Hit[27,-2940,-4200,94]; CastNano[266315] |
| 266283 | Jobe Double | DOTNanotechnicianStrainB (10) | 200 | Expansion op22 32; Profession == 11; MaterialCreation > 1307; Level > 199; NanoFocusLevel op22 4; Flags op101 266314 | CastNano[266275]; Hit[27,-2230,-3715,94]; CastNano[266315] |
| 218140 | Izgimmer's Frosty Welcome | NOSTACKING (0) | 201 | Profession == 11; Level > 200; MaterialCreation > 1195; Expansion op22 2; Specialization op22 8; NanoFocusLevel op22 8 | Hit[27,-3092,-5906,95]; Hit[27,-52,-52,92]; Hit[27,-84,-84,96]; Hit[27,-116,-116,95]; Hit[27,-174,-174,97]; Hit[27,-233,-233,94] |
| 218142 | Izgimmer's Celestial Fury | NOSTACKING (0) | 203 | Profession == 11; Level > 202; MaterialCreation > 1284; Expansion op22 2; Specialization op22 8; NanoFocusLevel op22 8 | Hit[27,-3193,-6002,95]; Hit[27,-53,-53,92]; Hit[27,-86,-86,96]; Hit[27,-119,-119,95]; Hit[27,-178,-178,97]; Hit[27,-238,-238,94] |
| 218144 | Izgimmer's Infinite Slicer | NOSTACKING (0) | 205 | Profession == 11; Level > 204; MaterialCreation > 1374; Expansion op22 2; Specialization op22 8; NanoFocusLevel op22 8 | Hit[27,-3277,-6116,91]; Hit[27,-54,-54,92]; Hit[27,-87,-87,96]; Hit[27,-121,-121,95]; Hit[27,-182,-182,97]; Hit[27,-243,-243,94] |
| 266288 | Enfraam's Surprise Assault | DOTNanotechnicianStrainB (10) | 207 | Expansion op22 32; Profession == 11; MaterialCreation > 1544; Level > 206; NanoFocusLevel op22 8; Flags op101 266314 | CastNano[266275]; Hit[27,-3750,-4850,94]; CastNano[266315] |
| 218146 | Izgimmer's Interstellar Chill | NOSTACKING (0) | 207 | Profession == 11; Level > 206; MaterialCreation > 1463; Expansion op22 2; Specialization op22 8; NanoFocusLevel op22 8 | Hit[27,-3351,-6241,95]; Hit[27,-55,-55,92]; Hit[27,-90,-90,96]; Hit[27,-124,-124,95]; Hit[27,-186,-186,97]; Hit[27,-248,-248,94] |
| 218148 | Izgimmer's Positronic Annihilation | NOSTACKING (0) | 209 | Profession == 11; Level > 208; MaterialCreation > 1553; Expansion op22 2; Specialization op22 8; NanoFocusLevel op22 8 | Hit[27,-3427,-6365,94]; Hit[27,-56,-56,92]; Hit[27,-91,-91,96]; Hit[27,-127,-127,95]; Hit[27,-190,-190,97]; Hit[27,-254,-254,94] |
| 218150 | Izgimmer's Defilement of Being | NOSTACKING (0) | 211 | Profession == 11; Level > 210; MaterialCreation > 1642; Expansion op22 2; Specialization op22 8; NanoFocusLevel op22 8 | Hit[27,-3519,-6524,96]; Hit[27,-57,-57,92]; Hit[27,-93,-93,96]; Hit[27,-130,-130,95]; Hit[27,-194,-194,97]; Hit[27,-260,-260,94] |
| 218152 | Izgimmer's Celestial Implosion | NOSTACKING (0) | 212 | Profession == 11; Level > 211; MaterialCreation > 1687; Expansion op22 2; Specialization op22 8; NanoFocusLevel op22 8 | Hit[27,-3643,-6677,94]; Hit[27,-59,-59,92]; Hit[27,-96,-96,96]; Hit[27,-133,-133,95]; Hit[27,-200,-200,97]; Hit[27,-267,-267,94] |
| 218154 | Izgimmer's Contagion | NOSTACKING (0) | 213 | Profession == 11; Level > 212; MaterialCreation > 1732; Expansion op22 2; Specialization op22 8; NanoFocusLevel op22 8 | Hit[27,-3725,-6877,96]; Hit[27,-61,-61,92]; Hit[27,-98,-98,96]; Hit[27,-137,-137,95]; Hit[27,-205,-205,97]; Hit[27,-274,-274,94] |
| 266282 | Enfraam's Double | DOTNanotechnicianStrainB (10) | 214 | Expansion op22 32; Profession == 11; MaterialCreation > 1944; Level > 213; NanoFocusLevel op22 8; Flags op101 266314 | CastNano[266275]; Hit[27,-2850,-4570,94]; CastNano[266315] |
| 218156 | Izgimmer's Quasar Flicker | NOSTACKING (0) | 214 | Profession == 11; Level > 213; MaterialCreation > 1776; Expansion op22 2; Specialization op22 8; NanoFocusLevel op22 8 | Hit[27,-3876,-7008,92]; Hit[27,-63,-63,92]; Hit[27,-101,-101,96]; Hit[27,-141,-141,95]; Hit[27,-211,-211,97]; Hit[27,-282,-282,94] |
| 218158 | Izgimmer's Corrosive Tear | NOSTACKING (0) | 215 | Profession == 11; Level > 214; MaterialCreation > 1821; Expansion op22 2; Specialization op22 8; NanoFocusLevel op22 8 | Hit[27,-3987,-7180,93]; Hit[27,-64,-64,92]; Hit[27,-104,-104,96]; Hit[27,-144,-144,95]; Hit[27,-217,-217,97]; Hit[27,-289,-289,94] |
| 218160 | Izgimmer's Inferno | NOSTACKING (0) | 216 | Profession == 11; Level > 215; MaterialCreation > 1866; Expansion op22 2; Specialization op22 8; NanoFocusLevel op22 8 | Hit[27,-4029,-7526,97]; Hit[27,-66,-66,92]; Hit[27,-108,-108,96]; Hit[27,-149,-149,95]; Hit[27,-224,-224,97]; Hit[27,-299,-299,94] |
| 218162 | Izgimmer's Malignant Declaration | NOSTACKING (0) | 217 | Profession == 11; Level > 216; MaterialCreation > 1911; Expansion op22 2; Specialization op22 8; NanoFocusLevel op22 8 | Hit[27,-4166,-7779,96]; Hit[27,-68,-68,92]; Hit[27,-111,-111,96]; Hit[27,-154,-154,95]; Hit[27,-232,-232,97]; Hit[27,-309,-309,94] |
| 218164 | Izgimmer's Gelid Caress | NOSTACKING (0) | 218 | Profession == 11; Level > 217; MaterialCreation > 1956; Expansion op22 2; Specialization op22 8; NanoFocusLevel op22 8 | Hit[27,-4308,-8028,95]; Hit[27,-71,-71,92]; Hit[27,-115,-115,96]; Hit[27,-160,-160,95]; Hit[27,-239,-239,97]; Hit[27,-319,-319,94] |
| 218166 | Izgimmer's Cataclysm | NOSTACKING (0) | 219 | Profession == 11; Level > 218; MaterialCreation > 2000; Expansion op22 2; Specialization op22 8; NanoFocusLevel op22 8 | Hit[27,-4491,-8265,94]; Hit[27,-73,-73,92]; Hit[27,-119,-119,96]; Hit[27,-165,-165,95]; Hit[27,-247,-247,97]; Hit[27,-330,-330,94] |
| 266281 | Izgimmer's Double | DOTNanotechnicianStrainB (10) | 220 | Expansion op22 32; Profession == 11; MaterialCreation > 2239; Level > 219; NanoFocusLevel op22 8; Flags op101 266314 | CastNano[266275]; Hit[27,-3000,-5500,94]; CastNano[266315] |
| 266287 | Izgimmer's Tactical Nuke | DOTNanotechnicianStrainB (10) | 220 | Expansion op22 32; Profession == 11; MaterialCreation > 2239; Level > 219; NanoFocusLevel op22 8; Flags op101 266314 | CastNano[266275]; Hit[27,-4500,-6500,94]; CastNano[266315] |
| 218168 | Izgimmer's Ultimatum | NOSTACKING (0) | 220 | Profession == 11; Level > 219; MaterialCreation > 2045; Expansion op22 2; Specialization op22 8; NanoFocusLevel op22 8 | Hit[27,-4826,-8950,94]; Hit[27,-79,-79,92]; Hit[27,-128,-128,96]; Hit[27,-178,-178,95]; Hit[27,-267,-267,97]; Hit[27,-357,-357,94] |
| 28639 | Abyssal Flames | NOSTACKING (0) | - | MaterialCreation > 162; VisualProfession == 11 | Hit[27,-93,-167,97] |
| 45255 | Accelerated Decay | NOSTACKING (0) | - | MaterialCreation > 113; VisualProfession == 11 | Hit[27,-58,-109,94] |
| 45256 | Accelerated Titanium Pellet | NOSTACKING (0) | - | MaterialCreation > 127; VisualProfession == 11 | Hit[27,-122,-237,90] |
| 45257 | Acidic Conversion | NOSTACKING (0) | - | MaterialCreation > 137; VisualProfession == 11 | Hit[27,-69,-144,93] |
| 42539 | Acidic Projection | NOSTACKING (0) | - | MaterialCreation > 40; VisualProfession == 11 | Hit[27,-24,-43,93] |
| 45137 | Aggressive Mutagen | NOSTACKING (0) | - | MaterialCreation > 147; VisualProfession == 11 | Hit[27,-75,-157,96] |
| 45259 | Arctic Welcome | NOSTACKING (0) | - | MaterialCreation > 343; VisualProfession == 11 | Hit[27,-234,-484,95] |
| 45938 | Augmented Energized Beam | NOSTACKING (0) | - | MaterialCreation > 245; VisualProfession == 11 | Hit[27,-86,-210,92] |
| 42542 | Bacterial Invasion | NOSTACKING (0) | - | MaterialCreation > 57; VisualProfession == 11 | Hit[27,-45,-61,96] |
| 45939 | Bane of the Living | NOSTACKING (0) | - | MaterialCreation > 531; VisualProfession == 11 | Hit[27,-284,-525,96] |
| 45941 | Basic Crystalizing Ray | NOSTACKING (0) | - | MaterialCreation > 343; VisualProfession == 11 | Hit[27,-140,-291,95] |
| 45261 | Bio-Acid Spray | NOSTACKING (0) | - | MaterialCreation > 560; VisualProfession == 11 | Hit[27,-456,-993,93] |
| 45942 | Blade Chaos | NOSTACKING (0) | - | MaterialCreation > 409; VisualProfession == 11 | Hit[27,-188,-371,91] |
| 28595 | Blight | DOTNanotechnicianStrainA (8) | - | MaterialCreation > 170; SpaceTime > 170; VisualProfession == 11 | Hit[27,-8,-8,96] |
| 70628 | Boil from Within | DOTNanotechnicianStrainA (8) | - | MaterialCreation > 589; SpaceTime > 589; VisualProfession == 11 | Hit[27,-58,-58,97] |
| 45935 | Burn From Within | NOSTACKING (0) | - | MaterialCreation > 430; VisualProfession == 11 | Hit[27,-212,-400,97] |
| 45244 | Burning Orb | NOSTACKING (0) | - | MaterialCreation > 264; VisualProfession == 11 | Hit[27,-178,-355,97] |
| 45245 | Burning Quartet | NOSTACKING (0) | - | MaterialCreation > 583; VisualProfession == 11 | Hit[27,-723,-812,97] |
| 45246 | Burning Triumvirate | NOSTACKING (0) | - | MaterialCreation > 434; VisualProfession == 11 | Hit[27,-318,-717,97] |
| 45139 | Cellular Decay | NOSTACKING (0) | - | MaterialCreation > 194; VisualProfession == 11 | Hit[27,-119,-231,96] |
| 254432 | Cellular Re-Structure | DOTNanotechnicianStrainA (8) | - | MaterialCreation > 1133; Profession == 11; SpaceTime > 1133; AlienLevel > 7 | Hit[27,-460,-480,92]; Hit[27,-460,-480,92]; Hit[27,-920,-960,92]; Hit[27,-1380,-1440,92]; Hit[27,-1840,-1920,92] |
| 28598 | Chaos Lights | NOSTACKING (0) | - | MaterialCreation > 638; VisualProfession == 11 | Hit[27,-314,-767,92] |
| 45248 | Chaotic Entropy | NOSTACKING (0) | - | MaterialCreation > 228; VisualProfession == 11 | Hit[27,-69,-373,92] |
| 45249 | Chemical Burn | NOSTACKING (0) | - | MaterialCreation > 122; VisualProfession == 11 | Hit[27,-87,-98,93] |
| 45250 | Chemical Liquefaction | NOSTACKING (0) | - | MaterialCreation > 479; VisualProfession == 11 | Hit[27,-383,-811,93] |
| 45936 | Chilling Stream | NOSTACKING (0) | - | MaterialCreation > 170; VisualProfession == 11 | Hit[27,-44,-133,95] |
| 45251 | Coherent Positron Stream | NOSTACKING (0) | - | MaterialCreation > 662; VisualProfession == 11 | Hit[27,-577,-1411,92] |
| 45252 | Collapsing Hadron String | NOSTACKING (0) | - | MaterialCreation > 686; VisualProfession == 11 | Hit[27,-574,-1387,94] |
| 45253 | Compressed Shockwave | NOSTACKING (0) | - | MaterialCreation > 329; VisualProfession == 11 | Hit[27,-447,-1021,91] |
| 45931 | Condensed Halon Jet | NOSTACKING (0) | - | MaterialCreation > 157; VisualProfession == 11 | Hit[27,-45,-101,95] |
| 45932 | Condensed Pellet of Fire | NOSTACKING (0) | - | MaterialCreation > 466; VisualProfession == 11 | Hit[27,-195,-527,97] |
| 45933 | Convergent Energy Beam | NOSTACKING (0) | - | MaterialCreation > 363; VisualProfession == 11 | Hit[27,-151,-308,92] |
| 45238 | Coronet of Frost | NOSTACKING (0) | - | MaterialCreation > 615; VisualProfession == 11 | Hit[27,-503,-1142,95]; CastNano[144629] |
| 28601 | Corrosive Spray | NOSTACKING (0) | - | MaterialCreation > 57; VisualProfession == 11 | Hit[27,-17,-30,93] |
| 45138 | Corrupt Molecular Integrity | NOSTACKING (0) | - | MaterialCreation > 473; VisualProfession == 11 | Hit[27,-378,-798,96] |
| 28602 | Corruption | DOTNanotechnicianStrainA (8) | - | MaterialCreation > 35; SpaceTime > 35; VisualProfession == 11 | Hit[27,-3,-3,96] |
| 45934 | Crystalizing Ray | NOSTACKING (0) | - | MaterialCreation > 479; VisualProfession == 11 | Hit[27,-256,-463,95] |
| 45240 | Deep Chemical Burn | NOSTACKING (0) | - | MaterialCreation > 356; VisualProfession == 11 | Hit[27,-241,-508,93] |
| 42541 | Deep Slash | NOSTACKING (0) | - | MaterialCreation > 83; VisualProfession == 11 | Hit[27,-44,-69,91] |
| 45923 | Dense Matter Missile | NOSTACKING (0) | - | MaterialCreation > 215; VisualProfession == 11 | Hit[27,-70,-184,90] |
| 45924 | Dense Matter Missile MK II | NOSTACKING (0) | - | MaterialCreation > 405; VisualProfession == 11 | Hit[27,-155,-415,90] |
| 45241 | Dispersed Nanoblade Cloud | NOSTACKING (0) | - | MaterialCreation > 493; VisualProfession == 11 | Hit[27,-394,-836,91] |
| 45926 | Dissolving Sphere | NOSTACKING (0) | - | MaterialCreation > 505; VisualProfession == 11 | Hit[27,-273,-490,93] |
| 45927 | Dual Energized Beams | NOSTACKING (0) | - | MaterialCreation > 103; VisualProfession == 11 | Hit[27,-25,-63,92] |
| 45928 | Dual Ion Stream | NOSTACKING (0) | - | MaterialCreation > 290; VisualProfession == 11 | Hit[27,-103,-267,94] |
| 45929 | Encircle With Blades | NOSTACKING (0) | - | MaterialCreation > 662; VisualProfession == 11 | Hit[27,-333,-768,91] |
| 45930 | Energized Beam | NOSTACKING (0) | - | MaterialCreation > 29; VisualProfession == 11 | Hit[27,-6,-32,92] |
| 28605 | Energy Projectile | NOSTACKING (0) | - | MaterialCreation > 257; VisualProfession == 11 | Hit[27,-209,-448,92] |
| 45915 | Engulf in Flame | NOSTACKING (0) | - | MaterialCreation > 693; VisualProfession == 11 | Hit[27,-344,-881,97] |
| 45243 | Entropy Beam | NOSTACKING (0) | - | MaterialCreation > 157; VisualProfession == 11 | Hit[27,-50,-201,92] |
| 45916 | Eroding Spray | NOSTACKING (0) | - | MaterialCreation > 363; VisualProfession == 11 | Hit[27,-127,-350,93] |
| 28606 | Eye of Light | DOTNanotechnicianStrainA (8) | - | MaterialCreation > 782; VisualProfession == 11; SpaceTime > 782 | Hit[27,-145,-145,92] |
| 45918 | Feeble Blade Chaos | NOSTACKING (0) | - | MaterialCreation > 46; VisualProfession == 11 | Hit[27,-14,-29,91] |
| 45227 | Feeble Gravitational Anomaly | NOSTACKING (0) | - | MaterialCreation > 421; VisualProfession == 11 | Hit[27,-707,-1209,91]; CastNano[144630] |
| 45919 | Ferocious Impactor Missile | NOSTACKING (0) | - | MaterialCreation > 746; VisualProfession == 11 | Hit[27,-528,-1057,90] |
| 45920 | Fiery Blast | NOSTACKING (0) | - | MaterialCreation > 656; VisualProfession == 11 | Hit[27,-324,-799,97] |
| 28607 | Fire Snake | NOSTACKING (0) | - | MaterialCreation > 142; VisualProfession == 11 | Hit[27,-38,-92,97] |
| 28608 | Fire Stream | NOSTACKING (0) | - | MaterialCreation > 51; VisualProfession == 11 | Hit[27,-26,-46,97] |
| 45228 | Focused Ray | NOSTACKING (0) | - | MaterialCreation > 543; VisualProfession == 11 | Hit[27,-569,-816,92] |
| 45921 | Foul Bane | NOSTACKING (0) | - | MaterialCreation > 425; VisualProfession == 11 | Hit[27,-167,-456,96] |
| 28609 | Freezing Surge | NOSTACKING (0) | - | MaterialCreation > 451; VisualProfession == 11 | Hit[27,-186,-503,95] |
| 254437 | Frost Blades | DOTNanotechnicianStrainA (8) | - | MaterialCreation > 1641; Profession == 11; SpaceTime > 1640; AlienLevel > 15 | Hit[27,-630,-660,95]; Hit[27,-1260,-1320,95]; Hit[27,-1890,-1980,95]; Hit[27,-2520,-2640,95]; Hit[27,-630,-660,95] |
| 45229 | Frosty Welcome | NOSTACKING (0) | - | MaterialCreation > 206; VisualProfession == 11 | Hit[27,-126,-257,95] |
| 45908 | Furious Wind Blade | NOSTACKING (0) | - | MaterialCreation > 486; VisualProfession == 11 | Hit[27,-204,-551,91] |
| 45231 | Gaping Puncture | NOSTACKING (0) | - | MaterialCreation > 186; VisualProfession == 11 | Hit[27,-110,-217,90] |
| 275692 | Garuk's Improved Viral Assault | NOSTACKING (0) | - | Profession == 11; MaterialCreation > 1999; NanoFocusLevel op22 64 | Hit[27,-500,-750,0]; Hit[27,-12800,-16600,0]; Hit[27,-11600,-15200,0]; Hit[27,-10400,-14800,0]; Hit[27,-9200,-13400,0]; Hit[27,-8000,-12000,0] |
| 45232 | Glacial Advance | NOSTACKING (0) | - | MaterialCreation > 236; VisualProfession == 11 | Hit[27,-288,-580,95] |
| 45909 | Greater Blade Chaos | NOSTACKING (0) | - | MaterialCreation > 555; VisualProfession == 11 | Hit[27,-293,-568,91] |
| 45910 | Greater Chilling Stream | NOSTACKING (0) | - | MaterialCreation > 336; VisualProfession == 11 | Hit[27,-118,-319,95] |
| 45911 | Greater Crystalizing Ray | NOSTACKING (0) | - | MaterialCreation > 638; VisualProfession == 11 | Hit[27,-327,-719,95] |
| 45912 | Greater RNA Reaper | NOSTACKING (0) | - | MaterialCreation > 595; VisualProfession == 11 | Hit[27,-308,-644,94] |
| 45913 | Greater Shower with Sludge | NOSTACKING (0) | - | MaterialCreation > 615; VisualProfession == 11 | Hit[27,-293,-733,93] |
| 45914 | Greater Toxic Field | NOSTACKING (0) | - | MaterialCreation > 389; VisualProfession == 11 | Hit[27,-169,-339,93] |
| 42543 | Halon Cloud | NOSTACKING (0) | - | MaterialCreation > 103; VisualProfession == 11 | Hit[27,-54,-95,95] |
| 45323 | Hoary Seep | NOSTACKING (0) | - | MaterialCreation > 270; VisualProfession == 11 | Hit[27,-254,-294,95] |
| 28611 | Hot Foot | NOSTACKING (0) | - | MaterialCreation > 68; VisualProfession == 11 | Hit[27,-33,-53,97] |
| 28612 | Ice Flechette | NOSTACKING (0) | - | MaterialCreation > 4; VisualProfession == 11 | Hit[27,-13,-17,95]; CastNano[144632] |
| 45904 | Impactor Missile | NOSTACKING (0) | - | MaterialCreation > 577; VisualProfession == 11 | Hit[27,-300,-613,90] |
| 45222 | Impaling Tracer | NOSTACKING (0) | - | MaterialCreation > 644; VisualProfession == 11 | Hit[27,-531,-1225,90] |
| 28614 | Invasive Presence | NOSTACKING (0) | - | MaterialCreation > 323; VisualProfession == 11 | Hit[27,-120,-283,96] |
| 45905 | Ion Stream | NOSTACKING (0) | - | MaterialCreation > 73; VisualProfession == 11 | Hit[27,-19,-41,94] |
| 45223 | Isotope Deluge | NOSTACKING (0) | - | MaterialCreation > 245; VisualProfession == 11 | Hit[27,-163,-326,94] |
| 45907 | Lesser Bane of the Living | NOSTACKING (0) | - | MaterialCreation > 202; VisualProfession == 11 | Hit[27,-68,-154,96] |
| 45224 | Lesser Coronet of Frost | NOSTACKING (0) | - | MaterialCreation > 505; VisualProfession == 11 | Hit[27,-405,-862,95]; CastNano[144628] |
| 45898 | Lesser Encircle With Blades | NOSTACKING (0) | - | MaterialCreation > 224; VisualProfession == 11 | Hit[27,-80,-178,91] |
| 45899 | Lesser RNA Reaper | NOSTACKING (0) | - | MaterialCreation > 78; VisualProfession == 11 | Hit[27,-29,-34,94] |
| 28621 | Lightning Strike | NOSTACKING (0) | - | MaterialCreation > 377; VisualProfession == 11 | Hit[27,-259,-538,92] |
| 45901 | Liquefying Sphere | NOSTACKING (0) | - | MaterialCreation > 608; VisualProfession == 11 | Hit[27,-420,-559,93] |
| 254439 | Magma Covering | DOTNanotechnicianStrainA (8) | - | MaterialCreation > 1820; Profession == 11; SpaceTime > 1819; AlienLevel > 17 | Hit[27,-741,-761,97]; Hit[27,-1482,-1522,97]; Hit[27,-2223,-2283,97]; Hit[27,-2964,-3044,97]; Hit[27,-741,-761,97] |
| 45214 | Major Chemical Burn | NOSTACKING (0) | - | MaterialCreation > 677; VisualProfession == 11 | Hit[27,-559,-1333,93] |
| 45140 | Mephitic Ichor | NOSTACKING (0) | - | MaterialCreation > 577; VisualProfession == 11 | Hit[27,-475,-1039,96] |
| 44538 | Meson Blast | NOSTACKING (0) | - | MaterialCreation > 486; VisualProfession == 11 | Hit[27,-388,-824,92] |
| 45216 | Meta-Dioxin Spray | NOSTACKING (0) | - | MaterialCreation > 626; VisualProfession == 11 | Hit[27,-516,-1173,93] |
| 45902 | Micro Flechette | NOSTACKING (0) | - | MaterialCreation > 93; VisualProfession == 11 | Hit[27,-21,-62,90] |
| 45903 | Micro Flechette Swarm | NOSTACKING (0) | - | MaterialCreation > 543; VisualProfession == 11 | Hit[27,-229,-634,90] |
| 28622 | Microblade Whirlwind | DOTNanotechnicianStrainA (8) | - | MaterialCreation > 232; VisualProfession == 11; SpaceTime > 232 | Hit[27,-12,-12,91] |
| 45895 | Minor Bane | NOSTACKING (0) | - | MaterialCreation > 264; VisualProfession == 11 | Hit[27,-97,-235,96] |
| 45896 | Minor Toxic Barb | NOSTACKING (0) | - | MaterialCreation > 18; VisualProfession == 11 | Hit[27,-12,-20,96] |
| 45897 | Minor Toxic Field | NOSTACKING (0) | - | MaterialCreation > 63; VisualProfession == 11 | Hit[27,-16,-32,93] |
| 42540 | Molecule Lance | NOSTACKING (0) | - | MaterialCreation > 88; VisualProfession == 11 | Hit[27,-47,-75,90] |
| 45218 | Momentum Impaler | NOSTACKING (0) | - | MaterialCreation > 571; VisualProfession == 11 | Hit[27,-469,-1023,90] |
| 45219 | Nano Contagion | NOSTACKING (0) | - | MaterialCreation > 251; VisualProfession == 11 | Hit[27,-170,-334,96] |
| 45220 | Nanoblade Cloud | NOSTACKING (0) | - | MaterialCreation > 656; VisualProfession == 11 | Hit[27,-544,-1257,91] |
| 45221 | Neutron Spiral | NOSTACKING (0) | - | MaterialCreation > 316; VisualProfession == 11 | Hit[27,-129,-526,94] |
| 45205 | Ol' Faithful | NOSTACKING (0) | - | MaterialCreation > 455; VisualProfession == 11 | Hit[27,-594,-611,90] |
| 45892 | Open Wound | NOSTACKING (0) | - | MaterialCreation > 40; VisualProfession == 11 | Hit[27,-16,-25,91] |
| 28626 | Particle Accelerator | NOSTACKING (0) | - | MaterialCreation > 566; VisualProfession == 11 | Hit[27,-252,-665,92] |
| 45207 | Particle Shower | NOSTACKING (0) | - | MaterialCreation > 413; VisualProfession == 11 | Hit[27,-288,-660,94] |
| 45893 | Pellet of Fire | NOSTACKING (0) | - | MaterialCreation > 385; VisualProfession == 11 | Hit[27,-140,-377,97] |
| 28627 | Phoenix Swarm | NOSTACKING (0) | - | MaterialCreation > 174; VisualProfession == 11 | Hit[27,-104,-189,97] |
| 45208 | Phosphor Torch | NOSTACKING (0) | - | MaterialCreation > 385; VisualProfession == 11 | Hit[27,-266,-564,97] |
| 28628 | Plasma Lights | NOSTACKING (0) | - | MaterialCreation > 240; VisualProfession == 11 | Hit[27,-85,-212,92] |
| 28630 | Poison Missile | NOSTACKING (0) | - | MaterialCreation > 152; VisualProfession == 11 | Hit[27,-38,-113,96] |
| 254435 | Poisoned Thorns | DOTNanotechnicianStrainA (8) | - | MaterialCreation > 1328; Profession == 11; SpaceTime > 1327; AlienLevel > 10 | Hit[27,-513,-533,96]; Hit[27,-1026,-1066,96]; Hit[27,-1539,-1599,96]; Hit[27,-2053,-2132,96]; Hit[27,-513,-533,96] |
| 45209 | Positron Stream | NOSTACKING (0) | - | MaterialCreation > 443; VisualProfession == 11 | Hit[27,-330,-740,92] |
| 45211 | Radiation Scour | NOSTACKING (0) | - | MaterialCreation > 363; VisualProfession == 11 | Hit[27,-364,-401,94] |
| 28632 | Radioactive Cloud | NOSTACKING (0) | - | MaterialCreation > 460; VisualProfession == 11 | Hit[27,-244,-442,94] |
| 45212 | Rend Flesh | NOSTACKING (0) | - | MaterialCreation > 602; VisualProfession == 11 | Hit[27,-492,-1109,91] |
| 45886 | RNA Reaper | NOSTACKING (0) | - | MaterialCreation > 296; VisualProfession == 11 | Hit[27,-113,-254,94] |
| 70629 | Searing Agony | DOTNanotechnicianStrainA (8) | - | MaterialCreation > 381; SpaceTime > 381; VisualProfession == 11 | Hit[27,-31,-31,97] |
| 254441 | Self Illumination | DOTNanotechnicianStrainA (8) | - | MaterialCreation > 1999; Profession == 11; SpaceTime > 1993; AlienLevel > 19 | Hit[27,-830,-860,94]; Hit[27,-1660,-1720,94]; Hit[27,-2490,-2580,94]; Hit[27,-3220,-3440,94]; Hit[27,-830,-860,94] |
| 28634 | Shockwave Slash | NOSTACKING (0) | - | MaterialCreation > 310; VisualProfession == 11 | Hit[27,-111,-287,91] |
| 45887 | Shower With Sludge | NOSTACKING (0) | - | MaterialCreation > 190; VisualProfession == 11 | Hit[27,-55,-156,93] |
| 28636 | Slime Cascade | DOTNanotechnicianStrainA (8) | - | MaterialCreation > 439; VisualProfession == 11; SpaceTime > 439 | Hit[27,-38,-38,93] |
| 45888 | Smiting Missile | NOSTACKING (0) | - | MaterialCreation > 117; VisualProfession == 11 | Hit[27,-38,-66,90] |
| 45889 | Smiting Missile Mk II | NOSTACKING (0) | - | MaterialCreation > 264; VisualProfession == 11 | Hit[27,-99,-223,90] |
| 45890 | Solar Wind | NOSTACKING (0) | - | MaterialCreation > 712; VisualProfession == 11 | Hit[27,-387,-959,94] |
| 45891 | Stargasp | NOSTACKING (0) | - | MaterialCreation > 727; VisualProfession == 11 | Hit[27,-419,-1024,92] |
| 45196 | Stinging Missile | NOSTACKING (0) | - | MaterialCreation > 296; VisualProfession == 11 | Hit[27,-198,-411,90] |
| 45881 | Sudden Chill | NOSTACKING (0) | - | MaterialCreation > 113; VisualProfession == 11 | Hit[27,-26,-79,95] |
| 45200 | Tear Flesh | NOSTACKING (0) | - | MaterialCreation > 537; VisualProfession == 11 | Hit[27,-428,-935,91] |
| 45201 | Thunderclap | NOSTACKING (0) | - | MaterialCreation > 290; VisualProfession == 11 | Hit[27,-194,-400,91] |
| 45882 | Toxic Field | NOSTACKING (0) | - | MaterialCreation > 178; VisualProfession == 11 | Hit[27,-44,-135,93] |
| 45883 | Toxic Sphere | NOSTACKING (0) | - | MaterialCreation > 589; VisualProfession == 11 | Hit[27,-277,-694,93] |
| 45885 | Tri-Ion Stream | NOSTACKING (0) | - | MaterialCreation > 512; VisualProfession == 11 | Hit[27,-223,-578,94] |
| 45204 | Viral Assault | NOSTACKING (0) | - | MaterialCreation > 310; VisualProfession == 11 | Hit[27,-296,-343,96] |
| 45191 | Void Warmth | NOSTACKING (0) | - | MaterialCreation > 405; VisualProfession == 11 | Hit[27,-281,-633,95] |
| 28592 | Vulcan Flechette | NOSTACKING (0) | - | MaterialCreation > 525; VisualProfession == 11 | Hit[27,-417,-905,90] |
| 45193 | Weak Chemical Liquefaction | NOSTACKING (0) | - | MaterialCreation > 277; VisualProfession == 11 | Hit[27,-234,-478,93] |
| 45194 | Weak Gravity Collapse | NOSTACKING (0) | - | MaterialCreation > 166; VisualProfession == 11 | Hit[27,-174,-369,91] |
| 45879 | Weak Smiting Missile | NOSTACKING (0) | - | MaterialCreation > 18; VisualProfession == 11 | Hit[27,-12,-22,90] |
| 45880 | Wind Blade | NOSTACKING (0) | - | MaterialCreation > 132; VisualProfession == 11 | Hit[27,-33,-95,91] |

## Debuffs (20)

| id | name | nano line (strain) | minLvl | cast reqs | effect |
| ---: | --- | --- | ---: | --- | --- |
| 83951 | Brutal Cornea Attack | AAODebuffs (13) | - | SensoryImprovement > 729; PsychologicalModification > 729; VisualProfession == 11 | Skill[276,-190]; TauntNpc[299] |
| 83943 | Claw Eyes | AAODebuffs (13) | - | SensoryImprovement > 46; PsychologicalModification > 46; VisualProfession == 11 | Skill[276,-45]; TauntNpc[59] |
| 259354 | Constant Barrage | NanoResistanceDebuff_LineA (643) | - | MaterialCreation > 1699; PsychologicalModification > 1699; Profession == 11; Specialization op22 8; NanoFocusLevel op22 8; Expansion op22 2 | Hit[27,-2150,-2150,92]; CastNano[259357]; CastNano[259356]; CastNano[259355] |
| 83955 | Cornea Attack | AAODebuffs (13) | - | SensoryImprovement > 447; PsychologicalModification > 447; VisualProfession == 11 | Skill[276,-118]; TauntNpc[193] |
| 83958 | Curtain of Darkness | AAODebuffs (13) | - | SensoryImprovement > 228; PsychologicalModification > 228; VisualProfession == 11 | Skill[276,-70]; TauntNpc[125] |
| 259339 | Distracting Shower | NanoResistanceDebuff_LineA (643) | - | MaterialCreation > 699; PsychologicalModification > 699; Profession == 11; Specialization op22 1; NanoFocusLevel op22 1; Expansion op22 2 | Hit[27,-455,-455,92]; CastNano[259342]; CastNano[259341]; CastNano[259340] |
| 83952 | Eviscerate Eyes | AAODebuffs (13) | - | SensoryImprovement > 701; PsychologicalModification > 701; VisualProfession == 11 | Skill[276,-175]; TauntNpc[277] |
| 83953 | Eyeblighter | AAODebuffs (13) | - | SensoryImprovement > 650; PsychologicalModification > 650; VisualProfession == 11 | Skill[276,-157]; TauntNpc[252] |
| 83950 | Foul Eyeblighter | AAODebuffs (13) | - | SensoryImprovement > 753; PsychologicalModification > 753; VisualProfession == 11 | Skill[276,-205]; TauntNpc[319] |
| 83942 | Gouge Eyes | AAODebuffs (13) | - | SensoryImprovement > 142; PsychologicalModification > 142; VisualProfession == 11 | Skill[276,-89]; TauntNpc[89] |
| 259348 | Incessant Flurry | NanoResistanceDebuff_LineA (643) | - | MaterialCreation > 949; PsychologicalModification > 949; Profession == 11; Specialization op22 2; NanoFocusLevel op22 2; Expansion op22 2 | Hit[27,-775,-775,92]; CastNano[259346]; CastNano[259345]; CastNano[259344] |
| 83956 | Lesser Eyeblighter | AAODebuffs (13) | - | SensoryImprovement > 401; PsychologicalModification > 401; VisualProfession == 11 | Skill[276,-105]; TauntNpc[174] |
| 275697 | Optic Plague | AAODebuffs (13) | - | SensoryImprovement > 1780; PsychologicalModification > 1780; Profession == 11; NanoFocusLevel op22 64 | TauntNpc[609]; Skill[276,-499] |
| 259349 | Overwhelming Storm | NanoResistanceDebuff_LineA (643) | - | MaterialCreation > 1249; PsychologicalModification > 1249; Profession == 11; Specialization op22 4; NanoFocusLevel op22 4; Expansion op22 2 | Hit[27,-1575,-1575,92]; CastNano[259352]; CastNano[259351]; CastNano[259350] |
| 83949 | Photon Deflector | AAODebuffs (13) | - | SensoryImprovement > 802; PsychologicalModification > 802; VisualProfession == 11 | Skill[276,-223]; TauntNpc[345] |
| 83944 | Poke Eyes | AAODebuffs (13) | - | SensoryImprovement > 24; PsychologicalModification > 24; VisualProfession == 11 | Skill[276,-20]; TauntNpc[54] |
| 83954 | Pronounce Blindness | AAODebuffs (13) | - | SensoryImprovement > 555; PsychologicalModification > 555; VisualProfession == 11 | Skill[276,-139]; TauntNpc[222] |
| 83957 | Shroud of Darkness | AAODebuffs (13) | - | SensoryImprovement > 316; PsychologicalModification > 316; VisualProfession == 11 | Skill[276,-88]; TauntNpc[149] |
| 83948 | Shroud of the Grave | AAODebuffs (13) | - | SensoryImprovement > 841; PsychologicalModification > 841; VisualProfession == 11 | Skill[276,-237]; TauntNpc[366] |
| 83947 | Visions of the Void | AAODebuffs (13) | - | SensoryImprovement > 873; PsychologicalModification > 873; VisualProfession == 11 | Skill[276,-244]; TauntNpc[385] |

## Mezz / Calm (16)

| id | name | nano line (strain) | minLvl | cast reqs | effect |
| ---: | --- | --- | ---: | --- | --- |
| 253380 | Enfraam's Perceiver | Mezz (147) | 198 | Profession == 11; Level > 197; PsychologicalModification > 988; SensoryImprovement > 988; Expansion op22 2; Specialization op22 4; NanoFocusLevel op22 4 | Modify RangeIncreaserWeapon -14; Modify NanoRange -14 |
| 253382 | Optimized Perceiver | Mezz (147) | 201 | Profession == 11; Level > 200; PsychologicalModification > 1195; SensoryImprovement > 1195; Expansion op22 2; Specialization op22 8; NanoFocusLevel op22 8 | Modify RangeIncreaserWeapon -18; Modify NanoRange -18 |
| 253384 | Enfraam's Perception Deciever | Mezz (147) | 211 | Profession == 11; Level > 210; PsychologicalModification > 1642; SensoryImprovement > 1640; Expansion op22 2; Specialization op22 8; NanoFocusLevel op22 8 | Modify RangeIncreaserWeapon -22; Modify NanoRange -22 |
| 100440 | Bewilder | Mezz (147) | - | PsychologicalModification > 157; SensoryImprovement > 157; VisualProfession == 11 | Modify MeleeInit -291; Modify RangedInit -291; Modify PhysicalInit -291; Modify NanoCInit -291 |
| 100441 | Discourage Involvement | Mezz (147) | - | PsychologicalModification > 602; SensoryImprovement > 602; VisualProfession == 11 | Modify MeleeInit -643; Modify RangedInit -643; Modify PhysicalInit -643; Modify NanoCInit -643 |
| 259335 | Empowered Bewilder | Mezz (147) | - | PsychologicalModification > 499; SensoryImprovement > 499; Profession == 11; Specialization op22 1; Expansion op22 2 | Mezz[] |
| 259362 | Empowered Discourage Involvement | Mezz (147) | - | PsychologicalModification > 1349; SensoryImprovement > 1349; Profession == 11; Specialization op22 4; Expansion op22 2 | Mezz[] |
| 259364 | Empowered Peaceful Intentions | Mezz (147) | - | PsychologicalModification > 1799; SensoryImprovement > 1799; Profession == 11; Specialization op22 8; Expansion op22 2 | Mezz[] |
| 259367 | Empowered Project Calm | Mezz (147) | - | PsychologicalModification > 899; SensoryImprovement > 899; Profession == 11; Specialization op22 2; Expansion op22 2 | Mezz[] |
| 28625 | Neural Stunner | Mezz (147) | - | MaterialCreation > 699; BiologicalMetamorphosis > 699; VisualProfession == 11 | TauntNpc[4369] |
| 100443 | Peaceful Intentions | Mezz (147) | - | PsychologicalModification > 733; SensoryImprovement > 733; VisualProfession == 11 | Modify MeleeInit -802; Modify RangedInit -802; Modify PhysicalInit -802; Modify NanoCInit -802 |
| 100442 | Project Calm | Mezz (147) | - | PsychologicalModification > 283; SensoryImprovement > 283; VisualProfession == 11 | Modify MeleeInit -404; Modify RangedInit -404; Modify PhysicalInit -404; Modify NanoCInit -404 |
| 259336 | Specialized Bewilder | Mezz (147) | - | PsychologicalModification > 499; SensoryImprovement > 499; Profession == 11; Specialization op22 1; NanoFocusLevel op22 1; Expansion op22 2 | Mezz[] |
| 259363 | Specialized Discourage Involvement | Mezz (147) | - | PsychologicalModification > 1349; SensoryImprovement > 1349; Profession == 11; Specialization op22 4; NanoFocusLevel op22 4; Expansion op22 2 | Mezz[] |
| 259365 | Specialized Peaceful Intentions | Mezz (147) | - | PsychologicalModification > 1799; SensoryImprovement > 1799; Profession == 11; Specialization op22 8; NanoFocusLevel op22 8; Expansion op22 2 | Mezz[] |
| 259366 | Specialized Project Calm | Mezz (147) | - | PsychologicalModification > 899; SensoryImprovement > 899; Profession == 11; Specialization op22 2; NanoFocusLevel op22 2; Expansion op22 2 | Mezz[] |

## Heals / HoT (8)

| id | name | nano line (strain) | minLvl | cast reqs | effect |
| ---: | --- | --- | ---: | --- | --- |
| 90405 | Basic Humidity Extractor | NanoOverTime_LineA (14) | - | MaterialMetamorphosis > 88; SpaceTime > 88; VisualProfession == 11 | Hit[214,21,21,0] |
| 90401 | Boundless Humidity Extractor | NanoOverTime_LineA (14) | - | MaterialMetamorphosis > 683; SpaceTime > 683; VisualProfession == 11 | Hit[214,191,191,0] |
| 90403 | Efficient Humidity Extractor | NanoOverTime_LineA (14) | - | MaterialMetamorphosis > 370; SpaceTime > 370; VisualProfession == 11 | Hit[214,102,102,0] |
| 90404 | Humidity Extractor | NanoOverTime_LineA (14) | - | MaterialMetamorphosis > 190; SpaceTime > 190; VisualProfession == 11 | Hit[214,51,51,0] |
| 275024 | Izgimmer's Wealth | NanoPointHeals (588) | - | MaterialMetamorphosis > 1819; SpaceTime > 1819; Profession == 11; NanoFocusLevel op22 64 | Hit[214,20000,20000,0] |
| 90406 | Personal Notum Harvester | NanoOverTime_LineA (14) | - | MaterialMetamorphosis > 785; SpaceTime > 785; VisualProfession == 11 | Hit[214,235,235,0] |
| 90400 | Rudimentary Humidity Extractor | NanoOverTime_LineA (14) | - | MaterialMetamorphosis > 18; SpaceTime > 18; VisualProfession == 11 | Hit[214,5,5,0] |
| 90402 | Superior Humidity Extractor | NanoOverTime_LineA (14) | - | MaterialMetamorphosis > 466; SpaceTime > 466; VisualProfession == 11 | Hit[214,131,131,0] |

## Travel / Summon Utility (2)

| id | name | nano line (strain) | minLvl | cast reqs | effect |
| ---: | --- | --- | ---: | --- | --- |
| 150334 | Team Warp Time and Space: Capital City | SelfGrid (1030) | - | MaterialCreation > 447; SpaceTime > 447; MaterialMetamorphosis > 447; VisualProfession == 11; ExpansionPlayfield == 0; PlayfieldType op107 2 | TeamCastNano[150322] |
| 150346 | Warp Time and Space: Capital City | SelfGrid (1030) | - | MaterialCreation > 409; SpaceTime > 409; MaterialMetamorphosis > 409; VisualProfession == 11; ExpansionPlayfield == 0; PlayfieldType op107 2 | Teleport[636,67,720,800]; Teleport[545,8,543,640]; Teleport[259,18,317,730] |

## Misc / Utility (42)

| id | name | nano line (strain) | minLvl | cast reqs | effect |
| ---: | --- | --- | ---: | --- | --- |
| 203805 | Tear Constraints | NOSTACKING (0) | 50 | Profession == 11; PsychologicalModification > 426; SpaceTime > 426; Level > 49 | ReduceNanoStrainDuration[146,43]; SystemText[Your movement is less restricted.] |
| 205441 | Enfraam's Inverted Restrainer | NOSTACKING (0) | 75 | Profession == 11; MaterialCreation > 569; SpaceTime > 569; Level > 74 | TeamCastNano[205429]; SystemText[The confines around your team-mates are lessened.] |
| 266298 | Explosive Cataclysm | NOSTACKING (0) | 75 | Expansion op22 32; Profession == 11; MaterialCreation > 562; Level > 74; NanoFocusLevel op22 1; Flags op101 266314 | AreaCastNano[266316,5]; CastNano[266315] |
| 266297 | Earthshattering Cataclysm | NOSTACKING (0) | 120 | Expansion op22 32; Profession == 11; MaterialCreation > 824; Level > 119; NanoFocusLevel op22 2; Flags op101 266314 | AreaCastNano[266317,5]; CastNano[266315] |
| 203807 | Rend Constraints | NOSTACKING (0) | 135 | Profession == 11; PsychologicalModification > 849; SpaceTime > 849; Level > 134 | ReduceNanoStrainDuration[146,95]; SystemText[Your movement is less restricted.] |
| 205443 | Izgimmer's Transposed Restraints | NOSTACKING (0) | 155 | Profession == 11; MaterialCreation > 911; SpaceTime > 911; Level > 154 | TeamCastNano[205430]; SystemText[The confines around your team-mates are lessened.] |
| 201523 | Living Codex of Izgimmer | NOSTACKING (0) | 165 | Profession == 11; PsychologicalModification > 930; SensoryImprovement > 930; Level > 164 | CastNano[201467]; CastNano[201468]; CastNano[201469]; CastNano[201470]; CastNano[201471]; CastNano[201472]; CastNano[201473]; CastNano[201474]; CastNano[201475]; CastNano[201476] |
| 266296 | Notum Cataclysm | NOSTACKING (0) | 165 | Expansion op22 32; Profession == 11; MaterialCreation > 1114; Level > 164; NanoFocusLevel op22 2; Flags op101 266314 | AreaCastNano[266318,5]; CastNano[266315] |
| 203809 | Sunder Constraints | NOSTACKING (0) | 175 | Profession == 11; PsychologicalModification > 988; SpaceTime > 988; Level > 174 | ReduceNanoStrainDuration[146,132]; SystemText[Your movement is less restricted.] |
| 266295 | Jobe Cataclysm | NOSTACKING (0) | 200 | Expansion op22 32; Profession == 11; MaterialCreation > 1307; Level > 199; NanoFocusLevel op22 4; Flags op101 266314 | AreaCastNano[266319,5]; CastNano[266315] |
| 266294 | Enfraam's Cataclysm | NOSTACKING (0) | 214 | Expansion op22 32; Profession == 11; MaterialCreation > 1944; Level > 213; NanoFocusLevel op22 8; Flags op101 266314 | AreaCastNano[266320,5]; CastNano[266315] |
| 266293 | Mastablasta's Cataclysm | NOSTACKING (0) | 220 | Expansion op22 32; Profession == 11; MaterialCreation > 2239; Level > 219; NanoFocusLevel op22 8; Flags op101 266314 | AreaCastNano[266321,5]; CastNano[266315] |
| 45940 | Barrage of Blades | NOSTACKING (0) | - | MaterialCreation > 443; VisualProfession == 11 | AreaCastNano[158525,12] |
| 28593 | Barrage of Fire | NOSTACKING (0) | - | MaterialCreation > 512; VisualProfession == 11 | AreaCastNano[158507,12] |
| 28594 | Blaze of Hephaestos | NOSTACKING (0) | - | MaterialCreation > 774; VisualProfession == 11 | AreaCastNano[158524,12] |
| 28596 | Boil Blood | DOTNanotechnicianStrainA (8) | - | MaterialCreation > 710; VisualProfession == 11; SpaceTime > 710 | AreaCastNano[70622,12] |
| 45943 | Brief Poison Fog | NOSTACKING (0) | - | MaterialCreation > 127; VisualProfession == 11 | AreaCastNano[158523,12] |
| 28599 | Circle of Winter | NOSTACKING (0) | - | MaterialCreation > 240; VisualProfession == 11 | AreaCastNano[158522,12] |
| 45937 | Circle Scythe | NOSTACKING (0) | - | MaterialCreation > 283; VisualProfession == 11 | AreaCastNano[158521,12] |
| 45925 | Dense Poison Fog | NOSTACKING (0) | - | MaterialCreation > 473; VisualProfession == 11 | AreaCastNano[158520,15] |
| 83962 | Enveloping Darkness | NOSTACKING (0) | - | SensoryImprovement > 270; PsychologicalModification > 270; VisualProfession == 11 | AreaCastNano[83938,20] |
| 45917 | Expanding Neutron Pulse | NOSTACKING (0) | - | MaterialCreation > 329; VisualProfession == 11 | AreaCastNano[158519,15] |
| 273382 | Forget Me! | DeTaunt (842) | - | BiologicalMetamorphosis > 1540; PsychologicalModification > 1540; Profession == 11; NanoFocusLevel op22 64 | AreaCastNano[273383,20] |
| 45922 | Frigid Landscape | NOSTACKING (0) | - | MaterialCreation > 739; VisualProfession == 11 | AreaCastNano[158518,16] |
| 285182 | Gravity Shift - Block | Strain917 (917) | - | Profession == 3; Profession == 7; Profession == 11; Flags op91 285181; Flags op42 0; PlayfieldProxy == 7015 |  |
| 45906 | Isotope Waves | NOSTACKING (0) | - | MaterialCreation > 668; VisualProfession == 11 | AreaCastNano[158517,16] |
| 28620 | Kel's Neutronium Plaything | NOSTACKING (0) | - | MaterialCreation > 993; VisualProfession == 11 | AreaCastNano[158516,25] |
| 279354 | Kicker of Kell | NOSTACKING (0) | - | VisualProfession == 11; SensoryImprovement > 119; SpaceTime > 119; Flags op101 279355 | AreaCastNano[279357,20]; CastNano[279355] |
| 83959 | Legions of the Eyeblighter | NOSTACKING (0) | - | SensoryImprovement > 744; PsychologicalModification > 744; VisualProfession == 11 | AreaCastNano[83940,20] |
| 45900 | Limited Shrapnel Spray | NOSTACKING (0) | - | MaterialCreation > 397; VisualProfession == 11 | AreaCastNano[158515,12] |
| 83964 | Mass Claw Eyes | NOSTACKING (0) | - | SensoryImprovement > 98; PsychologicalModification > 98; VisualProfession == 11 | AreaCastNano[83946,20] |
| 83961 | Mass Cornea Attack | NOSTACKING (0) | - | SensoryImprovement > 512; PsychologicalModification > 512; VisualProfession == 11 | AreaCastNano[83941,20] |
| 83960 | Mass Pronounce Blindness | NOSTACKING (0) | - | SensoryImprovement > 620; PsychologicalModification > 620; VisualProfession == 11 | AreaCastNano[83939,20] |
| 45894 | Mild Toxic Spill | NOSTACKING (0) | - | MaterialCreation > 182; VisualProfession == 11 | AreaCastNano[158514,12] |
| 28629 | Plasma Swirl | NOSTACKING (0) | - | MaterialCreation > 363; VisualProfession == 11 | AreaCastNano[158513,10] |
| 28631 | Radiation Pulse | NOSTACKING (0) | - | MaterialCreation > 35; VisualProfession == 11 | AreaCastNano[158512,9] |
| 28633 | Shockball | NOSTACKING (0) | - | MaterialCreation > 78; VisualProfession == 11 | AreaCastNano[158506,8] |
| 28635 | Shrapnel Burst | NOSTACKING (0) | - | MaterialCreation > 571; VisualProfession == 11 | AreaCastNano[158511,10] |
| 45884 | Toxic Spill | NOSTACKING (0) | - | MaterialCreation > 615; VisualProfession == 11 | AreaCastNano[158510,14] |
| 28637 | Tremor | NOSTACKING (0) | - | MaterialCreation > 817; VisualProfession == 11 | AreaCastNano[158509,19] |
| 28638 | Volcanic Eruption | NOSTACKING (0) | - | MaterialCreation > 835; VisualProfession == 11 | AreaCastNano[158508,20] |
| 83963 | Wild Eye Gouger | NOSTACKING (0) | - | SensoryImprovement > 174; PsychologicalModification > 174; VisualProfession == 11 | AreaCastNano[83945,20] |

## Detaunt (1)

| id | name | nano line (strain) | minLvl | cast reqs | effect |
| ---: | --- | --- | ---: | --- | --- |
| 201935 | Resonance Blast | NOSTACKING (0) | 195 | Profession == 11; Level > 194; MaterialCreation > 1046 | Hit[27,-3105,-5970,94]; TauntNpc[-3000] |

## Evade Buffs (2)

| id | name | nano line (strain) | minLvl | cast reqs | effect |
| ---: | --- | --- | ---: | --- | --- |
| 28603 | Dark Movement | MajorEvasionBuffs (144) | - | SensoryImprovement > 417; SpaceTime > 417; VisualProfession == 11 | Modify DuckExp +80; Modify DodgeRanged +80; Modify EvadeClsC +80 |
| 270802 | Improved Dark Movement | MajorEvasionBuffs (144) | - | SensoryImprovement > 1640; SpaceTime > 1640; Profession == 11; NanoFocusLevel op22 64 | Modify DuckExp +160; Modify EvadeClsC +160; Modify DodgeRanged +160 |

## Health Buffs (1)

| id | name | nano line (strain) | minLvl | cast reqs | effect |
| ---: | --- | --- | ---: | --- | --- |
| 266302 | Restrain Enthusiasm | HPBuff (151) | - | Expansion op22 32; MaterialCreation > 799; Profession == 11; SensoryImprovement > 799; Profession == 9 | CastNano[266302] |

## Reflect / Absorb Shields (12)

| id | name | nano line (strain) | minLvl | cast reqs | effect |
| ---: | --- | --- | ---: | --- | --- |
| 117675 | Advanced Collapsing Barrier | AbsorbACBuff (219) | - | MaterialCreation > 748; BiologicalMetamorphosis > 748; VisualProfession == 11 | ChangeVariable[238,644]; ChangeVariable[239,644]; ChangeVariable[240,644]; ChangeVariable[241,644]; ChangeVariable[242,644]; ChangeVariable[243,644]; ChangeVariable[244,644]; ChangeVariable[245,644] |
| 270356 | Advanced Fleeting Immunity | AbsorbACBuff (219) | - | MaterialCreation > 883; BiologicalMetamorphosis > 883; Profession == 11 | ChangeVariable[238,834]; ChangeVariable[239,834]; ChangeVariable[240,834]; ChangeVariable[241,834]; ChangeVariable[242,834]; ChangeVariable[243,834]; ChangeVariable[244,834]; ChangeVariable[245,834] |
| 117678 | Advanced Layered Protection | AbsorbACBuff (219) | - | MaterialCreation > 493; BiologicalMetamorphosis > 493; VisualProfession == 11 | ChangeVariable[238,428]; ChangeVariable[239,428]; ChangeVariable[240,428]; ChangeVariable[241,428]; ChangeVariable[242,428]; ChangeVariable[243,428]; ChangeVariable[244,428]; ChangeVariable[245,428] |
| 117677 | Collapsing Barrier | AbsorbACBuff (219) | - | MaterialCreation > 680; BiologicalMetamorphosis > 680; VisualProfession == 11 | ChangeVariable[238,563]; ChangeVariable[239,563]; ChangeVariable[240,563]; ChangeVariable[241,563]; ChangeVariable[242,563]; ChangeVariable[243,563]; ChangeVariable[244,563]; ChangeVariable[245,563] |
| 117676 | Fleeting Immunity | AbsorbACBuff (219) | - | MaterialCreation > 838; BiologicalMetamorphosis > 838; VisualProfession == 11 | ChangeVariable[238,725]; ChangeVariable[239,725]; ChangeVariable[240,725]; ChangeVariable[241,725]; ChangeVariable[242,725]; ChangeVariable[243,725]; ChangeVariable[244,725]; ChangeVariable[245,725] |
| 117679 | Layered Protection | AbsorbACBuff (219) | - | MaterialCreation > 381; BiologicalMetamorphosis > 381; VisualProfession == 11 | ChangeVariable[238,332]; ChangeVariable[239,332]; ChangeVariable[240,332]; ChangeVariable[241,332]; ChangeVariable[242,332]; ChangeVariable[243,332]; ChangeVariable[244,332]; ChangeVariable[245,332] |
| 302074 | Nanobot Aegis | ReflectShield (2) | - | Profession == 11; Expansion op22 2 | ChangeVariable[659,85]; ChangeVariable[661,150]; Hit[214,-500,-500,0]; Set[214,0]; CastNano[273388]; CastNano[263265] |
| 263265 | Nanobot Shelter | ReflectShield (2) | - | Expansion op22 2; MaterialMetamorphosis > 597; MaterialCreation > 597; Profession == 11 | CastNano[301609] |
| 301609 | Nanobot Shelter | ReflectShield (2) | - | Expansion op22 2; Profession == 11 | ChangeVariable[659,20]; ChangeVariable[661,110] |
| 273386 | Superior Fleeting Immunity | AbsorbACBuff (219) | - | MaterialCreation > 1540; BiologicalMetamorphosis > 1540; Profession == 11; NanoFocusLevel op22 64 | ChangeVariable[238,1334]; ChangeVariable[239,1334]; ChangeVariable[240,1334]; ChangeVariable[241,1334]; ChangeVariable[242,1334]; ChangeVariable[243,1334]; ChangeVariable[244,1334]; ChangeVariable[245,1334] |
| 273388 | Superior Nanobot Shelter | ReflectShield (2) | - | MaterialMetamorphosis > 1540; MaterialCreation > 1540; Profession == 11; NanoFocusLevel op22 64 | CastNano[301610] |
| 301610 | Superior Nanobot Shelter | ReflectShield (2) | - | Expansion op22 2; Profession == 11 | ChangeVariable[659,25]; ChangeVariable[661,110] |

## Root (16)

| id | name | nano line (strain) | minLvl | cast reqs | effect |
| ---: | --- | --- | ---: | --- | --- |
| 56024 | Ball and Chain | Root (146) | - | MaterialCreation > 499; SpaceTime > 499; VisualProfession == 11 | (1 non-effect functions only) |
| 56028 | Burden of Atlas | Root (146) | - | MaterialCreation > 864; SpaceTime > 864; VisualProfession == 11 | (1 non-effect functions only) |
| 259337 | Empowered Ball and Chain | Root (146) | - | MaterialCreation > 499; SpaceTime > 499; Profession == 11; Specialization op22 1; Expansion op22 2 | (1 non-effect functions only) |
| 259376 | Empowered Burden of Atlas | Root (146) | - | MaterialCreation > 1799; SpaceTime > 1799; Profession == 11; Specialization op22 8; Expansion op22 2 | (1 non-effect functions only) |
| 259374 | Empowered Feet of Lead | Root (146) | - | MaterialCreation > 1349; SpaceTime > 1349; Profession == 11; Specialization op22 4; Expansion op22 2 | (1 non-effect functions only) |
| 259372 | Empowered Gravity Pull | Root (146) | - | MaterialCreation > 899; SpaceTime > 899; Profession == 11; Specialization op22 2; Expansion op22 2 | (1 non-effect functions only) |
| 56025 | Feet of Iron | Root (146) | - | MaterialCreation > 349; SpaceTime > 349; VisualProfession == 11 | (1 non-effect functions only) |
| 56022 | Feet of Lead | Root (146) | - | MaterialCreation > 768; SpaceTime > 768; VisualProfession == 11 | (1 non-effect functions only) |
| 56027 | Feet of Stone | Root (146) | - | MaterialCreation > 73; SpaceTime > 73; VisualProfession == 11 | (1 non-effect functions only) |
| 56023 | Gravity Pull | Root (146) | - | MaterialCreation > 689; SpaceTime > 689; VisualProfession == 11 | (1 non-effect functions only) |
| 42110 | Malaise of Movement | Root (146) | - | MaterialCreation > 4; SpaceTime > 4; VisualProfession == 11 | (1 non-effect functions only) |
| 259338 | Specialized Ball and Chain | Root (146) | - | MaterialCreation > 499; SpaceTime > 499; Profession == 11; Specialization op22 1; NanoFocusLevel op22 1; Expansion op22 2 | (1 non-effect functions only) |
| 259377 | Specialized Burden of Atlas | Root (146) | - | MaterialCreation > 1799; SpaceTime > 1799; Profession == 11; Specialization op22 8; NanoFocusLevel op22 8; Expansion op22 2 | (1 non-effect functions only) |
| 259375 | Specialized Feet of Lead | Root (146) | - | MaterialCreation > 1349; SpaceTime > 1349; Profession == 11; Specialization op22 4; NanoFocusLevel op22 4; Expansion op22 2 | (1 non-effect functions only) |
| 259373 | Specialized Gravity Pull | Root (146) | - | MaterialCreation > 899; SpaceTime > 899; Profession == 11; Specialization op22 2; NanoFocusLevel op22 2; Expansion op22 2 | (1 non-effect functions only) |
| 56026 | Weak Gravity Pull | Root (146) | - | MaterialCreation > 198; SpaceTime > 198; VisualProfession == 11 | (1 non-effect functions only) |

## Taunt / Aggro (36)

| id | name | nano line (strain) | minLvl | cast reqs | effect |
| ---: | --- | --- | ---: | --- | --- |
| 45258 | Annihilating Hadron String | NOSTACKING (0) | - | MaterialCreation > 771; VisualProfession == 11 | Hit[27,-1526,-3912,94]; TauntNpc[4469] |
| 45260 | Atomic Collapse | NOSTACKING (0) | - | MaterialCreation > 731; VisualProfession == 11 | Hit[27,-1037,-3183,94]; TauntNpc[3211] |
| 28597 | Burning Bones | NOSTACKING (0) | - | MaterialCreation > 844; VisualProfession == 11 | Hit[27,-1005,-2476,97]; TauntNpc[2520] |
| 45247 | Cascade of the Storm | NOSTACKING (0) | - | MaterialCreation > 806; VisualProfession == 11 | Hit[27,-932,-2218,92]; TauntNpc[2229] |
| 28600 | Compacted Neutron Missile | NOSTACKING (0) | - | MaterialCreation > 759; VisualProfession == 11 | Hit[27,-1306,-1445,91]; CastNano[144625]; TauntNpc[1891] |
| 45254 | Conduction Stream | NOSTACKING (0) | - | MaterialCreation > 723; VisualProfession == 11 | Hit[27,-673,-1672,92]; TauntNpc[1565] |
| 45237 | Contained Plasma Sphere | NOSTACKING (0) | - | MaterialCreation > 632; VisualProfession == 11 | Hit[27,-974,-2047,97]; CastNano[144627]; TauntNpc[2118] |
| 45239 | Crown of Frost | NOSTACKING (0) | - | MaterialCreation > 716; VisualProfession == 11 | Hit[27,-652,-1624,95]; CastNano[144626]; TauntNpc[1511] |
| 28604 | Electrifying Containment | NOSTACKING (0) | - | MaterialCreation > 849; VisualProfession == 11 | Hit[27,-1020,-2498,92]; CastStunNano[144622,25]; TauntNpc[6183] |
| 45242 | Energized Collapse | NOSTACKING (0) | - | MaterialCreation > 518; VisualProfession == 11 | Hit[27,-812,-1534,94]; TauntNpc[1565] |
| 269473 | Freezing Lancets | NOSTACKING (0) | - | MaterialCreation > 1093; Profession == 11; Specialization op22 4; NanoFocusLevel op22 4; Expansion op22 2 | Hit[27,-1988,-3191,95]; CastStunNano[269474,15]; TauntNpc[3382] |
| 45230 | Full-Body Acid Coating | NOSTACKING (0) | - | MaterialCreation > 861; VisualProfession == 11 | Hit[27,-1034,-2556,93]; TauntNpc[2619] |
| 28610 | Furious Assault | NOSTACKING (0) | - | MaterialCreation > 393; VisualProfession == 11 | Hit[27,-397,-928,90]; CastStunNano[144631,40]; TauntNpc[2520] |
| 45233 | Glacial Finality | NOSTACKING (0) | - | MaterialCreation > 810; VisualProfession == 11 | Hit[27,-1189,-2695,95]; TauntNpc[2890] |
| 45234 | Gravitational Anomaly | NOSTACKING (0) | - | MaterialCreation > 780; VisualProfession == 11 | Hit[27,-1164,-2603,91]; CastNano[144624]; TauntNpc[2781] |
| 45235 | Greater Searing Stream | NOSTACKING (0) | - | MaterialCreation > 788; VisualProfession == 11 | Hit[27,-904,-2111,93]; TauntNpc[2113] |
| 45236 | Greater Viral Assault | NOSTACKING (0) | - | MaterialCreation > 832; VisualProfession == 11 | Hit[27,-988,-2382,96]; TauntNpc[2421] |
| 28613 | Internal Combustion | NOSTACKING (0) | - | MaterialCreation > 750; VisualProfession == 11 | Hit[27,-894,-1980,97]; TauntNpc[1994] |
| 28616 | Izgimmer's Enveloping Flame | NOSTACKING (0) | - | MaterialCreation > 846; VisualProfession == 11 | Hit[27,-2249,-3074,97]; CastNano[144623]; TauntNpc[4344] |
| 28618 | Izgimmer's Last Word | NOSTACKING (0) | - | MaterialCreation > 999; VisualProfession == 11 | Hit[27,-4118,-8907,94]; TauntNpc[15646] |
| 28619 | Izgimmer's Little Nuke | NOSTACKING (0) | - | MaterialCreation > 861; VisualProfession == 11 | Hit[27,-2834,-4739,92]; TauntNpc[7033] |
| 45225 | Linear Acceleration | NOSTACKING (0) | - | MaterialCreation > 737; VisualProfession == 11 | Hit[27,-754,-1753,90]; TauntNpc[1693] |
| 45226 | Localized Dimensional Inversion | NOSTACKING (0) | - | MaterialCreation > 870; VisualProfession == 11 | Hit[27,-1294,-2415,91]; TauntNpc[2728] |
| 45215 | Malign Devourer | NOSTACKING (0) | - | MaterialCreation > 695; VisualProfession == 11 | Hit[27,-605,-1446,96]; TauntNpc[1338] |
| 28623 | Molecular Deconstruction | NOSTACKING (0) | - | MaterialCreation > 855; VisualProfession == 11 | Hit[27,-1050,-2569,96]; TauntNpc[2645] |
| 45217 | Molecular Flechettes | NOSTACKING (0) | - | MaterialCreation > 765; VisualProfession == 11 | Hit[27,-852,-1951,90]; TauntNpc[1934] |
| 45206 | Particle Flare | NOSTACKING (0) | - | MaterialCreation > 595; VisualProfession == 11 | Hit[27,-607,-1418,94]; TauntNpc[1319] |
| 45210 | Quark Collapse | NOSTACKING (0) | - | MaterialCreation > 835; VisualProfession == 11 | Hit[27,-994,-2404,94]; TauntNpc[2446] |
| 45213 | Searing Circle | NOSTACKING (0) | - | MaterialCreation > 705; VisualProfession == 11 | Hit[27,-978,-2415,97]; TauntNpc[2442] |
| 45195 | Searing Stream | NOSTACKING (0) | - | MaterialCreation > 742; VisualProfession == 11 | Hit[27,-1257,-1321,93]; TauntNpc[1750] |
| 45197 | Stinging Missile Swarm | NOSTACKING (0) | - | MaterialCreation > 820; VisualProfession == 11 | Hit[27,-1497,-1762,90]; TauntNpc[2324] |
| 45198 | Sudden Affliction | NOSTACKING (0) | - | MaterialCreation > 756; VisualProfession == 11 | Hit[27,-836,-1889,96]; TauntNpc[1870] |
| 45199 | Superior Malign Devourer | NOSTACKING (0) | - | MaterialCreation > 794; VisualProfession == 11 | Hit[27,-912,-2157,96]; TauntNpc[2159] |
| 45202 | Thunderous Blow | NOSTACKING (0) | - | MaterialCreation > 215; VisualProfession == 11 | Hit[27,-130,-276,91]; CastStunNano[144621,50]; TauntNpc[1665] |
| 45203 | Unstable Hadron String | NOSTACKING (0) | - | MaterialCreation > 549; VisualProfession == 11 | Hit[27,-896,-1823,94]; TauntNpc[1865] |
| 45192 | Warmth of the Grave | NOSTACKING (0) | - | MaterialCreation > 867; VisualProfession == 11 | Hit[27,-1270,-2356,95]; TauntNpc[2652] |

