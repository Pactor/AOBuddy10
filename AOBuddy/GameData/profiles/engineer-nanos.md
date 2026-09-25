# Engineer (profession 3) — Castable Nano Reference

Generated 2026-09-23T04:36:03Z for the AOBuddy10 bot. **288 nanos.**

## Method / provenance

- **Nano data (ground truth):** `E:\Funcom\OmniCell\OmniCell\Datafiles\nanos.ocp` — OmniCell OMNICELL-CONTENT v3 pack (client 18.8.50_EP1 extraction), loaded through `OmniCell.Core` `NanoLoader.CacheAllNanos` (net10 DLL). 10965 nano formulas in the pack.
- **Names:** joined by nano id against `itemnames.sql` (`itemnames` table).
- **Enums:** stat ids and nano-line names from `AOSharp.Common/GameData/Stat.cs` and `NanoEnums.cs`.
- **Extractor source:** `E:\Funcom\AOBuddy10\tools\mp-nano-extractor` (re-runnable).
- **No web data was used for any id, name or level.**

## Engineer-castability criterion

A nano is included when one of its cast `Actions` (`ActionType.ToUse` = 3) has an `EqualTo` requirement on **`Profession`(stat 60) == 3** or **`VisualProfession`(stat 368) == 3**. VisualProfession is how most older profession nanos are locked. Split: 181 via Profession(60), 107 via VisualProfession(368). Every match uses the EqualTo operator.

## Category counts

| Category | Count |
| --- | ---: |
| Pets - Robot | 90 |
| Pets - Towers / Deployables | 1 |
| Pet Heals / Repair | 10 |
| Pet Buffs / Pet Utility | 36 |
| Auras - Team (friendly) | 15 |
| Auras - Debuff (hostile) | 14 |
| Special Attack Absorbers | 14 |
| Self / Team Buffs | 87 |
| Heals / HoT | 1 |
| Travel / Recall | 4 |
| Misc / Utility | 16 |
| **Total** | **288** |

## Pets - Robot (90)

| id | name | nano line (strain) | minLvl | cast reqs | effect |
| ---: | --- | --- | ---: | --- | --- |
| 223325 | Prototype Predator M-30 | NOSTACKING (0) | 100 | MaterialCreation > 687; SpaceTime > 687; Profession == 3; Cash > 1592; Level > 99; Expansion op22 2; Specialization op22 2 | Hit[61,-1592,-1592,0]; SpawnItem[EGPR,100,0] |
| 301855 | Predator M-30 | NOSTACKING (0) | 150 | MaterialCreation > 889; SpaceTime > 889; Profession == 3; Cash > 2591; Level > 149; Expansion op22 2; Specialization op22 2 | Hit[61,-2592,-2592,0]; SpawnItem[EIPR,150,0] |
| 223313 | Slayerdroid Annihilator | NOSTACKING (0) | 175 | MaterialCreation > 1095; SpaceTime > 1094; Profession == 3; Cash > 3208; Level > 174; Expansion op22 2; Specialization op22 4 | Hit[61,-3208,-3208,0]; SpawnItem[ENSY,201,0] |
| 223327 | Upgraded Predator M-30 | NOSTACKING (0) | 175 | MaterialCreation > 1086; SpaceTime > 1086; Profession == 3; Cash > 2872; Level > 174; Expansion op22 2; Specialization op22 4 | Hit[61,-2872,-2872,0]; SpawnItem[ENUP,175,0] |
| 223329 | Advanced Predator M-30 | NOSTACKING (0) | 203 | MaterialCreation > 1284; SpaceTime > 1284; Profession == 3; Cash > 3240; Level > 202; Expansion op22 2; Specialization op22 8 | Hit[61,-3240,-3240,0]; SpawnItem[ENAV,203,0] |
| 223315 | Devastator Drone | NOSTACKING (0) | 205 | MaterialCreation > 1254; SpaceTime > 1249; Profession == 3; Cash > 3272; Level > 204; Expansion op22 2; Specialization op22 8 | Hit[61,-3272,-3272,0]; SpawnItem[ENDV,205,0] |
| 223331 | Semi-Sentient Predator M-30 | NOSTACKING (0) | 207 | MaterialCreation > 1463; SpaceTime > 1462; Profession == 3; Cash > 3304; Level > 206; Expansion op22 2; Specialization op22 8 | Hit[61,-3304,-3304,0]; SpawnItem[EGSE,207,0] |
| 223317 | Battlefield Devastator Drone | NOSTACKING (0) | 209 | MaterialCreation > 1403; SpaceTime > 1400; Profession == 3; Cash > 3336; Level > 208; Expansion op22 2; Specialization op22 8 | Hit[61,-3336,-3336,0]; SpawnItem[EGBA,209,0] |
| 223333 | Military-Grade Predator M-30 | NOSTACKING (0) | 211 | MaterialCreation > 1642; SpaceTime > 1640; Profession == 3; Cash > 3368; Level > 210; Expansion op22 2; Specialization op22 8 | Hit[61,-3368,-3368,0]; SpawnItem[EIMA,211,0] |
| 223319 | Fieldsweeper Devastator Drone | NOSTACKING (0) | 213 | MaterialCreation > 1582; SpaceTime > 1578; Profession == 3; Cash > 3400; Level > 212; Expansion op22 2; Specialization op22 8 | Hit[61,-3400,-3400,0]; SpawnItem[ENFE,213,0] |
| 223335 | Marauder M-45 | NOSTACKING (0) | 215 | MaterialCreation > 1721; SpaceTime > 1718; Profession == 3; Cash > 3432; Level > 214; Expansion op22 2; Specialization op22 8 | Hit[61,-3432,-3432,0]; SpawnItem[EGMR,215,0] |
| 223321 | Desolator Assault Drone | NOSTACKING (0) | 217 | MaterialCreation > 1762; SpaceTime > 1756; Profession == 3; Cash > 3464; Level > 216; Expansion op22 2; Specialization op22 8 | Hit[61,-3400,-3400,0]; SpawnItem[EGDE,217,0] |
| 223337 | Military-Grade Marauder M-45 | NOSTACKING (0) | 219 | MaterialCreation > 1850; SpaceTime > 1846; Profession == 3; Cash > 3496; Level > 218; Expansion op22 2; Specialization op22 8 | Hit[61,-3496,-3496,0]; SpawnItem[MIMD,219,0] |
| 223323 | Widowmaker Battle Drone | NOSTACKING (0) | 220 | MaterialCreation > 1895; SpaceTime > 1890; Profession == 3; Cash > 3512; Level > 219; Expansion op22 2; Specialization op22 8 | Hit[61,-3512,-3512,0]; SpawnItem[EGDS,220,0] |
| 45730 | Advanced Android | NOSTACKING (0) | - | MaterialCreation > 190; SpaceTime > 190; Profession == 3; Cash > 291 | Hit[61,-292,-292,0]; SpawnItem[EGAN,37,0] |
| 45731 | Advanced Automaton | NOSTACKING (0) | - | MaterialCreation > 68; SpaceTime > 68; Profession == 3 | SpawnItem[EGAU,11,0] |
| 45732 | Advanced Gladiatorbot | NOSTACKING (0) | - | MaterialCreation > 367; SpaceTime > 367; Profession == 3; Cash > 555 | Hit[61,-556,-556,0]; SpawnItem[ENGA,70,0] |
| 45733 | Advanced Guardbot | NOSTACKING (0) | - | MaterialCreation > 538; SpaceTime > 538; Profession == 3; Cash > 819 | Hit[61,-820,-820,0]; SpawnItem[ENGU,103,0] |
| 45734 | Advanced Warbot | NOSTACKING (0) | - | MaterialCreation > 679; SpaceTime > 679; Profession == 3; Cash > 1083 | Hit[61,-1084,-1084,0]; SpawnItem[ENWA,136,0] |
| 45735 | Advanced Warmachine | NOSTACKING (0) | - | MaterialCreation > 791; SpaceTime > 791; Profession == 3; Cash > 1371 | Hit[61,-1372,-1372,0]; SpawnItem[EGWA,172,0] |
| 45736 | Android | NOSTACKING (0) | - | MaterialCreation > 166; SpaceTime > 166; Profession == 3; Cash > 243 | Hit[61,-244,-244,0]; SpawnItem[EGAN,31,0] |
| 45737 | Automaton | NOSTACKING (0) | - | MaterialCreation > 46; SpaceTime > 46; Profession == 3 | SpawnItem[EGAU,7,0] |
| 45723 | Common Android | NOSTACKING (0) | - | MaterialCreation > 153; SpaceTime > 153; Profession == 3; Cash > 219 | Hit[61,-220,-220,0]; SpawnItem[ENAN,28,0] |
| 45724 | Common Automaton | NOSTACKING (0) | - | MaterialCreation > 39; SpaceTime > 39; Profession == 3 | SpawnItem[ENAU,6,0] |
| 45725 | Common Gladiatorbot | NOSTACKING (0) | - | MaterialCreation > 313; SpaceTime > 313; Profession == 3; Cash > 483 | Hit[61,-484,-484,0]; SpawnItem[ENGA,61,0] |
| 45726 | Common Guardbot | NOSTACKING (0) | - | MaterialCreation > 493; SpaceTime > 493; Profession == 3; Cash > 747 | Hit[61,-748,-748,0]; SpawnItem[ENGU,94,0] |
| 45727 | Common Warbot | NOSTACKING (0) | - | MaterialCreation > 643; SpaceTime > 643; Profession == 3; Cash > 1011 | Hit[61,-1012,-1012,0]; SpawnItem[ENWA,127,0] |
| 45728 | Common Warmachine | NOSTACKING (0) | - | MaterialCreation > 764; SpaceTime > 764; Profession == 3; Cash > 1299 | Hit[61,-1300,-1300,0]; SpawnItem[EGWA,163,0] |
| 45729 | Decommissioned Wardroid | NOSTACKING (0) | - | MaterialCreation > 826; SpaceTime > 826; Profession == 3; Cash > 1467 | Hit[61,-1468,-1468,0]; SpawnItem[ENWR,184,0] |
| 45712 | Feeble Android | NOSTACKING (0) | - | MaterialCreation > 98; SpaceTime > 98; Profession == 3; Cash > 131 | Hit[61,-132,-132,0]; SpawnItem[ENAN,17,0] |
| 43325 | Feeble Automaton | NOSTACKING (0) | - | SpaceTime > 4; MaterialCreation > 4; Profession == 3 | SpawnItem[ENAU,1,0] |
| 45713 | Feeble Gladiatorbot | NOSTACKING (0) | - | SpaceTime > 229; MaterialCreation > 229; Profession == 3; Cash > 363 | Hit[61,-364,-364,0]; SpawnItem[ENGA,46,0] |
| 45714 | Feeble Guardbot | NOSTACKING (0) | - | MaterialCreation > 418; SpaceTime > 418; Profession == 3; Cash > 627 | Hit[61,-628,-628,0]; SpawnItem[ENGU,79,0] |
| 45715 | Feeble Warbot | NOSTACKING (0) | - | MaterialCreation > 583; SpaceTime > 583; Profession == 3; Cash > 891 | Hit[61,-892,-892,0]; SpawnItem[ENWA,112,0] |
| 45716 | Feeble Warmachine | NOSTACKING (0) | - | MaterialCreation > 719; SpaceTime > 719; Profession == 3; Cash > 1179 | Hit[61,-1180,-1180,0]; SpawnItem[EGWA,148,0] |
| 45717 | Flawed Android | NOSTACKING (0) | - | MaterialCreation > 138; SpaceTime > 138; Profession == 3; Cash > 195 | Hit[61,-196,-196,0]; SpawnItem[ENAN,25,0] |
| 45718 | Flawed Automaton | NOSTACKING (0) | - | MaterialCreation > 32; SpaceTime > 32; Profession == 3 | SpawnItem[ENAU,5,0] |
| 45719 | Flawed Gladiatorbot | NOSTACKING (0) | - | MaterialCreation > 295; SpaceTime > 295; Profession == 3; Cash > 459 | Hit[61,-460,-460,0]; SpawnItem[ENGA,58,0] |
| 45720 | Flawed Guardbot | NOSTACKING (0) | - | MaterialCreation > 478; SpaceTime > 478; Profession == 3; Cash > 723 | Hit[61,-724,-724,0]; SpawnItem[ENGU,91,0] |
| 45721 | Flawed Warbot | NOSTACKING (0) | - | MaterialCreation > 631; SpaceTime > 631; Profession == 3; Cash > 987 | Hit[61,-988,-988,0]; SpawnItem[ENWA,124,0] |
| 45722 | Flawed Warmachine | NOSTACKING (0) | - | MaterialCreation > 755; SpaceTime > 755; Profession == 3; Cash > 1275 | Hit[61,-1276,-1276,92]; SpawnItem[EGWA,160,0] |
| 45701 | Gladiatorbot | NOSTACKING (0) | - | MaterialCreation > 331; SpaceTime > 331; Profession == 3; Cash > 507 | Hit[61,-508,-508,0]; SpawnItem[ENGA,64,0] |
| 45702 | Guardbot | NOSTACKING (0) | - | MaterialCreation > 508; SpaceTime > 508; Profession == 3; Cash > 771 | Hit[61,-772,-772,0]; SpawnItem[ENGU,97,0] |
| 45703 | Inferior Android | NOSTACKING (0) | - | MaterialCreation > 128; SpaceTime > 128; Profession == 3; Cash > 179 | Hit[61,-180,-180,0]; SpawnItem[ENAN,23,0] |
| 45704 | Inferior Automaton | NOSTACKING (0) | - | MaterialCreation > 25; SpaceTime > 25; Profession == 3 | SpawnItem[ENAU,4,0] |
| 45705 | Inferior Gladiatorbot | NOSTACKING (0) | - | MaterialCreation > 277; SpaceTime > 277; Profession == 3; Cash > 435 | Hit[61,-436,-436,0]; SpawnItem[ENGA,55,0] |
| 45706 | Inferior Guardbot | NOSTACKING (0) | - | MaterialCreation > 463; SpaceTime > 463; Profession == 3; Cash > 699 | Hit[61,-700,-700,0]; SpawnItem[ENGU,88,0] |
| 45707 | Inferior Warbot | NOSTACKING (0) | - | MaterialCreation > 619; SpaceTime > 619; Profession == 3; Cash > 963 | Hit[61,-964,-964,0]; SpawnItem[ENWA,121,0] |
| 45708 | Inferior Warmachine | NOSTACKING (0) | - | MaterialCreation > 746; SpaceTime > 746; Profession == 3; Cash > 1251 | Hit[61,-1252,-1252,0]; SpawnItem[EGWA,157,0] |
| 45709 | Lesser Android | NOSTACKING (0) | - | MaterialCreation > 118; SpaceTime > 118; Profession == 3; Cash > 163 | Hit[61,-164,-164,0]; SpawnItem[ENAN,21,0] |
| 45710 | Lesser Automaton | NOSTACKING (0) | - | MaterialCreation > 18; SpaceTime > 18; Profession == 3 | SpawnItem[ENAU,3,0] |
| 45711 | Lesser Gladiatorbot | NOSTACKING (0) | - | MaterialCreation > 261; SpaceTime > 261; Profession == 3; Cash > 411 | Hit[61,-412,-412,0]; SpawnItem[ENGA,52,0] |
| 45686 | Lesser Guardbot | NOSTACKING (0) | - | MaterialCreation > 448; SpaceTime > 448; Profession == 3; Cash > 675 | Hit[61,-676,-676,0]; SpawnItem[ENGU,85,0] |
| 45687 | Lesser Warbot | NOSTACKING (0) | - | MaterialCreation > 607; SpaceTime > 607; Profession == 3; Cash > 939 | Hit[61,-940,-940,0]; SpawnItem[ENWA,118,0] |
| 45688 | Lesser Warmachine | NOSTACKING (0) | - | MaterialCreation > 737; SpaceTime > 737; Profession == 3; Cash > 1227 | Hit[61,-1228,-1228,0]; SpawnItem[EGWA,154,0] |
| 45689 | Military-Grade Warbot | NOSTACKING (0) | - | MaterialCreation > 710; SpaceTime > 710; Profession == 3; Cash > 1155 | Hit[61,-1156,-1156,0]; SpawnItem[ENWA,145,0] |
| 45690 | Military-Grade Warmachine | NOSTACKING (0) | - | MaterialCreation > 818; SpaceTime > 818; Profession == 3; Cash > 1443 | Hit[61,-1444,-1444,0]; SpawnItem[EGWA,181,0] |
| 45691 | Patchwork Android | NOSTACKING (0) | - | MaterialCreation > 108; SpaceTime > 108; Profession == 3; Cash > 147 | Hit[61,-148,-148,0]; SpawnItem[ENAN,19,0] |
| 45692 | Patchwork Automaton | NOSTACKING (0) | - | MaterialCreation > 11; SpaceTime > 11; Profession == 3 | SpawnItem[ENAU,2,0] |
| 45693 | Patchwork Gladiatorbot | NOSTACKING (0) | - | MaterialCreation > 245; SpaceTime > 245; Profession == 3; Cash > 387 | Hit[61,-388,-388,0]; SpawnItem[ENGA,49,0] |
| 45694 | Patchwork Guardbot | NOSTACKING (0) | - | MaterialCreation > 433; SpaceTime > 433; Profession == 3; Cash > 651 | Hit[61,-652,-652,0]; SpawnItem[ENGU,82,0] |
| 45695 | Patchwork Warbot | NOSTACKING (0) | - | MaterialCreation > 595; SpaceTime > 595; Profession == 3; Cash > 915 | Hit[61,-916,-916,0]; SpawnItem[ENWA,115,0] |
| 45696 | Patchwork Warmachine | NOSTACKING (0) | - | MaterialCreation > 728; SpaceTime > 728; Profession == 3; Cash > 1204 | Hit[61,-1204,-1204,0]; SpawnItem[EGWA,151,0] |
| 45697 | Perfected Android | NOSTACKING (0) | - | MaterialCreation > 202; SpaceTime > 202; Profession == 3; Cash > 315 | Hit[61,-316,-316,0]; SpawnItem[EGAN,40,0] |
| 45698 | Perfected Automaton | NOSTACKING (0) | - | MaterialCreation > 78; SpaceTime > 78; Profession == 3 | SpawnItem[EGAU,13,0] |
| 45699 | Perfected Gladiatorbot | NOSTACKING (0) | - | MaterialCreation > 385; SpaceTime > 385; Profession == 3; Cash > 579 | Hit[61,-580,-580,0]; SpawnItem[ENGA,73,0] |
| 45700 | Perfected Guardbot | NOSTACKING (0) | - | MaterialCreation > 553; SpaceTime > 553; Profession == 3; Cash > 843 | Hit[61,-844,-844,0]; SpawnItem[ENGU,106,0] |
| 45677 | Perfected Warbot | NOSTACKING (0) | - | MaterialCreation > 691; SpaceTime > 691; Profession == 3; Cash > 1107 | Hit[61,-1108,-1108,0]; SpawnItem[ENWA,139,0] |
| 45678 | Perfected Warmachine | NOSTACKING (0) | - | MaterialCreation > 800; SpaceTime > 800; Profession == 3; Cash > 1395 | Hit[61,-1396,-1396,0]; SpawnItem[EGWA,175,0] |
| 275815 | Ravening M-60 | NOSTACKING (0) | - | MaterialCreation > 1959; SpaceTime > 1957; Profession == 3; Cash > 3694; NanoFocusLevel op22 64 | Hit[61,-3694,-3694,0]; SpawnItem[RAM6,220,0] |
| 45679 | Reactivated Wardroid | NOSTACKING (0) | - | MaterialCreation > 835; SpaceTime > 835; Profession == 3; Cash > 1492 | Hit[61,-1492,-1492,0]; SpawnItem[ENWR,187,0] |
| 45680 | Semi-Sentient Android | NOSTACKING (0) | - | MaterialCreation > 214; SpaceTime > 214; Profession == 3; Cash > 339 | Hit[61,-340,-340,0]; SpawnItem[EGAN,43,0] |
| 45681 | Semi-Sentient Automaton | NOSTACKING (0) | - | MaterialCreation > 88; SpaceTime > 88; Profession == 3 | SpawnItem[EGAU,15,0] |
| 45682 | Semi-Sentient Gladiatorbot | NOSTACKING (0) | - | MaterialCreation > 403; SpaceTime > 403; Profession == 3; Cash > 603 | Hit[61,-604,-604,0]; SpawnItem[ENGA,76,0] |
| 45683 | Semi-Sentient Guardbot | NOSTACKING (0) | - | MaterialCreation > 568; SpaceTime > 568; Profession == 3; Cash > 867 | Hit[61,-868,-868,0]; SpawnItem[ENGU,109,0] |
| 45684 | Semi-Sentient Warbot | NOSTACKING (0) | - | MaterialCreation > 701; SpaceTime > 701; Profession == 3; Cash > 1131 | Hit[61,-1132,-1132,0]; SpawnItem[ENWA,142,0] |
| 45685 | Semi-Sentient Wardroid | NOSTACKING (0) | - | MaterialCreation > 844; SpaceTime > 844; Profession == 3; Cash > 1515 | Hit[61,-1516,-1516,0]; SpawnItem[ENWR,190,0] |
| 45670 | Semi-Sentient Warmachine | NOSTACKING (0) | - | MaterialCreation > 809; SpaceTime > 809; Profession == 3; Cash > 1419 | Hit[61,-1420,-1420,0]; SpawnItem[EGWA,178,0] |
| 45671 | Slayerdroid Guardian | NOSTACKING (0) | - | MaterialCreation > 873; SpaceTime > 873; Profession == 3; Cash > 1595 | Hit[61,-1596,-1596,0]; SpawnItem[ENSA,200,0] |
| 45672 | Slayerdroid Protector | NOSTACKING (0) | - | MaterialCreation > 852; SpaceTime > 852; Profession == 3; Cash > 1539 | Hit[61,-1540,-1540,0]; SpawnItem[ENSL,193,0] |
| 45673 | Slayerdroid Sentinel | NOSTACKING (0) | - | MaterialCreation > 867; SpaceTime > 867; Profession == 3; Cash > 1579 | Hit[61,-1580,-1580,0]; SpawnItem[ENSA,198,0] |
| 45674 | Slayerdroid Warden | NOSTACKING (0) | - | MaterialCreation > 861; SpaceTime > 861; Profession == 3; Cash > 1563 | Hit[61,-1564,-1564,0]; SpawnItem[ENSL,196,0] |
| 45675 | Upgraded Android | NOSTACKING (0) | - | MaterialCreation > 178; SpaceTime > 178; Profession == 3; Cash > 267 | Hit[61,-268,-268,0]; SpawnItem[EGAN,34,0] |
| 45676 | Upgraded Automaton | NOSTACKING (0) | - | MaterialCreation > 57; SpaceTime > 57; Profession == 3 | SpawnItem[EGAU,9,0] |
| 45664 | Upgraded Gladiatorbot | NOSTACKING (0) | - | MaterialCreation > 349; SpaceTime > 349; Profession == 3; Cash > 531 | Hit[61,-532,-532,0]; SpawnItem[ENGA,67,0] |
| 45665 | Upgraded Guardbot | NOSTACKING (0) | - | MaterialCreation > 523; SpaceTime > 523; Profession == 3; Cash > 795 | Hit[61,-796,-796,0]; SpawnItem[ENGU,100,0] |
| 45666 | Upgraded Warbot | NOSTACKING (0) | - | MaterialCreation > 667; SpaceTime > 667; Profession == 3; Cash > 1059 | Hit[61,-1060,-1060,0]; SpawnItem[ENWA,133,0] |
| 45667 | Upgraded Warmachine | NOSTACKING (0) | - | MaterialCreation > 782; SpaceTime > 782; Profession == 3; Cash > 1347 | Hit[61,-1348,-1348,0]; SpawnItem[EGWA,169,0] |
| 45668 | Warbot | NOSTACKING (0) | - | MaterialCreation > 655; SpaceTime > 655; Profession == 3; Cash > 1035 | Hit[61,-1036,-1036,0]; SpawnItem[ENWA,130,0] |
| 45669 | Warmachine | NOSTACKING (0) | - | MaterialCreation > 773; SpaceTime > 773; Profession == 3; Cash > 1323 | Hit[61,-1324,-1324,0]; SpawnItem[EGWA,166,0] |

## Pets - Towers / Deployables (1)

| id | name | nano line (strain) | minLvl | cast reqs | effect |
| ---: | --- | --- | ---: | --- | --- |
| 274369 | Jamming Tower | NOSTACKING (0) | - | MaterialCreation > 19; SpaceTime > 19; Profession == 3 | SpawnItem[EGJA,1,0] |

## Pet Heals / Repair (10)

| id | name | nano line (strain) | minLvl | cast reqs | effect |
| ---: | --- | --- | ---: | --- | --- |
| 223305 | Synchronized Energy Spike | NOSTACKING (0) | 185 | Profession == 3; MaterialCreation > 1118; SpaceTime > 1118; Level > 184; Expansion op22 2; Specialization op22 4; Breed == 7 | ReduceNanoStrainDuration[145,1103]; ReduceNanoStrainDuration[146,1103]; SystemText[Your pet moves more freely.]; Hit[27,350,350,0] |
| 203867 | Energized Casing of the Faithful Servant | NOSTACKING (0) | 195 | Profession == 3; MaterialCreation > 1037; SpaceTime > 1037; Level > 194; Breed == 7 | ReduceNanoStrainDuration[145,1387]; ReduceNanoStrainDuration[146,1387]; SystemText[Your pet moves more freely.]; Hit[27,350,350,0] |
| 223339 | Synchronized Capacitor Overload | NOSTACKING (0) | 214 | Profession == 3; MaterialCreation > 1776; SpaceTime > 1773; Level > 213; Expansion op22 2; Specialization op22 8; Breed == 7 | ReduceNanoStrainDuration[145,1393]; ReduceNanoStrainDuration[146,1393]; SystemText[Your pet moves more freely.]; Hit[27,350,350,0] |
| 116791 | A Maker's Touch | NOSTACKING (0) | - | MaterialCreation > 841; SpaceTime > 841; BiologicalMetamorphosis > 760; VisualProfession == 3; Breed == 7 | Hit[27,1285,2267,0] |
| 116796 | Field Workshop | NOSTACKING (0) | - | MaterialCreation > 571; SpaceTime > 571; BiologicalMetamorphosis > 517; VisualProfession == 3; Breed == 7 | Hit[27,800,1411,0] |
| 116795 | Intricate Repairs | NOSTACKING (0) | - | MaterialCreation > 737; SpaceTime > 737; BiologicalMetamorphosis > 665; VisualProfession == 3; Breed == 7 | Hit[27,1063,1875,0] |
| 116794 | Patchy Repairs | NOSTACKING (0) | - | MaterialCreation > 170; SpaceTime > 170; BiologicalMetamorphosis > 146; VisualProfession == 3; Breed == 7 | Hit[27,243,412,0] |
| 116793 | Quick Fix | NOSTACKING (0) | - | MaterialCreation > 83; SpaceTime > 83; BiologicalMetamorphosis > 69; VisualProfession == 3; Breed == 7 | Hit[27,120,199,0] |
| 116792 | Rebuild Casing | NOSTACKING (0) | - | MaterialCreation > 405; SpaceTime > 405; BiologicalMetamorphosis > 359; VisualProfession == 3; Breed == 7 | Hit[27,576,992,0] |
| 116797 | Recondition Parts | NOSTACKING (0) | - | MaterialCreation > 245; SpaceTime > 245; BiologicalMetamorphosis > 220; VisualProfession == 3; Breed == 7 | Hit[27,398,611,0] |

## Pet Buffs / Pet Utility (36)

| id | name | nano line (strain) | minLvl | cast reqs | effect |
| ---: | --- | --- | ---: | --- | --- |
| 203871 | Energize Shell | NOSTACKING (0) | 25 | Profession == 3; MaterialCreation > 238; SpaceTime > 238; Level > 24; Breed == 7 | ReduceNanoStrainDuration[145,414]; ReduceNanoStrainDuration[146,414]; SystemText[Your pet moves more freely.] |
| 205231 | Monitor Combat Array | PetShortTermDamageBuffs (225) | 25 | MaterialCreation > 223; MaterialMetamorphosis > 223; SpaceTime > 223; Profession == 3; Level > 24; Breed == 7; NPCFamily == 95 | Modify AddAllOff +79; Modify AddAllDef +14 |
| 205233 | Enhance Combat Array | PetShortTermDamageBuffs (225) | 50 | MaterialCreation > 334; MaterialMetamorphosis > 334; SpaceTime > 334; Profession == 3; Level > 49; Breed == 7; NPCFamily == 95 | Modify AddAllOff +120; Modify AddAllDef +21 |
| 204372 | Intrusive Aura Cancellation | NOSTACKING (0) | 50 | Profession == 3; Level > 49; SpaceTime > 364; MaterialMetamorphosis > 364; Breed == 7 | ReduceNanoStrainDuration[288,900000] |
| 204362 | Intrusive Aura of Entanglement | EngineerPetAOESnareBuff (288) | 50 | Profession == 3; Level > 49; SpaceTime > 364; MaterialMetamorphosis > 364; Breed == 7 | AreaCastNano[204357,15]; AreaCastNano[204357,15]; Modify AddAllOff +15 |
| 204339 | Lesser Polarized Screening | PetSnare_RootResistanceBuff (285) | 50 | Profession == 3; Level > 49; MaterialCreation > 304; SpaceTime > 304; Breed == 7 | Modify NanoResist +10; ResistNanoStrain[145,25]; ResistNanoStrain[146,25] |
| 205235 | Boost Combat Array | PetShortTermDamageBuffs (225) | 75 | MaterialCreation > 478; MaterialMetamorphosis > 478; SpaceTime > 478; Profession == 3; Level > 74; Breed == 7; NPCFamily == 95 | Modify AddAllOff +165; Modify AddAllDef +28 |
| 203873 | Greater Energize Shell | NOSTACKING (0) | 75 | Profession == 3; MaterialCreation > 473; SpaceTime > 473; Level > 74; Breed == 7 | ReduceNanoStrainDuration[145,583]; ReduceNanoStrainDuration[146,583]; SystemText[Your pet moves more freely.] |
| 204364 | Intrusive Aura of Binding | EngineerPetAOESnareBuff (288) | 75 | Profession == 3; Level > 74; SpaceTime > 601; MaterialMetamorphosis > 601; Breed == 7 | AreaCastNano[204358,15]; AreaCastNano[204358,15]; Modify AddAllOff +30 |
| 205237 | Overdrive Combat Array | PetShortTermDamageBuffs (225) | 75 | MaterialCreation > 593; MaterialMetamorphosis > 593; SpaceTime > 593; Profession == 3; Level > 74; Breed == 7; NPCFamily == 95 | Modify AddAllOff +205; Modify AddAllDef +36 |
| 204335 | Polarized Screening | PetSnare_RootResistanceBuff (285) | 75 | Profession == 3; Level > 74; MaterialCreation > 513; SpaceTime > 513; Breed == 7 | Modify NanoResist +25; ResistNanoStrain[145,32]; ResistNanoStrain[146,32] |
| 205239 | Assist Aggression Subsystem | PetShortTermDamageBuffs (225) | 100 | MaterialCreation > 747; MaterialMetamorphosis > 747; SpaceTime > 747; Profession == 3; Level > 99; Breed == 7; NPCFamily == 95 | Modify AddAllOff +250; Modify AddAllDef +43; ResistNanoStrain[145,1]; ResistNanoStrain[146,1] |
| 203861 | Lesser Conductive Spike | NOSTACKING (0) | 100 | Profession == 3; MaterialCreation > 636; SpaceTime > 636; Level > 99; Breed == 7 | ReduceNanoStrainDuration[145,711]; ReduceNanoStrainDuration[146,711]; SystemText[Your pet moves more freely.] |
| 269907 | Pet Steal Back | Charm_Short (1022) | 100 | VisualProfession == 3; VisualProfession == 8; VisualProfession == 12; MaterialCreation > 769; SpaceTime > 769; Level > 99; Breed == 7 | RemoveNanoStrain[202]; SystemText[Your manifestation moves more freely.] |
| 204337 | Superior Polarized Screening | PetSnare_RootResistanceBuff (285) | 100 | Profession == 3; Level > 99; MaterialCreation > 754; SpaceTime > 754; Breed == 7 | Modify NanoResist +50; ResistNanoStrain[145,41]; ResistNanoStrain[146,41] |
| 203863 | Conductive Spike | NOSTACKING (0) | 125 | Profession == 3; MaterialCreation > 815; SpaceTime > 815; Level > 124; Breed == 7 | ReduceNanoStrainDuration[145,896]; ReduceNanoStrainDuration[146,896]; SystemText[Your pet moves more freely.] |
| 204366 | Intrusive Aura of Malaise | EngineerPetAOESnareBuff (288) | 125 | Profession == 3; Level > 124; SpaceTime > 829; MaterialMetamorphosis > 829; Breed == 7 | AreaCastNano[204359,15]; AreaCastNano[204359,15]; Modify AddAllOff +45 |
| 205241 | Monitor Aggression Subsystem | PetShortTermDamageBuffs (225) | 135 | MaterialCreation > 840; MaterialMetamorphosis > 840; SpaceTime > 840; Profession == 3; Level > 134; Breed == 7; NPCFamily == 95 | Modify AddAllOff +291; Modify AddAllDef +50; ResistNanoStrain[145,2]; ResistNanoStrain[146,2] |
| 203865 | Greater Conductive Spike | NOSTACKING (0) | 145 | Profession == 3; MaterialCreation > 891; SpaceTime > 891; Level > 144; Breed == 7 | ReduceNanoStrainDuration[145,1084]; ReduceNanoStrainDuration[146,1084]; SystemText[Your pet moves more freely.] |
| 204341 | Greater Polarized Screening | PetSnare_RootResistanceBuff (285) | 145 | Profession == 3; Level > 144; MaterialCreation > 878; SpaceTime > 878; Breed == 7 | Modify NanoResist +100; ResistNanoStrain[145,53]; ResistNanoStrain[146,53] |
| 205243 | Enhance Aggression Subsystem | PetShortTermDamageBuffs (225) | 155 | MaterialCreation > 902; MaterialMetamorphosis > 902; SpaceTime > 902; Profession == 3; Level > 154; Breed == 7; NPCFamily == 95 | Modify AddAllOff +338; Modify AddAllDef +58; ResistNanoStrain[145,3]; ResistNanoStrain[146,3] |
| 205245 | Boost Aggression Subsystem | PetShortTermDamageBuffs (225) | 165 | MaterialCreation > 962; MaterialMetamorphosis > 962; SpaceTime > 962; Profession == 3; Level > 164; Breed == 7; NPCFamily == 95 | Modify AddAllOff +374; Modify AddAllDef +68; ResistNanoStrain[145,5]; ResistNanoStrain[146,5] |
| 204368 | Intrusive Aura of Sloth | EngineerPetAOESnareBuff (288) | 165 | Profession == 3; Level > 164; SpaceTime > 930; MaterialMetamorphosis > 930; Breed == 7 | AreaCastNano[204360,15]; AreaCastNano[204360,15]; Modify AddAllOff +60 |
| 205247 | Overdrive Aggression Subsystem | PetShortTermDamageBuffs (225) | 185 | MaterialCreation > 1025; MaterialMetamorphosis > 1025; SpaceTime > 1025; Profession == 3; Level > 184; Breed == 7; NPCFamily == 95 | Modify AddAllOff +417; Modify AddAllDef +79; ResistNanoStrain[145,8]; ResistNanoStrain[146,8] |
| 204370 | Intrusive Aura of the Humble Servant | EngineerPetAOESnareBuff (288) | 195 | Profession == 3; Level > 194; SpaceTime > 1043; MaterialMetamorphosis > 1043; Breed == 7 | AreaCastNano[204361,15]; AreaCastNano[204361,15]; Modify AddAllOff +100 |
| 205249 | Omni-Pol Pacification Logic System | PetShortTermDamageBuffs (225) | 195 | MaterialCreation > 1046; MaterialMetamorphosis > 1046; SpaceTime > 1046; Profession == 3; Level > 194; Breed == 7; NPCFamily == 95 | Modify AddAllOff +446; Modify AddAllDef +88; ResistNanoStrain[145,11]; ResistNanoStrain[146,11] |
| 267606 | Lesser Software Hacking Shielding | PetDefensiveNanos (816) | 201 | MaterialMetamorphosis > 1300; MaterialCreation > 1300; SpaceTime > 1300; Profession == 3; Level > 200; Breed == 7; NPCFamily == 95 | ResistNanoStrain[147,15]; ResistNanoStrain[202,15]; Modify AddAllDef +200 |
| 267608 | Advanced Software Hacking Shielding | PetDefensiveNanos (816) | 215 | MaterialMetamorphosis > 1700; MaterialCreation > 1700; SpaceTime > 1700; Profession == 3; Level > 214; Breed == 7; NPCFamily == 95 | Modify AddAllDef +300; ResistNanoStrain[147,25]; ResistNanoStrain[202,25]; Modify ProjectileAC +1000; Modify MeleeAC +1000; Modify EnergyAC +1000; Modify ChemicalAC +1000; Modify RadiationAC +1000; Modify ColdAC +1000; Modify FireAC +1000; Modify PoisonAC +1000 |
| 269908 | Improved Pet Steal Back | Charm_Short (1022) | 215 | VisualProfession == 3; VisualProfession == 8; VisualProfession == 12; MaterialCreation > 1399; SpaceTime > 1399; Level > 214; Breed == 7 | RemoveNanoStrain[202]; SystemText[Your manifestation moves more freely.] |
| 267607 | Software Hacking Shielding | PetDefensiveNanos (816) | 215 | MaterialMetamorphosis > 1500; MaterialCreation > 1500; SpaceTime > 1500; Profession == 3; Level > 214; Breed == 7; NPCFamily == 95 | ResistNanoStrain[147,20]; ResistNanoStrain[202,20]; Modify AddAllDef +250 |
| 205229 | Assist Combat Array | PetShortTermDamageBuffs (225) | - | MaterialCreation > 101; MaterialMetamorphosis > 101; SpaceTime > 101; Profession == 3; Breed == 7; NPCFamily == 95 | Modify AddAllOff +32; Modify AddAllDef +6 |
| 275016 | Formula 22 | MPPetInitiativeBuffs (217) | - | MaterialCreation > 1824; SpaceTime > 1824; Profession == 3; NanoFocusLevel op22 64; Breed == 7; NPCFamily == 95 | Modify MeleeInit +240; Modify RangedInit +240; Modify PhysicalInit +240; Modify NanoCInit +240; Modify Aggressiveness +80 |
| 275835 | Intrusive Aura of Slave | EngineerPetAOESnareBuff (288) | - | Profession == 3; SpaceTime > 1769; MaterialMetamorphosis > 1769; NanoFocusLevel op22 64; Breed == 7 | AreaCastNano[275834,15]; Modify AddAllOff +130 |
| 203869 | Lesser Energize Shell | NOSTACKING (0) | - | Profession == 3; MaterialCreation > 95; SpaceTime > 95; Breed == 7 | ReduceNanoStrainDuration[145,292]; ReduceNanoStrainDuration[146,292]; SystemText[Your pet moves more freely.] |
| 269869 | Pet Attention | PetRoot (1013) | - | VisualProfession == 3; VisualProfession == 8; VisualProfession == 12; MaterialCreation > 769; SpaceTime > 769 | SystemText[Your pets move more freely.] |
| 269870 | Pet Cleanse | PetDebuffCleanse (1047) | - | VisualProfession == 3; VisualProfession == 8; VisualProfession == 12; MaterialCreation > 1399; SpaceTime > 1399 | SystemText[Your pets move more freely.] |

## Auras - Team (friendly) (15)

| id | name | nano line (strain) | minLvl | cast reqs | effect |
| ---: | --- | --- | ---: | --- | --- |
| 154567 | Sympathetic Armor Boost | EngineerAuras (227) | - | MaterialCreation > 170; MaterialMetamorphosis > 170; SpaceTime > 170; VisualProfession == 3 | TeamCastNano[154549]; TeamCastNano[154549] |
| 154561 | Sympathetic Arms Enhancement | EngineerAuras (227) | - | MaterialCreation > 381; MaterialMetamorphosis > 381; SpaceTime > 381; VisualProfession == 3 | TeamCastNano[154543]; TeamCastNano[154543] |
| 154564 | Sympathetic Defensive Screen | EngineerAuras (227) | - | MaterialCreation > 677; MaterialMetamorphosis > 677; SpaceTime > 677; VisualProfession == 3 | TeamCastNano[154546]; TeamCastNano[154546] |
| 154558 | Sympathetic Energy Cocoon | EngineerAuras (227) | - | MaterialCreation > 705; MaterialMetamorphosis > 705; SpaceTime > 705; VisualProfession == 3 | TeamCastNano[154540]; TeamCastNano[154540] |
| 154560 | Sympathetic Entropy Infusion | EngineerAuras (227) | - | MaterialCreation > 810; MaterialMetamorphosis > 810; SpaceTime > 810; VisualProfession == 3 | TeamCastNano[154542]; TeamCastNano[154542] |
| 154563 | Sympathetic Force Field | EngineerAuras (227) | - | MaterialCreation > 746; MaterialMetamorphosis > 746; SpaceTime > 746; VisualProfession == 3 | TeamCastNano[154545]; TeamCastNano[154545] |
| 154562 | Sympathetic Fortress Screen | EngineerAuras (227) | - | MaterialCreation > 870; MaterialMetamorphosis > 870; SpaceTime > 870; VisualProfession == 3 | TeamCastNano[154544]; TeamCastNano[154544] |
| 154552 | Sympathetic Harmonic Cocoon | EngineerAuras (227) | - | MaterialCreation > 537; MaterialMetamorphosis > 537; SpaceTime > 537; VisualProfession == 3 | TeamCastNano[154537]; TeamCastNano[154537] |
| 154553 | Sympathetic Harmonic Field | EngineerAuras (227) | - | MaterialCreation > 417; MaterialMetamorphosis > 417; SpaceTime > 417; VisualProfession == 3 | TeamCastNano[154538]; TeamCastNano[154538] |
| 154557 | Sympathetic Plasma Shielding | EngineerAuras (227) | - | MaterialCreation > 864; MaterialMetamorphosis > 864; SpaceTime > 864; VisualProfession == 3 | TeamCastNano[154539]; TeamCastNano[154539] |
| 154565 | Sympathetic Protective Field | EngineerAuras (227) | - | MaterialCreation > 455; MaterialMetamorphosis > 455; SpaceTime > 455; VisualProfession == 3 | TeamCastNano[154547]; TeamCastNano[154547] |
| 154550 | Sympathetic Reactive Cocoon | EngineerAuras (227) | - | MaterialCreation > 855; MaterialMetamorphosis > 855; SpaceTime > 855; VisualProfession == 3 | TeamCastNano[154535]; TeamCastNano[154535] |
| 154551 | Sympathetic Reactive Field | EngineerAuras (227) | - | MaterialCreation > 731; MaterialMetamorphosis > 731; SpaceTime > 731; VisualProfession == 3 | TeamCastNano[154536]; TeamCastNano[154536] |
| 154559 | Sympathetic Retaliatory Barrier | EngineerAuras (227) | - | MaterialCreation > 430; MaterialMetamorphosis > 430; SpaceTime > 430; VisualProfession == 3 | TeamCastNano[154541]; TeamCastNano[154541] |
| 154566 | Sympathetic Shielding Barrier | EngineerAuras (227) | - | MaterialCreation > 329; MaterialMetamorphosis > 329; SpaceTime > 329; VisualProfession == 3 | TeamCastNano[154548]; TeamCastNano[154548] |

## Auras - Debuff (hostile) (14)

| id | name | nano line (strain) | minLvl | cast reqs | effect |
| ---: | --- | --- | ---: | --- | --- |
| 154722 | Disruptive Barrier Negator | EngineerDebuffAuras (236) | - | MaterialCreation > 518; MaterialMetamorphosis > 518; SpaceTime > 518; VisualProfession == 3 | AreaCastNano[154708,10]; AreaCastNano[154708,10] |
| 154727 | Disruptive Cocoon Harmonics | EngineerDebuffAuras (236) | - | MaterialCreation > 493; MaterialMetamorphosis > 493; SpaceTime > 493; VisualProfession == 3 | AreaCastNano[154713,10]; AreaCastNano[154713,10] |
| 154728 | Disruptive Field Harmonics | EngineerDebuffAuras (236) | - | MaterialCreation > 194; MaterialMetamorphosis > 194; SpaceTime > 194; VisualProfession == 3 | AreaCastNano[154714,10]; AreaCastNano[154714,10] |
| 154724 | Disruptive Field Negator | EngineerDebuffAuras (236) | - | MaterialCreation > 93; MaterialMetamorphosis > 93; SpaceTime > 93; VisualProfession == 3 | AreaCastNano[154710,10]; AreaCastNano[154710,10] |
| 154726 | Disruptive Phase Harmonics | EngineerDebuffAuras (236) | - | MaterialCreation > 723; MaterialMetamorphosis > 723; SpaceTime > 723; VisualProfession == 3 | AreaCastNano[154712,10]; AreaCastNano[154712,10] |
| 154718 | Disruptive Photon Absorber | EngineerDebuffAuras (236) | - | PsychologicalModification > 344; MaterialMetamorphosis > 393; SpaceTime > 393; VisualProfession == 3 | AreaCastNano[154704,7]; AreaCastNano[154704,7] |
| 154716 | Disruptive Photon Annihilator | EngineerDebuffAuras (236) | - | PsychologicalModification > 629; MaterialMetamorphosis > 701; SpaceTime > 701; VisualProfession == 3 | AreaCastNano[154702,7]; AreaCastNano[154702,7] |
| 154719 | Disruptive Photon Deflector | EngineerDebuffAuras (236) | - | PsychologicalModification > 183; MaterialMetamorphosis > 206; SpaceTime > 206; VisualProfession == 3 | AreaCastNano[154705,7]; AreaCastNano[154705,7] |
| 154717 | Disruptive Photon Devourer | EngineerDebuffAuras (236) | - | PsychologicalModification > 512; MaterialMetamorphosis > 566; SpaceTime > 566; VisualProfession == 3 | AreaCastNano[154703,7]; AreaCastNano[154703,7] |
| 154721 | Disruptive Retaliatory Negator | EngineerDebuffAuras (236) | - | MaterialCreation > 712; MaterialMetamorphosis > 712; SpaceTime > 712; VisualProfession == 3 | AreaCastNano[154707,10]; AreaCastNano[154707,10] |
| 154720 | Disruptive Retributive Negator | EngineerDebuffAuras (236) | - | MaterialCreation > 838; MaterialMetamorphosis > 838; SpaceTime > 838; VisualProfession == 3 | AreaCastNano[154706,10]; AreaCastNano[154706,10] |
| 154723 | Disruptive Shielding Negator | EngineerDebuffAuras (236) | - | MaterialCreation > 264; MaterialMetamorphosis > 264; SpaceTime > 264; VisualProfession == 3 | AreaCastNano[154709,10]; AreaCastNano[154709,10] |
| 154715 | Disruptive Void Projector | EngineerDebuffAuras (236) | - | PsychologicalModification > 710; MaterialMetamorphosis > 782; SpaceTime > 782; VisualProfession == 3 | AreaCastNano[154701,9]; AreaCastNano[154701,9] |
| 154725 | Null Space Disruptor | EngineerDebuffAuras (236) | - | MaterialCreation > 873; MaterialMetamorphosis > 873; SpaceTime > 873; VisualProfession == 3 | AreaCastNano[154711,10]; AreaCastNano[154711,10] |

## Special Attack Absorbers (14)

| id | name | nano line (strain) | minLvl | cast reqs | effect |
| ---: | --- | --- | ---: | --- | --- |
| 204345 | Sloughing Protective Barrier | EngineerSpecialAttackAbsorber (286) | 50 | Profession == 3; Level > 49; MaterialCreation > 452; SpaceTime > 452 | ChangeVariable[517,2] |
| 204347 | Coherent Sloughing Protective Barrier | EngineerSpecialAttackAbsorber (286) | 75 | Profession == 3; Level > 74; MaterialCreation > 508; SpaceTime > 508 | ChangeVariable[517,2] |
| 204418 | Sloughing Protective Field | EngineerSpecialAttackAbsorber (286) | 75 | Profession == 3; Level > 74; MaterialCreation > 569; SpaceTime > 569 | TeamCastNano[204415] |
| 204349 | Sloughing Defensive Shield | EngineerSpecialAttackAbsorber (286) | 100 | Profession == 3; Level > 99; MaterialCreation > 673; SpaceTime > 673 | ChangeVariable[517,4] |
| 204351 | Coherent Sloughing Defensive Shield | EngineerSpecialAttackAbsorber (286) | 125 | Profession == 3; Level > 124; MaterialCreation > 832; SpaceTime > 832 | ChangeVariable[517,4] |
| 223307 | Isochronal Sloughing Protective Barrier | EngineerSpecialAttackAbsorber (286) | 125 | Profession == 3; Level > 124; MaterialCreation > 896; SpaceTime > 896; Expansion op22 2; Specialization op22 2 | ChangeVariable[517,5]; ChangeVariable[517,5] |
| 204420 | Sloughing Shielding Field | EngineerSpecialAttackAbsorber (286) | 135 | Profession == 3; Level > 134; MaterialCreation > 855; SpaceTime > 855 | TeamCastNano[204416] |
| 204353 | Sloughing Assault Shield | EngineerSpecialAttackAbsorber (286) | 175 | Profession == 3; Level > 174; MaterialCreation > 990; SpaceTime > 990 | ChangeVariable[517,6] |
| 204355 | Coherent Sloughing Assault Shield | EngineerSpecialAttackAbsorber (286) | 185 | Profession == 3; Level > 184; MaterialCreation > 1024; SpaceTime > 1024 | ChangeVariable[517,7] |
| 223309 | Isochronal Sloughing Defensive Shield | EngineerSpecialAttackAbsorber (286) | 195 | Profession == 3; Level > 194; MaterialCreation > 1137; SpaceTime > 1137; Expansion op22 2; Specialization op22 4 | ChangeVariable[517,7]; ChangeVariable[517,7] |
| 204422 | Sloughing Combat Field | EngineerSpecialAttackAbsorber (286) | 195 | Profession == 3; Level > 194; MaterialCreation > 1034; SpaceTime > 1034 | TeamCastNano[231055] |
| 237273 | Isochronal Sloughing Combat Field | SpecialAttackAbsorberBase (768) | 212 | Profession == 3; Level > 211; MaterialCreation > 1687; SpaceTime > 1684; Expansion op22 2; Specialization op22 8 | TeamCastNano[233081]; TeamCastNano[233081] |
| 223311 | Isochronal Sloughing Assault Shield | EngineerSpecialAttackAbsorber (286) | 218 | Profession == 3; Level > 217; MaterialCreation > 1759; SpaceTime > 1754; Expansion op22 2; Specialization op22 8 | ChangeVariable[517,9]; ChangeVariable[516,1]; ChangeVariable[517,9]; ChangeVariable[516,1] |
| 273343 | Improved Isochronal Sloughing Combat Field | SpecialAttackAbsorberBase (768) | - | SpaceTime > 2047; MaterialCreation > 2047; Profession == 3; NanoFocusLevel op22 64 | TeamCastNano[273344]; TeamCastNano[273344] |

## Self / Team Buffs (87)

| id | name | nano line (strain) | minLvl | cast reqs | effect |
| ---: | --- | --- | ---: | --- | --- |
| 227652 | Electrical Engineering Knowledge | ElectricalEngineeringBuff (172) | 75 | PsychologicalModification > 468; SensoryImprovement > 468; Profession == 3; Level > 74; Specialization op22 1; Expansion op22 2; Expansion op22 2; Level > 64 | Modify ElectricalEngineering +80 |
| 227655 | Field Quantum Physics Knowledge | FieldQuantumPhysicsBuff (173) | 80 | PsychologicalModification > 488; SensoryImprovement > 488; Profession == 3; Level > 79; Specialization op22 1; Expansion op22 2; Expansion op22 2; Level > 69 | Modify QuantumFT +80 |
| 227657 | Mechanical Engineering Knowledge | MechanicalEngineeringBuff (174) | 80 | PsychologicalModification > 507; SensoryImprovement > 507; Profession == 3; Level > 79; Specialization op22 1; Expansion op22 2; Expansion op22 2; Level > 69 | Modify MechanicalEngineering +80 |
| 227659 | Pharmaceuticals Knowledge | PharmaceuticalsBuff (175) | 85 | PsychologicalModification > 526; SensoryImprovement > 526; Profession == 3; Level > 84; Specialization op22 1; Expansion op22 2; Expansion op22 2; Level > 74 | Modify Pharmaceuticals +80 |
| 227661 | Weapon Smithing Knowledge | WeaponSmithingBuff (176) | 90 | PsychologicalModification > 545; SensoryImprovement > 545; Profession == 3; Level > 89; Specialization op22 1; Expansion op22 2; Expansion op22 2; Level > 79 | Modify WeaponSmithing +80 |
| 227663 | Electrical Engineering Mastery | ElectricalEngineeringBuff (172) | 130 | PsychologicalModification > 817; SensoryImprovement > 817; Profession == 3; Level > 129; Specialization op22 2; Expansion op22 2; Expansion op22 2; Level > 119 | Modify ElectricalEngineering +125 |
| 227665 | Field Quantum Physics Mastery | FieldQuantumPhysicsBuff (173) | 130 | PsychologicalModification > 825; SensoryImprovement > 825; Profession == 3; Level > 129; Specialization op22 2; Expansion op22 2; Expansion op22 2; Level > 119 | Modify QuantumFT +125 |
| 227667 | Mechanical Engineering Mastery | MechanicalEngineeringBuff (174) | 135 | PsychologicalModification > 838; SensoryImprovement > 838; Profession == 3; Level > 134; Specialization op22 1; Expansion op22 2; Expansion op22 2; Level > 124 | Modify MechanicalEngineering +125 |
| 227669 | Pharmaceuticals Mastery | PharmaceuticalsBuff (175) | 135 | PsychologicalModification > 844; SensoryImprovement > 844; Profession == 3; Level > 134; Specialization op22 1; Expansion op22 2; Expansion op22 2; Level > 124 | Modify Pharmaceuticals +125 |
| 227671 | Weapon Smithing Mastery | WeaponSmithingBuff (176) | 140 | PsychologicalModification > 854; SensoryImprovement > 854; Profession == 3; Level > 139; Specialization op22 1; Expansion op22 2; Expansion op22 2; Level > 129 | Modify WeaponSmithing +125 |
| 227674 | Assurance Advocacy | ArmorBuff (3) | 185 | MaterialCreation > 1102; MaterialMetamorphosis > 1102; Profession == 3; Level > 184; Specialization op22 4; Expansion op22 2; Expansion op22 2; Level > 174 | Modify ProjectileAC +1500; Modify MeleeAC +1500; Modify EnergyAC +1500; Modify ChemicalAC +1500; Modify RadiationAC +1500; Modify ColdAC +1500; Modify FireAC +1500; Modify PoisonAC +1500 |
| 227676 | Assurance Attention | ArmorBuff (3) | 201 | MaterialCreation > 1195; MaterialMetamorphosis > 1195; Profession == 3; Level > 200; Specialization op22 8; Expansion op22 2; Expansion op22 2; Level > 190 | Modify ProjectileAC +2500; Modify MeleeAC +2500; Modify EnergyAC +2500; Modify ChemicalAC +2500; Modify RadiationAC +2500; Modify ColdAC +2500; Modify FireAC +2500; Modify PoisonAC +2500 |
| 227678 | Assurance Relief | ArmorBuff (3) | 210 | MaterialCreation > 1598; MaterialMetamorphosis > 1600; Profession == 3; Level > 209; Specialization op22 8; Expansion op22 2; Expansion op22 2; Level > 199 | Modify ProjectileAC +3500; Modify MeleeAC +3500; Modify EnergyAC +3500; Modify ChemicalAC +3500; Modify RadiationAC +3500; Modify ColdAC +3500; Modify FireAC +3500; Modify PoisonAC +3500 |
| 227680 | Gift of Assurance | ArmorBuff (3) | 220 | MaterialCreation > 1839; MaterialMetamorphosis > 1844; Profession == 3; Level > 219; Specialization op22 8; Expansion op22 2; Expansion op22 2; Level > 209 | Modify ProjectileAC +5000; Modify MeleeAC +5000; Modify EnergyAC +5000; Modify ChemicalAC +5000; Modify RadiationAC +5000; Modify ColdAC +5000; Modify FireAC +5000; Modify PoisonAC +5000 |
| 70538 | Advanced Defensive Screen | ArmorBuff (3) | - | MaterialCreation > 589; MaterialMetamorphosis > 589; VisualProfession == 3 | Modify ProjectileAC +389; Modify MeleeAC +389; Modify EnergyAC +389; Modify ChemicalAC +389; Modify RadiationAC +389; Modify ColdAC +389; Modify PoisonAC +389; Modify FireAC +389 |
| 70529 | Advanced Force Field | ArmorBuff (3) | - | MaterialCreation > 832; MaterialMetamorphosis > 832; VisualProfession == 3 | Modify ProjectileAC +652; Modify MeleeAC +652; Modify EnergyAC +652; Modify ChemicalAC +652; Modify RadiationAC +652; Modify ColdAC +652; Modify PoisonAC +652; Modify FireAC +652 |
| 70539 | Advanced Protective Field | ArmorBuff (3) | - | MaterialCreation > 549; MaterialMetamorphosis > 549; VisualProfession == 3 | Modify ProjectileAC +364; Modify MeleeAC +364; Modify EnergyAC +364; Modify ChemicalAC +364; Modify RadiationAC +364; Modify ColdAC +364; Modify PoisonAC +364; Modify FireAC +364 |
| 70547 | Advanced Shielding Barrier | ArmorBuff (3) | - | MaterialCreation > 512; MaterialMetamorphosis > 512; VisualProfession == 3 | Modify ProjectileAC +343; Modify MeleeAC +343; Modify EnergyAC +343; Modify ChemicalAC +343; Modify RadiationAC +343; Modify ColdAC +343; Modify PoisonAC +343; Modify FireAC +343 |
| 29789 | Aegis Barrier | ArmorBuff (3) | - | MaterialMetamorphosis > 873; MaterialCreation > 873; VisualProfession == 3 | Modify ProjectileAC +720; Modify MeleeAC +720; Modify EnergyAC +720; Modify ChemicalAC +720; Modify RadiationAC +720; Modify ColdAC +720; Modify PoisonAC +720; Modify FireAC +720 |
| 29743 | Armor Megaboost | ArmorBuff (3) | - | MaterialMetamorphosis > 18; MaterialCreation > 18; VisualProfession == 3 | Modify ProjectileAC +13; Modify MeleeAC +13; Modify EnergyAC +13; Modify ChemicalAC +13; Modify RadiationAC +13; Modify ColdAC +13; Modify PoisonAC +13; Modify FireAC +13 |
| 117224 | Assume Profession: Engineer | FalseProfession (218) | - | VisualProfession == 5; VisualProfession == 3; Profession == 5; PsychologicalModification > 427; SensoryImprovement > 426; BiologicalMetamorphosis > 427 | RemoveNanoStrain[680]; Skill[318,70]; ModifyPercentage PsychologicalModification -15; ModifyPercentage SensoryImprovement -15; ModifyPercentage MaterialMetamorphosis -15; ModifyPercentage MaterialCreation -15; ModifyPercentage SpaceTime -15; ModifyPercentage BiologicalMetamorphosis -15; ChangeVariable[368,3] |
| 70541 | Basic Defensive Screen | ArmorBuff (3) | - | MaterialCreation > 182; MaterialMetamorphosis > 182; VisualProfession == 3 | Modify ProjectileAC +122; Modify MeleeAC +122; Modify EnergyAC +122; Modify ChemicalAC +122; Modify RadiationAC +122; Modify ColdAC +122; Modify PoisonAC +122; Modify FireAC +122 |
| 70533 | Basic Force Field | ArmorBuff (3) | - | MaterialCreation > 741; MaterialMetamorphosis > 741; VisualProfession == 3 | Modify ProjectileAC +545; Modify MeleeAC +545; Modify EnergyAC +545; Modify ChemicalAC +545; Modify RadiationAC +545; Modify ColdAC +545; Modify PoisonAC +545; Modify FireAC +545 |
| 70542 | Basic Protective Field | ArmorBuff (3) | - | MaterialCreation > 162; SpaceTime > 162; VisualProfession == 3 | Modify ProjectileAC +105; Modify MeleeAC +105; Modify EnergyAC +105; Modify ChemicalAC +105; Modify RadiationAC +105; Modify ColdAC +105; Modify PoisonAC +105; Modify FireAC +105 |
| 70543 | Basic Shielding Barrier | ArmorBuff (3) | - | MaterialCreation > 132; MaterialMetamorphosis > 132; VisualProfession == 3 | Modify ProjectileAC +85; Modify MeleeAC +85; Modify EnergyAC +85; Modify ChemicalAC +85; Modify RadiationAC +85; Modify ColdAC +85; Modify PoisonAC +85; Modify FireAC +85 |
| 269463 | Boosted Tendons | DamageBuffs_LineA (4) | - | MaterialMetamorphosis > 911; MaterialCreation > 911; Profession == 3; Expansion op22 2 | Modify ProjectileDamageModifier +35; Modify MeleeDamageModifier +35; Modify EnergyDamageModifier +35; Modify ChemicalDamageModifier +35; Modify RadiationDamageModifier +35; Modify ColdDamageModifier +35; Modify FireDamageModifier +35; Modify PoisonDamageModifier +35; Modify RangedInit +205 |
| 55769 | Citadel of Spikes | DamageShields (1) | - | MaterialMetamorphosis > 693; MaterialCreation > 693; VisualProfession == 3 | Modify ShieldProjectileAC +39; Modify ShieldMeleeAC +39; Modify ShieldEnergyAC +39; Modify ShieldChemicalAC +39; Modify ShieldRadiationAC +39; Modify ShieldColdAC +39; Modify ShieldFireAC +39; Modify ShieldPoisonAC +39; Modify MeleeAC +240 |
| 81922 | Controlled Energy Overload | DamageBuffs_LineA (4) | - | MaterialMetamorphosis > 460; MaterialCreation > 460; VisualProfession == 3 | Skill[280,16] |
| 29189 | Crowbar Subtlety | Breaking_Entry_DisarmTrapsBuff (206) | - | PsychologicalModification > 103; SensoryImprovement > 103; VisualProfession == 3 | Modify BreakingEntry +32 |
| 70551 | Defensive Screen | ArmorBuff (3) | - | MaterialCreation > 385; MaterialMetamorphosis > 385; VisualProfession == 3 | Modify ProjectileAC +252; Modify MeleeAC +252; Modify EnergyAC +252; Modify ChemicalAC +252; Modify RadiationAC +252; Modify ColdAC +252; Modify PoisonAC +252; Modify FireAC +252 |
| 55774 | Electrical Chastiser | DamageShields (1) | - | MaterialMetamorphosis > 113; MaterialCreation > 113; VisualProfession == 3 | Modify ShieldProjectileAC +4; Modify ShieldMeleeAC +4; Modify ShieldEnergyAC +4; Modify ShieldChemicalAC +4; Modify ShieldRadiationAC +4; Modify ShieldColdAC +4; Modify ShieldFireAC +4; Modify ShieldPoisonAC +4; Modify EnergyAC +100 |
| 55772 | Electrical Discharge Field | DamageShields (1) | - | MaterialMetamorphosis > 363; MaterialCreation > 363; VisualProfession == 3 | Modify ShieldProjectileAC +15; Modify ShieldMeleeAC +15; Modify ShieldEnergyAC +15; Modify ShieldChemicalAC +15; Modify ShieldRadiationAC +15; Modify ShieldColdAC +15; Modify ShieldFireAC +15; Modify ShieldPoisonAC +15; Modify EnergyAC +140 |
| 55770 | Energy Cocoon | DamageShields (1) | - | MaterialMetamorphosis > 531; MaterialCreation > 531; VisualProfession == 3 | Modify ShieldProjectileAC +24; Modify ShieldMeleeAC +24; Modify ShieldEnergyAC +24; Modify ShieldChemicalAC +24; Modify ShieldRadiationAC +24; Modify ShieldColdAC +24; Modify ShieldFireAC +24; Modify ShieldPoisonAC +24; Modify EnergyAC +220 |
| 29758 | Energy Spike | DamageBuffs_LineA (4) | - | MaterialMetamorphosis > 174; MaterialCreation > 174; VisualProfession == 3 | Skill[280,8] |
| 273346 | Engineer Composite Specialist Tradeskills (8 hours) | ElectricalEngineeringBuff (172) | - | PsychologicalModification > 1033; SensoryImprovement > 1033; Profession == 3; NanoFocusLevel op22 64 | Modify MechanicalEngineering +200; Modify ElectricalEngineering +200; Modify QuantumFT +200; Modify WeaponSmithing +200; Modify Pharmaceuticals +200; Modify Chemistry +200 |
| 29760 | Entropy Weapon | DamageBuffs_LineA (4) | - | MaterialMetamorphosis > 683; MaterialCreation > 683; VisualProfession == 3 | Skill[278,20]; Skill[279,20]; Skill[280,20]; Skill[281,20]; Skill[282,20]; Skill[311,20]; Skill[316,20]; Skill[317,20] |
| 29762 | Extreme Prejudice | GrenadeBuffs (207) | - | SensoryImprovement > 647; PsychologicalModification > 647; VisualProfession == 3 | Modify Grenade +120; Modify HeavyWeapons +50; Modify Pistol +120 |
| 32034 | False Profession: Engineer | FalseProfession (218) | - | VisualProfession == 5; VisualProfession == 3; Profession == 5; PsychologicalModification > 103; SensoryImprovement > 103; BiologicalMetamorphosis > 103 | RemoveNanoStrain[680]; Skill[318,150]; ModifyPercentage PsychologicalModification -25; ModifyPercentage SensoryImprovement -25; ModifyPercentage MaterialMetamorphosis -25; ModifyPercentage MaterialCreation -25; ModifyPercentage SpaceTime -25; ModifyPercentage BiologicalMetamorphosis -25; ChangeVariable[368,3] |
| 70544 | Flawed Defensive Screen | ArmorBuff (3) | - | MaterialCreation > 103; MaterialMetamorphosis > 103; VisualProfession == 3 | Modify ProjectileAC +66; Modify MeleeAC +66; Modify EnergyAC +66; Modify ChemicalAC +66; Modify RadiationAC +66; Modify ColdAC +66; Modify PoisonAC +66; Modify FireAC +66 |
| 70534 | Flawed Force Field | ArmorBuff (3) | - | MaterialCreation > 725; MaterialMetamorphosis > 725; VisualProfession == 3 | Modify ProjectileAC +517; Modify MeleeAC +517; Modify EnergyAC +517; Modify ChemicalAC +517; Modify RadiationAC +517; Modify ColdAC +517; Modify PoisonAC +517; Modify FireAC +517 |
| 70545 | Flawed Protective Field | ArmorBuff (3) | - | MaterialCreation > 73; MaterialMetamorphosis > 73; VisualProfession == 3 | Modify ProjectileAC +46; Modify MeleeAC +46; Modify EnergyAC +46; Modify ChemicalAC +46; Modify RadiationAC +46; Modify ColdAC +46; Modify PoisonAC +46; Modify FireAC +46 |
| 70546 | Flawed Shielding Barrier | ArmorBuff (3) | - | MaterialCreation > 51; MaterialMetamorphosis > 51; VisualProfession == 3 | Modify ProjectileAC +33; Modify MeleeAC +33; Modify EnergyAC +33; Modify ChemicalAC +33; Modify RadiationAC +33; Modify ColdAC +33; Modify PoisonAC +33; Modify FireAC +33 |
| 70531 | Force Field | ArmorBuff (3) | - | MaterialCreation > 774; MaterialMetamorphosis > 774; VisualProfession == 3 | Modify ProjectileAC +590; Modify MeleeAC +590; Modify EnergyAC +590; Modify ChemicalAC +590; Modify RadiationAC +590; Modify ColdAC +590; Modify PoisonAC +590; Modify FireAC +590 |
| 29763 | Fortress of Spikes | DamageShields (1) | - | MaterialMetamorphosis > 815; MaterialCreation > 815; VisualProfession == 3 | Modify ShieldProjectileAC +53; Modify ShieldMeleeAC +53; Modify ShieldEnergyAC +53; Modify ShieldChemicalAC +53; Modify ShieldRadiationAC +53; Modify ShieldColdAC +53; Modify ShieldFireAC +53; Modify ShieldPoisonAC +53; Modify MeleeAC +800 |
| 70548 | Greater Armor Megaboost | ArmorBuff (3) | - | MaterialCreation > 35; MaterialMetamorphosis > 35; VisualProfession == 3 | Modify ProjectileAC +23; Modify MeleeAC +23; Modify EnergyAC +23; Modify ChemicalAC +23; Modify RadiationAC +23; Modify ColdAC +23; Modify PoisonAC +23; Modify FireAC +23 |
| 70299 | Greater Harmonic Cocoon | ReflectShield (2) | - | SpaceTime > 735; MaterialCreation > 735; VisualProfession == 3; ExpansionPlayfield == 0 | Modify ReflectProjectileAC +25; Modify ReflectMeleeAC +25; Modify ReflectEnergyAC +25; Modify ReflectChemicalAC +25; Modify ReflectRadiationAC +25; Modify ReflectColdAC +25; Modify ReflectFireAC +25; Modify ReflectPoisonAC +25; Modify MaxReflectedProjectileDmg +30; Modify MaxReflectedMeleeDmg +30; Modify MaxReflectedEnergyDmg +30; Modify MaxReflectedChemicalDmg +30; Modify MaxReflectedRadiationDmg +30; Modify MaxReflectedColdDmg +30; Modify MaxReflectedFireDmg +30; Modify MaxReflectedPoisonDmg +30 |
| 55767 | Greater Retaliatory Barrier | DamageShields (1) | - | MaterialMetamorphosis > 762; MaterialCreation > 762; VisualProfession == 3 | Modify ShieldProjectileAC +43; Modify ShieldMeleeAC +43; Modify ShieldEnergyAC +43; Modify ShieldChemicalAC +43; Modify ShieldRadiationAC +43; Modify ShieldColdAC +43; Modify ShieldFireAC +43; Modify ShieldPoisonAC +43; Modify RadiationAC +600 |
| 29764 | Gun Enhancement | DamageBuffs_LineA (4) | - | MaterialMetamorphosis > 63; MaterialCreation > 63; VisualProfession == 3 | Skill[278,5] |
| 70296 | Harmonic Cocoon | ReflectShield (2) | - | SpaceTime > 644; MaterialCreation > 644; VisualProfession == 3; ExpansionPlayfield == 0 | Modify ReflectProjectileAC +19; Modify ReflectMeleeAC +19; Modify ReflectEnergyAC +19; Modify ReflectChemicalAC +19; Modify ReflectRadiationAC +19; Modify ReflectColdAC +19; Modify ReflectFireAC +19; Modify ReflectPoisonAC +19; Modify MaxReflectedProjectileDmg +15; Modify MaxReflectedMeleeDmg +15; Modify MaxReflectedEnergyDmg +15; Modify MaxReflectedChemicalDmg +15; Modify MaxReflectedRadiationDmg +15; Modify MaxReflectedColdDmg +15; Modify MaxReflectedFireDmg +15; Modify MaxReflectedPoisonDmg +15 |
| 70557 | Heavy Assault Force Field | ArmorBuff (3) | - | MaterialCreation > 849; MaterialMetamorphosis > 849; VisualProfession == 3 | Modify ProjectileAC +672; Modify MeleeAC +672; Modify EnergyAC +672; Modify ChemicalAC +672; Modify RadiationAC +672; Modify ColdAC +672; Modify PoisonAC +672; Modify FireAC +672 |
| 70555 | Lesser Defensive Screen | ArmorBuff (3) | - | MaterialCreation > 270; MaterialMetamorphosis > 270; VisualProfession == 3 | Modify ProjectileAC +189; Modify MeleeAC +189; Modify EnergyAC +189; Modify ChemicalAC +189; Modify RadiationAC +189; Modify ColdAC +189; Modify PoisonAC +189; Modify FireAC +189 |
| 70532 | Lesser Force Field | ArmorBuff (3) | - | MaterialCreation > 753; MaterialMetamorphosis > 753; VisualProfession == 3 | Modify ProjectileAC +566; Modify MeleeAC +566; Modify EnergyAC +566; Modify ChemicalAC +566; Modify RadiationAC +566; Modify ColdAC +566; Modify PoisonAC +566; Modify FireAC +566 |
| 70297 | Lesser Harmonic Cocoon | ReflectShield (2) | - | SpaceTime > 447; MaterialCreation > 447; VisualProfession == 3; ExpansionPlayfield == 0 | Modify ReflectProjectileAC +16; Modify ReflectMeleeAC +16; Modify ReflectEnergyAC +16; Modify ReflectChemicalAC +16; Modify ReflectRadiationAC +16; Modify ReflectColdAC +16; Modify ReflectFireAC +16; Modify ReflectPoisonAC +16; Modify MaxReflectedProjectileDmg +10; Modify MaxReflectedMeleeDmg +10; Modify MaxReflectedEnergyDmg +10; Modify MaxReflectedChemicalDmg +10; Modify MaxReflectedRadiationDmg +10; Modify MaxReflectedColdDmg +10; Modify MaxReflectedFireDmg +10; Modify MaxReflectedPoisonDmg +10 |
| 263301 | Lesser Miniaturization | EngineerMiniaturization (811) | - | Expansion op22 2; SpaceTime > 572; MaterialCreation > 572; Profession == 3; MonsterData == 218783; MonsterData == 218928; Flags op3 0; NPCFamily == 95 | Modify AddAllDef +30; Modify Scale -30 |
| 70556 | Lesser Protective Field | ArmorBuff (3) | - | MaterialCreation > 236; MaterialMetamorphosis > 236; VisualProfession == 3 | Modify ProjectileAC +168; Modify MeleeAC +168; Modify EnergyAC +168; Modify ChemicalAC +168; Modify RadiationAC +168; Modify ColdAC +168; Modify PoisonAC +168; Modify FireAC +168 |
| 55773 | Lesser Retaliatory Barrier | DamageShields (1) | - | MaterialMetamorphosis > 186; MaterialCreation > 186; VisualProfession == 3 | Modify ShieldProjectileAC +7; Modify ShieldMeleeAC +7; Modify ShieldEnergyAC +7; Modify ShieldChemicalAC +7; Modify ShieldRadiationAC +7; Modify ShieldColdAC +7; Modify ShieldFireAC +7; Modify ShieldPoisonAC +7; Modify RadiationAC +110 |
| 70540 | Lesser Shielding Barrier | ArmorBuff (3) | - | MaterialCreation > 210; MaterialMetamorphosis > 210; VisualProfession == 3 | Modify ProjectileAC +147; Modify MeleeAC +147; Modify EnergyAC +147; Modify ChemicalAC +147; Modify RadiationAC +147; Modify ColdAC +147; Modify PoisonAC +147; Modify FireAC +147 |
| 117213 | Mimic Profession: Engineer | FalseProfession (218) | - | VisualProfession == 5; VisualProfession == 3; Profession == 5; PsychologicalModification > 716; SensoryImprovement > 716; BiologicalMetamorphosis > 716 | RemoveNanoStrain[680]; Skill[318,15]; ModifyPercentage PsychologicalModification -5; ModifyPercentage SensoryImprovement -5; ModifyPercentage MaterialMetamorphosis -5; ModifyPercentage MaterialCreation -5; ModifyPercentage SpaceTime -5; ModifyPercentage BiologicalMetamorphosis -5; ChangeVariable[368,3] |
| 263303 | Miniaturization | EngineerMiniaturization (811) | - | Expansion op22 2; SpaceTime > 572; MaterialCreation > 572; Profession == 3; MonsterData == 218783; MonsterData == 218928; Flags op3 0; NPCFamily == 95 | Modify AddAllDef +50; Modify Scale -50 |
| 70298 | Minor Harmonic Cocoon | ReflectShield (2) | - | SpaceTime > 310; MaterialCreation > 310; VisualProfession == 3; ExpansionPlayfield == 0 | Modify ReflectProjectileAC +13; Modify ReflectMeleeAC +13; Modify ReflectEnergyAC +13; Modify ReflectChemicalAC +13; Modify ReflectRadiationAC +13; Modify ReflectColdAC +13; Modify ReflectFireAC +13; Modify ReflectPoisonAC +13; Modify MaxReflectedProjectileDmg +7; Modify MaxReflectedMeleeDmg +7; Modify MaxReflectedEnergyDmg +7; Modify MaxReflectedChemicalDmg +7; Modify MaxReflectedRadiationDmg +7; Modify MaxReflectedColdDmg +7; Modify MaxReflectedFireDmg +7; Modify MaxReflectedPoisonDmg +7 |
| 70294 | Partial Harmonic Cocoon | ReflectShield (2) | - | SpaceTime > 147; MaterialCreation > 147; VisualProfession == 3; ExpansionPlayfield == 0 | Modify ReflectProjectileAC +10; Modify ReflectMeleeAC +10; Modify ReflectEnergyAC +10; Modify ReflectChemicalAC +10; Modify ReflectRadiationAC +10; Modify ReflectColdAC +10; Modify ReflectFireAC +10; Modify ReflectPoisonAC +10; Modify MaxReflectedProjectileDmg +3; Modify MaxReflectedMeleeDmg +3; Modify MaxReflectedEnergyDmg +3; Modify MaxReflectedChemicalDmg +3; Modify MaxReflectedRadiationDmg +3; Modify MaxReflectedColdDmg +3; Modify MaxReflectedFireDmg +3; Modify MaxReflectedPoisonDmg +3 |
| 70535 | Perfected Defensive Screen | ArmorBuff (3) | - | MaterialCreation > 697; MaterialMetamorphosis > 697; VisualProfession == 3 | Modify ProjectileAC +472; Modify MeleeAC +472; Modify EnergyAC +472; Modify ChemicalAC +472; Modify RadiationAC +472; Modify ColdAC +472; Modify PoisonAC +472; Modify FireAC +472 |
| 70536 | Perfected Protective Field | ArmorBuff (3) | - | MaterialCreation > 675; MaterialMetamorphosis > 675; VisualProfession == 3 | Modify ProjectileAC +440; Modify MeleeAC +440; Modify EnergyAC +440; Modify ChemicalAC +440; Modify RadiationAC +440; Modify ColdAC +440; Modify PoisonAC +440; Modify FireAC +440 |
| 70537 | Perfected Shielding Barrier | ArmorBuff (3) | - | MaterialCreation > 638; MaterialMetamorphosis > 638; VisualProfession == 3 | Modify ProjectileAC +418; Modify MeleeAC +418; Modify EnergyAC +418; Modify ChemicalAC +418; Modify RadiationAC +418; Modify ColdAC +418; Modify PoisonAC +418; Modify FireAC +418 |
| 29772 | Philosopher's Stone | Chemistry_PharmBuff (196) | - | PsychologicalModification > 256; SensoryImprovement > 256; VisualProfession == 3 | Modify Chemistry +62; Modify Pharmaceuticals +62 |
| 29773 | Pillar of Spikes | DamageShields (1) | - | MaterialMetamorphosis > 434; MaterialCreation > 434; VisualProfession == 3 | Modify ShieldProjectileAC +19; Modify ShieldMeleeAC +19; Modify ShieldEnergyAC +19; Modify ShieldChemicalAC +19; Modify ShieldRadiationAC +19; Modify ShieldColdAC +19; Modify ShieldFireAC +19; Modify ShieldPoisonAC +19; Modify MeleeAC +180 |
| 29246 | Pistol Mastery | PistolBuff (199) | - | VisualProfession == 1; VisualProfession == 12; VisualProfession == 3; VisualProfession == 10; VisualProfession == 8; PsychologicalModification > 120; SensoryImprovement > 120; Flags op4 0 | Modify Pistol +40 |
| 29774 | Plasma Shield | DamageShields (1) | - | MaterialMetamorphosis > 858; MaterialCreation > 858; VisualProfession == 3 | Modify ShieldProjectileAC +65; Modify ShieldMeleeAC +65; Modify ShieldEnergyAC +65; Modify ShieldChemicalAC +65; Modify ShieldRadiationAC +65; Modify ShieldColdAC +65; Modify ShieldFireAC +65; Modify ShieldPoisonAC +65; Modify EnergyAC +1200 |
| 70553 | Protective Field | ArmorBuff (3) | - | MaterialCreation > 349; MaterialMetamorphosis > 349; VisualProfession == 3 | Modify ProjectileAC +231; Modify MeleeAC +231; Modify EnergyAC +231; Modify ChemicalAC +231; Modify RadiationAC +231; Modify ColdAC +231; Modify PoisonAC +231; Modify FireAC +231 |
| 29778 | Quick Weapon | InitiativeBuffs (152) | - | PsychologicalModification > 199; SensoryImprovement > 199; VisualProfession == 3 | Modify RangedInit +52 |
| 29779 | Rapid Weapon | InitiativeBuffs (152) | - | PsychologicalModification > 593; SensoryImprovement > 593; VisualProfession == 3 | Modify RangedInit +114 |
| 70295 | Reactive Harmonic Cocoon | ReflectShield (2) | - | SpaceTime > 823; MaterialCreation > 823; VisualProfession == 3; ExpansionPlayfield == 0 | Modify ReflectProjectileAC +29; Modify ReflectMeleeAC +29; Modify ReflectEnergyAC +29; Modify ReflectChemicalAC +29; Modify ReflectRadiationAC +29; Modify ReflectColdAC +29; Modify ReflectFireAC +29; Modify ReflectPoisonAC +29; Modify MaxReflectedProjectileDmg +40; Modify MaxReflectedMeleeDmg +40; Modify MaxReflectedEnergyDmg +40; Modify MaxReflectedChemicalDmg +40; Modify MaxReflectedRadiationDmg +40; Modify MaxReflectedColdDmg +40; Modify MaxReflectedFireDmg +40; Modify MaxReflectedPoisonDmg +40 |
| 55771 | Retaliatory Barrier | DamageShields (1) | - | MaterialMetamorphosis > 620; MaterialCreation > 620; VisualProfession == 3 | Modify ShieldProjectileAC +28; Modify ShieldMeleeAC +28; Modify ShieldEnergyAC +28; Modify ShieldChemicalAC +28; Modify ShieldRadiationAC +28; Modify ShieldColdAC +28; Modify ShieldFireAC +28; Modify ShieldPoisonAC +28; Modify RadiationAC +230 |
| 70554 | Shielding Barrier | ArmorBuff (3) | - | MaterialCreation > 303; MaterialMetamorphosis > 303; VisualProfession == 3 | Modify ProjectileAC +206; Modify MeleeAC +206; Modify EnergyAC +206; Modify ChemicalAC +206; Modify RadiationAC +206; Modify ColdAC +206; Modify PoisonAC +206; Modify FireAC +206 |
| 31593 | Slayerdroid Transference | NOSTACKING (0) | - | VisualProfession == 3; SpaceTime > 855; MaterialCreation > 855; MonsterData == 0 | Modify MartialArts +400; Modify MultiMelee +50; Modify Brawl +50; Modify FastAttack +50; Modify ProjectileAC +150; Modify MeleeAC +150; Modify EnergyAC +150; ChangeVariable[360,50] |
| 55768 | Sparkling Field Array | DamageShields (1) | - | MaterialMetamorphosis > 729; MaterialCreation > 729; VisualProfession == 3 | Modify ShieldProjectileAC +39; Modify ShieldMeleeAC +39; Modify ShieldEnergyAC +39; Modify ShieldChemicalAC +39; Modify ShieldRadiationAC +39; Modify ShieldColdAC +39; Modify ShieldFireAC +39; Modify ShieldPoisonAC +39; Modify EnergyAC +400 |
| 29781 | Spike Armor | DamageShields (1) | - | MaterialMetamorphosis > 251; MaterialCreation > 251; VisualProfession == 3 | Modify ShieldProjectileAC +11; Modify ShieldMeleeAC +11; Modify ShieldEnergyAC +11; Modify ShieldChemicalAC +11; Modify ShieldRadiationAC +11; Modify ShieldColdAC +11; Modify ShieldFireAC +11; Modify ShieldPoisonAC +11; Modify MeleeAC +120 |
| 29782 | Spike Shield | DamageShields (1) | - | MaterialMetamorphosis > 40; MaterialCreation > 40; VisualProfession == 3 | Modify ShieldProjectileAC +1; Modify ShieldMeleeAC +1; Modify ShieldEnergyAC +1; Modify ShieldChemicalAC +1; Modify ShieldRadiationAC +1; Modify ShieldColdAC +1; Modify ShieldFireAC +1; Modify ShieldPoisonAC +1; Modify MeleeAC +40 |
| 70549 | Superior Defensive Screen | ArmorBuff (3) | - | MaterialCreation > 473; MaterialMetamorphosis > 473; VisualProfession == 3 | Modify ProjectileAC +322; Modify MeleeAC +322; Modify EnergyAC +322; Modify ChemicalAC +322; Modify RadiationAC +322; Modify ColdAC +322; Modify PoisonAC +322; Modify FireAC +322 |
| 70530 | Superior Force Field | ArmorBuff (3) | - | MaterialCreation > 798; MaterialMetamorphosis > 798; VisualProfession == 3 | Modify ProjectileAC +617; Modify MeleeAC +617; Modify EnergyAC +617; Modify ChemicalAC +617; Modify RadiationAC +617; Modify ColdAC +617; Modify PoisonAC +617; Modify FireAC +617 |
| 70550 | Superior Protective Field | ArmorBuff (3) | - | MaterialCreation > 443; MaterialMetamorphosis > 443; VisualProfession == 3 | Modify ProjectileAC +301; Modify MeleeAC +301; Modify EnergyAC +301; Modify ChemicalAC +301; Modify RadiationAC +301; Modify ColdAC +301; Modify PoisonAC +301; Modify FireAC +301 |
| 70552 | Superior Shielding Barrier | ArmorBuff (3) | - | MaterialCreation > 409; MaterialMetamorphosis > 409; VisualProfession == 3 | Modify ProjectileAC +273; Modify MeleeAC +273; Modify EnergyAC +273; Modify ChemicalAC +273; Modify RadiationAC +273; Modify ColdAC +273; Modify PoisonAC +273; Modify FireAC +273 |
| 29188 | Swift Weapon | InitiativeBuffs (152) | - | MaterialMetamorphosis > 4; SensoryImprovement > 4; VisualProfession == 3 | Modify RangedInit +12 |
| 266307 | Technical Incompetence | NOSTACKING (0) | - | Expansion op22 32; BiologicalMetamorphosis > 799; Profession == 5; PsychologicalModification > 799; Profession == 3 | Modify BiologicalMetamorphosis -750; Modify MaterialMetamorphosis -750; Modify MaterialCreation -750; Modify PsychologicalModification -750; Modify SpaceTime -750; Modify SensoryImprovement -750 |
| 29785 | Trap Artifice | DisarmTrapBuff (235) | - | PsychologicalModification > 349; SensoryImprovement > 349; VisualProfession == 3 | Modify TrapDisarm +79 |
| 29788 | Ultimate Force Field | ArmorBuff (3) | - | MaterialCreation > 864; MaterialMetamorphosis > 864; VisualProfession == 3 | Modify ProjectileAC +690; Modify MeleeAC +690; Modify EnergyAC +690; Modify ChemicalAC +690; Modify RadiationAC +690; Modify ColdAC +690; Modify PoisonAC +690; Modify FireAC +690 |
| 266309 | Unsteady Hands | NOSTACKING (0) | - | Expansion op22 32; MaterialCreation > 799; Profession == 3; SpaceTime > 799; Profession == 4 | Modify Burst -600; Modify MGSMG -600; Modify RangedInit -600 |

## Heals / HoT (1)

| id | name | nano line (strain) | minLvl | cast reqs | effect |
| ---: | --- | --- | ---: | --- | --- |
| 270351 | Master's Repairs | CompleteHealingLine (282) | - | MaterialCreation > 925; SpaceTime > 925; BiologicalMetamorphosis > 836; Profession == 3 | (1 non-effect functions only) |

## Travel / Recall (4)

| id | name | nano line (strain) | minLvl | cast reqs | effect |
| ---: | --- | --- | ---: | --- | --- |
| 233839 | Shadowland Anchor | ShadowlandBindandRecall (738) | 75 | Profession == 3; Level > 74; MaterialCreation > 542; SpaceTime > 542; Expansion op22 2; ExpansionPlayfield == 1; PlayfieldType op107 1 | SetAnchor[]; Set[214,1] |
| 233843 | Initiate Shadowland Recall | ShadowlandBindandRecall (738) | 80 | Profession == 3; Level > 79; MaterialCreation > 542; SpaceTime > 542; Expansion op22 2; ExpansionPlayfield == 1; PlayfieldType op107 1 | RecallToAnchor[]; Set[214,1] |
| 233845 | Impel Shadowland Recall | ShadowlandBindandRecall (738) | 185 | Profession == 3; Level > 184; MaterialCreation > 1118; SpaceTime > 1118; Expansion op22 2; ExpansionPlayfield == 1; PlayfieldType op107 1 | RecallToAnchor[] |
| 233841 | Shadowland Safeguard | ShadowlandBindandRecall (738) | 190 | Profession == 3; Level > 189; MaterialCreation > 1117; SpaceTime > 1117; Expansion op22 2; ExpansionPlayfield == 1; PlayfieldType op107 1 | SetAnchor[] |

## Misc / Utility (16)

| id | name | nano line (strain) | minLvl | cast reqs | effect |
| ---: | --- | --- | ---: | --- | --- |
| 233099 | Empowered Partial Harmonic Cocoon | ShadowlandReflectBase (694) | 25 | SpaceTime > 193; MaterialCreation > 193; Profession == 3; Level > 24; Expansion op22 2 | TeamCastNano[233082]; TeamCastNano[233082] |
| 233089 | Empowered Minor Harmonic Cocoon | ShadowlandReflectBase (694) | 50 | SpaceTime > 416; MaterialCreation > 416; Profession == 3; Level > 49; Expansion op22 2 | TeamCastNano[233083]; TeamCastNano[233083] |
| 233091 | Empowered Lesser Harmonic Cocoon | ShadowlandReflectBase (694) | 75 | SpaceTime > 595; MaterialCreation > 595; Profession == 3; Level > 74; Expansion op22 2 | TeamCastNano[233084]; TeamCastNano[233084] |
| 233093 | Empowered Harmonic Cocoon | ShadowlandReflectBase (694) | 100 | SpaceTime > 861; MaterialCreation > 861; Profession == 3; Level > 99; Expansion op22 2 | TeamCastNano[233085]; TeamCastNano[233085] |
| 233095 | Empowered Greater Harmonic Cocoon | ShadowlandReflectBase (694) | 145 | SpaceTime > 980; MaterialCreation > 980; Profession == 3; Level > 144; Expansion op22 2 | TeamCastNano[233086]; TeamCastNano[233086] |
| 204343 | Coherent Polarized Screening | NOSTACKING (0) | 185 | Profession == 3; Level > 184; MaterialCreation > 1024; SpaceTime > 1024 | (1 non-effect functions only) |
| 233097 | Empowered Reactive Harmonic Cocoon | ShadowlandReflectBase (694) | 185 | SpaceTime > 1102; MaterialCreation > 1102; Profession == 3; Level > 184; Expansion op22 2 | TeamCastNano[233087]; TeamCastNano[233087] |
| 202260 | Shield of the Obedient Servant | NOSTACKING (0) | 185 | Profession == 3; Level > 184; MaterialCreation > 1022; MaterialMetamorphosis > 1022 | (1 non-effect functions only) |
| 302254 | Sedative Injectors | SiphonBox683 (683) | 211 | MaterialCreation > 1300; SpaceTime > 1300; Profession == 3; Level > 210; Flags op118 0; NPCFamily == 95 | AddOffProc[5,302253] |
| 154914 | Beacon Warp | BeaconWarp (293) | - | MaterialCreation > 421; SpaceTime > 421; MaterialMetamorphosis > 421; Profession == 3; ExpansionPlayfield == 0 | SummonPlayer[] |
| 156284 | Does Nothing 1 | NOSTACKING (0) | - | MaterialCreation > 202; SpaceTime > 202; Profession == 3; Cash > 79 |  |
| 156285 | Does nothing 2 | NOSTACKING (0) | - | MaterialCreation > 644; SpaceTime > 644; Profession == 3; Cash > 239 |  |
| 273341 | Empowered Pre-Nullity Cocoon | ShadowlandReflectBase (694) | - | SpaceTime > 1929; MaterialCreation > 1929; Profession == 3; NanoFocusLevel op22 64 | TeamCastNano[302379]; TeamCastNano[302379] |
| 285182 | Gravity Shift - Block | Strain917 (917) | - | Profession == 3; Profession == 7; Profession == 11; Flags op91 285181; Flags op42 0; PlayfieldProxy == 7015 |  |
| 270790 | Improved Shield of the Obedient Servant | NOSTACKING (0) | - | Profession == 3; MaterialCreation > 1839; MaterialMetamorphosis > 1769; NanoFocusLevel op22 64 | (1 non-effect functions only) |
| 154913 | Team Beacon Warp | BeaconWarp (293) | - | MaterialCreation > 742; SpaceTime > 742; MaterialMetamorphosis > 742; Profession == 3; ExpansionPlayfield == 0 | SummonTeamMates[] |

