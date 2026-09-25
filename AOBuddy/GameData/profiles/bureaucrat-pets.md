# Bureaucrat (profession 8) - Pets

Research for AOBuddy10 pet AI. **Every id, name, spawn level, skill requirement, target ceiling and effect
string in this file is copied out of `bureaucrat-nanos.json`** (343 Bureaucrat-castable nanos extracted from
`nanos.ocp`, client 18.8.50_EP1), by script - nothing was transcribed by hand. All 145 id/name pairs were
re-checked against `itemnames.sql` with `tools/verify-profile-ids.py`: **145 of 145 exact match, 0 mismatch,
0 not-in-db.** Mechanics come from the cited AO-Universe / AODB-wiki pages. Anything not traceable to the
data or a source is listed under **Unverified** and is not stated as fact anywhere else in the file.

> **Coverage: 145 of 145 Bureaucrat pet nanos.** 65 of 65 bot summons, 37 of 37 charms, 2 of 2 Associates,
> 1 of 1 Means Test Pet, 37 of 37 pet buffs/utility, 3 of 3 pet repairs. A sweep of the other 15 categories
> in `bureaucrat-nanos.json` for `SpawnItem` / `SummonPet` / `CharmNpc` / `petTargeted` / `Breed == 7` /
> `NPCFamily == 95` found **0** further pet nanos. No sampling anywhere in this file.

---

## How the Bureaucrat differs from the Meta-Physicist and the Engineer (read this first)

| | Meta-Physicist | Engineer | **Bureaucrat** |
|---|---|---|---|
| Pet sources | summon only | summon only | **summon AND charm** |
| Pets at once | 3 (attack/heal/mezz) | up to 2 | **3: bot + 2 charms** (AO-Universe: 1 long + 1 short, or 2 short) |
| Summon function | `SummonPet` | `SpawnItem` | **`SpawnItem` for all 65 bots**, `SummonPet` for only 3 late nanos |
| Summon skills | TS + MC/BioMet/MatMet | MC + TS (equal) | **MC + TS (equal) for bots; PM + SI + BM (equal) for charms - two stacks** |
| Credit cost | none | 79 of 90 robots | **none - all 65 bots are free** |
| Bot ladder shape | n/a | 15 irregular families (1-12 each) | **9 families x 7 grades = a complete 63-cell matrix** |
| Charm nanos | 0 | 0 | **37** (`CharmOther`, nano strain 202) |
| Heal pet | 10 summons | none (7 repair nanos) | **none - 3 repair nanos, and the Heal SLOT holds a short charm** |
| Pet clan | NpcClan 96/97/98 | NpcClan 95 `EngineerAttackPet` | **NpcClan 95 - the same clan as an Engineer robot** |
| Pet-buff targeting | one pet kind | one pet kind | **split: Gallant Hero -> bot only, Gallant Slave -> charmed NPC only** |
| Pet initiative line | 4 nanos (strain 217) | 1 nano | **0** - it has a pet TAUNT line instead (strain 232, 3 nanos) |
| Damage transfer | 2 (`DamageToPet` 1024) | - | **0** - the nearest thing is `Take the Bullet` (267917) |
| Team-gated pet nanos | 0 | 0 | **11 'Team Empowered' charms** (`NumberOfTeamMembers > 1`) |

---

## Summary

The Bureaucrat (profession 8) is the only profession in the game that both SUMMONS a pet and TAKES one: the AODB wiki says a Crat 'can have both a "summoned" droid and a "Charmed" monster ally', and AO-Universe's Bureaucrat Guide puts the ceiling at three - 'It is possible to have 1 long and 1 short charm, or you can have 2 short charms. You can not use your Long charm if you already have a pet on short charm.' The local client data backs that up exactly: 105 of the Crat's 343 castable nanos are pet SUMMONS, and they split into two completely separate systems funded by two completely separate skill stacks. (1) THE BOT - 65 SpawnItem nanos, paid for with Matter Creation (130) + Time & Space (131) at identical values, arranged as a near-perfect matrix of NINE families (BUWO Worker, BUHE Helper, BUAT Attendant, BUAS Assistant, BUAI Aide, BUSC Secretary, BUAD Administrator, BUMI Minion, BUBO Bodyguard) x SEVEN quality grades (Basic, Limited, Faithful, Advanced, Supervisor-Grade, Executive-Grade, Director-Grade) = 63, topped by Corporate Guardian (235386, spawn level 205, Shadowlands + Specialization bit 8) and CEO Guardian (273300, spawn level 215, NanoFocusLevel bit 64). Director-Grade Bodyguard (46391, spawn level 200) is the froob ceiling. Unlike the Engineer's robots, not one of the 65 costs credits. (2) THE CHARM - 37 CharmOther nanos (nano strain 202) paid for with Psychological Modification (129) + Sensory Improvement (122) + Biological Metamorphosis (128), again at identical values. They form two parallel lines that the data separates cleanly: 26 nanos set PetType 12 and carry 'Pets op66 7001' (AO-Universe's LONG charm, Temporary Glamor -> The Voice of God, of which 11 are 'Team Empowered' Shadowlands team variants) and 11 set PetType 11 and carry 'Pets op66 7002' (the SHORT charm, Lesser Pheromone Control -> The Choir Fantastic). AOSharp's PetType enum, confirmed against sniffs, reads 11 as the Heal slot and 12 as the Support slot - which is mechanically how a Crat holds a bot and two charms at the same time. Every charm carries a 'Psychic < N' ceiling that rises with the tier (69 at Temporary Glamor, 551 at the top) and writes the charmed pet's own keep-alive requirements into PetReq1/2/3. On top of that sit three true SummonPet pets - Carlita Desposito (293899, L160, self-scaling to template 215), Carlo Pinnetti (258580, L220) and the 20 x SummonPet 'Means Test Pet' (258222, L220) - 40 pet buffs and repairs, and a pet-buff library that is itself split by pet KIND: Gallant Hero (9 nanos, NPCFamily == 95) only works on the bot, Gallant Slave (9 nanos, HasRunningNanoLine 202 + a Level ceiling) only works on a charmed NPC. There is no Bureaucrat heal pet and no Bureaucrat pet-initiative line; the bot is repaired by three nanos the Crat casts itself. Every id, name, requirement and effect below is copied out of bureaucrat-nanos.json (client 18.8.50_EP1 nanos.ocp extraction); mechanics come from the cited sources; anything not traceable is under _unverified.

---

## The complete bot ladder - 65 of 65

`Spawn lvl` is the **SpawnItem template-level argument** - the BOT's level. It is **not** a character-level
requirement: 63 of the 65 bot summons carry no `Level` requirement at all (only Corporate Guardian does,
`Level > 204`). It is nonetheless the correct ordering key, because it rises monotonically through the seven
quality grades in every one of the nine families. `MC/TS` is Matter Creation / Time & Space, which are
required at **exactly the same value** on all 65. `Char lvl` is the explicit `Level` requirement where one
exists. No Bureaucrat bot summon costs credits.

### Attack bot - Worker family (BUWO)  <sub>`SpawnItem BUWO`</sub> - 7 nanos

The first bot family and the whole spawn-level 2-14 band. Seven nanos, Basic -> Director-Grade, MC/TS 6 -> 69. 'Basic Worker-Droid' (46397) needs MC/TS 6, so it is castable within the first few levels.

*Seven quality grades in fixed order Basic < Limited < Faithful < Advanced < Supervisor-Grade < Executive-Grade < Director-Grade - an ordering taken from the SpawnItem level argument, which rises monotonically through those seven names in every one of the nine families. Cast the highest grade your MC/TS allows; the new bot replaces the one you have. Matter Creation and Time & Space are required at exactly the same value on all 65 bot summons.*

| Grade | Nano | id | Spawn lvl | MC/TS | Char lvl | Gate |
|---|---|---|---|---|---|---|
| Basic | Basic Worker-Droid | 46397 | **2** | 6 |  |  |
| Limited | Limited Worker-Droid | 46362 | **3** | 21 |  |  |
| Faithful | Faithful Worker-Droid | 46363 | **6** | 34 |  |  |
| Advanced | Advanced Worker | 46399 | **8** | 43 |  |  |
| Supervisor-Grade | Supervisor-Grade Worker-Droid | 46351 | **10** | 52 |  |  |
| Executive-Grade | Executive-Grade Worker-Droid | 46385 | **12** | 60 |  |  |
| Director-Grade | Director-Grade Worker-Droid | 46386 | **14** | 69 |  |  |

### Attack bot - Helper family (BUHE)  <sub>`SpawnItem BUHE`</sub> - 7 nanos

Spawn levels 16-28, MC/TS 78 -> 129. Note the client's own naming drift inside this family: the Limited grade is 'Limited Helper-Bot' (46359), not '-Droid', and the Advanced grade is plain 'Advanced Helper' (46411).

*Seven quality grades in fixed order Basic < Limited < Faithful < Advanced < Supervisor-Grade < Executive-Grade < Director-Grade - an ordering taken from the SpawnItem level argument, which rises monotonically through those seven names in every one of the nine families. Cast the highest grade your MC/TS allows; the new bot replaces the one you have. Matter Creation and Time & Space are required at exactly the same value on all 65 bot summons.*

| Grade | Nano | id | Spawn lvl | MC/TS | Char lvl | Gate |
|---|---|---|---|---|---|---|
| Basic | Basic Helper-Droid | 46405 | **16** | 78 |  |  |
| Limited | Limited Helper-Bot | 46359 | **18** | 86 |  |  |
| Faithful | Faithful Helper-Droid | 46374 | **20** | 95 |  |  |
| Advanced | Advanced Helper | 46411 | **22** | 103 |  |  |
| Supervisor-Grade | Supervisor-Grade Helper-Droid | 46357 | **24** | 111 |  |  |
| Executive-Grade | Executive-Grade Helper-Droid | 46382 | **26** | 120 |  |  |
| Director-Grade | Director-Grade Helper-Droid | 46392 | **28** | 129 |  |  |

### Attack bot - Attendant family (BUAT)  <sub>`SpawnItem BUAT`</sub> - 7 nanos

Spawn levels 30-48, MC/TS 138 -> 211. The grade steps are a flat +3 spawn levels each, the first family where the grade ladder becomes perfectly regular.

*Seven quality grades in fixed order Basic < Limited < Faithful < Advanced < Supervisor-Grade < Executive-Grade < Director-Grade - an ordering taken from the SpawnItem level argument, which rises monotonically through those seven names in every one of the nine families. Cast the highest grade your MC/TS allows; the new bot replaces the one you have. Matter Creation and Time & Space are required at exactly the same value on all 65 bot summons.*

| Grade | Nano | id | Spawn lvl | MC/TS | Char lvl | Gate |
|---|---|---|---|---|---|---|
| Basic | Basic Attendant-Droid | 46403 | **30** | 138 |  |  |
| Limited | Limited Attendant-Droid | 46367 | **33** | 150 |  |  |
| Faithful | Faithful Attendant-Droid | 46372 | **36** | 162 |  |  |
| Advanced | Advanced Attendant-Droid | 46409 | **39** | 174 |  |  |
| Supervisor-Grade | Supervisor-Grade Attendant-Droid | 46355 | **42** | 187 |  |  |
| Executive-Grade | Executive-Grade Attendant-Droid | 46380 | **45** | 199 |  |  |
| Director-Grade | Director-Grade Attendant-Droid | 46390 | **48** | 211 |  |  |

### Attack bot - Assistant family (BUAS)  <sub>`SpawnItem BUAS`</sub> - 7 nanos

Spawn levels 51-69, MC/TS 225 -> 320. Still +3 spawn levels per grade.

*Seven quality grades in fixed order Basic < Limited < Faithful < Advanced < Supervisor-Grade < Executive-Grade < Director-Grade - an ordering taken from the SpawnItem level argument, which rises monotonically through those seven names in every one of the nine families. Cast the highest grade your MC/TS allows; the new bot replaces the one you have. Matter Creation and Time & Space are required at exactly the same value on all 65 bot summons.*

| Grade | Nano | id | Spawn lvl | MC/TS | Char lvl | Gate |
|---|---|---|---|---|---|---|
| Basic | Basic Assistant-Droid | 46402 | **51** | 225 |  |  |
| Limited | Limited Assistant-Droid | 46366 | **54** | 240 |  |  |
| Faithful | Faithful Assistant-Droid | 46371 | **57** | 256 |  |  |
| Advanced | Advanced Assistant-Droid | 46408 | **60** | 272 |  |  |
| Supervisor-Grade | Supervisor-Grade Assistant-Droid | 46354 | **63** | 288 |  |  |
| Executive-Grade | Executive-Grade Assistant-Droid | 46379 | **66** | 304 |  |  |
| Director-Grade | Director-Grade Assistant-Droid | 46389 | **69** | 320 |  |  |

### Attack bot - Aide family (BUAI)  <sub>`SpawnItem BUAI`</sub> - 7 nanos

Spawn levels 72-91, MC/TS 335 -> 427. +3 per grade except the last step (Executive-Grade 87 -> Director-Grade 91, +4).

*Seven quality grades in fixed order Basic < Limited < Faithful < Advanced < Supervisor-Grade < Executive-Grade < Director-Grade - an ordering taken from the SpawnItem level argument, which rises monotonically through those seven names in every one of the nine families. Cast the highest grade your MC/TS allows; the new bot replaces the one you have. Matter Creation and Time & Space are required at exactly the same value on all 65 bot summons.*

| Grade | Nano | id | Spawn lvl | MC/TS | Char lvl | Gate |
|---|---|---|---|---|---|---|
| Basic | Basic Aide-Droid | 46401 | **72** | 335 |  |  |
| Limited | Limited Aide-Droid | 46365 | **75** | 349 |  |  |
| Faithful | Faithful Aide-Droid | 46370 | **78** | 364 |  |  |
| Advanced | Advanced Aide-Droid | 46407 | **81** | 378 |  |  |
| Supervisor-Grade | Supervisor-Grade Aide-Droid | 46353 | **84** | 393 |  |  |
| Executive-Grade | Executive-Grade Aide-Droid | 46378 | **87** | 407 |  |  |
| Director-Grade | Director-Grade Aide-Droid | 46388 | **91** | 427 |  |  |

### Attack bot - Secretary family (BUSC)  <sub>`SpawnItem BUSC`</sub> - 7 nanos

Spawn levels 95-119, MC/TS 449 -> 573. The step widens to +4 spawn levels per grade.

*Seven quality grades in fixed order Basic < Limited < Faithful < Advanced < Supervisor-Grade < Executive-Grade < Director-Grade - an ordering taken from the SpawnItem level argument, which rises monotonically through those seven names in every one of the nine families. Cast the highest grade your MC/TS allows; the new bot replaces the one you have. Matter Creation and Time & Space are required at exactly the same value on all 65 bot summons.*

| Grade | Nano | id | Spawn lvl | MC/TS | Char lvl | Gate |
|---|---|---|---|---|---|---|
| Basic | Basic Secretary-Droid | 46396 | **95** | 449 |  |  |
| Limited | Limited Secretary-Droid | 46361 | **99** | 472 |  |  |
| Faithful | Faithful Secretary-Droid | 46376 | **103** | 492 |  |  |
| Advanced | Advanced Secretary-Droid | 46398 | **107** | 512 |  |  |
| Supervisor-Grade | Supervisor-Grade Secretary-Droid | 46350 | **111** | 532 |  |  |
| Executive-Grade | Executive-Grade Secretary-Droid | 46384 | **115** | 553 |  |  |
| Director-Grade | Director-Grade Secretary-Droid | 46394 | **119** | 573 |  |  |

### Attack bot - Administrator family (BUAD)  <sub>`SpawnItem BUAD`</sub> - 7 nanos

Spawn levels 123-147, MC/TS 593 -> 653. The MC/TS cost per grade collapses here - the whole family spans only 60 skill points for 24 spawn levels, so upgrading inside it is cheap.

*Seven quality grades in fixed order Basic < Limited < Faithful < Advanced < Supervisor-Grade < Executive-Grade < Director-Grade - an ordering taken from the SpawnItem level argument, which rises monotonically through those seven names in every one of the nine families. Cast the highest grade your MC/TS allows; the new bot replaces the one you have. Matter Creation and Time & Space are required at exactly the same value on all 65 bot summons.*

| Grade | Nano | id | Spawn lvl | MC/TS | Char lvl | Gate |
|---|---|---|---|---|---|---|
| Basic | Basic Administrator | 46400 | **123** | 593 |  |  |
| Limited | Limited Administrator-Droid | 46364 | **127** | 609 |  |  |
| Faithful | Faithful Administrator-Droid | 46369 | **131** | 619 |  |  |
| Advanced | Advanced Administrator-Droid | 46406 | **135** | 627 |  |  |
| Supervisor-Grade | Supervisor-Grade Administrator-Droid | 46352 | **139** | 636 |  |  |
| Executive-Grade | Executive-Grade Administrator-Droid | 46377 | **143** | 645 |  |  |
| Director-Grade | Director-Grade Administrator-Droid | 46387 | **147** | 653 |  |  |

### Attack bot - Minion family (BUMI)  <sub>`SpawnItem BUMI`</sub> - 7 nanos

Spawn levels 151-175, MC/TS 661 -> 722. Same flat skill curve as the Administrator family.

*Seven quality grades in fixed order Basic < Limited < Faithful < Advanced < Supervisor-Grade < Executive-Grade < Director-Grade - an ordering taken from the SpawnItem level argument, which rises monotonically through those seven names in every one of the nine families. Cast the highest grade your MC/TS allows; the new bot replaces the one you have. Matter Creation and Time & Space are required at exactly the same value on all 65 bot summons.*

| Grade | Nano | id | Spawn lvl | MC/TS | Char lvl | Gate |
|---|---|---|---|---|---|---|
| Basic | Basic Minion | 46395 | **151** | 661 |  |  |
| Limited | Limited Minion | 46360 | **155** | 669 |  |  |
| Faithful | Faithful Minion | 46375 | **159** | 677 |  |  |
| Advanced | Advanced Minion | 46412 | **163** | 688 |  |  |
| Supervisor-Grade | Supervisor-Grade Minion | 46358 | **167** | 699 |  |  |
| Executive-Grade | Executive-Grade Minion | 46383 | **171** | 710 |  |  |
| Director-Grade | Director-Grade Minion | 46393 | **175** | 722 |  |  |

### Attack bot - Bodyguard family (BUBO)  <sub>`SpawnItem BUBO`</sub> - 7 nanos

The top froob family: spawn levels 179-200, MC/TS 736 -> 786. Director-Grade Bodyguard (46391, spawn level 200, MC/TS 786) is the highest bot in the game that needs no expansion, no Specialization and no nano focus - the froob ceiling.

*Seven quality grades in fixed order Basic < Limited < Faithful < Advanced < Supervisor-Grade < Executive-Grade < Director-Grade - an ordering taken from the SpawnItem level argument, which rises monotonically through those seven names in every one of the nine families. Cast the highest grade your MC/TS allows; the new bot replaces the one you have. Matter Creation and Time & Space are required at exactly the same value on all 65 bot summons.*

| Grade | Nano | id | Spawn lvl | MC/TS | Char lvl | Gate |
|---|---|---|---|---|---|---|
| Basic | Basic Bodyguard | 46404 | **179** | 736 |  |  |
| Limited | Limited Bodyguard | 46368 | **183** | 746 |  |  |
| Faithful | Faithful Bodyguard | 46373 | **187** | 755 |  |  |
| Advanced | Advanced Bodyguard | 46410 | **191** | 765 |  |  |
| Supervisor-Grade | Supervisor-Grade Bodyguard | 46356 | **194** | 771 |  |  |
| Executive-Grade | Executive-Grade Bodyguard | 46381 | **197** | 778 |  |  |
| Director-Grade | Director-Grade Bodyguard | 46391 | **200** | 786 |  |  |

### Attack bot - Corporate Guardian (Shadowlands)  <sub>`SpawnItem BRBD`</sub> - 1 nano

A single Shadowlands nano that jumps the bot from spawn level 200 to 205 - and it costs a huge skill step to do it: MC/TS 1,226 against the Director-Grade Bodyguard's 786, a +440 jump for +5 spawn levels. It is also the only bot summon in the data with an explicit character-level requirement (Level > 204).

*Requires Shadowlands (Expansion op22 2), Specialization bit 8 and character level 205. Template code changes from the BU** froob prefix to BR**.*

| Grade | Nano | id | Spawn lvl | MC/TS | Char lvl | Gate |
|---|---|---|---|---|---|---|
| n/a | Corporate Guardian | 235386 | **205** | 1226 | 205 | Expansion op22 2, Specialization op22 8 |

### Attack bot - CEO Guardian (nano-focus gated)  <sub>`SpawnItem BRBY`</sub> - 1 nano

The best bot in the data: spawn level 215, MC/TS 1,469. Unlike the Corporate Guardian it is NOT Shadowlands/Specialization gated - it carries NanoFocusLevel op22 64 instead, the same late nano-focus gate the Engineer's Ravening M-60 uses, and no character-level requirement at all.

*The gate is 'NanoFocusLevel op22 64' (a BitAnd on stat 355). What unlocks that bit is not derivable from the nano data - see _unverified.*

| Grade | Nano | id | Spawn lvl | MC/TS | Char lvl | Gate |
|---|---|---|---|---|---|---|
| n/a | CEO Guardian | 273300 | **215** | 1469 |  | NanoFocusLevel op22 64 |

**Grade order (taken from the spawn-level argument, not from the English):** Basic < Limited < Faithful < Advanced < Supervisor-Grade < Executive-Grade < Director-Grade. It holds in all nine families without a single exception.

**Family order by spawn level:** BUWO 2-14 -> BUHE 16-28 -> BUAT 30-48 -> BUAS 51-69 -> BUAI 72-91 -> BUSC 95-119 -> BUAD 123-147 -> BUMI 151-175 -> BUBO 179-200 -> BRBD 205 -> BRBY 215.

---

## Charm - 37 of 37

The Bureaucrat does not summon these: it takes an NPC that already exists. All 37 are nano strain **202**
(`CharmOther`) and all 37 need Psychological Modification, Sensory Improvement and Biological Metamorphosis
at **exactly the same value**, so the `Charm skill` column below is all three at once and is the ordering key.
`Target Psychic <` is the ceiling the nano places on what it can charm (see Unverified for why this is read
as a cap on the TARGET). `PetType` is the wire pet-slot value the nano writes with `ChangeVariable[512,N]`;
AOSharp `PetType.cs` reads 11 as the Heal slot and 12 as the Support slot.

### Charmed NPC - long charm line (PetType 12) - 15 nanos

The Bureaucrat's headline trick and the thing no other profession has as a permanent tool: it does not SUMMON this pet, it takes an NPC that already exists and makes it fight for you. Fifteen solo nanos from Temporary Glamor (99208, PM 108) up to The Voice of God (231010, PM 1,499). AO-Universe's Bureaucrat Guide calls this line the LONG charm ('The Voice of Truth ... long duration'). Every one of the fifteen carries 'Pets op66 7001' and sets PetType 12.

*Each charm does four things at once, all visible in the nano's own functions: CharmNpc[] takes the target; TauntNpc[N] dumps a tier-scaled taunt on it (1,223 at Temporary Glamor, 8,720 at Total Mental Domination and above, plus a flat TauntNpc[50000] on most tiers); Hit[27,-1,-1,94] pokes the target for 1 point; and a block of ChangeVariable calls writes the pet's OWN keep-alive requirement into PetReq1/2/3 = 129/122/128 (PM/SI/BM) with PetReqVal1/2/3 set to the caster's required skill value - the over-equip check for a charmed pet. The whole line is one nano strain (202), so a second charm of this line replaces the first. AO-Universe: 'You can not charm calmed mobs/bosses/uniques/guards', and the AODB wiki warns 'previously charmed enemies give no XP' and that the charm is 'a small but ticking time bomb that goes off when the Charm wears off (the former ally attacks)'.*

| Nano | id | Charm skill (PM=SI=BM) | Target Psychic < | PetType | Pets op66 | Char lvl | Gate |
|---|---|---|---|---|---|---|---|
| Temporary Glamor | 99208 | 108 | **69** | 12 | 7001 |  |  |
| Bend Will | 99206 | 166 | **99** | 12 | 7001 |  |  |
| Dominate Psyche | 99204 | 228 | **125** | 12 | 7001 |  |  |
| Solicit Support | 99202 | 303 | **159** | 12 | 7001 |  |  |
| Impose Will | 99201 | 401 | **201** | 12 | 7001 |  |  |
| Captivated Thoughts | 99199 | 505 | **243** | 12 | 7001 |  |  |
| Allure of Servitude | 99198 | 632 | **287** | 12 | 7001, 2001 |  |  |
| Insidious Beguilement | 99195 | 710 | **327** | 12 | 7001, 2001 |  |  |
| Inveigle Support | 99194 | 750 | **375** | 12 | 7001, 2001 |  |  |
| Total Mental Domination | 99192 | 826 | **455** | 12 | 7001, 2001 |  |  |
| The Voice of Truth | 231013 | 999 | **501** | 12 | 7001, 2001 | 195 | Specialization op22 4 |
| My Way | 231012 | 1199 | **551** | 12 | 7001, 2001 | 215 | Specialization op22 8 |
| Peer Pressure | 231009 | 1299 | **551** | 12 | 7001, 2001 | 215 | Specialization op22 8 |
| The Voice of One | 231008 | 1499 | **551** | 12 | 7001, 2001 | 216 | Specialization op22 8 |
| The Voice of God | 231010 | 1499 | **551** | 12 | 7001, 2001 | 220 | Specialization op22 8 |

### Charmed NPC - long charm line, Team Empowered variants (PetType 12) - 11 nanos

Eleven Shadowlands duplicates of the long-charm line, one per tier from Temporary Glamor up to The Voice of Truth, each requiring the SAME PM/SI/BM as the nano it mirrors but adding three gates: Shadowlands, a Specialization bit (1, 2 or 4 depending on tier) and NumberOfTeamMembers > 1. Two things change in the effect: the nano carries SIX CharmNpc[] functions instead of one, and the target's Psychic ceiling is RAISED - Bend Will stops at Psychic < 99, Team Empowered Bend Will reaches Psychic < 138; The Voice of Truth 501 -> 542. So the team version charms strictly harder targets than the solo version of the same tier.

*Nothing in the Meta-Physicist or Engineer data resembles this: there is no 'Team Empowered' anything in either class's pet set. What the six CharmNpc[] copies actually do (one charmed NPC shared with the team vs one per team member) is not derivable from the local data - see _unverified.*

| Nano | id | Charm skill (PM=SI=BM) | Target Psychic < | PetType | Pets op66 | Char lvl | Gate |
|---|---|---|---|---|---|---|---|
| Team Empowered Temporary Glamor | 230397 | 108 | **93** | 12 | 7001 |  | Expansion op22 2, Specialization op22 1, NumberOfTeamMembers > 1 |
| Team Empowered Bend Will | 230391 | 166 | **138** | 12 | 7001 |  | Expansion op22 2, Specialization op22 1, NumberOfTeamMembers > 1 |
| Team Empowered Dominate Psyche | 230392 | 228 | **183** | 12 | 7001 |  | Expansion op22 2, Specialization op22 1, NumberOfTeamMembers > 1 |
| Team Empowered Solicit Support | 230396 | 303 | **228** | 12 | 7001 |  | Expansion op22 2, Specialization op22 1, NumberOfTeamMembers > 1 |
| Team Empowered Impose Will | 230393 | 401 | **273** | 12 | 7001 |  | Expansion op22 2, Specialization op22 1, NumberOfTeamMembers > 1 |
| Team Empowered Captivated Thoughts | 229964 | 505 | **318** | 12 | 7001 |  | Expansion op22 2, Specialization op22 2, NumberOfTeamMembers > 1 |
| Team Empowered Allure of Servitude | 229961 | 632 | **363** | 12 | 7001, 2001 |  | Expansion op22 2, Specialization op22 2, NumberOfTeamMembers > 1 |
| Team Empowered Insidious Beguilement | 230394 | 710 | **408** | 12 | 7001, 2001 |  | Expansion op22 2, Specialization op22 2, NumberOfTeamMembers > 1 |
| Team Empowered Inveigle Support | 230395 | 750 | **453** | 12 | 7001, 2001 |  | Expansion op22 2, Specialization op22 2, NumberOfTeamMembers > 1 |
| Team Empowered Total Mental Domination | 230398 | 826 | **498** | 12 | 7001, 2001 |  | Expansion op22 2, Specialization op22 4, NumberOfTeamMembers > 1 |
| Team Empowered Voice of Truth | 231011 | 999 | **542** | 12 | 7001, 2001 | 195 | Specialization op22 4, NumberOfTeamMembers > 1 |

### Charmed NPC - short charm line (PetType 11) - 11 nanos

The second, parallel charm line - eleven nanos from Lesser Pheromone Control (99207, PM 137) to The Choir Fantastic (275009, PM 1,912). AO-Universe's Bureaucrat Guide calls this the SHORT charm ('Displace Thought Patterns ... short duration') and states: 'It is possible to have 1 long and 1 short charm, or you can have 2 short charms. You can not use your Long charm if you already have a pet on short charm.' Every one of the eleven carries 'Pets op66 7002' (against 7001 on the long line) and sets PetType 11 - a perfect 37-of-37 correlation between the two markers.

*Same four-part effect as the long line (CharmNpc + TauntNpc + Hit[27,-1,-1,94] + the PetReq/PetReqVal over-equip block), same nano strain 202, and for a given PM investment the short line reaches a LOWER target Psychic ceiling than the long line at first (Lesser Pheromone Control PM 137 -> Psychic < 83) but overtakes at the top (The Choir Fantastic PM 1,912 -> Psychic < 551, the same ceiling as The Voice of God). The Choir Fantastic is the only charm gated on NanoFocusLevel bit 64.*

| Nano | id | Charm skill (PM=SI=BM) | Target Psychic < | PetType | Pets op66 | Char lvl | Gate |
|---|---|---|---|---|---|---|---|
| Lesser Pheromone Control | 99207 | 137 | **83** | 11 | 7002 |  |  |
| Soft Siren Call | 99205 | 194 | **111** | 11 | 7002 |  |  |
| Personal Magnetism | 99203 | 257 | **143** | 11 | 7002 |  |  |
| Pheromone Control | 30086 | 363 | **185** | 11 | 7002 |  |  |
| Lesser Charismatic Rapture | 99200 | 455 | **223** | 11 | 7002 |  |  |
| Siren Call | 30092 | 571 | **265** | 11 | 7002 |  |  |
| Enrapturing Bondage | 99197 | 686 | **307** | 11 | 7002, 2001 |  |  |
| Temporary Allegiance | 99196 | 731 | **347** | 11 | 7002, 2001 |  |  |
| Charismatic Rapture | 99193 | 780 | **408** | 11 | 7002, 2001 |  |  |
| Displace Thought Patterns | 99209 | 870 | **455** | 11 | 7002, 2001 |  |  |
| The Choir Fantastic | 275009 | 1912 | **551** | 11 | 7002, 2001 |  | NanoFocusLevel op22 64 |

**Solo vs Team Empowered, same tier, same skill cost:** Temporary Glamor 69 -> 93 | Bend Will 99 -> 138 | Dominate Psyche 125 -> 183 | Solicit Support 159 -> 228 | Impose Will 201 -> 273 | Captivated Thoughts 243 -> 318 | Allure of Servitude 287 -> 363 | Insidious Beguilement 327 -> 408 | Inveigle Support 375 -> 453 | Total Mental Domination 455 -> 498 | The Voice of Truth 501 -> 542. The team version always reaches a higher target Psychic ceiling for the same PM/SI/BM.

---

## SummonPet pets - 3 of 3

### Mezz/Support pet - the two named Associates  <sub>`SupportPets`, strain 1017</sub>

The Bureaucrat's only true SummonPet support pets, and the only two nanos it shares a strain (1017 SupportPets) with the Meta-Physicist's mezz pets. Carlita Desposito (293899) is self-scaling: five SummonPet functions for template levels 175/200/205/210/215, so the nano picks the best the caster's skills allow. Carlo Pinnetti (258580) is a single template level 220 and needs MC/TS 1,399 at character level 220.

*Both carry the same 'Pets op66 7001' as the long-charm line, so they and a long charm compete for the same slot. AO-Universe's guide lists 'Carlo Pinetti' (their spelling) as a level 220 pet from Biodome raids; the client name is Carlo Pinnetti.*

| Nano | id | Char lvl | Detail |
|---|---|---|---|
| Carlita Desposito | 293899 | 160 | SummonPet template levels 175/200/205/210/215 (5 SummonPet functions in the nano); MC/ST 899/899; character Level > 159; gates: Shadowlands (Expansion op22 2); Specialization bit 4; Pets op66 7001, Pets op66 2001 |
| Carlo Pinnetti | 258580 | 220 | SummonPet template level 220 (1 SummonPet function in the nano); MC/ST 1399/1399; character Level > 219; gates: Shadowlands (Expansion op22 2); Specialization bit 8; Pets op66 7001, Pets op66 2001 |

### Attack pet - Means Test Pet  <sub>`NOSTACKING (SummonPet)`, strain 0</sub>

A single level-220 nano and the strangest entry in the Bureaucrat pet set: it is a SummonPet nano (not SpawnItem, so not a bot) that is paid for out of the CHARM skills, and its effect block repeats SummonPet template [0,220,-1] twenty times. It is the only Bureaucrat pet nano that requires all three charm skills above 2,000.

*Whether the twenty SummonPet functions mean twenty pets or twenty copies of one summon is not established from local data - the Meta-Physicist's heal pets list their template seven times for one pet, so repetition is not a count. See _unverified.*

| Nano | id | Char lvl | Detail |
|---|---|---|---|
| Means Test Pet | 258222 | 220 | SummonPet template level 220, listed 20 times; PM/SI/BM 2000/2000/2000; character Level > 219; gates: Specialization bit 4; Pets op66 7001 |

---

## Pet buffs, utility and repairs - 40 of 40

37 nanos from `Pet Buffs / Pet Utility` plus all 3 from `Pet Heals / Repair`. The `Targets` column is the
decisive one: `Breed == 7` is the playbook's encoding for *cast on your pet*, `NPCFamily == 95` narrows that
to the bot, and `HasRunningNanoLine 202` (printed as `Flags op92 202`) narrows it to a charmed NPC.

### Gallant Hero - bot damage/taunt burst (strain 225) - 9 nanos

| Nano | id | Char lvl | Effect | Targets | Skills |
|---|---|---|---|---|---|
| Gallant Hero: The Enraged Drone | 205287 | 25 | Add All Off. +51, Aggressiveness +10 on the bot | Breed == 7, NPCFamily == 95 | PsychologicalModification > 188, SensoryImprovement > 188, SpaceTime > 164 |
| Gallant Hero: The Aggravated Servant | 205291 | 50 | Add All Off. +121, Aggressiveness +14 on the bot | Breed == 7, NPCFamily == 95 | PsychologicalModification > 447, SensoryImprovement > 447, SpaceTime > 396 |
| Gallant Hero: The Infuriated Minion | 205289 | 50 | Add All Off. +88, Aggressiveness +12 on the bot | Breed == 7, NPCFamily == 95 | PsychologicalModification > 304, SensoryImprovement > 304, SpaceTime > 277 |
| Gallant Hero: The Indignant Flunky | 205293 | 75 | Add All Off. +156, Aggressiveness +16 on the bot | Breed == 7, NPCFamily == 95 | PsychologicalModification > 562, SensoryImprovement > 562, SpaceTime > 521 |
| Gallant Hero: The Angry Servitor | 205295 | 100 | Add All Off. +187, Aggressiveness +19 on the bot | Breed == 7, NPCFamily == 95 | PsychologicalModification > 696, SensoryImprovement > 696, SpaceTime > 636 |
| Gallant Hero: The Bitter Clerk | 205297 | 125 | Add All Off. +223, Aggressiveness +22 on the bot | Breed == 7, NPCFamily == 95 | PsychologicalModification > 826, SensoryImprovement > 826, SpaceTime > 750 |
| Gallant Hero: The Irate Attache | 205299 | 145 | Add All Off. +260, Aggressiveness +25 on the bot | Breed == 7, NPCFamily == 95 | PsychologicalModification > 886, SensoryImprovement > 886, SpaceTime > 812 |
| Gallant Hero: The Incensed Retainer | 205301 | 165 | Add All Off. +298, Aggressiveness +27 on the bot | Breed == 7, NPCFamily == 95 | PsychologicalModification > 958, SensoryImprovement > 958, SpaceTime > 886 |
| Gallant Hero: The Vengeful Butler | 205303 | 195 | Add All Off. +354, Aggressiveness +30 on the bot | Breed == 7, NPCFamily == 95 | PsychologicalModification > 1043, SensoryImprovement > 1043, SpaceTime > 956 |

### Gallant Slave - charmed-NPC buff (strain 225, Shadowlands) - 9 nanos

| Nano | id | Char lvl | Effect | Targets | Skills |
|---|---|---|---|---|---|
| Gallant Slave: The Enraged Slave | 230372 | 30 | Add All Off. +30, Aggressiveness +10, Max Health +200, Add All Def. +48, all five damage modifiers +12, plus an instant heal for 200 | Level < 23, Flags op92 202, NPCFamily > 0 | PsychologicalModification > 204, BiologicalMetamorphosis > 204, SpaceTime > 164 |
| Gallant Slave: The Infuriated Thrall | 230374 | 55 | Add All Off. +60, Aggressiveness +12, Max Health +600, Add All Def. +90, all five damage modifiers +24, plus an instant heal for 600 | Level < 43, Flags op92 202, NPCFamily > 0 | PsychologicalModification > 389, BiologicalMetamorphosis > 389, SpaceTime > 304 |
| Gallant Slave: The Aggravated Serf | 230376 | 65 | Add All Off. +121, Aggressiveness +14, Max Health +1200, Add All Def. +135, all five damage modifiers +30, plus an instant heal for 1200 | Level < 49, Flags op92 202, NPCFamily > 0 | PsychologicalModification > 489, BiologicalMetamorphosis > 489, SpaceTime > 434 |
| Gallant Slave: The Indignant Peon | 230378 | 80 | Add All Off. +156, Aggressiveness +16, Max Health +1350, Add All Def. +144, all five damage modifiers +40, plus an instant heal for 1350 | Level < 63, Flags op92 202, NPCFamily > 0 | PsychologicalModification > 619, BiologicalMetamorphosis > 619, SpaceTime > 569 |
| Gallant Slave: The Angry Drudge | 230380 | 110 | Add All Off. +187, Aggressiveness +19, Max Health +2500, Add All Def. +180, all five damage modifiers +60, plus an instant heal for 2500 | Level < 87, Flags op92 202, NPCFamily > 0 | PsychologicalModification > 759, BiologicalMetamorphosis > 759, SpaceTime > 699 |
| Gallant Slave: The Bitter Vassal | 230382 | 135 | Add All Off. +223, Aggressiveness +22, Max Health +3200, Add All Def. +220, all five damage modifiers +80, plus an instant heal for 3200 | Level < 107, Flags op92 202, NPCFamily > 0 | PsychologicalModification > 909, BiologicalMetamorphosis > 909, SpaceTime > 824 |
| Gallant Slave: The Irate Chattel | 230384 | 160 | Add All Off. +260, Aggressiveness +25, Max Health +3800, Add All Def. +250, all five damage modifiers +90, plus an instant heal for 3800 | Level < 127, Flags op92 202, NPCFamily > 0 | PsychologicalModification > 974, BiologicalMetamorphosis > 974, SpaceTime > 889 |
| Gallant Slave: The Incensed Subordinate | 230386 | 180 | Add All Off. +298, Aggressiveness +27, Max Health +5000, Add All Def. +300, all five damage modifiers +100, plus an instant heal for 5000 | Level < 143, Flags op92 202, NPCFamily > 0 | PsychologicalModification > 1049, BiologicalMetamorphosis > 1049, SpaceTime > 974 |
| Gallant Slave: The Vengeful Toiler | 230388 | 214 | Add All Off. +354, Aggressiveness +30, Max Health +6200, Add All Def. +378, all five damage modifiers +110, plus an instant heal for 6200 | Level < 171, Flags op92 202, NPCFamily > 0 | PsychologicalModification > 1149, BiologicalMetamorphosis > 1149, SpaceTime > 1049 |

### Exoskeleton Pulse - root/snare shortener on the pet - 5 nanos

| Nano | id | Char lvl | Effect | Targets | Skills |
|---|---|---|---|---|---|
| Minor Exoskeleton Pulse | 203850 | 25 | Cuts nano-strain 145 and 146 (root / snare) duration on the pet by 308; system text 'Your pet moves more freely.' | Breed == 7 | MaterialCreation > 194, SpaceTime > 194 |
| Lesser Exoskeleton Pulse | 203852 | 50 | Cuts nano-strain 145 and 146 (root / snare) duration on the pet by 437; system text 'Your pet moves more freely.' | Breed == 7 | MaterialCreation > 396, SpaceTime > 396 |
| Exoskeleton Pulse | 203855 | 100 | Cuts nano-strain 145 and 146 (root / snare) duration on the pet by 583; system text 'Your pet moves more freely.' | Breed == 7 | MaterialCreation > 617, SpaceTime > 617 |
| Superior Exoskeleton Pulse | 203857 | 145 | Cuts nano-strain 145 and 146 (root / snare) duration on the pet by 825; system text 'Your pet moves more freely.' | Breed == 7 | MaterialCreation > 807, SpaceTime > 807 |
| Greater Exoskeleton Pulse | 203859 | 185 | Cuts nano-strain 145 and 146 (root / snare) duration on the pet by 1074; system text 'Your pet moves more freely.' | Breed == 7 | MaterialCreation > 939, SpaceTime > 939 |

### Corporate Insurance Policy - bot defence + charm resistance (strain 816) - 3 nanos

| Nano | id | Char lvl | Effect | Targets | Skills |
|---|---|---|---|---|---|
| Lesser Corporate Insurance Policy | 267603 | 201 | Add All Def. +250, resists nano-strains 147 and 202 | Breed == 7, NPCFamily == 95 | PsychologicalModification > 1300, SensoryImprovement > 1300, SpaceTime > 900 |
| Corporate Insurance Policy | 267604 | 215 | Add All Def. +300, resists nano-strains 147 and 202 | Breed == 7, NPCFamily == 95 | PsychologicalModification > 1500, SensoryImprovement > 1500, SpaceTime > 1200 |
| Greater Corporate Insurance Policy | 267605 | 219 | Add All Def. +325, resists nano-strains 147 and 202, and +2000 to all eight ACs | Breed == 7, NPCFamily == 95 | PsychologicalModification > 1900, SensoryImprovement > 1900, SpaceTime > 1400 |

### Nanite Robot Protection - bot HP / nano resist (strain 817) - 3 nanos

| Nano | id | Char lvl | Effect | Targets | Skills |
|---|---|---|---|---|---|
| Basic Nanite Robot Protection | 267616 | 175 | Max Health +1000, Nano Resist +100, Add All Def. +100, 10% resist against nano-strain 145 (root) | Breed == 7, NPCFamily == 95, Flags op118 0 | PsychologicalModification > 1000, SensoryImprovement > 1000, SpaceTime > 750 |
| Lesser Nanite Robot Protection | 267615 | 201 | Max Health +1500, Nano Resist +250, Add All Def. +200, 20% resist against nano-strain 145 (root) | Breed == 7, NPCFamily == 95, Flags op118 0 | PsychologicalModification > 1400, SensoryImprovement > 1400, SpaceTime > 1000 |
| Nanite Robot Protection | 267612 | 219 | Max Health +3000, Nano Resist +500, Add All Def. +300, 25% resist against nano-strain 145 (root) | Breed == 7, NPCFamily == 95, Flags op118 0 | PsychologicalModification > 1800, SensoryImprovement > 1800, SpaceTime > 1400 |

### Pet taunt buffs (strain 232) - 3 nanos

| Nano | id | Char lvl | Effect | Targets | Skills |
|---|---|---|---|---|---|
| Mud Slinger | 155615 |  | Aggressiveness +31 on the bot | Breed == 7, NPCFamily == 95 | PsychologicalModification > 219, SpaceTime > 195 |
| Defamation 101 | 155616 |  | Aggressiveness +57 on the bot | Breed == 7, NPCFamily == 95 | PsychologicalModification > 608, SpaceTime > 547 |
| Character Assassin | 155617 |  | Aggressiveness +80 on the bot | Breed == 7, NPCFamily == 95 | PsychologicalModification > 820, SpaceTime > 744 |

### Take the Bullet - 1 nano

| Nano | id | Char lvl | Effect | Targets | Skills |
|---|---|---|---|---|---|
| Take the Bullet | 267917 | 215 | Hits the pet for 40,000 (twice) and casts 'Blast Shield' (267918) on it: +500 Add All Def., 75% resist and a removal of nano-strains 145 and 146, eight ChangeVariable[8000] entries, at the price of -3,000 to all six of the pet's nano skills | Breed == 7, NPCFamily == 95, Flags op118 0 | PsychologicalModification > 1800, SensoryImprovement > 1800 |

### Pet recovery - shared Engi/Crat/MP (strain 1022) - 2 nanos

| Nano | id | Char lvl | Effect | Targets | Skills |
|---|---|---|---|---|---|
| Pet Steal Back | 269907 | 100 | RemoveNanoStrain[202]: strips a CharmOther effect, i.e. takes your pet back after it was charmed away; system text 'Your manifestation moves more freely.' | Breed == 7 | MaterialCreation > 769, SpaceTime > 769 |
| Improved Pet Steal Back | 269908 | 215 | RemoveNanoStrain[202]: strips a CharmOther effect, i.e. takes your pet back after it was charmed away; system text 'Your manifestation moves more freely.' | Breed == 7 | MaterialCreation > 1399, SpaceTime > 1399 |

### Shared pet utility - Engi/Crat/MP - 2 nanos

| Nano | id | Char lvl | Effect | Targets | Skills |
|---|---|---|---|---|---|
| Pet Attention | 269869 |  | System text 'Your pets move more freely.' - the extracted function list carries no Modify/Remove entry beyond the text, so what it strips is not readable from this pack |  | MaterialCreation > 769, SpaceTime > 769 |
| Pet Cleanse | 269870 |  | System text 'Your pets move more freely.' - the extracted function list carries no Modify/Remove entry beyond the text, so what it strips is not readable from this pack |  | MaterialCreation > 1399, SpaceTime > 1399 |

### Droid repair - the Bureaucrat's entire pet-healing capability - 3 nanos

| Nano | id | Char lvl | Effect | Targets | Skills |
|---|---|---|---|---|---|
| Droid Repair | 30072 |  | Heals the pet for 399-707 | Breed == 7 | MaterialCreation > 299, SpaceTime > 299, BiologicalMetamorphosis > 343 |
| Droid Overhaul | 30071 |  | Heals the pet for 732-1339 | Breed == 7 | MaterialCreation > 583, SpaceTime > 583, BiologicalMetamorphosis > 650 |
| Thorough Overhaul | 116798 |  | Heals the pet for 873-1718 | Breed == 7 | MaterialCreation > 719, SpaceTime > 719, BiologicalMetamorphosis > 791 |

---

## Pet commands

The authoritative vocabulary is the **wire enum** in this repo: `AOSharp.Common/SmokeLounge/AOtomation/
Messaging/Messages/N3Messages/PetCommand.cs` (used by `AOBuddy/PetController.cs` and
`AOSharp.Clientless/Dynel/LocalPlayer.cs`). The slash spellings and descriptions are AO-Universe's
*Basic Pet Commands Guide*, quoted. Two enum members have no sourced slash spelling and are marked unknown.

| Command | Wire value | What |
|---|---|---|
| `/pet follow` | PetCommand.Follow = 1 | Follow the owner; movement only. AO-Universe: 'Follow owner'. The AO-Universe Bureaucrat Guide warns against it for a Crat - '/pet behind and /pet wait, /pet follow seems to be a cause of the problem'. |
| `/pet behind` | PetCommand.Behind = 2 | Stay behind the owner / retreat from combat. AO-Universe: 'Stay behind owner/Retreat from combat', and it recommends issuing it immediately after zoning. |
| `/pet wait` | PetCommand.Wait = 4 | AO-Universe: 'Wait (Stay exactly where they are/Don't move or attack anything)'. |
| `/pet guard` | PetCommand.Guard = 6 | AO-Universe: 'Guard owner (Usual status right after pets are cast)'. Pets 'often default back to GUARD after a player zones in game'. |
| `/pet attack` | PetCommand.Attack = 7 | AO-Universe: 'Attack <target>'. |
| `(social)` | PetCommand.Social = 9 | Present in the wire enum (AOSharp PetCommand.cs) and matching PetType.Social (0xE, vanity pets). The slash-command spelling is not listed by the AO-Universe pet-command guide - unknown. |
| `/pet terminate` | PetCommand.Terminate = 10 | AO-Universe: 'Terminate all current pets'. For a Crat this is how you free the slot a charm is holding. |
| `(release)` | PetCommand.Release = 11 | Present in the wire enum. Distinct from Terminate, and the obvious candidate for letting a charmed NPC go, but the AO-Universe pet-command guide does not list a '/pet release' and no local capture confirms it - the slash spelling and the exact behaviour are unknown. |
| `/pet heal` | PetCommand.Heal = 12 | AO-Universe: 'Heal (Target yourself, another player, another pet, or certain NPCs)'. The Bureaucrat has NO heal pet, so this is not a Crat tool; a Crat repairs its bot by casting Droid Repair (30072) / Droid Overhaul (30071) / Thorough Overhaul (116798) itself. |
| `/pet report` | PetCommand.Report = 14 | AO-Universe: 'Report health and fighting target'. |
| `/pet chat <message>` | PetCommand.Chat = 16 | AO-Universe: 'Talk (one word msg)'. |
| `/pet rename <name>` | not in PetCommand (client-side) | AO-Universe: 'Rename pet'. Renaming is how you scope later commands to one of the Crat's two or three pets. |
| `/pet PETNAME <command>` | n/a | AO-Universe: 'Terminate Specific Pet' - any command can be scoped to one named pet this way. This matters more for a Bureaucrat than for an Engineer or MP because a Crat routinely runs a bot AND one or two charmed NPCs at once. |
| `/macro Term /pet "%t" terminate` | n/a | AO-Universe gives this verbatim as the macro for 'Terminate Specific Pet (Macro) (Target the pet to Terminate)'. |
| `/pet help` | n/a | AO-Universe: 'Help on pet commands'. |
| `/open pet` | n/a | AO-Universe: 'Open Pet Info window (if not open)'. |

---

## Mechanics

- TWO SKILL STACKS, NOT ONE. All 65 bot summons need Matter Creation == Time & Space (verified: MC and SpaceTime carry the identical value on every one of the 65). All 37 charms need Psychological Modification == Sensory Improvement == Biological Metamorphosis (identical on all 37). A Crat that wants both a good bot and a good charm is funding five nano skills. AO-Universe's guide describes exactly this fork: 'For the first ~60 levels the bot will be more useful since charms have a very short duration. Around level 60, you should start focusing more on charms as they last longer, and you can charm monsters that have more HP than your bot.'
- THREE PETS MAXIMUM, in three slots. Bot = PetType.Attack (0xA). Short charm = PetType 11 = PetType.Heal (0xB). Long charm, Carlita, Carlo and Means Test Pet = PetType 12 = PetType.Support (0xC), all of which carry 'Pets op66 7001'. AO-Universe: 'It is possible to have 1 long and 1 short charm, or you can have 2 short charms. You can not use your Long charm if you already have a pet on short charm.'
- THE BOT LADDER IS A 9 x 7 MATRIX. Nine family template codes x seven quality grades = 63 nanos, plus Corporate Guardian and CEO Guardian = 65. Order the ladder by the SpawnItem LEVEL ARGUMENT (2 -> 215), which is the pet's level and NOT a character-level requirement: only Corporate Guardian carries a character Level requirement at all (Level > 204). Within every family the grade order Basic < Limited < Faithful < Advanced < Supervisor-Grade < Executive-Grade < Director-Grade holds without exception.
- NO CREDIT COST. Not one of the 65 Bureaucrat bot summons carries the Hit[61] Cash drain or the 'Cash > N' cast requirement that 79 of the Engineer's 90 robots carry. The Crat bot is free to build.
- THE CHARM IS A FOUR-PART NANO. Every one of the 37 fires: CharmNpc[] (take the target), TauntNpc[N] with a tier-scaled value plus a flat TauntNpc[50000] on most tiers, Hit[27,-1,-1,94] (a 1-point poke at the target), and a ChangeVariable block writing PetReq1/2/3 = 129/122/128 and PetReqVal1/2/3 = the caster's skill requirement - i.e. the charm stamps its own over-equip requirement onto the pet at cast time.
- THE TARGET HAS A CEILING: 'Psychic < N'. Every charm carries one, rising from Psychic < 69 (Temporary Glamor) to Psychic < 551 (My Way, Peer Pressure, The Voice of One, The Voice of God, The Choir Fantastic). Reading it as a cap on the TARGET rather than on the caster is what the local data supports: the Bureaucrat's Fear nanos carry 'Psychic < N' in the same requirement list as 'NPCFamily != 0 / != 94 / != 95 / != 96 / != 97 / != 98', which can only be target requirements. See _unverified.
- TEAM EMPOWERED CHARMS CHARM HARDER. The 11 'Team Empowered' variants need the same PM/SI/BM as the nano they mirror but add Shadowlands + a Specialization bit + NumberOfTeamMembers > 1, and every one has a HIGHER Psychic ceiling than its solo twin: Bend Will 99 -> 138, Dominate Psyche 125 -> 183, Allure of Servitude 287 -> 363, Total Mental Domination 455 -> 498, The Voice of Truth 501 -> 542. They also carry six CharmNpc[] functions instead of one.
- PET BUFFS ARE SPLIT BY PET KIND, and the data says so explicitly. Gallant Hero (9 nanos) requires Breed == 7 AND NPCFamily == 95 - the bot clan - so it can never be put on a charmed NPC. Gallant Slave (9 nanos) instead requires 'Flags op92 202' (HasRunningNanoLine 202 = the target is running a CharmOther effect), NPCFamily > 0 and 'Level < N' - so it only works on a charmed NPC, and only on one well below your own level. The two lines share nano strain 225 and give the same Add All Off. at each tier (+30/+60/+121/+156/+187/+223/+260/+298/+354 for Slave, +51/+88/+121/+156/+187/+223/+260/+298/+354 for Hero), but Gallant Slave also adds Max Health, Add All Def., all five damage modifiers and an instant heal.
- THE GALLANT SLAVE LEVEL CEILING IS ABOUT 80%. The pairs run (nano level 30, target Level < 23), (55, <43), (65, <49), (80, <63), (110, <87), (135, <107), (160, <127), (180, <143), (214, <171) - consistently ~0.78-0.80 of the nano's own level requirement. AO-Universe: 'Buffs our charmed pet but only if the charm is alot lower then you (below a certain level).'
- BOT-ONLY DEFENCE, PET-WIDE UTILITY. Corporate Insurance Policy (3) and Nanite Robot Protection (3) both require NPCFamily == 95, so they are bot-only. Exoskeleton Pulse (5), Droid Repair/Overhaul/Thorough Overhaul (3) and Pet Steal Back (2) carry Breed == 7 with no NPCFamily clause, so they reach any pet. Pet Attention (269869) and Pet Cleanse (269870) carry neither, so they are cast on the caster and hit all pets.
- YOUR OWN BOT CAN BE CHARMED AWAY - and there are two counters in the data. Corporate Insurance Policy (267603/267604/267605) carries ResistNanoStrain[202] at 20/25/30%, i.e. resistance to CharmOther. Pet Steal Back (269907) and Improved Pet Steal Back (269908) carry RemoveNanoStrain[202], which takes the pet back. Those two are shared with the Engineer and the Meta-Physicist (VisualProfession 3, 8 and 12 all appear in the ToUse).
- THE PET-AGGRO TOOL IS A BUFF, NOT A COMMAND. Mud Slinger (155615, +31), Defamation 101 (155616, +57) and Character Assassin (155617, +80) are strain 232 PetTauntBuff nanos cast on the bot; they add Aggressiveness so the bot holds what the Crat pulls. The Crat has NO pet-initiative line at all (MPPetInitiativeBuffs strain 217: Meta-Physicist 4 nanos, Engineer 1, Bureaucrat 0).
- SPECIALIZATION AND EXPANSION GATE THE TOP END. 'Expansion op22 2' = Shadowlands (AOSharp ExpansionFlags bit 1). 'Specialization op22 <bits>' appears on exactly 20 of the 145 Bureaucrat pet nanos - bit 1 on 5, bit 2 on 4, bit 4 on 5, bit 8 on 6 - covering the 11 Team Empowered charms, the 6 top solo long charms (The Voice of Truth, My Way, Peer Pressure, The Voice of One, The Voice of God and Corporate Guardian), Carlita (bit 4), Carlo (bit 8) and Means Test Pet (bit 4). 22 pet nanos carry the Shadowlands gate: the 11 Team Empowered charms, the 9 Gallant Slave buffs, Corporate Guardian and Carlita - Carlo and the top solo charms are Specialization-gated WITHOUT an Expansion clause. 'NanoFocusLevel op22 64' gates exactly two: CEO Guardian (273300) and The Choir Fantastic (275009).
- 'Breed == 7' MEANS THE NANO IS CAST ON YOUR PET, not that your breed matters (playbook section 3; Breed 7 = HumanMonster). 29 of the Bureaucrat's 40 pet buffs/repairs carry it, and 19 of those also carry NPCFamily == 95. The 9 Gallant Slave nanos and the 2 shared utility nanos do not - Gallant Slave identifies its target by the running charm instead.
- DEFAULT STATE IS GUARD, and AO-Universe says pets 'often default back to GUARD after a player zones in game' and recommends '/pet behind' immediately after a zone. The Bureaucrat Guide goes further and tells Crats to live on '/pet behind and /pet wait', because '/pet follow seems to be a cause of the problem'.
- CHARMED MOBS GIVE NO XP AND TURN ON YOU. AODB wiki: 'previously charmed enemies give no XP, so use discretion', and the charm is 'a small but ticking time bomb that goes off when the Charm wears off (the former ally attacks)'. AO-Universe: 'You can not charm calmed mobs/bosses/uniques/guards.'
- BOT-SELECTION RULE FOR THE BOT (AOBuddy10): filter bureaucrat-nanos.json category 'Pets - Bots', drop anything whose gates you fail (Expansion / Specialization / NanoFocusLevel / Level), and pick the highest spawnLevel whose MaterialCreation requirement your live Matter Creation satisfies - MC and Time & Space are always equal, so one comparison is enough. Do not hardcode a per-character-level table: 63 of the 65 have no character-level requirement at all.
- CHARM-SELECTION RULE (AOBuddy10): within a line (PetType 11 or 12), the nano's PM value IS the ordering key and PM == SI == BM, so one comparison picks the tier. Then check the TARGET: the mob's Psychic must be below the nano's ceiling. If the target's Psychic is not readable, the charm attempt is a gamble - prefer the highest castable tier and fall back a tier on failure.

---

## Deviations from the Meta-Physicist / Engineer pet templates

- CHARM IS A REAL, PERMANENT PET SOURCE. 37 of the Bureaucrat's 145 pet nanos are CharmOther (nano strain 202). The Meta-Physicist has ZERO charm nanos and the Engineer has ZERO; both only ever summon. The Crat's pet AI therefore needs a target-selection step (which NPC to take) that neither template class has.
- THE BOT LADDER IS A REGULAR MATRIX, NOT AN IRREGULAR LIST. 9 families x 7 grades = 63 summons, every family complete, grade order identical in all nine. The Engineer's 90 robots fall into 15 families of 1 to 12 nanos with no shared grade vocabulary; the Meta-Physicist has no SpawnItem nanos at all (it uses SummonPet).
- THE CRAT'S BOTS ARE FREE. Zero of the 65 Bureaucrat SpawnItem nanos carry the Hit[61] Cash drain or a 'Cash > N' requirement; 79 of the Engineer's 90 robots do (132 to 3,694 credits).
- TWO PARALLEL CHARM LINES, distinguished in the data by PetType and by the Pets op66 code, with a perfect 37-of-37 correlation: 26 nanos -> PetType 12 + 'Pets op66 7001' (AO-Universe's LONG charm) and 11 nanos -> PetType 11 + 'Pets op66 7002' (the SHORT charm). Nothing in the MP or Engineer pet sets has two parallel lines of one strain that occupy different pet slots.
- 'TEAM EMPOWERED' VARIANTS EXIST AND ARE STRICTLY STRONGER AT THE SAME SKILL COST. 11 of the 37 charms; same PM/SI/BM as their solo twin, six CharmNpc[] functions instead of one, and a Psychic ceiling raised by 39 to 76 points (Bend Will 99 -> 138, The Voice of Truth 501 -> 542). The price is Shadowlands + a Specialization bit + NumberOfTeamMembers > 1. The MP and Engineer have no team-gated pet nanos whatsoever.
- THE CHARM WRITES THE PET'S OWN KEEP-ALIVE REQUIREMENT. Every charm sets PetReq1/2/3 (stats 467/468/469) to 129/122/128 and PetReqVal1/2/3 (485/486/487) to the caster's required skill value, via ChangeVariable. No Meta-Physicist or Engineer summon does this - their pets' requirements are fixed by the template.
- A TARGET-STAT CEILING ('Psychic < N', 69 -> 551 across the line) gates every charm, and 8 of the 9 Bureaucrat Fear nanos carry the same construct. No Meta-Physicist or Engineer pet nano carries any target-stat ceiling.
- NO HEAL PET AT ALL, and the thinnest repair kit of the three pet classes. The Meta-Physicist has 10 heal-pet summons; the Engineer has none but gets 7 repair nanos; the Bureaucrat has none and gets THREE (Droid Repair 30072, Droid Overhaul 30071, Thorough Overhaul 116798). The PetType.Heal (0xB) slot on a Crat is occupied by a short CHARM, not by a healer.
- THE PET-BUFF LIBRARY IS SPLIT BY WHICH PET IT TARGETS - a distinction neither template class needs. Gallant Hero (9 nanos) requires NPCFamily == 95 (bot only); Gallant Slave (9 nanos) requires HasRunningNanoLine 202 (charmed NPC only) and additionally a 'Level < N' CEILING on the pet at roughly 80% of the nano's own level requirement. No MP or Engineer pet buff has a target-level ceiling.
- ONLY THREE SummonPet PETS, ALL LATE AND ALL GATED: Carlita Desposito (293899, L160, Spec bit 4), Carlo Pinnetti (258580, L220, Spec bit 8) and Means Test Pet (258222, L220, Spec bit 4). The MP has 43 SummonPet nanos across three strains with a continuous froob ladder from level 1. The Crat's froob progression is entirely SpawnItem bots.
- MEANS TEST PET IS PAID FOR OUT OF THE CHARM SKILLS. It is a SummonPet nano requiring PM/SI/BM > 2,000 each - no Matter Creation, no Time & Space. No MP or Engineer summon crosses skill stacks like that.
- THE CRAT HAS NO PET-INITIATIVE LINE AND NO DAMAGE-TRANSFER LINE. MPPetInitiativeBuffs (217): MP 4 nanos, Engineer 1, Bureaucrat 0. DamageToPet (1024): MP 2 (Sacrificial Bond / Sacrificial Shielding), Bureaucrat 0 - the Crat's nearest equivalent is the single 'Take the Bullet' (267917), which puts a Blast Shield on the bot at the cost of -3,000 to all six of the bot's nano skills. The Crat instead has a pet-TAUNT line the MP lacks (PetTauntBuff strain 232, 3 nanos, +31/+57/+80 Aggressiveness).
- THE BOT SHARES THE ENGINEER'S PET CLAN. Every bot-only Bureaucrat buff requires NPCFamily == 95, which AOSharp's NpcClan enum names 'EngineerAttackPet'. So a Crat droid and an Engineer robot are the same clan to the requirement system - the MP's pets are 96/97/98.
- THE CLIENT'S OWN BOT NAMES ARE NOT CONSISTENT, so a name-based matcher will break. 18 of the 63 matrix bots do not end in '-Droid': all seven Bodyguard grades and all seven Minion grades carry no suffix at all, as do Advanced Worker (46399), Advanced Helper (46411) and Basic Administrator (46400), while Limited Helper-Bot (46359) says '-Bot' where its six BUHE siblings say '-Droid'. Match on the SpawnItem template code, not on the name.
- GRADE ORDER CANNOT BE READ OFF THE ENGLISH. 'Supervisor-Grade' outranks 'Advanced' and 'Director-Grade' outranks 'Executive-Grade'; and in the Nanite Robot Protection line 'Basic' (L175) is WEAKER than 'Lesser' (L201). Both orderings here are taken from the data (SpawnItem level argument / Level requirement), not from the words.
- TWO NANO-FOCUS-GATED PET NANOS rather than one: CEO Guardian (273300) and The Choir Fantastic (275009) both carry 'NanoFocusLevel op22 64'. The Engineer has exactly one (Ravening M-60). The Meta-Physicist has none.
- CORPORATE GUARDIAN COSTS +440 MC/TS FOR +5 SPAWN LEVELS (786 -> 1,226 for spawn level 200 -> 205), and CEO Guardian another +243 for +10 more. The froob part of the ladder moves in 6-to-25-point steps. The Shadowlands/endgame bots are a different economy entirely.

---

## Unverified

- The numeric encoding of 'Pets op66 <N>' (Operator.TestNumPets = 66, on stat Pets = 251) is NOT established. What IS established from the data: all 26 long-charm nanos, both Associates and Means Test Pet carry 7001; all 11 short-charm nanos carry 7002; 19 of the 37 charms and both Associates additionally carry 2001; and no bot summon carries any Pets requirement at all. Two readings fit: (a) the code identifies the pet slot, which matches the PetType 12/11 split perfectly; (b) the last digit is a maximum count, which matches AO-Universe's 'you can have 2 short charms'. OmniCell/CellAO implements TestNumPets as a plain LessThan on stat 251, which is a server re-implementation rather than client truth, so it settles nothing.
- 'Flags op118 0' - operator 118 is absent from both OmniCell.Enums/Operator.cs and AOSharp UseCriteriaOperator.cs. It appears on 4 Bureaucrat pet nanos (Basic/Lesser/plain Nanite Robot Protection and Take the Bullet). Meaning unknown; recorded verbatim, not interpreted.
- 'Psychic < N' on the 37 charms is read here as a ceiling on the TARGET. The evidence is local and circumstantial: the same construct appears on 8 Bureaucrat Fear nanos alongside 'NPCFamily != 0 / 94 / 95 / 96 / 97 / 98', which can only be target requirements, and the OmniCell requirement evaluator resolves each requirement against a per-requirement Target that the nano extractor discards. It has NOT been confirmed against wire data or a citable source, and the per-requirement Target field was not re-read for this pass.
- What the SIX CharmNpc[] functions on each Team Empowered charm actually do - one NPC shared with the team, or one per team member - is not derivable from the local data. The Meta-Physicist's heal pets list their SummonPet template seven times for a single pet, so function repetition is demonstrably NOT a count.
- Whether Means Test Pet (258222) summons twenty pets or one is likewise unresolved; the nano repeats SummonPet template [0,220,-1] twenty times.
- Charm DURATIONS are not in this extraction. AO-Universe distinguishes 'long duration' (The Voice of Truth) from 'short duration' (Displace Thought Patterns) but gives no numbers, and the extracted function list carries no duration field.
- What unlocks 'NanoFocusLevel op22 64' (stat 355, bit 64) is not derivable from nano data. The engineer-pets.json pass called it an 'endgame nano-focus gate'; that wording is inherited here, not independently verified.
- The Specialization bit -> specialization NUMBER mapping (bit 1/2/4/8 = Specialization 1/2/3/4) is the conventional reading and is consistent with the level requirements on the nanos that carry each bit, but was not verified against a citable source in this pass.
- AOSharp's NpcClan enum names value 95 'EngineerAttackPet'. Every Bureaucrat bot-only buff requires NPCFamily == 95, which is why this file says Crat bots carry clan 95 - but no Bureaucrat bot was read out of a capture to confirm it directly in this pass. AOSharp PetType.cs separately states its PetType values were 'Confirmed against roughly a hundred pets in sniffs/ and captures/ ... across Meta-Physicists, Engineers and Bureaucrats', and names the 'Bureaucrat Worker' as the Attack-slot example, so the PetType half of the claim IS sniff-backed.
- PetCommand.Social (9) and PetCommand.Release (11) are in the wire enum (AOSharp PetCommand.cs) but the AO-Universe pet-command guide lists neither, so their slash spellings and exact behaviour - in particular whether Release is how you let a charmed NPC go - are unknown.
- A web search snippet attributed to the AODB wiki a line about a Bureaucrat 'pet dog from level 100' whose commands go to both pets. The actual http://wiki.aodb.us/wiki/Bureaucrat page does not contain it and the claim matches the AODB Engineer Guide's second-pet rule, so it is NOT recorded as Bureaucrat behaviour here.
- Nano ACQUISITION (which shop, mission or drop each nano crystal comes from) is out of scope for this file and is not recorded; bureaucrat-nano-sources.json does not exist yet.
- Charm success rate, resist mechanics, and whether a failed charm still lands the TauntNpc are not in the local data.

---

## Sources

- LOCAL (source of truth for every id, name, requirement and effect string in this file): AOBuddy/GameData/profiles/bureaucrat-nanos.json - 343 Bureaucrat-castable nanos extracted from E:\Funcom\OmniCell\OmniCell\Datafiles\nanos.ocp (OMNICELL-CONTENT v3, client 18.8.50_EP1) via OmniCell.Core NanoLoader.CacheAllNanos, names from E:\Funcom\attic\extracted-client-data\itemnames.sql.
- LOCAL: AOSharp.Common/GameData/PetType.cs - PetType Attack=0xA, Heal=0xB, Support=0xC, Social=0xE; the file states the values arrive in SimpleNpcInfo.PetType and PetToMasterMessage.AttachNotificationValue and were 'Confirmed against roughly a hundred pets in sniffs/ and captures/ ... across Meta-Physicists, Engineers and Bureaucrats', naming the 'Bureaucrat Worker' as the Attack-slot example. This is what makes ChangeVariable[512,11] / [512,12] on the charm nanos readable as Heal-slot / Support-slot.
- LOCAL: AOSharp.Common/SmokeLounge/AOtomation/Messaging/Messages/N3Messages/PetCommand.cs - the wire pet-command enum (Follow=1, Behind=2, Wait=4, Guard=6, Attack=7, Social=9, Terminate=10, Release=11, Heal=12, Report=14, Chat=16).
- LOCAL: AOSharp.Common/GameData/CharacterFlags.cs - NpcClan (95 EngineerAttackPet, 96/97/98 the MP pets) and ExpansionFlags (bit 1 = ShadowLands, so 'Expansion op22 2' = Shadowlands).
- LOCAL: AOSharp.Common/GameData/Stat.cs - Pets=251, PetReq1/2/3=467/468/469, PetReqVal1/2/3=485/486/487, PetType=512, NanoFocusLevel=355, NPCFamily=455, Health=27, Level=54, Profession=60, VisualProfession=368.
- LOCAL: OmniCell.Enums/Operator.cs and OmniCell.Core/Requirements/RequirementLambdaCreator.cs - operator numbers used in castReqs: 22 BitAnd, 66 TestNumPets, 92 HasRunningNanoLine. The evaluator resolves each requirement against a per-requirement Target, which the nano extractor flattens away.
- LOCAL (comparison baselines for _deviations): AOBuddy/GameData/profiles/metaphysicist-pets.json and engineer-pets.json.
- AO-Universe, 'Bureaucrat Guide - Version 1.2' (https://www.ao-universe.com/guides/classic-ao/profession-guides/bureaucrat-guide---version-12) - long vs short charm and the pet cap ('It is possible to have 1 long and 1 short charm, or you can have 2 short charms. You can not use your Long charm if you already have a pet on short charm.'); 'You can not charm calmed mobs/bosses/uniques/guards.'; Gallant Slave 'buffs our charmed pet but only if the charm is alot lower then you (below a certain level)'; 'Gallant Hero: The Vengeful Butler buffs our Bodyguard pet with more AR/dmg/taunt per hit'; the bot-vs-charm skill fork around level 60; '/pet behind and /pet wait, /pet follow seems to be a cause of the problem'; 'Carlo Pinetti' as a level 220 pet; trimmers on the robotic pet.
- AO-Universe, 'Basic Pet Commands Guide' (https://www.ao-universe.com/guides/classic-ao/gameplay-guides-6/basic-pet-commands-guide) - the verbatim command table used in the commands block, the GUARD-after-zoning default, and the /macro Term terminate-by-name macro.
- AODB Wiki, 'Bureaucrat' (http://wiki.aodb.us/wiki/Bureaucrat) - a Crat 'can have both a "summoned" droid and a "Charmed" monster ally'; 'previously charmed enemies give no XP, so use discretion'; charm as 'a small but ticking time bomb that goes off when the Charm wears off (the former ally attacks)'; the shared chant/pulse ability with Engineers and Keepers.
- NOT USED: auno.org, aoitems.com and anarchyonline.fandom.com block automation (playbook section 4). No secondhand claim from any of them appears in this file.
