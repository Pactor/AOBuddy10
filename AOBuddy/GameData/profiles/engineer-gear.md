# Engineer — Gear Reference (AOBuddy10)

Profession 3. Companion to `engineer-gear.json`. Symbiant unit line: **Control**.

Every id, QL range, slot, equip requirement, profession/breed/side lock, NODROP flag and stat bonus below
was read out of `items.ocp` (OMNICELL-CONTENT v3, 120,842 templates, client 18.8.50_EP1) with
`tools/eng-gear-extractor --prof 3 --wield`. That run returned **68,637 wearable templates, 61,510 usable
by profession 3, and 132 models hard-locked to `Profession EqualTo 3`**. All 119 id→name pairs verified
against `itemnames.sql`, 0 mismatches.

Bots → `engineer-pets.json`. Implants → `engineer-implants.json`. Symbiants → `engineer-symbiants.json`.
Weapons → `engineer-weapons.json`. Not repeated here.

---

## The only question that matters

**Does it raise Matter Creation and Time & Space *together*?** Every bot summon reads both at the same
value, so the bot — which is the Engineer's damage, tanking and aggro — is a direct function of that pair.

After that: ACs / Max Health / Nano Resist (there is **no self-heal**), Max Nano / Nano Pool / nano-cost
(re-summoning and re-auraing is expensive), Max NCU (the buff stack is large), and the nine trade skills
this class is the best in the game at.

---

## Progression at a glance

| Band | Wear | Why |
|---|---|---|
| ~20–100 | **Carbonum** (162426–162436) — and you build it yourself | +8 Nano C. Init and +8 NCU per piece |
| ~100–220 (SL) | **First / Second Tier Engineer**, then **Chosen** (Omni) / **Faithful** (Clan) | free profession set; Chosen/Faithful body carries **Material Creation +25** and the Helper (back) **Mech.E +60** |
| any level | **Miy's Nano** (268833–268845, cloak 270329) | **no level, expansion or profession gate at all**; Space Time on the body, Material Creation on the gloves, Nano C. Init +50, nano cost −5 % |
| ~175–220 | **Azure Reveries** | −10 % nano cost on the breastplate, lots of NCU |
| endgame (LE) | **Ofab Engineer**, upgraded ×2 | the Victory-Point baseline; helmet carries **Space Time +16** and **Nano C. Init +60** |
| endgame (AI) | **Combined Officer's** or **Combined Scout's** | **all six nano schools +30** on seven slots — +210 to both bot skills from armour alone |

---

## Ofab Engineer — and how differently it reads from the Soldier's

Engineer only (`Profession EqualTo 3`), Lost Eden, QL300 needs **Intelligence 1045 / Psychic 855**.
Tiers `Ofab` → `Improved` → `Penultimate`, upgraded with **Kyr'Ozch Bio-Material clumps**, not VP.

| Slot | Ofab id | What it gives at QL300 |
|---|---|---|
| Head | 264575 | ACs 1418, **Nano C. Init +60**, Ranged Init +60, Intelligence +20, **Space Time +16** |
| Body | 264596 | ACs 1575, **Max Nano +750**, Weapon Smithing +50, Grenade +50, NCU +25 |
| Arms | 264590 | ACs 473, Mech.E +25, Elec.E +25, Ranged Init +25, Matter Metamorphosis +8 |
| Hands | 264581 | ACs 499, **Chemistry +35**, Pistol +23, Psychological Modification +13 |
| Legs | 264608 | ACs 945, Computer Literacy +25, Fling Shot +20, NCU +15 |
| Feet | 264602 | ACs 630, **Elec.E +40, Quantum FT +40**, Sensory Improvement +20, Run Speed +40 |
| Back | **267936** (QL300 only) | ACs **2000**, Health +700, **Mech.E +70**, **Add All Def. +50**, NCU +25 |
| Shoulder | **268003** (QL300 only) | Health +400, NCU +25, Bio Metamorphosis +15, Add All Def. +10 |

Where the Soldier's Ofab gloves give Assault Rifle / Full Auto / MG-SMG, the Engineer's give **Chemistry
+35**; where the Soldier's boots give Ranged Energy, the Engineer's give **Elec.E +40 and Quantum FT +40**.
Funcom built the Engineer set around the tradeskill half of the class.

> ⚠ **The Special Edition helmet (`267369`) is NOT an automatic upgrade for an Engineer.** It gains ACs,
> Health, Nano C. Init (+100 vs +60), Nano Resist +25, Add All Def. +25 and Crit Resist +3 — but it
> **loses Intelligence +20 and Space Time +16**. If you are close to the next bot's MC/TS requirement,
> read both blocks before you spend the Victory Points. (A Soldier has no such dilemma — theirs is strictly
> better.)

---

## Combined Officer's vs Combined Scout's

Neither is profession-locked — there is no `Profession EqualTo` clause on Combined armour. What makes
Officer's "the Engineer's set" is the stat spread.

| | Officer's (Arithmetic + Spiritual) | Scout's (Arithmetic + Observant) |
|---|---|---|
| Abilities @QL300 | **Int 1000 / Psy 1000** | **Int 1000 / Sense 1000** |
| Nano schools | all six **+30** | all six **+30** |
| Trade skills | nine **+30** | nine **+30** |
| Extra | Max Nano +200, Nano Pool +50, NCU +15, **nano cost −3 %** | **+40 all three inits**, **+30 all three evades**, Perception, Concealment, B&E, Trap Disarm |

Scout's is under-rated for an Engineer specifically: **Evade-ClsC costs 4.0** and Dodge-Rng 2.5 for this
class (`engineer-build.json` calls close-combat evades "effectively unaffordable"), so +30 to each from
armour is worth more here than to almost anyone else.

---

## The Engineer-only items most people miss

- **Liquid Notum NCU TC-03 Prototype** (`215232`) — belt deck slot. **Max NCU +72** (more than an
  Accelerated NCU Memory's +64) **plus Mech.E +10 and Space Time +4**. Engineer only, Shadowlands,
  Computer Literacy 1700.
- **Modified A-4000 NCU-sheet** (`152270`) — *head slot*, **Max NCU +40** and Nano Programming +40, for
  Computer Literacy 900 and **no level or expansion gate**. Best NCU-per-slot before endgame.
- **Spirit Tech Circlet of Yadmaron** (`235398`) — head. **Max Health +750**, Max Nano +250, ACs +100,
  Ranged Init −60. For a class with no self-heal, +750 health on a hat is enormous, and the Ranged Init
  penalty only touches *your* gun, never the bot.
- **Rusty's Ring of Bolts** (`267566`) — **Space Time +20 and Material Creation +20** on one ring, plus
  Add All Def. +30, Nano Resist +25, Comp. Lit. +20, Matter Metamorphosis +20. Level 220, Lost Eden.
- **Phatmos' Booty** (`246110`) — neck. **Add All Def. +75**, Body Dev. +75, Run Speed +75,
  Concealment +75, **Grenade +75**, ACs +500. Title Level 7, Computer Literacy 1500.
- **Ring of Crawling Ants** (`165477`) — **Evade-ClsC +42** at level 130, on a class that pays 4.0 for it.
- **Breastplate of Technical Ceremonies** (`165427`) — a pure tradeskill body piece with **no expansion
  gate**: Mech.E +20, Weapon Smithing +20, Elec.E +20, NP +10, Chemistry +10, Comp. Lit. +10.
- **Apprehensive Spirit Pitcher** (`246095`) — a HUD item with a *damage shield* on it (Chemical +5).
- **Custom-Made Engineer Ring of Tinkering** (`163672`) — Tutoring +36 and four trade skills at +24.

> ⚠ **De'Valos Sleeves (`253184`) is NANOMAGE ONLY.** Its `ToWear` list contains `Breed EqualTo 3`. It is
> the obvious "+30 to all six nano schools on an arm slot" item and a non-Nanomage Engineer simply cannot
> wear it. (The sibling `metaphysicist-gear.json` lists it without that caveat.)

> ℹ **Correction to a sibling file:** `metaphysicist-gear.json` states Miy's is "NOT present in the local
> itemnames.sql extract". That is wrong — all 35 Miy's models are in `items.ocp` and `itemnames.sql`, and
> the Nano-variant ids are listed in `engineer-gear.json`.

---

## Honest coverage

7 armour sets (every Engineer-locked set in the client data plus Combined Officer's, Combined Scout's,
Miy's Nano, Azure Reveries and Carbonum), 11 headwear entries, 12 NCU/belt entries and 23 misc/HUD/ring
entries. This is the **curated** best-in-slot/progression layer — **not** the exhaustive per-slot catalogue
`CLASS-PROFILE-PLAYBOOK` §9 demands (that would be all 61,510 Engineer-usable wearable templates; an
earlier pass generated a partial one into scratch but it was never written to a profile file).

Nine things are left in `_unverified`, chiefly *where* Azure Reveries, the Spirit Tech Circlet, Hood of
Black Waters, Phatmos' Booty, the Technical Ceremonies pieces and the Jobe Engineer Support System
actually come from, and the Miy's drop camps. Their requirements and bonuses are exact; their sources are
marked unknown rather than guessed.

---

## Sources

**Local:** `items.ocp` via `tools/eng-gear-extractor --prof 3 --wield` · `itemnames.sql` ·
`engineer-nanos.json` (the NanoFocusLevel gates and bot cast requirements) · siblings
`engineer-endgame.json`, `engineer-build.json`, `engineer-pets.json`, `engineer-symbiants.json`,
`engineer-weapons.json`.

**Web:** [Engineer:Armor](http://wiki.aodb.us/wiki/Engineer:Armor) ·
[Armorsets](http://wiki.aodb.us/wiki/Armorsets) · [Alien Armor](http://wiki.aodb.us/wiki/Alien_Armor) ·
[The Beast](http://wiki.aodb.us/wiki/The_Beast) ·
[Miy's Nano Armor](https://wiki.aodb.us/wiki/Miy's_Nano_Armor) ·
[Engineer Guide MKIII](https://www.ao-universe.com/guides/classic-ao/profession-guides/engineer-guide-mkiii-13).

**Not used** (block automation, playbook §4): auno.org, aoitems.com, anarchyonline.fandom.com.
