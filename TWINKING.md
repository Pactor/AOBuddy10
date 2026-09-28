# Twinking, measured

Everything below is read out of the local client data unless it says otherwise. Numbers are
from `nanos.ocp` and `items.ocp` (client 18.8.50_EP1 extraction) through `OmniCell.Core`'s
loaders, names from `itemnames.sql`, perks from `AOBuddy/GameData/Perks.xml`, and the
ability-to-skill table from `AOSharp.Clientless/GameData/SkillTrickle.json`.

The tool that produced it is `tools/twink-extractor`, and it is re-runnable:

    TwinkExtract probe    --name <regex> [--nanos|--items]
    TwinkExtract probe    --id <id>[,<id>...]
    TwinkExtract modifies --stat <id> [--nanos|--items]
    TwinkExtract requires --stat <id> [--nanos|--items]
    TwinkExtract stats    --id <id>

Stat ids used throughout: Intelligence 19, Agility 17, Sense 20, Treatment 124, First Aid 123,
Computer Literacy 161, CurrentNCU 180, MaxNCU 181, BeltSlots 45.

Three things in here are **not** in the local data and are marked where they appear: the
80% pet-control rule, shop and mission availability, and anything about where an item drops.

---

## 0. The arithmetic everything rests on

**Trickle-down.** A skill gets a free contribution from your abilities:

    trickle = floor( ( Str·w0 + Agi·w1 + Sta·w2 + Int·w3 + Sense·w4 + Psy·w5 ) / 4 )

The weights per skill are in `SkillTrickle.json`; the divisor 4 is in
`SimpleChar.GetTrickle`. The two that matter here:

| skill | Str | Agi | Sta | Int | Sense | Psy |
|---|---|---|---|---|---|---|
| **Computer Literacy** (161) | – | – | – | **1.00** | – | – |
| **Treatment** (124) | – | **0.30** | – | **0.50** | **0.20** | – |
| First Aid (123) | – | 0.30 | – | 0.30 | 0.40 | – |

So Computer Literacy is purely Intelligence, at **1 point of Comp Lit per 4 Intelligence** —
which is what the ao-universe guide says too, and the guide is reachable, not bot-blocked.
Your "Treatment is 50% Int, 30% Agility, 20% Sense" is exactly right.

That single fact is why the whole sequence has the shape it does: Intelligence is the lever
for Comp Lit, and Comp Lit is the lever for NCU.

**A nano's NCU cost is the `Level` field on the nano template.** Verified three ways: Robust
Treatment reads 42 and costs 42; the whole Skill Wrangler ladder reads 11/22/26/35/46/54,
which are its known NCU costs; and the NCU-granting nanos themselves read 1, which is what a
nano you cast to *gain* NCU has to be.

**Buff durations** are in hundredths of a second. Calibrated against nanos that name their own
duration: `Composite Medical Expertise (1 hour)` reads `TimeExist = 360000`, the 2/4/8-hour
Composite Attribute Boosts read 720000 / 1440000 / 2880000.

---

## 1. Maximum Intelligence, for maximum Computer Literacy

Your step one. Intelligence is the only thing Comp Lit trickles from, so every point of Int is
a quarter point of Comp Lit *plus* whatever the item gives directly.

**Implants that give Intelligence**, best at QL 200:

| slot | grade | +Int |
|---|---|---|
| Head | Shiny | **+55** |
| Eye | Bright / Shiny / Shiny Jobe | +33 |
| Ear | Faded / Bright / Shiny / Jobe | +22 |

**Implants that give Computer Literacy directly**, best at QL 200:

| slot | grade | +Comp Lit |
|---|---|---|
| Head | Shiny | **+105** |
| Eye | Bright / Shiny / Shiny Jobe | +63 |
| Right Hand | Faded / Bright / Shiny / Jobe | +42 |

This matches the guide's "Eyes Bright, Head Shining, Right Hand Faded" exactly — it is the
same three positions, read independently out of the item pack.

**Intelligence buff nanos**, with NCU cost:

| nano | +Int | NCU |
|---|---|---|
| Odin's Other Eye | +103 | 57 |
| Izgimmer's Hippocampal Augmentor | +83 | 51 |
| Enfraam's Cortex Accelerator | +48 | 38 |
| Odin's Missing Eye | +43 | 51 |
| Neuronal Stimulator | +23 | 7 |
| Composite Attribute Boost (1/2/4/8 h) | +12 | 4 |

**Computer Literacy buff nanos** — note these are Trader lines, not self-casts for most:

| nano | +Comp Lit | NCU |
|---|---|---|
| Trading Mogul | **+260** | 51 |
| Bulk Trader | +160 | 30 |
| Frequent Customer | +55 | 7 |
| Nano Can Buff: Computer Literacy Mastery | +35 | 8 |
| Computer Literacy Expertise | +20 | 4 |
| Composite Utility Expertise | +20 | 4 |
| Computer Literacy Proficiency | +10 | 2 |

**The biggest Comp Lit items** (excluding implants and clusters), all reachable gear:

| item | +Comp Lit | gate |
|---|---|---|
| Clan Merits – Awakened Combat/Defense Paragon | +275 | Clan, 500 tokens |
| Omni-Tek Award – Awakened Combat/Defense Exemplar | +275 | Omni |
| Profiteer's Helper | +250 | Neutral, Int 500 |
| Clan Merits – Paragon / OT Award – Exemplar | +250 | 250 tokens |

(`Constrained Gridspace Waveform` reads +14,400 and `Blackmane's Stat Buffer` +2,000 behind a
`GmLevel > 99` gate — those are GM and test items, not gear.)

---

## 2. NCU

Your sequence is right, and here are the actual ladders.

### The belt

The belt is a *platform* that grants slots; the slots hold memory. Both halves cost Comp Lit.

| slots | belt | Comp Lit | level |
|---|---|---|---|
| 1 | Belt Component Platform Ti-100X | 6 | – |
| 2 | Belt Component Platform Ti-200X | 20 | 1 |
| 3 | Belt Component Platform 300X | 80 | 25 |
| 4 | Belt Component Platform 4IX | 200 | 60 |
| 5 | Belt Component Platform 5000 | 300 | 100 |
| 6 | **Belt Component Platform 6K-X** (QL 150) | **500** | 150 |
| 6 | Belt Component Platform 6K-X (QL 200) | 600 | 200 |

Six is the cap on the ordinary line. Others reach six with different gates —
`Reinforced NCU Component Belt` (Comp Lit 400 + Nano Programming 260), `Worm Control Band`
(Comp Lit 400 + Nano Programming 800), `Belt of Justice` (Clan, tokens), `Viral Belt`
(Comp Lit 1125, level 150, Alien Invasion), and the Shadowlands
`Component Platform of Intelligence` / `of Agility` (Comp Lit 1300).

### The memory that goes in it

| NCU | QL | item | Comp Lit needed |
|---|---|---|---|
| 2 | 1 | 2 – 3 NCU Memory | 6 |
| 3 | 1 | 3 NCU Memory | 20 |
| 4 | 20 | 4 – 7 NCU Memory | 35 |
| 8 | 25 | 8 NCU Memory | 80 |
| 16 | 50 | 16 – 31 NCU Memory | 121 |
| 25 | 60 | 25 NCU Memory | 200 |
| 32 | 120 | 32 – 63 NCU Memory | 280 |
| 35 | 100 | 35 NCU Memory | 300 |
| 45 | 150 | 45 NCU Memory | 500 |
| 55 | 200 | 55 NCU Memory | 600 |
| 64 | 200 | 64 NCU Memory | 750 |
| 70 | 200 | Enhanced Safeguarded NCU Memory Unit | 1150 |
| 70 | 200 | Protected Safeguarded NCU Memory Unit | 1250 |
| **100** | 215 | **100 NCU Memory** | **1000** |

Six slots of 100 NCU Memory is **600 NCU from the belt**, at Comp Lit 1000 and a QL 215 belt
(Comp Lit 999, level 215).

### Perks

Two lines, and they stack:

| perk | +NCU | also gives | gate |
|---|---|---|---|
| NCU Extensions 1–8 | 10, 11, 12, 12, 15, 17, 18, 35 = **130** | +Comp Lit each | level 40+ |
| Grid NCU Extension 1–4 | 10, 13, 17, 30 = **70** | +15 Comp Lit each | **Fixer only** (Profession == 4), level 50+ |

**200 NCU from perks** if you are a Fixer, 130 otherwise. The guide is right that Grid NCU
Extensions are Fixer-exclusive — the requirement is a hard `Profession EqualTo 4`. Worth
knowing: both lines *also* give Comp Lit, which the guide does not mention.

### Nanos

Here is the correction to your recollection: the line is **NCU Compressor**, not "NCU
Compression". Otherwise you had it exactly — **+20 NCU, available at level 25**
(`Level GreaterThan 24`), and it costs 1 NCU to run.

| nano | +NCU | level |
|---|---|---|
| **NCU Compressor** | **+20** | **25** |
| Retool NCU | +40 | 25 |
| Jury-rigged NCU Analyzer | +60 | 50 |
| Deck Recoder | +85 | 75 |
| Recompiling Memory Analyzer | +110 | 125 |
| QuarkStor NCU Core | +150 | 135 |
| Active Viral Compressor | +195 | 165 |
| Sentient Viral Recoder | +250 | 185 |

All are nano strain 257, so **only one runs at a time** — the line replaces itself as you
level, it does not accumulate. There is also `NCU Booster` (+10/+23/+40/+70 at levels
10/40/110/180) on a separate line, and two +500 items (`Sync Compressor`,
`Firewalled Sync Compressor`).

### Everything else

3,474 item templates modify MaxNCU. The notable non-belt ones include
`Memory NCU (1..6/6)` armour pieces (up to +192 at QL 300), `Viral Memory Storage Unit`
(+125), `Android NCU Injector` (+150), and the Spirit line (`Taurus' Spirit of Reflection`
+125, and so on). The full list is in `ncu-items.tsv` from the extractor.

---

## 3. Swapping Intelligence out for Treatment

Once the NCU is banked, the Int implants come out and the Treatment set goes in. This is the
step where the order matters, because Treatment is what gates the implants themselves.

**What an implant costs you to insert**, measured off the QL 1 and QL 200 templates that every
implant interpolates between:

| implant kind | Treatment at QL 1 | at QL 200 | ≈ per QL |
|---|---|---|---|
| ordinary (Faded / Bright / Shiny) | 10 | **950** | 4.72 |
| Jobe (Shadowlands) | 10 | **1004** | 5.00 |

So an implant needs roughly **4.75 × QL** Treatment, and a Jobe one **5 × QL**. That is the
number the whole treatment-buffing exercise exists to reach.

An implant also wants an **ability**, not just Treatment — `Eye Implant: Rifle, Shiny` QL 200
reads `Treatment > 950 ; Agility > 403`. Buffing Treatment alone is not always enough.

**Treatment buff nanos:**

| nano | +Treatment | NCU | notes |
|---|---|---|---|
| Superior First Aid | **+80** | 37 | also +80 First Aid |
| Robust Treatment | +60 | 42 | **Adventurer only** (VisualProfession == 6), 4 hours |
| Treatment Transfer | +60 | **1** | blocked while nano line 947 runs |
| Specialist Treatment | +35 | 11 | |
| Nano Can Buff: Treatment Mastery | +35 | 8 | |
| Treatment Expertise | +20 | 4 | |
| Composite Medical Expertise (1 hour) | +20 | 4 | |

Your "+60 Robust Treatment, 42 NCU" is exact. Two things you may not have had:
**Superior First Aid is bigger and cheaper** (+80 for 37 NCU), and **Treatment Transfer gives
+60 for a single point of NCU**.

**The Omni-Med Suit** — and here is the one number to correct. The set is:

| piece | +Treatment | +First Aid | slot |
|---|---|---|---|
| Omni-Med Suit Shirt | 20 | 20 | 32 |
| Omni-Med Suit Sleeves | 14 | 14 | 640 |
| Omni-Med Suit Trousers **or** Skirt | 14 | 14 | 2048 |
| Omni-Med Suit Gloves | 10 | 10 | 256 |
| Omni-Med Suit Boots | 6 | 6 | 16384 |

Trousers and Skirt share slot 2048, so the most you can wear at once is five pieces:
**+64 Treatment and +64 First Aid**, not +70. There is no helmet in the set. The pieces carry
**no wear requirement at all** — no level, no profession, no ability — which is what makes
them a twinking staple.

So a realistic Treatment stack before inserting: base + trickle + 64 (suit) + 80 (Superior
First Aid) + 60 (Treatment Transfer, 1 NCU) — and the suit comes straight back off afterwards,
because an implant only checks its requirement at the moment it goes in.

---

## 4. The wrangle, and the equip window

Trader wrangles raise **every weapon skill and every nano skill at once** — Rifle, all melee,
Martial Arts, Grenade, Heavy Weapons, Pistol, MG/SMG, Shotgun, Assault Rifle, Bow, Sharp
Object, Piercing, and all six nano schools. That is why it is the last step before equipping.

| wrangle | +skill | NCU | Trader PM/SpaceTime needed |
|---|---|---|---|
| Skill Wrangler (Commonplace) | +22 | 11 | 167 |
| Skill Wrangler (Inferior) | +46 | 22 | 330 |
| Skill Wrangler | +56 | 26 | 398 |
| Skill Wrangler (Advanced) | +74 | 35 | 519 |
| Skill Wrangler (Greater) | +99 | 46 | 690 |
| Skill Wrangler (Exceptional) | +121 | 54 | 807 |

Nano strain 220, `TimeExist = 18000` → **3 minutes**. All are `VisualProfession EqualTo 7`
(Trader). The Shadowlands `Umbral Wrangler` line is separate.

**Canned buffs are real and they are in the data.** Your "twinking can" is the **Nano Can**:
every wrangle step has a `Skill Wrangler (…) - Nano Can` twin with identical amount, NCU and
strain but **no profession requirement** — so you can self-apply a +121 wrangle without a
Trader. The same pattern exists for the other lines: `Nano Can: Robust Treatment`,
`Nano Can Buff: Superior First Aid`, `Nano Can Buff: Computer Literacy Mastery`,
`Nano Can Buff: Neuronal Stimulator`, and more.

---

## 5. Pet classes

The same sequence applies, with one extra consideration you raised: the wrangle lets you
*upload and summon* a pet nano above your natural skill, but control is checked continuously.

**The 80% rule — your knowledge, not found in local data.** Nothing in `nanos.ocp`,
`items.ocp`, the AOSharp game data or the OmniCell server encodes "you must keep 80% of the
skill requirement or the pet stops obeying". I looked for it specifically. It is a server-side
behaviour, so treat your statement as the specification; there is nothing here to check it
against, and nothing here contradicts it either.

What the data *does* say: pet nanos gate on Matter Creation / Time and Space and the like, and
the bot already reads pet type off the wire (see `HANDOFF-pets-and-casting.md`).

---

## 6. The order, corrected

1. **Stack Intelligence** — Head Shiny (+55), Eye (+33), Ear (+22) implants, plus Int buffs.
   Every 4 Int is 1 Comp Lit for free.
2. **Stack Computer Literacy on top** — Head Shiny (+105), Eye (+63), Right Hand (+42)
   implants; neck items up to +275; Trader Comp Lit buffs up to +260.
3. **Equip the belt and fill it** — the platform and each memory chip are separately gated on
   Comp Lit. Six slots × 100 NCU = 600.
4. **Take the perks** — NCU Extensions 1–8 (+130), and Grid NCU Extension 1–4 (+70) if Fixer.
5. **Run the NCU nano** — NCU Compressor at level 25 (+20), rising to Sentient Viral Recoder
   (+250) at 185. One at a time; strain 257.
6. **Now swap to Treatment** — Int implants out, Omni-Med Suit on (+64), Superior First Aid
   (+80, 37 NCU), Treatment Transfer (+60, 1 NCU), Robust Treatment (+60, 42 NCU) if you can
   get an Adventurer. Insert implants at ~4.75 × QL Treatment, watching the ability
   requirement too.
7. **Suit off, wrangle on** — Exceptional Skill Wrangler is +121 to every weapon and nano
   skill for 54 NCU, three minutes. Equip inside that window.
8. **Pets**: summon under the wrangle, then keep enough skill to hold them (your 80% figure).

---

## What is not in the local data

- The 80% pet-control threshold.
- Where anything comes from: which mission, vendor, or drop. `items.ocp` has requirements and
  effects, not sources.
- Implant cluster crafting (which cluster goes in which slot to build a given implant) — the
  clusters are in the pack as items, but the combine recipes are not.
- Whether a given Nano Can is still obtainable on the live server.

## Regenerating the evidence

Every table above comes from the extractor, and the full rows behind them are not committed
because they are query output - a few seconds to rebuild and five megabytes to keep:

    cd tools/twink-extractor && dotnet build -c Release
    ./bin/Release/TwinkExtract.exe modifies --stat 181 > data/mod-181.tsv   # MaxNCU
    ./bin/Release/TwinkExtract.exe modifies --stat 161 > data/mod-161.tsv   # Computer Literacy
    ./bin/Release/TwinkExtract.exe modifies --stat 124 > data/mod-124.tsv   # Treatment
    ./bin/Release/TwinkExtract.exe modifies --stat 19  > data/mod-19.tsv    # Intelligence
    ./bin/Release/TwinkExtract.exe modifies --stat 45  > data/mod-45.tsv    # BeltSlots
    ./bin/Release/TwinkExtract.exe requires --stat 124 --items > data/req-124.tsv
    ./bin/Release/TwinkExtract.exe requires --stat 161 --items > data/req-161.tsv

`req-124.tsv` is the implant table - 45,744 rows, every template that asks for Treatment.

## Sources

- `E:\Funcom\OmniCell\OmniCell\Datafiles\nanos.ocp` (10,965 nanos)
- `E:\Funcom\OmniCell\OmniCell\Datafiles\items.ocp` (120,842 item templates)
- `E:\Funcom\attic\extracted-client-data\itemnames.sql` (131,384 names)
- `AOBuddy/GameData/Perks.xml` (992 perk ids)
- `AOSharp.Clientless/GameData/SkillTrickle.json` + `SimpleChar.GetTrickle`
- Cross-checked against
  <https://www.ao-universe.com/guides/classic-ao/gameplay-guides-6/twinking-computer-literacy>
  — it agrees on the trickle rate, the three implant positions, the Fixer-only Grid perks and
  the top neck items.
