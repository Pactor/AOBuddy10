# Shade - Buffs

Buff reference for AOBuddy10 Shade decision-making. Unlike every other class profiled so far, the outward-facing half of this file is almost empty: a Shade's nanos are self-only. selfBuffs below is EXHAUSTIVE - all 79 castable nanos from shade-nanos.json, not a sample.

## Self-buff lines (all 79 castable nanos, exhaustive)

| Nano line | Role | Nanos | What |
|---|---|---|---|
| ShadeProcBuff | proc | 28 | The Shade offensive weapon-proc lines. ao-universe groups them as: DD + lifetap (Ritualistic Touch/Blow/Grasp/Caress/Embrace, Sacrificial Touch/Blow/Grasp/Caress/Embrace, Ceremonial Grasp/Caress/Embrace), HP and nano DoT (Rudimentary -> Primitive -> Intrinsic -> Elemental -> Primordial -> Primal -> Chthonic Dissipation), stun (Quintessence of Incapacitation -> Paralyzation -> Petrification -> Transfixion -> Stupefication) and initiative debuff (Degeneration of Rapidity -> Celerity -> Haste). ONE PROC ACTIVE AT A TIME. Patch 18.7 made damage procs heal the Shade a little and DoT procs restore a little nano. |
| ShadePiercingBuff | weapon skill | 8 | Self Piercing buff line. ao-universe order: Whetstone Effect -> Sharpen Dagger -> Piercing Tooth -> Piercing Gash -> Probe of Death -> Master Piercer -> Puncture of the Tarasque -> Improved Puncture of the Tarasque. The top three also add damage-type modifiers and Multi Melee. Patch 18.7 added Sharp Objects modifiers to this line. |
| MartialArtsBuff | special | 7 | Self Martial Arts + Dimach line: Slap of the Zombie -> Knock of the Poltergeist -> Kick of the Phantom -> Hand of the Shadow -> Touch of the Specter -> Voice of the Banshee -> Kiss of the Vampire. |
| MultiwieldBuff | off-hand | 6 | Self Multi Melee buff line: Dual Defender -> Double Cover -> Twice the Shield -> Double Fence -> Duplex Wall -> Triple Wall. Multi Melee is what holds the off-hand weapon; the top tier also adds Parry, Martial Arts and Add All Off/Def. |
| SneakAttackBuffs | special | 6 | Self Sneak Attack buff line: Footpad Apprentice -> Silent Dagger -> Dagger in the Back -> Backpiercer -> Shuffle of the Rogue -> Backstabber. IMPORTANT: since patch 18.7 the FIRST TWO of these can be cast on another player - they are the only outward buff a Shade has. |
| FastAttackBuffs | special | 6 | Self Fast Attack buff line: Fervor of the Henchman -> Devotee -> Minion -> Disciple -> Fanatic -> Zealot. |
| AgilityBuff | defence | 5 | Self Agility + evades line: Sneak -> Mugger -> Scrounger -> Prowler -> Improved Prowler. Patch 18.7 added Concealment modifiers to this line, and the top tier adds Run Speed. |
| NOSTACKING | special | 2 | Client "NOSTACKING" strain. Symbiosis (id 225281) is the Sublime Rapport perk-level-10 special that upgrades the Totemic Rites "Ritual" perks; Pierce Nerves (id 266310) shows Modify Piercing -1000, i.e. it is a DEBUFF cast on a target, not a self-buff - the nano extraction cannot tell target from self, so this is flagged. |
| HealthDrain | sustain | 2 | Health-drain nanos. Dissolving Vitality is the lower version of Sneaking Health Drain. These are the Shade substitute for a heal. |
| WeaponEffectAdd_On2 | debuff proc | 2 | Nanite Depravation / Basic Nanite Depravation: an offensive proc that debuffs the target. |
| SpiritDrain | core mechanic | 1 | Spirit Siphon: the nano that produces Soul Capsules, and therefore Spirits. See shade-symbiants.json. ao-universe says it is based on Dimach skill. |
| RunspeedBuffs | travel | 1 | Faster than your Shadow: the Shade run-speed buff. Run Speed is the Shade primary Shadowlands travel skill and costs 1.0 IP. |
| ConcealmentBuff | utility | 1 | Shadow in the Night: the Shade Concealment buff. |
| AADBuffs | defence | 1 | Winding Serpent: flat Add All Def. |
| NemesisNanoPrograms | PvP | 1 | Shade's Caress: the Shade nemesis nano. ao-universe notes it removes auras from Keeper NCUs. |
| EmergencySneak | utility | 1 | Smoke Bomb: drop aggro and hide IN COMBAT. Added in patch 18.7. |
| SelfRoot_SnareResistBuff | utility | 1 | Release Me Now: breaks and resists root/snare. ao-universe gives it a 4-minute cooldown. |

### Every nano

| id | Nano | Line | Min lvl | Effect |
|---|---|---|---|---|
| 266300 | Shade's Caress | NemesisNanoPrograms | 100 | CastNano[275242] |
| 297342 | Spirit Siphon | SpiritDrain | 0 | CastNano[301114]; SpawnItem[IERC,5,0]; SpawnItem[IERC,10,0]; SpawnItem[IERC,20,0]; SpawnItem[IERC,30,0]; SpawnItem[IERC,40,0]; SpawnItem[IERC,50,0]; S |
| 275839 | Nanite Depravation | WeaponEffectAdd_On2 | 0 | AddOffProc[25,275840] |
| 275841 | Basic Nanite Depravation | WeaponEffectAdd_On2 | 0 | AddOffProc[21,275842] |
| 301593 | Winding Serpent | AADBuffs | 0 | Modify AddAllDef +25 |
| 273393 | Improved Prowler | AgilityBuff | 0 | Modify EvadeClsC +140; Modify DuckExp +80; Modify DodgeRanged +80; Modify Agility +110; Modify Concealment +100; Modify RunSpeed +350 |
| 210746 | Sneak | AgilityBuff | 25 | Modify EvadeClsC +30; Modify DuckExp +15; Modify DodgeRanged +15; Modify Agility +25; Modify Concealment +20 |
| 210748 | Mugger | AgilityBuff | 75 | Modify EvadeClsC +50; Modify DuckExp +25; Modify DodgeRanged +25; Modify Concealment +45; Modify Agility +45 |
| 210750 | Scrounger | AgilityBuff | 135 | Modify EvadeClsC +75; Modify DuckExp +35; Modify DodgeRanged +35; Modify Concealment +60; Modify Agility +65 |
| 210752 | Prowler | AgilityBuff | 185 | Modify EvadeClsC +70; Modify DuckExp +40; Modify DodgeRanged +40; Modify Agility +80; Modify Concealment +80 |
| 210804 | Dual Defender | MultiwieldBuff | 0 | Modify MultiMelee +20; Modify Parry +12 |
| 275843 | Triple Wall | MultiwieldBuff | 0 | Modify MultiMelee +180; Modify Parry +100; Modify MartialArts +30; Modify AddAllOff +30; Modify AddAllDef +30; Modify MeleeInit +150 |
| 210806 | Double Cover | MultiwieldBuff | 25 | Modify MultiMelee +45; Modify Parry +27 |
| 210808 | Twice the Shield | MultiwieldBuff | 75 | Modify MultiMelee +80; Modify Parry +48 |
| 210810 | Double Fence | MultiwieldBuff | 145 | Modify MultiMelee +112; Modify Parry +68 |
| 210812 | Duplex Wall | MultiwieldBuff | 195 | Modify MultiMelee +130; Modify Parry +80 |
| 210353 | Ritualistic Touch | ShadeProcBuff | 0 | AddOffProc[25,210341] |
| 210355 | Ritualistic Blow | ShadeProcBuff | 0 | AddOffProc[25,210342] |
| 210387 | Rudimentary Dissipation | ShadeProcBuff | 0 | AddOffProc[15,210382] |
| 210401 | Degeneration of Rapidity | ShadeProcBuff | 0 | AddOffProc[100,301120] |
| 210407 | Degeneration of Celerity | ShadeProcBuff | 0 | AddOffProc[100,301121] |
| 224177 | Degeneration of Haste | ShadeProcBuff | 0 | AddOffProc[100,224156] |
| 210357 | Ritualistic Grasp | ShadeProcBuff | 25 | AddOffProc[25,210343] |
| 210376 | Quintessence of Incapacitation | ShadeProcBuff | 25 | AddOffProc[10,210373] |
| 210359 | Ritualistic Caress | ShadeProcBuff | 50 | AddOffProc[25,210344] |
| 210389 | Primitive Dissipation | ShadeProcBuff | 50 | AddOffProc[15,210383] |
| 210361 | Ritualistic Embrace | ShadeProcBuff | 75 | AddOffProc[25,210345] |
| 210363 | Sacrificial Touch | ShadeProcBuff | 100 | AddOffProc[25,210346] |
| 210378 | Quintessence of Paralyzation | ShadeProcBuff | 100 | AddOffProc[12,210375] |
| 210391 | Intrinsic Dissipation | ShadeProcBuff | 100 | AddOffProc[15,210384] |
| 210365 | Sacrificial Blow | ShadeProcBuff | 135 | AddOffProc[25,210347] |
| 210393 | Elemental Dissipation | ShadeProcBuff | 155 | AddOffProc[15,210385] |
| 210367 | Sacrificial Grasp | ShadeProcBuff | 165 | AddOffProc[25,210348] |
| 210369 | Sacrificial Caress | ShadeProcBuff | 175 | AddOffProc[25,210349] |
| 210371 | Sacrificial Embrace | ShadeProcBuff | 195 | AddOffProc[25,210350] |
| 210380 | Quintessence of Petrification | ShadeProcBuff | 195 | AddOffProc[13,210375] |
| 210395 | Primordial Dissipation | ShadeProcBuff | 195 | AddOffProc[15,210386] |
| 224163 | Ceremonial Grasp | ShadeProcBuff | 203 | AddOffProc[25,224114] |
| 224169 | Quintessence of Transfixion | ShadeProcBuff | 207 | AddOffProc[14,224153] |
| 224159 | Primal Dissipation | ShadeProcBuff | 209 | AddOffProc[15,224155] |
| 224165 | Ceremonial Caress | ShadeProcBuff | 212 | AddOffProc[25,224112] |
| 224161 | Chthonic Dissipation | ShadeProcBuff | 216 | AddOffProc[15,224154] |
| 224167 | Ceremonial Embrace | ShadeProcBuff | 218 | AddOffProc[26,224113] |
| 224171 | Quintessence of Stupefication | ShadeProcBuff | 220 | AddOffProc[15,224151] |
| 210321 | Fervor of the Henchman | FastAttackBuffs | 0 | Modify FastAttack +30 |
| 210323 | Fervor of the Devotee | FastAttackBuffs | 0 | Modify FastAttack +70 |
| 210325 | Fervor of the Minion | FastAttackBuffs | 0 | Modify FastAttack +90 |
| 210327 | Fervor of the Disciple | FastAttackBuffs | 0 | Modify FastAttack +110 |
| 210329 | Fervor of the Fanatic | FastAttackBuffs | 0 | Modify FastAttack +135 |
| 210331 | Fervor of the Zealot | FastAttackBuffs | 0 | Modify FastAttack +150 |
| 210732 | Slap of the Zombie | MartialArtsBuff | 0 | Modify Dimach +19; Modify MartialArts +16 |
| 210734 | Knock of the Poltergeist | MartialArtsBuff | 25 | Modify Dimach +40; Modify MartialArts +34 |
| 210736 | Kick of the Phantom | MartialArtsBuff | 50 | Modify Dimach +75; Modify MartialArts +64 |
| 210738 | Hand of the Shadow | MartialArtsBuff | 100 | Modify Dimach +94; Modify MartialArts +80 |
| 210740 | Touch of the Specter | MartialArtsBuff | 135 | Modify Dimach +110; Modify MartialArts +94 |
| 210742 | Voice of the Banshee | MartialArtsBuff | 165 | Modify Dimach +127; Modify MartialArts +108 |
| 210744 | Kiss of the Vampire | MartialArtsBuff | 195 | Modify Dimach +140; Modify MartialArts +120 |
| 225281 | Symbiosis | NOSTACKING | 0 | CastNano[209986]; CastNano[209987]; CastNano[210019]; CastNano[210020] |
| 266310 | Pierce Nerves | NOSTACKING | 0 | Modify Piercing -1000 |
| 210787 | Footpad Apprentice | SneakAttackBuffs | 0 | Modify SneakAttack +30 |
| 210789 | Silent Dagger | SneakAttackBuffs | 0 | Modify SneakAttack +70 |
| 210791 | Dagger in the Back | SneakAttackBuffs | 0 | Modify SneakAttack +90 |
| 210793 | Backpiercer | SneakAttackBuffs | 0 | Modify SneakAttack +110 |
| 210795 | Shuffle of the Rogue | SneakAttackBuffs | 0 | Modify SneakAttack +135 |
| 210797 | Backstabber | SneakAttackBuffs | 0 | Modify SneakAttack +150 |
| 273390 | Sneaking Health Drain | HealthDrain | 0 | CastNano[273391]; Hit[27,-950,-950,91]; Hit[27,-950,-950,91] |
| 301895 | Dissolving Vitality | HealthDrain | 0 | CastNano[301894]; Hit[27,-250,-250,91]; Hit[27,-250,-250,91] |
| 272371 | Faster than your Shadow | RunspeedBuffs | 0 | Modify RunSpeed +250 |
| 273395 | Shadow in the Night | ConcealmentBuff | 0 | Modify Concealment +750 |
| 301160 | Smoke Bomb | EmergencySneak | 0 | (2 non-effect functions only) |
| 281239 | Release Me Now | SelfRoot_SnareResistBuff | 0 | RemoveNanoStrain[145]; RemoveNanoStrain[146]; ResistNanoStrain[145,100]; ResistNanoStrain[146,100] |
| 211152 | Sharpen Dagger | ShadePiercingBuff | 0 | Modify Piercing +29 |
| 211154 | Whetstone Effect | ShadePiercingBuff | 0 | Modify Piercing +12 |
| 270804 | Improved Puncture of the Tarasque | ShadePiercingBuff | 0 | Modify Piercing +200; Modify MultiMelee +30; Modify SharpObject +140; Modify MeleeDamageModifier +45; Modify ProjectileDamageModifier +45; Modify Ener |
| 211150 | Piercing Tooth | ShadePiercingBuff | 25 | Modify Piercing +54 |
| 211148 | Piercing Gash | ShadePiercingBuff | 75 | Modify Piercing +85 |
| 211146 | Probe of Death | ShadePiercingBuff | 100 | Modify Piercing +110; Modify MeleeDamageModifier +3; Modify ProjectileDamageModifier +3; Modify EnergyDamageModifier +3; Modify FireDamageModifier +3; |
| 211144 | Master Piercer | ShadePiercingBuff | 155 | Modify Piercing +127; Modify MultiMelee +4; Modify MeleeDamageModifier +7; Modify ProjectileDamageModifier +7; Modify EnergyDamageModifier +7; Modify  |
| 211142 | Puncture of the Tarasque | ShadePiercingBuff | 195 | Modify Piercing +140; Modify MultiMelee +10; Modify SharpObject +80; Modify MeleeDamageModifier +15; Modify ProjectileDamageModifier +15; Modify Energ |

## What a Shade GIVES to others

THIS IS THE SHADE'S BIGGEST DEVIATION FROM EVERY OTHER CLASS PROFILE. A Meta-Physicist hands out 14 buff lines; a Shade hands out TWO nanos, and both are the same Sneak Attack line, and they only help the three Sneak-Attack professions. ao-universe states the kit outright: "The Shade utilizes two types of nano, the first type are self-only skill buffs and the second, again self-only, are Process or 'proc' buffs." The only exception is the patch-18.7 change: "Shade's two first Sneak attack buffs can now be cast on any target, the best target nano increases the target's sneak attack skill by 70 points." Everything else a Shade casts outward is hostile - debuffs, stuns, DoTs and leeches. Any team-buff logic written against the MP or Doctor shape will find nothing to do here.

| id | Nano | Effect | Wanted by |
|---|---|---|---|
| 210787 | Footpad Apprentice | Sneak Attack +30 - castable on another player since patch 18.7. | Adventurers, Keepers and other Shades - the three professions that use Sneak Attack. Patch 18.7 also made this nano line castable BY Adventurers and Keepers. |
| 210789 | Silent Dagger | Sneak Attack +70 - the best targetable version. Castable on another player since patch 18.7. | Adventurers, Keepers and other Shades. |

## What a Shade WANTS from others

THIN ON PURPOSE. Unlike the Meta-Physicist file, no allowed source publishes a "what a Shade should ask for" list. The five entries above are everything the fetched pages actually state. A Shade obviously also benefits from the generic buffs every profession wants (Trader wrangles, Enforcer Essence for HP, Doctor initiative and HP buffs, Crat/Keeper team auras) but no fetched page says so for this class, so they are NOT asserted here.

| Buff | From | Effect | Use | Note |
|---|---|---|---|---|
| Composite Infuses / Mocham's Gift lines | Meta-Physicist (or an Agent in False Profession) | Raises all six nano skills at once. | cast | ao-universe names Infuses and Mochams from an MP, or an Agent in False Profession, as what a Shade needs to meet its own nano requirements. This matters because the top Shade proc needs 1,840 Psychological Modification. source=web |
| Mark of Peril | Martial Artist | Critical chance. | raid | The ao-universe Shade guide says Mark of Peril and the Agent's Take the Shot STACK for critical chance. source=web |
| Take the Shot | Agent | CONFLICTING SOURCES - see note. | raid | SOURCE CONFLICT, do not hard-code: the ao-universe Comprehensive Shade Guide presents Take the Shot as a crit buff that stacks with Mark of Peril for a Shade, but ao-universe's own Buffing Guide says Take the Shot buffs AIMED SHOT - a ranged special a Shade cannot use. Unresolved. source=web |
| Computer Literacy buffs (tutoring device, 10-Intel contract, the Gauntlet buff, CL pistols, a Syndicate Brain Spirit) | various / items | Raises Computer Literacy toward the ~1,750 needed for a Pandemonium belt at level 200. | twink | Computer Literacy is DARK BLUE for a Shade (IP cost 2.4) and is the single hardest requirement it has to twink. The documented stack is on the Funcom forums: Syndicate Brain +25 CL, Apotheosis 1 +50 CL, research ability and completion trickle, a CL tutoring device, a 10-Intel contract, the Gauntlet buff, CL pistols, Intelligence trickle. source=web |
| Leet aura / team morphs | Adventurer | Crit modifier, Concealment and Run Speed. | raid | Patch 18.7 gave Shades access to some team morphs as Adventurer buffs; wiki.aodb.us records "Leet auras affect CritMod, Concealment, and RunSpeed". source=web |
| Bloodletting | Trader | Not a stat buff - it is a PERK-CHAIN TRIGGER. | raid | wiki.aodb.us/wiki/Perk_Chains: all three Spirit Phylactery "Unsealed" perks gain a bonus when the target carries a Trader's Bloodletting. Worth asking for in a raid team. source=web |

## Proc rules

The ShadeProcBuff nanos are separate NANO STRAINS from the LE research procs, so a Shade runs one nano proc alongside its research procs. ao-universe: use the STUN proc for PvP and ordinary mobs, a DAMAGE proc for team damage, and the INITIATIVE-DEBUFF proc (Degeneration of Rapidity/Celerity/Haste) for solo aliens and bosses, which are immune to stuns - it interrupts their nukes. The stun line scales badly at the top: "only 1% additional chance per tier at high levels".

SEPARATE SYSTEM - see shade-build.json leProcPerks.researchLineMapping. The 12 LE research procs split into two mutually exclusive types and only ONE of each type can be active at a time.

The proc nanos are gated on Psychological Modification above all - it is a cast requirement on 73 of the 79 Shade nanos and tops out at 1,840 (read from shade-nanos.json). Biological Metamorphosis (32 nanos, to 1,579), Time & Space (33, to 1,569) and Sensory Improvement (43, to 1,243) gate the rest. Matter Creation gates exactly ONE nano and is not worth IP.

## Keep-up order for the bot

- 1. Weapon skill first: the top ShadePiercingBuff you can cast (Improved Puncture of the Tarasque id 270804 +200 Piercing / +30 Multi Melee / +140 Sharp Objects, else Puncture of the Tarasque id 211142 +140 Piercing). This is what sets the QL of weapon you can hold.
- 2. Multi Melee: Triple Wall id 275843 (+180 Multi Melee, +100 Parry, +30 Martial Arts, +30 Add All Off, +30 Add All Def), else Duplex Wall id 210812.
- 3. Sneak Attack: Backstabber id 210797 (+150).
- 4. Fast Attack: Fervor of the Zealot id 210331 (+150).
- 5. Agility/evades: Improved Prowler id 273393 (+140 Evade-ClsC, +110 Agility, +100 Concealment, +80 Duck-Exp, +80 Dodge-Rng), else Prowler id 210752.
- 6. Dimach/Martial Arts: Kiss of the Vampire id 210744 (+140 Dimach, +120 Martial Arts).
- 7. Add All Def: Winding Serpent id 301593 (+25).
- 8. Concealment: Shadow in the Night id 273395 (+750) when you need to set up or travel.
- 9. Run Speed: Faster than your Shadow id 272371 (+250) out of combat.
- 10. ONE offensive proc, chosen for the fight - see procRules.

Within a nano LINE only the highest version holds - casting Prowler over Improved Prowler downgrades you. The nanoLine field on every row above is the strain to check before casting.

## Situational

- Smoke Bomb id 301160 - drop aggro and hide IN COMBAT. The Shade panic button.
- Release Me Now id 281239 - breaks and resists root/snare (strains 145/146), 4-minute cooldown per ao-universe.
- Sneaking Health Drain id 273390 and Dissolving Vitality id 301895 - the Shade's only healing, and both are drains, so they need a target.
- Spirit Siphon id 297342 - cast on any target under 20% health, on cooldown, all the time. It is how you get Spirits.
- Shade's Caress id 266300 - the nemesis nano; ao-universe notes it removes auras from Keeper NCUs.

## Where to buy Shade nanos

Shade nanos are sold by the Shadowlands garden and sanctuary vendors. ao-universe's shopping-by-profession page puts PANDEMONIUM GARDEN far ahead with 13 Shade listings, then Inferno Sanctuary/Garden, Adonis Garden/Sanctuary, Elysium Garden/Sanctuary and Penumbra Sanctuary; Scheol Garden carries only one.

shade-nano-sources.json resolved a crystal item for 78 of the 79 nanos and classified 77 as mission-roll and 2 as shadowlands from the crystal's own item criteria. The local data has no vendor table, so the garden list above is the web source for where to buy rather than roll.

## Where the Shade breaks the class-profile template

- GIVES ALMOST NOTHING TO OTHERS - 2 nanos, one line, three eligible professions. See _givesToOthersFinding. This is the single largest structural difference from the MP template.
- NO HEALS. The Heals/HoT category in shade-nanos.json holds two HEALTH DRAINS, not heals. A Shade sustains by leeching off the target, which means it cannot top itself up out of combat and cannot help a teammate at all.
- THE BUFF KIT IS A WEAPON-SKILL KIT. 70 of the 79 nanos are self buffs and the big ones all feed one attack: Piercing, Multi Melee, Sneak Attack, Fast Attack, Dimach, evades. There is no nano that changes what the Shade DOES, only how hard it hits.
- PROCS ARE THE KIT, AND THEY HAVE TWO SEPARATE STACKING SYSTEMS. 21 of the 79 nanos are ShadeProcBuff, and on top of them sit 12 LE research procs with their own two-type exclusivity rule. A bot has to model both: one nano proc, one type-1 research proc, one type-2 research proc.
- THE WEAPON-SKILL BUFF IS A GEAR GATE, NOT A DAMAGE BUFF. +200 Piercing from Improved Puncture of the Tarasque is what lets a Shade equip the next weapon QL. Buff-before-equip ordering matters more for this class than for a caster.
- NANO SCHOOLS ARE LOPSIDED. Psychological Modification gates 73 of 79 nanos; Matter Creation gates 1. A generic "train the nano schools" routine wastes most of the IP it spends.

## Deliberately unverified

- No fetched source gives the numeric effect, proc chance or duration of any Shade proc nano - only the line names and the in-client effect functions recorded above.
- SOURCE CONFLICT on Take the Shot (Agent): the Shade guide presents it as a crit buff stacking with Mark of Peril; ao-universe's Buffing Guide says it buffs Aimed Shot. Unresolved - recorded as a conflict, not resolved in either direction.
- The cross-profession buff list is only what the fetched pages state for a Shade. The generic buffs every profession wants are deliberately NOT listed here as Shade facts.
- Pierce Nerves (id 266310, Modify Piercing -1000) and Symbiosis (id 225281) sit on the client strain NOSTACKING and are almost certainly not self-buffs - Pierce Nerves reads as a target debuff and Symbiosis as the Sublime Rapport perk-10 special. The nano extraction does not record target type, so they are carried in the list with this flag rather than silently dropped.
- keepUp on each row is a derived judgement from the nano line role, not a sourced value.
- The Shadowlands garden vendor list is ao-universe's count of Shade listings per garden, not an id-level vendor table - the local data has no vendor data for these.

## Sources

- LOCAL AOBuddy/GameData/profiles/shade-nanos.json (nanos.ocp extraction, client 18.8.50_EP1) - every id, nano line, strain, level requirement, cast requirement and effect in selfBuffs.
- LOCAL AOBuddy/GameData/profiles/shade-nano-sources.json - the per-nano crystal/acquisition join.
- LOCAL E:/Funcom/attic/extracted-client-data/itemnames.sql - id -> name verification.
- https://www.ao-universe.com/guides/shadowlands/professions-guides/comprehensive-shade-guide
- https://www.ao-universe.com/guides/classic-ao/gameplay-guides-6/buffing-guide
- https://www.ao-universe.com/guides/shadowlands/gameplay-guides-5/sl-nano-shopping-by-profession
- http://wiki.aodb.us/wiki/Patch_18.7
- https://wiki.aodb.us/wiki/Shade
- https://forums.funcom.com/t/shade-equiping-pande-belt-at-200/79965
- https://wiki.aodb.us/wiki/Perk_Chains
