# Soldier — Gear Reference (AOBuddy10)

Profession 1. Companion to `soldier-gear.json`. Symbiant unit type: **Artillery**.

Every id, QL range, slot, equip requirement, profession lock, NODROP flag and stat bonus below was read
out of `items.ocp` (OMNICELL-CONTENT v3, 120,842 templates, client 18.8.50_EP1) with
`tools/eng-gear-extractor --prof 1 --wield`. That run returned **68,637 wearable templates, 61,643 usable
by profession 1, and 166 models hard-locked to `Profession EqualTo 1`**. Every id→name pair was verified
against `itemnames.sql` (140 pairs, 0 mismatches). The web is used only for *where things come from*,
because the client data has requirements and bonuses but no drop tables.

Weapons → `soldier-weapons.json`. Implants → `soldier-implants.json`. Symbiants → `soldier-symbiants.json`.
Not repeated here.

---

## What a Soldier's gear is actually for

Assault Rifle, Burst, Full Auto, Fling Shot, **Ranged Init.**, Add All Off., damage modifiers, Max Health
and the Strength/Stamina/Agility that gate its own armour. A Soldier does **not** want caster armour: the
only nano skills it needs are Matter Creation and Time & Space, and those come cheaper from symbiants and
buffs than from armour.

---

## Progression at a glance

| Band | Wear | Why |
|---|---|---|
| ~20–80 | **Carbonum** (162426–162436) | cheap ACs and **+8 NCU per piece** |
| ~100–200 (SL) | **First / Second Tier Soldier Jobe** | free profession set, AC + Health |
| ~150–220 | **Miy's Ranged** (268860–268871, cloak 270340) | the only cheap set that puts AR / Rifle / Shotgun / RE / Bow / Pistol / MG-SMG on seven slots, +50 Ranged Init, +44 NCU |
| 180–200 froob | **Notum Infused Kevlar** (165141–165150) | Soldier-only, level-180 gate, the best froob profession armour that exists |
| 200+ (SL) | **Pernicious** (226453–226464) | every piece carries the same `+10 AR / MG-SMG / RE / Full Auto / Burst / Dodge-Rng / Ranged Init / Nano Resist` — partial sets are fine |
| 220 (SL) | **Chosen (Omni) / Faithful (Clan) Soldier** | tier 3; keep the Cuirass + Shoulderplate longest |
| endgame (LE) | **Ofab Soldier**, upgraded ×2 | the baseline; bought with **Victory Points** |
| endgame (AI) | **Combined Commando's** or **Sharpshooter's** | +30 to *every* weapon skill, or +40 to all three inits |

---

## Ofab Soldier — the numbers

Soldier only (`Profession EqualTo 1`), Lost Eden (`Expansion BitAnd 32`), QL300 needs
**Strength 900 / Stamina 1100**. Tiers: `Ofab` → `Improved` → `Penultimate`, upgraded with **Kyr'Ozch
Bio-Material clumps**, *not* with VP.

| Slot | Ofab id | Improved | Penultimate | What it gives at QL300 |
|---|---|---|---|---|
| Head | 264431 | 264429 | 264427 | ACs 1418, Health 400, **Ranged Init +50**, AAO +15 |
| Body | 264449 | 264447 | 264445 | ACs **1890**, Health 750, Stamina +25, AAO +15 |
| Arms | 264443 | 264441 | 264439 | ACs 578, **Burst +25, Aimed Shot +25, Multi Ranged +25**, Ranged Init +25 |
| Hands | 264437 | 264435 | 264433 | ACs 394, **Assault Rifle +50, Full Auto +50, MG/SMG +50** |
| Legs | 264461 | 264459 | 264457 | ACs 1392, **Assault Rifle +50, MG/SMG +50** |
| Feet | 264455 | 264453 | 264451 | ACs 709, Ranged Energy +50, Run Speed +40 |
| Back | **267939** (QL300 only) | — | — | ACs **2000**, NCU +25, Burst +60, **+5 to every Reflect AC** |
| Shoulder | **268186** (QL300 only) | — | — | Health 450, Ranged Init +50, Assault Rifle +25, AAO +10 |

> **Buy the Special Edition helmet (`267379`) instead of the normal one.** It beats even the Penultimate
> on every stat: ACs 1600 vs 1418, Health 750 vs 600, **Ranged Init 150 vs 70**, **Add All Off. 50 vs 20**,
> plus Strength +25 / Stamina +25. This is the "Special Edition Head" on the 220 VP checklist.

---

## Combined Commando's vs Combined Sharpshooter's

Neither is profession-locked — the ToWear list has **no** `Profession EqualTo` clause. What makes
Commando's "the Soldier's set" is the stat spread, not a lock.

|  | Commando's | Sharpshooter's |
|---|---|---|
| Recipe | Strong + Supple | Observant + Supple |
| Abilities at QL300 | **Strength 1000 / Agility 1000** | **Agility 1000 / Sense 1000** |
| Weapon skills | +30 to *every* melee **and** ranged skill incl. AR / Burst / Full Auto / Fling Shot | +30 to the ranged set |
| Extra | Add All Off. +15, Add All Def. +15, **+10 to all eight damage modifiers** | **+40 to all three initiatives**, evades, Perception, Concealment |

Take Sharpshooter's if you are short on initiative, or if your breed makes Agility/Sense easier than
Strength/Agility (Opifex). This matches what `soldier-endgame.json` already says.

---

## The Nanodeck — not really "gear", it is a nano gate

`Soldier Nanodeck` (270768) → `Activated Soldier Nanodeck` (280768) → `Stellar Soldier Nanodeck` (280782).
All three are `Profession EqualTo 1`, `ToWield Level > 214`, **NODROP + Unique**.

Wearing **any** of them sets `NanoFocusLevel` bit 64, which is the requirement on **six** of the Soldier's
best nanos (Augmented Mirror Shield MK V, Improved Total Focus, Total Combat Survival, Improved Soldier
Clip Junkie, Improved Ranged Energy Weapon Mastery, Art of War). No amount of nano skill substitutes.

The **Stellar** version additionally gives Assault Rifle / Full Auto / Burst / MG-SMG / Ranged Energy /
Heavy Weapons / First Aid / Strength **+30 each**. The chain and the Star of Fidelity (`244690`) are in
`soldier-endgame.json`.

---

## The best of the small stuff

- **Dreadloch Damage Amplifier** (`267260`) — HUD. **+25 to all eight damage modifiers**, Add All Off. +15,
  Ranged Init +25. Locked to **Profession 1 or 2** (Soldier or Martial Artist) — verified in the data.
- **Dreadloch Stabilising Aid** (`267165`) — HUD. Ranged Init +50, AR +25, MG-SMG +25, Burst +15,
  AAO +10, Critical Increase +1. No profession lock.
- **Deflection Amplifier** (`263256`) — HUD3, Soldier only. Only +10 to each Reflect AC, and only QL1–10
  exists, but almost nothing else in the game puts Reflect AC on an *item*.
- **Soldiers' Ring of Focus** (`267578`) — the endgame ring: Body Dev. +50, Full Auto +25, AAO +20,
  Burst +20, Heal Delta +10. Level 220, Lost Eden.
- **Custom-Made Soldier Ring of Attack / Defense** (`163652` / `163650`) — level 200 NODROP pair;
  attack = +36 Ranged Init and a spread of +6/+3 weapon skills, defense = +300 Health and +75 to every AC.
- **Xan's Viral Belt Component Platform** (`279449`) — 6 slots, NCU +30 and **+35 to all six attributes**.
- **Accelerated NCU Memory** (`164603`) — +64 NCU per chip at QL200; six of them is +384 NCU.
- ⚠ **"Small Ebony Figurine" is nine different items sharing one name.** The Soldier wants `202424`
  (Assault Rifle +8 / Ranged Init +12). `202300` is the Nano Programming one, `202417` the Pistol one.
  You cannot buy these by name — check the id.

---

## Honest coverage

This file is the **curated** best-in-slot/progression layer: 9 armour sets (every Soldier-locked set in the
client data plus Combined, Miy's Ranged and Carbonum), 10 headwear entries, 10 NCU/belt entries and 20
misc/HUD/ring entries. It is **not** the exhaustive per-slot catalogue that `CLASS-PROFILE-PLAYBOOK` §9
demands — that would be all 61,643 Soldier-usable wearable templates, and it has not been generated as a
profile file for any class yet.

Nine things are deliberately left as `unknown` in `_unverified`, chiefly: where Pernicious, Notum Infused
Kevlar and Omni-Pol Trooper actually come from, and the Miy's Ranged drop camps. A search-engine snippet
claiming Pernicious "drops off dragon mobs in most SL zones" was **not** confirmed on a page I read, so it
is not recorded as fact.

---

## Sources

**Local:** `items.ocp` via `tools/eng-gear-extractor --prof 1 --wield` · `itemnames.sql` ·
sibling profiles `soldier-endgame.json`, `soldier-build.json`, `soldier-symbiants.json`, `soldier-weapons.json`.

**Web:** [wiki.aodb.us/wiki/Armorsets](http://wiki.aodb.us/wiki/Armorsets) (set taxonomy — Pernicious =
Dynaboss Armor) · [wiki.aodb.us/wiki/Soldier:Armor](http://wiki.aodb.us/wiki/Soldier:Armor) ·
[wiki.aodb.us/wiki/Alien_Armor](http://wiki.aodb.us/wiki/Alien_Armor) (Combined recipes) ·
[wiki.aodb.us/wiki/The_Beast](http://wiki.aodb.us/wiki/The_Beast) (Star of Fidelity = the Soldier's Star) ·
[wiki.aodb.us/wiki/Miy's_Nano_Armor](https://wiki.aodb.us/wiki/Miy's_Nano_Armor) ·
[Tepamina's Soldier Guide](https://www.ao-universe.com/guides/classic-ao/profession-guides/tepaminas-soldier-guide-13).

**Not used** (block automation, playbook §4): auno.org, aoitems.com, anarchyonline.fandom.com.
