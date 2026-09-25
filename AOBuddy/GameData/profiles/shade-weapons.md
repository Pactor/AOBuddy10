# Shade - Weapons

Shade weapons + allowed special attacks (for AOBuddy10 bot decision-making)

## Usable weapon skills

The client data does not encode 'profession X uses weapon skill Y', so the list is operator-supplied - but each entry is justified by three measured signals recorded per type: (1) skillCostFactor/skillCostRank from reference/skillcaps.json, (2) weaponsProfessionLocked, the count of templates whose ToWield locks them to this profession, and (3) professionNanoBuffLines, the profession's own nano lines that buff that skill, read from shade-nanos.json.

| Skill | IP cost | Cost rank | Templates usable / total | Shade-locked | Player-wieldable models | Nano buff lines |
|---|---|---|---|---|---|---|
| Piercing | 1 | 1 | 759 / 820 | 89 | 295 | ShadePiercingBuff |
| MartialArts | 1.6 | 5 | 223 / 256 | 2 | 87 | MartialArtsBuff |
| MeleeEnergy | 4 | 14 | 256 / 280 | 1 | 115 | None |

### Every other weapon skill in the game costs a Shade 4.0 IP

| Skill | IP cost | Usable in data / total |
|---|---|---|
| AssaultRifle | 4 | 1467 / 1547 |
| Pistol | 4 | 1159 / 1398 |
| 1hEdged | 4 | 887 / 960 |
| Rifle | 4 | 852 / 905 |
| 1hBlunt | 4 | 820 / 854 |
| 2hEdged | 4 | 605 / 764 |
| Shotgun | 4 | 537 / 603 |
| MGSMG | 4 | 499 / 562 |
| 2hBlunt | 4 | 446 / 503 |
| RangedEnergy | 4 | 439 / 451 |
| Bow | 4 | 352 / 377 |
| Grenade | 4 | 319 / 359 |
| HeavyWeapons | 4 | 84 / 85 |
| MultiMelee | 1 | 0 / 6 |

## Shade-locked weapons (92 templates)

| Weapon skill | Templates |
|---|---|
| Piercing | 89 |
| MartialArts | 2 |
| MeleeEnergy | 1 |

Distinct names: Bloodlust, Bronto Vet Lancet, Die Kleine Nadel, Die Nadel, Heavy Bronto Vet Lancet, Huzzum's Iron Fist, Light Bronto Vet Lancet, Nippy John Stiletto, Sigd I, Sigd II, Sigd III, Sigd IV, Sigd IX, Sigd V, Sigd VI, Sigd VII, Sigd VIII, Sigd X, Special Edition Kyr'Ozch Energy Rapier, Special Edition Kyr'Ozch Nunchacko, Special Edition Kyr'Ozch Rapier, Spike of Menace, Susurrating Spike of Menace, Thousand Stings of Boredom, Thousand Stings of Fatigue, Thousand Stings of Fear, Thousand Stings of Pain, Thousand Stings of Panic, Thousand Stings of Sorrow, Trick Poker, Twinkling Trick Poker

## Piercing - representative weapons

| id | Weapon | QL | Wield | Specials | Lock | Damage | dpsProxy |
|---|---|---|---|---|---|---|---|
| 245607 | Nippy John Stiletto | 160 | Piercing 800, Parry 320, Riposte 320 | SneakAttack 400, FastAttack 400, Dimach 160 | Profession EqualTo Shade | 175-375 @ 1s/1s | 137.5 |
| 214165 | Twinkling Trick Poker | 300 | Piercing 1700 | SneakAttack 850, FastAttack 850, Dimach 340 | Profession EqualTo Shade | 220-475 @ 2s/2s | 86.9 |
| 214160 | Susurrating Spike of Menace | 300 | Piercing 1700 | SneakAttack 850, FastAttack 850, Dimach 340 | Profession EqualTo Shade | 220-475 @ 2s/2s | 86.9 |
| 214164 | Trick Poker | 299 | Piercing 1694 | SneakAttack 847, FastAttack 847, Dimach 339 | Profession EqualTo Shade | 219-474 @ 2s/2s | 86.6 |
| 214159 | Spike of Menace | 299 | Piercing 1694 | SneakAttack 847, FastAttack 847, Dimach 339 | Profession EqualTo Shade | 219-474 @ 2s/2s | 86.6 |
| 288288 | Special Edition Kyr'Ozch Rapier | 150 | Piercing 699 | SneakAttack 349, FastAttack 349, Dimach 149 | Profession EqualTo Shade | 200-234 @ 1.3s/1.3s | 83.5 |
| 226486 | Die Nadel | 300 | Piercing 1800 | SneakAttack 900, FastAttack 900 | Profession EqualTo Shade | 100-550 @ 2.2s/2.2s | 73.9 |
| 247069 | Bloodlust | 300 | Piercing 2000, Parry 800, Riposte 800 | SneakAttack 1000, FastAttack 1000, Dimach 400 | Profession EqualTo Shade | 100-550 @ 2.2s/2.2s | 73.9 |
| 231103 | Sigd X | 300 | Piercing 1800, Parry 720, Riposte 720 | SneakAttack 900, Dimach 360 | Profession EqualTo Shade | 125-300 @ 1.9s/1.9s | 55.9 |
| 231102 | Sigd IX | 299 | Piercing 1794, Parry 718, Riposte 718 | SneakAttack 897, Dimach 359 | Profession EqualTo Shade | 125-299 @ 1.9s/1.9s | 55.8 |
| 231100 | Sigd VIII | 249 | Piercing 1494, Parry 598, Riposte 598 | SneakAttack 747, Dimach 299 | Profession EqualTo Shade | 105-251 @ 1.75s/1.75s | 50.9 |
| 231098 | Sigd VII | 199 | Piercing 1194, Parry 477, Riposte 477 | SneakAttack 597, Dimach 239 | Profession EqualTo Shade | 86-204 @ 1.6s/1.6s | 45.3 |
| 231096 | Sigd VI | 149 | Piercing 894, Parry 357, Riposte 357 | SneakAttack 447, Dimach 179 | Profession EqualTo Shade | 67-156 @ 1.45s/1.45s | 38.4 |
| 231094 | Sigd V | 99 | Piercing 594, Parry 237, Riposte 237 | SneakAttack 297, Dimach 119 | Profession EqualTo Shade | 48-108 @ 1.29s/1.29s | 30.2 |
| 231092 | Sigd IV | 59 | Piercing 354, Parry 141, Riposte 141 | SneakAttack 177, Dimach 71 | Profession EqualTo Shade | 32-70 @ 1.17s/1.17s | 21.8 |
| 231090 | Sigd III | 39 | Piercing 234, Parry 93 | SneakAttack 117, Dimach 47 | Profession EqualTo Shade | 25-51 @ 1.11s/1.11s | 17.1 |
| 231088 | Sigd II | 29 | Piercing 174, Parry 69 | Dimach 35 | Profession EqualTo Shade | 21-42 @ 1.08s/1.08s | 14.6 |
| 231086 | Sigd I | 19 | Piercing 114 | Dimach 23 | Profession EqualTo Shade | 17-32 @ 1.05s/1.05s | 11.7 |
| 280719 | Deceit of the Xan | 300 | Piercing 2250 | SneakAttack 1125, FastAttack 1125, Dimach 450 | - | 300-350 @ 1s/1s | 162.5 |
| 244778 | Lady of Deceit | 299 | Piercing 2250, Parry 900, Riposte 900 | SneakAttack 1125, Dimach 450 | - | 300-400 @ 3s/3s | 58.3 |
| 265019 | Ofab Viper Mk 6 | 300 | Piercing 2200 | SneakAttack 1100, FastAttack 1100, Dimach 440 | - | 308-374 @ 1s/1s | 170.5 |
| 254716 | Kyr'Ozch Energy Rapier | 300 | Piercing 2000 | - | - | 280-340 @ 1s/1s | 155 |
| 305525 | Uklesh's Talon | 300 | Piercing 2149 | SneakAttack 1099, FastAttack 1099, Dimach 449 | - | 290-375 @ 1s/1s | 166.2 |
| 305026 | Slayerdroid Notum-Imbued Claw | 200 | Piercing 1999 | SneakAttack 999, FastAttack 1199, Dimach 499 | - | 321-368 @ 1s/1.1s | 164 |
| 265005 | Ofab Viper Mk 4 | 300 | Piercing 2200 | FastAttack 1100, Dimach 440 | - | 294-357 @ 1s/1s | 162.8 |
| 265012 | Ofab Viper Mk 5 | 300 | Piercing 2200 | SneakAttack 1100, FastAttack 1100, Dimach 440 | - | 294-357 @ 1s/1s | 162.8 |

## MartialArts - representative weapons

| id | Weapon | QL | Wield | Specials | Lock | Damage | dpsProxy |
|---|---|---|---|---|---|---|---|
| 302946 | Corrupted Lord of Wisdom | 300 | MartialArts 1999 | FastAttack 999, Brawl 1099, Dimach 450 | - | 200-550 @ 1.8s/1.8s | 104.2 |
| 294001 | Lady of Wisdom | 300 | MartialArts 2250 | FastAttack 1125, Brawl 1350, Dimach 450 | - | 250-400 @ 2.5s/2.5s | 65 |
| 267258 | Dreadloch Shen Sticks | 300 | MartialArts 2100 | Brawl 1300, Dimach 650 | - | 310-490 @ 1.5s/1.5s | 133.3 |
| 303406 | Junior Ofab Boar | 220 | MartialArts 1499 | Brawl 999, Dimach 799 | - | 235-290 @ 1.5s/1.5s | 87.5 |
| 288664 | Kyr'Ozch Nunchacko | 300 | MartialArts 2000 | - | - | 290-340 @ 1.5s/1.5s | 105 |
| 301885 | X1-R4 Viral Force-Blades | 300 | MartialArts 2199 | Brawl 1229, Dimach 659, SneakAttack 999 | - | 307-472 @ 1.4s/1.4s | 139.1 |
| 293996 | Kuma Tonfa - Right Hand | 300 | MartialArts 2250 | FastAttack 1125, Brawl 1350, Dimach 450 | - | 325-495 @ 1.5s/1.5s | 136.7 |
| 293993 | Kuma Tonfa - Left Hand | 300 | MartialArts 2250 | FastAttack 1125, Brawl 1350, Dimach 450 | - | 325-495 @ 1.5s/1.5s | 136.7 |

## MeleeEnergy - representative weapons

| id | Weapon | QL | Wield | Specials | Lock | Damage | dpsProxy |
|---|---|---|---|---|---|---|---|
| 280730 | Dusk of the Xan | 300 | MeleeEnergy 1799 | SneakAttack 899, FastAttack 899, Dimach 450 | - | 300-350 @ 1s/1s | 162.5 |
| 246244 | Sword of Dawn | 200 | MeleeEnergy 999 | - | - | 250-750 @ 2s/2s | 125 |
| 254702 | Kyr'Ozch Energy Hammer | 300 | MeleeEnergy 2000 | - | - | 325-400 @ 1s/1s | 181.2 |
| 246243 | Broken Sword of Dawn | 199 | MeleeEnergy 500 | - | - | 126-376 @ 1s/1s | 125.5 |
| 254709 | Kyr'Ozch Energy Hammer - Type 112 | 300 | MeleeEnergy 2000 | FastAttack 1000, Brawl 1200, Dimach 400 | - | 325-400 @ 1s/1s | 181.2 |
| 280729 | Dawn of the Xan | 300 | MeleeEnergy 1799 | SneakAttack 899, FastAttack 899, Dimach 450 | - | 300-350 @ 1s/1s | 162.5 |
| 246255 | Broken Sword of Dusk | 199 | MeleeEnergy 500 | - | - | 126-376 @ 1s/1s | 125.5 |
| 246256 | Sword of Dusk | 200 | MeleeEnergy 999 | - | - | 250-750 @ 2s/2s | 125 |

## Caveats

- The usable weapon-skill list is an operator choice (see howWeaponSkillsWereChosen). The three measured signals recorded per type are the evidence; the choice itself is a judgement.
- Requirement values scale with QL; a named weapon spans a QL range. Values shown are for the exact template QL listed.
- dpsProxy = avgDamage / (attack + recharge) from the item's own stats. It is a comparison number only - it ignores initiative caps, crit, add-damage, buffs and specials.
- Representative weapons are the highest-dpsProxy model of each weapon LINE present in the skill plus the next highest-dpsProxy models; that is a stated selection rule, not a community tier list.
- GM/test/monster templates are filtered out with the same measured bounds as shade-weapons-endgame.json (max damage >= 1000, primary requirement > 2600, or QL >= 200 with a requirement under 100). See that file's definition.exclusions.
