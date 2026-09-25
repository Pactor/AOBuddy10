# Soldier — Leveling 1→220 (+AI) (AOBuddy10)

Profession 1. Companion to `soldier-leveling.json`. Symbiant unit type: **Artillery**.

A Soldier has **no pet, no real heal and no crowd control**. It levels by holding the biggest Assault
Rifle its skills allow and keeping a stack of reflect and absorb nanos up. So the whole curve is a race
between **two** numbers:

| Track | Skills | What it buys |
|---|---|---|
| The gun | Assault Rifle, Burst, Full Auto, Fling Shot, Ranged Init. | what you can hold |
| The shield | **Matter Creation (cost 2.5)**, **Time & Space (cost 3.2)** | what you can cast |

Matter Creation is a cast requirement on **102 of 177** Soldier nanos; Time & Space on **64**. A Soldier
who trains only the gun has no reflect, and reflect *is* the class.

**Where the numbers come from:** every "at level N you can cast X" below is the nano's own `ToUse Level`
requirement from `soldier-nanos.json`; every weapon/armour requirement is the item's own `ToWield`/`ToWear`
list from `items.ocp`. Every *place to grind* window is either an exact column range from the local
**Faffy's AO Levelling Chart** or the level range on the **AODB Hunting Grounds** page. Nothing here is
estimated.

---

## The reflect timeline — the single most important thing to plan around

| Level | What unlocks |
|---|---|
| ~1–10 | **Total Mirror Shield Mk I** (`70308`) needs only **MC 58 / TS 46** — a Soldier has 75 % reflect before level 10 |
| ~10–60 | Mk II → Mk X (`70309` … `70300`, top at MC 695 / TS 590) |
| **60–175** | **NOTHING NEW.** This is the reflect plateau. Bridge it with armour, Body Dev. and (if you own SL) the Empowered line |
| 175 | **Augmented Mirror Shield MK I** `223229` — MC 914 / TS 880 |
| 203 / 213 / 220 | MK II `223181` / MK III `223183` / MK IV `223185` (MK IV also needs **Specialization bit 4**) |
| — | **MK V** `273400` has *no* level gate but needs a worn **Soldier Nanodeck** |

The Shadowlands **Empowered / Resonance** line (strain 694) is a *different* nano line, so it **stacks**
with a mirror shield, and one cast covers you *and* the team. It steps at levels
**0 / 25 / 50 / 75 / 100 / 125 / 145 / 155 / 175 / 185 / 201 / 207 / 212 / 220**. If you own SL, buy the
next tier the level you reach it.

---

## Brackets

| Levels | Where | Soldier-specific |
|---|---|---|
| 1–6 | Arete ID-card chain, Old Backyards | TMS Mk I at MC 58/TS 46 — reflect from level ~5 |
| 6–15 | Condemned Subway (Workmen 11–15), Newland | **Loot the Ring of the Nucleus Basalis** (`202717`, +50 NCU) |
| 15–25 | Deep Subway + elite daily, Oasis, The Pipes | +50 % XP cans from Canbert at 17 |
| 25–40 | TotW cultists, Steps of Madness rooms 1–3, Crippler Cave | **Level 25** opens 7 nanos at once — all need SL + Spec bit 1 |
| 40–60 | **TotW — last chance, entry locks at 60**, Virus Builders, Biomare, Nascence hecklers | Level 50 team damage; reflect plateau begins |
| 60–80 | Chapel of Chants, Whispervale, Biomare missions, Mort borgs, The Reck | **Level 75: first detaunt** (Bypass Me `223211`) — your emergency button |
| 80–110 | Callous Mortiigs, Coldrock, Port 7, Ely hecklers, Mantis Den 1 | Level 100 nano tier; **Specialization 1**; build Str/Sta toward Ofab's 900/1100 |
| 110–130 | Ely heckler runs, Scheol, PW borgs, Inner Sanctum key, Ely Catacombs daily | Max **Body Dev.** — cost 1.1, your second-cheapest skill |
| 130–160 | Whirling Rocks, Adonis hecklers, Solo Dark Ruins, IS floors, Mantis Den 3 | **Specialization 2** ~150; Art of Peace at 155 |
| 160–190 | Adonis hecklers, Dark Ruins Team, Alappa, Scheol/Ely quest DM-XP | **Level 175 — Augmented Mirror Shield MK I.** The plateau ends |
| 190–200 | Penumbra hecklers, Inferno missions, Battlestations | Soldier Clip Junkie (+120 FA); get a **Perennium Blaster** (XP bonus!) — **froob cap** |
| 200–210 | Inferno, daily stack, **Dust Brigade 1 at 205** | Get the **Soldier Nanodeck**. Six of your best nanos don't exist without it |
| 210–220 | Tuin, Inferno questlines, Arid Rift, raids | AMS MK III at 212, MK IV + Pre-Nullity Sphere at 220 |
| AI 1–30 | Alien dailies, mothership, city defence, sectors | **Title Level 4 and 6** unlock your only two procs |

Full `where[]` lists, per-bracket `fight` text and the exact nano ids are in the JSON.

---

## The Perennium Blaster gives XP

`Perennium Blaster` (`246422`–`246427`, QL50–199) and `Rebuilt Perennium Blaster` (`260700`–`260705`)
carry **`XPModifier +3`** as a *worn* bonus; the Superior versions (`246428` / `260706`, QL200) carry
**+4**. Read straight out of `items.ocp`. Soldier-only, Shadowlands, wants Assault Rifle 996 / Burst 747 /
Full Auto 897. Twinking into it early literally pays for itself — AOWiki calls the Rebuilt version
"arguably the best assault rifle in game".

---

## The froob reality check (read this before rolling a free Soldier)

From the client data, **every single tier** of the Soldier weapon-buff family carries `Expansion op22 2`
*and* a Specialization bit:

- A Sergeant's Knowledge → Art of Peace (Assault Rifle)
- Metal Stream → Soldier Clip Junkie (Full Auto)
- Alleysweeper → Swiss Cheese (Shotgun)
- Artillery Fire → Battery Fire (Heavy Weapons)
- Power Volley → The Power of Three (Burst)

So does the entire **`Fight (Team)`** damage line, every **Empowered** team reflect, every **Augmented
Mirror Shield**, the **Perennium Blaster** and all Tier armour. **A froob Soldier cannot cast one of them.**

What a froob Soldier *does* have is still a complete class: the **Total Mirror Shield** line, the
**Absorption Shield / Combat Barrier** AC line, Assault Rifle Mastery (+60), **Riot Control** (+110 Burst),
Rifle/Pistol/Ranged Energy Mastery, Total Focus, Automatic Targeting, Precognition, Offensive Steamroller
and the Body Boost → Battlefield Endurance HP line. 75 % reflect from level 10 and the best AC buffs on
Rubi-Ka. It just plateaus at Mk X and never gets a Full Auto buff.

Best froob gear: the **Division 9 Plasmaprojector** line (Soldier-only, no expansion gate) and
**Notum Infused Kevlar Armor** (Soldier-only, level 180+, no expansion gate).

---

## Tips that are actually class-specific

1. **Train MC and TS alongside Assault Rifle**, not after it.
2. **Pull one mob at a time.** All 20 taunts and all 6 detaunts are single-target and you cannot heal.
3. **Learn your detaunts as they unlock** (75 / 145 / 195 / 207 / 214 / 219) — shedding aggro *is* your
   emergency button.
4. **Buy every Specialization on schedule** — bits 1/2/4/8 are literal *cast requirements*, not a bonus.
5. **Max Body Dev.** — cost 1.1, cheaper than for anyone but an Enforcer. The reflect layer is spread over
   that HP pool.
6. **Do TotW completely before 60** — entry hard-locks.
7. **Get a Trader wrangle before every equip step** — it raises the gun skills *and* the shield skills in
   one cast.
8. **At 205, drop everything and do Dust Brigade 1.** The Nanodeck is worth more than any other single item
   to this class.
9. **Carry stims at every level.** Your whole heal kit is two tiny DrainHeals.

---

## Honest coverage

14 brackets, 16 key quests, 9 keys, a 13-step Soldier-only weapon ladder, 16 tips. Every class-specific
level gate is exact (from the nano/item data). Every location window is exact (from Faffy's chart or AODB
Hunting Grounds) — but the *locations themselves are class-agnostic*, and this file makes **no** claim
about which spot is fastest for a Soldier specifically.

Eight things are left in `_unverified`, including: where the Biomare gamma access card comes from, the
Specialization quest givers, how the Hellfury/Hellspinner "Boosted"/"Augmented" upgrades are performed,
and the places where Faffy's chart and AODB disagree by a few levels (both numbers are given rather than
picking one). Faffy's chart is a *froob* guide, so its Rubi-Ka windows are tuned for characters without
expansions.

---

## Sources

**Local:** `soldier-nanos.json` (177 nanos, every level/Specialization/Expansion gate) · `items.ocp` via
`tools/eng-gear-extractor --prof 1 --wield` (weapon ladder, XPModifier, profession locks) ·
`itemnames.sql` · **`E:\Funcom\FAFFY'S LEVELLING GUIDE CHART\Chart.html`** (parsed column-by-column) ·
sibling profiles `soldier-build.json`, `soldier-buffs.json`, `soldier-endgame.json`, `soldier-gear.json`,
`soldier-weapons.json`.

**Web:** [AODB Hunting Grounds](http://wiki.aodb.us/wiki/Hunting_Grounds) ·
[Soldier:Weapons](http://wiki.aodb.us/wiki/Soldier:Weapons) ·
[Soldier:Breed and Skills](http://wiki.aodb.us/wiki/Soldier:Breed_and_Skills) ·
[Tepamina's Soldier Guide](https://www.ao-universe.com/guides/classic-ao/profession-guides/tepaminas-soldier-guide-13).

**Not used** (block automation, playbook §4): auno.org, aoitems.com, anarchyonline.fandom.com.
