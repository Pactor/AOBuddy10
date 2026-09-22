# Meta-Physicist (profession 12) — Pets

Research for AOBuddy10 pet AI. **Every id/name here is cross-checked against
`metaphysicist-nanos.json`** (client 18.8.50 extraction); pet *mechanics* and
*best-pet* guidance come from the cited AO community sources. Nothing is invented —
anything not traceable to a source or the local data is listed under **Unverified**.

The MP controls up to **three simultaneous manifestation pets**: one **Attack**, one
**Heal**, one **Mezz/Support**. Casting a second pet of a line replaces the first.

| Line | NanoLine (strain) | Summon skills | Role |
|------|-------------------|---------------|------|
| Attack | `AttackPets` (1015) | Time&Space (131) + Matter Creation (130) | Primary damage dealer |
| Heal | `HealPets` (1016) | Time&Space (131) + Biological Metamorphosis (128) | Heals owner/team/pets/NPCs/towers |
| Mezz/Support | `SupportPets` (1017) | Time&Space (131) + Matter Metamorphosis (127) | Single-target **stun** (not a calm) |

Older MP pet nanos gate on `VisualProfession(368)==12`; newer/Shadowlands ones on
`Profession(60)==12`.

---

## Attack pets — `AttackPets` (1015)

The MP's main weapon for most of the game; always run the strongest pet your skills (or a
wrangle) allow. Froob pets form a continuous MC/TS-gated ladder through six "emotion"
families — **Anger → Fury → Rage → Wrath → Frenzy → Enmity** — each with
Inferior / base / Superior / Greater / Supreme / Transcendent sub-tiers, topped by
**Lemur / Fiend / Demon**. Shadowlands adds the named high-end pets, ending in **The Rihwen**.

| Nano | id | Char lvl | Pet (template) / gate |
|------|----|----------|-----------------------|
| Summon Anger Manifestation | 43324 | 0 | self-scaling froob, templates 1–10 (earliest pet) |
| Fury Externalization | 43718 | 0 | template ~19 |
| Rage Materialization | 43736 | 0 | template ~44 |
| Wrath Incarnation | 43714 | 0 | template ~76 |
| Frenzy Embodiment | 43717 | 0 | template ~113 |
| Enmity Personification | 43716 | 0 | template ~155 |
| Summon Lemur | 29319 | 0 | template ~185 (top-froob) |
| Summon Fiend | 43738 | 0 | template ~191 |
| **Summon Demon** | 29318 | 0 | template ~197 — **strongest froob attack pet** |
| Summon Biazu the Vile | 225894 | 150 | SL, templates 160–180 (MC/TS > 874) |
| Summon Urolok the Rotten | 225896 | 175 | SL, templates 180–200 (MC/TS > 1150) |
| Summon Ettu the Cursed | 225898 | 201 | SL, templates 200–212 (MC/TS > 1300) |
| Summon Zhok the Abomination | 225900 | 201 | SL, templates 212–219 (MC/TS > 1700) |
| **Summon The Rihwen** | 254859 | 220 | templates 220–225 — **endgame best attack pet** |

*(Full 51-entry list incl. every Inferior/Superior/Greater/Supreme/Transcendent sub-tier
is in `metaphysicist-nanos.json` under "Pets - Attack".)*

---

## Heal pets — `HealPets` (1016)

10 strengths. Heals the owner, teammates, other pets, some NPCs, and Notum Wars towers.
The Soothing Spirit perk (SL) improves output.

| Nano | id | Char lvl | Pet template / gate |
|------|----|----------|---------------------|
| Calling of Medinos | 125738 | 0 | 14 (BioMet/TS > 83) |
| Calling of Salvinous | 125745 | 0 | 33 |
| Calling of Valentyia | 125744 | 0 | 55 |
| Calling of Sanoo | 125743 | 0 | 77 |
| Calling of Restite | 125742 | 0 | 99 |
| Calling of The Vivificator | 125741 | 0 | 123 |
| Calling of Altumus | 125740 | 0 | 146 |
| Calling of Curatem The Grand | 125739 | 0 | 169 |
| Calling of Belamorte | 125746 | 0 | 192 (strongest froob) |
| **Calling of Mortificant the Eternal** | 225902 | 207 | 215 — **best heal pet (SL)** |

---

## Mezz/Support pets — `SupportPets` (1017)

Applies a single-target **STUN** (target cannot move, cast, or attack) — **not** a calm.
One target at a time. 16 pre-SL versions + the SL **Yidira**.

| Nano | id | Char lvl | Pet template / gate |
|------|----|----------|---------------------|
| Lesser Distracting Sphere | 156128 | 0 | 5 (MatMeta/TS > 35) |
| Distracting Sphere | 156124 | 0 | 10 |
| Greater Distracting Sphere | 156126 | 0 | 15 |
| Supreme Distracting Sphere | 156117 | 0 | 20 |
| Lesser Deranged Mindreaver | 156127 | 0 | 31 |
| Deranged Mindreaver | 156123 | 0 | 39 |
| Greater Deranged Mindreaver | 156125 | 0 | 49 |
| Supreme Deranged Mindreaver | 156130 | 0 | 60 |
| Summoning of Absuum | 156129 | 0 | 72 |
| Summoning of Ignatus Mind-Clouder | 156122 | 0 | 90 |
| Summoning of Demenus | 156121 | 0 | 106 |
| Summoning of Duoco | 156120 | 0 | 135 |
| Summoning of Distral | 156119 | 0 | 150 |
| Summoning of Confane | 156118 | 0 | 166 |
| Summoning of Balbuto the Gibberer | 156131 | 0 | 181 |
| Summoning of Tumulten | 150309 | 0 | 199 (strongest non-SL) |
| **Summoning of Yidira** | 269516 | 0 | 210 — **best mezz pet (SL)** |

---

## Pet buffs (from "Pet Buffs / Pet Utility", 40 nanos)

- **Short-term attack-pet damage burst** — `PetShortTermDamageBuffs` (225): the **Evocation
  of …** ladder (Unrestrained Ferocity 205197 → Unleashed Malice 205195 → Relentless Fury
  205191 → Fathomless Rage 205183 → Implacable Hatred 205185 → Maddening Wrath 205187 →
  Pure Malevolence 205189 → The Abomination 205193 → **The Enraged 267598, +500 AddAllOff,
  L201**). Froob equivalents are the **Anima of …** set (151824–151831). All require the
  target to be `NPCFamily==97` (an attack pet). **Sacrifice Combat pet** (267533) destroys
  the attack pet for a +400 AddAllOff burst.
- **Long-term pet damage** — `MPPetDamageBuffs` (216): the **Instill With …** line (Rage
  116813, Terrible Anger 116821, Righteous Frenzy 116812, Fury 116815, Ferocious Purpose
  116816, Enduring Wrath 116817, **Malign Intent 116814**), persistent pet weapon-skill buffs.
- **Pet initiative / aggro** — `MPPetInitiativeBuffs` (217): **Chant of Frenzied Blows**
  116820 < **Effortless Strikes** 116811 < **High Chant of Frenzied Blows** 116818 <
  **High Chant of Effortless Strikes** 116819 (+920 all inits, +AggDef, +Aggressiveness).
- **Pet defensive / resist** — `PetDefensiveNanos` (816): Evocation of The Enlightened 267599
  (+250 AddAllDef), The Empowered 267600 (+300). `PetDamageOverTimeResistNanos` (817): The
  Cleansed 267617 (+300 NanoResist), The Pure 267609 (+600). Heal-delta: Healthy
  Manifestation 273374 (843). Damage-type/AR: Touch of Poison 275853 (863).
- **Damage transfer / utility** — `DamageToPet` (1024): Sacrificial Bond 300506, Sacrificial
  Shielding 267281 (redirect owner damage to pet). Pet Attention 269869 (1013), Pet Cleanse
  269870 (1047).

---

## Pet control / commands (AO-Universe)

| Command | Effect |
|---------|--------|
| `/pet follow` | Pets follow the owner (mode) |
| `/pet attack` | Pets engage current target (mode) |
| `/pet guard` | Follow + attack anything that attacks owner — **default after cast/zone** |
| `/pet behind` | Stay behind / retreat — issue after zoning to avoid uncontrolled aggro |
| `/pet wait` | Park: no move, no attack |
| `/pet heal` | Heal pet heals current friendly target (immediate) |
| `/pet report` | Pet reports health + target (immediate) |
| `/pet rename <name>` | Rename a pet (to target it by name) |
| `/pet terminate` | Terminate **all** pets |
| `/pet "PETNAME" terminate` | Terminate one named pet (any command can be name-scoped) |
| `/pet help` | Open Pet Info window with macro links |
| `/oe <skill#>` | Check over-equip vs a pet nano's skill requirement (wrangling) |

## Mechanics summary

- One Attack + one Heal + one Mezz pet at a time; same-line recast replaces.
- Pets are **no longer on a timer** — they persist, so MPs can accept a wrangle to
  over-equip and summon a higher pet, then let the buff drop.
- Mezz pet = **stun**, single target, locks out move/cast/attack.
- Attack damage/init buffs (225/216/217) need `NPCFamily==97`; defensive/resist/heal buffs
  target the pet directly (families 96/97/98).
- Default mode is **guard**; `/pet behind` after zoning is the convention.
- Commands are **per-pet** (rename → name-scope) so the three pets can be driven independently.
- Pet recovery: **Pet Steal Back** 269907 / Improved 269908 (Charm_Short 1022) reclaim a
  charmed/stolen pet.
- **Bot rule:** pick the best castable pet per line by filtering `metaphysicist-nanos.json`
  by `nanoLine` and choosing the highest skill-requirement nano the character's live stats
  satisfy — do **not** hardcode a per-level table.

## Unverified / flags

- Froob pet **character-level** breakpoints: `metaphysicist-nanos.json` reports `minLevel 0`
  for nearly all froob pet nanos — the real gate is nano-skill, not level. The "template ~N"
  values are the SummonPet template arg from local data (an ordering proxy), not a published
  level chart.
- Mezz count: Fandom says "16 versions"; local extract has **17** (16 + SL Yidira). Discrepancy
  is the SL addition, not independently reconciled.
- **No pet-warp nano** found in the MP-castable local extract (`PetWarp=1019` exists in the
  enum but no matching MP nano). Verify before implementing a warp action.
- Buff durations / stacking / short-vs-long overwrite rules not verified against wire data.
- "Best" endgame attack pet at 220 (Rihwen vs wrangled Zhok) is community consensus, not measured.
- Soothing Spirit heal-pet boost: from the Funcom forum guide; magnitude unverified.

## Sources

- LOCAL `metaphysicist-nanos.json` — all ids/names/strains/minLevels/castReqs/templates.
- LOCAL `AOSharp.Common/GameData/NanoEnums.cs`, `CharacterFlags.cs` — NanoLine + family enums.
- [AO-Universe — Basic Pet Commands Guide](https://www.ao-universe.com/guides/classic-ao/gameplay-guides-6/basic-pet-commands-guide)
- [AODB Wiki — Meta-Physicist](http://wiki.aodb.us/wiki/Meta-Physicist)
- [Anarchy Online Fandom — Meta-Physicist](https://anarchyonline.fandom.com/wiki/Meta-Physicist)
- [Funcom Forums — \[MP\] Meta-physicist guide](https://forums.funcom.com/t/mp-meta-physicist-guide/74897)
