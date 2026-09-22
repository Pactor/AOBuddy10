# Metaphysicist Weapons & Allowed Special Attacks

Authoritative profile for AOBuddy10 bot decision-making. Profession: **Metaphysicist (id 12)**.

Companion machine-readable files (same directory): `metaphysicist-weapons.json` (weapons + specials + full wield skills) and `metaphysicist-weapon-sources.json` (acquisition sources for every representative weapon id).

## Provenance (no invented data)

| Thing | Source |
|---|---|
| Item criteria (wield skill, specials, profession, level) | `the OmniCell datafiles\items.ocp` — full converted item pack, `OMNICELL-CONTENT` v3, client **18.8.50_EP1** (120,842 templates) |
| Id → Name | `the extracted client data\itemnames.sql` |
| Extractor | `tools\mp-weapon-extractor\` (net10; self-contained `.ocp` reader) |
| MP weapon-skill cost ("green" skills) | AOWiki `wiki.aodb.us/wiki/Meta-Physicist` and `Meta-Physicist:Weapons` |

Every weapon id/name/QL/requirement below was read from the files above. **Nothing is hand-authored.** The MP skill-cost color is the only web-sourced fact (items.ocp does not store a per-profession cost).

## How a weapon states its requirements (verified from OmniCell.Core model)

A weapon's wield requirements live in its **ToWield action** (`OmniCell.Enums.ActionType.ToWield = 8`). Each requirement is `{Statnumber, Operator, Value}`:

- **Operator `GreaterThan` (2)** means **`stat >= Value`** (AO comparisons are inclusive).
- **Wield-skill stat** (Bow 111, Pistol 112, 1hBlunt 102, …) = the weapon **type** and skill needed.
- **Special-attack stat** present in the criteria = a special that weapon **allows** (Value = skill needed at that QL).
- **Profession stat (60)** with `EqualTo (0)` + a profession id = a profession lock.
- **Level stat (54)** = level requirement.

**Requirement values scale with QL.** A named weapon spans a QL range stored as low/high templates; values shown are for the exact template QL listed. Example (Bow-Blaster line, no profession lock → MP-usable):

- `271433 Bow-Blaster - 000` ql1: `Bow>=10` → `271434` ql300: `Bow>=1500` (no special).
- `271437 Bow-Blaster - 402` ql1: `Bow>=10, AimedShot>=5` → `271438` ql300: `Bow>=1500, AimedShot>=750`.
- Playable line `122767 Ill-Treated Bow-Blaster` … `122785 Premium Bow-Blaster` ql200: `Rifle>=765, Bow>=510, FlingShot>=425, AimedShot>=425`.

(The domain-fact figures "Bow>638 / AimedShot>319" are the same Bow-Blaster line at an intermediate QL.)

## Special-attack stat set (on-demand)

`Brawl(142)`, `Dimach(144)`, `SneakAttack(146)`, `FastAttack(147)`, `Burst(148)`, `FlingShot(150)`, `AimedShot(151)`, `FullAuto(167)`, `Backstab(489)`.
Passive (not on-demand, ignore): `Riposte(143)`, `Parry(145)`. Note `BowSpecialAttack(121)` is a support **skill** for bow Aimed Shot, not itself an on-demand special.

**Runtime rule for the bot:** for the equipped weapon, read its ToWield criteria; every requirement whose stat is in the special set is a usable special on that weapon.

### `wield` = complete "skills to train to equip"

In the JSON, each weapon's `wield` lists **every** trainable-skill requirement in its ToWield action (weapon skill(s) plus any Multi Melee/Multi Ranged or other skill), each `{skill, requiredValue}`; `specials` lists the special-attack skills. Non-skill gates (expansion, side, breed, title level, Strength) are not skills and are excluded from `wield` (expansion is captured separately in the sources file).

**Dual-wield / Multi skills:** In this data, MultiMelee(101)/MultiRanged(134) appear as a ToWield criterion on only 6 weapons — all "Small Ebony Figurine" variants locked to non-MP professions. **No MP-usable weapon carries a Multi requirement.** Dual-wielding is gated by the *character's* Multi Melee/Ranged skill for the off-hand slot, not by a per-weapon criterion; an MP dual-wields by training Multi Ranged/Melee and equipping two normal weapons (each still only requires its own wield skill).

## Which weapon TYPES an MP uses

Determination: MPs are a nano profession with limited weapon IP. AOWiki states MPs **"spend less IP on 1HB, 2HE, Bow and Pistol"** (their reduced-cost weapon skills); the MP:Weapons guide recommends **1h Blunt, Pistol, Melee Energy, Bow**. Cross-checked against the data: **59 weapons are hard-locked to Metaphysicist** (Profession EqualTo 12), and they fall exactly into Bow(16), 1hEdged(14), MeleeEnergy(13), 2hBlunt(6), 2hEdged(5), 1hBlunt(3), Pistol(2) — confirming these are the intended MP weapon families.

Practical MP weapon types (counts = weapons of that type in data / MP-equippable subset; specials = union observed across MP-usable weapons of that type):

| Weapon skill | Stat | MP cost | Weapons (total / MP-usable) | Allowed specials observed | Notes |
|---|---|---|---|---|---|
| **Bow** | 111 | reduced (green) | 377 / 364 | AimedShot, FlingShot | Signature MP line; MP-only Shadowlands bows + generic bows |
| **Pistol** | 112 | reduced (green) | 1398 / 1160 | AimedShot, Burst, FlingShot, FullAuto | Common ranged option; single or dual |
| **1h Blunt** | 102 | reduced (green) | 854 / 822 | Brawl, Dimach, FastAttack, SneakAttack | Melee / energy shields (often + Melee Energy) |
| **2h Edged** | 105 | reduced (green) | 764 / 605 | Brawl, Dimach, FastAttack, SneakAttack | Cheap melee; MP-only 2he exist |
| **Melee Energy** | 104 | guide-recommended | 280 / 263 | Brawl, Dimach, FastAttack, SneakAttack | MP-only energy weapons/shields (Jupiter Aegis, The Argument) |
| **1h Edged** | 103 | standard | 960 / 887 | Brawl, Dimach, FastAttack, SneakAttack | MP-only Shank of Maze line |
| **2h Blunt** | 107 | standard | 503 / 447 | Brawl, Dimach, FastAttack, SneakAttack | MP-only 2hb (Howlet) |

**Technically equippable but atypical** (usable only if the skill is raised via implants/buffs; not normal MP play): AssaultRifle, Rifle, Piercing, Shotgun, MG/SMG, Ranged Energy, Grenade, Martial Arts, Heavy Weapons. Per-type MP-usable counts are in the JSON.

## Representative real weapons (id | name | QL | wield | specials)

**Bow**
- `214333` Sprite Bow — ql100 — Bow>=600 — (no special) — *MP-only*
- `214338` Bow of Sympathy — ql100 — Bow>=600 — (no special) — *MP-only*
- `122719` Kevlar Bow — ql100 — Bow>=470 — AimedShot>=235
- `212368` Sancrosanct Shere Bow — ql100 — Bow>=565 — AimedShot>=565
- `288286` Special Edition Kyr'Ozch Crossbow — ql150 — Bow>=699 — FlingShot>=349 — *MP-only*
- `122785` Premium Bow-Blaster — ql200 — Rifle>=765, Bow>=510 — FlingShot>=425, AimedShot>=425
- `271438` Bow-Blaster - 402 — ql300 — Bow>=1500 — AimedShot>=750

**Pistol**
- `122990` Worn Kolt 58 Magnum — ql100 — Pistol>=453 — FlingShot>=226
- `123786` MTI 99 Rebel Match Pistol — ql100 — Pistol>=428 — Burst>=321, FlingShot>=214
- `122282` Worn Kolt PDW-37 — ql100 — Pistol>=439, MGSMG>=293 — Burst>=367, FlingShot>=244
- `288293` Special Edition Kyr'Ozch Pistol — ql150 — Pistol>=699 — Burst>=499, FlingShot>=349 — *MP-only*

**1h Blunt**
- `122354` Loose Metaphysic Energy Shield — ql60 — 1hBlunt>=136, MeleeEnergy>=91 — FastAttack>=76, Brawl>=91, Dimach>=30
- `245669` Superior Skylight Shield — ql160 — *MP-only*
- `288292` Special Edition Kyr'Ozch Hammer — ql150 — *MP-only*

**Melee Energy** (all MP-only)
- `208069` Jupiter Aegis — ql40 — MeleeEnergy>=170 — FastAttack>=85, Brawl>=102
- `223467` The Argument — ql150 — MeleeEnergy>=775 — Dimach>=155
- `226329` Notum Saturated Animal Bone — ql1 — MeleeEnergy>=10 — SneakAttack, FastAttack, Brawl, Dimach

**1h Edged** — `238956` Shank of Maze — ql1 — 1hEdged>=10 — SneakAttack>=5, FastAttack>=5, Brawl>=6 — *MP-only*
**2h Edged** — `246715` Panther — ql100 — *MP-only*
**2h Blunt** — `246705` Howlet — ql100 — *MP-only*

Full representative lists (with QL-scaled values) are in `metaphysicist-weapons.json`.

## Key non-weapon MP gear (categories + representative ids)

Representative, not exhaustive; ids verified present in `itemnames.sql`. Stat requirements for gear were **not** extracted here (weapons were the focus).

- **NCU memory** (raises Max NCU for the MP's many buffs/pet buffs): e.g. `303990` "100 NCU Memory", `303992` "25 NCU Memory", `304504` "3 NCU Memory".
- **Belt / NCU deck** (holds NCU memory): e.g. `304503` "Belt Component Platform Ti-200X", `303998` "Belt Component Platform 6K-X".
- **Symbiants** — MPs favor **Support** and **Control** lines for Intelligence/Psychic/Sense and nano skills: e.g. Support `305220`/`305218` "Prototype … Symbiant, Support Unit"; Control `305142`/`305140` "Prototype … Symbiant, Control Unit".
- **Implants** (focus skills — Nano skills, Comp Literacy, Treatment, Psychic/Intelligence): profession implant reference `302097` "Basic ICC Meta-Physicist Specific Implants".
- **Notum-related defensive**: e.g. `253017` "Notum Shield" (item), `303066` "Damaged Notum Tower Shield".

## Acquisition sources (`metaphysicist-weapon-sources.json`)

Sources for every representative weapon id. Per the no-invention rule, `how` is `"unknown"` unless verifiable: none of these ids appear in OmniCell's local drop/vendor tables (mobdroptable/vendortemplate/moblootprofiles), and public AO DBs were unreachable in this environment (auno.org anti-bot, aoitems.com parked, fandom 402), so no vendor/mob/mission was recorded rather than guessed. The **`expansion`** field IS data-traceable — from each weapon's ToWield Expansion(389) requirement (ExpansionFlags enum): Sancrosanct Shere Bow (212368) and Superior Skylight Shield (245669) require **Shadowlands**; the Special Edition Kyr'Ozch Crossbow/Pistol/Hammer (288286/288293/288292) require **Alien Invasion**. The other 15 carry no expansion wield-gate → `expansion: null`.

## Caveats

- MP weapon-skill "green/cheap" rating is from AOWiki, not items.ocp (which gates only by skill value).
- All requirement values are QL-specific (low/high template endpoints); a name spans a QL range.
- EquipSlot (Weap_RightHand 0x06 / Weap_LeftHand 0x08) is not stored in items.ocp; "weapon" here = has a ToWield action requiring a weapon wield skill.
- The AOBuddy SDK `ItemData.bin` was evaluated and rejected as the source: it is a curated subset (only 82 weapons, and the Bow-Blaster was absent). items.ocp (10,730 weapons) is authoritative.
