# Engineer (profession 3) - Pets

Research for AOBuddy10 pet AI. **Every id, name, bot level, skill requirement and credit cost here comes
from `engineer-nanos.json`** (client 18.8.50_EP1 extraction of `nanos.ocp`); all 137 ids used from the four
pet categories were re-checked name-for-name against `itemnames.sql` in this pass - **137 of 137 exact
match, 0 mismatches**. Mechanics and acquisition come from the cited AO community sources. Nothing is
invented: anything not traceable to the data or a source is listed under **Unverified**.

> **Coverage: 91 of 91 pet-summon nanos (90 robots + 1 tower), 10 of 10 pet heals, 36 of 36 pet
> buffs/utility, plus 3 pet-targeted nanos the extractor filed under other categories. No sampling.**

---

## How the Engineer differs from the Meta-Physicist (read this first)

| | Meta-Physicist | **Engineer** |
|---|---|---|
| Pet roles | 3 slots: Attack + Heal + Mezz | **1 role: attack ROBOT.** No heal pet, no mezz pet |
| Pets at once | 3 | **up to 2** (a second pet from level 100, AODB) |
| Summon function | `SummonPet` (53167) | **`SpawnItem` (53064)**, args `[<4-char code>, <bot level>, 0]` |
| Summon skills | TS + MC / BioMet / MatMet (per role) | **MC (130) + TS (131), always equal**, for every bot |
| Profession gate | mixed `Profession(60)` / `VisualProfession(368)` | **all 90 bots gate on `Profession(60) == 3`**; only the pet-heal line uses 368 |
| Cost to summon | none | **credits** - `Hit[61,-N,-N,0]` (stat 61 = Cash) on 79 of 90 bots |
| Pet family id | NpcClan 96 / 97 / 98 | **NpcClan 95 = `EngineerAttackPet`** |
| Healing the pet | a heal PET does it | **the Engineer casts 7 repair nanos on the bot himself** (BioMet skill) |
| Over-equip | pet stays obedient after the wrangle drops | **pet STOPS OBEYING below 80% of the summon req** and drops to behind |
| Upgrades | nanos only | **tradeskill TRIMMERS and Android NCU Upgrades** (items, not nanos) |

---

## Summary

The Engineer (profession 3) is a pet class built around ONE thing: a combat ROBOT. Unlike the Meta-Physicist's three simultaneous manifestations (attack / heal / mezz), every Engineer pet in the local client data is an attack robot - 90 robot summons plus one deployable (Jamming Tower). There is no Engineer heal pet and no Engineer mezz pet; the Engineer repairs the bot himself with a seven-step pet-heal nano line (Quick Fix 116793 ... A Maker's Touch 116791, all Hit[27] on stat 27 = Health, cast on the bot via Breed == 7). Bots are summoned with SpawnItem (FunctionType 53064), args [<4-char template code>, <bot level>, 0], rather than the MP's SummonPet (53167); OmniCell's server source documents the two functions as identical in call shape and effect. Every summon needs Matter Creation (130) + Time & Space (131) at the same value plus Profession(60) == 3 - no Engineer bot uses the VisualProfession(368) path - and 79 of the 90 also DEDUCT CREDITS: Hit[61,-N,-N,0] on stat 61 = Cash, matching a 'Cash > N-1' cast requirement, from 132 credits for a Feeble Android up to 3,694 for the Ravening M-60. The 11 Automaton summons are free. The ladder runs Automaton -> Android -> Gladiatorbot -> Guardbot -> Warbot -> Warmachine -> Wardroid -> Slayerdroid (bot level 200, the froob ceiling), then with Shadowlands: Slayerdroid Annihilator, the Predator M-30 line, the Devastator Drone line, Marauder M-45, Desolator Assault Drone and the Widowmaker Battle Drone (bot 220), with the nano-focus-gated Ravening M-60 as the final unlock. The Engineer's defining constraint is over-equipping: an OE pet does not merely hit softer, it STOPS OBEYING and drops to behind, so MC and TS must stay at or above 80% of the summon requirement for as long as you want the bot. Coverage: all 90 robot summons plus the tower are enumerated below - 91 of 91, no sampling - and every id/name is verified against itemnames.sql.

---

## The complete bot ladder - 90 of 90 robot summons + the tower

`Bot lvl` is the SpawnItem template-level argument (the BOT's level, not a character-level requirement).
`MC/TS` is the Matter Creation / Time & Space needed to cast. `Credits` is the `Hit[61]` drain.
`Char lvl` is the explicit `Level` requirement where one exists (blank = none; the real gate is MC/TS).

### Attack robot - Automaton family  <sub>`SpawnItem ENAU/EGAU`</sub> - 11 nanos

The Engineer's first robot and the whole bot-level 1-15 band. Eleven nanos: six ENAU (Feeble / Patchwork / Lesser / Inferior / Flawed / Common Automaton, bot 1-6) then five EGAU (Automaton / Upgraded / Advanced / Perfected / Semi-Sentient Automaton, bot 7-15). Uniquely in the whole ladder, NONE of the eleven Automaton summons carries a Hit[61] credit drain or a Cash cast requirement - Automatons are free to build.

*Feeble Automaton (43325) needs only MC/TS 5, so it is castable from character creation. Recast the next one up the moment MC/TS allows.*

| Nano | id | Bot lvl | MC/TS | Credits | Char lvl | Gate | Nano source |
|---|---|---|---|---|---|---|---|
| Feeble Automaton | 43325 | 1 | 5 | *free* |  |  |  |
| Patchwork Automaton | 45692 | 2 | 12 | *free* |  |  |  |
| Lesser Automaton | 45710 | 3 | 19 | *free* |  |  |  |
| Inferior Automaton | 45704 | 4 | 26 | *free* |  |  |  |
| Flawed Automaton | 45718 | 5 | 33 | *free* |  |  |  |
| Common Automaton | 45724 | 6 | 40 | *free* |  |  |  |
| Automaton | 45737 | 7 | 47 | *free* |  |  |  |
| Upgraded Automaton | 45676 | 9 | 58 | *free* |  |  |  |
| Advanced Automaton | 45731 | 11 | 69 | *free* |  |  |  |
| Perfected Automaton | 45698 | 13 | 79 | *free* |  |  |  |
| Semi-Sentient Automaton | 45681 | 15 | 89 | *free* |  |  |  |

### Attack robot - Android family  <sub>`SpawnItem ENAN/EGAN`</sub> - 11 nanos

Bot levels 17-43. Six ENAN (Feeble -> Common Android) then five EGAN (Android -> Semi-Sentient Android). This is the first family that charges credits: 132 up to 340.

*The credit drain starts here - every cast deducts the Hit[61] amount from Cash, so a low-level Engineer who keeps re-summoning is spending real money.*

| Nano | id | Bot lvl | MC/TS | Credits | Char lvl | Gate | Nano source |
|---|---|---|---|---|---|---|---|
| Feeble Android | 45712 | 17 | 99 | 132 |  |  |  |
| Patchwork Android | 45691 | 19 | 109 | 148 |  |  |  |
| Lesser Android | 45709 | 21 | 119 | 164 |  |  |  |
| Inferior Android | 45703 | 23 | 129 | 180 |  |  |  |
| Flawed Android | 45717 | 25 | 139 | 196 |  |  |  |
| Common Android | 45723 | 28 | 154 | 220 |  |  |  |
| Android | 45736 | 31 | 167 | 244 |  |  |  |
| Upgraded Android | 45675 | 34 | 179 | 268 |  |  |  |
| Advanced Android | 45730 | 37 | 191 | 292 |  |  |  |
| Perfected Android | 45697 | 40 | 203 | 316 |  |  |  |
| Semi-Sentient Android | 45680 | 43 | 215 | 340 |  |  |  |

### Attack robot - Gladiatorbot family  <sub>`SpawnItem ENGA`</sub> - 11 nanos

Bot levels 46-76, eleven nanos (Feeble / Patchwork / Lesser / Inferior / Flawed / Common / base / Upgraded / Advanced / Perfected / Semi-Sentient Gladiatorbot). MC/TS 230 -> 404; 364 -> 604 credits.

*From Gladiatorbot on, every RK family reuses the same eleven- or twelve-step prefix ladder, so a bot AI can pick purely by skill requirement.*

| Nano | id | Bot lvl | MC/TS | Credits | Char lvl | Gate | Nano source |
|---|---|---|---|---|---|---|---|
| Feeble Gladiatorbot | 45713 | 46 | 230 | 364 |  |  |  |
| Patchwork Gladiatorbot | 45693 | 49 | 246 | 388 |  |  |  |
| Lesser Gladiatorbot | 45711 | 52 | 262 | 412 |  |  |  |
| Inferior Gladiatorbot | 45705 | 55 | 278 | 436 |  |  |  |
| Flawed Gladiatorbot | 45719 | 58 | 296 | 460 |  |  |  |
| Common Gladiatorbot | 45725 | 61 | 314 | 484 |  |  |  |
| Gladiatorbot | 45701 | 64 | 332 | 508 |  |  |  |
| Upgraded Gladiatorbot | 45664 | 67 | 350 | 532 |  |  |  |
| Advanced Gladiatorbot | 45732 | 70 | 368 | 556 |  |  |  |
| Perfected Gladiatorbot | 45699 | 73 | 386 | 580 |  |  |  |
| Semi-Sentient Gladiatorbot | 45682 | 76 | 404 | 604 |  |  |  |

### Attack robot - Guardbot family  <sub>`SpawnItem ENGU`</sub> - 11 nanos

Bot levels 79-109, eleven nanos on the same prefix ladder. MC/TS 419 -> 569; 628 -> 868 credits.

*Standard robot summon: cast the nano, pay the credits, and the new bot replaces whatever bot you had. Keep MC and TS at or above 80% of the cast requirement or the bot stops obeying and drops to behind (see Mechanics).*

| Nano | id | Bot lvl | MC/TS | Credits | Char lvl | Gate | Nano source |
|---|---|---|---|---|---|---|---|
| Feeble Guardbot | 45714 | 79 | 419 | 628 |  |  |  |
| Patchwork Guardbot | 45694 | 82 | 434 | 652 |  |  |  |
| Lesser Guardbot | 45686 | 85 | 449 | 676 |  |  |  |
| Inferior Guardbot | 45706 | 88 | 464 | 700 |  |  |  |
| Flawed Guardbot | 45720 | 91 | 479 | 724 |  |  |  |
| Common Guardbot | 45726 | 94 | 494 | 748 |  |  |  |
| Guardbot | 45702 | 97 | 509 | 772 |  |  |  |
| Upgraded Guardbot | 45665 | 100 | 524 | 796 |  |  |  |
| Advanced Guardbot | 45733 | 103 | 539 | 820 |  |  |  |
| Perfected Guardbot | 45700 | 106 | 554 | 844 |  |  |  |
| Semi-Sentient Guardbot | 45683 | 109 | 569 | 868 |  |  |  |

### Attack robot - Warbot family  <sub>`SpawnItem ENWA`</sub> - 12 nanos

Bot levels 112-145, twelve nanos - the prefix ladder plus a 'Military-Grade Warbot' top step. MC/TS 584 -> 711; 892 -> 1,156 credits.

*Standard robot summon: cast the nano, pay the credits, and the new bot replaces whatever bot you had. Keep MC and TS at or above 80% of the cast requirement or the bot stops obeying and drops to behind (see Mechanics).*

| Nano | id | Bot lvl | MC/TS | Credits | Char lvl | Gate | Nano source |
|---|---|---|---|---|---|---|---|
| Feeble Warbot | 45715 | 112 | 584 | 892 |  |  |  |
| Patchwork Warbot | 45695 | 115 | 596 | 916 |  |  |  |
| Lesser Warbot | 45687 | 118 | 608 | 940 |  |  |  |
| Inferior Warbot | 45707 | 121 | 620 | 964 |  |  |  |
| Flawed Warbot | 45721 | 124 | 632 | 988 |  |  |  |
| Common Warbot | 45727 | 127 | 644 | 1,012 |  |  |  |
| Warbot | 45668 | 130 | 656 | 1,036 |  |  |  |
| Upgraded Warbot | 45666 | 133 | 668 | 1,060 |  |  |  |
| Advanced Warbot | 45734 | 136 | 680 | 1,084 |  |  |  |
| Perfected Warbot | 45677 | 139 | 692 | 1,108 |  |  |  |
| Semi-Sentient Warbot | 45684 | 142 | 702 | 1,132 |  |  |  |
| Military-Grade Warbot | 45689 | 145 | 711 | 1,156 |  |  |  |

### Attack robot - Warmachine family  <sub>`SpawnItem EGWA`</sub> - 12 nanos

Bot levels 148-181, twelve nanos ending in 'Military-Grade Warmachine'. MC/TS 720 -> 819; 1,180 -> 1,444 credits.

*Standard robot summon: cast the nano, pay the credits, and the new bot replaces whatever bot you had. Keep MC and TS at or above 80% of the cast requirement or the bot stops obeying and drops to behind (see Mechanics).*

| Nano | id | Bot lvl | MC/TS | Credits | Char lvl | Gate | Nano source |
|---|---|---|---|---|---|---|---|
| Feeble Warmachine | 45716 | 148 | 720 | 1,180 |  |  |  |
| Patchwork Warmachine | 45696 | 151 | 729 | 1,204 *(req 1,205)* |  |  |  |
| Lesser Warmachine | 45688 | 154 | 738 | 1,228 |  |  |  |
| Inferior Warmachine | 45708 | 157 | 747 | 1,252 |  |  |  |
| Flawed Warmachine | 45722 | 160 | 756 | 1,276 |  |  |  |
| Common Warmachine | 45728 | 163 | 765 | 1,300 |  |  |  |
| Warmachine | 45669 | 166 | 774 | 1,324 |  |  |  |
| Upgraded Warmachine | 45667 | 169 | 783 | 1,348 |  |  |  |
| Advanced Warmachine | 45735 | 172 | 792 | 1,372 |  |  |  |
| Perfected Warmachine | 45678 | 175 | 801 | 1,396 |  |  |  |
| Semi-Sentient Warmachine | 45670 | 178 | 810 | 1,420 |  |  |  |
| Military-Grade Warmachine | 45690 | 181 | 819 | 1,444 |  |  |  |

### Attack robot - Wardroid family  <sub>`SpawnItem ENWR`</sub> - 3 nanos

Bot levels 184-190, only three nanos: Decommissioned / Reactivated / Semi-Sentient Wardroid. MC/TS 827 -> 845.

*Standard robot summon: cast the nano, pay the credits, and the new bot replaces whatever bot you had. Keep MC and TS at or above 80% of the cast requirement or the bot stops obeying and drops to behind (see Mechanics).*

| Nano | id | Bot lvl | MC/TS | Credits | Char lvl | Gate | Nano source |
|---|---|---|---|---|---|---|---|
| Decommissioned Wardroid | 45729 | 184 | 827 | 1,468 |  |  |  |
| Reactivated Wardroid | 45679 | 187 | 836 | 1,492 *(req 1,493)* |  |  |  |
| Semi-Sentient Wardroid | 45685 | 190 | 845 | 1,516 |  |  |  |

### Attack robot - Slayerdroid family  <sub>`SpawnItem ENSL/ENSA`</sub> - 4 nanos

Bot levels 193-200, four nanos: Protector and Warden (ENSL), then Sentinel and Guardian (ENSA). Slayerdroid Guardian (45671, bot level 200, MC/TS 874) is the strongest robot an Engineer can build WITHOUT Shadowlands - the froob ceiling.

*Cross-check: the AODB Engineer Guide states the Slayerdroid Guardian 'needs 874 Matter Creation/Time and Space to cast but requires 700 MC/TS to maintain'. 874 is exactly the local data's 'MaterialCreation > 873', and 700 is 874 x 0.8 rounded up - the documented over-equip control threshold. Local data and the guide agree exactly.*

| Nano | id | Bot lvl | MC/TS | Credits | Char lvl | Gate | Nano source |
|---|---|---|---|---|---|---|---|
| Slayerdroid Protector | 45672 | 193 | 853 | 1,540 |  |  |  |
| Slayerdroid Warden | 45674 | 196 | 862 | 1,564 |  |  |  |
| Slayerdroid Sentinel | 45673 | 198 | 868 | 1,580 |  |  |  |
| Slayerdroid Guardian | 45671 | 200 | 874 | 1,596 |  |  |  |

### Attack robot - Slayerdroid Annihilator  <sub>`SpawnItem ENSY`</sub> - 1 nano

A single Shadowlands nano that jumps straight to bot level 201 - one above the best froob bot - for MC/TS 1,096/1,095 and 3,208 credits, at character level 175.

*Requires Shadowlands and Specialization bit 4. Nano from the Adonis Garden (AODB wiki).*

| Nano | id | Bot lvl | MC/TS | Credits | Char lvl | Gate | Nano source |
|---|---|---|---|---|---|---|---|
| Slayerdroid Annihilator | 223313 | 201 | 1096/1095 | 3,208 *(req 3,209)* | 175 | SL Spec4 | Adonis Garden |

### Attack robot - Predator M-30 line  <sub>`SpawnItem EGPR/EIPR/ENUP/ENAV/EGSE/EIMA`</sub> - 6 nanos

Six Shadowlands nanos, bot levels 100 -> 211, each with its own template code: Prototype (EGPR, bot 100), Predator M-30 (EIPR, 150), Upgraded (ENUP, 175), Advanced (ENAV, 203), Semi-Sentient (EGSE, 207), Military-Grade (EIMA, 211). All six carry an explicit character-Level requirement equal to their own bot level (100 / 150 / 175 / 203 / 207 / 211) - as do the Devastator, Marauder, Desolator and Widowmaker nanos. The one Shadowlands bot that does NOT follow that rule is the Slayerdroid Annihilator (bot 201 at character level 175).

*The Predator line's entry nano is the only bot summon gated at character level 100, which lines up with the AODB Engineer Guide's 'from level 100 you can also have a pet dog' and its note that commands then go to BOTH pets. Local data does not prove the Predator line IS that second pet - see _unverified.*

| Nano | id | Bot lvl | MC/TS | Credits | Char lvl | Gate | Nano source |
|---|---|---|---|---|---|---|---|
| Prototype Predator M-30 | 223325 | 100 | 688 | 1,592 *(req 1,593)* | 100 | SL Spec2 |  |
| Predator M-30 | 301855 | 150 | 890 | 2,592 | 150 | SL Spec2 | Scheol Garden |
| Upgraded Predator M-30 | 223327 | 175 | 1087 | 2,872 *(req 2,873)* | 175 | SL Spec4 | Adonis/Inferno Garden |
| Advanced Predator M-30 | 223329 | 203 | 1285 | 3,240 *(req 3,241)* | 203 | SL Spec8 | Adonis Sanctuary |
| Semi-Sentient Predator M-30 | 223331 | 207 | 1464/1463 | 3,304 *(req 3,305)* | 207 | SL Spec8 | Penumbra Garden |
| Military-Grade Predator M-30 | 223333 | 211 | 1643/1641 | 3,368 *(req 3,369)* | 211 | SL Spec8 | Inferno Garden |

### Attack robot - Devastator Drone line  <sub>`SpawnItem ENDV/EGBA/ENFE`</sub> - 3 nanos

Three Shadowlands nanos: Devastator Drone (ENDV, bot 205), Battlefield Devastator Drone (EGBA, 209), Fieldsweeper Devastator Drone (ENFE, 213). MC/TS 1,255 -> 1,583; 3,272 - 3,400 credits.

*Standard robot summon: cast the nano, pay the credits, and the new bot replaces whatever bot you had. Keep MC and TS at or above 80% of the cast requirement or the bot stops obeying and drops to behind (see Mechanics).*

| Nano | id | Bot lvl | MC/TS | Credits | Char lvl | Gate | Nano source |
|---|---|---|---|---|---|---|---|
| Devastator Drone | 223315 | 205 | 1255/1250 | 3,272 *(req 3,273)* | 205 | SL Spec8 | Penumbra Garden |
| Battlefield Devastator Drone | 223317 | 209 | 1404/1401 | 3,336 *(req 3,337)* | 209 | SL Spec8 | Penumbra Sanctuary |
| Fieldsweeper Devastator Drone | 223319 | 213 | 1583/1579 | 3,400 *(req 3,401)* | 213 | SL Spec8 | Inferno Garden |

### Attack robot - Marauder M-45 line  <sub>`SpawnItem EGMR/MIMD`</sub> - 2 nanos

Two Shadowlands nanos: Marauder M-45 (EGMR, bot 215) and Military-Grade Marauder M-45 (MIMD, bot 219).

*Standard robot summon: cast the nano, pay the credits, and the new bot replaces whatever bot you had. Keep MC and TS at or above 80% of the cast requirement or the bot stops obeying and drops to behind (see Mechanics).*

| Nano | id | Bot lvl | MC/TS | Credits | Char lvl | Gate | Nano source |
|---|---|---|---|---|---|---|---|
| Marauder M-45 | 223335 | 215 | 1722/1719 | 3,432 *(req 3,433)* | 215 | SL Spec8 | Inferno Sanctuary |
| Military-Grade Marauder M-45 | 223337 | 219 | 1851/1847 | 3,496 *(req 3,497)* | 219 | SL Spec8 | Pandemonium Vendors |

### Attack robot - Desolator Assault Drone  <sub>`SpawnItem EGDE`</sub> - 1 nano

Single Shadowlands nano, bot level 217, MC 1,763 / TS 1,757, character level 217.

*Data anomaly worth knowing: this is the ONE bot whose cast requirement (Cash >= 3,465) is higher than the credits it actually deducts (Hit[61,-3400,-3400,0]).*

| Nano | id | Bot lvl | MC/TS | Credits | Char lvl | Gate | Nano source |
|---|---|---|---|---|---|---|---|
| Desolator Assault Drone | 223321 | 217 | 1763/1757 | 3,400 *(req 3,465)* | 217 | SL Spec8 | Inferno Sanctuary |

### Attack robot - Widowmaker Battle Drone  <sub>`SpawnItem EGDS`</sub> - 1 nano

Single Shadowlands nano, bot level 220, MC 1,896 / TS 1,891, 3,512 credits, character level 220. The highest bot level reachable through the Shadowlands/Specialization line.

*Nano from Pandemonium vendors (AODB wiki).*

| Nano | id | Bot lvl | MC/TS | Credits | Char lvl | Gate | Nano source |
|---|---|---|---|---|---|---|---|
| Widowmaker Battle Drone | 223323 | 220 | 1896/1891 | 3,512 *(req 3,513)* | 220 | SL Spec8 | Pandemonium Vendors |

### Attack robot - Ravening M-60  <sub>`SpawnItem RAM6`</sub> - 1 nano

Bot level 220 and the single most expensive summon in the data: MC 1,960 / TS 1,958 and 3,694 credits. It is the ONLY robot summon gated by 'NanoFocusLevel op22 64' instead of Expansion/Specialization - a late endgame nano-focus gate rather than a Shadowlands gate.

*Same bot level as the Widowmaker but a far higher skill requirement, so in practice it is the last bot an Engineer unlocks. Its nano source is not listed on the AODB Engineer nano page and is left unknown.*

| Nano | id | Bot lvl | MC/TS | Credits | Char lvl | Gate | Nano source |
|---|---|---|---|---|---|---|---|
| Ravening M-60 | 275815 | 220 | 1960/1958 | 3,694 *(req 3,695)* |  | NanoFocus64 |  |

### Deployable - Jamming Tower  <sub>`SpawnItem EGJA`</sub> - 1 nano

The Engineer's only non-robot SpawnItem nano in the data: Jamming Tower (274369) spawns template EGJA at level 1 for MC/TS 20 and no credit cost. It sits in its own category because its template family and level argument (EGJA, 1) belong to no bot ladder.

*Local data shows only the spawn - the nano carries no attack, heal or buff function - so what the tower actually jams is NOT established here. Neither the AODB Engineer nano page nor the AO-Universe Engineer guide covers it, so it is left unknown rather than guessed.*

| Nano | id | Bot lvl | MC/TS | Credits | Char lvl | Gate | Nano source |
|---|---|---|---|---|---|---|---|
| Jamming Tower | 274369 | 1 | 20 | *free* |  |  |  |

**Ladder totals:** 75 non-Shadowlands bots (bot level 1-200, topping out at Slayerdroid Guardian 45671),
14 Shadowlands-gated bots (`Expansion op22 2`, bot level 100-220) and 1 nano-focus-gated bot
(Ravening M-60 275815, `NanoFocusLevel op22 64`). 90 robots + 1 Jamming Tower = **91 of 91**.

---

## Bot repair - the Engineer has no heal pet

All seven are `Hit[27,min,max]` (stat 27 = Health), gated on Matter Creation + Time & Space +
**Biological Metamorphosis**, `VisualProfession == 3`, and `Breed == 7` (HumanMonster) - the encoding
for "this nano is cast on a pet". BioMet is a real Engineer skill investment, purely to repair the bot.

| Repair nano | id | Heals | MC/TS | BioMet |
|---|---|---|---|---|
| Quick Fix | 116793 | 120-199 | 84 | 70 |
| Patchy Repairs | 116794 | 243-412 | 171 | 147 |
| Recondition Parts | 116797 | 398-611 | 246 | 221 |
| Rebuild Casing | 116792 | 576-992 | 406 | 360 |
| Field Workshop | 116796 | 800-1411 | 572 | 518 |
| Intricate Repairs | 116795 | 1063-1875 | 738 | 666 |
| A Maker's Touch | 116791 | 1285-2267 | 842 | 761 |

Three later nanos repair **and** cleanse (350 heal + cut `Snare(145)`/`Root(146)` strain duration):

- **Synchronized Energy Spike** (223305), level 185+
- **Energized Casing of the Faithful Servant** (203867), level 195+
- **Synchronized Capacitor Overload** (223339), level 214+

> AODB's Engineer Guide says **"All Bot heals are mission reward only"** and that the Sympathetic and
> Disruptive lines are not sold in shops - see Unverified.

---

## Pet buffs and pet utility - 39 nanos, by nano line

All 36 in the `Pet Buffs / Pet Utility` category, plus 3 pet-targeted nanos the extractor filed
elsewhere (2 Miniaturization under *Self / Team Buffs*, Sedative Injectors under *Misc / Utility*).

### `NOSTACKING(0)` - 7 nanos

The `Energize Shell` and `Conductive Spike` lines shorten Snare/Root already on the bot (`ReduceNanoStrainDuration[145/146]`), plus the strain-288 cancellation nano.

| Nano | id | Lvl | Effect |
|---|---|---|---|
| Lesser Energize Shell | 203869 | - | ReduceNanoStrainDuration[145,292] / ReduceNanoStrainDuration[146,292] / SystemText[Your pet moves more freely.] |
| Energize Shell | 203871 | 25 | ReduceNanoStrainDuration[145,414] / ReduceNanoStrainDuration[146,414] / SystemText[Your pet moves more freely.] |
| Intrusive Aura Cancellation | 204372 | 50 | ReduceNanoStrainDuration[288,900000] |
| Greater Energize Shell | 203873 | 75 | ReduceNanoStrainDuration[145,583] / ReduceNanoStrainDuration[146,583] / SystemText[Your pet moves more freely.] |
| Lesser Conductive Spike | 203861 | 100 | ReduceNanoStrainDuration[145,711] / ReduceNanoStrainDuration[146,711] / SystemText[Your pet moves more freely.] |
| Conductive Spike | 203863 | 125 | ReduceNanoStrainDuration[145,896] / ReduceNanoStrainDuration[146,896] / SystemText[Your pet moves more freely.] |
| Greater Conductive Spike | 203865 | 145 | ReduceNanoStrainDuration[145,1084] / ReduceNanoStrainDuration[146,1084] / SystemText[Your pet moves more freely.] |

### `MPPetInitiativeBuffs(217)` - 1 nano

Strain named after the MP, but the Engineer casts this one: endgame pet initiative and aggressiveness.

| Nano | id | Lvl | Effect |
|---|---|---|---|
| Formula 22 | 275016 | - | Modify MeleeInit +240 / Modify RangedInit +240 / Modify PhysicalInit +240 / Modify NanoCInit +240 / Modify Aggressiveness +80 |

### `PetShortTermDamageBuffs(225)` - 11 nanos

The bot's attack-rating ladder: five `Combat Array` steps then the same five prefixes on `Aggression Subsystem`, topped by the Omni-Pol nano. Every one requires `NPCFamily == 95` (EngineerAttackPet) and `Breed == 7`. **The Engineer has no long-term pet damage line** - the MP's Instill (strain 216) does not appear in the Engineer-castable extract at all.

| Nano | id | Lvl | Effect |
|---|---|---|---|
| Assist Combat Array | 205229 | - | Modify AddAllOff +32 / Modify AddAllDef +6 |
| Monitor Combat Array | 205231 | 25 | Modify AddAllOff +79 / Modify AddAllDef +14 |
| Enhance Combat Array | 205233 | 50 | Modify AddAllOff +120 / Modify AddAllDef +21 |
| Boost Combat Array | 205235 | 75 | Modify AddAllOff +165 / Modify AddAllDef +28 |
| Overdrive Combat Array | 205237 | 75 | Modify AddAllOff +205 / Modify AddAllDef +36 |
| Assist Aggression Subsystem | 205239 | 100 | Modify AddAllOff +250 / Modify AddAllDef +43 / ResistNanoStrain[145,1] / ResistNanoStrain[146,1] |
| Monitor Aggression Subsystem | 205241 | 135 | Modify AddAllOff +291 / Modify AddAllDef +50 / ResistNanoStrain[145,2] / ResistNanoStrain[146,2] |
| Enhance Aggression Subsystem | 205243 | 155 | Modify AddAllOff +338 / Modify AddAllDef +58 / ResistNanoStrain[145,3] / ResistNanoStrain[146,3] |
| Boost Aggression Subsystem | 205245 | 165 | Modify AddAllOff +374 / Modify AddAllDef +68 / ResistNanoStrain[145,5] / ResistNanoStrain[146,5] |
| Overdrive Aggression Subsystem | 205247 | 185 | Modify AddAllOff +417 / Modify AddAllDef +79 / ResistNanoStrain[145,8] / ResistNanoStrain[146,8] |
| Omni-Pol Pacification Logic System | 205249 | 195 | Modify AddAllOff +446 / Modify AddAllDef +88 / ResistNanoStrain[145,11] / ResistNanoStrain[146,11] |

### `PetSnare_RootResistanceBuff(285)` - 4 nanos

`Polarized Screening` - the bot RESISTS Snare(145)/Root(146) and gains NanoResist.

| Nano | id | Lvl | Effect |
|---|---|---|---|
| Lesser Polarized Screening | 204339 | 50 | Modify NanoResist +10 / ResistNanoStrain[145,25] / ResistNanoStrain[146,25] |
| Polarized Screening | 204335 | 75 | Modify NanoResist +25 / ResistNanoStrain[145,32] / ResistNanoStrain[146,32] |
| Superior Polarized Screening | 204337 | 100 | Modify NanoResist +50 / ResistNanoStrain[145,41] / ResistNanoStrain[146,41] |
| Greater Polarized Screening | 204341 | 145 | Modify NanoResist +100 / ResistNanoStrain[145,53] / ResistNanoStrain[146,53] |

### `EngineerPetAOESnareBuff(288)` - 6 nanos

**Engineer-only, no MP equivalent: this turns the BOT into an AoE snare emitter.** Each nano is `AreaCastNano[<child>, 15]` cast on the bot plus a small AddAllOff. `Intrusive Aura Cancellation` (204372) exists solely to switch it off - its single function is `ReduceNanoStrainDuration[288, 900000]`, which burns the remaining duration of strain 288 on the bot.

| Nano | id | Lvl | Effect |
|---|---|---|---|
| Intrusive Aura of Slave | 275835 | - | AreaCastNano[275834,15] / Modify AddAllOff +130 |
| Intrusive Aura of Entanglement | 204362 | 50 | AreaCastNano[204357,15] / AreaCastNano[204357,15] / Modify AddAllOff +15 |
| Intrusive Aura of Binding | 204364 | 75 | AreaCastNano[204358,15] / AreaCastNano[204358,15] / Modify AddAllOff +30 |
| Intrusive Aura of Malaise | 204366 | 125 | AreaCastNano[204359,15] / AreaCastNano[204359,15] / Modify AddAllOff +45 |
| Intrusive Aura of Sloth | 204368 | 165 | AreaCastNano[204360,15] / AreaCastNano[204360,15] / Modify AddAllOff +60 |
| Intrusive Aura of the Humble Servant | 204370 | 195 | AreaCastNano[204361,15] / AreaCastNano[204361,15] / Modify AddAllOff +100 |

### `PetDefensiveNanos(816)` - 3 nanos

`Software Hacking Shielding` - bot AddAllDef plus resistance to Mezz(147) and CharmOther(202). The top one also adds +1000 to every AC.

| Nano | id | Lvl | Effect |
|---|---|---|---|
| Lesser Software Hacking Shielding | 267606 | 201 | ResistNanoStrain[147,15] / ResistNanoStrain[202,15] / Modify AddAllDef +200 |
| Advanced Software Hacking Shielding | 267608 | 215 | Modify AddAllDef +300 / ResistNanoStrain[147,25] / ResistNanoStrain[202,25] / Modify ProjectileAC +1000 / Modify MeleeAC +1000 / Modify EnergyAC +1000 / Modify ChemicalAC +1000 / M |
| Software Hacking Shielding | 267607 | 215 | ResistNanoStrain[147,20] / ResistNanoStrain[202,20] / Modify AddAllDef +250 |

### `PetRoot / SnareandMezzRemoval(1013)` - 1 nano

Shared Engi/Crat/MP pet utility.

| Nano | id | Lvl | Effect |
|---|---|---|---|
| Pet Attention | 269869 | - | SystemText[Your pets move more freely.] |

### `Charm_Short(1022)` - 2 nanos

Reclaim a charmed bot - `RemoveNanoStrain[202]` = CharmOther. Shared with Crat and MP (ToUse lists VisualProfession 3, 8 and 12).

| Nano | id | Lvl | Effect |
|---|---|---|---|
| Pet Steal Back | 269907 | 100 | RemoveNanoStrain[202] / SystemText[Your manifestation moves more freely.] |
| Improved Pet Steal Back | 269908 | 215 | RemoveNanoStrain[202] / SystemText[Your manifestation moves more freely.] |

### `PetDebuffCleanse(1047)` - 1 nano

Shared Engi/Crat/MP pet debuff cleanse.

| Nano | id | Lvl | Effect |
|---|---|---|---|
| Pet Cleanse | 269870 | - | SystemText[Your pets move more freely.] |

### `EngineerMiniaturization(811)` - 2 nanos

**Yes - Miniaturization IS the "shrink the bot" utility.** `Modify Scale` is negative and it adds AddAllDef, and the ToUse list carries `NPCFamily == 95` plus `MonsterData == 218783/218928`, so it is cast on the bot. Scheol quest reward (AODB wiki). `engineer-nanos.json` files these two under *Self / Team Buffs* - that categorisation is misleading.

| Nano | id | Lvl | Effect |
|---|---|---|---|
| Lesser Miniaturization | 263301 | - | Modify AddAllDef +30 / Modify Scale -30 |
| Miniaturization | 263303 | - | Modify AddAllDef +50 / Modify Scale -50 |

### `SiphonBox683(683)` - 1 nano

Installs an offensive proc on the bot (`AddOffProc`), ToUse `NPCFamily == 95`. Filed under *Misc / Utility* in `engineer-nanos.json`.

| Nano | id | Lvl | Effect |
|---|---|---|---|
| Sedative Injectors | 302254 | 211 | AddOffProc[5,302253] |

---

## What is NOT a pet mechanic (stated plainly)

- **`EngineerAuras` (strain 227, 15 nanos, "Sympathetic ...")** are `TeamCastNano` and
  **`EngineerDebuffAuras` (strain 236, 14 nanos, "Disruptive ...")** are `AreaCastNano`. In the local data
  **both are cast by the ENGINEER** (`VisualProfession == 3`) with **no** `NPCFamily` and **no** `Breed == 7`
  requirement. On the evidence they are the Engineer's own team / hostile auras and they belong in
  `engineer-buffs.json`, not here. The **only** genuinely pet-emitted aura in the data is the
  `Intrusive Aura of ...` line (strain 288) above, which does carry `Breed == 7`.
- **`EngineerSpecialAttackAbsorber` (strain 286, the "Sloughing ..." line, 14 nanos)** is self/team:
  `ChangeVariable` on `SpecialAttackShield(517)` or `TeamCastNano`, with no pet requirement. Not a pet mechanic.
- **Beacon Warp (154914) / Team Beacon Warp (154913)** (strain 293) warp *players*, not pets.
  No `PetWarp` (NanoLine 1019) nano exists in the Engineer-castable extract.

---

## Pet commands

| Command | Effect |
|---|---|
| `/pet guard` | Follow the Engineer and attack anything that attacks him. This is the DEFAULT state right after a bot is cast, and again after zoning (AO-Universe Basic Pet Commands Guide). |
| `/pet follow` | Follow the owner; movement only, no engaging. |
| `/pet attack` | Attack the current target. |
| `/pet behind` | Stay behind the owner / retreat from combat. AO-Universe recommends issuing it immediately after zoning so the bot does not pull guards or towers. It is also the state an OVER-EQUIPPED bot is forced into. |
| `/pet wait` | Stay exactly where it is: do not move, do not attack anything. |
| `/pet heal` | Listed by AO-Universe as a generic pet command (heal yourself, another player, a pet, or certain NPCs). The ENGINEER has no heal pet, so this is not an Engineer tool - an Engineer repairs the bot by casting the pet-heal nanos (Quick Fix 116793 ... A Maker's Touch 116791) himself. |
| `/pet report` | The bot reports its health and current fighting target. |
| `/pet chat <message>` | Make the bot say a one-word message (AO-Universe). |
| `/pet rename <name>` | Rename the bot. The AODB Engineer Guide notes the new name must be 4 or more characters. |
| `/pet terminate` | Terminate all current pets. |
| `/pet "PETNAME" <command>` | Scope any command to ONE named pet. This matters specifically for the Engineer: the AODB Engineer Guide states that from level 100, with a second pet out, every command except /pet rename goes to BOTH pets unless it is name-scoped. |
| `/pet help` | Open the Pet Info window (clickable command macros). |
| `/tell Helpbot oe <skill>` | NOT a client command - the over-equip check is a chat-bot tell. AODB notes Helpbot is down and that Aquest now carries those services (/tell Aquest help). The MP profile's '/oe <skill#>' entry is this same thing written as if it were a slash command. |

---

## Over-equipping the bot (the Engineer-defining rule)

- **Formula (AO-Universe):** `OE = 100 - (buffed skill / requirement) x 100`. **20 or more = over-equipped.**
- **For a weapon** OE costs damage (25% / 50% / 75% / 100% at 20/40/60/80). **For a PET it costs CONTROL:**
  *"All pets commanded with more than 20% over-equipped summon/charm stats will not respond. They will
  finish fighting all their attackers, before going to the pet behind state. Pets will refuse to obey
  commands until the skills are back up."*
- **Refusal message:** `Illegal command. Master not recognised.` (AODB Engineer:Tips and Tricks).
- **Control threshold (AODB):** `skill required to control = skill to make x 0.8` (round up).
- **Worked example, guide and local data agreeing exactly:** Slayerdroid Guardian (45671) - AODB says
  *"needs 874 Matter Creation/Time and Space to cast but requires 700 MC/TS to maintain"*; local data says
  `MaterialCreation > 873` (= 874) and 874 x 0.8 = 700 (rounded up).
- **So a wrangle/Mochams over-equip is legitimate for CASTING a higher bot**, but the buff may only fall to
  80% of the requirement, never off entirely. This is the sharpest behavioural difference from the MP.
- **There is no client-side `/oe` command.** The check is a chat-bot tell: `/tell Helpbot oe <skill>`;
  AODB notes Helpbot is down and Aquest now carries it (`/tell Aquest help`).

---

## Tradeskill bot upgrades (Engineer-only; ITEMS, not nanos - none are in `engineer-nanos.json`)

| Upgrade | Type | Effect |
|---|---|---|
| Trimmer - Positive Aggressive-Defensive | permanent | Sets the bot's agg/def bar, up to 100% agg at QL200 - attacks faster, evades worse |
| Trimmer - Negative Aggressive-Defensive | permanent | The reverse: slower attacks, better evades |
| Trimmer - Increase Aggressiveness | permanent | Adds taunt to the bot's hits (~+90 at QL200). Built from XU-11 Serum + Chemical Impact Injector + Trimmer Casing (Mechanical Engineering + a little Chemistry, QL 30-200) |
| Trimmer - Divert Energy to Offense | temporary | QL200: +40 AAO and +40 damage, ACs -1000 |
| Trimmer - Divert Energy to Defense | temporary | Armour class up, attack rating down |
| Trimmer - Divert Energy to Hitpoints | temporary | Max health up, defence down |
| Trimmer - Divert Energy to Avoidance | temporary | Evades up, max health down (AODB notes it is bugged and raises Poison AC instead of Duck Explosions) |
| Damage type trimmers (fire / energy / cold) | 10 min | Change the bot's damage type; cut its hitpoints significantly; lock Mechanical Engineering for 60 min, so once per hour |
| Trimmer - Improve Actuators | 10 min | Emergency combined heal + damage boost: heals the bot up to ~6k HP and boosts damage for 600s; locks Mechanical Engineering 1 hour. Made from Smelly Liquid (drops from Alien Generals) + Trimmer Casing |
| Android NCU Upgrade | permanent | ~+110 NCU on the bot at QL200, at the cost of ~400 AC |

*(Exact QLs, magnitudes and component lists are guide text, not local item data - `items.ocp` was not
queried for trimmer templates in this pass. See Unverified.)*

---

## Mechanics summary

- ONE role only: every Engineer pet in the data is an ATTACK robot. 90 of the 91 pet-summon nanos are robots (category 'Pets - Robot'); the 91st is the Jamming Tower. There is no Engineer heal pet and no Engineer mezz pet - the MP's three-slot attack/heal/mezz model does NOT transfer.
- Summon function: Engineer bots use SpawnItem (FunctionType 53064) with args [<4-char template code>, <bot level>, 0], where the MP uses SummonPet (53167). OmniCell's server source (Server/ZoneEngine/Core/Pets.cs) documents that all 476 SpawnItem calls take the same three arguments as SummonPet and that the hashes are pet names (it names ENAU, ENGA, ENGU, ENWA as Engineer templates), so for pet purposes SpawnItem summons a pet exactly as SummonPet does.
- Summon skills are always Matter Creation (130) + Time & Space (131) at the same value (one to three points apart on a few endgame nanos), plus Profession(60) == 3. Unlike the MP, NO Engineer robot summon gates via VisualProfession(368) - all 90 use Profession 60. The pet-heal line is the exception: those seven gate via VisualProfession == 3.
- Bots COST CREDITS. 79 of the 90 robot summons carry Hit[61,-N,-N,0] (stat 61 = Cash) plus a matching 'Cash > N-1' cast requirement, so casting deducts N credits. The 11 Automaton summons are the exception - free. Range: 132 credits (Feeble Android) to 3,694 (Ravening M-60). One anomaly: Desolator Assault Drone (223321) requires Cash >= 3,465 but deducts only 3,400. This is a real resource constraint an Engineer bot AI must check before casting - an MP never pays to summon.
- OVER-EQUIP is the Engineer's defining pet constraint. AO-Universe's OE guide gives OE = 100 - (buffed skill / requirement) x 100, and 20 or more means over-equipped. For a PET this is not a damage penalty as it is for a weapon: 'All pets commanded with more than 20% over-equipped summon/charm stats will not respond. They will finish fighting all their attackers, before going to the pet behind state. Pets will refuse to obey commands until the skills are back up.' The refusal message is 'Illegal command. Master not recognised.' (AODB Engineer:Tips and Tricks).
- So the maintenance rule is: keep MC and TS at or above 80% of the SUMMON requirement, not merely at the value you cast with. AODB: 'Amount of Skill Required to Control = Amount of skill to make or wear * 0.8 (round up)'. Worked example from that guide, cross-checked against local data: Slayerdroid Guardian (45671) needs MC/TS 874 to cast (data: 'MaterialCreation > 873') and 700 to keep controlling it. A wrangle / Mochams over-equip is therefore legitimate for CASTING a higher bot, but the buff may only fall to 80% of the requirement - it may not drop off entirely. This is the sharpest difference from the MP, whose pets stay obedient after the wrangle expires.
- Bot persistence: the AODB Engineer Guide states the bot lasts until you terminate it, it dies, or you log off, and that on a lost connection it waits 2 minutes for you to reconnect before powering down.
- Command range is 50 m; out of range the client answers "Pet is too far away. It can't hear your command" (AODB Engineer Guide).
- TWO PETS from level 100 (AODB Engineer Guide): 'From level 100 you can also have a pet dog. From then on, if you issue a command, other than /pet rename, it is issued to both pets. To issue a command to one pet use /pet "petname" command.' Any bot AI must name-scope commands once a second pet is out. Note the direction of the difference: the MP has three pets in three distinct slots with three distinct roles; the Engineer has up to two, both attack-role.
- Pet heals are cast BY the Engineer ON the bot. The seven-step line Quick Fix (116793) -> Patchy Repairs (116794) -> Recondition Parts (116797) -> Rebuild Casing (116792) -> Field Workshop (116796) -> Intricate Repairs (116795) -> A Maker's Touch (116791) are all Hit[27,min,max] (stat 27 = Health), gated on MaterialCreation + SpaceTime + BiologicalMetamorphosis and VisualProfession(368) == 3, with Breed == 7 (HumanMonster) in ToUse - the encoding for 'cast on a pet'. Heal sizes run 120-199 up to 1,285-2,267.
- Three later repair nanos also cleanse: Synchronized Energy Spike (223305, L185), Energized Casing of the Faithful Servant (203867, L195) and Synchronized Capacitor Overload (223339, L214) each heal 350 AND cut Snare(145)/Root(146) strain duration.
- Pet attack-rating ladder: PetShortTermDamageBuffs strain 225, eleven nanos - Assist / Monitor / Enhance / Boost / Overdrive 'Combat Array', then the same five prefixes on 'Aggression Subsystem', topped by Omni-Pol Pacification Logic System (205249, L195, +446 AddAllOff / +88 AddAllDef). Every one requires NPCFamily == 95 (EngineerAttackPet) and Breed == 7, i.e. cast on the bot. The Engineer has no separate long-term pet damage line equivalent to the MP's Instill (strain 216) - strain 216 does not appear in the Engineer-castable extract at all.
- Bot mobility and defence utilities, all cast on the bot: the Energize Shell line (203869 / 203871 / 203873) and the Conductive Spike line (203861 / 203863 / 203865) shorten Snare/Root on the bot; Polarized Screening (strain 285: 204339 / 204335 / 204337 / 204341) makes the bot RESIST Snare/Root; Software Hacking Shielding (strain 816: 267606 / 267607 / 267608) adds +200..+300 AddAllDef plus resistance to Mezz(147) and CharmOther(202), and the top one adds +1000 to every AC.
- The bot itself can be made an AoE SNARE emitter - an Engineer-only trick with no MP equivalent. EngineerPetAOESnareBuff strain 288: 'Intrusive Aura of Entanglement / Binding / Malaise / Sloth / the Humble Servant / Slave' (204362, 204364, 204366, 204368, 204370, 275835). Each is AreaCastNano[<child nano>, 15] cast on the bot plus a small AddAllOff. Intrusive Aura Cancellation (204372) exists solely to switch it off: its single function is ReduceNanoStrainDuration[288, 900000], i.e. it burns the remaining duration of strain 288 on the bot - useful before a mezzed pull.
- Miniaturization (263303) and Lesser Miniaturization (263301), strain 811, shrink the bot: Modify Scale -50 / -30 together with Modify AddAllDef +50 / +30, ToUse NPCFamily == 95. Scheol quest reward (AODB wiki). They are filed under 'Self / Team Buffs' in engineer-nanos.json; that category is misleading - they target the bot.
- Sedative Injectors (302254, L211) installs an offensive proc on the bot: AddOffProc[5, 302253] with ToUse NPCFamily == 95.
- Pet recovery and cleanse are shared with Bureaucrat and Meta-Physicist - their ToUse lists carry VisualProfession == 3, 8 AND 12: Pet Attention (269869, strain 1013), Pet Cleanse (269870, strain 1047), Pet Steal Back (269907) and Improved Pet Steal Back (269908) (strain 1022, RemoveNanoStrain[202] = CharmOther) reclaim a charmed bot.
- Formula 22 (275016, strain 217 MPPetInitiativeBuffs - the strain is named after the MP but the Engineer casts this one) is the Engineer's endgame pet initiative buff: +240 to all four initiatives and +80 Aggressiveness on the bot. Requires MC/TS 1,825 and the NanoFocusLevel bit-64 gate.
- AURAS - which is which, stated plainly. EngineerAuras (strain 227, 15 nanos, 'Sympathetic ...') are TeamCastNano and EngineerDebuffAuras (strain 236, 14 nanos, 'Disruptive ...') are AreaCastNano; in the local data BOTH are cast by the ENGINEER (VisualProfession == 3) with no NPCFamily and no Breed == 7 requirement, so on the evidence they are the Engineer's own team and hostile auras and they belong in engineer-buffs.json, not here. The ONLY genuinely pet-emitted aura in the data is the 'Intrusive Aura of ...' line (strain 288), which does carry Breed == 7 and is therefore covered above. EngineerSpecialAttackAbsorber (strain 286, the 'Sloughing ...' line, 14 nanos) is likewise self/team - ChangeVariable on SpecialAttackShield(517) or TeamCastNano, with no pet requirement - so it is not a pet mechanic either.
- TRADESKILL BOT UPGRADES are an Engineer-only system with no MP equivalent, and they are ITEMS, not nanos, so none appear in engineer-nanos.json. Permanent: Trimmer - Positive Aggressive-Defensive (sets the bot's agg/def bar, up to 100% agg at QL200 - faster attacks, worse evades), Trimmer - Negative Aggressive-Defensive (the reverse), Trimmer - Increase Aggressiveness (adds taunt to the bot's hits, about +90 at QL200; built from XU-11 Serum + Chemical Impact Injector + Trimmer Casing with Mechanical Engineering and a little Chemistry, QL 30-200). Temporary: the four 'Divert Energy to ...' trimmers - Offense / Defense / Hitpoints / Avoidance - each trading one stat for another (a QL200 Divert Energy to Offense gives +40 AAO and +40 damage while cutting ACs by 1000). Damage-type trimmers (fire / energy / cold) change the bot's damage type for 10 minutes, cut its hitpoints significantly, and lock Mechanical Engineering for 60 minutes, so once per hour. Trimmer - Improve Actuators is an emergency combined heal-and-damage boost (heals the bot up to about 6k HP and boosts damage for 600 seconds, locking Mechanical Engineering for an hour; made from Smelly Liquid, which drops from Alien Generals, plus a Trimmer Casing). Android NCU Upgrade adds NCU to the bot (about +110 at QL200) at the cost of about 400 AC.
- On the wire the Engineer bot is an ordinary pet: AOSharp PetType.Attack (0xA) arrives in the pet's SimpleCharFullUpdate (SimpleNpcInfo.PetType) and again in PetToMasterMessage.AttachNotificationValue, and PetType.cs records this as confirmed across Engineer pets in the local sniffs/ and captures/ sets. The nano-side family id for an Engineer bot is NpcClan 95 = EngineerAttackPet (AOSharp CharacterFlags.cs) - that is what every pet-targeted Engineer nano checks. The MP's equivalents are 96 (heal), 97 (attack) and 98 (mezz).
- Bot rule for AOBuddy: pick the strongest castable bot by filtering engineer-nanos.json category 'Pets - Robot' and taking the highest MaterialCreation/SpaceTime requirement the character's LIVE stats satisfy at 100%, and also check the live Cash stat against that summon's credit cost before casting. Do NOT hardcode a per-level table. After casting, keep watching MC/TS: if either falls below 80% of that nano's requirement the bot silently stops obeying and drops to behind, with no packet saying so beyond the refusal text.

---

## Unverified / flags

- Whether the Engineer bot can be GIVEN A WEAPON: NOT verified, and therefore deliberately left out of the body of this file. Neither the AODB Engineer Guide, AODB Engineer:Tips and Tricks, nor AO-Universe's Engineer Guide MKIII says a bot can be handed a weapon, and no local item or nano evidence was found.
- The 'robot shell' step. The AODB Engineer Guide describes summoning as two steps - cast the nano to 'create a robot shell', then right-click the shell to spawn the robot - which is consistent with the function literally being named SpawnItem (53064). BUT OmniCell's server source (Pets.cs) states all 476 SpawnItem calls take the same three arguments as SummonPet and summon the pet directly, and the local sniff-confirmed PetType data shows Engineer bots arriving as ordinary pets. Local data cannot settle whether the current client still has an intermediate shell ITEM. Verify on the wire before building a summon routine that expects to find and Use() a shell.
- Whether the level-100 'pet dog' (the documented Engineer second pet) IS the Predator M-30 line. The correlation is strong - the Predator entry nano is the only bot summon gated at character level 100 - but no cited source names the Predator line as the second/dog pet, and the AODB Engineer nano page does not characterise it that way. Not asserted.
- Which nano the AODB wiki row 'Predator M-30 - Scheol Garden' refers to. Local data has two nanos in that name family: 'Prototype Predator M-30' (223325, character level 100) and 'Predator M-30' (301855, character level 150). The acquisition note is attached to the exact name match (301855); 223325's source is left unknown.
- Nano SOURCES for the 75 non-Shadowlands bots (11 Automaton, 11 Android, 11 Gladiatorbot, 11 Guardbot, 12 Warbot, 12 Warmachine, 3 Wardroid, 4 Slayerdroid), for the Ravening M-60 (275815) and for the Jamming Tower (274369) are NOT recorded here - the AODB Engineer nano page lists only the Shadowlands and Unique entries, and auno / aoitems / fandom block automation.
- What the Jamming Tower (274369) actually does. Its nano's only function is SpawnItem[EGJA,1,0]; there is no local record of the tower's own behaviour and no cited guide covers it.
- Bot-heal availability: the AODB Engineer Guide says 'All Bot heals are mission reward only' and that the Sympathetic and Disruptive lines are not sold in shops. Not cross-checked against a second source, and not checked against nano-crystal criteria in items.ocp.
- 'Slayerdroid Transference' (31593) - VisualProfession == 3, MonsterData == 0, +400 Martial Arts, +50 MultiMelee / Brawl / FastAttack, +150 Projectile/Melee/Energy AC, ChangeVariable[Scale(360), 50]. Name and effects suggest something Slayerdroid-related, but it carries NO NPCFamily and NO Breed == 7 requirement, so local data does not show it as pet-targeted. Left out of petBuffs; identify it before using it.
- No PetWarp (NanoLine 1019) nano exists in the Engineer-castable extract. The Engineer's warps are Beacon Warp (154914, SummonPlayer) and Team Beacon Warp (154913, SummonTeamMates) - player warps, strain 293 BeaconWarp, with an ExpansionPlayfield == 0 requirement. A reading of AODB's Engineer:Tips and Tricks as touching on 'pet warping' is NOT confirmed and no pet-warp nano is in the data.
- Exact trimmer QLs, stat magnitudes and component lists beyond the figures quoted (QL200 numbers, 5- and 10-minute durations, the 60-minute Mechanical Engineering lock) come from AODB / AO-Universe guide text, not from local item data. items.ocp was not queried for trimmer templates in this pass - do that before the bot ever tries to build or use one.
- Character-level breakpoints for the 75 non-Shadowlands bots: engineer-nanos.json reports minLevel 0 for all of them. The real gate is the MC/TS requirement plus the credit cost. The 'bot lvl N' figure in each progression note is the SpawnItem template-level argument from local data - that is the BOT's level, not a character-level requirement.
- Whether any community guide's talk of 'the bot's aura' means the strain 227/236 Engineer auras rather than the strain 288 Intrusive Aura line. Local data is unambiguous that 227/236 have no pet requirement; the guide wording was not reconciled against that.
- Relative bot strength within a family beyond the template-level ordering (damage, HP, AC of each bot template) is not in nanos.ocp - the bot's own stats live in the mob template, which was not read in this pass.

---

## Sources

- LOCAL: AOBuddy/GameData/profiles/engineer-nanos.json (client 18.8.50_EP1 extraction of nanos.ocp via OmniCell.Core NanoLoader; names from itemnames.sql) - the source of every id, name, minLevel, castReq, SpawnItem template code, bot level and credit cost in this file.
- LOCAL: E:/Funcom/attic/extracted-client-data/itemnames.sql - all 137 ids used from the four pet categories were re-verified name-for-name against this table in this pass: 137 of 137 exact match, 0 mismatches, 0 missing.
- LOCAL: AOSharp.Common/GameData/CharacterFlags.cs - NpcClan.EngineerAttackPet = 95 (vs MPHealPets 96 / MPAttackPets 97 / MPMezzPets 98).
- LOCAL: AOSharp.Common/GameData/PetType.cs - PetType.Attack = 0xA, documented as confirmed against Engineer pets in the local sniffs/ and captures/ sets; the pet role arrives in SimpleCharFullUpdate (SimpleNpcInfo.PetType) and in PetToMasterMessage.AttachNotificationValue.
- LOCAL: AOSharp.Common/GameData/Breed.cs (HumanMonster = 7); NanoEnums.cs (strain names: Snare 145, Root 146, Mezz 147, CharmOther 202, MPPetInitiativeBuffs 217, PetShortTermDamageBuffs 225, PetSnare_RootResistanceBuff 285, EngineerSpecialAttackAbsorber 286, EngineerPetAOESnareBuff 288, EngineerAuras 227, EngineerDebuffAuras 236, EngineerMiniaturization 811, PetDefensiveNanos 816, SnareandMezzRemoval 1013, Charm_Short 1022, PetDebuffCleanse 1047); Stat.cs (Health 27, Cash 61, Scale 360, SpecialAttackShield 517).
- LOCAL: OmniCell/Server/ZoneEngine/Core/Pets.cs - documents that SummonPet (53167) and SpawnItem (53064) are called identically (4-char template hash, level, flag) and names ENAU / ENGA / ENGU / ENWA as Engineer automaton and guard templates.
- AO-Universe - Basic Pet Commands Guide (https://www.ao-universe.com/guides/classic-ao/gameplay-guides-6/basic-pet-commands-guide) - the command list, /pet chat, named-pet scoping, and the 'guard after zoning, issue behind' convention.
- AO-Universe - Over-Equipped (https://www.ao-universe.com/guides/classic-ao/gameplay-guides-6/over-equipped) - the OE formula, the 20% threshold, the weapon damage tiers, and the pet-specific rule that OE pets finish their current fight then drop to behind and refuse commands.
- AODB Wiki - Engineer Guide (http://wiki.aodb.us/wiki/Engineer_Guide) - bot shell summoning, bot persistence and the 2-minute disconnect grace, the 50 m command range, /pet rename needing 4+ characters, the level-100 second pet and both-pets command behaviour, the 0.8 control-skill formula with the Slayerdroid Guardian 874/700 example, and 'all bot heals are mission reward only'.
- AODB Wiki - Engineer:Tips and Tricks (https://wiki.aodb.us/wiki/Engineer:Tips_and_Tricks) - the 'Illegal command. Master not recognised.' over-equip refusal message; Trimmer - Divert Energy to Offense (QL200: +40 AAO, +40 damage, -1000 AC); Trimmer - Increase Aggressiveness (about +90 at QL200); Trimmer - Positive Aggressive-Defensive.
- AODB Wiki - Engineer Nano Programs (http://wiki.aodb.us/wiki/Engineer_Nano_Programs) - Shadowlands bot-nano acquisition (Scheol / Adonis / Penumbra / Inferno Gardens and Sanctuaries, Pandemonium vendors) and Miniaturization / Lesser Miniaturization as Scheol quest rewards.
- AODB Wiki - Trimmer - Improve Actuators (http://wiki.aodb.us/wiki/Trimmer_-_Improve_Actuators) and Trimmer - Increase Aggressiveness (http://wiki.aodb.us/wiki/Trimmer_-_Increase_Aggressiveness) - trimmer effects, durations, skill locks and components.
- AO-Universe - Engineer Guide MKIII 1/3 and 2/3 (https://www.ao-universe.com/guides/classic-ao/profession-guides/engineer-guide-mkiii-13 and .../engineer-guide-mkiii-23) - the trimmer roster (permanent agg/def, temporary Divert Energy, damage-type, Improve Actuators), Android NCU Upgrade, the 50 m range, and OE behind-mode.
- AO-Universe - Aggression Trimmer (https://www.ao-universe.com/guides/classic-ao/tradeskill-guides-6/gadgets-5/aggression-trimmer) and Damage Modifier Trimmers (https://www.ao-universe.com/guides/alien-invasion/tradeskill-guides-3/gadgets-2/damage-modifier-trimmers) - trimmer build components and damage-type trimmer mechanics.
- AODB Wiki - Over Equipped (http://wiki.aodb.us/wiki/Over_Equipped) and Helpbot (http://wiki.aodb.us/wiki/Helpbot) - the OE check is a chat-bot tell ('/tell Helpbot oe <skill>'); Helpbot is down and Aquest now provides it. There is no client-side /oe command.
- NOT USED (blocked to automation per the playbook): auno.org, aoitems.com, anarchyonline.fandom.com. No claim in this file comes from them.
