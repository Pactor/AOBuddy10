# Shade (profession 15) — Castable Nano Reference

Generated 2026-09-23T04:34:55Z for the AOBuddy10 bot. **79 nanos.**

## Method / provenance

- **Nano data (ground truth):** `E:\Funcom\OmniCell\OmniCell\Datafiles\nanos.ocp` — OmniCell OMNICELL-CONTENT v3 pack (client 18.8.50_EP1 extraction), loaded through `OmniCell.Core` `NanoLoader.CacheAllNanos` (net10 DLL). 10965 nano formulas in the pack.
- **Names:** joined by nano id against `itemnames.sql` (`itemnames` table).
- **Enums:** stat ids and nano-line names from `AOSharp.Common/GameData/Stat.cs` and `NanoEnums.cs`.
- **Extractor source:** `E:\Funcom\AOBuddy10\tools\mp-nano-extractor` (re-runnable).
- **No web data was used for any id, name or level.**

## Shade-castability criterion

A nano is included when one of its cast `Actions` (`ActionType.ToUse` = 3) has an `EqualTo` requirement on **`Profession`(stat 60) == 15** or **`VisualProfession`(stat 368) == 15**. VisualProfession is how most older profession nanos are locked. Split: 77 via Profession(60), 2 via VisualProfession(368). Every match uses the EqualTo operator.

## Category counts

| Category | Count |
| --- | ---: |
| Summon Weapon / Shield (Creation) | 1 |
| Self / Team Buffs | 70 |
| Heals / HoT | 2 |
| Misc / Utility | 5 |
| Root | 1 |
| **Total** | **79** |

## Summon Weapon / Shield (Creation) (1)

| id | name | nano line (strain) | minLvl | cast reqs | effect |
| ---: | --- | --- | ---: | --- | --- |
| 297342 | Spirit Siphon | SpiritDrain (1035) | - | PercentRemainingHealth < 21; Flags op4 0; Expansion op22 2; Profession == 15; SelectedTargetType op22 16 | CastNano[301114]; SpawnItem[IERC,5,0]; SpawnItem[IERC,10,0]; SpawnItem[IERC,20,0]; SpawnItem[IERC,30,0]; SpawnItem[IERC,40,0]; SpawnItem[IERC,50,0]; SpawnItem[IERC,60,0]; SpawnItem[IERC,70,0]; SpawnItem[IERC,80,0]; SpawnItem[IERC,90,0]; SpawnItem[IERC,100,0]; SpawnItem[IERC,110,0]; SpawnItem[IERC,120,0]; SpawnItem[IERC,130,0]; SpawnItem[IERC,140,0]; SpawnItem[IERC,150,0]; SpawnItem[IERC,160,0]; SpawnItem[IERC,170,0]; SpawnItem[IERC,180,0]; SpawnItem[IERC,190,0]; SpawnItem[IERC,200,0]; SpawnItem[IERC,210,0]; SpawnItem[IERC,220,0]; SpawnItem[IERC,230,0]; SpawnItem[IERC,240,0]; SpawnItem[IERC,250,0]; SpawnItem[IERC,260,0]; SpawnItem[IERC,270,0]; SpawnItem[IERC,280,0]; SpawnItem[IERC,290,0]; SpawnItem[IERC,300,0]; Hit[27,-1,0,0] |

## Self / Team Buffs (70)

| id | name | nano line (strain) | minLvl | cast reqs | effect |
| ---: | --- | --- | ---: | --- | --- |
| 210806 | Double Cover | MultiwieldBuff (245) | 25 | Profession == 15; PsychologicalModification > 258; SensoryImprovement > 234; Level > 24 | Modify MultiMelee +45; Modify Parry +27 |
| 210734 | Knock of the Poltergeist | MartialArtsBuff (209) | 25 | Profession == 15; PsychologicalModification > 182; SensoryImprovement > 158; Level > 24 | Modify Dimach +40; Modify MartialArts +34 |
| 211150 | Piercing Tooth | ShadePiercingBuff (546) | 25 | Profession == 15; PsychologicalModification > 284; SensoryImprovement > 259; Level > 24 | Modify Piercing +54 |
| 210376 | Quintessence of Incapacitation | ShadeProcBuff (521) | 25 | Profession == 15; SpaceTime > 251; BiologicalMetamorphosis > 251; PsychologicalModification > 278; Level > 24; Specialization op22 1 | AddOffProc[10,210373] |
| 210357 | Ritualistic Grasp | ShadeProcBuff (521) | 25 | Profession == 15; SpaceTime > 202; BiologicalMetamorphosis > 202; PsychologicalModification > 228; Level > 24; Specialization op22 1 | AddOffProc[25,210343] |
| 210746 | Sneak | AgilityBuff (195) | 25 | Profession == 15; PsychologicalModification > 218; SensoryImprovement > 194; Level > 24 | Modify EvadeClsC +30; Modify DuckExp +15; Modify DodgeRanged +15; Modify Agility +25; Modify Concealment +20 |
| 210736 | Kick of the Phantom | MartialArtsBuff (209) | 50 | Profession == 15; PsychologicalModification > 447; SensoryImprovement > 396; Level > 49 | Modify Dimach +75; Modify MartialArts +64 |
| 210389 | Primitive Dissipation | ShadeProcBuff (521) | 50 | Profession == 15; SpaceTime > 367; BiologicalMetamorphosis > 367; PsychologicalModification > 408; Level > 49; Specialization op22 1 | AddOffProc[15,210383] |
| 210359 | Ritualistic Caress | ShadeProcBuff (521) | 50 | Profession == 15; SpaceTime > 304; BiologicalMetamorphosis > 304; PsychologicalModification > 334; Level > 49; Specialization op22 1 | AddOffProc[25,210344] |
| 210748 | Mugger | AgilityBuff (195) | 75 | Profession == 15; PsychologicalModification > 503; SensoryImprovement > 461; Level > 74 | Modify EvadeClsC +50; Modify DuckExp +25; Modify DodgeRanged +25; Modify Concealment +45; Modify Agility +45 |
| 211148 | Piercing Gash | ShadePiercingBuff (546) | 75 | Profession == 15; PsychologicalModification > 530; SensoryImprovement > 490; Level > 74 | Modify Piercing +85 |
| 210361 | Ritualistic Embrace | ShadeProcBuff (521) | 75 | Profession == 15; SpaceTime > 495; BiologicalMetamorphosis > 495; PsychologicalModification > 542; Level > 74; Specialization op22 1 | AddOffProc[25,210345] |
| 210808 | Twice the Shield | MultiwieldBuff (245) | 75 | Profession == 15; PsychologicalModification > 601; SensoryImprovement > 555; Level > 74 | Modify MultiMelee +80; Modify Parry +48 |
| 210738 | Hand of the Shadow | MartialArtsBuff (209) | 100 | Profession == 15; PsychologicalModification > 666; SensoryImprovement > 611; Level > 99 | Modify Dimach +94; Modify MartialArts +80 |
| 210391 | Intrinsic Dissipation | ShadeProcBuff (521) | 100 | Profession == 15; SpaceTime > 699; BiologicalMetamorphosis > 699; PsychologicalModification > 765; Level > 99; Specialization op22 2 | AddOffProc[15,210384] |
| 211146 | Probe of Death | ShadePiercingBuff (546) | 100 | Profession == 15; PsychologicalModification > 783; SensoryImprovement > 709; Level > 99 | Modify Piercing +110; Modify MeleeDamageModifier +3; Modify ProjectileDamageModifier +3; Modify EnergyDamageModifier +3; Modify FireDamageModifier +3; Modify ColdDamageModifier +3; Modify ChemicalDamageModifier +3; Modify PoisonDamageModifier +3; Modify RadiationDamageModifier +3 |
| 210378 | Quintessence of Paralyzation | ShadeProcBuff (521) | 100 | Profession == 15; SpaceTime > 706; BiologicalMetamorphosis > 706; PsychologicalModification > 773; Level > 99; Specialization op22 2 | AddOffProc[12,210375] |
| 210363 | Sacrificial Touch | ShadeProcBuff (521) | 100 | Profession == 15; SpaceTime > 645; BiologicalMetamorphosis > 645; PsychologicalModification > 701; Level > 99; Specialization op22 2 | AddOffProc[25,210346] |
| 210365 | Sacrificial Blow | ShadeProcBuff (521) | 135 | Profession == 15; SpaceTime > 848; BiologicalMetamorphosis > 848; PsychologicalModification > 930; Level > 134; Specialization op22 2 | AddOffProc[25,210347] |
| 210750 | Scrounger | AgilityBuff (195) | 135 | Profession == 15; PsychologicalModification > 838; SensoryImprovement > 762; Level > 134 | Modify EvadeClsC +75; Modify DuckExp +35; Modify DodgeRanged +35; Modify Concealment +60; Modify Agility +65 |
| 210740 | Touch of the Specter | MartialArtsBuff (209) | 135 | Profession == 15; PsychologicalModification > 861; SensoryImprovement > 786; Level > 134 | Modify Dimach +110; Modify MartialArts +94 |
| 210810 | Double Fence | MultiwieldBuff (245) | 145 | Profession == 15; PsychologicalModification > 875; SensoryImprovement > 801; Level > 144 | Modify MultiMelee +112; Modify Parry +68 |
| 210393 | Elemental Dissipation | ShadeProcBuff (521) | 155 | Profession == 15; SpaceTime > 926; BiologicalMetamorphosis > 926; PsychologicalModification > 1006; Level > 154; Specialization op22 4 | AddOffProc[15,210385] |
| 211144 | Master Piercer | ShadePiercingBuff (546) | 155 | Profession == 15; PsychologicalModification > 917; SensoryImprovement > 845; Level > 154 | Modify Piercing +127; Modify MultiMelee +4; Modify MeleeDamageModifier +7; Modify ProjectileDamageModifier +7; Modify EnergyDamageModifier +7; Modify FireDamageModifier +7; Modify ColdDamageModifier +7; Modify ChemicalDamageModifier +7; Modify PoisonDamageModifier +7; Modify RadiationDamageModifier +7 |
| 210367 | Sacrificial Grasp | ShadeProcBuff (521) | 165 | Profession == 15; SpaceTime > 939; BiologicalMetamorphosis > 939; PsychologicalModification > 1018; Level > 164; Specialization op22 4 | AddOffProc[25,210348] |
| 210742 | Voice of the Banshee | MartialArtsBuff (209) | 165 | Profession == 15; PsychologicalModification > 962; SensoryImprovement > 890; Level > 164 | Modify Dimach +127; Modify MartialArts +108 |
| 210369 | Sacrificial Caress | ShadeProcBuff (521) | 175 | Profession == 15; SpaceTime > 1010; BiologicalMetamorphosis > 1010; PsychologicalModification > 1095; Level > 174; Specialization op22 4 | AddOffProc[25,210349] |
| 210752 | Prowler | AgilityBuff (195) | 185 | Profession == 15; PsychologicalModification > 1011; SensoryImprovement > 930; Level > 184 | Modify EvadeClsC +70; Modify DuckExp +40; Modify DodgeRanged +40; Modify Agility +80; Modify Concealment +80 |
| 210812 | Duplex Wall | MultiwieldBuff (245) | 195 | Profession == 15; PsychologicalModification > 1031; SensoryImprovement > 947; Level > 194 | Modify MultiMelee +130; Modify Parry +80 |
| 210744 | Kiss of the Vampire | MartialArtsBuff (209) | 195 | Profession == 15; PsychologicalModification > 1046; SensoryImprovement > 959; Level > 194 | Modify Dimach +140; Modify MartialArts +120 |
| 210395 | Primordial Dissipation | ShadeProcBuff (521) | 195 | Profession == 15; SpaceTime > 1043; BiologicalMetamorphosis > 1043; PsychologicalModification > 1137; Level > 194; Specialization op22 4 | AddOffProc[15,210386] |
| 211142 | Puncture of the Tarasque | ShadePiercingBuff (546) | 195 | Profession == 15; PsychologicalModification > 1039; SensoryImprovement > 953; Level > 194 | Modify Piercing +140; Modify MultiMelee +10; Modify SharpObject +80; Modify MeleeDamageModifier +15; Modify ProjectileDamageModifier +15; Modify EnergyDamageModifier +15; Modify FireDamageModifier +15; Modify ColdDamageModifier +15; Modify ChemicalDamageModifier +15; Modify PoisonDamageModifier +15; Modify RadiationDamageModifier +15 |
| 210380 | Quintessence of Petrification | ShadeProcBuff (521) | 195 | Profession == 15; SpaceTime > 1046; BiologicalMetamorphosis > 1046; PsychologicalModification > 1140; Level > 194; Specialization op22 4 | AddOffProc[13,210375] |
| 210371 | Sacrificial Embrace | ShadeProcBuff (521) | 195 | Profession == 15; SpaceTime > 1051; BiologicalMetamorphosis > 1051; PsychologicalModification > 1147; Level > 194; Specialization op22 4 | AddOffProc[25,210350] |
| 224163 | Ceremonial Grasp | ShadeProcBuff (521) | 203 | Profession == 15; SpaceTime > 1158; BiologicalMetamorphosis > 1159; PsychologicalModification > 1284; Level > 202; Specialization op22 8 | AddOffProc[25,224114] |
| 224169 | Quintessence of Transfixion | ShadeProcBuff (521) | 207 | Profession == 15; SpaceTime > 1296; BiologicalMetamorphosis > 1299; PsychologicalModification > 1463; Level > 206; Specialization op22 8 | AddOffProc[14,224153] |
| 224159 | Primal Dissipation | ShadeProcBuff (521) | 209 | Profession == 15; SpaceTime > 1227; BiologicalMetamorphosis > 1232; PsychologicalModification > 1399; Level > 208; Specialization op22 8 | AddOffProc[15,224155] |
| 224165 | Ceremonial Caress | ShadeProcBuff (521) | 212 | Profession == 15; SpaceTime > 1319; BiologicalMetamorphosis > 1324; PsychologicalModification > 1519; Level > 211; Specialization op22 8 | AddOffProc[25,224112] |
| 224161 | Chthonic Dissipation | ShadeProcBuff (521) | 216 | Profession == 15; SpaceTime > 1444; BiologicalMetamorphosis > 1415; PsychologicalModification > 1679; Level > 215; Specialization op22 8 | AddOffProc[15,224154] |
| 224167 | Ceremonial Embrace | ShadeProcBuff (521) | 218 | Profession == 15; SpaceTime > 1499; BiologicalMetamorphosis > 1514; PsychologicalModification > 1760; Level > 217; Specialization op22 8 | AddOffProc[26,224113] |
| 224171 | Quintessence of Stupefication | ShadeProcBuff (521) | 220 | Profession == 15; SpaceTime > 1569; BiologicalMetamorphosis > 1579; PsychologicalModification > 1840; Level > 219; Specialization op22 8 | AddOffProc[15,224151] |
| 210793 | Backpiercer | SneakAttackBuffs (208) | - | Profession == 15; Profession == 14; Profession == 6; PsychologicalModification > 891; SensoryImprovement > 891 | Modify SneakAttack +110 |
| 210797 | Backstabber | SneakAttackBuffs (208) | - | Profession == 15; Profession == 14; Profession == 6; PsychologicalModification > 1034; SensoryImprovement > 1034 | Modify SneakAttack +150 |
| 210791 | Dagger in the Back | SneakAttackBuffs (208) | - | Profession == 15; Profession == 14; Profession == 6; PsychologicalModification > 681; SensoryImprovement > 681 | Modify SneakAttack +90 |
| 210407 | Degeneration of Celerity | ShadeProcBuff (521) | - | Profession == 15; SpaceTime > 1028; BiologicalMetamorphosis > 1028; PsychologicalModification > 1118; Specialization op22 4; EquippedWeapons op107 4 | AddOffProc[100,301121] |
| 224177 | Degeneration of Haste | ShadeProcBuff (521) | - | Profession == 15; SpaceTime > 1539; BiologicalMetamorphosis > 1549; PsychologicalModification > 1800; Specialization op22 8; EquippedWeapons op107 4 | AddOffProc[100,224156] |
| 210401 | Degeneration of Rapidity | ShadeProcBuff (521) | - | Profession == 15; SpaceTime > 224; BiologicalMetamorphosis > 224; PsychologicalModification > 250; Specialization op22 1; EquippedWeapons op107 4 | AddOffProc[100,301120] |
| 210804 | Dual Defender | MultiwieldBuff (245) | - | Profession == 15; PsychologicalModification > 95; SensoryImprovement > 81 | Modify MultiMelee +20; Modify Parry +12 |
| 272371 | Faster than your Shadow | RunspeedBuffs (150) | - | PsychologicalModification > 749; SensoryImprovement > 749; Profession == 15 | Modify RunSpeed +250 |
| 210323 | Fervor of the Devotee | FastAttackBuffs (519) | - | Profession == 15; Profession == 14; Profession == 2; Profession == 9; PsychologicalModification > 308; SensoryImprovement > 308 | Modify FastAttack +70 |
| 210327 | Fervor of the Disciple | FastAttackBuffs (519) | - | Profession == 15; Profession == 14; Profession == 2; Profession == 9; PsychologicalModification > 789; SensoryImprovement > 789 | Modify FastAttack +110 |
| 210329 | Fervor of the Fanatic | FastAttackBuffs (519) | - | Profession == 15; Profession == 14; Profession == 2; Profession == 9; PsychologicalModification > 878; SensoryImprovement > 878 | Modify FastAttack +135 |
| 210321 | Fervor of the Henchman | FastAttackBuffs (519) | - | Profession == 15; Profession == 14; Profession == 2; Profession == 9; PsychologicalModification > 81; SensoryImprovement > 81 | Modify FastAttack +30 |
| 210325 | Fervor of the Minion | FastAttackBuffs (519) | - | Profession == 15; Profession == 14; Profession == 2; Profession == 9; PsychologicalModification > 555; SensoryImprovement > 555 | Modify FastAttack +90 |
| 210331 | Fervor of the Zealot | FastAttackBuffs (519) | - | Profession == 15; Profession == 14; Profession == 2; Profession == 9; PsychologicalModification > 949; SensoryImprovement > 949 | Modify FastAttack +150 |
| 210787 | Footpad Apprentice | SneakAttackBuffs (208) | - | Profession == 15; Profession == 14; Profession == 6; PsychologicalModification > 141; SensoryImprovement > 123 | Modify SneakAttack +30 |
| 273393 | Improved Prowler | AgilityBuff (195) | - | Profession == 15; PsychologicalModification > 1429; SensoryImprovement > 1243; NanoFocusLevel op22 64 | Modify EvadeClsC +140; Modify DuckExp +80; Modify DodgeRanged +80; Modify Agility +110; Modify Concealment +100; Modify RunSpeed +350 |
| 270804 | Improved Puncture of the Tarasque | ShadePiercingBuff (546) | - | Profession == 15; PsychologicalModification > 1429; SensoryImprovement > 1243; NanoFocusLevel op22 64 | Modify Piercing +200; Modify MultiMelee +30; Modify SharpObject +140; Modify MeleeDamageModifier +45; Modify ProjectileDamageModifier +45; Modify EnergyDamageModifier +45; Modify FireDamageModifier +45; Modify ColdDamageModifier +45; Modify ChemicalDamageModifier +45; Modify PoisonDamageModifier +45; Modify RadiationDamageModifier +45 |
| 266310 | Pierce Nerves | NOSTACKING (0) | - | Expansion op22 32; MaterialCreation > 799; Profession == 1; SpaceTime > 799; Profession == 15 | Modify Piercing -1000 |
| 210355 | Ritualistic Blow | ShadeProcBuff (521) | - | Profession == 15; SpaceTime > 105; BiologicalMetamorphosis > 105; PsychologicalModification > 123 | AddOffProc[25,210342] |
| 210353 | Ritualistic Touch | ShadeProcBuff (521) | - | Profession == 15; SpaceTime > 31; BiologicalMetamorphosis > 31; PsychologicalModification > 37 | AddOffProc[25,210341] |
| 210387 | Rudimentary Dissipation | ShadeProcBuff (521) | - | Profession == 15; SpaceTime > 129; BiologicalMetamorphosis > 129; PsychologicalModification > 148 | AddOffProc[15,210382] |
| 273395 | Shadow in the Night | ConcealmentBuff (193) | - | PsychologicalModification > 1429; SensoryImprovement > 1243; Profession == 15; NanoFocusLevel op22 64 | Modify Concealment +750 |
| 211152 | Sharpen Dagger | ShadePiercingBuff (546) | - | Profession == 15; PsychologicalModification > 124; SensoryImprovement > 107 | Modify Piercing +29 |
| 210795 | Shuffle of the Rogue | SneakAttackBuffs (208) | - | Profession == 15; Profession == 14; Profession == 6; PsychologicalModification > 988; SensoryImprovement > 988 | Modify SneakAttack +135 |
| 210789 | Silent Dagger | SneakAttackBuffs (208) | - | Profession == 15; Profession == 14; Profession == 6; PsychologicalModification > 308; SensoryImprovement > 308 | Modify SneakAttack +70 |
| 210732 | Slap of the Zombie | MartialArtsBuff (209) | - | Profession == 15; PsychologicalModification > 40; SensoryImprovement > 35 | Modify Dimach +19; Modify MartialArts +16 |
| 275843 | Triple Wall | MultiwieldBuff (245) | - | Profession == 15; PsychologicalModification > 1429; SensoryImprovement > 1243; NanoFocusLevel op22 64 | Modify MultiMelee +180; Modify Parry +100; Modify MartialArts +30; Modify AddAllOff +30; Modify AddAllDef +30; Modify MeleeInit +150 |
| 211154 | Whetstone Effect | ShadePiercingBuff (546) | - | Profession == 15; PsychologicalModification > 4; SensoryImprovement > 4 | Modify Piercing +12 |
| 301593 | Winding Serpent | AADBuffs (1002) | - | PsychologicalModification > 337; SensoryImprovement > 337; VisualProfession == 15; Expansion op22 32 | Modify AddAllDef +25 |

## Heals / HoT (2)

| id | name | nano line (strain) | minLvl | cast reqs | effect |
| ---: | --- | --- | ---: | --- | --- |
| 301895 | Dissolving Vitality | HealthDrain (844) | - | BiologicalMetamorphosis > 699; SpaceTime > 699; Profession == 15 | CastNano[301894]; Hit[27,-250,-250,91]; Hit[27,-250,-250,91] |
| 273390 | Sneaking Health Drain | HealthDrain (844) | - | BiologicalMetamorphosis > 1554; SpaceTime > 1544; Profession == 15; NanoFocusLevel op22 64 | CastNano[273391]; Hit[27,-950,-950,91]; Hit[27,-950,-950,91] |

## Misc / Utility (5)

| id | name | nano line (strain) | minLvl | cast reqs | effect |
| ---: | --- | --- | ---: | --- | --- |
| 266300 | Shade's Caress | NemesisNanoPrograms (1000) | 100 | Expansion op22 32; PsychologicalModification > 799; Profession == 15; SensoryImprovement > 799; Level > 99 | CastNano[275242] |
| 275841 | Basic Nanite Depravation | WeaponEffectAdd_On2 (861) | - | Profession == 15; SpaceTime > 1334; BiologicalMetamorphosis > 1444; PsychologicalModification > 1509; NanoFocusLevel op22 64 | AddOffProc[21,275842] |
| 275839 | Nanite Depravation | WeaponEffectAdd_On2 (861) | - | Profession == 15; SpaceTime > 1544; BiologicalMetamorphosis > 1554; PsychologicalModification > 1629; NanoFocusLevel op22 64 | AddOffProc[25,275840] |
| 301160 | Smoke Bomb | EmergencySneak (1049) | - | NumFightingOpponents == 0; Flags op123 2; Flags op4 0; Profession == 15; Concealment > 599; Expansion op22 2 | (2 non-effect functions only) |
| 225281 | Symbiosis | NOSTACKING (0) | - | Profession == 15; Flags op4 0 | CastNano[209986]; CastNano[209987]; CastNano[210019]; CastNano[210020] |

## Root (1)

| id | name | nano line (strain) | minLvl | cast reqs | effect |
| ---: | --- | --- | ---: | --- | --- |
| 281239 | Release Me Now | SelfRoot_SnareResistBuff (283) | - | VisualProfession == 9; VisualProfession == 14; VisualProfession == 15; VisualProfession == 1; SensoryImprovement > 99; PsychologicalModification > 99; Expansion op22 32 | RemoveNanoStrain[145]; RemoveNanoStrain[146]; ResistNanoStrain[145,100]; ResistNanoStrain[146,100] |

