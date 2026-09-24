# RESTRUCTURE — architecture work list (2026-09-24)

A point-for-point work list from the architecture review. Each item is self-contained, names its
files and symbols, and ends with a DONE-WHEN test, so items can be taken one per session (or
several when marked batchable) in or out of order where dependencies allow.

Progress: mark `[x]` and append the commit hash next to the item when it lands.

## Ground rules (apply to every item)

1. **Behavior-preserving.** No tuned constant changes value, no arbiter reorders, no wire packets
   added or removed unless an item says so. This bot's behaviour was settled against captures and
   live logs; restructuring must not re-litigate it.
2. **One item per commit.** If a change spreads past its item's file list, split the commit.
3. **Verify each commit** with `dotnet build AOBuddy10.slnx` and, where the item says so, a live
   smoke run comparing `aobuddy.log` shape before/after (the `hb [...]` heartbeat line and the
   `STATE ... -> ...` transitions are the regression baseline).
4. **Symbol names are the anchor.** Line numbers below are as of 2026-09-24 (commit 7320276) and
   will drift; find things by symbol.
5. Never touch follow / combat / travel / zone-crossing behaviour as a side effect of moving code.

## Non-goals (deliberately out of scope)

- No DI container. `Main.Init` stays the composition root; wiring stays explicit constructor calls.
- No instance-ification of the static SDK (`Client`, `DynelManager`, ...). One-bot-per-process is
  the host model; the seam we build instead is the `IGameSession` facade (Phase 6).
- No rewrite of `Walk()`/`Decide()` semantics, no config value tuning, no new bot features here.

## Phase overview and dependencies

| Phase | Theme | Depends on |
| --- | --- | --- |
| 0 | Quick safety fixes | — |
| 1 | Movement vocabulary harvest (pure dedup) | — |
| 2 | One clock + status read-model + JsonStore | — |
| 3 | Main.cs decomposition | 2 (BotStatus, clock) |
| 4 | Config vs runtime state split | — (R4.1 benefits from 3) |
| 5 | MissionRun: one route executor, split the file | 2; R5.2 needs R2.2 |
| 6 | Test seam and capture-replay harness | 1, 2 for the tested foundation |
| 7 | SDK hygiene | independent; R7.2 pairs with R0.2 |

Suggested order: 0 → 1 → 2 → 3 → 4 → 5, with 6 started as soon as Phase 1 lands (R6.1 has no
dependencies) so Phase 5's risky work happens under the harness.

---

## Phase 0 — quick safety fixes (one afternoon, all batchable)

### R0.1 BotApi commands run on the tick thread — [x] done 2026-09-24
Commands enqueue into a `ConcurrentQueue` drained at the top of `OnUpdate` (before the
`LocalPlayer` null return, so they run even before login); the `API CMD` log line moved there with
them and carries the executing thread id. BotApi's reply collector is unchanged.
**What.** `BotApi` (AOBuddy/BotApi.cs) executes `HandleCommand` inline on its listener `Thread`
while the update thread ticks the same controllers — unsynchronized mutation of controller state.
**Move.** Enqueue incoming command text into a `ConcurrentQueue<(string, Action<string> reply)>`;
drain it at the top of `Main.OnUpdate` before anything else. Replies still go back via the saved
reply delegate.
**Files.** BotApi.cs, Main.cs (`OnUpdate` head).
**DONE WHEN.** `POST /command` still answers within ~2 s; a log line shows API commands running on
the update thread (e.g. reuse the existing `API CMD` log, add thread id once for proof); no other
behaviour change.

### R0.2 Kill the CWD-relative paths (bot side) — [x] done 2026-09-24
`SupportController` now takes `pluginDir`; `buff_owner_noland.txt` resolves under it. A legacy
CWD-relative file is moved over on first load, and the resolved path is logged once (found or
absent).
**What.** `SupportController.OwnerNoLandFile = "buff_owner_noland.txt"` resolves against the
process working directory; every other bot file is pluginDir-relative. It silently breaks (the
read is try/catch-swallowed) whenever the host is started with another CWD.
**Move.** Pass `pluginDir` (SupportController already receives it? — it does not; add the
parameter or route the path through BotContext) and resolve the file under the plugin dir. Log
once when the file is found/absent so the failure mode is visible next time.
**Files.** SupportController.cs (symbol `OwnerNoLandFile`, its read/write sites), BotContext.cs or
Main.Init wiring.
**DONE WHEN.** Starting the host from a different CWD still loads the no-land list (test: run
`Build\Test.exe` from `F:\`); a log line names the resolved path.
**Related.** R7.2 does the same for `ItemData.bin` in the SDK.

### R0.3 Config lists: replace, don't append — [x] done 2026-09-24
`LoadConfig` deserializes with `ObjectCreationHandling.Replace`. Verified with a scratch harness:
`[]` -> empty, absent key -> defaults, shorter list replaces (no union), dictionary still reads.
NOTE the semantic change: a config.json list no longer unions with built-in defaults — hand-edited
partial lists (e.g. a one-item `KeepItems`) must now be written in full.
**What.** Newtonsoft merges a config.json list into the C# defaults (documented gotcha at
Config.cs:46-47), so a user's shorter list silently unions with the built-in one.
**Move.** In `Main.LoadConfig` (Main.cs:1971), deserialize with
`JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace }` (or set the
attribute on the list properties in BuddyConfig). Add a comment removing the gotcha note.
**DONE WHEN.** A config.json with `"AttackPetBuffLines": []` yields an empty list at runtime;
existing configs with no such key still get the defaults.
**Careful.** Dictionaries (`ResearchBonuses`) keep merge semantics — verify they still read fine.

### R0.4 Dead documentation pointers — [x] done 2026-09-24
Pointers stripped (MISSION-MODE-PLAN.md x3, Config.cs x3, NavController.cs x1) — the deleted docs
were removed deliberately in 63d6382 ("carry the code and its data, not the notes"), so they are
not restored; the plan's rule sentence was already inline. Root-level plan docs were LEFT at the
root (active hand-off files, no references elsewhere). Grep is clean.
**What.** References to deleted files: `AOBuddy/CLAUDE.md` (MISSION-MODE-PLAN.md intro),
`NANO_BUFF_DESIGN.md` (Config.cs comment above AutoBuff), `NAV_DESIGN.md` (NavController.cs
comment). `docs/` holds only `img/`.
**Move.** For each: either restore the doc from git history (`git log --all -- <path>`) into
`docs/` if it still describes reality, or strip the pointer. Move surviving root-level plans
(MISSION-MODE-PLAN.md, NAV-CLIENTDATA.md) under `docs/` and leave a root README link; update
README.md's layout table.
**DONE WHEN.** `grep -rn "CLAUDE.md\|NANO_BUFF_DESIGN\|NAV_DESIGN" --include=*.cs --include=*.md .`
(excluding `Build/`, `.git/`) returns nothing unresolved; README's links work.

### R0.5 Deduplicate the `forward`/`run` and `zone` command bodies — [x] done 2026-09-24
Extracted `Main.WorkTheZoneLine(p, reply)`; `zone` keeps its attempt-reset prologue. Replies are
the same strings as before.
**What.** Main.cs `case "forward"/"run"` (≈1133-1177) and `case "zone"` (≈1178-1225) are the same
~40 lines; only the attempt-reset prologue differs.
**Move.** Extract `WorkTheZoneLine(LocalPlayer me, Action<string> reply)`; `zone` resets the
episode counters first, then both call it.
**Files.** Main.cs.
**DONE WHEN.** `forward` and `zone` produce byte-identical replies to before for: recorded-line
route case, near-line sweep case, fallback sweep case.

### R0.6 `ctx.TellOwner` — one owner-tell helper — [x] done 2026-09-24
`BotContext.TellOwner` added; the four Init lambdas, `ResupplyController.Tell`, and Main's four
direct owner-tells (death/ding/arrived-alone/zone-gave-up) route through it. Left alone: the local
`Tell` in `SupportController.CheckSupplies` sends by the owner's dynel ID (only works in view) and
also logs — a different channel, not a duplicate of the by-name tell. Grep: one site, BotContext.
**What.** The lambda `text => { try { Client.Chat.SendPrivateMessage(_config.Owner, text); } catch { } }`
is written 4× in Main.Init (≈167-177), plus `ResupplyController.Tell` (≈888) and a local `Tell`
in SupportController (≈1336).
**Move.** Add `BotContext.TellOwner(string)` (wrapping the try/catch); pass `ctx.TellOwner` where
the lambdas now go; make the two private copies call it.
**DONE WHEN.** `grep -n "SendPrivateMessage(_config.Owner" AOBuddy/*.cs` hits only BotContext.cs
(one site) — the remaining direct calls in Main (death/arrival/ding/level messages, ≈492, 590, 683,
775) can also route through it or stay, but the duplicate *helpers* are gone.

---

## Phase 1 — movement vocabulary harvest (pure dedup, no behavior change)

The same walker primitives are hand-rolled per controller. Consolidate them onto `Movement`
(AOBuddy/Movement.cs) or a new `Walker` static class beside it, **keeping each caller's current
constants** — where callers disagree (e.g. stuck timeouts), the shared helper takes them as
parameters. Do not "unify" values; that is a behaviour change (ground rule 1).

### R1.1 `Flat()` horizontal distance — [ ]
6 copies: MissionRun.cs≈1923, MissionController.cs≈965, HuntController.cs≈210,
OverlandController.cs≈595, Zoning.cs≈487, ChewyBuffs.cs≈711 (2-arg variant).
One helper (suggest `Movement.Flat(Vector3 a, Vector3 b)`); callers delegate.

### R1.2 `MoveSpeed(me)` wrapper — [ ]
3 copies that just call `_ctx.RunVelocity(me)`: MissionController.cs≈975,
OverlandController.cs≈506, FollowController.cs≈315. Delete the wrappers, call `_ctx.RunVelocity`
directly (or one shared extension).

### R1.3 `Hold(me)` — [ ]
`if (_move.Moving) _move.Stop(...)` copied in MissionController.cs≈546, OverlandController.cs≈542,
FollowController.cs≈576. Move onto `Movement.StopIfMoving(LocalPlayer, int sendIntervalMs)`;
each caller passes its current send-interval argument unchanged.

### R1.4 Step-toward-waypoint primitive — [ ]
The `dir = (wp - pos).Normalize(); step = min(speed*dt, MaxStep, dist); _move.Advance(...)` block:
MissionController.WalkTick≈929-936, OverlandController.WalkTick≈505-511, FollowController.Step≈567-573,
TravelController.Tick≈123-128, ResupplyController.ApproachTick≈369-372.
One `Movement.StepToward(...)` returning the remaining distance. The five bodies differ only in
speed source and cap arguments — parameterize, don't merge values.

### R1.5 Stuck detector — [ ]
`if (d < _bestDist - 0.3f) {...} else _stuckTime += dt; if (_stuckTime > N) skip` copied 4×:
MissionController≈914-927, OverlandController≈484-501, FollowController≈605-611,
ResupplyController≈360-366. Extract a small `StuckWatch` struct (Reset/Update returns stuck?) on
Movement; each site keeps its own instance and threshold.

### R1.6 Use-object packet helper — [ ]
Hand-rolled `GenericCmdMessage { Command = Use, Temp4 = 1 }` in 8 sites: OverlandController≈535,
TravelController≈110, ResupplyController≈739, MissionController≈479, MissionRun≈917/1225/1258/1265/1295.
One `GameCommands.Use(Identity target)` (new small static, or on Movement) — send exactly the same
bytes as today (verify one against a capture if unsure).

### R1.7 `IsMob` / `IsHuntable` share their flag masks — [ ]
MissionRun.IsMob≈1893 and HuntController.IsHuntable≈171 encode the same dynel-flag masks
(0x200000 / 0x800000 / 0x8000000, Side==3) with different extra conditions. Keep both predicates
(they answer different questions) but move the mask constants + shared mask test into one place
(e.g. `DynelFlags.IsMobLike(SimpleChar)` in a small shared file) with a comment citing the capture
that settled each mask.

### R1.8 Inventory pooling over main + open bags — [ ]
3 copies: SupportController.AllInvItems≈1383, ResupplyController.Have≈800, MissionRun.Bags≈1146.
One `InventoryPool.AllItems()` / `.Bags()` helper (suggest on BotContext or a small static beside
Config.cs).

### R1.9 Nav-grid async load — [ ]
Verbatim duplicate: MissionRun.HikeGrid≈798-817 vs OverlandController.EnsureNav≈569-593
(`Task.Run(() => AOBuddyNav.Load + OverlandGrid/FloorGrid.Build)`). Extract into one loader
(e.g. a static on AOBuddyNav or OverlandController) both call. This is also the seam Phase 5's
"overland as sole route executor" needs.

### R1.10 `ZoneRouteOptions` construction — [ ]
Built 3×: MissionRun≈307, ≈824, OverlandController≈192. One factory next to the type.

**Phase 1 DONE WHEN.** `dotnet build` clean; grep for each duplicated signature finds one
definition; a live follow + one mission run produce logs indistinguishable in shape from a
pre-change run (same `hb` fields, same `STATE` transitions, no new `SETPOS APPLIED` storms).

---

## Phase 2 — one clock, a status read-model, one persistence helper

### R2.1 `IClock` on BotContext — [ ]
**What.** Four incompatible time bases: `Stopwatch` (Main, FollowController, VitalsTracker),
`Environment.TickCount64` (MissionController.Now≈116, OverlandController.Now≈87), `DateTime.UtcNow`
(CombatController set-asides≈48), and accumulated `dt` quanta (SupportController.AdvanceClocks≈180,
advanced in TickMs steps so it drifts from wall time).
**Move.** One `Clock` class (a Stopwatch wrapper exposing `double Seconds` and `double Milliseconds`),
created in Main.Init, hung on BotContext (`ctx.Clock`). Migrate each site. For
`SupportController.AdvanceClocks`: keep the method name if other code calls it, but back it with
the clock; its `_sessionSeconds` semantic (tick quanta) must stay — make it read the clock
*relative to a session epoch set at login* rather than accumulating quanta, which is the same
value without the drift.
**Careful.** CombatController's DateTime-based set-aside expiry times and Mission/Overland's
TickCount epochs convert to clock seconds — do the conversion at the field, not at each comparison.
**DONE WHEN.** `grep -rn "Stopwatch\|TickCount\|DateTime.UtcNow" AOBuddy/*.cs` hits only the Clock
class; one live session shows heartbeat timestamps and timeout behaviour unchanged (e.g. stim
interval, rest max, zone-episode give-up still fire at the same wall-clock offsets).

### R2.2 `BotStatus` read-model on BotContext — [ ]
**What.** MissionRun's constructor takes 7 lambdas (`tell, dead, recovering, buffing, fighting,
needsRecovery, inCombat, selfHp`, MissionRun.cs:72-78) that close over Main's private fields — an
implicit back-reference to Main. HuntController fabricates `Func<bool> inMission` for itself.
**Move.** Add to BotContext a small read-model refreshed by Main once per tick, before `Walk`:
```
Dead, Resting (sitting), HasPendingCasts, SecondsSinceCast, InCombat (combat OR hostiles engaged,
  exactly today's `_combat.InCombat || _combat.HostilesEngaged(me, owner)`),
NeedsRecovery, SelfHpPct, Casting (me.IsCasting), OwnerVisible, OwnerDistance
```
MissionRun reads `ctx.Status.X` instead of the lambdas; its constructor loses all seven.
HuntController's `inMission` becomes `() => ctx.Status.InMission` (add that field; Main sets it
from `_mission.InMission`). ChewyBuffController and the `Decide` ladder can migrate opportunistically.
**Careful.** The `fighting` lambda (Main.cs:182-204) is the mission fight-or-run policy — that is
*decision logic*, not status. Keep it a delegate but move the delegate into MissionRun's own file
(or a named `MissionFightPolicy` method on Main) so the policy reads as policy; only the raw
condition lambdas become status reads.
**DONE WHEN.** MissionRun's ctor parameter list is ctx/roll/mission/overland/follow/combat/tell/
pluginDir only; a blitz run and a fight-style run behave as before (log the same fight turns).

### R2.3 `JsonStore` — one persistence helper — [ ]
**What.** Scattered file IO with silent catches: Main (paths/*.json, config, aobuddy.log,
missions/*.bin), MissionRun (six private JSONs: missionrun/missionterminal/keepitems/personalbags/
rewardids/rewardnames), ResupplyController (resupply.json), NavController (nav/<pf>.json with its
own snapshot-copy autosave).
**Move.** One `JsonStore` static: `Load<T>(path)`, `SaveAtomic(path, Action<T> write or object
graph)` — temp file + `File.Replace`/rename, log-once failure policy via ctx.Log, no silent
swallows. Migrate the sites; NavController's dirty-autosave keeps its cadence but writes through
the store.
**DONE WHEN.** `grep -rn "File.WriteAllText" AOBuddy/*.cs` hits only JsonStore (and the .bin debug
capture, which stays raw); corrupting resupply.json by hand produces one logged line and a clean
recreate instead of silence.

---

## Phase 3 — Main.cs decomposition (back to wiring-only)

Target: Main.cs ≈400 lines. Each extraction is one commit; after each, Main still compiles and
the smoke run is clean. Extraction order matters (R3.1 before R3.2; R3.4 anytime).

### R3.1 `OwnerTracker` — [ ]
**What moves.** Owner keyframe/velocity state and interpolation (`_ownerKeyPos, _ownerKeyTime,
_ownerVel, _ownerKeyHeading, _ownerMovingKey`, `PredictOwnerPos`, `IsMovingMove`, the keyframe
capture in the CharDCMove handler Main.cs:247-282); owner lookup + identity (`FindOwner`,
`IsOwner`, `IsOwnerSender`, `_ownerChatId`, `OwnerCharId` caching); visibility bookkeeping
(`_ownerVisibleLast, _lastOwnerPos, _ownerLostSeconds, _ownerLostDist, _ownerLostMoving`).
**Interface.** `OwnerTracker(ctx)` with `PlayerChar Find()`, `Vector3 PredictedPos(PlayerChar)`,
`void OnKeyframe(CharDCMoveMessage)`, `bool Visible`, `Vector3? LastPos`, `double LostSeconds`,
`int ChatId`. Main feeds it the message and reads it; the mirror call
(`_follow.MirrorLocked` block) stays in Main's handler.
**DONE WHEN.** Main.cs no longer contains the words KeyPos/Interp; follow behaviour on a run with
corners and ramps is unchanged (trail counts in `hb` line similar, no `SETPOS IGNORED` storms).

### R3.2 `ZoneEpisode` (owner-loss / crossing coordinator) — [ ]
> **FOUND DURING PHASE 0 (2026-09-24):** `Main._ownerLostDist` and `Main._ownerLostMoving` are
> declared, read by the auto zone-sweep gate (`crossingLikely = _ownerLostDist <= ZoneLossMeters &&
> _ownerLostMoving`), but **never assigned** (CS0649, pre-existing on HEAD before any restructure
> work). So `crossingLikely` is always `false` and the whole auto zone-sweep ladder in OnUpdate is
> unreachable — only the manual `zone`/`forward` commands and nav replay cross lines today. The
> assignments (meant for the "he just dropped out of view" branch) were evidently lost in an edit.
> Decide while extracting ZoneEpisode: restore the assignments (re-enables auto sweeps) or delete
> the dead ladder and the fields. Do NOT silently restore — it is a behaviour change.
**What moves.** `_zoneAttempts, _zoneEpisodeElapsed, _zoneGaveUp, _zoneSweepTried, _arrivedAlone`
(+ `ArrivedAloneSeconds`, `ZoneLossMeters`, `ZoneMaxAttempts`, `ZoneEpisodeSeconds`) and the two
OnUpdate blocks that use them: the auto zone-sweep/give-up ladder (≈755-801) and the
arrived-alone watch (≈672-688). Depends on R3.1 for owner-visibility inputs.
**Interface.** `ZoneEpisode(ctx, follow, nav).Tick(me, ownerVisible, dt)` returning nothing but
issuing the same sweeps/tells; `ResetOnReacquire()`, `ResetOnZone()` (called from ClearNav),
`ResetAttempts()` (the `zone` command's fresh budget).
**DONE WHEN.** Losing the owner at a zone line still sweeps at most 3× / 75 s, gives up with the
same tell, and `zone` re-arms — the exact strings are the regression test.

### R3.3 `PerkBonuses` service — [ ]
**What moves.** `InitPermanentBonuses`, `RefreshPerksFromWire`, `RecomputePerkBonuses`,
`MergeResearch`, `ApplyPermanentBonuses`, `ReportPerks`, and the fields `_perkData,
_permanentBonuses, _bonusTarget, _perkDiag, _perkOverride, _lastPerkSig` (Main.cs:1834-1969).
**Interface.** `PerkBonuses(pluginDir, config, log).Init()`, `.Tick(LocalPlayer)` (refresh +
apply), `.Report(reply)`. Main calls Init once and Tick per frame; the `perks` command calls
Report.
**DONE WHEN.** The `perks` command output is byte-identical; login auto-detect log lines unchanged.

### R3.4 Command routing out of the switch — [ ]
**What.** `HandleCommand` is a ~425-line switch (Main.cs:1097-1522) while three systems already
own their commands (`_hunt.Command`, `_roll.Command`, `_overland.Command`).
**Move.** Give each controller a `Command(string[] args, Action<string> reply)` (pet commands →
PetController; nano/knowledge reports → a new `KnowledgeReports` home or SupportController; path
record/save/replay/list → FollowController or a `PathStore`; resupply/vendordebug → already there,
finish it; stat/pos/status stay in Main). Main keeps a `Dictionary<string, (string usage,
Action<Action<string>, string[]> handler)>` built at Init — a dispatch table, not a switch body.
Help texts move with their commands (HelpPages.cs already exists as the catalog).
**Careful.** Some commands mutate Main-owned state (`mode`, `_zoneAttempts` reset in `zone`,
`follow`/`stay` flipping Follow). Those handlers can be small closures over Main in the table —
the win is locality, not purity.
**DONE WHEN.** Every command from `help` still parses and replies identically (spot-check:
`pets`, `pethealtarget`, `mission run status`, `navdata`, `resupply machines`, `buffs plan`).

### R3.5 Wire-capture/diagnostics observer — [ ]
**What moves.** The MissionDebug block inside Main's second MessageReceived handler
(PlayfieldAnarchyF logging + missions/*.bin saving, the 0x5C436609 raw probe, Main.cs:284-317),
plus `_lastZoneInPacket` custody can stay in Main (MissionController needs it) or move to the
observer with an accessor.
**Move.** `WireCapture(config, pluginDir).OnMessage(Message)` subscribed via the R3.6 helper.
**DONE WHEN.** `missiondbg on` produces the same MISSIONDBG lines and .bin files.

### R3.6 `SafeSubscribe` — handler isolation without the boilerplate — [ ]
**What.** Main.Init's five try/catch-wrapped forwards (≈224-236) and two big inline handlers.
**Move.** Extension `ClientEvents.SafeSubscribe(Action<Message> handler, string tag,
Action<string> log)` wrapping try/catch + tagged log; Init becomes a list of subscriptions. Keep
the existing tags (VITALS/RESUPPLY/MISSIONROLL/MISSIONRUN/MISSION) so log greps still work.
**DONE WHEN.** Init's message wiring is a flat list; throwing a test exception inside Vitals'
handler logs `VITALS feed error: ...` and the other handlers still run that frame.

---

## Phase 4 — config vs runtime state

### R4.1 `RuntimeState` split (do this one first) — [ ]
**What.** Commands and controllers mutate `BuddyConfig` live: `_config.Follow` (Main≈1121),
`_config.UsePets`/`AutoResummon`/`BuffPets` (pets commands), `MissionShop`/`MissionStyle`/
`MissionFightDifficulty` (MissionRun≈99/140/149), and MissionRun **temporarily rewrites
`ResupplyCashReserve` around a bag purchase** (≈1407-1410) — a live shared-state race.
SupportController sets `AutoBuff=false` on error (≈316).
**Move.** New `RuntimeState` on BotContext (`ctx.State`): Mode, Follow, UseSpecials, UsePets,
AutoResummon, BuffPets, MissionShop, MissionStyle, AutoBuff-off latch, NavRecord/NavUse toggles.
Main and controllers read/write State, never Config, for these. The `ResupplyCashReserve` rewrite
becomes a parameter to the resupply call instead of a config mutation.
**Careful.** Mechanical but wide: every read of these fields switches to `ctx.State.X`. Keep
config as the *default source* at Init (State is initialized from Config).
**DONE WHEN.** `grep -n "Config.Follow\|Config.UsePets\|Config.MissionShop\|Config.AutoBuff ="
AOBuddy/*.cs` returns only RuntimeState initialization; `follow`/`stay`/`pets`/`mission run shop`
behave exactly as before across a shop trip (the crash-window case the reserve rewrite covered).

### R4.2 Persist runtime toggles (optional) — [ ]
**What.** Toggles set by tell are lost on restart (config was never saved back — by design, but
the owner may want mode/follow/pets to stick).
**Move.** RuntimeState saves to `state.json` via JsonStore on change (throttled), loads at Init
after Config defaults. Owner can wipe the file to reset.
**DONE WHEN.** Toggle `stay`, restart, bot starts with follow off and says so in the login CONFIG
log line.

### R4.3 Nested config sections (optional, breaking) — [ ]
**What.** 120+ flat fields on BuddyConfig make "which controller reads what" invisible.
**Move.** Group into nested objects (Follow/Combat/Heal/Resupply/Mission/Nav/Buff...) with
`[JsonProperty]` keeping names. **Breaking** for existing config.json files unless a migration
reads the flat form too — decide explicitly: either ship a one-time converter command
(`config migrate`) or dual-read for one release. Skip this item if the migration cost isn't worth
it; R4.1 already removes the urgent problem.
**DONE WHEN.** Existing config still loads (values equal, logged CONFIG line identical) after the
chosen migration path.

---

## Phase 5 — MissionRun: one route executor, then split the file

### R5.1 OverlandController becomes the sole route executor — [ ]
**What.** MissionRun.HikeTick (≈842-970) re-implements overland travel: its own grid legs, direct
`Zoning.FindRoute` calls (≈315, 831), its own verbatim nav-grid loader (fixed by R1.9). Two route
executors means travel fixes don't reach mission hikes.
**Move.** Give OverlandController whatever HikeTick lacks (a "hike" mode that stands on doors
rather than using them — the wire difference is already documented in commits 43bd584/f3be9d1),
then MissionRun's Hike/ToDoor phases become `_overland.Command(...)` + waiting on `Active`/arrival,
exactly like its existing travel phases.
**Careful.** This is the riskiest item in the list — hike-vs-travel semantics were settled by
captures (doors stood on, walk-back when pulled). Land it only after R6.1/R6.2 exist so there's a
harness, and smoke-test a full mission run including a hike leg and the pulled-back recovery.
**DONE WHEN.** A mission run that crosses a city reaches the door the same way (log the same
HIKE/DOOR lines); `grep -n "FindRoute" AOBuddy/MissionRun.cs` returns nothing.

### R5.2 Constructor cleanup via BotStatus — [ ] (needs R2.2)
Drop the seven condition lambdas; keep at most `tell` and the fight-policy delegate (see R2.2).
`public ResupplyController Resupply` (MissionRun≈1051, set post-hoc in Main≈209) becomes a proper
constructor parameter.

### R5.3 Split the file — [ ]
1925 lines → `MissionRun.cs` (phase machine + Tick skeleton), `MissionRunShop.cs` (ShopTick ≈170
lines, the 8 shop steps, keep/personal/reward persistence), `MissionRunHike.cs` (until R5.1 lands;
delete after), `MissionRunFight.cs` (Attacker/fight policy, IsMob). Partial classes are fine here
(one system, one file-per-concern charter is about systems); alternatively make Shop a nested
class. Keep `Phase` enum in the main file.

### R5.4 Phase-handler extraction (optional) — [ ]
If Tick is still hard to read after R5.1-R5.3: one method per phase (`TickToTerminal`, ...) or a
`Dictionary<Phase, Func<...>>`. Only if it pays for itself; a 395-line switch-on-phase Tick split
into 17 named methods is already most of the readability win.

**Phase 5 DONE WHEN.** A full `mission run` session (roll → travel → hike → blitz → stash →
repeat, one death mid-run) matches a pre-change log in phase sequence and timing bands.

---

## Phase 6 — test seam and capture-replay harness

### R6.1 `AOBuddy.Tests` test project — [ ]
**What.** No test seam exists; verification is live play. After Phase 1, the harvested primitives
(StepToward, StuckWatch, Flat, JsonStore, Clock, BotStatus transitions, zone-episode give-up
logic, NavGrid/FloorGrid/OverlandGrid queries, MissionGrid A*) are plain code testable without a
game connection.
**Move.** New `AOBuddy.Tests` xunit project referenced into `AOBuddy10.slnx`. Mind
Directory.Build.props: everything builds into `Build\` — add a `tests/Directory.Build.props`
opt-out mirroring `tools/Directory.Build.props` (or put the test project under `tools/`, but a
first-class `tests/` reads better). Keep GameData-dependent tests on small fixture files, not the
committed DBs.
**DONE WHEN.** `dotnet test` runs green in CI-less local use; at least the Phase-1 helpers and
grid queries are covered.

### R6.2 `IGameSession` facade — [ ]
**What.** Controllers call the static SDK surface (`DynelManager.LocalPlayer`, `Client.Chat`,
`Targeting.SetTarget`, `Playfield.ModelId`) inline, which is what makes replay impossible.
**Move.** A narrow interface with only the members controllers actually use, plus
`LiveGameSession` forwarding to the statics (one file, mechanical). Adopt **controller by
controller** — FollowController first (it's the arbiter's default), then Combat/Support; MissionRun
last. Don't big-bang it; each adoption is a commit and a smoke run.
**Careful.** The facade must not become a god-interface. If a controller needs something novel, it
goes on the interface *with* its live implementation in the same commit.
**DONE WHEN.** FollowController compiles against `IGameSession` with a fake in tests driving a
recorded keyframe sequence through crumbs/arrive/stuck logic.

### R6.3 Capture-replay driver — [ ]
**What.** The assets exist — decoded captures, PcapDecode pointed at this repo's
AOSharp.Common.dll, saved zone-in packets, `navdata verify` self-tests — but nothing feeds them
back through the bot.
**Move.** A console harness (under `tools/` or the test project) that: reads a decoded message
stream, dispatches each message through the same handlers Main subscribes (via IGameSession
fakes + real VitalsTracker/zone logic), pumps the tick with a fake clock, and writes the resulting
`aobuddy.log`-equivalent. Diff two runs to see a refactor's behavioural footprint.
**Careful.** Scope it to *decision replay* (positions, phases, tells, sweeps) — full SDK state
reconstruction is not needed and not worth it.
**DONE WHEN.** Replaying the same capture before and after a Phase-5 commit produces identical
decision traces.

### R6.4 Regression scenarios — [ ]
Encode the historic bug classes as replay fixtures (each already documented with a date-stamped
log in comments): ramp rubberband (`SETPOS IGNORED` policy), snare negative RunSpeed, owner
sit/stand loop, give-up-at-75s zone episode, stim-lock fight pause, pet-straggler regen block.
Each becomes an assertion on the decision trace. Add fixtures as bugs get fixed — the list only
grows in value.

---

## Phase 7 — SDK hygiene (AOSharp.Clientless / host)

### R7.1 Plugin hosting out of the SDK — [ ]
**What.** `ClientDomain`/`Plugin` (reflection loading, assembly probing, ClientDomain.cs:84-131)
live inside AOSharp.Clientless — host concerns inside the client library, widening the fork's
diff surface against upstream AOSharp.
**Move.** Extract to the host (Test) or a new `AOSharp.Clientless.Hosting` project; the SDK keeps
`Client`, `ClientlessPluginEntry` (the contract) only. The bot references nothing that moves.
**DONE WHEN.** Solution builds; host behaves identically (plugin discovery log unchanged).

### R7.2 `ItemData` path resolution — [ ]
`GameData\ItemData.bin` is CWD-relative (ItemData.cs:17-18). Resolve against the assembly
location (or accept an explicit root from `Client.Init`). Same DONE-WHEN shape as R0.2.

### R7.3 No more bare `catch { }` on plugin lifecycle — [ ]
`Plugin.Initialize`/`Teardown` swallow everything (ClientDomain.cs:178, 189) — a failing plugin
just doesn't load, silently. Log the exception through the session logger before swallowing.

### R7.4 Packet-loop handler isolation (investigate first) — [ ]
A throwing handler inside `ProcessCachedPacket` is caught at packet level and drops the rest of
that packet's processing (NetworkSession.cs:167-286). The bot-side SafeSubscribe (R3.6) covers the
bot's own handlers; decide whether the SDK should isolate per-subscriber too (a copy of the
invocation list + per-handler try/catch in `Client`'s raisers) or whether R3.6 suffices. Note the
decision and the reason here either way — don't change the SDK's dispatch semantics silently.

### R7.5 Reconnect backoff (small) — [ ]
Fixed `ReconnectDelay = 30000`, `Task.Delay(...).ContinueWith(Connect)` (NetworkSession.cs:453,
TODO at Client.cs:23). Add exponential backoff with a cap. Low priority; note while in the area.

---

## Definition of done for the whole effort

- Main.cs is wiring + the two arbiters (~400 lines); no keyframe, zone-attempt, perk, or
  command-switch bodies remain.
- One movement vocabulary, one clock, one persistence helper, one owner-tell helper.
- Config is read-only after Init; runtime state is explicit and (optionally) persisted.
- OverlandController is the only thing that executes routes.
- `dotnet test` covers the harvested primitives and the historic bug classes replay-clean.
- A full live session (follow + assist + zone + mission run + shop + death) is log-shape-identical
  to a pre-effort session.