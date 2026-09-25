# Shade - Endgame weapons

Every endgame-tier weapon model a Shade can wield, from `items.ocp`, with a cited acquisition where one exists.

## Coverage

| | |
|---|---|
| Models | 149 |
| With a cited acquisition | 122 |
| Left unknown on purpose | 27 |
| Filled by the Shade acquisition overlay | 95 |

## The Shade acquisition overlay

gen-weapons-endgame.ps1 carries a shared, name-matched acquisition table built for the classes done before this one. It knows none of the Shade-only weapon lines, so 120 of the 149 Shade models came back how="unknown". A dedicated citation-only web pass on ao-universe.com, wiki.aodb.us and forums.funcom.com filled in the lines below. Anything not listed here stays unknown - nothing was approximated.

Other class passes are running against tools/mp-weapon-extractor/gen-weapons-endgame.ps1 at the same time, so this overlay is a separate additive script (shadework/gen/weapon_acq.py) rather than an edit to that shared file. To fold it in permanently, add these name-matched entries to $Script:Acq in the .ps1 - they are inert for every other profession because the names are Shade-only.

## Corrections to assumptions that circulate about Shade weapons

- PERENNIUM IS NOT A SHADE LINE. Perennium weapons are Blaster / Sniper / Beamer - ranged, for Soldiers, Agents and Fixers. There is no Perennium Piercing weapon.
- VEKTOR ND IS A SHOTGUN, not a Piercing weapon.
- "BLADES OF INRIKUS" DOES NOT EXIST. The real item is BLADES OF BOLTAR, crafted from an Energy Redistribution Unit plus a QL250+ Kyr'Ozch Energy Rapier Type 992, and upgradeable further into X1-R4 Viral Force-Blades with an Action Probability Estimator. That chain DOES start from a Shade-relevant Piercing weapon.
- DREADLOCH HAS NO PIERCING WEAPON FOR A SHADE on any allowed source. The only Shade-relevant Dreadloch item named anywhere is the Dreadloch Endurance Booster, which is Nanomage-only. Dreadloch bosses at the Clan and Omni camps DO drop the bio-material clumps used for Ofab armour upgrades.

## The documented mid-game weapon hole

A documented, real problem for this class, recorded because it changes how to play it: a level-115 Shade on the Funcom forums reports that after the Fear-forged Blades "there seems to be a major gap on RK2019" - Sigds cap around QL150 from garden shops, the 070/000 weapons need heavy Brawl (IP cost 4.0 for a Shade, and no Shade nano or perk buffs Brawl), and the Nippy John Stiletto and Piercing Evil are both "right out of reach" at that level. The sourced bridges are the Kyr'Ozch Energy Rapier Type 992 line (AI) and the Ofab Viper Mk5/Mk6 line (LE victory points).

## Searched and genuinely not found

- **Thousand Stings** - SEARCHED AND NOT FOUND. "Thousand Stings" returns zero hits on wiki.aodb.us (full-text search), forums.funcom.com (Discourse search) and the ao-universe forum (0 matches), and the phrase does not appear in the Comprehensive Shade Guide. The six Thousand Stings models are real Shade-locked Piercing weapons in items.ocp (ids 230477-230487) but their source is genuinely undocumented on the allowed sources. Left unknown.
- **Bronto Vet Lancet** - SEARCHED AND NOT FOUND. "Lancet" returns no results on wiki.aodb.us and 0 matches on the ao-universe forum. The only hit in the entire allowed corpus is one player sentence: "I am currently using Bronto Lancets several levels below my character. but they are the best dps I can find... I check the Shere gard[en]". That poster hunting in Shere Garden (Elysium) is SUGGESTIVE of garden shop stock like the Sigd - and it is confirmed that "If you have the Shadowlands extension, you can access special NPC vendors in each Garden or Sanctuary" - but that is inference, not a sourced fact. Left unknown.

## Weapon-line rules

- Several Rubi-Ka weapon families appear in items.ocp as [name] - 870 / - 070 / - 050 / - 010 / - 000 (Stabber, Squibber, Slank Chop, Light Spear, Nightspear, Notum Spear, E-Blade, Nano-Charged Stun Glove). The suffix is the upgrade GRADE, not a different weapon: -870 is shop stock from general stores such as Fair Trade, and -000 is the top of the free, repeatable Basic Upgrade Kit chain run at Engineer Heath Bridges near the Bronto Burger in Holes in the Wall. Sourced from the Funcom Shade weapon threads, which recommend exactly this route for a mid-level Shade.
- The Inamorata (Omni/Redeemed) and Sacrosanct (Clan/Unredeemed - spelled "Sancrosanct" in items.ocp) families are the base of the Turn Spirit chain and drop CROSSED OVER: unredeemed mobs drop the redeemed weapons and vice versa. The Aban/Enel/Thar/Xum/-Shere variants are the glyph-upgraded middle step, not separate drops.

- Recorded because it decides whether a Shade can Backstab with it: the Newland Fish Knife One-Five Stars, Kiddy, Aleksander's Blooded Punchknife and Ofab Viper Mk1-Mk4 carry NO Sneak Attack special.

## Piercing (101 models, IP cost 1)

| id | Model | QL | Tier | Wield | Specials | Expansion | Where |
|---|---|---|---|---|---|---|---|
| 265019 | Ofab Viper Mk 6 | 1-300 | expansion | Piercing 2200 | SneakAttack 1100, FastAttack 1100, Dimach 440 | LostEden | shop|tradeskill - Battlestation - Ofab terminals, Unicorn Defence Hub |
| 305525 | Uklesh's Talon | 300 | high-rk | Piercing 2149 | SneakAttack 1099, FastAttack 1099, Dimach 449 | - | drop - Temple of Three Winds RAID - Uklesh the Beguiling |
| 305026 | Slayerdroid Notum-Imbued Claw | 200 | high-rk | Piercing 1999 | SneakAttack 999, FastAttack 1199, Dimach 499 | - | unknown |
| 265005 | Ofab Viper Mk 4 | 1-300 | expansion | Piercing 2200 | FastAttack 1100, Dimach 440 | LostEden | shop|tradeskill - Battlestation - Ofab terminals, Unicorn Defence Hub |
| 265012 | Ofab Viper Mk 5 | 1-300 | expansion | Piercing 2200 | SneakAttack 1100, FastAttack 1100, Dimach 440 | LostEden | shop|tradeskill - Battlestation - Ofab terminals, Unicorn Defence Hub |
| 280719 | Deceit of the Xan | 300 | TL6 | Piercing 2250 | SneakAttack 1125, FastAttack 1125, Dimach 450 | LegacyOfTheXan | tradeskill - Legacy of the Xan team instances (device) + Pandemonium (base weapon) |
| 254716 | Kyr'Ozch Energy Rapier | 1-300 | expansion | Piercing 2000 | - | AlienInvasion | drop - Alien city raids / Alien Mothership |
| 254723 | Kyr'Ozch Energy Rapier - Type 992 | 1-300 | expansion | Piercing 2000, Parry 800, Riposte 800 | SneakAttack 1000, FastAttack 1000, Dimach 400 | AlienInvasion | tradeskill - Alien city raids / Alien Mothership (base weapon + clump) |
| 264991 | Ofab Viper Mk 2 | 1-300 | expansion | Piercing 2200 | Dimach 440 | LostEden | shop|tradeskill - Battlestation - Ofab terminals, Unicorn Defence Hub |
| 264998 | Ofab Viper Mk 3 | 1-300 | expansion | Piercing 2200 | FastAttack 1100, Dimach 440 | LostEden | shop|tradeskill - Battlestation - Ofab terminals, Unicorn Defence Hub |
| 274978 | Improved Hacked Medi-Blade | 300 | high-rk | Piercing 2200 | SneakAttack 1100, FastAttack 1100, Dimach 440 | - | drop + tradeskill - Escaped Prisoners daily (Milky Way), from Warden Ystanes |
| 264984 | Ofab Viper Mk 1 | 1-300 | expansion | Piercing 2200 | Dimach 440 | LostEden | shop|tradeskill - Battlestation - Ofab terminals, Unicorn Defence Hub |
| 254779 | Kyr'Ozch Spear | 1-300 | expansion | Piercing 2000 | - | AlienInvasion | drop - Alien city raids / Alien Mothership |
| 254786 | Kyr'Ozch Spear - Type 112 | 1-300 | expansion | Piercing 2000 | FastAttack 1000, Brawl 1200, Dimach 400 | AlienInvasion | tradeskill - Alien city raids / Alien Mothership (base weapon + clump) |
| 274974 | Hacked Medi-Blade | 300 | high-rk | Piercing 2200 | SneakAttack 1100, FastAttack 1100, Dimach 440 | - | drop + tradeskill - Escaped Prisoners daily (Milky Way), from Warden Ystanes |
| 271324 | Light Spear - 870 | 1-300 | high-rk | Piercing 1500 | Brawl 900, FastAttack 750, Dimach 300 | - | shop - General stores (for example Fair Trade) |
| 271291 | Slank Chop - 870 | 1-300 | high-rk | Piercing 1500 | Brawl 900, FastAttack 750, Dimach 300 | - | shop - General stores (for example Fair Trade) |
| 271314 | Squibber - 870 | 1-300 | high-rk | Piercing 1500 | Brawl 900, FastAttack 750, Dimach 300 | - | shop - General stores (for example Fair Trade) |
| 271304 | Stabber - 870 | 1-300 | high-rk | Piercing 1500 | Brawl 900, FastAttack 750, Dimach 300 | - | shop - General stores (for example Fair Trade) |
| 271316 | Light Spear - 000 | 1-300 | high-rk | Piercing 1500 | - | - | tradeskill - Crafted with Basic Upgrade Kits from Engineer Heath Bridges, Holes in the Wall |
| 271318 | Light Spear - 010 | 1-300 | high-rk | Piercing 1500 | Brawl 900 | - | tradeskill - Crafted with Basic Upgrade Kits from Engineer Heath Bridges, Holes in the Wall |
| 271320 | Light Spear - 050 | 1-300 | high-rk | Piercing 1500 | Brawl 900, FastAttack 750 | - | tradeskill - Crafted with Basic Upgrade Kits from Engineer Heath Bridges, Holes in the Wall |
| 271322 | Light Spear - 070 | 1-300 | high-rk | Piercing 1500 | Brawl 900, FastAttack 750, Dimach 300 | - | tradeskill - Crafted with Basic Upgrade Kits from Engineer Heath Bridges, Holes in the Wall |
| 271283 | Slank Chop - 000 | 1-300 | high-rk | Piercing 1500 | - | - | tradeskill - Crafted with Basic Upgrade Kits from Engineer Heath Bridges, Holes in the Wall |
| 271285 | Slank Chop - 010 | 1-300 | high-rk | Piercing 1500 | Brawl 900 | - | tradeskill - Crafted with Basic Upgrade Kits from Engineer Heath Bridges, Holes in the Wall |
| 271287 | Slank Chop - 050 | 1-300 | high-rk | Piercing 1500 | Brawl 900, FastAttack 750 | - | tradeskill - Crafted with Basic Upgrade Kits from Engineer Heath Bridges, Holes in the Wall |
| 271289 | Slank Chop - 070 | 1-300 | high-rk | Piercing 1500 | Brawl 900, FastAttack 750, Dimach 300 | - | tradeskill - Crafted with Basic Upgrade Kits from Engineer Heath Bridges, Holes in the Wall |
| 271306 | Squibber - 000 | 1-300 | high-rk | Piercing 1500 | - | - | tradeskill - Crafted with Basic Upgrade Kits from Engineer Heath Bridges, Holes in the Wall |
| 271308 | Squibber - 010 | 1-300 | high-rk | Piercing 1500 | Brawl 750 | - | tradeskill - Crafted with Basic Upgrade Kits from Engineer Heath Bridges, Holes in the Wall |
| 271310 | Squibber - 050 | 1-300 | high-rk | Piercing 1500 | Brawl 900, FastAttack 750 | - | tradeskill - Crafted with Basic Upgrade Kits from Engineer Heath Bridges, Holes in the Wall |
| 271312 | Squibber - 070 | 1-300 | high-rk | Piercing 1500 | Brawl 900, FastAttack 750, Dimach 300 | - | tradeskill - Crafted with Basic Upgrade Kits from Engineer Heath Bridges, Holes in the Wall |
| 271296 | Stabber - 000 | 1-300 | high-rk | Piercing 1500 | - | - | tradeskill - Crafted with Basic Upgrade Kits from Engineer Heath Bridges, Holes in the Wall |
| 271298 | Stabber - 010 | 1-300 | high-rk | Piercing 1500 | Brawl 900 | - | tradeskill - Crafted with Basic Upgrade Kits from Engineer Heath Bridges, Holes in the Wall |
| 271300 | Stabber - 050 | 1-300 | high-rk | Piercing 1500 | Brawl 900, FastAttack 750 | - | tradeskill - Crafted with Basic Upgrade Kits from Engineer Heath Bridges, Holes in the Wall |
| 271302 | Stabber - 070 | 1-300 | high-rk | Piercing 1500 | Brawl 900, FastAttack 750, Dimach 300 | - | tradeskill - Crafted with Basic Upgrade Kits from Engineer Heath Bridges, Holes in the Wall |
| 271344 | Nightspear - 870 | 1-300 | high-rk | Piercing 1500 | Brawl 900, FastAttack 750, Dimach 300 | - | shop - General stores (for example Fair Trade) |
| 271334 | Notum Spear - 870 | 1-300 | high-rk | Piercing 1500 | Brawl 900, FastAttack 750, Dimach 300 | - | shop - General stores (for example Fair Trade) |
| 253241 | Aleksander's Blooded Punchknife | 300 | expansion | Piercing 1700 | FastAttack 850 | AlienInvasion | drop - Alien City raids - Generals and Fleet Admirals |
| 253240 | Aleksander's Punchknife | 1-299 | expansion | Piercing 1694 | FastAttack 847 | AlienInvasion | drop - Alien City raids - Generals and Fleet Admirals (family source) |
| 302935 | Corrupted Lord of Deceit | 300 | TL7 | Piercing 1999 | SneakAttack 899, FastAttack 899, Dimach 450 | LegacyOfTheXan | drop - Pyramid of Home / "Dark Pyramid Below" (6-man, level 201-220) |
| 271336 | Nightspear - 000 | 1-300 | high-rk | Piercing 1500 | - | - | tradeskill - Crafted with Basic Upgrade Kits from Engineer Heath Bridges, Holes in the Wall |
| 271338 | Nightspear - 010 | 1-300 | high-rk | Piercing 1500 | Brawl 900 | - | tradeskill - Crafted with Basic Upgrade Kits from Engineer Heath Bridges, Holes in the Wall |
| 271340 | Nightspear - 050 | 1-300 | high-rk | Piercing 1500 | Brawl 900, FastAttack 750 | - | tradeskill - Crafted with Basic Upgrade Kits from Engineer Heath Bridges, Holes in the Wall |
| 271342 | Nightspear - 070 | 1-300 | high-rk | Piercing 1500 | Brawl 900, FastAttack 750, Dimach 300 | - | tradeskill - Crafted with Basic Upgrade Kits from Engineer Heath Bridges, Holes in the Wall |
| 271326 | Notum Spear - 000 | 1-300 | high-rk | Piercing 1500 | - | - | tradeskill - Crafted with Basic Upgrade Kits from Engineer Heath Bridges, Holes in the Wall |
| 271328 | Notum Spear - 010 | 1-300 | high-rk | Piercing 1500 | Brawl 900 | - | tradeskill - Crafted with Basic Upgrade Kits from Engineer Heath Bridges, Holes in the Wall |
| 271330 | Notum Spear - 050 | 1-300 | high-rk | Piercing 1500 | Brawl 900, FastAttack 750 | - | tradeskill - Crafted with Basic Upgrade Kits from Engineer Heath Bridges, Holes in the Wall |
| 271332 | Notum Spear - 070 | 1-300 | high-rk | Piercing 1500 | Brawl 900, FastAttack 750, Dimach 300 | - | tradeskill - Crafted with Basic Upgrade Kits from Engineer Heath Bridges, Holes in the Wall |
| 249079 | Premium Variable Density Tanto | 200 | expansion | Piercing 1100 | FastAttack 550 | AlienInvasion | unknown |
| 214160 | Susurrating Spike of Menace | 300 | high-rk | Piercing 1700 | SneakAttack 850, FastAttack 850, Dimach 340 | - | tradeskill - Turn Spirit weapon line - OMNI-TEK side, QL300 |
| 214165 | Twinkling Trick Poker | 300 | high-rk | Piercing 1700 | SneakAttack 850, FastAttack 850, Dimach 340 | - | tradeskill - Turn Spirit weapon line - CLAN side, QL300 |
| 214159 | Spike of Menace | 100-299 | high-rk | Piercing 1694 | SneakAttack 847, FastAttack 847, Dimach 339 | - | tradeskill - Turn Spirit weapon line - OMNI-TEK side |
| 214164 | Trick Poker | 100-299 | high-rk | Piercing 1694 | SneakAttack 847, FastAttack 847, Dimach 339 | - | tradeskill - Turn Spirit weapon line - CLAN side |
| 245768 | Piercing Evil | 220-260 | expansion | Piercing 1600, Parry 640, Riposte 640 | SneakAttack 800, FastAttack 800, Dimach 320 | Shadowlands | drop - Inferno - the mob "Anitaap's Shadow" |
| 247069 | Bloodlust | 1-300 | high-rk | Piercing 2000, Parry 800, Riposte 800 | SneakAttack 1000, FastAttack 1000, Dimach 400 | - | quest - Elysium - The Melting Pot (720 x 1565), the Yuttos NPC "One Who Wins Over Mind" |
| 226486 | Die Nadel | 300 | high-rk | Piercing 1800 | SneakAttack 900, FastAttack 900 | - | unknown - unknown - not documented separately from Die Kleine Nadel |
| 206707 | Improved Tango Dirk | 1-200 | high-rk | Piercing 1038 | Brawl 623 | - | unknown - unknown - wiki.aodb.us documents the item but not its source |
| 247080 | Spear of Forbidden Ceremonies | 200-300 | high-rk | Piercing 1800, Parry 720, Riposte 720 | SneakAttack 900, FastAttack 900, Dimach 360 | - | drop - Inferno - the Catacomb Boss and the Lord of the Void |
| 208098 | Newland Fish Knife - Five Stars | 200 | high-rk | Piercing 1176 | FastAttack 588, Brawl 706 | - | unknown |
| 212886 | Inamorata Aban Rapier | 100-300 | expansion | Piercing 1600 | SneakAttack 800 | Shadowlands | drop (+ optional glyph upgrade) - Shadowlands - sided mobs (UNREDEEMED mobs drop the REDEEMED weapons and vice versa) |
| 212851 | Inamorata Aban-Shere Rapier | 100-300 | expansion | Piercing 1600 | SneakAttack 800, FastAttack 800, Brawl 960 | Shadowlands | drop (+ optional glyph upgrade) - Shadowlands - sided mobs (UNREDEEMED mobs drop the REDEEMED weapons and vice versa) |
| 212856 | Inamorata Aban-Thar Rapier | 100-300 | expansion | Piercing 1600 | SneakAttack 800, FastAttack 800 | Shadowlands | drop (+ optional glyph upgrade) - Shadowlands - sided mobs (UNREDEEMED mobs drop the REDEEMED weapons and vice versa) |
| 212871 | Inamorata Enel Rapier | 100-300 | expansion | Piercing 1600 | Brawl 960 | Shadowlands | drop (+ optional glyph upgrade) - Shadowlands - sided mobs (UNREDEEMED mobs drop the REDEEMED weapons and vice versa) |
| 212861 | Inamorata Enel-Thar Rapier | 100-300 | expansion | Piercing 1600 | FastAttack 800, Brawl 960 | Shadowlands | drop (+ optional glyph upgrade) - Shadowlands - sided mobs (UNREDEEMED mobs drop the REDEEMED weapons and vice versa) |
| 212866 | Inamorata Enel-Xum Rapier | 100-300 | expansion | Piercing 1600 | Brawl 960, Dimach 320 | Shadowlands | drop (+ optional glyph upgrade) - Shadowlands - sided mobs (UNREDEEMED mobs drop the REDEEMED weapons and vice versa) |
| 212891 | Inamorata Rapier | 100-300 | expansion | Piercing 1600 | - | Shadowlands | drop (+ optional glyph upgrade) - Shadowlands - sided mobs (UNREDEEMED mobs drop the REDEEMED weapons and vice versa) |
| 212881 | Inamorata Thar Rapier | 100-300 | expansion | Piercing 1600 | FastAttack 800 | Shadowlands | drop (+ optional glyph upgrade) - Shadowlands - sided mobs (UNREDEEMED mobs drop the REDEEMED weapons and vice versa) |
| 212876 | Inamorata Xum Rapier | 100-300 | expansion | Piercing 1600 | Dimach 320 | Shadowlands | drop (+ optional glyph upgrade) - Shadowlands - sided mobs (UNREDEEMED mobs drop the REDEEMED weapons and vice versa) |
| 212812 | Sancrosanct Aban Stiletto | 100-300 | expansion | Piercing 1600 | SneakAttack 800 | Shadowlands | drop (+ optional glyph upgrade) - Shadowlands - sided mobs (UNREDEEMED mobs drop the REDEEMED weapons and vice versa) |
| 212845 | Sancrosanct Aban-Shere Stiletto | 100-300 | expansion | Piercing 1600 | SneakAttack 800, FastAttack 800, Brawl 960 | Shadowlands | drop (+ optional glyph upgrade) - Shadowlands - sided mobs (UNREDEEMED mobs drop the REDEEMED weapons and vice versa) |
| 212839 | Sancrosanct Aban-Thar Stiletto | 100-300 | expansion | Piercing 1600 | SneakAttack 800, FastAttack 800 | Shadowlands | drop (+ optional glyph upgrade) - Shadowlands - sided mobs (UNREDEEMED mobs drop the REDEEMED weapons and vice versa) |
| 212822 | Sancrosanct Enel Stiletto | 100-300 | expansion | Piercing 1600 | Brawl 960 | Shadowlands | drop (+ optional glyph upgrade) - Shadowlands - sided mobs (UNREDEEMED mobs drop the REDEEMED weapons and vice versa) |
| 212834 | Sancrosanct Enel-Thar Stiletto | 100-300 | expansion | Piercing 1600 | FastAttack 800, Brawl 960 | Shadowlands | drop (+ optional glyph upgrade) - Shadowlands - sided mobs (UNREDEEMED mobs drop the REDEEMED weapons and vice versa) |
| 212827 | Sancrosanct Enel-Xum Stiletto | 100-300 | expansion | Piercing 1600 | Brawl 960, Dimach 320 | Shadowlands | drop (+ optional glyph upgrade) - Shadowlands - sided mobs (UNREDEEMED mobs drop the REDEEMED weapons and vice versa) |
| 212800 | Sancrosanct Stiletto | 100-300 | expansion | Piercing 1600 | - | Shadowlands | drop (+ optional glyph upgrade) - Shadowlands - sided mobs (UNREDEEMED mobs drop the REDEEMED weapons and vice versa) |
| 212817 | Sancrosanct Thar Stiletto | 100-300 | expansion | Piercing 1600 | FastAttack 800 | Shadowlands | drop (+ optional glyph upgrade) - Shadowlands - sided mobs (UNREDEEMED mobs drop the REDEEMED weapons and vice versa) |
| 212805 | Sancrosanct Xum Stiletto | 100-300 | expansion | Piercing 1600 | Dimach 320 | Shadowlands | drop (+ optional glyph upgrade) - Shadowlands - sided mobs (UNREDEEMED mobs drop the REDEEMED weapons and vice versa) |
| 165133 | Slayerdroid Crystal Claw | 200 | high-rk | Piercing 1111 | FastAttack 556, Brawl 667 | - | unknown |
| 244778 | Lady of Deceit | 299 | TL6 | Piercing 2250, Parry 900, Riposte 900 | SneakAttack 1125, Dimach 450 | - | drop - The Beast, Pandemonium (Shadowlands) |
| 244779 | Lord of Deceit | 300 | TL6 | Piercing 2250, Parry 900, Riposte 900 | SneakAttack 1125, Dimach 450 | - | drop - The Beast, Pandemonium (Shadowlands) |
| 212785 | Inamorata Aban Trident | 100-300 | expansion | Piercing 1440, MeleeEnergy 960 | SneakAttack 800 | Shadowlands | drop (+ optional glyph upgrade) - Shadowlands - sided mobs (UNREDEEMED mobs drop the REDEEMED weapons and vice versa) |
| 212765 | Inamorata Aban-Shere Trident | 100-300 | expansion | Piercing 1440, MeleeEnergy 960 | SneakAttack 800, FastAttack 800, Brawl 960 | Shadowlands | drop (+ optional glyph upgrade) - Shadowlands - sided mobs (UNREDEEMED mobs drop the REDEEMED weapons and vice versa) |
| 212770 | Inamorata Aban-Thar Trident | 100-300 | expansion | Piercing 1440, MeleeEnergy 960 | SneakAttack 800, FastAttack 800 | Shadowlands | drop (+ optional glyph upgrade) - Shadowlands - sided mobs (UNREDEEMED mobs drop the REDEEMED weapons and vice versa) |
| 212780 | Inamorata Enel Trident | 100-300 | expansion | Piercing 1440, MeleeEnergy 960 | Brawl 960 | Shadowlands | drop (+ optional glyph upgrade) - Shadowlands - sided mobs (UNREDEEMED mobs drop the REDEEMED weapons and vice versa) |
| 212775 | Inamorata Enel-Thar Trident | 100-300 | expansion | Piercing 1440, MeleeEnergy 960 | FastAttack 800, Brawl 960 | Shadowlands | drop (+ optional glyph upgrade) - Shadowlands - sided mobs (UNREDEEMED mobs drop the REDEEMED weapons and vice versa) |
| 212790 | Inamorata Thar Trident | 100-300 | expansion | Piercing 1440, MeleeEnergy 960 | FastAttack 800 | Shadowlands | drop (+ optional glyph upgrade) - Shadowlands - sided mobs (UNREDEEMED mobs drop the REDEEMED weapons and vice versa) |
| 212795 | Inamorata Trident | 100-300 | expansion | Piercing 1440, MeleeEnergy 960 | - | Shadowlands | drop (+ optional glyph upgrade) - Shadowlands - sided mobs (UNREDEEMED mobs drop the REDEEMED weapons and vice versa) |
| 212739 | Sancrosanct Aban Spear | 100-300 | expansion | Piercing 1440, MeleeEnergy 960 | SneakAttack 800 | Shadowlands | drop (+ optional glyph upgrade) - Shadowlands - sided mobs (UNREDEEMED mobs drop the REDEEMED weapons and vice versa) |
| 212760 | Sancrosanct Aban-Shere Spear | 100-300 | expansion | Piercing 1440, MeleeEnergy 960 | SneakAttack 800, FastAttack 800, Brawl 960 | Shadowlands | drop (+ optional glyph upgrade) - Shadowlands - sided mobs (UNREDEEMED mobs drop the REDEEMED weapons and vice versa) |
| 212755 | Sancrosanct Aban-Thar Spear | 100-300 | expansion | Piercing 1440, MeleeEnergy 960 | SneakAttack 800, FastAttack 800 | Shadowlands | drop (+ optional glyph upgrade) - Shadowlands - sided mobs (UNREDEEMED mobs drop the REDEEMED weapons and vice versa) |
| 212734 | Sancrosanct Enel Spear | 100-300 | expansion | Piercing 1440, MeleeEnergy 960 | Brawl 960 | Shadowlands | drop (+ optional glyph upgrade) - Shadowlands - sided mobs (UNREDEEMED mobs drop the REDEEMED weapons and vice versa) |
| 212750 | Sancrosanct Enel-Thar Spear | 100-300 | expansion | Piercing 1440, MeleeEnergy 960 | FastAttack 800, Brawl 960 | Shadowlands | drop (+ optional glyph upgrade) - Shadowlands - sided mobs (UNREDEEMED mobs drop the REDEEMED weapons and vice versa) |
| 212729 | Sancrosanct Spear | 100-300 | expansion | Piercing 1440, MeleeEnergy 960 | - | Shadowlands | drop (+ optional glyph upgrade) - Shadowlands - sided mobs (UNREDEEMED mobs drop the REDEEMED weapons and vice versa) |
| 212745 | Sancrosanct Thar Spear | 100-300 | expansion | Piercing 1440, MeleeEnergy 960 | FastAttack 800 | Shadowlands | drop (+ optional glyph upgrade) - Shadowlands - sided mobs (UNREDEEMED mobs drop the REDEEMED weapons and vice versa) |
| 231103 | Sigd X | 300 | high-rk | Piercing 1800, Parry 720, Riposte 720 | SneakAttack 900, Dimach 360 | - | shop - Shadowlands garden shops (Scheol and Adonis normal gardens) |
| 160449 | Kiddy | 200 | high-rk | Piercing 986 | FastAttack 493, Brawl 592, Dimach 197 | - | unknown |
| 158300 | Pitchfork of Red Terror | 200 | high-rk | Piercing 1083 | Brawl 650 | - | unknown |
| 158843 | Stereotypical Dragon Tooth Poker | 200 | high-rk | Piercing 940 | Brawl 564 | - | unknown |
| 121683 | Spear of the Night | 200 | high-rk | Piercing 880, 2hEdged 587 | SneakAttack 489, Brawl 587, Dimach 196 | - | unknown |
| 123018 | Premium Notum Spear | 200 | high-rk | Piercing 1072 | SneakAttack 536, Brawl 643, Dimach 214 | - | unknown |
| 211196 | Obfuscating Dagger of Night | 300 | expansion | Piercing 1590 | SneakAttack 795, FastAttack 795, Dimach 318 | Shadowlands | unknown |

## MartialArts (30 models, IP cost 1.6)

| id | Model | QL | Tier | Wield | Specials | Expansion | Where |
|---|---|---|---|---|---|---|---|
| 301885 | X1-R4 Viral Force-Blades | 300 | expansion | MartialArts 2199 | Brawl 1229, Dimach 659, SneakAttack 999 | AlienInvasion | tradeskill - Alien Playfield (APF) - upgrade of the Blades of Boltar |
| 293993 | Kuma Tonfa - Left Hand | 300 | TL6 | MartialArts 2250 | FastAttack 1125, Brawl 1350, Dimach 450 | LegacyOfTheXan | unknown |
| 293996 | Kuma Tonfa - Right Hand | 300 | TL6 | MartialArts 2250 | FastAttack 1125, Brawl 1350, Dimach 450 | LegacyOfTheXan | unknown |
| 267258 | Dreadloch Shen Sticks | 300 | expansion | MartialArts 2100 | Brawl 1300, Dimach 650 | LostEden | drop - Dreadloch camps (Lost Eden), level 210-220 |
| 257147 | Blades of Boltar | 300 | expansion | MartialArts 1999 | Brawl 1334, Dimach 444 | AlienInvasion | tradeskill - Alien Invasion - built from a QL250+ Kyr'Ozch Energy Rapier Type 992 |
| 288664 | Kyr'Ozch Nunchacko | 1-300 | expansion | MartialArts 2000 | - | AlienInvasion | drop - Alien city raids / Alien Mothership |
| 288671 | Kyr'Ozch Nunchacko - Type 48 | 1-300 | expansion | MartialArts 2000 | Brawl 1200, Dimach 400 | AlienInvasion | tradeskill - Alien city raids / Alien Mothership (base weapon + clump) |
| 302948 | Corrupted Lady of Wisdom | 300 | TL7 | MartialArts 1999 | FastAttack 999, Brawl 1099, Dimach 450 | - | drop - Pyramid of Home / "Dark Pyramid Below" (6-man, level 201-220) |
| 302946 | Corrupted Lord of Wisdom | 300 | TL7 | MartialArts 1999 | FastAttack 999, Brawl 1099, Dimach 450 | - | drop - Pyramid of Home / "Dark Pyramid Below" (6-man, level 201-220) |
| 271047 | Nano-Charged Stun Glove - 870 | 1-300 | high-rk | MartialArts 1500 | Brawl 900, FastAttack 750, Dimach 300 | - | shop - General stores (for example Fair Trade) |
| 271039 | Nano-Charged Stun Glove - 000 | 1-300 | high-rk | MartialArts 1500 | - | - | tradeskill - Crafted with Basic Upgrade Kits from Engineer Heath Bridges, Holes in the Wall |
| 271041 | Nano-Charged Stun Glove - 010 | 1-300 | high-rk | MartialArts 1500 | Brawl 900 | - | tradeskill - Crafted with Basic Upgrade Kits from Engineer Heath Bridges, Holes in the Wall |
| 271043 | Nano-Charged Stun Glove - 050 | 1-300 | high-rk | MartialArts 1500 | Brawl 900, FastAttack 750 | - | tradeskill - Crafted with Basic Upgrade Kits from Engineer Heath Bridges, Holes in the Wall |
| 271045 | Nano-Charged Stun Glove - 070 | 1-300 | high-rk | MartialArts 1500 | Brawl 900, FastAttack 750, Dimach 300 | - | tradeskill - Crafted with Basic Upgrade Kits from Engineer Heath Bridges, Holes in the Wall |
| 294001 | Lady of Wisdom | 300 | high-rk | MartialArts 2250 | FastAttack 1125, Brawl 1350, Dimach 450 | - | drop - The Beast, Pandemonium (Shadowlands) |
| 293999 | Lord of Wisdom | 300 | high-rk | MartialArts 2250 | FastAttack 1125, Brawl 1350, Dimach 450 | - | drop - The Beast, Pandemonium (Shadowlands) |
| 212392 | Inamorata Enel Swatter | 100-300 | expansion | MartialArts 1440, 1hBlunt 960 | Brawl 960 | Shadowlands | drop (+ optional glyph upgrade) - Shadowlands - sided mobs (UNREDEEMED mobs drop the REDEEMED weapons and vice versa) |
| 212398 | Inamorata Enel-Xum Swatter | 100-300 | expansion | MartialArts 1440, 1hBlunt 960 | Brawl 960, Dimach 320 | Shadowlands | drop (+ optional glyph upgrade) - Shadowlands - sided mobs (UNREDEEMED mobs drop the REDEEMED weapons and vice versa) |
| 212387 | Inamorata Swatter | 100-300 | expansion | MartialArts 1440, 1hBlunt 960 | - | Shadowlands | drop (+ optional glyph upgrade) - Shadowlands - sided mobs (UNREDEEMED mobs drop the REDEEMED weapons and vice versa) |
| 212403 | Inamorata Xum Swatter | 100-300 | expansion | MartialArts 1440, 1hBlunt 960 | Dimach 320 | Shadowlands | drop (+ optional glyph upgrade) - Shadowlands - sided mobs (UNREDEEMED mobs drop the REDEEMED weapons and vice versa) |
| 212413 | Sacrosanct Enel Nunchaku | 100-300 | expansion | MartialArts 1440, 1hBlunt 960 | Brawl 960 | Shadowlands | drop (+ optional glyph upgrade) - Shadowlands - sided mobs (UNREDEEMED mobs drop the REDEEMED weapons and vice versa) |
| 212418 | Sacrosanct Enel-Xum Nunchaku | 100-300 | expansion | MartialArts 1440, 1hBlunt 960 | Brawl 960, Dimach 320 | Shadowlands | drop (+ optional glyph upgrade) - Shadowlands - sided mobs (UNREDEEMED mobs drop the REDEEMED weapons and vice versa) |
| 212408 | Sacrosanct Nunchaku | 100-300 | expansion | MartialArts 1440, 1hBlunt 960 | - | Shadowlands | drop (+ optional glyph upgrade) - Shadowlands - sided mobs (UNREDEEMED mobs drop the REDEEMED weapons and vice versa) |
| 212423 | Sacrosanct Xum Nunchaku | 100-300 | expansion | MartialArts 1440, 1hBlunt 960 | Dimach 320 | Shadowlands | drop (+ optional glyph upgrade) - Shadowlands - sided mobs (UNREDEEMED mobs drop the REDEEMED weapons and vice versa) |
| 160201 | Brand New Bang-Bang Glove | 200 | high-rk | MartialArts 917, 1hBlunt 611 | Brawl 611, Dimach 204 | - | unknown |
| 152357 | Lotus Parry Stick | 200 | high-rk | MartialArts 805, 1hBlunt 536 | Brawl 536, Dimach 179 | - | unknown |
| 211276 | Fist of Justice | 1-299 | expansion | MartialArts 1211, 1hEdged 807 | Brawl 807, Dimach 269 | Shadowlands | unknown |
| 211277 | True Fist of Justice | 300 | expansion | MartialArts 1215, 1hEdged 810 | Brawl 810, Dimach 270 | Shadowlands | unknown |
| 211234 | Flabbergaster | 1-299 | expansion | MartialArts 1256, Piercing 837 | SneakAttack 698, Brawl 837, Dimach 279 | Shadowlands | unknown |
| 211235 | Hard Flabbergaster | 300 | expansion | MartialArts 1260, Piercing 840 | SneakAttack 700, Brawl 840, Dimach 280 | Shadowlands | unknown |

## MeleeEnergy (18 models, IP cost 4)

| id | Model | QL | Tier | Wield | Specials | Expansion | Where |
|---|---|---|---|---|---|---|---|
| 254702 | Kyr'Ozch Energy Hammer | 1-300 | expansion | MeleeEnergy 2000 | - | AlienInvasion | drop - Alien city raids / Alien Mothership |
| 254709 | Kyr'Ozch Energy Hammer - Type 112 | 1-300 | expansion | MeleeEnergy 2000 | FastAttack 1000, Brawl 1200, Dimach 400 | AlienInvasion | tradeskill - Alien city raids / Alien Mothership (base weapon + clump) |
| 280729 | Dawn of the Xan | 300 | TL6 | MeleeEnergy 1799 | SneakAttack 899, FastAttack 899, Dimach 450 | LegacyOfTheXan | tradeskill - Legacy of the Xan team instances (device) + Pandemonium (base weapon) |
| 280730 | Dusk of the Xan | 300 | TL6 | MeleeEnergy 1799 | SneakAttack 899, FastAttack 899, Dimach 450 | LegacyOfTheXan | tradeskill - Legacy of the Xan team instances (device) + Pandemonium (base weapon) |
| 246244 | Sword of Dawn | 200 | high-rk | MeleeEnergy 999 | - | - | tradeskill - Pandemonium (Shadowlands) |
| 246256 | Sword of Dusk | 200 | high-rk | MeleeEnergy 999 | - | - | tradeskill - Pandemonium (Shadowlands) |
| 254744 | Kyr'Ozch Energy Sword | 1-300 | expansion | MeleeEnergy 2000 | - | AlienInvasion | drop - Alien city raids / Alien Mothership |
| 254751 | Kyr'Ozch Energy Sword - Type 76 | 1-300 | expansion | MeleeEnergy 2000 | FastAttack 1000, Brawl 1200 | AlienInvasion | tradeskill - Alien city raids / Alien Mothership (base weapon + clump) |
| 271422 | E-Blade - 850 | 1-300 | high-rk | MeleeEnergy 1500 | Brawl 900, FastAttack 750, Dimach 300 | - | unknown |
| 271416 | E-Blade - 000 | 1-300 | high-rk | MeleeEnergy 1500 | - | - | tradeskill - Crafted with Basic Upgrade Kits from Engineer Heath Bridges, Holes in the Wall |
| 271418 | E-Blade - 010 | 1-300 | high-rk | MeleeEnergy 1500 | Brawl 900 | - | tradeskill - Crafted with Basic Upgrade Kits from Engineer Heath Bridges, Holes in the Wall |
| 271420 | E-Blade - 050 | 1-300 | high-rk | MeleeEnergy 1500 | Brawl 900, FastAttack 750 | - | tradeskill - Crafted with Basic Upgrade Kits from Engineer Heath Bridges, Holes in the Wall |
| 160212 | Strong North Wind Katana | 200 | high-rk | MeleeEnergy 996 | FastAttack 498, Brawl 598, Dimach 199 | - | unknown |
| 211253 | Spirit Treasure of the Enigma | 300 | expansion | MeleeEnergy 1400 | - | Shadowlands | unknown |
| 211252 | Treasure of the Enigma | 1-299 | expansion | MeleeEnergy 1395 | - | Shadowlands | unknown |
| 249016 | Premium Bio-Energy Shield | 200 | expansion | MeleeEnergy 800 | FastAttack 400, Brawl 480, Dimach 160 | AlienInvasion | unknown |
| 211207 | Loathful Nipper | 1-299 | expansion | MeleeEnergy 1346 | SneakAttack 673, FastAttack 673 | Shadowlands | unknown |
| 211208 | Loathful Nipper of Below | 300 | expansion | MeleeEnergy 1350 | SneakAttack 675, FastAttack 675 | Shadowlands | unknown |

## Other sourced Piercing weapons that are not endgame-tier

- **Spear of Forbidden Ceremonies** (QL 250-300) - Inferno Catacomb Boss and Lord of the Void. Two-handed. Tradeable.
- **Notum-Infused Silvertail Dagger** (QL 1-15) - Craft: Snake Bile (Shining Coilers) + Handle-Shaped Gofle Toe Bone (Beit Gras, under the bridge) = Bone Handle; add a Silvertail Horn.
- **Metallic Mantis Predator Blade** (QL 120 common) - Free from the Mantis Den.
- **The -000 / -870 shop weapons (Slank Chop 000, Squibber 000, Stabber 000, Stabber - 870)** (QL up to ~200) - "Stabber - 870... buyable in general stores like Fairtrade". The true 000 weapons are crafted from rollable RK weapons with Basic Upgrade Kits, free and repeatable from Engineer Heath Bridges near the Bronto Burger in Holes in the Wall (QL180 kits, enough for QL200 output).

## Overlay sources

- http://wiki.aodb.us/wiki/Alien_Loot
- http://wiki.aodb.us/wiki/Blades_of_Boltar
- http://wiki.aodb.us/wiki/Escaped_Prisoner
- http://wiki.aodb.us/wiki/Patch_15.7
- http://wiki.aodb.us/wiki/Steps_of_Madness
- http://wiki.aodb.us/wiki/The_Reck
- http://wiki.aodb.us/wiki/Turn_Spirit_weapons
- http://wiki.aodb.us/wiki/Xan_Weapon_Upgrade
- https://forums.funcom.com/t/new-shade-coming-back/120089
- https://forums.funcom.com/t/shade-rk2019-weapons/83733
- https://forums.funcom.com/t/shade-weapon-help/41635
- https://wiki.aodb.us/index.php?title=Shade:Weapons&action=raw
- https://wiki.aodb.us/wiki/OFAB_Weapon_Upgrades
- https://www.ao-universe.com/forum/viewtopic.php?t=4652
- https://www.ao-universe.com/guides/alien-invasion/quests-guides/sector-07-alien-playfield
- https://www.ao-universe.com/guides/alien-invasion/tradeskill-guides-3/general-crafting-2/apf-loot-tradeskills
- https://www.ao-universe.com/guides/alien-invasion/tradeskill-guides-3/weapon-2/upgrading-alien-weapons
- https://www.ao-universe.com/guides/classic-ao/encounter-guides-3/low-level-encounter/condemned-subway
- https://www.ao-universe.com/guides/classic-ao/tradeskill-guides-6/weapon-4/crude-upgrades
- https://www.ao-universe.com/guides/shadowlands/professions-guides/comprehensive-shade-guide
- https://www.ao-universe.com/guides/shadowlands/quests-guides-2/elysium/one-who-wins-over-mind
- https://www.ao-universe.com/guides/shadowlands/tradeskill-guides-5/weapon-3/inamorata-and-sacrosanct-weapons
- https://www.ao-universe.com/guides/shadowlands/tradeskill-guides-5/weapon-3/turn-spirit-weapons
