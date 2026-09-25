# Doctor — Gear Reference

Profession ID **10**.

> **Every number in this file was read out of `items.ocp` with `tools/eng-gear-extractor --prof 10 [--wield]`** (OMNICELL-CONTENT v3, client 18.8.50_EP1, 120,842 templates, **61,458 Doctor-wearable**), and every id was verified against `itemnames.sql`. No bonus figure here comes from a website. Implants, symbiants and weapons are covered in their own files and excluded here.

## What a Doctor stacks, in order

1. **Heal Multiplier** (stat `0x217`) — a percentage on every heal it will ever cast, and **only 66 items in the entire Doctor-wearable dump grant it**. No other class competes for them.
2. **Biological Metamorphosis / Matter Metamorphosis** — which rung of the heal ladder you can stand on.
3. **Treatment / First Aid** — gate every implant, symbiant and kit QL.
4. **Nano C. Init** — cast and recharge speed = healing throughput.
5. **Max Nano / Nano Pool / nano-cost reduction** — the fight timer.
6. **Max NCU** — it runs more standing buffs than any other class.
7. **Max Health / Body Dev.** — healing makes it the aggro magnet, and it has no reflect, no absorb and cost-3.2 close-combat evades.

## Armor sets

### Ofab Doctor (Lost Eden, Battlestation VP) — **the endgame baseline**
Three tiers: Ofab → Improved → **Penultimate** (ids 264646-264681). QL300 Penultimate, per piece:

| Piece | Id | Bonuses |
|---|---|---|
| Helmet | 264647 | Int +25, **Nano C. Init +65**, **Matter Metamorphosis +30**, Max Health +350 |
| Body | 264665 | **Max Nano +1000**, Nano C. Init +65, Stamina +20, Sensory Improvement +25, **Max NCU +32** |
| Sleeves | 264659 | Nano C. Init +50, **First Aid +45**, Time & Space +20, Max Health +350 |
| Gloves | 264653 | Pistol +40, Sense +25, Psychological Modification +20, Max Health +350 |
| Pants | 264677 | Max Health +350, Agility +16, **Computer Literacy +24**, Max NCU +25 |
| Boots | 264671 | Run Speed +55, **Matter Creation +35**, Max Health +350 |
| **Back** (OFAB Doctor Protective Gear) | 267933 | **Treatment +50**, **Heal Multiplier +2**, Psychic +25, Max NCU +25, Max Health +300, AAO +25 |
| **Shoulder** (OFAB Doctor Shoulder Wear) | 301697 | **Biological Metamorphosis +20**, Max Health +450, Pistol +15, AAD +15 |

Requires **Profession = Doctor, Stamina 1099, Psychic 854, Lost Eden** at QL300.
There is also a **Special Edition Ofab Doctor Helmet** (267352): Int +30, Nano C. Init +75, AAD +50, Max Health +500, −5% nano cost, Nano Delta +10, Max Nano +500 — strictly better than the Penultimate helmet on every stat except Matter Metamorphosis, which it drops entirely.

### Combined Paramedic's (Alien Invasion) — the survivability/sustain set
Six pieces (246632 / 246648 / 246650 / 246652 / 246654 / 246656), **identical on every piece at QL300**: Max Health **+200**, Body Dev. **+50**, Max Nano **+200**, Nano Pool **+50**, nano cost **−3%**, ~1200-1425 across all eight ACs. No profession lock. Requires Stamina 999, Psychic 999, Title Level 5.

Six pieces = **+1200 HP, +300 Body Dev., +1200 nano, +300 Nano Pool, −18% nano cost**. The client data confirms it is an HP/nano set with **no nano-skill bonuses at all** — a Doctor buys survivability and sustain there, not casting ceiling. `Odum's Combined Paramedic's Headwear` (257377, NODROP, level 209) adds +15 to all eight damage modifiers on top.

### Chosen / Faithful Doctor Suit (Shadowlands) — free, and the same layout
Chosen (Sanctuary, 214841-214858) and Faithful (Redeemed, 217710-217725) are **stat-identical** — verified piece by piece. Same slot-to-stat structure as Ofab at roughly 60-75% of the numbers: helmet Int/Pharma/MM, body Max Nano/NCU/SI, sleeves Nano Init/First Aid/TS, gloves Pistol/Sense/PM, pants HP/Agi/CompLit/NCU, boots RunSpeed/MC, back Psychic/Treatment, shoulder NCU/Str/BM/HP. The one thing it lacks that Ofab has is **Heal Multiplier**. Run it while farming VP.

Preceded by **First Tier** (217698-217707, QL100-160) and **Second Tier** (214755-214768, QL160-220) — one stat per piece, free, upgrading in place.

### Loren's Azure Reveries — **the best froob set**
(304609-304614, base set 165303-165308.) Requires only **Stamina 649 and Level 199** — no profession lock, **no expansion**. Between six pieces: Treatment +12, First Aid +20, BM +12, MM +15, MC +15, PM +15, SI +15, TS +8, **Nano C. Init +25**, Nano Programming +10, Psychic +14, Int +12, **−10% nano cost**, **+108 Max NCU**, **+2050 Max Health**, Run Speed +100. For a froob Doctor — and the froob heal kit is nearly complete — this is the best set in the game.

### Miy's Nano Armor — second choice
QL300 (268833-268845, cloak 270329/270330). Spreads **one** nano skill per piece (BM helmet, MM sleeves, MC gloves, PM legs, SI boots, TS body) with no Treatment, no First Aid and no Heal Multiplier. The **Body piece** (268837: Nano C. Init +50, Max Nano +375, −5% nano cost) and the **Cloak** (270330: all six abilities +10, +200 nano, +200 HP) are worth mixing into any set.

### Carbonum (162426-162436)
Cheap tradeskilled leveling armor, ~lvl 20-75. Pure AC — no nano, heal or Treatment bonuses.

## Heal Multiplier items — the section that matters most

| Item | Id | Slot | HM | Also gives | Reqs |
|---|---|---|---|---|---|
| **Sluggish Notum Lens** | 302931 | Feet/Deck6 | **+10** | Nano Damage +10 — **but Nano C. Init −800** | CompLit 1799, lvl 200 |
| **Sacred Chalice** | 305523 | Shoulder/RightHand | **+7** | Max Nano +1000, −5% cost, Skill Lock −10, Nano Range +120 | **Doctor**, BM 1799, lvl 209, NODROP |
| **Ancient Resorative Fungus** | 302925 | Head/HUD3 | **+5** | Max Nano +1000, Nano C. Init +100, Nano Delta +10, Range +50, AAD +25, −10% cost | **level 200 only** |
| **Sheffy's Micro Coil** | 267563 | Finger/HUD2 | **+5** | **BM +20, MM +20**, Nano C. Init +100, HP +150, −2% cost | Doctor, lvl 219, LE |
| **Doctor's Left Hand of Grace** | 267618 | LeftHand *(weapon)* | **+5** | Max Nano +250 | Doctor, **Pistol 1700, Fling Shot 850** |
| **Nano Assistant** | 269187 | Neck/Head/HUD | **+5** | Nano Range +10 | CompLit 1199, BM 1199 — **no lock, no level** |
| **Umbral Hand Wraps of Restoration** | 301678 | Hands | **+4** | SI/MM/BM +20 each, **Martial Arts +20**, 150-300 AC | Agi 699, Int 799, TL5 |
| **Perfected Infused DB Bracer** | 274551 | RightWrist | **+4** | HP +1200, Nano +1200, all abilities +25, AAO/AAD +75 | lvl 200, CompLit 1799, SL |
| **Cloak of the Reanimated Healer (5/5)** | 274707 | Back | **+3** | **all six nano skills +18**, Treatment +13, First Aid +13, Nano C. Init +50, Nano Delta +20, Heal Delta +10, 1000 AC | Int 729, Psy 729, TL5 |
| **Research Attunement Device – Medical Lv3** | 269409 | Head/HUD3 | **+3** | **Treatment +100, First Aid +100, BM +100** | level 199 only |
| Ancient Medical Bracer | 267752 | Either wrist | +2 | Treatment +25, First Aid +25, BM +25 | lvl 200, BM 1400 |
| DB Bracer – Third Edition | 292564 | **LeftWrist** | +2 | HP +1200, Nano +1200, First Aid +20 | lvl 200, CompLit 1799, SL |
| **Cure for Baldness** | 246274 | Head/HUD3 | +2 | Treatment +20, First Aid +20, Nano Pool +50, Body Dev +50, 1000 AC | **Doctor, Pharma Tech 499, NO level req** |
| Notum-Threaded Omni-Med Suit Sleeves | 304620 | Arms | +1 | Treatment +14, First Aid +14, MM +12, BM +12 | Doctor, Int 499, lvl 179 |
| Notum Ring of the Three | 305489 | Finger | +1 | Max NCU +15, HP +100, MC +15, TS +15, 125 AC | level 204 |

Notes: the two Dust Brigade bracer lines fit **opposite wrists**, so you can run both (+6 HM, +2400 HP, +2400 nano between them). **Cure for Baldness** is gated only on Pharma Tech (cost factor **1.0** for a Doctor) with no level requirement — which is why *"Twinking Cure of Baldness on Level 1 Doctor"* is a named community guide.

## NCU and belt

**Memory of Future Events** (305516, **+200 NCU**, CompLit 1849, lvl 204) · **Memory NCU 6/6** (278782, +192, LoX) · **Spirit Infused Yuttos Modified NCU** (267797, +125, +10 Treatment) · **Infused Computer Deck Range Increaser** (292190, +120 NCU **and +150 Nano Range** — the range matters when healing from outside an AoE) · **Accelerated NCU Memory** chip line 164603-164608 (QL200 = +64 NCU, +6 Nano C. Init) · **Superior Ring of the Nucleus Basalis** (305028, +75 NCU) · Belt Component Platform family (36777 and the 6K-X variants).

## Doctor-locked odds and ends

**Custom-Made Doctor Ring of Life** (163681: BM +7, First Aid +14, Treatment +14, Max Nano +200) · **Custom-Made Doctor Ring of Skill** (163679: Nano C. Init +30, Pharma +10) · **Doctor NICS** (215238, the profession HUD item — modest: Pistol +7, Int +3, Sta +3).

⚠️ **Nano Targeting Helper** (269185) boosts nano **damage**, not healing. The healer's equivalent in that slot group is **Nano Assistant** (269187).

⚠️ **De'Valos Sleeves** (253185: all six nano skills **+30**, Max NCU +15) carry a `Breed EqualTo 3` criterion on **both** QL records — `AOSharp.Common/GameData/Breed.cs` gives 3 = **Nanomage**. The extractor flattens the criteria tree, so an OR-group is indistinguishable from a hard lock. **Verify in game before planning around this item.**

## Where a Doctor differs from the MP template

- The file is organised around **Heal Multiplier**, a stat the MP gear file never mentions.
- The alien set is **Combined Paramedic's**, an HP/nano set — not the MP's nano-skill Combined Officer's.
- **Ofab ranks above Miy's**, the reverse of the MP file, because Ofab carries Treatment, First Aid, Heal Multiplier, Computer Literacy and +82 Max NCU that Miy's has at no QL.
- **Azure Reveries is promoted from "budget" to "high"** — it is the best no-expansion set for a class whose froob kit is nearly complete.
- **A weapon appears in the gear list** (Doctor's Left Hand of Grace) because its purpose is +5 Heal Multiplier.
- **Grid Armor is not a consideration** — it occupies the Body/Back/Arm slots that carry the Doctor's Max Nano, Treatment and Heal Multiplier pieces.
- **Every number is read from items.ocp per id**, where the MP gear file quotes several set-level figures from the wiki and flags the per-QL lines as unverified.

## Flagged / unverified

- **Drop locations are the weak part** and it is structural: items.ocp carries requirements and bonuses but no loot tables, and auno.org / aoitems.com are blocked to automation. Sluggish Notum Lens, Sacred Chalice, Ancient Resorative Fungus, Nano Assistant, Memory of Future Events and Notum Ring of the Three have **no source recorded at all**.
- **Combat Medic's Light Tank Armor** (wiki: drops from Biomare Neutralizers, boosts BM/MM/Treatment/First Aid) was **not found** in the local extraction — lead only, no numbers.
- **De'Valos Sleeves' `Breed EqualTo 3`** — hard lock or OR-branch? Unresolved.
- Combined Paramedic's also carries `HairTexture BitAnd 65536` on every piece; what it gates is unknown and it is recorded verbatim, not interpreted.
- **Heal Multiplier's unit and stacking were not derived.** Items are ranked by raw integer bonus; do not compute an expected heal from these numbers.
- Only QL1 and top-QL records exist per template; intermediate QLs interpolate in game.
- This is a **curated** view of a 61,458-item dump — **not** the exhaustive per-slot catalogue that playbook §9 requires. That would be a `doctor-equippable.json`, which does not exist yet for any class.
