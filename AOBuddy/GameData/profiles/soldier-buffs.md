# Soldier — Buffs Reference (AOBuddy10)

Profession 1. Companion to `soldier-buffs.json`.

Every self / gives-to-others id, number, nano line and cast requirement here is read from the local
extraction `soldier-nanos.json` (nanos.ocp, client 18.8.50_EP1). The child nanos that the team and
Shadowlands reflect parents actually apply were read straight out of `nanos.ocp`. Every id→name pair
was verified against `itemnames.sql` (236 pairs, 0 mismatches). Cross-profession buffs have `id: null`
because they are other professions' nanos and are not in the Soldier extraction.

**Coverage: all 177 Soldier-castable nanos are placed in exactly one section.**

The Soldier has no pet, no real heal (two `DrainHeal` nanos) and no crowd control. Its entire nano kit
*is* buffs. Read the whole class through one sentence: **damage comes from the gun, survival comes from
a stack of reflect and absorb nanos, and the only things it gives a raid are a team reflect, a team AC
buff and a team damage buff.**

---

## 1. Self-buffs — what to keep up, and in what order

1. **Shadowlands reflect** — the `Empowered …` / `… Resonance Field` / **Pre-Nullity Sphere** (`233033`)
   ladder, 18 ranks. One cast puts a **bigger** reflect on the Soldier *and* a smaller one on the whole
   team. At 220: self `+38` reflect / `+68` max-reflected, team `+30` / `+68`.
   *This is the reflect that stays up*, because its strain **694** is separate from the mirror shields.
2. **Phalanx** (`29245`) → team-casts `162357`: **all eight ACs +350**, strain `NOSTACKING(0)`, so it
   collides with nothing. This — not the Absorption Shield line — is the Soldier's real AC give-away.
3. **Total Combat Survival** (`273398`) — Max Health **+1200**, Heal Delta +35 (needs a Nanodeck);
   otherwise **Battlefield Endurance** (`95697`, +405).
4. **Weapon buff for the gun actually held** — Art of War (`275027`, AR +200) with a Nanodeck,
   Composite Heavy Artillery (`269482`, six ranged skills +100), or Art of Peace (`203119`, +90).
5. **Improved Soldier Clip Junkie** (`273402`, Full Auto **+180**, needs Nanodeck) or
   Soldier Clip Junkie (`203137`, +120).
6. **Improved Total Focus** (`270806`, ten weapon skills +125, needs Nanodeck).
   ⚠ Do **not** fall back to plain **Total Focus** (`29255`) in PvP — it also gives **−40 to all three evades**.
7. **Riot Control** (`29251`, Burst +110) and **Full Automatic Targeting** (`270248`, Add All Off. +74).
8. **Offensive Steamroller** (`29240`) — all four initiatives +133, Fling Shot +30.
9. **Improved Precognition** (`275844`, evades +110, needs Nanodeck) when solo or in PvP.
10. The best proc the AI title level allows — **Notum-Charged Grenades** (`302399`, AI 6+) else
    **Fragmentation Grenades** (`302395`, AI 4+).

**NOT keep-up:** the **Total Mirror Shield** line (`70300`–`70309`) and the **Augmented Mirror Shield**
line (`223229` / `223181` / `223183` / `223185` / `273400`). AO-Universe's Tepamina guide: 75 % reflect,
maximum duration ~1:20, and they **lock your nano skills for 2 minutes**. These are cooldowns you spend
on a damage phase, not buffs you maintain.

### The Total Mirror Shield ladder — what actually scales

Every Mk gives the same **+75 to all eight Reflect ACs**. Only the *Max Reflected Damage* cap grows:

| Nano | id | Max reflected | MC / TS |
|---|---|---|---|
| Total Mirror Shield Mk I | 70308 | +7 | 58 / 46 |
| Mk II | 70309 | +21 | 142 / 114 |
| Mk III | 70306 | +38 | 231 / 185 |
| Mk IV | 70307 | +49 | 304 / 236 |
| Mk V | 70303 | +66 | 433 / 332 |
| Mk VI | 70304 | +80 | 533 / 391 |
| Mk VII | 70301 | +97 | 615 / 469 |
| Mk VIII | 70302 | +111 | 627 / 512 |
| Mk IX | 70305 | +115 | 642 / 546 |
| Mk X | 70300 | +136 | 695 / 590 |

### The Augmented Mirror Shield ladder

| Nano | id | Reflect ACs | Max reflected | Gate |
|---|---|---|---|---|
| Augmented Mirror Shield MK I | 223229 | +77 | +250 | level 175, MC 914 / TS 880 |
| MK II | 223181 | +78 | +300 | level 203, MC 1033 / TS 1003 |
| MK III | 223183 | +79 | +350 | level 213, MC 1281 / TS 1247 |
| MK IV | 223185 | +80 | +400 | **level 220 + Specialization bit 4**, MC 1454 / TS 1419 |
| MK V | 273400 | +82 | +500 | **NanoFocusLevel bit 64 (worn Soldier Nanodeck)**, MC 1527 / TS 1490 |

---

## 2. The Nanodeck gate — the most under-documented thing about a Soldier

**Six** of the Soldier's best nanos require `NanoFocusLevel op22 64`. That bit is set only by a worn
**Soldier Nanodeck** (`270768` → Activated `280768` → Stellar `280782`, all `ToWield Level > 214`,
verified in `items.ocp`):

- Augmented Mirror Shield MK V `273400`
- Improved Total Focus `270806`
- Total Combat Survival `273398`
- Improved Soldier Clip Junkie `273402`
- Improved Ranged Energy Weapon Mastery `275905`
- Art of War `275027`

No amount of nano skill substitutes for the deck. See `soldier-endgame.json` for the DB1 → activation
device → Star of Fidelity chain that builds it.

---

## 3. Nano-line stacking traps

- **`ReflectShield(2)` holds three different things**: the Total Mirror Shields, the Augmented Mirror
  Shields, **and** the 28 `Deflection Shield` / `Reflective Field` nanos the Soldier casts *on other
  people*. One occupant per character — a teammate's Reflective Field cast on a Soldier **overwrites his
  mirror shield**.
- **`ShadowlandReflectBase(694)` is a different line**, so an Empowered / Resonance field stacks on top
  of a mirror shield. That is the whole reason the reflect stack works.
- **All 28 Rubi-Ka reflects carry `ExpansionPlayfield == 0`** — they cannot be cast in a Shadowlands
  playfield at all. In SL you use the Empowered line, which team-casts by itself. *(Read from the client
  data, not from a guide.)*
- **`HPBuff(151)`** holds both Total Combat Survival (+1200, self) and the Body Boost → Battlefield
  Endurance give-away line (+20 → +405). Never let the small one land on a Soldier running the big one.
- **`AssaultRifleBuffs(212)`** holds Art of War, Art of Peace, A Sergeant's Knowledge **and** Composite
  Heavy Artillery — pick one.
- **`DamageBuffs_LineA(4)`** holds the Augmentation Cloud line, the Damage Amplifier/Multiplier pairs,
  Helepolis of the Besieger **and the children of the `Fight (Team)` line**. At endgame cast the team
  Fight line and stop casting Augmentation Clouds.
- `MajorEvasionBuffs(144)`: Precognition (+55, others) vs Improved Precognition (+110, self).
  `TotalFocus(769)`: Total Focus vs Improved Total Focus. `InitiativeBuffs(152)`: Quickshot / Attack
  Booster / Offensive Steamroller. `SiphonBox683(683)`: one proc only.
- `ArmorBuff(3)` is shared with every other profession's AC buff, so the Soldier's Absorption Shield
  line (max **+530**) is usually redundant next to an Engineer's Assurance line (up to **+5000** in the
  *same* line). **Phalanx is not** — it is in `NOSTACKING(0)`.

---

## 4. What a Soldier gives other people

| Buff | id | Effect | Note |
|---|---|---|---|
| Shadowlands reflect line | 233012–233051 | team reflect, one cast covers self + team | the headline give-away |
| **Phalanx** | 29245 → 162357 | all eight ACs **+350** | `NOSTACKING(0)` — collides with nothing |
| Team damage line (`Fight (Team)`) | 223187–223199 | all eight damage modifiers **+12 → +107** | children land in strain 4 |
| Rubi-Ka reflect line | 70310–70337 (28) | Reflect ACs +10 → +30 | **Rubi-Ka playfields only** |
| Armor buff line | 75401–75413, 29223 (14) | all ACs +21 → +530 | `ArmorBuff(3)`, usually redundant |
| Health line | 29091 → 95697 (7) | Max Health +20 → +405 | matches Who_Buffs_What exactly |
| Full Automatic Targeting | 270248 | Add All Off. +74 | |
| Composite Heavy Artillery | 269482 | AR / Rifle / Grenade / Heavy Wpn / RE / MG-SMG **+100** | six skills, one cast |
| Riot Control | 29251 | Burst +110 | |
| Assault Rifle Mastery | 29220 | Assault Rifle +60 | |
| Ranged Energy Weapon Mastery | 29230 | Ranged Energy +70 | |
| Rifle Mastery | 29250 | Rifle +50 | |
| Pistol Mastery | 29246 | Pistol +40 | shared with Engi / Crat / Doc / MP — the cast req proves it |
| Offensive Steamroller | 29240 | all four initiatives +133 | |
| Augmentation Cloud line | 222833–222838 (4) | all damage modifiers +15 → +40 | **also Fixer and Agent nanos** |
| Precognition | 29247 | all three evades +55 | |

---

## 5. What a Soldier should ask for

- **Trader Wrangle** — the one buff that fixes both halves of the class at once: weapon skills (equip
  the gun, cap Burst/Full Auto) *and* nano skills (land a bigger shield). Matter Creation costs 2.5 and
  Time & Space 3.2 for a Soldier — the two most expensive schools he owns, and they gate every reflect.
- **Enforcer Essence / Doctor Iron Circle** — Str/Sta. Ofab Soldier QL300 needs **Strength 900 /
  Stamina 1100**; Combined Commando's QL300 needs **Strength 1000 / Agility 1000** (both from `items.ocp`).
- **Agent Feline Grace / Enhanced Senses** — Agility and Sense for implants, symbiants and Aimed Shot.
- **Doctor heals** — the Soldier has none. The reflect stack only buys time.
- **Doctor nano init** — Nano C. Init costs **4.0** for a Soldier, yet he casts a long shield stack
  before every pull.
- **Fixer NCU** — he runs self reflect + team reflect + weapon buff + Full Auto buff + HP + damage + init
  simultaneously.
- **MP nano-skill buffs** (Mocham's / Composite, ids in `metaphysicist-buffs.json`) — raises MC and TS
  directly.
- **Trader trade-skill wrangle** — Star of Fidelity (`244690`) and Stellar Fidelity (`281045`) both need
  **Computer Literacy 1500** to equip.

---

## 6. Aggro tools (26 nanos)

Single-target only — AO-Universe rates the Soldier's add management below an Enforcer's for exactly this
reason. Taunts run `Ego Taunt` (`29228`, 87) → `Offensive Insult` (`229104`, 15000); detaunts run
`Bypass Me` (`223211`, −2133) → `Desist Me` (`223221`, −11000). Full ladders with levels are in the JSON
under `tauntsAndDetaunts`. TauntNpc values are raw function arguments — use them for ordering only.

---

## 7. Not castable by a Soldier

`False Profession: Soldier` (`32038`), `Assume Profession: Soldier` (`117227`) and
`Mimic Profession: Soldier` (`117216`) are **Agent** nanos — their cast action requires
`Profession == 5` and they set `VisualProfession := 1`. They make an *Agent* look like a Soldier. They
appear in the Soldier extraction only because of the "already looks like a Soldier" `VisualProfession == 1`
branch. `Test Body Boost - NOT USED` (`292504`) is named as unused in the client data.

---

## Sources

**Local (authoritative):** `soldier-nanos.json` (177 nanos from `nanos.ocp`, 18.8.50_EP1) ·
`nanos.ocp` read directly for child nanos 217782–217788, 231323–231340, 232233–232250, 162357,
302393/302394/302397/302398 · `itemnames.sql` (131,384 rows) · `AOSharp.Common/GameData/Stat.cs` ·
`OmniCell.Enums/FunctionType.cs` · `items.ocp` via `tools/eng-gear-extractor --prof 1` for the Nanodeck
and Star of Fidelity requirements.

**Web:** [wiki.aodb.us/wiki/Who_Buffs_What](https://wiki.aodb.us/wiki/Who_Buffs_What) (every Soldier row
matches the local data exactly) ·
[ao-universe Buffing Guide](https://www.ao-universe.com/guides/classic-ao/gameplay-guides-6/buffing-guide) ·
[Tepamina's Soldier Guide 1/3](https://www.ao-universe.com/guides/classic-ao/profession-guides/tepaminas-soldier-guide-13)
(and parts 2/3) · [wiki.aodb.us/wiki/Soldier](http://wiki.aodb.us/wiki/Soldier).

**Not used** (block automation, playbook §4): auno.org, aoitems.com, anarchyonline.fandom.com.

See `_unverified` in the JSON for the ten things deliberately **not** asserted — chiefly nano durations,
the `AddOffProc` chance unit, what nano strains 12 / 255 are, and the real intent of
`Feelings of Mortality` / `One Foot in the Grave`.
