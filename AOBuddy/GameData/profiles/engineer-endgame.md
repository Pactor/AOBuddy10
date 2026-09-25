# Engineer — Endgame (AOBuddy10)

Profession 3. Companion to `engineer-endgame.json`. Symbiant unit line: **Control**.

Everything Engineer-specific below — item ids, requirements, bonuses, profession locks, nano gates, the
symbiant line — was read fresh from `items.ocp` / `nanos.ocp` and verified against `itemnames.sql`
(77 distinct ids, all resolve, all names match). The **venue list** (which raids exist, their level bands
and locations) is aligned with the sibling `soldier-endgame.json`; see *Provenance* at the bottom.

---

## One sentence that explains the whole Engineer endgame

**Every bot summon reads Matter Creation *and* Time & Space at the same value.** The Ravening M-60
(`275815`) wants **MC 1960 / TS 1958**. So the Engineer's endgame shopping list is not "what raises my
damage" — it is "what raises MC and TS", because that *is* the damage.

---

## The four things that actually matter

### 1. The Engineer Nanodeck is a **wall**, not an upgrade

Seven Engineer nanos carry `NanoFocusLevel op22 64` in their cast requirements, and that bit is set only
by a worn Engineer Nanodeck:

| Nano | id | What you lose without it |
|---|---|---|
| **Ravening M-60** | 275815 | the best bot in the game (MC 1960 / TS 1958) |
| Improved Isochronal Sloughing Combat Field | 273343 | the top special-attack absorber |
| Empowered Pre-Nullity Cocoon | 273341 | the top reflect cocoon |
| Improved Shield of the Obedient Servant | 270790 | the top pet shield |
| **Engineer Composite Specialist Tradeskills (8 hours)** | 273346 | +200 Mech.E / Elec.E / Quantum FT |
| Formula 22 | 275016 | — |
| Intrusive Aura of Slave | 275835 | — |

Without the deck your best bot is the **Widowmaker Battle Drone** (`223323`, bot level 220). The deck
comes from **Dust Brigade 1** (Peacekeeper Constad, level 205+). **This is the highest-priority piece of
endgame content for the class.** A worn deck is also one of the three entry tickets to the Pyramid of Home.

### 2. The Stellar deck is a **bot** upgrade

`Engineer Nanodeck` (270755) + `Nanodeck Activation Device` (280785 / 281157, from Mitaar Hero / Vortexx /
the Xan 12-man) → `Activated Engineer Nanodeck` (280761)
→ + `Star of Ingenuity` (244692, the Beast or *Guarding the Source*) → **`Stellar Engineer Nanodeck` (280775)**.

The Stellar deck gives **Matter Creation +30 and Time & Space +30** on wear, plus Mech.E +60, Weapon
Smithing +60, Tutoring +60, and Grenade / Pistol / Shotgun / Ranged Energy / Martial Arts / Chemistry +30.
It is the biggest single item in the Engineer's game.

### 3. **Sloth of the Xan** is an Engineer weapon

From the 14-entry table on [Xan_Weapon_Upgrade](http://wiki.aodb.us/wiki/Xan_Weapon_Upgrade), two entries
land in Engineer skills:

| Lord/Lady weapon | → Xan weapon | Skill | What it gives |
|---|---|---|---|
| Lord/Lady of **Sloth** (244912 / 244911) | **Sloth of the Xan** `280727` | **Grenade** | **MC +30, TS +30**, Add All Off. +30, Max Health +600 |
| Lord/Lady of **Lust** (244914 / 244913) | **Lust of the Xan** `280728` | **Pistol** | Nano C. Init +50, Ranged Init +50, AAO +15, Health +300 |

Sloth of the Xan is a *weapon that makes your bot bigger*. This is where the Engineer's answer diverges
hardest from the Soldier's (Anger of the Xan, Assault Rifle).

### 4. Alien armour: **Combined Officer's**, not Commando's

| Set | Abilities @QL300 | What it gives |
|---|---|---|
| **Combined Officer's** (Arithmetic + Spiritual) | Int 1000 / Psy 1000 | **all six nano schools +30**, nine trade skills +30, Max Nano +200, Nano Pool +50, NCU +15, nano cost −3 % |
| **Combined Scout's** (Arithmetic + Observant) | Int 1000 / Sense 1000 | same nano + trade skills, **+40 all three inits**, +30 all evades / Perception / Concealment / B&E / Trap Disarm |

Both verified from `items.ocp`. Do **not** copy the Soldier's answer (Strong + Supple = Commando's).

---

## Progression order

1. **MC and TS in lockstep** — a bot checks both at the same number; one point of MC above TS is wasted.
2. **Base implants → Control symbiants.** Control is the *only* line an Engineer can wear (the stat-60
   lock set is `{Engineer, Trader, Bureaucrat, Meta-Physicist}` and Engineer appears in no other line).
3. **Ofab Engineer armour** via Battlestation Victory Points, upgraded twice with bio-material.
4. **Research, AI levels, perks** — Champion of Nano Combat is +100 to all six nano schools, i.e. +100 MC
   *and* +100 TS. Title Level 6 gates QL300 Combined armour.
5. **Dust Brigade 1 → the Engineer Nanodeck.**
6. **Combined Officer's** (or Scout's).
7. **Hold Hell at Bay ring** (`231234`) → Pandemonium → the Marauder M-45 (`223337`) and Widowmaker
   (`223323`) crystals from the Pandemonium vendors.
8. **The Beast → Star of Ingenuity → Stellar Nanodeck.**
9. **Sloth of the Xan** (or Lust of the Xan).
10. **Xan Control Betas → Intelligent QL300 → Alphas** (NODROP, crafted on the Engineer that wears them).
11. Long tail: DB2/DB3, Arid Rift, Neretva, Sector 42, the Engineer-only oddments.

---

## Where the Engineer's own loot comes from

| Content | Engineer-specific payoff |
|---|---|
| **Dust Brigade 1** | **Engineer Nanodeck** `270755`; Dust Brigade Engineer Pistol `274559` / Infused `274560` |
| **The Beast** | **Star of Ingenuity** `244692`; Lord/Lady of **Sloth** and **Lust** |
| Mitaar Hero / Vortexx / Xan 12-man | Nanodeck Activation Device `280785` / `281157`; Xan Weapon Upgrade Device |
| **Xan 12-man** | the *only* source of **Control** Xan Betas |
| **Pandemonium** | Marauder M-45 `223337` and **Widowmaker Battle Drone** `223323` crystals; Intelligent QL300 Control symbiants (Night Heart) |
| **Inferno vendors** | Predator M-30 `223333`, Fieldsweeper Devastator Drone `223319`, Desolator Assault Drone `223321` |
| **Battlestations** | the Ofab Engineer set; **Ofab Peregrine** (a *Pistol*, Mk 1 `265020` → Mk 6 `265061`) |
| Sector 7 | **Special Edition Kyr'Ozch Grenade Gun** `288296` (Engineer-locked) |
| Alien raids | bio-material + the Arithmetic/Spiritual viralbots for Combined Officer's |

> ⚠ **Ofab Peregrine is the Engineer's Ofab weapon.** Ofab Shark is an Assault Rifle (Soldier), Ofab Cobra
> a Rifle, Ofab Silverback a Shotgun, Ofab Panther 1h Blunt — all verified from the attack-skill field.
> Do not buy by name.

---

## Things easy to get wrong

- **Only one aura at a time** — a friendly Sympathetic aura (strain 227, 15 nanos) *or* a hostile
  Disruptive aura (strain 236, 14 nanos), never both. Pick per encounter.
- **The Engineer has no self-heal at all.** It heals its *bot*. Personal survival is Bio Shielding perk
  actions, the reflect/absorb cocoons, the 14 special-attack absorbers and raw HP.
- **Never summon while nano-debuffed** — the bot is built at the skill level you had when you cast it,
  and it persists. The trick runs the other way: summon *under* a wrangle and keep the over-equipped bot.
  AODB's control rule: *skill required to control = skill to make or wear × 0.8*.
- **Bio-material, not VP, pays for Ofab upgrades** (clumps at ≥80 % of the item QL).
- **Position so the bot can reach, and you cannot.** Pet commands reach 50 m; bot run speed is your run
  speed × 1.2.

---

## Provenance and what is *not* claimed

The **venue list** — which raids exist, their level bands, locations and general loot structure — is
carried across from the sibling `soldier-endgame.json`, which was built by reading the AO-Universe and
AODB pages listed in `_sources`. While writing *this* file, the following were re-read directly:
**The Beast**, **Xan Weapon Upgrade**, **Pandemonium**, **Alien Armor**, **Armorsets**, **Hunting Grounds**.

Nine items are left in `_unverified`, including: Sector 10's level band, the bot-crystal vendor locations
(from `engineer-pets.json`, AODB-sourced, not re-verified), the exact semantics of the `operator 34`
requirement that links the Stellar deck to the Star, where the "Corrupted" Lord of Sloth/Lust come from,
and the source of the Sealed Profession Nanodeck (`302080`). **No drop rates are asserted anywhere** —
the client data has no drop tables.

---

## Sources

**Local:** `items.ocp` via `tools/eng-gear-extractor --prof 3 --wield` plus a direct reader for the
Nanodeck/Star `ToWield` action lists · `engineer-nanos.json` (288 nanos; the seven NanoFocusLevel gates) ·
`itemnames.sql` · siblings `engineer-pets.json`, `engineer-symbiants.json`, `engineer-build.json`,
`soldier-endgame.json`.

**Web:** [The Beast](http://wiki.aodb.us/wiki/The_Beast) ·
[Xan Weapon Upgrade](http://wiki.aodb.us/wiki/Xan_Weapon_Upgrade) ·
[Alien Armor](http://wiki.aodb.us/wiki/Alien_Armor) · [Armorsets](http://wiki.aodb.us/wiki/Armorsets) ·
[Pandemonium](https://www.ao-universe.com/guides/shadowlands/quests-guides-2/pandemonium/pandemonium-2) ·
[The Xan 12-man](https://www.ao-universe.com/guides/legacy-of-the-xan/encounter-guides-2/the-xan-12-man) ·
[Mitaar Hero](https://www.ao-universe.com/guides/legacy-of-the-xan/encounter-guides-2/the-alien-threat) ·
[Vortexx](https://www.ao-universe.com/guides/legacy-of-the-xan/encounter-guides-2/lox-ground-chief-vortexx) ·
[Dust Brigade](https://www.ao-universe.com/guides/classic-ao/quests-guides-3/dust-brigade-peacekeeper-constad) ·
[Battlestations](https://www.ao-universe.com/guides/classic-ao/location-guides-5/battle-stations) ·
[Sector 7](https://www.ao-universe.com/guides/alien-invasion/quests-guides/sector-07-alien-playfield) ·
[Engineer Guide](http://wiki.aodb.us/wiki/Engineer_Guide) ·
[Engineer Guide MKIII](https://www.ao-universe.com/guides/classic-ao/profession-guides/engineer-guide-mkiii-13).

**Not used** (block automation, playbook §4): auno.org, aoitems.com, anarchyonline.fandom.com.
