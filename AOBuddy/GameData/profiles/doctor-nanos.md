# Doctor (profession 10) — Castable Nano Reference

Generated 2026-09-23T04:36:59Z for the AOBuddy10 bot. **240 nanos.**

## Method / provenance

- **Nano data (ground truth):** `E:\Funcom\OmniCell\OmniCell\Datafiles\nanos.ocp` — OmniCell OMNICELL-CONTENT v3 pack (client 18.8.50_EP1 extraction), loaded through `OmniCell.Core` `NanoLoader.CacheAllNanos` (net10 DLL). 10965 nano formulas in the pack.
- **Names:** joined by nano id against `itemnames.sql` (`itemnames` table).
- **Enums:** stat ids and nano-line names from `AOSharp.Common/GameData/Stat.cs` and `NanoEnums.cs`.
- **Extractor source:** `E:\Funcom\AOBuddy10\tools\mp-nano-extractor` (re-runnable).
- **No web data was used for any id, name or level.**

## Doc-castability criterion

A nano is included when one of its cast `Actions` (`ActionType.ToUse` = 3) has an `EqualTo` requirement on **`Profession`(stat 60) == 10** or **`VisualProfession`(stat 368) == 10**. VisualProfession is how most older profession nanos are locked. Split: 40 via Profession(60), 200 via VisualProfession(368). Every match uses the EqualTo operator.

## Category counts

| Category | Count |
| --- | ---: |
| Complete Healing line | 3 |
| Heals - Single Target | 58 |
| Heals - Team | 54 |
| Heals - HoT | 18 |
| Heals - Team HoT | 1 |
| Heal Delta / Regen Buffs | 4 |
| HP Buffs (short duration) | 14 |
| Cures / DoT & Strain Removal | 2 |
| Treatment / First Aid Buffs | 3 |
| DoTs | 58 |
| Nukes / Damage | 5 |
| Nukes | 1 |
| Debuffs - Initiative | 5 |
| Debuffs | 1 |
| Self / Team Buffs | 11 |
| Travel / Summon Utility | 2 |
| **Total** | **240** |

## Complete Healing line (3)

| id | name | nano line (strain) | minLvl | cast reqs | effect (sub-nanos resolved) |
| ---: | --- | --- | ---: | --- | --- |
| 42409 | Alpha and Omega | CompleteHealingLine (282) | - | BiologicalMetamorphosis > 873; MaterialMetamorphosis > 786; VisualProfession == 10 | TeamCastNano[42048] |
| 28650 | Complete Healing | CompleteHealingLine (282) | - | BiologicalMetamorphosis > 794; MaterialMetamorphosis > 722; VisualProfession == 10 | Hit[27,100,0,0] |
| 270747 | Improved Complete Healing | CompleteHealingLine (282) | - | BiologicalMetamorphosis > 2153; MaterialMetamorphosis > 2153; Profession == 10; NanoFocusLevel op22 64 | Hit[27,100,0,0] |

## Heals - Single Target (58)

| id | name | nano line (strain) | minLvl | cast reqs | effect (sub-nanos resolved) |
| ---: | --- | --- | ---: | --- | --- |
| 223281 | Cellular Recuperation | SingleTargetHealing (951) | 175 | BiologicalMetamorphosis > 1086; MaterialMetamorphosis > 1086; Profession == 10; Level > 174; Specialization op22 4; Expansion op22 2 | CastNano[241017]; Hit[27,952,1288,0] |
| 223285 | Assauge Pain | SingleTargetHealing (951) | 201 | BiologicalMetamorphosis > 1195; MaterialMetamorphosis > 1195; Profession == 10; Level > 200; Specialization op22 8; Expansion op22 2 | CastNano[241018]; Hit[27,1133,1533,0] |
| 223287 | Touch of Kindness | SingleTargetHealing (951) | 205 | BiologicalMetamorphosis > 1375; MaterialMetamorphosis > 1375; Profession == 10; Level > 204; Specialization op22 8; Expansion op22 2 | CastNano[241016]; Hit[27,1700,2300,0] |
| 223289 | Restorative Influx | SingleTargetHealing (951) | 209 | BiologicalMetamorphosis > 1555; MaterialMetamorphosis > 1555; Profession == 10; Level > 208; Specialization op22 8; Expansion op22 2 | CastNano[241019]; Hit[27,2097,2837,0] |
| 223291 | Revivification | SingleTargetHealing (951) | 212 | BiologicalMetamorphosis > 1690; MaterialMetamorphosis > 1690; Profession == 10; Level > 211; Specialization op22 8; Expansion op22 2 | CastNano[241020]; Hit[27,2607,3527,0] |
| 223293 | Blessing of Purity | SingleTargetHealing (951) | 214 | BiologicalMetamorphosis > 1780; MaterialMetamorphosis > 1780; Profession == 10; Level > 213; Specialization op22 8; Expansion op22 2 | CastNano[241021]; Hit[27,3337,4515,0] |
| 223295 | Renewal of Being | SingleTargetHealing (951) | 216 | BiologicalMetamorphosis > 1870; MaterialMetamorphosis > 1870; Profession == 10; Level > 215; Specialization op22 8; Expansion op22 2 | CastNano[241022]; Hit[27,4576,6191,0] |
| 223297 | Cellular Rebirth | SingleTargetHealing (951) | 218 | BiologicalMetamorphosis > 1960; MaterialMetamorphosis > 1960; Profession == 10; Level > 217; Specialization op22 8; Expansion op22 2 | CastNano[241023]; Hit[27,6001,7639,0] |
| 223299 | Bodily Invigoration | SingleTargetHealing (951) | 220 | BiologicalMetamorphosis > 2050; MaterialMetamorphosis > 2050; Profession == 10; Level > 219; Specialization op22 8; Expansion op22 2 | CastNano[241024]; Hit[27,7215,8385,0] |
| 43809 | Advanced Cellular Rebuild | NOSTACKING (0) | - | BiologicalMetamorphosis > 650; MaterialMetamorphosis > 583; VisualProfession == 10 | Hit[27,559,1034,0] |
| 28681 | Bind Wounds | NOSTACKING (0) | - | BiologicalMetamorphosis > 122; MaterialMetamorphosis > 103; VisualProfession == 10 | Hit[27,117,188,0] |
| 28645 | Bloom of Health | NOSTACKING (0) | - | BiologicalMetamorphosis > 615; MaterialMetamorphosis > 553; VisualProfession == 10 | Hit[27,536,982,0] |
| 43883 | Bodily Purification | NOSTACKING (0) | - | BiologicalMetamorphosis > 689; MaterialMetamorphosis > 617; VisualProfession == 10 | Hit[27,588,1082,0] |
| 43826 | Cellular Grafting | NOSTACKING (0) | - | BiologicalMetamorphosis > 228; MaterialMetamorphosis > 203; VisualProfession == 10 | Hit[27,260,337,0] |
| 43812 | Cellular Rebuild | NOSTACKING (0) | - | BiologicalMetamorphosis > 413; MaterialMetamorphosis > 369; VisualProfession == 10 | Hit[27,389,650,0] |
| 28648 | Close Wounds | NOSTACKING (0) | - | BiologicalMetamorphosis > 434; MaterialMetamorphosis > 393; VisualProfession == 10 | Hit[27,415,690,0] |
| 43884 | Compress Wounds | NOSTACKING (0) | - | BiologicalMetamorphosis > 727; MaterialMetamorphosis > 655; VisualProfession == 10 | Hit[27,630,1174,0] |
| 43813 | Counteract Damage | NOSTACKING (0) | - | BiologicalMetamorphosis > 240; MaterialMetamorphosis > 215; VisualProfession == 10 | Hit[27,269,368,0] |
| 43827 | Cursory Examination | NOSTACKING (0) | - | BiologicalMetamorphosis > 152; MaterialMetamorphosis > 129; VisualProfession == 10 | Hit[27,151,225,0] |
| 43885 | Deep Tissue Repair | NOSTACKING (0) | - | BiologicalMetamorphosis > 750; MaterialMetamorphosis > 679; VisualProfession == 10 | Hit[27,650,1225,0] |
| 28654 | Doctor's Grace | NOSTACKING (0) | - | BiologicalMetamorphosis > 46; MaterialMetamorphosis > 38; VisualProfession == 10 | Hit[27,45,73,0] |
| 43828 | Dress Wounds | NOSTACKING (0) | - | BiologicalMetamorphosis > 57; MaterialMetamorphosis > 47; VisualProfession == 10 | Hit[27,54,88,0] |
| 43886 | Emergency Medical Response | SingleTargetHealing (951) | - | BiologicalMetamorphosis > 849; MaterialMetamorphosis > 849; VisualProfession == 10 | Hit[27,1700,2040,0] |
| 43829 | Emergency Stitching | NOSTACKING (0) | - | BiologicalMetamorphosis > 24; MaterialMetamorphosis > 21; VisualProfession == 10 | Hit[27,27,43,0] |
| 43814 | Experimental Panacea | NOSTACKING (0) | - | BiologicalMetamorphosis > 486; MaterialMetamorphosis > 443; VisualProfession == 10 | Hit[27,446,794,0] |
| 43830 | Field Dressings | NOSTACKING (0) | - | BiologicalMetamorphosis > 68; MaterialMetamorphosis > 56; VisualProfession == 10 | Hit[27,65,102,0] |
| 43887 | Greater Bloom of Health | NOSTACKING (0) | - | BiologicalMetamorphosis > 742; MaterialMetamorphosis > 671; VisualProfession == 10 | Hit[27,871,980,0] |
| 43815 | Greater Cellular Grafting | NOSTACKING (0) | - | BiologicalMetamorphosis > 518; MaterialMetamorphosis > 472; VisualProfession == 10 | Hit[27,476,831,0] |
| 43888 | Greater Field Dressings | NOSTACKING (0) | - | BiologicalMetamorphosis > 710; MaterialMetamorphosis > 639; VisualProfession == 10 | Hit[27,603,1153,0] |
| 43889 | Hale and Hearty | NOSTACKING (0) | - | BiologicalMetamorphosis > 699; MaterialMetamorphosis > 627; VisualProfession == 10 | Hit[27,596,1117,0] |
| 43816 | Healer's Hands | NOSTACKING (0) | - | BiologicalMetamorphosis > 583; MaterialMetamorphosis > 527; VisualProfession == 10 | Hit[27,524,929,0] |
| 43817 | Healing Light | NOSTACKING (0) | - | BiologicalMetamorphosis > 560; MaterialMetamorphosis > 507; VisualProfession == 10 | Hit[27,506,894,0] |
| 28665 | Improved Healing | NOSTACKING (0) | - | BiologicalMetamorphosis > 4; MaterialMetamorphosis > 4; VisualProfession == 10 | Hit[27,16,30,0] |
| 43831 | Inferior Wound Bindings | NOSTACKING (0) | - | BiologicalMetamorphosis > 35; MaterialMetamorphosis > 30; VisualProfession == 10 | Hit[27,36,59,0] |
| 43890 | Internal Renewal | NOSTACKING (0) | - | BiologicalMetamorphosis > 735; MaterialMetamorphosis > 663; VisualProfession == 10 | Hit[27,636,1191,0] |
| 43818 | Lesser Bloom of Health | NOSTACKING (0) | - | BiologicalMetamorphosis > 316; MaterialMetamorphosis > 277; VisualProfession == 10 | Hit[27,300,497,0] |
| 43832 | Lesser Cellular Grafting | NOSTACKING (0) | - | BiologicalMetamorphosis > 18; MaterialMetamorphosis > 16; VisualProfession == 10 | Hit[27,21,37,0] |
| 43833 | Lesser Nano Bandage | NOSTACKING (0) | - | BiologicalMetamorphosis > 88; MaterialMetamorphosis > 74; VisualProfession == 10 | Hit[27,86,133,0] |
| 43834 | Lesser Nano Surgery | NOSTACKING (0) | - | BiologicalMetamorphosis > 132; MaterialMetamorphosis > 111; VisualProfession == 10 | Hit[27,130,199,0] |
| 43808 | Life Balm | NOSTACKING (0) | - | BiologicalMetamorphosis > 721; MaterialMetamorphosis > 649; VisualProfession == 10 | Hit[27,621,1165,0] |
| 43878 | Lifegiving Elixir | SingleTargetHealing (951) | - | BiologicalMetamorphosis > 1049; MaterialMetamorphosis > 1049; VisualProfession == 10 | Hit[27,2100,2520,0] |
| 43810 | Medical Response | NOSTACKING (0) | - | BiologicalMetamorphosis > 632; MaterialMetamorphosis > 568; VisualProfession == 10 | Hit[27,540,1018,0] |
| 43835 | Minor Balm | NOSTACKING (0) | - | BiologicalMetamorphosis > 194; MaterialMetamorphosis > 170; VisualProfession == 10 | Hit[27,207,286,0] |
| 43819 | Nano Bandage | NOSTACKING (0) | - | BiologicalMetamorphosis > 356; MaterialMetamorphosis > 309; VisualProfession == 10 | Hit[27,327,551,0] |
| 43820 | Nano Surgery | NOSTACKING (0) | - | BiologicalMetamorphosis > 443; MaterialMetamorphosis > 402; VisualProfession == 10 | Hit[27,426,706,0] |
| 43836 | Physician's Skill | NOSTACKING (0) | - | BiologicalMetamorphosis > 174; MaterialMetamorphosis > 150; VisualProfession == 10 | Hit[27,180,255,0] |
| 28672 | Premium Cover | NOSTACKING (0) | - | BiologicalMetamorphosis > 182; MaterialMetamorphosis > 158; VisualProfession == 10 | Hit[27,192,266,0] |
| 43881 | Recuperative Respite | SingleTargetHealing (951) | - | BiologicalMetamorphosis > 949; MaterialMetamorphosis > 949; VisualProfession == 10 | Hit[27,1900,2280,0] |
| 43837 | Relief from Pain | NOSTACKING (0) | - | BiologicalMetamorphosis > 108; MaterialMetamorphosis > 90; VisualProfession == 10 | Hit[27,99,170,0] |
| 43838 | Restorative Boost | NOSTACKING (0) | - | BiologicalMetamorphosis > 206; MaterialMetamorphosis > 183; VisualProfession == 10 | Hit[27,231,299,0] |
| 43821 | Superior Dress Wounds | NOSTACKING (0) | - | BiologicalMetamorphosis > 460; MaterialMetamorphosis > 422; VisualProfession == 10 | Hit[27,439,747,0] |
| 28676 | Superior Healing | NOSTACKING (0) | - | BiologicalMetamorphosis > 162; MaterialMetamorphosis > 138; VisualProfession == 10 | Hit[27,161,238,0] |
| 43811 | Superior Nano Bandage | NOSTACKING (0) | - | BiologicalMetamorphosis > 675; MaterialMetamorphosis > 604; VisualProfession == 10 | Hit[27,572,1055,0] |
| 43822 | Superior Wound Bindings | NOSTACKING (0) | - | BiologicalMetamorphosis > 381; MaterialMetamorphosis > 330; VisualProfession == 10 | Hit[27,343,588,0] |
| 28677 | Surgeon's Touch | NOSTACKING (0) | - | BiologicalMetamorphosis > 257; MaterialMetamorphosis > 230; VisualProfession == 10 | Hit[27,282,395,0] |
| 43823 | Tailored Cure | NOSTACKING (0) | - | BiologicalMetamorphosis > 283; MaterialMetamorphosis > 251; VisualProfession == 10 | Hit[27,294,437,0] |
| 43824 | Thorough Examination | NOSTACKING (0) | - | BiologicalMetamorphosis > 397; MaterialMetamorphosis > 349; VisualProfession == 10 | Hit[27,354,631,0] |
| 43825 | Tissue Repair | NOSTACKING (0) | - | BiologicalMetamorphosis > 537; MaterialMetamorphosis > 487; VisualProfession == 10 | Hit[27,490,857,0] |

## Heals - Team (54)

| id | name | nano line (strain) | minLvl | cast reqs | effect (sub-nanos resolved) |
| ---: | --- | --- | ---: | --- | --- |
| 43899 | Accumulate Scars | NOSTACKING (0) | - | BiologicalMetamorphosis > 718; MaterialMetamorphosis > 647; VisualProfession == 10 | TeamCastNano[43839] |
| 95710 | Basic Omni-Med Enhancement | NOSTACKING (0) | - | BiologicalMetamorphosis > 707; MaterialMetamorphosis > 636; SpaceTime > 636; VisualProfession == 10 | TeamCastNano[95684] |
| 43901 | Bestow Healing | NOSTACKING (0) | - | BiologicalMetamorphosis > 693; MaterialMetamorphosis > 621; VisualProfession == 10 | TeamCastNano[43840] |
| 27804 | Blood Circle | NOSTACKING (0) | - | BiologicalMetamorphosis > 127; MaterialMetamorphosis > 107; VisualProfession == 10 | TeamCastNano[27803]; TeamCastNano[144635] |
| 95719 | Bodily Reinforcement | NOSTACKING (0) | - | BiologicalMetamorphosis > 57; MaterialMetamorphosis > 47; SpaceTime > 47; VisualProfession == 10 | TeamCastNano[95674] |
| 43900 | Circle of Renewal | NOSTACKING (0) | - | BiologicalMetamorphosis > 703; MaterialMetamorphosis > 631; VisualProfession == 10 | TeamCastNano[43841] |
| 43902 | Circulate Health | NOSTACKING (0) | - | BiologicalMetamorphosis > 608; MaterialMetamorphosis > 547; VisualProfession == 10 | TeamCastNano[43842] |
| 42395 | Cleanse Wounds | NOSTACKING (0) | - | BiologicalMetamorphosis > 466; MaterialMetamorphosis > 427; VisualProfession == 10 | TeamCastNano[42049] |
| 28649 | Colossal Health | NOSTACKING (0) | - | BiologicalMetamorphosis > 555; MaterialMetamorphosis > 502; SpaceTime > 502; VisualProfession == 10 | TeamCastNano[95682] |
| 43912 | Company Policy | NOSTACKING (0) | - | BiologicalMetamorphosis > 186; MaterialMetamorphosis > 162; VisualProfession == 10 | TeamCastNano[43853] |
| 43891 | Conglomerate Health Plan | NOSTACKING (0) | - | BiologicalMetamorphosis > 855; MaterialMetamorphosis > 771; VisualProfession == 10 | TeamCastNano[43843] |
| 43904 | Consummate Carer | NOSTACKING (0) | - | BiologicalMetamorphosis > 499; MaterialMetamorphosis > 454; VisualProfession == 10 | TeamCastNano[43854] |
| 43894 | Deep Wound Cleanser | NOSTACKING (0) | - | BiologicalMetamorphosis > 798; MaterialMetamorphosis > 725; VisualProfession == 10 | TeamCastNano[43844] |
| 43892 | Distributed Care | NOSTACKING (0) | - | BiologicalMetamorphosis > 835; MaterialMetamorphosis > 755; VisualProfession == 10 | TeamCastNano[43845] |
| 43911 | Easing Touch | NOSTACKING (0) | - | BiologicalMetamorphosis > 245; MaterialMetamorphosis > 220; VisualProfession == 10 | TeamCastNano[43855] |
| 95716 | Enhance Constitution | NOSTACKING (0) | - | BiologicalMetamorphosis > 137; MaterialMetamorphosis > 116; SpaceTime > 116; VisualProfession == 10 | TeamCastNano[95676] |
| 42397 | Enhance Team Health | NOSTACKING (0) | - | BiologicalMetamorphosis > 18; MaterialMetamorphosis > 16; SpaceTime > 16; VisualProfession == 10 | TeamCastNano[42050] |
| 28660 | Exaggerated Health | NOSTACKING (0) | - | BiologicalMetamorphosis > 385; MaterialMetamorphosis > 335; SpaceTime > 335; VisualProfession == 10 | TeamCastNano[95681] |
| 43906 | Fast Team Tissue Repair | NOSTACKING (0) | - | BiologicalMetamorphosis > 393; MaterialMetamorphosis > 344; VisualProfession == 10 | TeamCastNano[43856] |
| 28662 | Gargantuan Health | NOSTACKING (0) | - | BiologicalMetamorphosis > 788; MaterialMetamorphosis > 716; SpaceTime > 716; VisualProfession == 10 | TeamCastNano[95687] |
| 42398 | Greater Team Healing | NOSTACKING (0) | - | BiologicalMetamorphosis > 370; MaterialMetamorphosis > 320; VisualProfession == 10 | TeamCastNano[42051] |
| 43893 | Halo of Health | NOSTACKING (0) | - | BiologicalMetamorphosis > 820; MaterialMetamorphosis > 744; VisualProfession == 10 | TeamCastNano[43846] |
| 43895 | Health Cartel | NOSTACKING (0) | - | BiologicalMetamorphosis > 780; MaterialMetamorphosis > 708; VisualProfession == 10 | TeamCastNano[43847] |
| 95717 | Health Graft | NOSTACKING (0) | - | BiologicalMetamorphosis > 98; MaterialMetamorphosis > 82; SpaceTime > 82; VisualProfession == 10 | TeamCastNano[95675] |
| 273315 | Improved Team Health Plan | NOSTACKING (0) | - | BiologicalMetamorphosis > 1830; MaterialMetamorphosis > 1830; Profession == 10; NanoFocusLevel op22 64 | TeamCastNano[273316] |
| 43917 | Inferior Cleanse Wounds | NOSTACKING (0) | - | BiologicalMetamorphosis > 83; MaterialMetamorphosis > 69; VisualProfession == 10 | TeamCastNano[43857] |
| 95720 | Infuse with Life | NOSTACKING (0) | - | BiologicalMetamorphosis > 748; MaterialMetamorphosis > 677; SpaceTime > 677; VisualProfession == 10 | TeamCastNano[95686] |
| 43913 | Lesser Circle of Renewal | NOSTACKING (0) | - | BiologicalMetamorphosis > 224; MaterialMetamorphosis > 199; VisualProfession == 10 | TeamCastNano[43858] |
| 95713 | Major Health Graft | NOSTACKING (0) | - | BiologicalMetamorphosis > 451; MaterialMetamorphosis > 412; SpaceTime > 412; VisualProfession == 10 | TeamCastNano[95680] |
| 42402 | Medic's Call | NOSTACKING (0) | - | BiologicalMetamorphosis > 270; MaterialMetamorphosis > 240; VisualProfession == 10 | TeamCastNano[42053] |
| 95714 | Medic's Respite | NOSTACKING (0) | - | BiologicalMetamorphosis > 232; MaterialMetamorphosis > 207; SpaceTime > 207; VisualProfession == 10 | TeamCastNano[95678] |
| 95718 | Metabolism Booster | NOSTACKING (0) | - | BiologicalMetamorphosis > 174; MaterialMetamorphosis > 150; SpaceTime > 150; VisualProfession == 10 | TeamCastNano[95677] |
| 42403 | Minor Team Healing | NOSTACKING (0) | - | BiologicalMetamorphosis > 51; MaterialMetamorphosis > 43; VisualProfession == 10 | TeamCastNano[42054] |
| 43914 | Minor Team Purification | NOSTACKING (0) | - | BiologicalMetamorphosis > 166; MaterialMetamorphosis > 142; VisualProfession == 10 | TeamCastNano[43859] |
| 95711 | Pre-Combat Conditioning | NOSTACKING (0) | - | BiologicalMetamorphosis > 662; MaterialMetamorphosis > 593; SpaceTime > 593; VisualProfession == 10 | TeamCastNano[95683] |
| 43907 | Professional Care | NOSTACKING (0) | - | BiologicalMetamorphosis > 447; MaterialMetamorphosis > 407; VisualProfession == 10 | TeamCastNano[43860] |
| 42404 | Radiant Heal | NOSTACKING (0) | - | BiologicalMetamorphosis > 577; MaterialMetamorphosis > 522; VisualProfession == 10 | TeamCastNano[42055] |
| 43897 | Remedy Dissemination | NOSTACKING (0) | - | BiologicalMetamorphosis > 746; MaterialMetamorphosis > 675; VisualProfession == 10 | TeamCastNano[43848] |
| 43915 | Spreading Health | NOSTACKING (0) | - | BiologicalMetamorphosis > 147; MaterialMetamorphosis > 124; VisualProfession == 10 | TeamCastNano[43861] |
| 95715 | Superior Bodily Reinforcement | NOSTACKING (0) | - | BiologicalMetamorphosis > 277; MaterialMetamorphosis > 246; SpaceTime > 246; VisualProfession == 10 | TeamCastNano[95679] |
| 95712 | Superior Metabolism Booster | NOSTACKING (0) | - | BiologicalMetamorphosis > 733; MaterialMetamorphosis > 661; SpaceTime > 661; VisualProfession == 10 | TeamCastNano[95685] |
| 95709 | Superior Omni-Med Enhancement | NOSTACKING (0) | - | BiologicalMetamorphosis > 852; MaterialMetamorphosis > 769; SpaceTime > 769; VisualProfession == 10 | TeamCastNano[95673] |
| 273312 | Superior Team Health Plan | NOSTACKING (0) | - | BiologicalMetamorphosis > 2153; MaterialMetamorphosis > 2153; Profession == 10; NanoFocusLevel op22 64 | TeamCastNano[273313] |
| 43908 | Syndicated Healing | NOSTACKING (0) | - | BiologicalMetamorphosis > 421; MaterialMetamorphosis > 378; VisualProfession == 10 | TeamCastNano[43862] |
| 43909 | Team Cellular Rebuild | NOSTACKING (0) | - | BiologicalMetamorphosis > 296; MaterialMetamorphosis > 261; VisualProfession == 10 | TeamCastNano[43863] |
| 43910 | Team Checkup | NOSTACKING (0) | - | BiologicalMetamorphosis > 336; MaterialMetamorphosis > 293; VisualProfession == 10 | TeamCastNano[43864] |
| 43896 | Team Compress Wounds | NOSTACKING (0) | - | BiologicalMetamorphosis > 759; MaterialMetamorphosis > 688; VisualProfession == 10 | TeamCastNano[43849] |
| 43916 | Team Field Dressings | NOSTACKING (0) | - | BiologicalMetamorphosis > 103; MaterialMetamorphosis > 86; VisualProfession == 10 | TeamCastNano[43865] |
| 42405 | Team Healing | NOSTACKING (0) | - | BiologicalMetamorphosis > 202; MaterialMetamorphosis > 179; VisualProfession == 10 | TeamCastNano[42056] |
| 270349 | Team Health Plan | NOSTACKING (0) | - | BiologicalMetamorphosis > 873; MaterialMetamorphosis > 786; Profession == 10 | TeamCastNano[270348] |
| 43905 | Team Nano Bandage | NOSTACKING (0) | - | BiologicalMetamorphosis > 543; MaterialMetamorphosis > 492; VisualProfession == 10 | TeamCastNano[43866] |
| 43898 | Team Purification | NOSTACKING (0) | - | BiologicalMetamorphosis > 731; MaterialMetamorphosis > 660; VisualProfession == 10 | TeamCastNano[43850] |
| 43903 | Team Tissue Repair | NOSTACKING (0) | - | BiologicalMetamorphosis > 668; MaterialMetamorphosis > 598; VisualProfession == 10 | TeamCastNano[43851] |
| 42408 | Weak Team Heal | NOSTACKING (0) | - | BiologicalMetamorphosis > 29; MaterialMetamorphosis > 25; VisualProfession == 10 | TeamCastNano[42059] |

## Heals - HoT (18)

| id | name | nano line (strain) | minLvl | cast reqs | effect (sub-nanos resolved) |
| ---: | --- | --- | ---: | --- | --- |
| 43874 | Active Remedy | HealOverTime (12) | - | BiologicalMetamorphosis > 178; MaterialMetamorphosis > 154; SpaceTime > 154; VisualProfession == 10 | Hit[27,30,42,0] |
| 43867 | Continuous Cellular Reconditioning | HealOverTime (12) | - | BiologicalMetamorphosis > 602; MaterialMetamorphosis > 542; SpaceTime > 542; VisualProfession == 10 | Hit[27,139,162,0] |
| 43875 | Course of Treatment | HealOverTime (12) | - | BiologicalMetamorphosis > 417; MaterialMetamorphosis > 374; SpaceTime > 374; VisualProfession == 10 | Hit[27,82,104,0] |
| 42396 | Cycle of Life | HealOverTime (12) | - | BiologicalMetamorphosis > 716; MaterialMetamorphosis > 645; SpaceTime > 645; VisualProfession == 10 | Hit[27,181,218,0] |
| 43868 | Cycle of Reconstruction | HealOverTime (12) | - | BiologicalMetamorphosis > 846; MaterialMetamorphosis > 765; SpaceTime > 765; VisualProfession == 10 | Hit[27,272,307,0] |
| 43852 | Deathless Blessing | HealOverTime (12) | - | BiologicalMetamorphosis > 864; MaterialMetamorphosis > 778; SpaceTime > 778; VisualProfession == 10 | Hit[27,277,323,0] |
| 43876 | Greater Lasting Heal | HealOverTime (12) | - | BiologicalMetamorphosis > 455; MaterialMetamorphosis > 417; SpaceTime > 417; VisualProfession == 10 | Hit[27,92,124,0] |
| 43869 | Greater Periodic Checkup | HealOverTime (12) | - | BiologicalMetamorphosis > 680; MaterialMetamorphosis > 609; SpaceTime > 609; VisualProfession == 10 | Hit[27,160,189,0] |
| 43870 | Greater Policy Payout | HealOverTime (12) | - | BiologicalMetamorphosis > 823; MaterialMetamorphosis > 746; SpaceTime > 746; VisualProfession == 10 | Hit[27,258,293,0] |
| 42399 | Health Pump | HealOverTime (12) | - | BiologicalMetamorphosis > 303; MaterialMetamorphosis > 266; SpaceTime > 266; VisualProfession == 10 | Hit[27,57,78,0] |
| 42401 | Lasting Heal | HealOverTime (12) | - | BiologicalMetamorphosis > 78; MaterialMetamorphosis > 65; SpaceTime > 65; VisualProfession == 10 | Hit[27,10,21,0] |
| 43877 | Lesser Policy Payout | HealOverTime (12) | - | BiologicalMetamorphosis > 531; MaterialMetamorphosis > 482; SpaceTime > 482; VisualProfession == 10 | Hit[27,112,143,0] |
| 43879 | Medical Sequencer | HealOverTime (12) | - | BiologicalMetamorphosis > 349; MaterialMetamorphosis > 304; SpaceTime > 304; VisualProfession == 10 | Hit[27,66,86,0] |
| 43871 | Molecular Rejuvenation | HealOverTime (12) | - | BiologicalMetamorphosis > 741; MaterialMetamorphosis > 669; SpaceTime > 669; VisualProfession == 10 | Hit[27,200,239,0] |
| 43880 | Periodic Checkup | HealOverTime (12) | - | BiologicalMetamorphosis > 142; MaterialMetamorphosis > 120; SpaceTime > 120; VisualProfession == 10 | Hit[27,22,35,0] |
| 43872 | Positive Life Reinforcement | HealOverTime (12) | - | BiologicalMetamorphosis > 806; MaterialMetamorphosis > 732; SpaceTime > 732; VisualProfession == 10 | Hit[27,243,288,0] |
| 43882 | Recurrent Remedy | HealOverTime (12) | - | BiologicalMetamorphosis > 210; MaterialMetamorphosis > 187; SpaceTime > 187; VisualProfession == 10 | Hit[27,38,51,0] |
| 43873 | Superior Health Pump | HealOverTime (12) | - | BiologicalMetamorphosis > 774; MaterialMetamorphosis > 702; SpaceTime > 702; VisualProfession == 10 | Hit[27,229,260,0] |

## Heals - Team HoT (1)

| id | name | nano line (strain) | minLvl | cast reqs | effect (sub-nanos resolved) |
| ---: | --- | --- | ---: | --- | --- |
| 269455 | Team-Enhanced Deathless Blessing | HealOverTime (12) | - | BiologicalMetamorphosis > 913; MaterialMetamorphosis > 810; SpaceTime > 810; Expansion op22 2; Profession == 10 | TeamCastNano[269453] |

## Heal Delta / Regen Buffs (4)

| id | name | nano line (strain) | minLvl | cast reqs | effect (sub-nanos resolved) |
| ---: | --- | --- | ---: | --- | --- |
| 28651 | Cellular Restoration | HealDeltaBuff (203) | - | BiologicalMetamorphosis > 491; VisualProfession == 10; MaterialCreation > 419 | Modify HealDelta +25 |
| 222824 | Continuous Reconstruction | HealDeltaBuff (203) | - | BiologicalMetamorphosis > 691; VisualProfession == 10; MaterialCreation > 619 | Modify HealDelta +65 |
| 222855 | Hearty Constitution | HealDeltaBuff (203) | - | BiologicalMetamorphosis > 577; MaterialCreation > 526; VisualProfession == 10 | Modify HealDelta +48 |
| 28663 | Natural Healing | HealDeltaBuff (203) | - | BiologicalMetamorphosis > 377; MaterialCreation > 326; VisualProfession == 10 | Modify HealDelta +10 |

## HP Buffs (short duration) (14)

| id | name | nano line (strain) | minLvl | cast reqs | effect (sub-nanos resolved) |
| ---: | --- | --- | ---: | --- | --- |
| 96251 | Bodily Amplification | DoctorShortHPBuffs (185) | - | BiologicalMetamorphosis > 215; MaterialMetamorphosis > 191; MaterialCreation > 191; VisualProfession == 10 | Modify MaxHealth +317; CastNano[144654] |
| 96256 | Constitution Magnification | DoctorShortHPBuffs (185) | - | BiologicalMetamorphosis > 697; MaterialMetamorphosis > 625; MaterialCreation > 625; VisualProfession == 10 | Modify MaxHealth +1017; CastNano[144665] |
| 96257 | Enhanced Health Surge | DoctorShortHPBuffs (185) | - | BiologicalMetamorphosis > 638; MaterialMetamorphosis > 573; MaterialCreation > 573; VisualProfession == 10 | Modify MaxHealth +928; CastNano[144663] |
| 96258 | Health Assembler | DoctorShortHPBuffs (185) | - | BiologicalMetamorphosis > 744; MaterialMetamorphosis > 673; MaterialCreation > 673; VisualProfession == 10 | Modify MaxHealth +1123; CastNano[144664] |
| 96248 | Health Augmentation | DoctorShortHPBuffs (185) | - | BiologicalMetamorphosis > 35; MaterialMetamorphosis > 30; MaterialCreation > 30; VisualProfession == 10 | Modify MaxHealth +57; CastNano[144662] |
| 96250 | Health Surge | DoctorShortHPBuffs (185) | - | BiologicalMetamorphosis > 157; MaterialMetamorphosis > 133; MaterialCreation > 133; VisualProfession == 10 | Modify MaxHealth +221; CastNano[144661] |
| 96247 | Life Channeler | DoctorShortHPBuffs (185) | - | BiologicalMetamorphosis > 867; MaterialMetamorphosis > 781; MaterialCreation > 781; VisualProfession == 10 | Modify MaxHealth +1344; CastNano[144660] |
| 96252 | Life Reinforcement | DoctorShortHPBuffs (185) | - | BiologicalMetamorphosis > 251; MaterialMetamorphosis > 225; MaterialCreation > 225; VisualProfession == 10 | Modify MaxHealth +379; CastNano[144659] |
| 96253 | Strengthen Resolve | DoctorShortHPBuffs (185) | - | BiologicalMetamorphosis > 323; MaterialMetamorphosis > 282; MaterialCreation > 282; VisualProfession == 10 | Modify MaxHealth +463; CastNano[144658] |
| 96254 | Superior Health Augmentation | DoctorShortHPBuffs (185) | - | BiologicalMetamorphosis > 405; MaterialMetamorphosis > 359; MaterialCreation > 359; VisualProfession == 10 | Modify MaxHealth +581; CastNano[144657] |
| 96259 | Superior Life Reinforcement | DoctorShortHPBuffs (185) | - | BiologicalMetamorphosis > 802; MaterialMetamorphosis > 729; MaterialCreation > 729; VisualProfession == 10 | Modify MaxHealth +1234; CastNano[144655] |
| 96249 | Survivability Booster | DoctorShortHPBuffs (185) | - | BiologicalMetamorphosis > 83; MaterialMetamorphosis > 69; MaterialCreation > 69; VisualProfession == 10 | Modify MaxHealth +118; CastNano[144656] |
| 275011 | Team Improved Life Channeler | NOSTACKING (0) | - | BiologicalMetamorphosis > 2042; MaterialMetamorphosis > 1727; MaterialCreation > 1727; Profession == 10; NanoFocusLevel op22 64 | TeamCastNano[275130] |
| 96255 | Temporary Cellular Enhancement | DoctorShortHPBuffs (185) | - | BiologicalMetamorphosis > 439; MaterialMetamorphosis > 398; MaterialCreation > 398; VisualProfession == 10 | Modify MaxHealth +644; CastNano[144666] |

## Cures / DoT & Strain Removal (2)

| id | name | nano line (strain) | minLvl | cast reqs | effect (sub-nanos resolved) |
| ---: | --- | --- | ---: | --- | --- |
| 28659 | Epsilon Purge | DOTRemoval (825) | - | SpaceTime > 696; SensoryImprovement > 696; VisualProfession == 10 | RemoveNanoStrain[6]; RemoveNanoStrain[7]; RemoveNanoStrain[8]; RemoveNanoStrain[9]; RemoveNanoStrain[582] |
| 279372 | Tjernberg Soothing Adrenaline | NOSTACKING (0) | - | VisualProfession == 10; SensoryImprovement > 119; SpaceTime > 119 | ReduceNanoStrainDuration[883,1000000] |

## Treatment / First Aid Buffs (3)

| id | name | nano line (strain) | minLvl | cast reqs | effect (sub-nanos resolved) |
| ---: | --- | --- | ---: | --- | --- |
| 28657 | Enhanced First Aid | FirstAidAndTreatmentBuff (190) | - | SensoryImprovement > 60; PsychologicalModification > 73; VisualProfession == 10 | Modify FirstAid +15; Modify Treatment +10 |
| 28674 | Specialist Treatment | FirstAidAndTreatmentBuff (190) | - | SensoryImprovement > 142; PsychologicalModification > 166; VisualProfession == 10 | Modify Treatment +35; Modify FirstAid +20 |
| 28675 | Superior First Aid | FirstAidAndTreatmentBuff (190) | - | SensoryImprovement > 517; PsychologicalModification > 571; VisualProfession == 10 | Modify FirstAid +80; Modify Treatment +80 |

## DoTs (58)

| id | name | nano line (strain) | minLvl | cast reqs | effect (sub-nanos resolved) |
| ---: | --- | --- | ---: | --- | --- |
| 223261 | Mutagenic Venom | DOTStrainC (582) | 100 | BiologicalMetamorphosis > 688; MaterialCreation > 633; Profession == 10; Level > 99; Expansion op22 2; Specialization op22 1 | Hit[27,-230,-230,96] |
| 223263 | Mutagenic Contamination | DOTStrainC (582) | 165 | BiologicalMetamorphosis > 1041; MaterialCreation > 961; Profession == 10; Level > 164; Expansion op22 2; Specialization op22 4 | Hit[27,-564,-564,96] |
| 267649 | Weak Mutagenic Plague | DOTStrainC (582) | 175 | BiologicalMetamorphosis > 1414; MaterialCreation > 1145; Profession == 10; Level > 174; NPCFamily == 0 | Hit[27,-1000,-1000,96] |
| 223265 | Mutagenic Catalyser | DOTStrainC (582) | 195 | BiologicalMetamorphosis > 1134; MaterialCreation > 1041; Profession == 10; Level > 194; Expansion op22 2; Specialization op22 4 | Hit[27,-490,-490,96] |
| 223267 | Viral Neurotoxin | DOT_LineA (6) | 203 | BiologicalMetamorphosis > 1285; MaterialCreation > 1158; Profession == 10; Level > 202; Expansion op22 2; Specialization op22 8 | Hit[27,-550,-550,96] |
| 267623 | Nanite Enhanced Mutagenic Plague | DOTStrainC (582) | 205 | BiologicalMetamorphosis > 1614; MaterialCreation > 1345; Profession == 10; Level > 204; NPCFamily == 0 | Hit[27,-1200,-1200,96] |
| 223269 | Entropic Sores | DOT_LineB (7) | 207 | BiologicalMetamorphosis > 1465; MaterialCreation > 1297; Profession == 10; Level > 206; Expansion op22 2; Specialization op22 8 | Hit[27,-591,-591,96] |
| 223271 | Mutagenic Contagion | DOTStrainC (582) | 211 | BiologicalMetamorphosis > 1645; MaterialCreation > 1436; Profession == 10; Level > 210; Expansion op22 2; Specialization op22 8 | Hit[27,-633,-633,96] |
| 223273 | Corrupting Ooze | DOT_LineA (6) | 213 | BiologicalMetamorphosis > 1735; MaterialCreation > 1506; Profession == 10; Level > 212; Expansion op22 2; Specialization op22 8 | Hit[27,-675,-675,96] |
| 267620 | Dr Blaze's Mutagenic Compound | DOTStrainC (582) | 217 | BiologicalMetamorphosis > 1915; MaterialCreation > 1645; Profession == 10; Level > 216; Flags op33 267619; Flags op4 0; Flags op33 280728; Flags op3 0; Flags op33 267618; Flags op3 0; NPCFamily == 0 | Hit[27,-2000,-2000,96] |
| 223275 | Flesh Eater | DOT_LineB (7) | 217 | BiologicalMetamorphosis > 1915; MaterialCreation > 1645; Profession == 10; Level > 216; Expansion op22 2; Specialization op22 8 | Hit[27,-714,-714,96] |
| 223277 | Mutagenic Pestilence | DOTStrainC (582) | 217 | BiologicalMetamorphosis > 1915; MaterialCreation > 1645; Profession == 10; Level > 216; Expansion op22 2; Specialization op22 8 | Hit[27,-756,-756,96] |
| 267621 | Nanite Enhanced Mutagenic Compound | DOTStrainC (582) | 219 | BiologicalMetamorphosis > 1814; MaterialCreation > 1545; Profession == 10; Level > 218; NPCFamily == 0 | Hit[27,-1500,-1500,96] |
| 223279 | Scythe Omega Virus | DOT_LineA (6) | 219 | BiologicalMetamorphosis > 2005; MaterialCreation > 1714; Profession == 10; Level > 218; Expansion op22 2; Specialization op22 8 | Hit[27,-837,-837,96] |
| 44144 | Abscess Explosion | DOT_LineA (6) | - | BiologicalMetamorphosis > 389; MaterialCreation > 339; VisualProfession == 10 | Hit[27,-76,-76,96] |
| 45324 | Acidic Lesions | DOT_LineA (6) | - | BiologicalMetamorphosis > 712; MaterialCreation > 641; VisualProfession == 10 | Hit[27,-168,-168,96] |
| 44176 | Active Viral Agent | DOT_LineA (6) | - | BiologicalMetamorphosis > 264; MaterialCreation > 235; VisualProfession == 10 | Hit[27,-30,-30,96] |
| 44174 | Advanced Nano Gorger | DOT_LineB (7) | - | BiologicalMetamorphosis > 439; MaterialCreation > 398; VisualProfession == 10 | Hit[27,-84,-84,96] |
| 44175 | All-Consuming Toxin | DOT_LineA (6) | - | BiologicalMetamorphosis > 753; MaterialCreation > 682; VisualProfession == 10 | Hit[27,-315,-315,96] |
| 44173 | Autonomous Viral Agent | DOT_LineB (7) | - | BiologicalMetamorphosis > 695; MaterialCreation > 623; VisualProfession == 10 | Hit[27,-170,-170,96] |
| 28641 | Biotoxin MK I | DOT_LineA (6) | - | BiologicalMetamorphosis > 137; MaterialCreation > 116; VisualProfession == 10 | Hit[27,-13,-13,96] |
| 28642 | Biotoxin MK II | DOT_LineA (6) | - | BiologicalMetamorphosis > 219; MaterialCreation > 195; VisualProfession == 10 | Hit[27,-32,-32,96] |
| 28643 | Biotoxin MK III | DOT_LineA (6) | - | BiologicalMetamorphosis > 363; MaterialCreation > 315; VisualProfession == 10 | Hit[27,-46,-46,96] |
| 28644 | Biotoxin MK IV | DOT_LineA (6) | - | BiologicalMetamorphosis > 451; MaterialCreation > 412; VisualProfession == 10 | Hit[27,-87,-87,96] |
| 273318 | Bone Eater | DOT_LineB (7) | - | BiologicalMetamorphosis > 2042; MaterialCreation > 1727; Profession == 10; NanoFocusLevel op22 64 | Hit[27,-892,-892,96] |
| 44171 | Cancerous Burrower | DOT_LineB (7) | - | BiologicalMetamorphosis > 170; MaterialCreation > 146; VisualProfession == 10 | Hit[27,-23,-23,96] |
| 44172 | Cellular Dismantlement | DOT_LineB (7) | - | BiologicalMetamorphosis > 737; MaterialCreation > 665; VisualProfession == 10 | Hit[27,-290,-290,96] |
| 44169 | Complex Nano Contagion | DOT_LineB (7) | - | BiologicalMetamorphosis > 686; MaterialCreation > 614; VisualProfession == 10 | Hit[27,-143,-143,96] |
| 44170 | Consuming Toxin | DOT_LineA (6) | - | BiologicalMetamorphosis > 512; MaterialCreation > 466; VisualProfession == 10 | Hit[27,-94,-94,96] |
| 44166 | Dark Venom | DOT_LineA (6) | - | BiologicalMetamorphosis > 236; MaterialCreation > 211; VisualProfession == 10 | Hit[27,-37,-37,96] |
| 44167 | Dissolve Molecular Bonding | DOT_LineA (6) | - | BiologicalMetamorphosis > 63; MaterialCreation > 52; VisualProfession == 10 | Hit[27,-4,-4,96] |
| 44168 | Elementary Nano Contagion | DOT_LineB (7) | - | BiologicalMetamorphosis > 78; MaterialCreation > 65; VisualProfession == 10 | Hit[27,-4,-4,96] |
| 44164 | Incandescent Venom | DOT_LineA (6) | - | BiologicalMetamorphosis > 595; MaterialCreation > 537; VisualProfession == 10 | Hit[27,-147,-147,96] |
| 44165 | Infected Wounds | DOT_LineA (6) | - | BiologicalMetamorphosis > 157; MaterialCreation > 133; VisualProfession == 10 | Hit[27,-21,-21,96] |
| 44162 | Internal Decomposition | DOT_LineA (6) | - | BiologicalMetamorphosis > 826; MaterialCreation > 748; VisualProfession == 10 | Hit[27,-392,-392,96] |
| 44163 | Limited Nano Reaper | DOT_LineB (7) | - | BiologicalMetamorphosis > 40; MaterialCreation > 34; VisualProfession == 10 | Hit[27,-4,-4,96] |
| 44160 | Metabolic Disassembly | DOT_LineB (7) | - | BiologicalMetamorphosis > 566; MaterialCreation > 512; VisualProfession == 10 | Hit[27,-124,-124,96] |
| 44161 | Minor Consuming Toxin | DOT_LineA (6) | - | BiologicalMetamorphosis > 182; MaterialCreation > 158; VisualProfession == 10 | Hit[27,-23,-23,96] |
| 44158 | Morgue Longings | DOT_LineA (6) | - | BiologicalMetamorphosis > 815; MaterialCreation > 740; VisualProfession == 10 | Hit[27,-379,-379,96] |
| 44159 | Nano Reaper | DOT_LineB (7) | - | BiologicalMetamorphosis > 473; MaterialCreation > 432; VisualProfession == 10 | Hit[27,-75,-75,96] |
| 44156 | Parasitic Affliction | DOT_LineB (7) | - | BiologicalMetamorphosis > 198; MaterialCreation > 174; VisualProfession == 10 | Hit[27,-54,-54,96] |
| 44157 | Parasitic Horde | DOT_LineB (7) | - | BiologicalMetamorphosis > 725; MaterialCreation > 653; VisualProfession == 10 | Hit[27,-181,-181,96] |
| 44154 | Perpetuating Nano Reaper | DOT_LineB (7) | - | BiologicalMetamorphosis > 644; MaterialCreation > 578; VisualProfession == 10 | Hit[27,-212,-213,96] |
| 263282 | Poisoned Wounds | DOT_LineB (7) | - | MaxHealth op50 3; Expansion op22 2; BiologicalMetamorphosis > 1125; MaterialCreation > 1002; Profession == 10 | Hit[27,-375,-375,96] |
| 44155 | Primitive Nano Gorger | DOT_LineB (7) | - | BiologicalMetamorphosis > 113; MaterialCreation > 95; VisualProfession == 10 | Hit[27,-10,-10,96] |
| 44152 | Primitive Viral Agent | DOT_LineA (6) | - | BiologicalMetamorphosis > 24; MaterialCreation > 21; VisualProfession == 10 | Hit[27,-3,-3,96] |
| 44153 | Protein Breakdown | DOT_LineA (6) | - | BiologicalMetamorphosis > 93; MaterialCreation > 78; VisualProfession == 10 | Hit[27,-8,-8,96] |
| 43376 | Prototype Biotoxin | DOT_LineA (6) | - | BiologicalMetamorphosis > 4; MaterialCreation > 4; VisualProfession == 10 | Hit[27,-3,-3,96] |
| 44150 | Rampant Decay | DOT_LineA (6) | - | BiologicalMetamorphosis > 782; MaterialCreation > 710; VisualProfession == 10 | Hit[27,-174,-174,96] |
| 44151 | Refined Nano Contagion | DOT_LineB (7) | - | BiologicalMetamorphosis > 329; MaterialCreation > 288; VisualProfession == 10 | Hit[27,-41,-41,96] |
| 44147 | Repeated Cellular Trauma | DOT_LineB (7) | - | BiologicalMetamorphosis > 677; MaterialCreation > 606; VisualProfession == 10 | Hit[27,-201,-201,96] |
| 44148 | Scythe A Virus | DOT_LineA (6) | - | BiologicalMetamorphosis > 409; MaterialCreation > 364; VisualProfession == 10 | Hit[27,-40,-40,96] |
| 44149 | Scythe B Virus | DOT_LineA (6) | - | BiologicalMetamorphosis > 858; MaterialCreation > 774; VisualProfession == 10 | Hit[27,-211,-211,96] |
| 44145 | Seethe with Germs | DOT_LineA (6) | - | BiologicalMetamorphosis > 290; MaterialCreation > 256; VisualProfession == 10 | Hit[27,-52,-52,96] |
| 44146 | Sentient Nano Gorger | DOT_LineB (7) | - | BiologicalMetamorphosis > 873; MaterialCreation > 786; VisualProfession == 10 | Hit[27,-431,-431,96] |
| 263281 | Simple Viral Agent | DOT_LineA (6) | - | MaxHealth op50 3; BiologicalMetamorphosis > 1059; MaterialCreation > 875; Profession == 10; Expansion op22 2 | Hit[27,-350,-350,96] |
| 275829 | Sophisticated Viral Agent | DOT_LineA (6) | - | MaxHealth op50 3; BiologicalMetamorphosis > 2105; MaterialCreation > 1814; Profession == 10; NanoFocusLevel op22 64 | Hit[27,-1037,-1037,96] |
| 28640 | Wrack and Ruin | DOT_LineA (6) | - | BiologicalMetamorphosis > 841; MaterialCreation > 760; VisualProfession == 10 | Hit[27,-544,-544,96]; Modify MartialArts -100; Modify _1hBlunt -100; Modify _1hEdged -100; Modify MeleeEnergy -100; Modify Skill2hEdged -100; Modify Piercing -100; Modify _2hBlunt -100; Modify SharpObject -100; Modify Grenade -100; Modify HeavyWeapons -100; Modify Bow -100; Modify Pistol -100; Modify Rifle -100; Modify MGSMG -100; Modify Shotgun -100; Modify AssaultRifle -100; Modify RangedEnergy -100 |

## Nukes / Damage (5)

| id | name | nano line (strain) | minLvl | cast reqs | effect (sub-nanos resolved) |
| ---: | --- | --- | ---: | --- | --- |
| 266303 | Fill Inbox | NOSTACKING (0) | - | Expansion op22 32; MaterialMetamorphosis > 799; Profession == 10; BiologicalMetamorphosis > 799; Profession == 8 | Modify BiologicalMetamorphosis -500; Modify MaterialMetamorphosis -500; Modify MaterialCreation -500; Modify PsychologicalModification -500; Modify SpaceTime -500; Modify SensoryImprovement -500; Hit[214,-750,-750,0] |
| 82009 | First-Degree Burns | NOSTACKING (0) | - | BiologicalMetamorphosis > 549; MaterialCreation > 497; VisualProfession == 10 | Hit[27,-288,-560,97] |
| 82008 | Gouge Flesh | NOSTACKING (0) | - | BiologicalMetamorphosis > 343; MaterialCreation > 299; VisualProfession == 10 | Hit[27,-142,-289,91] |
| 28667 | Inflict Harm | NOSTACKING (0) | - | BiologicalMetamorphosis > 51; MaterialCreation > 43; VisualProfession == 10 | Hit[27,-16,-29,91] |
| 82007 | Shatter Bone | NOSTACKING (0) | - | BiologicalMetamorphosis > 190; MaterialCreation > 166; VisualProfession == 10 | Hit[27,-64,-137,91] |

## Nukes (1)

| id | name | nano line (strain) | minLvl | cast reqs | effect (sub-nanos resolved) |
| ---: | --- | --- | ---: | --- | --- |
| 275701 | Malpractice | Nuke (1041) | - | BiologicalMetamorphosis > 2042; MaterialCreation > 1727; Profession == 10; NanoFocusLevel op22 64 | Hit[27,-1000,-2000,96]; Hit[27,-2000,-3000,96]; Hit[27,-2000,-3000,96]; Hit[27,-2000,-3000,96]; Hit[27,-3000,-4000,96]; Hit[27,-3000,-4000,96]; Hit[27,-3000,-4000,96]; Hit[27,-3500,-5000,96] |

## Debuffs - Initiative (5)

| id | name | nano line (strain) | minLvl | cast reqs | effect (sub-nanos resolved) |
| ---: | --- | --- | ---: | --- | --- |
| 99583 | Induce Muscle Spasms | InitiativeDebuffs (186) | - | BiologicalMetamorphosis > 389; PsychologicalModification > 389; VisualProfession == 10 | TauntNpc[908]; CastNano[301850]; CastNano[301849] |
| 99582 | Muscle Atrophy | InitiativeDebuffs (186) | - | BiologicalMetamorphosis > 93; PsychologicalModification > 93; VisualProfession == 10 | TauntNpc[350]; CastNano[301851]; CastNano[301852] |
| 301845 | Rapid Palsy | InitiativeDebuffs (186) | - | BiologicalMetamorphosis > 569; PsychologicalModification > 569; VisualProfession == 10 | TauntNpc[1113]; CastNano[301847]; CastNano[301848] |
| 99578 | Tired Limbs | InitiativeDebuffs (186) | - | BiologicalMetamorphosis > 29; PsychologicalModification > 29; VisualProfession == 10 | ScalingModify RangedInit -2500; ScalingModify PhysicalInit -2500; ScalingModify MeleeInit -2500; ScalingModify NanoCInit -2500 |
| 99577 | Uncontrollable Body Tremors | InitiativeDebuffs (186) | - | BiologicalMetamorphosis > 861; PsychologicalModification > 861; VisualProfession == 10 | CastNano[301843]; CastNano[301844] |

## Debuffs (1)

| id | name | nano line (strain) | minLvl | cast reqs | effect (sub-nanos resolved) |
| ---: | --- | --- | ---: | --- | --- |
| 266306 | Misdiagnosis | NOSTACKING (0) | - | Expansion op22 32; MaterialMetamorphosis > 799; Profession == 6; BiologicalMetamorphosis > 799; Profession == 10 | Modify HealMultiplier -50 |

## Self / Team Buffs (11)

| id | name | nano line (strain) | minLvl | cast reqs | effect (sub-nanos resolved) |
| ---: | --- | --- | ---: | --- | --- |
| 204425 | Vaccine of Ransacking | Ransack_DepriveResistBuff (287) | 75 | Profession == 10; Level > 74; BiologicalMetamorphosis > 478; MaterialCreation > 432 | ResistNanoStrain[135,14]; ResistNanoStrain[136,14]; ResistNanoStrain[147,3] |
| 204427 | Vaccine of Deprivation | Ransack_DepriveResistBuff (287) | 100 | Profession == 10; Level > 99; BiologicalMetamorphosis > 761; MaterialCreation > 691 | ResistNanoStrain[135,17]; ResistNanoStrain[136,17]; ResistNanoStrain[147,6] |
| 204429 | Vaccine of Plundering | Ransack_DepriveResistBuff (287) | 155 | Profession == 10; Level > 154; BiologicalMetamorphosis > 922; MaterialCreation > 850 | ResistNanoStrain[135,21]; ResistNanoStrain[136,21]; ResistNanoStrain[147,10] |
| 204431 | Vaccine of Divestiture | Ransack_DepriveResistBuff (287) | 195 | Profession == 10; Level > 194; BiologicalMetamorphosis > 1037; MaterialCreation > 951 | ResistNanoStrain[135,26]; ResistNanoStrain[136,26]; ResistNanoStrain[147,15] |
| 28658 | Enlarge | StrengthBuff (156) | - | BiologicalMetamorphosis > 190; MaterialMetamorphosis > 166; MaterialCreation > 166; VisualProfession == 10 | Modify Strength +10; Modify Stamina +10; Modify Scale +8 |
| 222856 | Improved Instinctive Control | InitiativeBuffs (152) | - | PsychologicalModification > 1033; SensoryImprovement > 1033; VisualProfession == 10; Expansion op22 2 | Modify NanoCInit +350; Modify NanoResist +75; Modify MaxNanoEnergy +300 |
| 222823 | Improved Nano Repulsor | NanoResistanceBuffs (204) | - | BiologicalMetamorphosis > 479; SpaceTime > 438; VisualProfession == 10 | Modify NanoResist +200 |
| 28669 | Instinctive Control | InitiativeBuffs (152) | - | PsychologicalModification > 430; SensoryImprovement > 388; VisualProfession == 10 | Modify NanoCInit +200; Modify NanoResist +25; Modify MaxNanoEnergy +150 |
| 42400 | Iron Circle | StrengthBuff (156) | - | BiologicalMetamorphosis > 401; MaterialMetamorphosis > 354; VisualProfession == 10 | Modify Strength +20; Modify Stamina +20 |
| 28671 | Nano Repulsor | NanoResistanceBuffs (204) | - | BiologicalMetamorphosis > 379; SpaceTime > 338; VisualProfession == 10 | Modify NanoResist +92 |
| 29246 | Pistol Mastery | PistolBuff (199) | - | VisualProfession == 1; VisualProfession == 12; VisualProfession == 3; VisualProfession == 10; VisualProfession == 8; PsychologicalModification > 120; SensoryImprovement > 120; Flags op4 0 | Modify Pistol +40 |

## Travel / Summon Utility (2)

| id | name | nano line (strain) | minLvl | cast reqs | effect (sub-nanos resolved) |
| ---: | --- | --- | ---: | --- | --- |
| 142736 | Digitizing Sequencer | TeamGrid (1031) | - | BiologicalMetamorphosis > 656; MaterialMetamorphosis > 656; SpaceTime > 656; VisualProfession == 10; ExpansionPlayfield == 0; PlayfieldType op107 2 | TeamCastNano[142729] |
| 142735 | Encode DNA Sequence | SelfGrid (1030) | - | BiologicalMetamorphosis > 377; MaterialMetamorphosis > 377; SpaceTime > 377; VisualProfession == 10; ExpansionPlayfield == 0; PlayfieldType op107 2 | (1 non-effect functions only) |

