# Meta-Physicist (profession 12) — Castable Nano Reference

Generated 2026-09-21T21:19:54Z for the AOBuddy10 bot. **286 nanos.**

## Method / provenance

- **Nano data (ground truth):** `E:/Funcom/OmniCell/OmniCell/Datafiles/nanos.ocp` — OmniCell OMNICELL-CONTENT v3 pack (client 18.8.50_EP1 extraction), loaded through `OmniCell.Core` `NanoLoader.CacheAllNanos` (net10 DLL). 10965 nano formulas in the pack.
- **Names:** joined by nano id against `itemnames.sql` (`itemnames` table).
- **Enums:** stat ids and nano-line names from `AOSharp.Common/GameData/Stat.cs` and `NanoEnums.cs`.
- **Extractor source:** `E:\Funcom\AOBuddy10\tools\mp-nano-extractor` (re-runnable).
- **No web data was used for any id, name or level.** A web check (AO wiki/aoitems) was used only to confirm the heal-pet line has ~10 strengths, which matches the 10 found locally.

## MP-castability criterion

A nano is included when one of its cast `Actions` (`ActionType.ToUse` = 3) has an `EqualTo` requirement on **`Profession`(stat 60) == 12** or **`VisualProfession`(stat 368) == 12**. VisualProfession is how most older MP nanos are profession-locked. Split: 133 via Profession(60), 153 via VisualProfession(368). Every match uses the EqualTo operator.

## Category counts

| Category | Count |
| --- | ---: |
| Pets - Attack | 51 |
| Pets - Mezz/Support | 17 |
| Pets - Heal | 10 |
| Pets - Charm | 2 |
| Pet Buffs / Pet Utility | 40 |
| Summon Weapon / Shield (Creation) | 32 |
| Self / Team Buffs | 58 |
| Nukes / Damage | 14 |
| Debuffs | 47 |
| Debuffs - Mind/Nano | 3 |
| Mezz / Calm | 1 |
| Heals / HoT | 1 |
| Travel / Recall | 2 |
| Misc / Utility | 8 |
| **Total** | **286** |

## Pets - Attack (51)

| id | name | nano line (strain) | minLvl | cast reqs | effect |
| ---: | --- | --- | ---: | --- | --- |
| 225894 | Summon Biazu the Vile | AttackPets (1015) | 150 | SpaceTime > 874; MaterialCreation > 874; Profession == 12; Pets op66 1; Expansion op22 2; Specialization op22 4; Level > 149 | SummonPet template=[0,180,-1]; SummonPet template=[0,175,-1]; SummonPet template=[0,170,-1]; SummonPet template=[0,165,-1]; SummonPet template=[0,160,-1] |
| 225896 | Summon Urolok the Rotten | AttackPets (1015) | 175 | SpaceTime > 1150; MaterialCreation > 1150; Profession == 12; Pets op66 1; Expansion op22 2; Specialization op22 4; Level > 174 | SummonPet template=[0,200,-1]; SummonPet template=[0,195,-1]; SummonPet template=[0,190,-1]; SummonPet template=[0,185,-1]; SummonPet template=[0,180,-1] |
| 225898 | Summon Ettu the Cursed | AttackPets (1015) | 201 | SpaceTime > 1300; MaterialCreation > 1300; Profession == 12; Pets op66 1; Expansion op22 2; Specialization op22 8; Level > 200 | SummonPet template=[0,212,-1]; SummonPet template=[0,209,-1]; SummonPet template=[0,206,-1]; SummonPet template=[0,203,-1]; SummonPet template=[0,200,-1] |
| 225900 | Summon Zhok the Abomination | AttackPets (1015) | 201 | SpaceTime > 1700; MaterialCreation > 1700; Profession == 12; Pets op66 1; Expansion op22 2; Specialization op22 8; Level > 200 | SummonPet template=[0,219,-1]; SummonPet template=[0,218,-1]; SummonPet template=[0,216,-1]; SummonPet template=[0,214,-1]; SummonPet template=[0,212,-1] |
| 254859 | Summon The Rihwen | AttackPets (1015) | 220 | SpaceTime > 2039; MaterialCreation > 2044; Profession == 12; Pets op66 1; Expansion op22 2; Specialization op22 8; Level > 219 | SummonPet template=[0,225,-1]; SummonPet template=[0,223,-1]; SummonPet template=[0,220,-1] |
| 43715 | Anger Manifestation | AttackPets (1015) | - | SpaceTime > 24; MaterialCreation > 24; VisualProfession == 12; Pets op66 1 | SummonPet template=[0,3,-1] |
| 43716 | Enmity Personification | AttackPets (1015) | - | SpaceTime > 741; MaterialCreation > 741; Profession == 12; Pets op66 1 | SummonPet template=[0,155,-1] |
| 43717 | Frenzy Embodiment | AttackPets (1015) | - | SpaceTime > 602; MaterialCreation > 602; VisualProfession == 12; Pets op66 1 | SummonPet template=[0,113,-1] |
| 43718 | Fury Externalization | AttackPets (1015) | - | SpaceTime > 108; MaterialCreation > 108; VisualProfession == 12; Pets op66 1 | SummonPet template=[0,19,-1] |
| 43719 | Greater Anger Manifestation | AttackPets (1015) | - | SpaceTime > 40; MaterialCreation > 40; VisualProfession == 12; Pets op66 1 | SummonPet template=[0,6,-1] |
| 43720 | Greater Enmity Personification | AttackPets (1015) | - | SpaceTime > 771; MaterialCreation > 771; Profession == 12; Pets op66 1 | SummonPet template=[0,167,-1] |
| 43721 | Greater Frenzy Embodiment | AttackPets (1015) | - | SpaceTime > 675; MaterialCreation > 675; VisualProfession == 12; Pets op66 1 | SummonPet template=[0,125,-1] |
| 43722 | Greater Fury Externalization | AttackPets (1015) | - | SpaceTime > 137; MaterialCreation > 137; VisualProfession == 12; Pets op66 1 | SummonPet template=[0,25,-1] |
| 43723 | Greater Rage Materialization | AttackPets (1015) | - | SpaceTime > 257; MaterialCreation > 257; VisualProfession == 12; Pets op66 1 | SummonPet template=[0,52,-1] |
| 43724 | Greater Wrath Incarnation | AttackPets (1015) | - | SpaceTime > 434; MaterialCreation > 434; VisualProfession == 12; Pets op66 1 | SummonPet template=[0,84,-1] |
| 43725 | Inferior Anger Manifestation | AttackPets (1015) | - | SpaceTime > 18; MaterialCreation > 18; VisualProfession == 12; Pets op66 1 | SummonPet template=[0,2,-1] |
| 43726 | Inferior Enmity Personification | AttackPets (1015) | - | SpaceTime > 729; MaterialCreation > 729; Profession == 12; Pets op66 1 | SummonPet template=[0,149,-1] |
| 43727 | Inferior Frenzy Embodiment | AttackPets (1015) | - | SpaceTime > 566; MaterialCreation > 566; VisualProfession == 12; Pets op66 1 | SummonPet template=[0,107,-1] |
| 43728 | Inferior Fury Externalization | AttackPets (1015) | - | SpaceTime > 93; MaterialCreation > 93; VisualProfession == 12; Pets op66 1 | SummonPet template=[0,16,-1] |
| 43729 | Inferior Rage Materialization | AttackPets (1015) | - | SpaceTime > 202; MaterialCreation > 202; VisualProfession == 12; Pets op66 1 | SummonPet template=[0,40,-1] |
| 43730 | Inferior Wrath Incarnation | AttackPets (1015) | - | SpaceTime > 385; MaterialCreation > 385; VisualProfession == 12; Pets op66 1 | SummonPet template=[0,72,-1] |
| 43736 | Rage Materialization | AttackPets (1015) | - | SpaceTime > 219; MaterialCreation > 219; VisualProfession == 12; Pets op66 1 | SummonPet template=[0,44,-1] |
| 43324 | Summon Anger Manifestation | AttackPets (1015) | - | SpaceTime > 3; MaterialCreation > 3; VisualProfession == 12; Pets op66 1 | SummonPet template=[0,10,-1]; SummonPet template=[0,8,-1]; SummonPet template=[0,6,-1]; SummonPet template=[0,4,-1]; SummonPet template=[0,3,-1]; SummonPet template=[0,2,-1]; SummonPet template=[0,1,-1] |
| 29318 | Summon Demon | AttackPets (1015) | - | SpaceTime > 864; MaterialCreation > 864; Profession == 12; Pets op66 1 | SummonPet template=[0,197,-1] |
| 43737 | Summon Demon | AttackPets (1015) | - | SpaceTime > 788; MaterialCreation > 788; VisualProfession == 12; Pets op66 1 | SummonPet template=[0,200,-1]; SummonPet template=[0,197,-1]; SummonPet template=[0,191,-1]; SummonPet template=[0,187,-1]; SummonPet template=[0,185,-1]; SummonPet template=[0,183,-1]; SummonPet template=[0,180,-1] |
| 43731 | Summon Enmity Personification | AttackPets (1015) | - | SpaceTime > 692; MaterialCreation > 692; VisualProfession == 12; Pets op66 1 | SummonPet template=[0,179,-1]; SummonPet template=[0,173,-1]; SummonPet template=[0,167,-1]; SummonPet template=[0,161,-1]; SummonPet template=[0,155,-1]; SummonPet template=[0,149,-1]; SummonPet template=[0,143,-1] |
| 43738 | Summon Fiend | AttackPets (1015) | - | SpaceTime > 846; MaterialCreation > 846; Profession == 12; Pets op66 1 | SummonPet template=[0,191,-1] |
| 43732 | Summon Frenzy Embodiment | AttackPets (1015) | - | SpaceTime > 456; MaterialCreation > 456; VisualProfession == 12; Pets op66 1 | SummonPet template=[0,137,-1]; SummonPet template=[0,131,-1]; SummonPet template=[0,125,-1]; SummonPet template=[0,119,-1]; SummonPet template=[0,113,-1]; SummonPet template=[0,107,-1]; SummonPet template=[0,101,-1] |
| 43733 | Summon Fury Externalization | AttackPets (1015) | - | SpaceTime > 64; MaterialCreation > 64; VisualProfession == 12; Pets op66 1 | SummonPet template=[0,32,-1]; SummonPet template=[0,28,-1]; SummonPet template=[0,25,-1]; SummonPet template=[0,22,-1]; SummonPet template=[0,19,-1]; SummonPet template=[0,16,-1]; SummonPet template=[0,13,-1] |
| 29319 | Summon Lemur | AttackPets (1015) | - | SpaceTime > 829; MaterialCreation > 829; Profession == 12; Pets op66 1 | SummonPet template=[0,185,-1] |
| 43734 | Summon Rage Materialization | AttackPets (1015) | - | SpaceTime > 171; MaterialCreation > 171; VisualProfession == 12; Pets op66 1 | SummonPet template=[0,62,-1]; SummonPet template=[0,57,-1]; SummonPet template=[0,52,-1]; SummonPet template=[0,48,-1]; SummonPet template=[0,44,-1]; SummonPet template=[0,40,-1]; SummonPet template=[0,36,-1] |
| 43735 | Summon Wrath Incarnation | AttackPets (1015) | - | SpaceTime > 290; MaterialCreation > 290; VisualProfession == 12; Pets op66 1 | SummonPet template=[0,95,-1]; SummonPet template=[0,89,-1]; SummonPet template=[0,84,-1]; SummonPet template=[0,79,-1]; SummonPet template=[0,76,-1]; SummonPet template=[0,72,-1]; SummonPet template=[0,67,-1] |
| 43739 | Superior Anger Manifestation | AttackPets (1015) | - | SpaceTime > 29; MaterialCreation > 29; VisualProfession == 12; Pets op66 1 | SummonPet template=[0,4,-1] |
| 43740 | Superior Enmity Personification | AttackPets (1015) | - | SpaceTime > 753; MaterialCreation > 753; Profession == 12; Pets op66 1 | SummonPet template=[0,161,-1] |
| 43741 | Superior Frenzy Embodiment | AttackPets (1015) | - | SpaceTime > 638; MaterialCreation > 638; VisualProfession == 12; Pets op66 1 | SummonPet template=[0,119,-1] |
| 43742 | Superior Fury Externalization | AttackPets (1015) | - | SpaceTime > 122; MaterialCreation > 122; VisualProfession == 12; Pets op66 1 | SummonPet template=[0,22,-1] |
| 43743 | Superior Rage Materialization | AttackPets (1015) | - | SpaceTime > 236; MaterialCreation > 236; VisualProfession == 12; Pets op66 1 | SummonPet template=[0,48,-1] |
| 43744 | Superior Wrath Incarnation | AttackPets (1015) | - | SpaceTime > 413; MaterialCreation > 413; VisualProfession == 12; Pets op66 1 | SummonPet template=[0,79,-1] |
| 43745 | Supreme Anger Manifestation | AttackPets (1015) | - | SpaceTime > 51; MaterialCreation > 51; VisualProfession == 12; Pets op66 1 | SummonPet template=[0,8,-1] |
| 43746 | Supreme Enmity Personification | AttackPets (1015) | - | SpaceTime > 788; MaterialCreation > 788; Profession == 12; Pets op66 1 | SummonPet template=[0,173,-1] |
| 43747 | Supreme Frenzy Embodiment | AttackPets (1015) | - | SpaceTime > 691; MaterialCreation > 691; VisualProfession == 12; Pets op66 1 | SummonPet template=[0,131,-1] |
| 43748 | Supreme Fury Externalization | AttackPets (1015) | - | SpaceTime > 152; MaterialCreation > 152; VisualProfession == 12; Pets op66 1 | SummonPet template=[0,28,-1] |
| 43749 | Supreme Rage Materialization | AttackPets (1015) | - | SpaceTime > 290; MaterialCreation > 290; VisualProfession == 12; Pets op66 1 | SummonPet template=[0,57,-1] |
| 43750 | Supreme Wrath Incarnation | AttackPets (1015) | - | SpaceTime > 455; MaterialCreation > 455; VisualProfession == 12; Pets op66 1 | SummonPet template=[0,89,-1] |
| 43751 | Transcendent Anger Manifestation | AttackPets (1015) | - | SpaceTime > 63; MaterialCreation > 63; VisualProfession == 12; Pets op66 1 | SummonPet template=[0,10,-1] |
| 43918 | Transcendent Enmity Personification | AttackPets (1015) | - | SpaceTime > 810; MaterialCreation > 810; Profession == 12; Pets op66 1 | SummonPet template=[0,179,-1] |
| 43753 | Transcendent Frenzy Embodiment | AttackPets (1015) | - | SpaceTime > 703; MaterialCreation > 703; VisualProfession == 12; Pets op66 1 | SummonPet template=[0,137,-1] |
| 43919 | Transcendent Fury Externalization | AttackPets (1015) | - | SpaceTime > 170; MaterialCreation > 170; VisualProfession == 12; Pets op66 1 | SummonPet template=[0,32,-1] |
| 43920 | Transcendent Rage Materialization | AttackPets (1015) | - | SpaceTime > 323; MaterialCreation > 323; VisualProfession == 12; Pets op66 1 | SummonPet template=[0,62,-1] |
| 43756 | Transcendent Wrath Incarnation | AttackPets (1015) | - | SpaceTime > 493; MaterialCreation > 493; VisualProfession == 12; Pets op66 1 | SummonPet template=[0,95,-1] |
| 43714 | Wrath Incarnation | AttackPets (1015) | - | SpaceTime > 401; MaterialCreation > 401; VisualProfession == 12; Pets op66 1 | SummonPet template=[0,76,-1] |

## Pets - Mezz/Support (17)

| id | name | nano line (strain) | minLvl | cast reqs | effect |
| ---: | --- | --- | ---: | --- | --- |
| 156123 | Deranged Mindreaver | SupportPets (1017) | - | SpaceTime > 198; MaterialMetamorphosis > 198; Profession == 12; Pets op66 2001 | SummonPet template=[0,39,-1] |
| 156124 | Distracting Sphere | SupportPets (1017) | - | SpaceTime > 63; MaterialMetamorphosis > 63; Profession == 12; Pets op66 2001 | SummonPet template=[0,10,-1] |
| 156125 | Greater Deranged Mindreaver | SupportPets (1017) | - | SpaceTime > 240; MaterialMetamorphosis > 240; Profession == 12; Pets op66 2001 | SummonPet template=[0,49,-1] |
| 156126 | Greater Distracting Sphere | SupportPets (1017) | - | SpaceTime > 88; MaterialMetamorphosis > 88; Profession == 12; Pets op66 2001 | SummonPet template=[0,15,-1] |
| 156127 | Lesser Deranged Mindreaver | SupportPets (1017) | - | SpaceTime > 166; MaterialMetamorphosis > 166; Profession == 12; Pets op66 2001 | SummonPet template=[0,31,-1] |
| 156128 | Lesser Distracting Sphere | SupportPets (1017) | - | SpaceTime > 35; MaterialMetamorphosis > 35; Profession == 12; Pets op66 2001 | SummonPet template=[0,5,-1] |
| 156129 | Summoning of Absuum | SupportPets (1017) | - | SpaceTime > 385; MaterialMetamorphosis > 385; Profession == 12; Pets op66 2001 | SummonPet template=[0,72,-1] |
| 156131 | Summoning of Balbuto the Gibberer | SupportPets (1017) | - | SpaceTime > 817; MaterialMetamorphosis > 817; Profession == 12; Pets op66 2001 | SummonPet template=[0,181,-1] |
| 156118 | Summoning of Confane | SupportPets (1017) | - | SpaceTime > 768; MaterialMetamorphosis > 768; Profession == 12; Pets op66 2001 | SummonPet template=[0,166,-1] |
| 156121 | Summoning of Demenus | SupportPets (1017) | - | SpaceTime > 560; MaterialMetamorphosis > 560; Profession == 12; Pets op66 2001 | SummonPet template=[0,106,-1] |
| 156119 | Summoning of Distral | SupportPets (1017) | - | SpaceTime > 731; MaterialMetamorphosis > 731; Profession == 12; Pets op66 2001 | SummonPet template=[0,150,-1] |
| 156120 | Summoning of Duoco | SupportPets (1017) | - | SpaceTime > 699; MaterialMetamorphosis > 699; Profession == 12; Pets op66 2001 | SummonPet template=[0,135,-1] |
| 156122 | Summoning of Ignatus Mind-Clouder | SupportPets (1017) | - | SpaceTime > 460; MaterialMetamorphosis > 460; Profession == 12; Pets op66 2001 | SummonPet template=[0,90,-1] |
| 150309 | Summoning of Tumulten | SupportPets (1017) | - | SpaceTime > 870; MaterialMetamorphosis > 870; Profession == 12; Pets op66 2001 | SummonPet template=[0,199,-1] |
| 269516 | Summoning of Yidira | SupportPets (1017) | - | SpaceTime > 1297; MaterialMetamorphosis > 1297; Profession == 12; Expansion op22 2; Pets op66 2001 | SummonPet template=[0,210,-1] |
| 156130 | Supreme Deranged Mindreaver | SupportPets (1017) | - | SpaceTime > 310; MaterialMetamorphosis > 310; Profession == 12; Pets op66 2001 | SummonPet template=[0,60,-1] |
| 156117 | Supreme Distracting Sphere | SupportPets (1017) | - | SpaceTime > 113; MaterialMetamorphosis > 113; Profession == 12; Pets op66 2001 | SummonPet template=[0,20,-1] |

## Pets - Heal (10)

| id | name | nano line (strain) | minLvl | cast reqs | effect |
| ---: | --- | --- | ---: | --- | --- |
| 225902 | Calling of Mortificant the Eternal | HealPets (1016) | 207 | SpaceTime > 1462; BiologicalMetamorphosis > 1465; Profession == 12; Pets op66 1001; Level > 206; Expansion op22 2; Specialization op22 8 | SummonPet template=[0,215,-1]; SummonPet template=[0,215,-1]; SummonPet template=[0,215,-1]; SummonPet template=[0,215,-1]; SummonPet template=[0,215,-1]; SummonPet template=[0,215,-1]; SummonPet template=[0,215,-1] |
| 125740 | Calling of Altumus | HealPets (1016) | - | SpaceTime > 723; BiologicalMetamorphosis > 723; VisualProfession == 12; Pets op66 1001 | SummonPet template=[0,146,-1]; SummonPet template=[0,146,-1]; SummonPet template=[0,146,-1]; SummonPet template=[0,146,-1]; SummonPet template=[0,146,-1]; SummonPet template=[0,146,-1]; SummonPet template=[0,146,-1] |
| 125746 | Calling of Belamorte | HealPets (1016) | - | SpaceTime > 849; BiologicalMetamorphosis > 849; VisualProfession == 12; Pets op66 1001 | SummonPet template=[0,192,-1]; SummonPet template=[0,192,-1]; SummonPet template=[0,192,-1]; SummonPet template=[0,192,-1]; SummonPet template=[0,192,-1]; SummonPet template=[0,192,-1]; SummonPet template=[0,192,-1] |
| 125739 | Calling of Curatem The Grand | HealPets (1016) | - | SpaceTime > 777; BiologicalMetamorphosis > 777; VisualProfession == 12; Pets op66 1001 | SummonPet template=[0,169,-1]; SummonPet template=[0,169,-1]; SummonPet template=[0,169,-1]; SummonPet template=[0,169,-1]; SummonPet template=[0,169,-1]; SummonPet template=[0,169,-1]; SummonPet template=[0,169,-1] |
| 125738 | Calling of Medinos | HealPets (1016) | - | SpaceTime > 83; BiologicalMetamorphosis > 83; VisualProfession == 12; Pets op66 1001 | SummonPet template=[0,14,-1]; SummonPet template=[0,14,-1]; SummonPet template=[0,14,-1]; SummonPet template=[0,14,-1]; SummonPet template=[0,14,-1]; SummonPet template=[0,14,-1]; SummonPet template=[0,14,-1] |
| 125742 | Calling of Restite | HealPets (1016) | - | SpaceTime > 518; BiologicalMetamorphosis > 518; VisualProfession == 12; Pets op66 1001 | SummonPet template=[0,99,-1]; SummonPet template=[0,99,-1]; SummonPet template=[0,99,-1]; SummonPet template=[0,99,-1]; SummonPet template=[0,99,-1]; SummonPet template=[0,99,-1]; SummonPet template=[0,99,-1] |
| 125745 | Calling of Salvinous | HealPets (1016) | - | SpaceTime > 174; BiologicalMetamorphosis > 174; VisualProfession == 12; Pets op66 1001 | SummonPet template=[0,33,-1]; SummonPet template=[0,33,-1]; SummonPet template=[0,33,-1]; SummonPet template=[0,33,-1]; SummonPet template=[0,33,-1]; SummonPet template=[0,33,-1]; SummonPet template=[0,33,-1] |
| 125743 | Calling of Sanoo | HealPets (1016) | - | SpaceTime > 405; BiologicalMetamorphosis > 405; VisualProfession == 12; Pets op66 1001 | SummonPet template=[0,77,-1]; SummonPet template=[0,77,-1]; SummonPet template=[0,77,-1]; SummonPet template=[0,77,-1]; SummonPet template=[0,77,-1]; SummonPet template=[0,77,-1]; SummonPet template=[0,77,-1] |
| 125741 | Calling of The Vivificator | HealPets (1016) | - | SpaceTime > 662; BiologicalMetamorphosis > 662; VisualProfession == 12; Pets op66 1001 | SummonPet template=[0,123,-1]; SummonPet template=[0,123,-1]; SummonPet template=[0,123,-1]; SummonPet template=[0,123,-1]; SummonPet template=[0,123,-1]; SummonPet template=[0,123,-1]; SummonPet template=[0,123,-1] |
| 125744 | Calling of Valentyia | HealPets (1016) | - | SpaceTime > 277; BiologicalMetamorphosis > 277; VisualProfession == 12; Pets op66 1001 | SummonPet template=[0,55,-1]; SummonPet template=[0,55,-1]; SummonPet template=[0,55,-1]; SummonPet template=[0,55,-1]; SummonPet template=[0,55,-1]; SummonPet template=[0,55,-1]; SummonPet template=[0,55,-1] |

## Pets - Charm (2)

| id | name | nano line (strain) | minLvl | cast reqs | effect |
| ---: | --- | --- | ---: | --- | --- |
| 269907 | Pet Steal Back | Charm_Short (1022) | 100 | VisualProfession == 3; VisualProfession == 8; VisualProfession == 12; MaterialCreation > 769; SpaceTime > 769; Level > 99; Breed == 7 | RemoveNanoStrain[202] |
| 269908 | Improved Pet Steal Back | Charm_Short (1022) | 215 | VisualProfession == 3; VisualProfession == 8; VisualProfession == 12; MaterialCreation > 1399; SpaceTime > 1399; Level > 214; Breed == 7 | RemoveNanoStrain[202] |

## Pet Buffs / Pet Utility (40)

| id | name | nano line (strain) | minLvl | cast reqs | effect |
| ---: | --- | --- | ---: | --- | --- |
| 205195 | Evocation of Unleashed Malice | PetShortTermDamageBuffs (225) | 25 | MaterialCreation > 208; BiologicalMetamorphosis > 208; SpaceTime > 208; Profession == 12; Level > 24; Breed == 7; NPCFamily == 97 | Skill[282,57]; Skill[281,57]; Skill[317,57]; Skill[316,57]; Skill[311,57]; Skill[280,57]; Skill[278,57]; Skill[279,57]; Modify AddAllOff +52 |
| 205191 | Evocation of Relentless Fury | PetShortTermDamageBuffs (225) | 50 | MaterialCreation > 349; BiologicalMetamorphosis > 349; SpaceTime > 349; Profession == 12; Level > 49; Breed == 7; NPCFamily == 97 | Skill[282,77]; Skill[281,77]; Skill[317,77]; Skill[316,77]; Skill[311,77]; Skill[280,77]; Skill[278,77]; Skill[279,77]; Modify AddAllOff +88 |
| 205183 | Evocation of Fathomless Rage | PetShortTermDamageBuffs (225) | 75 | MaterialCreation > 519; BiologicalMetamorphosis > 519; SpaceTime > 519; Profession == 12; Level > 74; Breed == 7; NPCFamily == 97 | Skill[282,108]; Skill[281,108]; Skill[317,108]; Skill[316,108]; Skill[311,108]; Skill[280,108]; Skill[278,108]; Skill[279,108]; Modify AddAllOff +129 |
| 205185 | Evocation of Implacable Hatred | PetShortTermDamageBuffs (225) | 100 | MaterialCreation > 733; BiologicalMetamorphosis > 733; SpaceTime > 733; Profession == 12; Level > 99; Breed == 7; NPCFamily == 97 | Skill[282,143]; Skill[281,143]; Skill[317,143]; Skill[316,143]; Skill[311,143]; Skill[280,143]; Skill[278,143]; Skill[279,143]; Modify AddAllOff +175 |
| 205187 | Evocation of Maddening Wrath | PetShortTermDamageBuffs (225) | 135 | MaterialCreation > 852; BiologicalMetamorphosis > 852; SpaceTime > 852; Profession == 12; Level > 134; Breed == 7; NPCFamily == 97 | Skill[282,177]; Skill[281,177]; Skill[317,177]; Skill[316,177]; Skill[311,177]; Skill[280,177]; Skill[278,177]; Skill[279,177]; Modify AddAllOff +213 |
| 205189 | Evocation of Pure Malevolence | PetShortTermDamageBuffs (225) | 175 | MaterialCreation > 974; BiologicalMetamorphosis > 974; SpaceTime > 974; Profession == 12; Level > 174; Breed == 7; NPCFamily == 97 | Skill[282,227]; Skill[281,227]; Skill[317,227]; Skill[316,227]; Skill[311,227]; Skill[280,227]; Skill[278,227]; Skill[279,227]; Modify AddAllOff +269 |
| 205193 | Evocation of The Abomination | PetShortTermDamageBuffs (225) | 195 | MaterialCreation > 1046; BiologicalMetamorphosis > 1046; SpaceTime > 1046; Profession == 12; Level > 194; Breed == 7; NPCFamily == 97 | Skill[280,268]; Skill[278,268]; Skill[279,268]; Skill[282,268]; Skill[281,268]; Skill[317,268]; Skill[316,268]; Skill[311,268]; Modify AddAllOff +304 |
| 267533 | Sacrifice Combat pet | PetShortTermDamageBuffs (225) | 195 | MaterialCreation > 1500; BiologicalMetamorphosis > 1500; SpaceTime > 1500; Profession == 12; Level > 194; Breed == 7; NPCFamily == 97 | Hit[27,-8000,-8000,94]; Hit[27,-8000,-8000,96]; Skill[280,268]; Skill[278,268]; Skill[279,268]; Skill[282,268]; Skill[281,268]; Skill[317,268]; Skill[316,268]; Skill[311,268]; Modify AddAllOff +400 |
| 267599 | Evocation of The Enlightened | PetDefensiveNanos (816) | 201 | MaterialCreation > 1300; BiologicalMetamorphosis > 1300; SpaceTime > 1300; Profession == 12; Level > 200; Flags op118 0 | ResistNanoStrain[147,15]; ResistNanoStrain[202,15]; Modify AddAllDef +250 |
| 267598 | Evocation of The Enraged | PetShortTermDamageBuffs (225) | 201 | MaterialCreation > 1200; BiologicalMetamorphosis > 1200; SpaceTime > 1200; Profession == 12; Level > 200; Breed == 7; NPCFamily == 97 | Skill[280,275]; Skill[278,275]; Skill[279,275]; Skill[282,275]; Skill[281,275]; Skill[317,275]; Skill[316,275]; Skill[311,275]; Modify AddAllOff +500 |
| 267617 | Evocation of The Cleansed | PetDamageOverTimeResistNanos (817) | 205 | MaterialCreation > 1500; BiologicalMetamorphosis > 1500; SpaceTime > 1500; Profession == 12; Level > 204; Flags op118 0 | Modify NanoResist +300; ResistNanoStrain[582,25]; ResistNanoStrain[7,25]; ResistNanoStrain[9,25] |
| 267600 | Evocation of The Empowered | PetDefensiveNanos (816) | 215 | MaterialCreation > 1500; BiologicalMetamorphosis > 1500; SpaceTime > 1500; Profession == 12; Level > 214; Flags op118 0 | ResistNanoStrain[147,20]; ResistNanoStrain[202,20]; Modify AddAllDef +300 |
| 267609 | Evocation of The Pure | PetDamageOverTimeResistNanos (817) | 219 | MaterialCreation > 1900; BiologicalMetamorphosis > 1900; SpaceTime > 1900; Profession == 12; Level > 218; Flags op118 0 | Modify NanoResist +600; ResistNanoStrain[582,50]; ResistNanoStrain[7,50]; ResistNanoStrain[9,50] |
| 151830 | Anima of Fathomless Rage | PetShortTermDamageBuffs (225) | - | MaterialCreation > 409; BiologicalMetamorphosis > 409; SpaceTime > 409; VisualProfession == 12; Breed == 7; NPCFamily == 97 | Skill[278,105]; Skill[279,105]; Skill[280,105]; Skill[281,105]; Skill[282,105]; Skill[311,105]; Skill[316,105]; Skill[317,105] |
| 151824 | Anima of Implacable Hatred | PetShortTermDamageBuffs (225) | - | MaterialCreation > 571; BiologicalMetamorphosis > 571; SpaceTime > 571; VisualProfession == 12; Breed == 7; NPCFamily == 97 | Skill[278,140]; Skill[279,140]; Skill[280,140]; Skill[281,140]; Skill[282,140]; Skill[311,140]; Skill[316,140]; Skill[317,140] |
| 151828 | Anima of Maddening Wrath | PetShortTermDamageBuffs (225) | - | MaterialCreation > 697; BiologicalMetamorphosis > 697; SpaceTime > 697; VisualProfession == 12; Breed == 7; NPCFamily == 97 | Skill[278,171]; Skill[279,171]; Skill[280,171]; Skill[281,171]; Skill[282,171]; Skill[311,171]; Skill[316,171]; Skill[317,171] |
| 151829 | Anima of Pure Malevolence | PetShortTermDamageBuffs (225) | - | MaterialCreation > 780; BiologicalMetamorphosis > 780; SpaceTime > 780; VisualProfession == 12; Breed == 7; NPCFamily == 97 | Skill[278,219]; Skill[279,219]; Skill[280,219]; Skill[281,219]; Skill[282,219]; Skill[311,219]; Skill[316,219]; Skill[317,219] |
| 151826 | Anima of Relentless Fury | PetShortTermDamageBuffs (225) | - | MaterialCreation > 251; BiologicalMetamorphosis > 251; SpaceTime > 251; VisualProfession == 12; Breed == 7; NPCFamily == 97 | Skill[278,76]; Skill[279,76]; Skill[280,76]; Skill[281,76]; Skill[282,76]; Skill[311,76]; Skill[316,76]; Skill[317,76] |
| 151827 | Anima of The Abomination | PetShortTermDamageBuffs (225) | - | MaterialCreation > 873; BiologicalMetamorphosis > 873; SpaceTime > 873; VisualProfession == 12; Breed == 7; NPCFamily == 97 | Skill[278,254]; Skill[279,254]; Skill[280,254]; Skill[281,254]; Skill[282,254]; Skill[311,254]; Skill[316,254]; Skill[317,254] |
| 151825 | Anima of Unleashed Malice | PetShortTermDamageBuffs (225) | - | MaterialCreation > 147; BiologicalMetamorphosis > 147; SpaceTime > 147; VisualProfession == 12; Breed == 7; NPCFamily == 97 | Skill[278,55]; Skill[279,55]; Skill[280,55]; Skill[281,55]; Skill[282,55]; Skill[311,55]; Skill[316,55]; Skill[317,55] |
| 151831 | Anima of Unrestrained Ferocity | PetShortTermDamageBuffs (225) | - | MaterialCreation > 57; BiologicalMetamorphosis > 57; SpaceTime > 57; VisualProfession == 12; Breed == 7; NPCFamily == 97 | Skill[278,36]; Skill[279,36]; Skill[280,36]; Skill[281,36]; Skill[282,36]; Skill[311,36]; Skill[316,36]; Skill[317,36] |
| 116811 | Chant of Effortless Strikes | MPPetInitiativeBuffs (217) | - | MaterialCreation > 608; SensoryImprovement > 547; VisualProfession == 12; Breed == 7; NPCFamily == 97 | Modify AggDef +101; Modify RangedInit +510; Modify NanoCInit +510; Modify PhysicalInit +510; Modify MeleeInit +510; Modify Aggressiveness +50 |
| 116820 | Chant of Frenzied Blows | MPPetInitiativeBuffs (217) | - | MaterialCreation > 217; SensoryImprovement > 174; VisualProfession == 12; Breed == 7; NPCFamily == 97 | Modify AggDef +73; Modify RangedInit +360; Modify NanoCInit +360; Modify PhysicalInit +360; Modify MeleeInit +360; Modify Aggressiveness +30 |
| 205197 | Evocation of Unrestrained Ferocity | PetShortTermDamageBuffs (225) | - | MaterialCreation > 106; BiologicalMetamorphosis > 106; SpaceTime > 106; Profession == 12; Breed == 7; NPCFamily == 97 | Skill[279,37]; Skill[282,37]; Skill[281,37]; Skill[317,37]; Skill[316,37]; Skill[311,37]; Skill[280,37]; Skill[278,37]; Modify AddAllOff +24 |
| 273374 | Healthy Manifestation | PetHealDelta843 (843) | - | MaterialCreation > 2208; BiologicalMetamorphosis > 2108; Profession == 12; NanoFocusLevel op22 64; Breed == 7; NPCFamily == 96; NPCFamily == 97; NPCFamily == 98; Flags op4 0 | CastNano[274392] |
| 116819 | High Chant of Effortless Strikes | MPPetInitiativeBuffs (217) | - | MaterialCreation > 861; SensoryImprovement > 776; VisualProfession == 12; Breed == 7; NPCFamily == 97 | Modify AggDef +184; Modify PhysicalInit +920; Modify MeleeInit +920; Modify RangedInit +920; Modify NanoCInit +920; Modify Aggressiveness +80 |
| 116818 | High Chant of Frenzied Blows | MPPetInitiativeBuffs (217) | - | MaterialCreation > 739; SensoryImprovement > 667; VisualProfession == 12; Breed == 7; NPCFamily == 97 | Modify AggDef +139; Modify RangedInit +700; Modify NanoCInit +700; Modify PhysicalInit +700; Modify MeleeInit +700; Modify Aggressiveness +65 |
| 116817 | Instill With Enduring Wrath | MPPetDamageBuffs (216) | - | MaterialCreation > 765; BiologicalMetamorphosis > 765; VisualProfession == 12; Breed == 7; NPCFamily == 97 | Skill[278,52]; Skill[279,52]; Skill[280,52]; Skill[281,52]; Skill[282,52]; Skill[311,52]; Skill[316,52]; Skill[317,52] |
| 116816 | Instill With Ferocious Purpose | MPPetDamageBuffs (216) | - | MaterialCreation > 525; BiologicalMetamorphosis > 525; VisualProfession == 12; Breed == 7; NPCFamily == 97 | Skill[278,26]; Skill[279,26]; Skill[280,26]; Skill[281,26]; Skill[282,26]; Skill[311,26]; Skill[316,26]; Skill[317,26] |
| 116815 | Instill With Fury | MPPetDamageBuffs (216) | - | MaterialCreation > 190; BiologicalMetamorphosis > 190; VisualProfession == 12; Breed == 7; NPCFamily == 97 | Skill[278,18]; Skill[279,18]; Skill[280,18]; Skill[281,18]; Skill[282,18]; Skill[311,18]; Skill[316,18]; Skill[317,18] |
| 116814 | Instill With Malign Intent | MPPetDamageBuffs (216) | - | MaterialCreation > 873; BiologicalMetamorphosis > 873; VisualProfession == 12; Breed == 7; NPCFamily == 97 | Skill[278,61]; Skill[279,61]; Skill[280,61]; Skill[281,61]; Skill[282,61]; Skill[311,61]; Skill[316,61]; Skill[317,61] |
| 116813 | Instill With Rage | MPPetDamageBuffs (216) | - | MaterialCreation > 103; BiologicalMetamorphosis > 103; VisualProfession == 12; Breed == 7; NPCFamily == 97 | Skill[278,13]; Skill[279,13]; Skill[280,13]; Skill[281,13]; Skill[282,13]; Skill[311,13]; Skill[316,13]; Skill[317,13] |
| 116812 | Instill With Righteous Frenzy | MPPetDamageBuffs (216) | - | MaterialCreation > 689; BiologicalMetamorphosis > 689; VisualProfession == 12; Breed == 7; NPCFamily == 97 | Skill[278,41]; Skill[279,41]; Skill[280,41]; Skill[281,41]; Skill[282,41]; Skill[311,41]; Skill[316,41]; Skill[317,41] |
| 116821 | Instill With Terrible Anger | MPPetDamageBuffs (216) | - | MaterialCreation > 349; BiologicalMetamorphosis > 349; VisualProfession == 12; Breed == 7; NPCFamily == 97 | Skill[278,24]; Skill[279,24]; Skill[280,24]; Skill[281,24]; Skill[282,24]; Skill[311,24]; Skill[316,24]; Skill[317,24] |
| 269869 | Pet Attention | PetRoot (1013) | - | VisualProfession == 3; VisualProfession == 8; VisualProfession == 12; MaterialCreation > 769; SpaceTime > 769 | (2 non-effect functions only) |
| 269870 | Pet Cleanse | PetDebuffCleanse (1047) | - | VisualProfession == 3; VisualProfession == 8; VisualProfession == 12; MaterialCreation > 1399; SpaceTime > 1399 | (2 non-effect functions only) |
| 300505 | Sacrificial Bond | DamageToPet (1024) | - | Profession == 12; Expansion op22 32 | Modify ShoulderMesh +75; Modify AccessCount +25 |
| 300506 | Sacrificial Bond | DamageToPet (1024) | - | MaterialMetamorphosis > 1200; PsychologicalModification > 1000; Profession == 12; Expansion op22 32 | CastNano[300505] |
| 267281 | Sacrificial Shielding | DamageToPet (1024) | - | MaterialMetamorphosis > 1800; PsychologicalModification > 1600; Profession == 12; Expansion op22 32 | RemoveNanoStrain[146]; RemoveNanoStrain[145]; ResistNanoStrain[146,60]; ResistNanoStrain[145,60]; Modify ShoulderMesh +90; Modify AccessCount +100; CastNano[300505] |
| 275853 | Touch of Poison | MPAttackPetDamageType (863) | - | BiologicalMetamorphosis > 2146; MaterialCreation > 2146; Profession == 12; NanoFocusLevel op22 64; Breed == 7; NPCFamily == 97 | ChangeVariable[339,96]; Skill[317,30]; Modify AddAllOff +120 |

## Summon Weapon / Shield (Creation) (32)

| id | name | nano line (strain) | minLvl | cast reqs | effect |
| ---: | --- | --- | ---: | --- | --- |
| 229922 | Boon of Ergo - Aban-Shere | NOSTACKING (0) | 20 | Profession == 12; Level > 19; MaterialCreation > 199; SpaceTime > 199; Expansion op22 2 | SpawnItem[UNCS,25,0] |
| 204828 | Lien's Crystalizer | AttackPets (1015) | 25 | Profession == 12; Level > 24; MaterialCreation > 264; SpaceTime > 264 | SpawnItem[CREN,1,0] |
| 229926 | Boon of Ergo - Ocra-Bhotaar | NOSTACKING (0) | 40 | Profession == 12; Level > 39; MaterialCreation > 299; SpaceTime > 299; Expansion op22 2 | SpawnItem[UNCS,50,0] |
| 229928 | Boon of Ergo - Ocra-Thar | NOSTACKING (0) | 60 | Profession == 12; Level > 59; MaterialCreation > 399; SpaceTime > 399; Expansion op22 2 | SpawnItem[UNCS,75,0] |
| 229930 | Boon of Ergo - Ocra-Roch | NOSTACKING (0) | 80 | Profession == 12; Level > 79; MaterialCreation > 499; SpaceTime > 499; Expansion op22 2 | SpawnItem[UNCS,100,0] |
| 229932 | Boon of Ergo - Ocra-Xum | NOSTACKING (0) | 100 | Profession == 12; Level > 99; MaterialCreation > 599; SpaceTime > 599; Expansion op22 2 | SpawnItem[UNCS,125,0] |
| 229934 | Boon of Ergo - Ocra-Shere | NOSTACKING (0) | 120 | Profession == 12; Level > 119; MaterialCreation > 699; SpaceTime > 699; Expansion op22 2 | SpawnItem[UNCS,150,0] |
| 206752 | Saemus' Crystalizer | AttackPets (1015) | 135 | Profession == 12; Level > 134; MaterialCreation > 852; SpaceTime > 852 | SpawnItem[CNCR,1,0] |
| 229936 | Boon of Ergo - Enel-Bhotaar | NOSTACKING (0) | 140 | Profession == 12; Level > 139; MaterialCreation > 799; SpaceTime > 799; Expansion op22 2 | SpawnItem[UNCS,175,0] |
| 229938 | Boon of Ergo - Enel-Thar | NOSTACKING (0) | 170 | Profession == 12; Level > 169; MaterialCreation > 999; SpaceTime > 999; Expansion op22 2 | SpawnItem[UNCS,200,0] |
| 229940 | Boon of Ergo - Enel-Roch | NOSTACKING (0) | 200 | Profession == 12; Level > 199; MaterialCreation > 1199; SpaceTime > 1199; Expansion op22 2 | SpawnItem[UNCS,250,0] |
| 229942 | Boon of Ergo - Enel-Xum | NOSTACKING (0) | 220 | Profession == 12; Level > 219; MaterialCreation > 1499; SpaceTime > 1499; Expansion op22 2 | SpawnItem[UPCR,300,0] |
| 154983 | Creation: Asp of Semol | NOSTACKING (0) | - | MaterialCreation > 603; BiologicalMetamorphosis > 603; SpaceTime > 603; Profession == 12 | SpawnItem[ASOF,116,0] |
| 275849 | Creation: Asp of Titaniush | NOSTACKING (0) | - | MaterialCreation > 2208; BiologicalMetamorphosis > 2108; SpaceTime > 2208; Profession == 12; NanoFocusLevel op22 64 | SpawnItem[AFOT,215,0] |
| 154981 | Creation: Azure Cobra of Orma | NOSTACKING (0) | - | MaterialCreation > 1029; BiologicalMetamorphosis > 1029; SpaceTime > 1029; Profession == 12 | SpawnItem[AZCO,198,0] |
| 154968 | Creation: Belthior's Flame Ward | NOSTACKING (0) | - | MaterialCreation > 748; BiologicalMetamorphosis > 748; SpaceTime > 748; Profession == 12 | SpawnItem[BEFL,144,0] |
| 154978 | Creation: Bitis Striker | NOSTACKING (0) | - | MaterialCreation > 691; BiologicalMetamorphosis > 691; SpaceTime > 691; Profession == 12 | SpawnItem[BISR,133,0] |
| 154979 | Creation: Coplan's Hand Taipan | NOSTACKING (0) | - | MaterialCreation > 457; BiologicalMetamorphosis > 457; SpaceTime > 457; Profession == 12 | SpawnItem[COHN,88,0] |
| 154972 | Creation: Death Ward | NOSTACKING (0) | - | MaterialCreation > 912; BiologicalMetamorphosis > 912; SpaceTime > 912; Profession == 12 | SpawnItem[DEWA,171,0] |
| 154977 | Creation: Gold Acantophis | NOSTACKING (0) | - | MaterialCreation > 971; BiologicalMetamorphosis > 971; SpaceTime > 971; Profession == 12 | SpawnItem[GOAC,187,0] |
| 154969 | Creation: Living Shield of Evernan | NOSTACKING (0) | - | MaterialCreation > 567; BiologicalMetamorphosis > 567; SpaceTime > 567; Profession == 12 | SpawnItem[LVSH,109,0] |
| 154974 | Creation: Mocham's Guard | NOSTACKING (0) | - | MaterialCreation > 935; BiologicalMetamorphosis > 935; SpaceTime > 935; Profession == 12 | SpawnItem[MOGU,180,0] |
| 154976 | Creation: Notum Defender | NOSTACKING (0) | - | MaterialCreation > 353; BiologicalMetamorphosis > 353; SpaceTime > 353; Profession == 12 | SpawnItem[NODE,68,0] |
| 154971 | Creation: Shield of Asmodian | NOSTACKING (0) | - | MaterialCreation > 1034; BiologicalMetamorphosis > 1034; SpaceTime > 1034; Profession == 12 | SpawnItem[SHOA,199,0] |
| 275852 | Creation: Shield of Esa | NOSTACKING (0) | - | MaterialCreation > 2208; BiologicalMetamorphosis > 2108; SpaceTime > 2208; Profession == 12; NanoFocusLevel op22 64 | SpawnItem[KLXI,215,0] |
| 273376 | Creation: Shield of Zset | NOSTACKING (0) | - | MaterialCreation > 2208; BiologicalMetamorphosis > 2108; SpaceTime > 2208; Profession == 12; NanoFocusLevel op22 64; Flags op106 2 | SpawnItem[SLOZ,215,0]; SpawnItem[SZOZ,215,0] |
| 154973 | Creation: Solar Guard | NOSTACKING (0) | - | MaterialCreation > 426; BiologicalMetamorphosis > 426; SpaceTime > 426; Profession == 12 | SpawnItem[SOGU,82,0] |
| 154980 | Creation: The Crotalus | NOSTACKING (0) | - | MaterialCreation > 235; BiologicalMetamorphosis > 235; SpaceTime > 235; Profession == 12 | SpawnItem[THCR,45,0] |
| 154984 | Creation: Viper Staff | NOSTACKING (0) | - | MaterialCreation > 327; BiologicalMetamorphosis > 327; SpaceTime > 327; Profession == 12 | SpawnItem[VIST,63,0] |
| 154970 | Creation: Vital Buckler | NOSTACKING (0) | - | MaterialCreation > 291; BiologicalMetamorphosis > 291; SpaceTime > 291; Profession == 12 | SpawnItem[VIBU,56,0] |
| 154975 | Creation: Wave Breaker | NOSTACKING (0) | - | MaterialCreation > 650; BiologicalMetamorphosis > 650; SpaceTime > 650; Profession == 12 | SpawnItem[WABR,125,0] |
| 154982 | Creation: Wixel's Notum Python | NOSTACKING (0) | - | MaterialCreation > 815; BiologicalMetamorphosis > 815; SpaceTime > 815; Profession == 12 | SpawnItem[WINO,157,0] |

## Self / Team Buffs (58)

| id | name | nano line (strain) | minLvl | cast reqs | effect |
| ---: | --- | --- | ---: | --- | --- |
| 220331 | Composite Teachings | SenseImpBuff (165) | 25 | PsychologicalModification > 151; SensoryImprovement > 151; VisualProfession == 12; Level > 24; Expansion op22 2; Specialization op22 1; Expansion op22 2; Level > 14 | Modify SpaceTime +25; Modify MaterialMetamorphosis +25; Modify BiologicalMetamorphosis +25; Modify MaterialCreation +25; Modify SensoryImprovement +25; Modify PsychologicalModification +25 |
| 220333 | Composite Mastery | SenseImpBuff (165) | 50 | PsychologicalModification > 291; SensoryImprovement > 291; VisualProfession == 12; Level > 49; Expansion op22 2; Specialization op22 1; Expansion op22 2; Level > 39 | Modify SpaceTime +50; Modify MaterialMetamorphosis +50; Modify BiologicalMetamorphosis +50; Modify MaterialCreation +50; Modify SensoryImprovement +50; Modify PsychologicalModification +50 |
| 220335 | Composite Infuse With Knowledge | SenseImpBuff (165) | 100 | PsychologicalModification > 632; SensoryImprovement > 632; Profession == 12; Level > 99; Expansion op22 2; Specialization op22 1; Expansion op22 2; Level > 89 | Modify SpaceTime +90; Modify MaterialMetamorphosis +90; Modify BiologicalMetamorphosis +90; Modify MaterialCreation +90; Modify SensoryImprovement +90; Modify PsychologicalModification +90 |
| 220337 | Composite Mochams (1 hour) | SenseImpBuff (165) | 185 | PsychologicalModification > 1028; SensoryImprovement > 1028; Profession == 12; Level > 184; Expansion op22 2; Specialization op22 4; Expansion op22 2; Level > 174 | Modify SpaceTime +140; Modify MaterialMetamorphosis +140; Modify BiologicalMetamorphosis +140; Modify MaterialCreation +140; Modify SensoryImprovement +140; Modify PsychologicalModification +140 |
| 220339 | Composite Mochams (2 hours) | SenseImpBuff (165) | 205 | PsychologicalModification > 1228; SensoryImprovement > 1226; Profession == 12; Level > 204; Expansion op22 2; Specialization op22 8; Expansion op22 2; Level > 200 | Modify SpaceTime +140; Modify MaterialMetamorphosis +140; Modify BiologicalMetamorphosis +140; Modify MaterialCreation +140; Modify SensoryImprovement +140; Modify PsychologicalModification +140 |
| 275020 | Aggressive Construct Empowerment | AggressiveConstructEmpowerment (854) | 212 | BiologicalMetamorphosis > 2108; SpaceTime > 2208; Profession == 12; NanoFocusLevel op22 64; Level > 211; NPCFamily == 97 | CastNano[275021]; Modify AddAllOff +300 |
| 220341 | Composite Mochams (4 hours) | SenseImpBuff (165) | 215 | PsychologicalModification > 1575; SensoryImprovement > 1572; Profession == 12; Level > 214; Expansion op22 2; Specialization op22 8; Expansion op22 2; Level > 204 | Modify SpaceTime +140; Modify MaterialMetamorphosis +140; Modify BiologicalMetamorphosis +140; Modify MaterialCreation +140; Modify SensoryImprovement +140; Modify PsychologicalModification +140 |
| 220343 | Composite Mochams (8 hours) | SenseImpBuff (165) | 219 | PsychologicalModification > 1714; SensoryImprovement > 1710; Profession == 12; Level > 218; Expansion op22 2; Specialization op22 8; Expansion op22 2; Level > 208 | Modify SpaceTime +140; Modify MaterialMetamorphosis +140; Modify BiologicalMetamorphosis +140; Modify MaterialCreation +140; Modify SensoryImprovement +140; Modify PsychologicalModification +140 |
| 29327 | Advanced Symbol Manipulation | NanoProgrammingBuff (210) | - | PsychologicalModification > 438; SensoryImprovement > 438; VisualProfession == 12 | Modify NanoProgramming +92 |
| 29272 | Anticipation of Retaliation | MajorEvasionBuffs (144) | - | SensoryImprovement > 246; PsychologicalModification > 246; VisualProfession == 12 | Modify DuckExp +60; Modify DodgeRanged +60; Modify EvadeClsC +60 |
| 117219 | Assume Profession: Meta-Physicist | FalseProfession (218) | - | VisualProfession == 5; VisualProfession == 12; Profession == 5; PsychologicalModification > 532; SensoryImprovement > 532; BiologicalMetamorphosis > 532 | RemoveNanoStrain[680]; Skill[318,70]; ModifyPercentage PsychologicalModification -15; ModifyPercentage SensoryImprovement -15; ModifyPercentage MaterialMetamorphosis -15; ModifyPercentage MaterialCreation -15; ModifyPercentage SpaceTime -15; ModifyPercentage BiologicalMetamorphosis -15; ChangeVariable[368,12] |
| 29275 | BioMet Mastery | BioMetBuff (163) | - | PsychologicalModification > 183; SensoryImprovement > 183; VisualProfession == 12 | Modify BiologicalMetamorphosis +50 |
| 95411 | Coherent Notum Web | NPCostBuff (148) | - | MaterialMetamorphosis > 421; BiologicalMetamorphosis > 421; PsychologicalModification > 378; VisualProfession == 12 | Skill[318,-15] |
| 95525 | Dedication of Thought | InterruptModifier (183) | - | SpaceTime > 705; PsychologicalModification > 634; SensoryImprovement > 634; VisualProfession == 12 | Skill[383,51] |
| 95410 | Ease of Execution | NPCostBuff (148) | - | MaterialMetamorphosis > 117; BiologicalMetamorphosis > 117; PsychologicalModification > 99; VisualProfession == 12 | Skill[318,-7] |
| 95523 | Engrossing Activity | InterruptModifier (183) | - | SpaceTime > 232; PsychologicalModification > 207; SensoryImprovement > 207; VisualProfession == 12 | Skill[383,34] |
| 302257 | Eye of the Tigress | MartialArtistBowBuffs (815) | - | Expansion op22 2; PsychologicalModification > 513; SensoryImprovement > 513; Profession == 12 | Modify Bow +40; Modify AimedShot +40 |
| 32036 | False Profession: Meta-Physicist | FalseProfession (218) | - | VisualProfession == 5; VisualProfession == 12; Profession == 5; PsychologicalModification > 179; SensoryImprovement > 179; BiologicalMetamorphosis > 179 | RemoveNanoStrain[680]; Skill[318,150]; ModifyPercentage PsychologicalModification -25; ModifyPercentage SensoryImprovement -25; ModifyPercentage MaterialMetamorphosis -25; ModifyPercentage MaterialCreation -25; ModifyPercentage SpaceTime -25; ModifyPercentage BiologicalMetamorphosis -25; ChangeVariable[368,12] |
| 95524 | Ignore External Events | InterruptModifier (183) | - | SpaceTime > 439; PsychologicalModification > 398; SensoryImprovement > 398; VisualProfession == 12 | Skill[383,41] |
| 302188 | Improved Anticipation of Retaliation | MajorEvasionBuffs (144) | - | SensoryImprovement > 1149; PsychologicalModification > 1149; Profession == 12; NanoFocusLevel op22 64 | Modify DuckExp +150; Modify DodgeRanged +150; Modify EvadeClsC +150 |
| 266311 | Induce Stupor | NOSTACKING (0) | - | Expansion op22 32; BiologicalMetamorphosis > 799; Profession == 2; SensoryImprovement > 799; Profession == 12 | Modify BiologicalMetamorphosis -750; Modify MaterialMetamorphosis -750; Modify MaterialCreation -750; Modify PsychologicalModification -750; Modify SpaceTime -750; Modify SensoryImprovement -750 |
| 151761 | Infuse With Knowledge: Biological Metamorphose | BioMetBuff (163) | - | PsychologicalModification > 568; SensoryImprovement > 568; VisualProfession == 12 | Modify BiologicalMetamorphosis +90 |
| 151762 | Infuse With Knowledge: Material Creation | MatCreaBuff (159) | - | PsychologicalModification > 606; SensoryImprovement > 606; VisualProfession == 12 | Modify MaterialCreation +90 |
| 151759 | Infuse With Knowledge: Material Metamorphose | MatMetBuff (157) | - | PsychologicalModification > 583; SensoryImprovement > 583; VisualProfession == 12 | Modify MaterialMetamorphosis +90 |
| 151760 | Infuse With Knowledge: Psychological Modification | PsyModBuff (167) | - | PsychologicalModification > 623; SensoryImprovement > 623; VisualProfession == 12 | Modify PsychologicalModification +90 |
| 151757 | Infuse With Knowledge: Sensory Improvement | SenseImpBuff (165) | - | PsychologicalModification > 614; SensoryImprovement > 614; VisualProfession == 12 | Modify SensoryImprovement +90 |
| 151758 | Infuse With Knowledge: Time and Space | MatLocBuff (161) | - | PsychologicalModification > 598; SensoryImprovement > 598; VisualProfession == 12 | Modify SpaceTime +90 |
| 95521 | Internal Focus | InterruptModifier (183) | - | SpaceTime > 73; PsychologicalModification > 60; SensoryImprovement > 60; VisualProfession == 12 | Skill[383,27] |
| 29294 | MatCrea Mastery | MatCreaBuff (159) | - | VisualProfession == 12; VisualProfession == 11; PsychologicalModification > 207; SensoryImprovement > 207 | Modify MaterialCreation +50 |
| 29296 | MatMet Mastery | MatMetBuff (157) | - | PsychologicalModification > 235; SensoryImprovement > 235; VisualProfession == 12 | Modify MaterialMetamorphosis +50 |
| 263298 | Mesmerizing Construct Empowerment | MesmerizationConstructEmpowerment (810) | - | Expansion op22 2; BiologicalMetamorphosis > 569; SpaceTime > 567; Profession == 12; NPCFamily == 98 | CastNano[263297]; Modify PsychologicalModification +80; Modify SensoryImprovement +80 |
| 117208 | Mimic Profession: Meta-Physicist | FalseProfession (218) | - | VisualProfession == 5; VisualProfession == 12; Profession == 5; PsychologicalModification > 767; SensoryImprovement > 767; BiologicalMetamorphosis > 767 | RemoveNanoStrain[680]; Skill[318,15]; ModifyPercentage PsychologicalModification -5; ModifyPercentage SensoryImprovement -5; ModifyPercentage MaterialMetamorphosis -5; ModifyPercentage MaterialCreation -5; ModifyPercentage SpaceTime -5; ModifyPercentage BiologicalMetamorphosis -5; ChangeVariable[368,12] |
| 29299 | Mocham's Gift: BioMet | BioMetBuff (163) | - | PsychologicalModification > 758; SensoryImprovement > 758; VisualProfession == 12 | Modify BiologicalMetamorphosis +140 |
| 29300 | Mocham's Gift: MatCrea | MatCreaBuff (159) | - | PsychologicalModification > 765; SensoryImprovement > 765; VisualProfession == 12 | Modify MaterialCreation +140 |
| 29302 | Mocham's Gift: MatMet | MatMetBuff (157) | - | PsychologicalModification > 751; SensoryImprovement > 751; VisualProfession == 12 | Modify MaterialMetamorphosis +140 |
| 29303 | Mocham's Gift: PsyMod | PsyModBuff (167) | - | PsychologicalModification > 774; SensoryImprovement > 774; VisualProfession == 12 | Modify PsychologicalModification +140 |
| 29304 | Mocham's Gift: SenseImp | SenseImpBuff (165) | - | PsychologicalModification > 769; SensoryImprovement > 769; VisualProfession == 12 | Modify SensoryImprovement +140 |
| 29301 | Mocham's Gift: SpaceTime | MatLocBuff (161) | - | PsychologicalModification > 744; SensoryImprovement > 744; VisualProfession == 12 | Modify SpaceTime +140 |
| 95409 | Mocham's Neural Interface-Web | NPCostBuff (148) | - | MaterialMetamorphosis > 742; BiologicalMetamorphosis > 742; PsychologicalModification > 671; VisualProfession == 12 | Skill[318,-24] |
| 29307 | Neuron-Notum Interface | NPCostBuff (148) | - | MaterialMetamorphosis > 615; BiologicalMetamorphosis > 615; PsychologicalModification > 553; VisualProfession == 12 | Skill[318,-19] |
| 220345 | Neuronal Stimulator | Psy_IntBuff (576) | - | Profession == 11; Profession == 12; Profession == 8; BiologicalMetamorphosis > 193; PsychologicalModification > 193; Specialization op22 1; Expansion op22 2; Expansion op22 2 | Modify Intelligence +23; Modify Psychic +23 |
| 95408 | Notum Attunement | NPCostBuff (148) | - | MaterialMetamorphosis > 283; BiologicalMetamorphosis > 283; PsychologicalModification > 251; VisualProfession == 12 | Skill[318,-12] |
| 29308 | Notum Rejection | NPCostBuff (148) | - | BiologicalMetamorphosis > 701; PsychologicalModification > 629; VisualProfession == 12 | Skill[318,25] |
| 29309 | Odin's Missing Eye | Psy_IntBuff (576) | - | BiologicalMetamorphosis > 785; PsychologicalModification > 713; VisualProfession == 12 | Modify Intelligence +43; Modify Psychic +43; Modify PsychologicalModification +40; Modify SensoryImprovement +40; Modify SpaceTime +40; Modify MaterialCreation +40; Modify BiologicalMetamorphosis +40; Modify MaterialMetamorphosis +40; Modify NanoResist +100 |
| 273379 | Odin's Other Eye | Psy_IntBuff (576) | - | BiologicalMetamorphosis > 2048; PsychologicalModification > 1836; Profession == 12; NanoFocusLevel op22 64 | Modify Intelligence +103; Modify Psychic +103; Modify PsychologicalModification +100; Modify SensoryImprovement +100; Modify SpaceTime +100; Modify MaterialCreation +100; Modify BiologicalMetamorphosis +100; Modify MaterialMetamorphosis +100; Modify NanoResist +200 |
| 95522 | One Mind, One Purpose | InterruptModifier (183) | - | SpaceTime > 777; PsychologicalModification > 705; SensoryImprovement > 705; VisualProfession == 12 | Skill[383,57] |
| 29246 | Pistol Mastery | PistolBuff (199) | - | VisualProfession == 1; VisualProfession == 12; VisualProfession == 3; VisualProfession == 10; VisualProfession == 8; PsychologicalModification > 120; SensoryImprovement > 120; Flags op4 0 | Modify Pistol +40 |
| 29312 | PsyMod Mastery | PsyModBuff (167) | - | PsychologicalModification > 225; SensoryImprovement > 225; VisualProfession == 12 | Modify PsychologicalModification +50 |
| 120499 | Quantum Wings | NOSTACKING (0) | - | Flags op70 2; SpaceTime > 479; BiologicalMetamorphosis > 479; MaterialMetamorphosis > 479; VisualProfession == 12; PlayfieldType op107 1; ExpansionPlayfield == 0; Flags op112 0 | Modify RunSpeed +600 |
| 29315 | Sense Imp Mastery | SenseImpBuff (165) | - | PsychologicalModification > 215; SensoryImprovement > 215; VisualProfession == 12 | Modify SensoryImprovement +50 |
| 29295 | SpaceTime Mastery | MatLocBuff (161) | - | PsychologicalModification > 199; SensoryImprovement > 199; VisualProfession == 12 | Modify SpaceTime +50 |
| 29113 | Symbol Helper | NanoProgrammingBuff (210) | - | PsychologicalModification > 60; SensoryImprovement > 60; VisualProfession == 12 | Modify NanoProgramming +20 |
| 151768 | Teachings of Biological Metamorphose | BioMetBuff (163) | - | PsychologicalModification > 90; SensoryImprovement > 90; VisualProfession == 12 | Modify BiologicalMetamorphosis +25 |
| 151765 | Teachings of Material Creation | MatCreaBuff (159) | - | VisualProfession == 12; VisualProfession == 11; PsychologicalModification > 107; SensoryImprovement > 107 | Modify MaterialCreation +25 |
| 151767 | Teachings of Material Metamorphose | MatMetBuff (157) | - | PsychologicalModification > 95; SensoryImprovement > 95; VisualProfession == 12 | Modify MaterialMetamorphosis +25 |
| 151763 | Teachings of Psychological Modification | PsyModBuff (167) | - | PsychologicalModification > 116; SensoryImprovement > 116; VisualProfession == 12 | Modify PsychologicalModification +25 |
| 151764 | Teachings of Sensory Improvement | SenseImpBuff (165) | - | PsychologicalModification > 111; SensoryImprovement > 111; VisualProfession == 12 | Modify SensoryImprovement +25 |
| 151766 | Teachings of Time and Space | MatLocBuff (161) | - | PsychologicalModification > 99; SensoryImprovement > 99; VisualProfession == 12 | Modify SpaceTime +25 |

## Nukes / Damage (14)

| id | name | nano line (strain) | minLvl | cast reqs | effect |
| ---: | --- | --- | ---: | --- | --- |
| 203609 | Succor of Expediuum | NOSTACKING (0) | 155 | Profession == 12; MaterialCreation > 918; SpaceTime > 918; Level > 154; Breed == 7 | ReduceNanoStrainDuration[145,756]; ReduceNanoStrainDuration[146,756]; Hit[27,150,150,0] |
| 125760 | Chill Spear | NOSTACKING (0) | - | MaterialCreation > 555; PsychologicalModification > 502; VisualProfession == 12 | Hit[27,-314,-699,95]; Hit[27,-562,-1004,95] |
| 125765 | Frigid Blast | NOSTACKING (0) | - | MaterialCreation > 303; PsychologicalModification > 266; VisualProfession == 12 | Hit[27,-160,-288,95]; Hit[27,-284,-432,95] |
| 125764 | Frost Slivers | NOSTACKING (0) | - | MaterialCreation > 93; PsychologicalModification > 78; VisualProfession == 12 | Hit[27,-54,-65,95]; Hit[27,-73,-85,95] |
| 125763 | Glacial Lance | NOSTACKING (0) | - | MaterialCreation > 750; PsychologicalModification > 679; VisualProfession == 12 | Hit[27,-1093,-1959,95]; Hit[27,-602,-1268,95] |
| 267878 | Metaing's Improved Glacial Lance | NOSTACKING (0) | - | MaterialCreation > 941; PsychologicalModification > 854; Profession == 12 | Hit[27,-898,-1848,95]; Hit[27,-1533,-2725,95] |
| 270355 | Metaing's Improved Mind Quake | NOSTACKING (0) | - | MaterialCreation > 1080; PsychologicalModification > 981; Profession == 12 | Hit[27,-1095,-2254,96]; CastNano[270354]; TauntNpc[1491] |
| 29297 | Mind Banshee | NOSTACKING (0) | - | MaterialCreation > 693; PsychologicalModification > 621; VisualProfession == 12 | Hit[27,-484,-946,96]; CastNano[144617] |
| 125762 | Mind Howl | NOSTACKING (0) | - | MaterialCreation > 413; PsychologicalModification > 369; VisualProfession == 12 | Hit[27,-237,-439,96]; CastNano[144618] |
| 29114 | Mind Pain | NOSTACKING (0) | - | MaterialCreation > 4; PsychologicalModification > 4; VisualProfession == 12 | Hit[27,-9,-23,96]; CastNano[144616] |
| 125761 | Mind Quake | NOSTACKING (0) | - | MaterialCreation > 817; PsychologicalModification > 742; VisualProfession == 12 | Hit[27,-736,-1515,96]; CastNano[144620]; TauntNpc[1491] |
| 29298 | Mind Scream | NOSTACKING (0) | - | MaterialCreation > 182; PsychologicalModification > 158; VisualProfession == 12 | Hit[27,-78,-146,96]; CastNano[144619] |
| 267531 | Sacrifical power | DOTStrainC (582) | - | MaterialMetamorphosis > 1800; PsychologicalModification > 1600; Profession == 12; Breed == 7; NPCFamily == 97; Flags op118 0 | Modify AddAllOff +200; Hit[27,-800,-800,94]; Hit[27,-800,-800,96] |
| 267391 | Sacrifical Shielding | DOTStrainC (582) | - | MaterialMetamorphosis > 1800; PsychologicalModification > 1600; Profession == 12; Flags op118 0 | Modify AddAllOff +200; Hit[27,-800,-800,94]; Hit[27,-800,-800,96]; CastNano[267278] |

## Debuffs (47)

| id | name | nano line (strain) | minLvl | cast reqs | effect |
| ---: | --- | --- | ---: | --- | --- |
| 227109 | Distortion of Resolve | MPDamageDebuffLineB (659) | 145 | MaterialMetamorphosis > 971; PsychologicalModification > 889; Profession == 12; Level > 144; Specialization op22 2; Expansion op22 2; Level > 114; Flags op91 227149 | Modify ProjectileDamageModifier -72; Modify EnergyDamageModifier -72; Modify MeleeDamageModifier -72; Modify ColdDamageModifier -72; Modify FireDamageModifier -72; Modify PoisonDamageModifier -72; Modify ChemicalDamageModifier -72; Modify RadiationDamageModifier -72 |
| 227149 | Distortion of Will | MPDamageDebuffLineA (658) | 145 | MaterialMetamorphosis > 969; PsychologicalModification > 887; Profession == 12; Level > 144; Specialization op22 2; Expansion op22 2; Level > 114 | Modify ProjectileDamageModifier -51; Modify EnergyDamageModifier -51; Modify MeleeDamageModifier -51; Modify ColdDamageModifier -51; Modify FireDamageModifier -51; Modify PoisonDamageModifier -51; Modify ChemicalDamageModifier -51; Modify RadiationDamageModifier -51 |
| 227111 | Corruption of Will | MPDamageDebuffLineA (658) | 175 | MaterialMetamorphosis > 1086; PsychologicalModification > 1003; Profession == 12; Level > 174; Specialization op22 4; Expansion op22 2; Level > 134 | Modify ProjectileDamageModifier -75; Modify EnergyDamageModifier -75; Modify MeleeDamageModifier -75; Modify ColdDamageModifier -75; Modify FireDamageModifier -75; Modify PoisonDamageModifier -75; Modify ChemicalDamageModifier -75; Modify RadiationDamageModifier -75 |
| 227113 | Corruption of Resolve | MPDamageDebuffLineB (659) | 176 | MaterialMetamorphosis > 1089; PsychologicalModification > 1005; Profession == 12; Level > 175; Specialization op22 4; Expansion op22 2; Level > 134; Flags op91 227111 | Modify ProjectileDamageModifier -106; Modify EnergyDamageModifier -106; Modify MeleeDamageModifier -106; Modify ColdDamageModifier -106; Modify FireDamageModifier -106; Modify PoisonDamageModifier -106; Modify ChemicalDamageModifier -106; Modify RadiationDamageModifier -106 |
| 227117 | Degredation of Resolve | MPDamageDebuffLineB (659) | 195 | MaterialMetamorphosis > 1150; PsychologicalModification > 1054; Profession == 12; Level > 194; Specialization op22 4; Expansion op22 2; Level > 144; Flags op91 227115 | Modify ProjectileDamageModifier -141; Modify EnergyDamageModifier -141; Modify MeleeDamageModifier -141; Modify ColdDamageModifier -141; Modify FireDamageModifier -141; Modify PoisonDamageModifier -141; Modify ChemicalDamageModifier -141; Modify RadiationDamageModifier -141 |
| 227115 | Degredation of Will | MPDamageDebuffLineA (658) | 195 | MaterialMetamorphosis > 1150; PsychologicalModification > 1054; Profession == 12; Level > 194; Specialization op22 4; Expansion op22 2; Level > 144 | Modify ProjectileDamageModifier -97; Modify EnergyDamageModifier -97; Modify MeleeDamageModifier -97; Modify ColdDamageModifier -97; Modify FireDamageModifier -97; Modify PoisonDamageModifier -97; Modify ChemicalDamageModifier -97; Modify RadiationDamageModifier -97 |
| 227123 | Perversion of Resolve | MPDamageDebuffLineB (659) | 203 | MaterialMetamorphosis > 1285; PsychologicalModification > 1158; Profession == 12; Level > 202; Specialization op22 8; Expansion op22 2; Level > 149; Flags op91 227120 | Modify ProjectileDamageModifier -181; Modify EnergyDamageModifier -181; Modify MeleeDamageModifier -181; Modify ColdDamageModifier -181; Modify FireDamageModifier -181; Modify PoisonDamageModifier -181; Modify ChemicalDamageModifier -181; Modify RadiationDamageModifier -181 |
| 227120 | Perversion of Will | MPDamageDebuffLineA (658) | 203 | MaterialMetamorphosis > 1285; PsychologicalModification > 1158; Profession == 12; Level > 202; Specialization op22 8; Expansion op22 2; Level > 149 | Modify ProjectileDamageModifier -136; Modify EnergyDamageModifier -136; Modify MeleeDamageModifier -136; Modify ColdDamageModifier -136; Modify FireDamageModifier -136; Modify PoisonDamageModifier -136; Modify ChemicalDamageModifier -136; Modify RadiationDamageModifier -136 |
| 227132 | Debasement of Resolve | MPDamageDebuffLineB (659) | 211 | MaterialMetamorphosis > 1645; PsychologicalModification > 1436; Profession == 12; Level > 210; Specialization op22 8; Expansion op22 2; Level > 154; Flags op91 227128 | Modify ProjectileDamageModifier -247; Modify EnergyDamageModifier -247; Modify MeleeDamageModifier -247; Modify ColdDamageModifier -247; Modify FireDamageModifier -247; Modify PoisonDamageModifier -247; Modify ChemicalDamageModifier -247; Modify RadiationDamageModifier -247 |
| 227128 | Debasement of Will | MPDamageDebuffLineA (658) | 211 | MaterialMetamorphosis > 1645; PsychologicalModification > 1436; Profession == 12; Level > 210; Specialization op22 8; Expansion op22 2; Level > 154 | Modify ProjectileDamageModifier -160; Modify EnergyDamageModifier -160; Modify MeleeDamageModifier -160; Modify ColdDamageModifier -160; Modify FireDamageModifier -160; Modify PoisonDamageModifier -160; Modify ChemicalDamageModifier -160; Modify RadiationDamageModifier -160 |
| 227138 | Taint of Resolve | MPDamageDebuffLineB (659) | 214 | MaterialMetamorphosis > 1780; PsychologicalModification > 1540; Profession == 12; Level > 213; Specialization op22 8; Expansion op22 2; Level > 159; Flags op91 227134 | Modify ProjectileDamageModifier -329; Modify EnergyDamageModifier -329; Modify MeleeDamageModifier -329; Modify ColdDamageModifier -329; Modify FireDamageModifier -329; Modify PoisonDamageModifier -329; Modify ChemicalDamageModifier -329; Modify RadiationDamageModifier -329 |
| 227134 | Taint of Will | MPDamageDebuffLineA (658) | 214 | MaterialMetamorphosis > 1780; PsychologicalModification > 1540; Profession == 12; Level > 213; Specialization op22 8; Expansion op22 2; Level > 159 | Modify ProjectileDamageModifier -203; Modify EnergyDamageModifier -203; Modify MeleeDamageModifier -203; Modify ColdDamageModifier -203; Modify FireDamageModifier -203; Modify PoisonDamageModifier -203; Modify ChemicalDamageModifier -203; Modify RadiationDamageModifier -203 |
| 227143 | Defilement of Resolve | MPDamageDebuffLineB (659) | 218 | MaterialMetamorphosis > 1960; PsychologicalModification > 1680; Profession == 12; Level > 217; Specialization op22 8; Expansion op22 2; Level > 164; Flags op91 227140 | Modify ProjectileDamageModifier -411; Modify EnergyDamageModifier -411; Modify MeleeDamageModifier -411; Modify ColdDamageModifier -411; Modify FireDamageModifier -411; Modify PoisonDamageModifier -411; Modify ChemicalDamageModifier -411; Modify RadiationDamageModifier -411 |
| 227140 | Defilement of Will | MPDamageDebuffLineA (658) | 218 | MaterialMetamorphosis > 1960; PsychologicalModification > 1680; Profession == 12; Level > 217; Specialization op22 8; Expansion op22 2; Level > 164 | Modify ProjectileDamageModifier -250; Modify EnergyDamageModifier -250; Modify MeleeDamageModifier -250; Modify ColdDamageModifier -250; Modify FireDamageModifier -250; Modify PoisonDamageModifier -250; Modify ChemicalDamageModifier -250; Modify RadiationDamageModifier -250 |
| 227147 | Desecration of Resolve | MPDamageDebuffLineB (659) | 220 | MaterialMetamorphosis > 2050; PsychologicalModification > 1749; Profession == 12; Level > 219; Specialization op22 8; Expansion op22 2; Level > 169; Flags op91 227145 | Modify ProjectileDamageModifier -589; Modify EnergyDamageModifier -589; Modify MeleeDamageModifier -589; Modify ColdDamageModifier -589; Modify FireDamageModifier -589; Modify PoisonDamageModifier -589; Modify ChemicalDamageModifier -589; Modify RadiationDamageModifier -589 |
| 227145 | Desecration of Will | MPDamageDebuffLineA (658) | 220 | MaterialMetamorphosis > 2050; PsychologicalModification > 1749; Profession == 12; Level > 219; Specialization op22 8; Expansion op22 2; Level > 169 | Modify ProjectileDamageModifier -294; Modify EnergyDamageModifier -294; Modify MeleeDamageModifier -294; Modify ColdDamageModifier -294; Modify FireDamageModifier -294; Modify PoisonDamageModifier -294; Modify ChemicalDamageModifier -294; Modify RadiationDamageModifier -294 |
| 267269 | Beneficial Scourge | GeneralColdACDebuff (128) | - | MaterialMetamorphosis > 1769; PsychologicalModification > 1539; Profession == 12; NPCFamily == 0 | Modify HealMultiplier -50 |
| 29286 | Dominate: BioMet | BioMetDebuff (164) | - | PsychologicalModification > 661; SensoryImprovement > 661; VisualProfession == 12 | Modify BiologicalMetamorphosis -125 |
| 29287 | Dominate: MatCrea | MatCreaDebuff (160) | - | PsychologicalModification > 665; SensoryImprovement > 665; VisualProfession == 12 | Modify MaterialCreation -125 |
| 29289 | Dominate: MatMet | MatMetDebuff (158) | - | PsychologicalModification > 643; SensoryImprovement > 643; VisualProfession == 12 | Modify MaterialMetamorphosis -125 |
| 29290 | Dominate: PsyMod | PsyModDebuff (168) | - | PsychologicalModification > 657; SensoryImprovement > 657; VisualProfession == 12 | Modify PsychologicalModification -125 |
| 29291 | Dominate: SenseImp | SenseImpDebuff (166) | - | PsychologicalModification > 653; SensoryImprovement > 653; VisualProfession == 12 | Modify SensoryImprovement -125 |
| 29288 | Dominate: SpaceTime | MatLocDebuff (162) | - | PsychologicalModification > 649; SensoryImprovement > 649; VisualProfession == 12 | Modify SpaceTime -125 |
| 99121 | Douse Anger | MetaPhysicistDamageDebuff (187) | - | MaterialMetamorphosis > 78; PsychologicalModification > 65; VisualProfession == 12 | Skill[278,-6]; Skill[279,-6]; Skill[280,-6]; Skill[281,-6]; Skill[282,-6]; Skill[311,-6]; Skill[316,-6]; Skill[317,-6]; Modify MeleeInit -32; Modify RangedInit -32; Modify PhysicalInit -32; TauntNpc[122] |
| 267271 | Hostility Scourge | GeneralChemicalACDebuff (127) | - | MaterialMetamorphosis > 1769; PsychologicalModification > 1539; Profession == 12; NPCFamily == 0 | Modify NanoDamageMultiplier -50 |
| 267275 | Lesser Beneficial Scourge | GeneralColdACDebuff (128) | - | MaterialMetamorphosis > 1569; PsychologicalModification > 1339; Profession == 12; NPCFamily == 0 | Modify HealMultiplier -25 |
| 267272 | Lesser Hostility Scourge | GeneralChemicalACDebuff (127) | - | MaterialMetamorphosis > 1569; PsychologicalModification > 1339; Profession == 12; NPCFamily == 0 | Modify NanoDamageMultiplier -25 |
| 99123 | Lull Wrath | MetaPhysicistDamageDebuff (187) | - | MaterialMetamorphosis > 774; PsychologicalModification > 702; VisualProfession == 12 | Skill[278,-33]; Skill[279,-33]; Skill[280,-33]; Skill[281,-33]; Skill[282,-33]; Skill[311,-33]; Skill[316,-33]; Skill[317,-33]; Modify MeleeInit -244; Modify RangedInit -244; Modify PhysicalInit -244; TauntNpc[869] |
| 267283 | Nanite Enhanced Nano Shutdown | NanoShutdownDebuff (239) | - | PsychologicalModification > 1599; SpaceTime > 1399; Profession == 12; BiologicalMetamorphosis > 1399 | Modify SensoryImprovement -3000; Modify SpaceTime -3000; Modify MaterialCreation -3000; Modify PsychologicalModification -3000; Modify BiologicalMetamorphosis -3000; Modify MaterialMetamorphosis -3000 |
| 266299 | Nano Division | NanoResistanceDebuff_LineA (643) | - | MaterialCreation > 1199; SpaceTime > 1199; VisualProfession == 12; Expansion op22 32 | ScalingModify NanoResist -150 |
| 29306 | Nano Shutdown | NanoShutdownDebuff (239) | - | PsychologicalModification > 688; SpaceTime > 759; VisualProfession == 12; BiologicalMetamorphosis > 759 | Modify SensoryImprovement -1000; Modify SpaceTime -1000; Modify MaterialCreation -1000; Modify PsychologicalModification -1000; Modify BiologicalMetamorphosis -1000; Modify MaterialMetamorphosis -1000 |
| 99122 | Quell Anger | MetaPhysicistDamageDebuff (187) | - | MaterialMetamorphosis > 157; PsychologicalModification > 133; VisualProfession == 12 | Skill[278,-9]; Skill[279,-9]; Skill[280,-9]; Skill[281,-9]; Skill[282,-9]; Skill[311,-9]; Skill[316,-9]; Skill[317,-9]; Modify MeleeInit -61; Modify RangedInit -61; Modify PhysicalInit -61; TauntNpc[209] |
| 99120 | Quench Anger | MetaPhysicistDamageDebuff (187) | - | MaterialMetamorphosis > 206; PsychologicalModification > 183; VisualProfession == 12 | Skill[278,-12]; Skill[279,-12]; Skill[280,-12]; Skill[281,-12]; Skill[282,-12]; Skill[311,-12]; Skill[316,-12]; Skill[317,-12]; Modify MeleeInit -81; Modify RangedInit -81; Modify PhysicalInit -81; TauntNpc[277] |
| 99117 | Rage Abolishment | MetaPhysicistDamageDebuff (187) | - | MaterialMetamorphosis > 443; PsychologicalModification > 402; VisualProfession == 12 | Skill[278,-20]; Skill[279,-20]; Skill[280,-20]; Skill[281,-20]; Skill[282,-20]; Skill[311,-20]; Skill[316,-20]; Skill[317,-20]; Modify MeleeInit -156; Modify RangedInit -156; Modify PhysicalInit -156; TauntNpc[496] |
| 99115 | Rage Eradication | MetaPhysicistDamageDebuff (187) | - | MaterialMetamorphosis > 549; PsychologicalModification > 497; VisualProfession == 12 | Skill[278,-23]; Skill[279,-23]; Skill[280,-23]; Skill[281,-23]; Skill[282,-23]; Skill[311,-23]; Skill[316,-23]; Skill[317,-23]; Modify MeleeInit -181; Modify RangedInit -181; Modify PhysicalInit -181; TauntNpc[574] |
| 99118 | Rage Suppression | MetaPhysicistDamageDebuff (187) | - | MaterialMetamorphosis > 377; PsychologicalModification > 326; VisualProfession == 12 | Skill[278,-18]; Skill[279,-18]; Skill[280,-18]; Skill[281,-18]; Skill[282,-18]; Skill[311,-18]; Skill[316,-18]; Skill[317,-18]; Modify MeleeInit -134; Modify RangedInit -134; Modify PhysicalInit -134; TauntNpc[431] |
| 99112 | Shed Anger | MetaPhysicistDamageDebuff (187) | - | MaterialMetamorphosis > 40; PsychologicalModification > 34; VisualProfession == 12 | Skill[278,-3]; Skill[279,-3]; Skill[280,-3]; Skill[281,-3]; Skill[282,-3]; Skill[311,-3]; Skill[316,-3]; Skill[317,-3]; Modify MeleeInit -19; Modify RangedInit -19; Modify PhysicalInit -19; TauntNpc[60] |
| 99119 | Stifle Rage | MetaPhysicistDamageDebuff (187) | - | MaterialMetamorphosis > 264; PsychologicalModification > 235; VisualProfession == 12 | Skill[278,-15]; Skill[279,-15]; Skill[280,-15]; Skill[281,-15]; Skill[282,-15]; Skill[311,-15]; Skill[316,-15]; Skill[317,-15]; Modify MeleeInit -101; Modify RangedInit -101; Modify PhysicalInit -101; TauntNpc[344] |
| 99116 | Temper Wrath | MetaPhysicistDamageDebuff (187) | - | MaterialMetamorphosis > 644; PsychologicalModification > 578; VisualProfession == 12 | Skill[278,-26]; Skill[279,-26]; Skill[280,-26]; Skill[281,-26]; Skill[282,-26]; Skill[311,-26]; Skill[316,-26]; Skill[317,-26]; Modify MeleeInit -199; Modify RangedInit -199; Modify PhysicalInit -199; TauntNpc[657] |
| 29321 | Unmake: BioMet | BioMetDebuff (164) | - | PsychologicalModification > 339; SensoryImprovement > 339; VisualProfession == 12 | Modify BiologicalMetamorphosis -75 |
| 29322 | Unmake: MatCrea | MatCreaDebuff (160) | - | PsychologicalModification > 349; SensoryImprovement > 349; VisualProfession == 12 | Modify MaterialCreation -75 |
| 29324 | Unmake: MatMet | MatMetDebuff (158) | - | PsychologicalModification > 293; SensoryImprovement > 293; VisualProfession == 12 | Modify MaterialMetamorphosis -75 |
| 29325 | Unmake: PsyMod | PsyModDebuff (168) | - | PsychologicalModification > 330; SensoryImprovement > 330; VisualProfession == 12 | Modify PsychologicalModification -75 |
| 29326 | Unmake: SenseImp | SenseImpDebuff (166) | - | PsychologicalModification > 320; SensoryImprovement > 320; VisualProfession == 12 | Modify SensoryImprovement -75 |
| 29323 | Unmake: SpaceTime | MatLocDebuff (162) | - | PsychologicalModification > 309; SensoryImprovement > 309; VisualProfession == 12 | Modify SpaceTime -75 |
| 99113 | Wrath Abatement | MetaPhysicistDamageDebuff (187) | - | MaterialMetamorphosis > 852; PsychologicalModification > 769; VisualProfession == 12 | Skill[278,-37]; Skill[279,-37]; Skill[280,-37]; Skill[281,-37]; Skill[282,-37]; Skill[311,-37]; Skill[316,-37]; Skill[317,-37]; Modify MeleeInit -261; Modify RangedInit -261; Modify PhysicalInit -261; TauntNpc[982] |
| 99114 | Wrath Ebb | MetaPhysicistDamageDebuff (187) | - | MaterialMetamorphosis > 714; PsychologicalModification > 643; VisualProfession == 12 | Skill[278,-30]; Skill[279,-30]; Skill[280,-30]; Skill[281,-30]; Skill[282,-30]; Skill[311,-30]; Skill[316,-30]; Skill[317,-30]; Modify MeleeInit -222; Modify RangedInit -222; Modify PhysicalInit -222; TauntNpc[759] |

## Debuffs - Mind/Nano (3)

| id | name | nano line (strain) | minLvl | cast reqs | effect |
| ---: | --- | --- | ---: | --- | --- |
| 270354 | Metaing's Improved Mind Quake | MetaphysicistMindDamageNanoDebuffs (221) | - | MaterialCreation > 1080; PsychologicalModification > 981; Profession == 12 | Modify BiologicalMetamorphosis -150; Modify SensoryImprovement -150; Modify PsychologicalModification -150; Modify MaterialMetamorphosis -150; Modify MaterialCreation -150; Modify SpaceTime -150 |
| 144617 | Mind Banshee | MetaphysicistMindDamageNanoDebuffs (221) | - | MaterialCreation > 693; PsychologicalModification > 621; VisualProfession == 12 | Modify BiologicalMetamorphosis -65; Modify SensoryImprovement -65; Modify PsychologicalModification -65; Modify MaterialMetamorphosis -65; Modify MaterialCreation -65; Modify SpaceTime -65 |
| 144620 | Mind Quake | MetaphysicistMindDamageNanoDebuffs (221) | - | MaterialCreation > 817; PsychologicalModification > 742; VisualProfession == 12 | Modify BiologicalMetamorphosis -100; Modify SensoryImprovement -100; Modify PsychologicalModification -100; Modify MaterialMetamorphosis -100; Modify MaterialCreation -100; Modify SpaceTime -100 |

## Mezz / Calm (1)

| id | name | nano line (strain) | minLvl | cast reqs | effect |
| ---: | --- | --- | ---: | --- | --- |
| 29285 | Curse of Chronos | Mezz (147) | - | SensoryImprovement > 722; SpaceTime > 794; VisualProfession == 12 | TauntNpc[5663] |

## Heals / HoT (1)

| id | name | nano line (strain) | minLvl | cast reqs | effect |
| ---: | --- | --- | ---: | --- | --- |
| 260771 | Construct Empowerment | HealingConstructEmpowerment (807) | 207 | Expansion op22 2; BiologicalMetamorphosis > 1465; SpaceTime > 1462; Profession == 12; Level > 206; NPCFamily == 96 | CastNano[260770]; Modify HealMultiplier +12 |

## Travel / Recall (2)

| id | name | nano line (strain) | minLvl | cast reqs | effect |
| ---: | --- | --- | ---: | --- | --- |
| 233849 | Awaken Shadowland Soul Memory | ShadowlandBindandRecall (738) | 150 | Profession == 12; Level > 149; MaterialCreation > 969; SpaceTime > 969; Expansion op22 2; ExpansionPlayfield == 1; PlayfieldType op107 1 | RecallToAnchor[]; Set[214,1] |
| 233847 | Shadowland Soul Fetter | ShadowlandBindandRecall (738) | 150 | Profession == 12; Level > 149; MaterialCreation > 969; SpaceTime > 969; Expansion op22 2; ExpansionPlayfield == 1; PlayfieldType op107 1 | SetAnchor[]; Set[214,1] |

## Misc / Utility (8)

| id | name | nano line (strain) | minLvl | cast reqs | effect |
| ---: | --- | --- | ---: | --- | --- |
| 234997 | Channel Notum Vein: Adonis | NOSTACKING (0) | 75 | Flags op33 226989; Flags op3 0; GOS > 4999; Profession == 12; PsychologicalModification > 539; SensoryImprovement > 539; MaterialMetamorphosis > 583; ExpansionPlayfield == 1; Level > 74; Expansion op22 2 | TeamCastNano[234070] |
| 234993 | Channel Notum Vein: Elysium | NOSTACKING (0) | 75 | Flags op33 226987; Flags op3 0; GOS > -1; Profession == 12; PsychologicalModification > 526; SensoryImprovement > 526; MaterialMetamorphosis > 570; ExpansionPlayfield == 1; Level > 74; Expansion op22 2 | TeamCastNano[234068] |
| 234995 | Channel Notum Vein: Scheol | NOSTACKING (0) | 75 | Flags op33 226991; Flags op3 0; GOS > 999; Profession == 12; PsychologicalModification > 532; SensoryImprovement > 532; MaterialMetamorphosis > 576; ExpansionPlayfield == 1; Level > 74; Expansion op22 2 | TeamCastNano[234069] |
| 203605 | Fluctuate Manifestation | NOSTACKING (0) | 75 | Profession == 12; MaterialCreation > 535; SpaceTime > 535; Level > 74; Breed == 7 | ReduceNanoStrainDuration[145,417]; ReduceNanoStrainDuration[146,417] |
| 203607 | Modulate Manifestation | NOSTACKING (0) | 100 | Profession == 12; MaterialCreation > 769; SpaceTime > 769; Level > 99; Breed == 7 | ReduceNanoStrainDuration[145,547]; ReduceNanoStrainDuration[146,547] |
| 301888 | Induced Apathy | SiphonBox683 (683) | 211 | MaterialCreation > 1300; SpaceTime > 1300; Profession == 12; Level > 210; Flags op118 0; NPCFamily != 98; NPCFamily != 96 | AddOffProc[10,301887] |
| 267601 | Evocation of The Reinforced | NOSTACKING (0) | 219 | MaterialCreation > 1900; BiologicalMetamorphosis > 1900; SpaceTime > 1900; Profession == 12; Level > 218 | (1 non-effect functions only) |
| 270800 | Improved Instill With Malign Intent | NOSTACKING (0) | - | MaterialCreation > 2208; BiologicalMetamorphosis > 2108; Profession == 12; NanoFocusLevel op22 64 | (1 non-effect functions only) |

