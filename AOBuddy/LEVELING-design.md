# AOBuddy — Leveling feature design

Research + design for a class-agnostic, config-driven "leveling" feature (auto-train
skills, goal-directed training, auto-learn nanos) for the clientless C# teammate bot.

**Status:** research complete, no code written. This doc is the spec.

All message/field names below were confirmed by reflecting the fork the bot actually
runs — `AOSharp.Common.dll` in
`E:\Funcom\AOSharp.Clientless\packages\AOSharpSDK.1.0.89\lib\net48\` — not just the
OmniCell source. Where the fork's names differ from OmniCell's source models, the fork
name is what the bot must use; both are given.

Evidence base: 15 marked retail captures under `E:\Funcom\captures` /
`E:\Funcom\sniffs` (client `18.8.62_EP1`), decoded with
`E:\Funcom\OmniCell\Tools\Capture\bin\PcapDecode.exe`. The relevant marks are the
"auto distributing skills" / "manually raising first aid skill" / "lack ip nm" events
(char creation + skill window) and the "buying class nano programs" / "trying to learn
nano i do not have the skills to learn yet" events.

---

## 1. TRAIN A SKILL / ABILITY  (spend IP)

**Message: `SkillMessage`  —  client → server**  (also echoed server → client)
`N3MessageType.Skill = 1042306656 = 0x3E205660` (this dword is the on-wire
discriminator — it appears at a fixed offset in every capture, confirming the type).

Fork fields (`SmokeLounge.AOtomation.Messaging.Messages.N3Messages.SkillMessage`):

| Field | Type | Meaning |
|-------|------|---------|
| `Skills` | `GameTuple<Stat, uint>[]` | (skill/ability id, **new absolute value**) pairs |
| `Identity` | `Identity` | the character (self) |
| `Unknown` | `byte` | 0 |

`Stat` here is `AOSharp.Common.GameData.Stat` (the same enum `GetStat`/`TryGetStat`
use). `GameTuple<,>.Value1` = the stat, `.Value2` = the value.

### Values are ABSOLUTE, not deltas — proven two ways

1. **Server handler.** OmniCell's inbound handler
   `E:\Funcom\OmniCell\OmniCell\Server\ZoneEngine\Core\MessageHandlers\SkillMessageHandler.cs`
   does, per tuple:
   ```csharp
   client.Controller.Character.Stats[(int)stat.Value1].Value = (int)stat.Value2;   // assignment, not +=
   ```
   then recomputes total IP spent over the whole sheet and sets `Stat.IP`. It replies
   with a `SkillMessage` (server→client) carrying the resulting values **plus stat 53
   (IP)**. So the client tells the server "make these skills equal these numbers"; the
   server validates by recomputing cost.

2. **Captures.** The char-creation "auto distribute" sends the full 23-entry batch of
   abilities+skills with growing absolute values across passes (abilities 16–21 = 9 in
   one snapshot, 15 in a later one — a resent full snapshot, which only makes sense as
   absolute targets):

   `captures/newchar_s7.csv` line 133 (client), abilities set to 9:
   ```
   SkillMessage Skills=[23] raw=01-35-...-3E-20-56-60-...-00-00-00-17   (0x17 = 23 tuples)
     10 00 00 00 | 09 00 00 00   Strength(16) = 9
     11 00 00 00 | 09 00 00 00   Agility(17)  = 9
     ... 7B 00 00 00 | 0A 00 00 00  FirstAid(123) = 10 ...
   ```
   `captures/newchar_s20.csv` line 184 (client), same 23 stats, abilities now 15
   (`0x0F`), FirstAid 20 (`0x14`).

The tuple wire layout inside `Skills` is `statId:int32-BE, value:int32-BE` repeated,
preceded by an int32-BE count.

### To raise ONE skill
Send a `SkillMessage` whose `Skills` is a **single** tuple `(Stat.X, desiredNewValue)`.
The handler loops the array and accepts any subset, so you do not have to resend the
whole sheet. (`Identity` = local player, `Unknown` = 0.)

### IP available / IP spent reporting
- **IP available** is a normal stat: `Stat.IP = 53`. Read it live with
  `TryGetStat(Stat.IP, out int ip)`. The server pushes it via `StatMessage` /
  `SetStatMessage` and in the `SkillMessage` echo after a train.
- On level-up, `NewLevelMessage` (server→client, see §4) carries `AvailableIp` — the
  IP balance for the new level.
- There is **no separate "IP spent" field**; spent = (IP earned for level) − (IP now).

### Related actions seen in the enum (context)
`CharacterActionType.ResetSkill = 0x9A` exists (refund a skill) but was **not** observed
in captures — out of scope for leveling. There is no "use a trainer NPC" step: skill
raises in modern AO go straight through `SkillMessage`; no trainer interaction precedes
them in any capture.

---

## 2. IP / COST MODEL

AO's per-skill IP cost is the classic **cost-factor** system: each (skill × profession)
has a factor (1.0 green … 4.0 dark-blue); the cost to raise a skill from value `v` to
`v+1` is `floor(factor × v)`. Abilities use a per-breed factor.

**The full froob cost matrix already exists in this repo** and can be ported directly:
`E:\Funcom\OmniCell\OmniCell\Libraries\Source\OmniCell.Stats\SkillUpdate.cs` →
`CalculateIP(IStatList)`. It contains:

- `skillCosts[69, 15]` — row = skill id (100–168), columns 1–14 = the 14 professions'
  cost factors (order: Sol, MA, ENG, FIX, AGE, ADV, TRA, CRA, ENF, DOC, NT, MP, KEP, SHA).
- `attributeCost[6,5]` — ability id (16–21) then the factor per breed
  (Solitus, Opifex, Nanomage, Atrox).
- `baseAttributes[7,6]` — each breed's starting ability values (the floor you pay up from).
- `professionMatrix[15]` — maps profession id → column index into `skillCosts`.

The IP formula (from the same file):
```
ability:  cost += attributeCost[ability, breed] * v      for v = base .. current-1
skill:    cost += floor(skillCosts[skill, profCol] * v)  for v = 5   .. current-1
```
and the **IP earned by a level** is in `SkillMessageHandler.cs` (`baseIp`): `1500 +
(min(level,14)-1)*4000` then `+10000/level` for 15–49, `+20000` 50–99, `+40000` 100–149,
etc. Available IP = earned − spent, which is exactly `Stat.IP`.

**Source decision:** we do **not** need to read a DB at runtime. Port `skillCosts` /
`attributeCost` / `baseAttributes` / `professionMatrix` into a small static
`IpCost` helper. Read the live inputs from stats:
`Stat.Profession`, `Stat.Breed`, `Stat.Level`, the skill's current value, and
`Stat.IP`. `LocalPlayer.GetSkillMax(Stat)` gives the level cap for a skill (don't train
past it).

**Caveats / gaps in the ported table:**
- It is **froob-era** (68–69 skills, IDs 100–168; no SL-only skills, no research/perks,
  no LE). If the alt runs Shadowlands/AI content the SL skill costs and the higher
  per-level IP bands may be incomplete — verify before trusting cost math for an SL toon.
- OmniCell's handler does **not reject** an unaffordable train (it just sets the value
  and lets IP underflow). **Retail rejects** it — that is the "lack ip nm" capture at
  `captures/pcap/marked-20260909-152820.marks.txt` ("manually raising first aid skill"
  → "lack ip"). So the bot **must** pre-check affordability itself with the ported cost
  model and never send a raise it can't pay for.

---

## 3. UPLOAD / LEARN A NANO

**There is no dedicated client "upload nano" packet in any of the 15 captures.**
`CharacterActionType.UploadNano = 0xCC` exists in the enum but was **never sent** — I
enumerated every client `CharacterActionMessage` action code across all captures and got
`{0x69,0x57,0x13,0x8e,0xde,0xdd,0xdc,0x78,0x6c,0x98,0x80,0xa3,0x70,0x1a,0x1c,0xbb}` and
**no 0xCC**.

The actual mechanism is: **"use" the nano crystal item in inventory.** Right-clicking a
nano crystal uploads its program; the server consumes the crystal and pushes a
`SpellListMessage`, after which the nano id appears in `LocalPlayer.SpellList`.

**Message: `GenericCmdMessage`  —  client → server**
`N3MessageType.GenericCmd = 1381132376 = 0x52526858` (matches the wire discriminator).

Fork fields (`...N3Messages.GenericCmdMessage`) — **names differ from OmniCell source:**

| Fork field | OmniCell-source name | Meaning |
|------------|----------------------|---------|
| `Temp1` (int) | Verification | 0 |
| `Count` (int) | Serial | client command serial |
| `Action` (`GenericCmdAction`) | Action | **`Use = 3`** |
| `Temp4` (int) | Flag | 0/1 |
| `User` (`Identity`) | User | self |
| `Source` (`Nullable<Identity>`) | — (fork only) | usually null |
| `Target` (`Identity`) | Target (`Identity[]`) | **the crystal**: `Identity(IdentityType.Inventory, itemInstance)` |

Note the fork's `Target` is a **single `Identity`** (source model had an array). For a
plain crystal upload one target is correct; the two-identity variants
(`UseItemOnItem`, `UseItemOnCharacter`) are for tradeskills/stims, not nano learning.

`GenericCmdAction` in the fork: `None=0, Get=1, Drop=2, Use=3, Repair=4, UseItemOnItem=5`
(no `UseItemOnCharacter`).

Example client `Use` from `dec_newchar_s20` (a use-item command, same shape a crystal
upload takes):
```
GenericCmdMessage Action=Use Flag=1 User=CanbeAffected:227558005 Target=[1]
  raw=01-3F-...-52-52-68-58-...-00-00-00-03(Use)-00-00-00-01(flag)-...-<user>-<target>
```

**Simplest path for the bot: don't hand-build the packet.** The SDK already exposes
`Item.Use(...)` (used today for stims) and the bot calls `item.Use()` successfully — for
a nano crystal that same call produces the `GenericCmdMessage Use` that uploads it.
Confirmed `Item` methods in the fork: `Use(SimpleChar target, bool setTarget)`,
`Use(Identity slot)`, `UseOn(Dynel/Identity)`, `UseItemOnItem(...)`, plus
`HasPendingUse`. Building the `GenericCmdMessage` by hand via `Network.Send` is the
fallback if `Item.Use()` on a crystal turns out not to route to upload.

**Fork type support confirmed:** `GenericCmdMessage`, `GenericCmdAction`,
`SpellListMessage` all exist in `AOSharp.Common.dll`. (The fork's `SpellListMessage`
exposes no public payload fields — irrelevant, because the bot reads uploaded nanos via
`LocalPlayer.SpellList`, not by parsing that message.)

---

## 4. WHAT THE SDK EXPOSES

Reflected from the fork (`AOSharp.Common.dll` / `AOSharp.Core.dll`) and cross-checked
against current `Main.cs` usage.

**Stats / skills (all via `Stat` enum, `AOSharp.Common.GameData.Stat`, 705 members):**
- Six abilities: `Strength=16, Agility=17, Stamina=18, Intelligence=19, Sense=20,
  Psychic=21`.
- IP + level: `IP=53`, `Level=54`, `TitleLevel=37`, `XP=52`, `NextXP=350`.
- Combat/nano skills (ids used in the char-creation batch and useful as "combat skills"):
  `MartialArts=100, _1hBlunt=102, _1hEdged=103, Skill2hEdged=105, Piercing=106,
  _2hBlunt=107, Grenade=109, Bow=111, Pistol=112, Rifle=113, Shotgun=115,
  AssaultRifle=116, MeleeInit=118, RangedInit=119, PhysicalInit=120, Brawl=142,
  Dimach=144, FastAttack=147, Riposte=143, MultiMelee=101, MultiRanged=134,
  BodyDevelopment=152, NanoPool=132, DodgeRanged=154, EvadeClsC=155, DuckExp=153,
  FirstAid=123, Treatment=124, ComputerLiteracy=161, NanoProgramming=160`.
- Nano schools: `MatMet/BioMet(128), PsychoModi(129), MatCrea, MatLocSpace/SpaceTime(131)`
  etc. (`SensoryImprovement=122`).
- Read with `me.TryGetStat(Stat.X, out int v)` / `GetStat`. Skill cap:
  `me.GetSkillMax(Stat.X)`.

**Reading current skills & IP:** yes — `TryGetStat(Stat.IP)` for IP,
`TryGetStat(Stat.<skill>)` for any skill/ability, `GetSkillMax` for the cap.

**Detecting level-ups:** yes. `Stat.Level=54` changes on level. `NewLevelMessage`
(server→client) also fires with fork fields `Level, AvailableIp, CurrentXp, LastLevelXp,
NextLevelXp, IpResetPointsGained, XpKillRange, LastAwardedXp` — `AvailableIp` is the
fresh IP balance. The bot can poll `Stat.Level` each tick (like it polls HP/nano today)
or hook the message if the SDK surfaces an event.

**Uploaded/castable nanos:** `LocalPlayer.SpellList` (`int[]` of nano ids).
`ItemData.Find(id, out NanoItem ni)` resolves a nano; `ItemData.AllNanoIds()` enumerates
the offline DB; `ni.MeetsUseReqs(target, ignoreTargetReqs)` checks profession/level/skill
requirements against live stats. `Main.cs` already uses all of these in `ReportLearnable`
/ `ReportNanos` / `ClassLine`.

**Sending messages:** `Network.Send(MessageBody)` (and `Send(byte[])`,
`Send(ChatMessageBody)`). `Main.cs` already sends N3 messages this way (as
`Client.Send(new CharDCMoveMessage{…})`). So `SkillMessage` and `GenericCmdMessage` can
be sent today with no new plumbing — only thin helpers.

**Other relevant surface:** `me.Cast(id)` / `me.Cast(target, id)` (nano cast, used by the
heal system), `me.SetStat(Stat, int)` (sets a stat **locally** — this is NOT a train; it
does not go through the server's IP validation, so do not use it to raise skills),
`Item.Use(...)`, `Research.Train(int)` (research points — a separate progression system,
not IP skills), `Perk.IsTrained(...)` (read perk state). No dedicated `TrainSkill` /
`UploadNano` method exists — hence the helpers below.

---

## DESIGN — phased leveling feature

Mirror the heal system's split: **the state machine decides WHEN, config/goals decide
WHAT/priority.** Leveling work only runs when safe (out of combat, standing, not casting,
owner not fleeing) — the same gate `TryEmergencyHeal`/rest use. A new `TryLevelStep(me)`
is called once per decision tick from the out-of-combat branch, does at most one action
per tick (train one skill batch, or learn one nano), and is cheap after its first scan.

### Phase A — auto-raise combat skills used in combat
Goal: the skills she actually fights with keep pace as she levels, with zero config.
- Maintain a small set of "recently used combat skills". Derive usage from what she
  does: her equipped weapon's skill(s), the special attacks she fires
  (`CharSecSpecAttackMessage` carries a `SpecialAttackSkill`, e.g. 147=FastAttack seen in
  captures), and nano casts (nano school skills). Seed from config `CombatSkills` if set.
- When out of combat with spare IP, raise those skills toward the level cap
  (`GetSkillMax`), cheapest-first, spending only up to a per-tick / per-level budget
  (see `IpBudget` config) so IP isn't blown on one skill.
- Each raise: compute new value = current+N (N sized to the budget), verify
  `IpCost.ToRaise(stat, current, newValue) <= availableIp`, then `TrainSkill(stat,
  newValue)`.

### Phase B — goal-directed training
Goal: hit specific targets even when expensive (e.g. weapon/nano requirements to equip
gear or cast a key nano).
- Config `TrainingGoals`: list of `{ Skill, Target }` (e.g. `Strength → 125`).
- Each step: pick the goal with the lowest current/target ratio (or config order), raise
  it as far as this tick's budget affords, respecting `GetSkillMax`. Goals may be marked
  "priority" to bypass the combat-skill budget and drain IP toward them.
- Stop a goal when current ≥ target or the skill is capped.

### Phase C — auto-learn nanos from backpack/loot
Goal: upload nanos she now qualifies for, from crystals she's carrying.
- Reuse the existing `ReportLearnable` logic. Each step: scan `Inventory.Items` for nano
  crystals whose program id is **not** already in `SpellList` and whose
  `MeetsUseReqs(null, true)` passes with current (post-training) stats; upload one via
  `LearnNano(crystalItem)`.
- Run Phase C **after** A/B in a tick so a just-trained skill can unlock a nano the same
  session. Skip ids already in `SpellList`. Optional `AutoLearnNanos` on/off and an
  allow/deny name filter (some professions don't want every drop uploaded — NCU).

All three are class-agnostic: nothing hardcodes a profession; priorities come from
config + live stats (`Stat.Profession`, `Stat.Breed`) feed the cost model.

---

## New SDK / bot additions needed

Small helpers only — the transport (`Network.Send`) and item use already exist. Prefer
adding these to a new `AOBuddy` helper class (no SDK edits) unless a method clearly
belongs in the SDK; the notes below say which.

1. **`TrainSkill(Stat skill, int newAbsoluteValue)`** — build and send a `SkillMessage`
   with `Skills = [ (skill, (uint)newAbsoluteValue) ]`, `Identity = local player`,
   `Unknown = 0`, via `Network.Send`. (Bot-side helper; nothing new in the SDK required.)
   Optional batch overload taking several `(Stat,int)` pairs.

2. **`LearnNano(Item crystal)`** — call `crystal.Use()`. Fallback: send a
   `GenericCmdMessage { Action = Use, User = self, Target = crystal.Identity }`. (Bot-side
   helper.)

3. **`IpCost` static helper** — port of OmniCell `SkillUpdate.CalculateIP`'s tables
   (`skillCosts`, `attributeCost`, `baseAttributes`, `professionMatrix`) exposing:
   - `int PerPoint(Stat skill, int profession, int breed, int currentValue)`
   - `int ToRaise(Stat skill, int profession, int breed, int from, int to)`
   - `int AffordablePoints(...)` given `Stat.IP`.
   (Bot-side; pure data + arithmetic.)

4. **Level-up detection** — if the SDK exposes no `NewLevelMessage` event, poll
   `Stat.Level` in the tick and fire an internal `OnLevelUp` (recompute budgets, re-scan
   learnable). No SDK change needed if polling.

5. (Nice-to-have, SDK) a `LocalPlayer.SkillList` / `GetSkill(Stat)` convenience already
   covered by `TryGetStat`; not required.

## New config fields (add to `BuddyConfig`)

```csharp
// --- Leveling ---
public bool AutoTrainSkills   = false;         // master switch for Phases A/B
public bool AutoLearnNanos    = false;         // Phase C
// Skills to keep raised during play (empty = infer from weapon + specials + casts).
public List<string> CombatSkills = new List<string>();   // Stat enum names, e.g. "Pistol","RangedInit"
// Goal-directed targets. Skill = Stat enum name, Target = absolute value.
public List<TrainingGoal> TrainingGoals = new List<TrainingGoal>(); // { string Skill; int Target; bool Priority; }
// IP budgeting so one tick/level doesn't dump all IP into a single skill.
public int   IpReservePercent   = 0;    // keep this % of IP unspent (0 = spend freely)
public int   MaxPointsPerTick    = 5;   // cap skill points raised per decision tick
public bool  TrainOnlyOutOfCombat = true;
// Nano auto-learn filters (optional).
public List<string> LearnNanoAllow = new List<string>();  // name substrings to allow (empty = all qualifying)
public List<string> LearnNanoDeny  = new List<string>();  // name substrings to never upload
```
`CombatSkills`/`TrainingGoals` use `Stat` enum **names** (parsed with `Enum.TryParse`)
to stay class-agnostic and human-editable, matching how nano-id lists are configured today.

---

## Open questions / risks

1. **IP cost matrix is froob-only.** The ported `skillCosts` covers IDs 100–168 and 14
   professions; SL/AI-only skills, the research system, and higher expansion IP bands are
   not in it. Fine for a froob or low-level alt (the stated target); verify before
   trusting cost math on an SL/paid toon. `professionMatrix` has a `-1` entry (Keeper
   column) — confirm the Keeper/Shade mapping if the alt is one of those.
2. **Absolute-value semantics are proven against OmniCell's handler + char-creation
   captures, but not against a live single-skill retail raise** (the "manually raising
   first aid" capture wasn't decoded to the individual `SkillMessage` here). Low risk —
   handler assignment + full-snapshot captures agree — but confirm with one live
   single-skill train before relying on it in Phase B.
3. **Retail rejects unaffordable trains** ("lack ip nm"); OmniCell silently accepts them.
   The bot must gate every raise through `IpCost` + live `Stat.IP` and never over-send —
   a rejected raise on retail could desync the local stat view.
4. **Nano upload path assumes `Item.Use()` on a crystal routes to upload.** Confirmed
   `Item.Use` exists and is used for stims, and `GenericCmdMessage Use` is the observed
   wire form, but I did not capture a crystal upload end-to-end (no client upload packet
   was in the marks — the "learn nano" mark was a *failed* attempt: "i do not have the
   skills"). Validate one successful upload; keep the raw-`GenericCmdMessage` fallback.
5. **Mapping an inventory crystal → its uploaded nano id** needs care: the crystal item
   id is not the nano program id in `SpellList`. Resolve via `ItemData`/`NanoItem` so the
   "already known?" check compares the right id.
6. **Fork field-name drift.** `GenericCmdMessage` fields are `Temp1/Count/Temp4` in the
   fork vs `Verification/Serial/Flag` in OmniCell source, and `Target` is a single
   `Identity` not an array. Using `Item.Use()` sidesteps this; hand-building the message
   must use the fork names.
7. **`me.SetStat` is a local-only setter** — must not be mistaken for a train; only
   `SkillMessage` via `Network.Send` performs a server-validated IP spend.
