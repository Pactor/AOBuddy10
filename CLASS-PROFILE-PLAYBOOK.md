# AO Class Deep-Dive Playbook (AOBuddy10 / AODB)

How to build a complete, data-driven profile for ANY Anarchy Online profession and have it
render in the AODB web app — without hand-holding. This is the process used for the
Meta-Physicist; repeat it per class by changing the profession id + slug and re-running the
same passes.

## 0. Ground rules (non-negotiable — the project owner has zero tolerance for made-up data)
- **No invention.** Every id/name/number traces to LOCAL client data or a reputable AO web
  source. If it can't be verified, mark it `unknown` / list it under `_unverified` — never guess a
  vendor, mob, stat, or nano.
- **Local data is truth; the web organizes and fills gaps.** Verify every id/name against
  `itemnames.sql`.
- **Cite sources** in a `_sources` field on every JSON file. Flag coverage honestly.
- Prefer building a small **re-runnable extractor** (net10 console) over eyeballing, so the next
  class is a parameter change, not new work.

## 1. Profession ids (AOSharp.Common/GameData/Profession.cs)
1 Soldier · 2 MartialArtist · 3 Engineer · 4 Fixer · 5 Agent · 6 Adventurer · 7 Trader ·
8 Bureaucrat · 9 Enforcer · 10 Doctor · 11 NanoTechnician · 12 Metaphysicist · 13 Monster (NOT
playable) · 14 Keeper · 15 Shade.
**slug** = lowercased name, no spaces/hyphens (Metaphysicist→`metaphysicist`, NanoTechnician→
`nanotechnician`, MartialArtist→`martialartist`). The web app already lists all 15; a class
"lights up" as its `<slug>-*.json` files appear.

## 2. Authoritative LOCAL data + toolchain
- **Nano formulas/effects:** `E:\Funcom\OmniCell\OmniCell\Datafiles\nanos.ocp` (OMNICELL-CONTENT pack).
- **Item templates (criteria = requirements, incl. profession/wield/specials/expansion):**
  `E:\Funcom\OmniCell\OmniCell\Datafiles\items.ocp` (~120k templates, client 18.8.50_EP1, ~10.7k weapons).
- **Id→Name:** `E:\Funcom\attic\extracted-client-data\itemnames.sql` (table `itemnames`: Id, Name, ItemType).
- **Loaders:** `OmniCell.Core.dll` (net10) at
  `E:\Funcom\OmniCell\OmniCell\Libraries\Source\OmniCell.Core\bin\Release\net10.0\OmniCell.Core.dll`
  — `NanoLoader.CacheAllNanos(path)` → static `NanoList` (id→nano with Actions/NanoStrain/Events→Functions);
  `ItemLoader` / `Item` for item criteria. Source under `...\OmniCell.Core\` (NanoLoader.cs, ItemLoader.cs, Item.cs).
  MsgPack.Cli must be resolvable next to the DLL.
- **Enums for joining:** `E:\Funcom\AOBuddy10\AOSharp.Common\GameData\` — `Stat.cs`, `Profession.cs`,
  `EquipSlot.cs`, `NanoEnums.cs` (NanoLine, 994 entries), `PerkHash.cs`.
- **GOTCHA:** the legacy `attic\extracted-client-data\nanos.dat/items.dat` are the OLD zlib+MessagePack
  extractor cache (header 18.8.50_EP1) that the current net10 loader CANNOT read (throws on inflate).
  Use the `.ocp` packs. Both are the same 18.8.50_EP1 extraction.
- Existing re-runnable extractors (parameterize by profession id): `E:\Funcom\AOBuddy10\tools\mp-nano-extractor\`,
  `tools\mp-weapon-extractor\`, `tools\mp-nano-sources\`.

## 3. The encodings you need (verified against the data)
- **A nano is castable by profession P** when one of its cast Actions (`ActionType.ToUse=3`) carries an
  `EqualTo` requirement on **Profession (stat 60) == P** OR **VisualProfession (stat 368) == P**. The
  VisualProfession path is essential — many older profession nanos lock via 368, not 60 (missing it
  undercounts, e.g. dropped 6 of 7 MP heal pets).
- **A weapon is usable by profession P** unless its `ToWield` action has a `Profession(60) EqualTo`
  lock to a DIFFERENT profession. A hard `Profession EqualTo P` = profession-only item.
- **Weapon wield requirements** = every skill-stat requirement in `ToWield` (each `{skill, requiredValue}`),
  EXCLUDING the special-attack stats. Include Multi Melee (101) / Multi Ranged (134) if present (they are
  rare and usually on the OFF-HAND; most weapons don't carry them — dual-wield is gated by the CHARACTER's
  Multi skill, not the weapon).
- **Weapon special attacks** = special-attack stats present in `ToWield`: Brawl 142, Dimach 144,
  SneakAttack 146, FastAttack 147, Burst 148, FlingShot 150, AimedShot 151, FullAuto 167, Backstab 489.
  The `requiredValue` = skill you must train to use that special. (Riposte/Parry are passive — not on-demand.)
- **Expansion gate** = `Expansion (stat 389) BitAnd (op 22)` requirement in ToUse/ToWield. Bits
  (AOSharp `ExpansionFlags`): bit1(2)=Shadowlands, bit3(8)=AlienInvasion, bit5(32)=LostEden. This is the
  AUTHORITATIVE, no-guess signal for "requires Shadowlands/AI/LE".
- **Nano crystal ↔ nano:** find the crystal item whose `UploadNano` function (FunctionType 53019) argument
  is the nano id, to read the crystal's own criteria (shop/expansion).
- **Symbiants** are items with names containing "Symbiant"; lines Artillery/Control/Extermination/Infantry/
  Support, units Alpha/Beta/… + Xan. Implant slots in EquipSlot.cs are the `Imp_*` entries (Eye 0x21 … Feet 0x2D).

## 4. Reputable WEB sources (for organization + what local data lacks)
- ao-universe.com (guides: professions, symbiants, leveling, tradeskills), wiki.aodb.us (profession pages,
  skills/costs, implant clusters, breeds), forums.funcom.com (build/guide threads), Auno.
- **Blocked to automation (expect 403/402/parked):** auno.org (Anubis anti-bot), aoitems.com (parked),
  anarchyonline.fandom.com (402). Do NOT record fuzzy secondhand claims from these — leave `unknown`.
  Weapon/item DROP locations are the main thing local data lacks and these block; pull those from
  ao-universe / wiki guides instead, or leave `how:"unknown"`.

## 5. Category map — a complete class profile = these files in
`E:\Funcom\AOBuddy10\AOBuddy\GameData\profiles\` (per-class), keyed by `<slug>-`:
| file | tab | content |
|---|---|---|
| `<slug>-build.json` | Build | breed rec, perks, research, playstyle, skill priorities |
| `<slug>-nanos.json` (+`-nano-sources.json`) | Nanos | every castable nano by function + where-to-get |
| `<slug>-pets.json` | Pets | pet lines/progression/buffs/commands (pet classes only) |
| `<slug>-weapons.json` (+`-weapon-sources.json`) | Weapons | usable weapon types, reps, full wield reqs, specials, sources |
| `<slug>-implants.json` | Implants | cluster-implant build (leveling/budget) |
| `<slug>-symbiants.json` | Implants&Symbiants | Xan/Alpha + SL best-in-slot + alpha crafting |
| `<slug>-gear.json` | Gear | armor sets, NCU, nano-focus pieces |
| `<slug>-buffs.json` | (Buffs) | self-buffs, buffs wanted from other pros, buffs given |
| `<slug>-leveling.json` | (Leveling) | 1→220+AI path, hunting spots, keys, quests |
| `<slug>-endgame.json` | (Endgame) | raids, gear-progression order, MP role |

**Shared (all professions, build once) in `profiles\reference\`:** `breeds.json`, `professions.json`,
`skillcaps.json` (69 skills × 14 professions cost factors, indexed by `_professionOrder`), `implants.json`
(cluster→slot mapping). These are class-agnostic — don't regenerate per class.

## 6. JSON schemas (match the app renderers in tools/aodb/app.py)
Keep these shapes or update the renderer. Files may carry a UTF-8 BOM — the app loader uses `utf-8-sig`.
- **nanos:** `{categories:{<Category>:[{id,name,nanoLine,minLevel,effectSummary,castReqs}]}, categoryCounts, totalCount}`
- **nano-sources / weapon-sources:** `{sources:{"<id>":{how,where,expansion,tier,notes}}}` (how = shop|mission-roll|drop|shadowlands|quest|unknown)
- **weapons:** `{usableWeaponTypes:[{weaponSkill,weaponsTotalInData,weaponsMpUsable,role,mpSkillCost,allowedSpecialsObservedForType:[...],representativeWeapons:[{id,name,ql,wield:[{skill,requiredValue}],specials:[{special,requiredValue}],professionLock:[...]}]}], metaphysicistOnlyWeapons:{count,byWeaponSkill:[{weaponSkill,count}]}}` (rename the profession-only block per class or generalize)
- **build:** `{playstyle:{role,summary,strengths[],weaknesses[],tips[]}, breed:{recommended,reasoning,alternatives[]}, skillPriorities:[{skill,target,why}], perks:[{name,type,line,effect,recommendedAtLevel,priority}], research:[{name,track,effect}]}`
- **pets:** `{summary, petTypes:[{role,nanoLine,description,mechanics,progression:[{nano,id,minLevel,note}]}], petBuffs:[{name,id,effect}], commands:[{command,what}], mechanics:[...]}`
- **implants (cluster build):** `{summary, build:[{slot,slotLabel,shiny:{skill,note},bright:{...},faded:{...},ql,notes}], ladder}`
- **symbiants:** `{summary, bestInSlot:[{slot,slotLabel,symbiant,line,unit,ql,level,keyBonuses:{},reqs:{},source,id}], xanAlphaNote, alphaCrafting}`
- **gear:** `{summary, armorSets:[{name,slotsCovered[],keyBonuses:{},levelRange,source,priority}], headwear:[{name,effect,id}], ncuAndBelt:[...], misc:[{name,slot,effect,id}]}`
- **buffs:** `{selfBuffs:[{name,id,effect,keepUp,note}], wantedFromOthers:[{buff,fromProfession,effect,useCase}], generalBuffs:[...], givesToOthers:[{name,id,effect,wantedBy}]}`
- **leveling:** `{brackets:[{levelRange,where[],fight,petNotes,xpType}], keyQuests:[{name,where,reward,level}], keys:[{name,forWhat,how}], froobVsPaid, tips[]}`
- **endgame:** `{content:[{name,type,level,whatDropsForMP,mpRole,note}], progressionOrder:[{step,what,why}], notes[]}`
Every file also: `_sources:[...]` and (where relevant) `_unverified:[...]`.

## 7. The web app (tools/aodb/app.py)
- stdlib Python 3, no deps. Run: `python app.py` from `tools/aodb` → http://localhost:8888 (or the `aodb`
  launch config). Threaded server; serves `no-store` so no stale pages.
- Reads `profiles/` + `profiles/reference/`. `/api/profile?prof=<slug>` merges all `<slug>-*.json`.
  Adding a class needs NO app change — drop its JSON files and its tabs populate; tabs show "not
  generated yet" until then. Add a new category = add a `<slug>-x.json`, a `renderX()`, a tab entry, and a
  `profile_for` part.
- When you change app.py's HTML/JS you MUST restart the server (kill port 8888, relaunch) — the page is
  no-cache so the browser picks it up on next load. Data-file changes need no restart.

## 8. Run it for a class (the loop)
1. Pick profession id + slug. Shared `reference/` already done — skip it.
2. Dispatch background research agents, ONE per category file above, each with: the profession id/slug,
   the local toolchain + encodings in §2–3, the schema in §6, the rules in §0, and "be exhaustive/proactive
   — find the non-obvious things." Reuse/parameterize the mp-*-extractor tools for nanos/weapons/sources.
3. Each agent writes its `<slug>-*.json` (+`.md`) into `profiles\`, verifies ids vs itemnames.sql, reports
   coverage + `_unverified`.
4. As files land, refresh the app; do a render check (no `[object`, no `undefined`, not pending) per tab and
   fix schema drift in the renderer.
5. Relay honest coverage: what's verified vs unknown (esp. drop locations, which the blocked DBs gate).

## 9. EXHAUSTIVE means exhaustive (the standard — do not repeat the mistake)
A deep dive is **EVERYTHING**, enumerated from the data — the user's words: **"EVERYTHING THAT MP CAN EQUIP."**
Not representative samples, not "top picks," not per-category summaries. The load-bearing deliverable is the
**complete MP-equippable catalog**: EVERY item with an equip action (ToWield weapons / ToWear armor, implants,
symbiants, jewelry, NCU, HUD, utility, belt, deck) whose requirements do NOT profession-lock it to a
profession other than the target (Profession(60) EqualTo other = exclude; ==target = target-only; none =
include; ignore whether skill/ability reqs are met — those are twinkable, just record them). Organize by
equip SLOT category, per MODEL with QL range + full reqs + key stats + specials + expansion + source.
Curated best-in-slot layers ON TOP of the complete catalog — it never replaces it. Every OTHER category the
same way: perks = every line/rank; symbiants = every unit/tier/QL; nanos = every version; armor = every set/slot.
Output schema (may be split per slot-category if huge): `metaphysicist-equippable.json`
`{ profession, totalModels, totalTemplates, bySlot:[{slotCategory,count,items:[{id,name,qlRange,reqs,keyStats,specials,expansion,professionLock,itemType}]}], _unverified, _sources }`.
**Never call sampled/summary work "done"; state coverage as X of Y.** See memory [[deep-dive-means-exhaustive]].

## 10. Status / provenance / RESUME POINT
Built first for **Metaphysicist** (id 12), client 18.8.50_EP1. Extractors + all `metaphysicist-*.json` under
`AOBuddy\GameData\profiles\`; see each file's `_sources`/`_unverified`.
- **Truly exhaustive so far:** nanos (286) and symbiants (13 slots). Everything else (build, weapons, gear,
  buffs, leveling, endgame) is a SUMMARY/sample and must be redone to the §9 standard.
- **RESUME HERE (paused for compaction at ~8% usage):** build `metaphysicist-equippable.json` — generalize
  `tools/mp-weapon-extractor/Program.cs` (already parses items.ocp per template) to ALL equip slots: read
  each item's ToWear/ToWield placement flags, Wear modifiers (funcType 53045), categorize by slot, apply the
  §9 include/exclude rule. Then wire an "Equippable" view in the app (grouped by slot, searchable) and take
  every remaining category to exhaustive. The weapon extractor agent was put ON HOLD mid-session; re-dispatch
  fresh per the §8 loop with the §9 standard.
