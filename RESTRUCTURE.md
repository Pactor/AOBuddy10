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

### R1.1 `Flat()` horizontal distance — [x] done 2026-09-24
`Movement.Flat(a, b)` + a `(a, x, z)` coordinate overload. The five float copies and ChewyBuffs'
coordinate variant are gone. Zoning's private **double** Flat is KEPT on purpose: different return
type and precision domain (route costs in the planner); folding it into the float helper would
change comparison precision for no gain.

### R1.2 `MoveSpeed(me)` wrapper — [x] done 2026-09-24
Two wrappers existed (MissionController, FollowController) — OverlandController already called
`_ctx.RunVelocity` directly (the survey's third "copy" was a direct call). Both wrappers deleted;
call sites call `_ctx.RunVelocity(me)`.

### R1.3 `Hold(me)` — [x] done 2026-09-24
`Movement.Hold(me, sendIntervalMs)` added beside Stop; all ~27 call sites in Follow/Mission/Overland
rewritten; the three private copies deleted.

### R1.4 Step-toward-waypoint primitive — [x] done 2026-09-24, scope adjusted
The five bodies differ in MORE than speed source: Y handling (grid sampling vs FloorY vs none),
heading policy (turn beats vs SafeLook), run flag, walk-state strings. A single StepToward would
need flag blindness — worse than the duplication. Extracted the genuinely shared SAFETY CLAMP as
`Movement.CappedStep(speed, dt, maxStep, remain)`; every walker's step now goes through it.
FollowController.StackTick's plain speed cap and MissionController's door-push cap stay inline
(no remaining-distance term — different animal).

### R1.5 Stuck detector — [x] done 2026-09-24, scope adjusted
Reading the four "copies" showed TWO are look-alikes with different algorithms, not parameters:
FollowController's replay watch compares per-FRAME deltas (`_lastCrumbDist` tracks last, not best)
and ResupplyController's uses absolute phase time. Forcing them into one helper would change
behaviour. `Movement.StuckWatch` (best-distance form) migrates the two verbatim-identical sites
(MissionController 0.3m/3s, OverlandController 0.3m/StuckSeconds); the other two stay, documented
in StuckWatch's doc comment. They can join after R2.1 unifies clocks.

### R1.6 Use-object packet helper — [x] done 2026-09-24
`GameCommands.UseObject` (world object, Temp4=1) and `GameCommands.OpenContainer` (bag/container
open, Temp4=0) in a new GameCommands.cs. All NINE sites migrated — including MissionRoll's terminal
Use, which the survey missed. Exactly one definition remains.

### R1.7 `IsMob` / `IsHuntable` share their flag masks — [x] done 2026-09-24
`MobFilter` (new file): the three flag constants, SideMonster, and `IsFightableKind` (kind test
only). IsMob and IsHuntable keep their own extra conditions on top; HuntController's Why() strings
read the shared constants.

### R1.8 Inventory pooling over main + open bags — [x] done 2026-09-24
`SupportController.AllInvItems()` is now public and THE pooling query; ResupplyController.Have uses
it. MissionRun.Bags() is NOT a duplicate — it detects the bag items themselves (slot/identity type
filter, no container pooling) and stays.

### R1.9 Nav-grid async load — [x] done 2026-09-24
`NavGridCache` (new file): one off-thread loader, one per consumer (OverlandController travel,
MissionRun hikes). Behaviour note: MissionRun's hike now LOGS nav-load failures (tag MISSIONRUN)
where it used to fail silently — a diagnostic addition, Overland's line is byte-identical.

### R1.10 `ZoneRouteOptions` construction — [x] done 2026-09-24
`Zoning.RouteOptions(me)` factory carries the shared Stat reader; the three sites keep their own
Filter (deliberately different exit policies) and MissionRun's hike still sets UseScotty=false.

**Phase 1 DONE WHEN.** `dotnet build` clean; grep for each duplicated signature finds one
definition; a live follow + one mission run produce logs indistinguishable in shape from a
pre-change run (same `hb` fields, same `STATE` transitions, no new `SETPOS APPLIED` storms).

---

## Phase 2 — one clock, a status read-model, one persistence helper

### R2.1 `IClock` on BotContext — [x] done 2026-09-25
`Clock.cs`: `IClock` + `Clock` (Stopwatch wrapper, `Seconds`/`Milliseconds`), created in Main.Init,
`ctx.Clock`. Migrated: Main (owner keyframes, PredictOwnerPos, navdata load-duration log),
FollowController, VitalsTracker, MissionController/OverlandController `Now` (instance now — was a
static TickCount64 property), CombatController set-asides (double clock-seconds at the field),
BotApi's reply-collection windows (takes the clock; the 0.6/1.2/2.5 s windows are unchanged).
**SCOPE ADJUSTED — SupportController is NOT epoch-based.** The plan's "read the clock relative to a
session epoch set at login" assumed AdvanceClocks runs every tick; it does not — Decide returns
early while resupplying, casting, solo or idle, so `_sessionSeconds` is ACT-TICK time, paused in
those modes, and every stamp on it (`_auraLastCast`, `_ownerLastMovedAt`, `_lockUntil`, ...) is
compared as a *difference* on the same paused clock. Wall-since-login would un-pause those modes
and change decisions (e.g. auras would recast on returning from idle). So AdvanceClocks now
advances by the REAL elapsed between calls (the drift fix the item wanted) but still only when
called — same cadence, same pauses, no nominal-quanta undercount on frame stalls.
**KEPT off the clock on purpose** (grep will hit these three, each with a reason): MissionRun's
`_danger` stays `DateTime.UtcNow` — it round-trips danger.json across restarts and a process-local
epoch would expire every mark on relaunch (same domain argument as R1.1's double Flat);
FloorGrid/OverlandGrid keep scope-local `Stopwatch`es — off-thread static builders timing their
own load for one log line, where the shared clock's identity is irrelevant. `DateTime.Now` filename
stamps are wall-calendar names, not a time base. Build clean; live smoke run (heartbeat offsets,
stim interval, rest max, zone give-up) rides the owner's next session.

### R2.2 `BotStatus` read-model on BotContext — [x] done 2026-09-25
`BotStatus.cs`: plain-fields read-model on `ctx.Status` (Dead, Resting, HasPendingCasts,
SecondsSinceCast, InCombat, NeedsRecovery, SelfHpPct, Casting, InMission, OwnerVisible,
OwnerDistance). `Main.RefreshStatus` fills it once per tick JUST BEFORE Walk — after Chewy has
queued this frame's casts, so HasPendingCasts/SecondsSinceCast are current for systems that yield
to casting. MissionRun's seven ctor lambdas are gone: raw conditions read `ctx.Status.X`, its
`Buffing` composition (the <15 s window is the run's policy) and the fight-or-run POLICY are named
members of MissionRun.cs now (`FightOrRun`, comments moved verbatim — reads as policy, lives in its
own file, no delegate needed: Status.InCombat IS the old lambda's `InCombat ||
HostilesEngaged(lp, FindOwner())` by construction). Ctor is ctx/roll/mission/overland/follow/
pluginDir/tell/combat. HuntController's `inMission` reads `ctx.Status.InMission`.
**Timing notes verified:** Status.Dead holds false through the reclaim wait (OnUpdate returns at
the death handler before RefreshStatus) — identical to what the old lazy lambda could ever return,
since MissionRun only ticks on frames Main isn't dead; command threads read the last snapshot,
which is the freshness the lambdas gave a few statements later anyway. ChewyBuffController and the
Decide ladder keep their direct reads (the opportunistic migration was optional; nothing needs it
yet — they'll flip when R6.2's session seam or R3's extractions touch them). R5.2's residual is now
only the `Resupply` ctor param. Build clean; blitz + fight-style smoke rides the owner's next
session (same fight turns in the log is the regression test).

### R2.3 `JsonStore` — one persistence helper — [x] done 2026-09-25
`JsonStore.cs`: `Load<T>(path, log)` (null when absent OR corrupt, ONE logged line per path+direction
so a hand-corrupted file is visible and the caller starts clean) and `Save(path, text, log)`
(serialize at the call site so each site keeps its own formatting; write `.tmp` then
`File.Replace`/rename so a crash mid-write can never truncate state; returns bool, never throws).
Migrated EVERY state write: Main's paths (SavePath returns bool; `savepath` reply now says when it
failed), NavController's snapshot autosave (dirty stays set when the store reports failure, so it
retries), ResupplyController (the corrupt-resupply.json test case), MissionRun's eleven (keepitems,
personalbags, rewardids, rewardnames, bankrules, fairtrade, danger, tune, config.json key-save,
missionterminal, missionrun state), NanoCatalog's export (gained the log param), and
SupportController's noland file — not JSON, but the same job: an atomic state write with visible
failure. Matching loads went through `Load<JObject/JArray/T>` with each site's interpretation
try/catch KEPT (the store surfaces IO/parse failures; junk-token handling is the site's, as before).
**Kept raw on purpose:** the .bin wire captures and aobuddy.log (append; not state files), the
read-only GameData inputs (Zoning.json, FactionAreas, AOBuddyNav, PerkData, ChewyBuffs' read —
inputs with their own fallbacks), Main.LoadConfig (already logged + needs the R0.3
ObjectCreationHandling.Replace settings), and Main.LoadPath (an absent/corrupt path file throws to
the `path` command, which replies "Load failed: ..." — its error surface). Per-site failure
messages ("RESUPPLY: couldn't save...") are now the one JSONSTORE line per path. **Verified with a
scratch harness** (R0.3's pattern): corrupt→null + exactly one logged line, second failure silent,
absent file null+silent, atomic replace over a corrupt target, read-only target → false + one line
+ no throw, JArray round-trip — 10/10 pass. DONE-WHEN grep: `File.WriteAllText` hits JsonStore.cs
only.

---

## Phase 3 — Main.cs decomposition (back to wiring-only)

Target: Main.cs ≈400 lines. Each extraction is one commit; after each, Main still compiles and
the smoke run is clean. Extraction order matters (R3.1 before R3.2; R3.4 anytime).

### R3.1 `OwnerTracker` — [x] done 2026-09-25
`OwnerTracker.cs` (new, ctor takes ctx): `Find()`/`IsOwnerSender` (identity, incl. the proven tell id
`_tellId` and the dynel-id cache `ChatId`, mirrored into `ctx.OwnerCharId` for ResupplyController's
trade check), `UpdateVisible(owner, dt)` (the per-frame bookkeeping: LostSeconds/LastPos/id caches,
returns this frame's visibility while `Visible` still holds last frame's — Main reads the pair as the
reacquire/lost edge), `OnKeyframe(cm)` + `PredictedPos` (interpolation, verbatim), `ResetOnZone()`
(ClearNav's LostSeconds=0, nothing else — keyframes/LastPos kept exactly as the inline reset did).
Main's CharDCMove handler keeps the identity test (`_owner.ChatId`), the DIAG counters and the MIRROR
forward; the tell handler and every command's Find call route through the tracker. The never-assigned
`_ownerLostDist`/`_ownerLostMoving` moved as `LostDist`/`LostMoving` properties (still dead — R3.2
decides restore-vs-delete with the rest of the zone-episode state). FollowController's header doc
pointer updated. Build clean; DONE-WHEN grep (KeyPos/Interp in Main.cs) empty; the corners-and-ramps
follow smoke rides the owner's next session.
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

### R3.2 `ZoneEpisode` (owner-loss / crossing coordinator) — [x] done 2026-09-25, scope adjusted — ladder DELETED
**The Phase-0 decision: delete, not restore.** Beyond the never-assigned gate fields, the ladder was
doubly broken on HEAD: `_zoneEpisodeElapsed` was never INCREMENTED (the 75 s cap could never fire),
and the sideways retry `offset` (0/+3m/-3m) was computed but never passed to `StartZoneSweep`. It
was an abandoned edit, not a disabled feature — restoring it would be authoring new auto-sweep
behaviour, which is a deliberate feature commit, not a restructure step. Deleted: the ladder, the
gate, `_zoneAttempts/_zoneEpisodeElapsed/_zoneGaveUp/_zoneSweepTried`, the `ZoneLossMeters/
ZoneMaxAttempts/ZoneEpisodeSeconds` consts, their resets in ClearNav and the `zone` command, and
OwnerTracker's dead `LostDist`/`LostMoving`. Consequence: `zone`'s attempt-budget reset was a no-op
all along (WorkTheZoneLine never read the budget), so `zone` now shares the `forward`/`run` case
verbatim — identical replies. A marker comment at the old ladder site in OnUpdate records the
deletion and where line-crossing lives now (manual `zone`/`forward`, travel/overland, nav replay).
**What survived to `ZoneEpisode.cs`** (ctor takes ctx only — follow/nav were only for the deleted
ladder): the arrived-alone watch, verbatim (`Tick(me, ownerVisible, soloTravel, dt)` with
`_overland.Active || _run.Active` as soloTravel; `ResetOnZone()` = ClearNav's 0.001 clock-start).
Both tell strings are byte-identical. Build clean (AOBuddy warnings 37 → 35: the dead code carried
them); `zone`/`forward` reply regression + the arrived-alone tell ride the owner's next session.
**What moves.** `_zoneAttempts, _zoneEpisodeElapsed, _zoneGaveUp, _zoneSweepTried, _arrivedAlone`
(+ `ArrivedAloneSeconds`, `ZoneLossMeters`, `ZoneMaxAttempts`, `ZoneEpisodeSeconds`) and the two
OnUpdate blocks that use them: the auto zone-sweep/give-up ladder (≈755-801) and the
arrived-alone watch (≈672-688). Depends on R3.1 for owner-visibility inputs.
**Interface.** `ZoneEpisode(ctx, follow, nav).Tick(me, ownerVisible, dt)` returning nothing but
issuing the same sweeps/tells; `ResetOnReacquire()`, `ResetOnZone()` (called from ClearNav),
`ResetAttempts()` (the `zone` command's fresh budget).
**DONE WHEN.** Losing the owner at a zone line still sweeps at most 3× / 75 s, gives up with the
same tell, and `zone` re-arms — the exact strings are the regression test.

### R3.3 `PerkBonuses` service — [x] done 2026-09-25
`PerkBonuses.cs` (new): ctor `(pluginDir, config, log)`, `Init()` (was InitPermanentBonuses, same
spot in Main.Init — after LoadConfig/_logFile, before ItemValues.Load), `Tick(me)` (the
RefreshPerksFromWire + ApplyPermanentBonuses pair, same OnUpdate spot), `Report(reply)` (the
`perks`/`perk` command). All six methods and six fields moved verbatim — the wire id-signature
recompute guard, the `ReferenceEquals(_bonusTarget)` once-per-LocalPlayer-instance re-apply, the
CWD-fallback path probes (read-only fallback with a logged failure; kept on purpose), and every
diagnostic string byte-identical. `Truncate` (used by Report + four Main command replies) moved to
`HelpPages.Truncate` — one definition, no Main back-reference. `_pets` was taken, so the field is
`_perkBonuses`. Build clean (35 warnings, unchanged); Main.cs 1804 → 1660. The `perks`-command and
login auto-detect log-line regression rides the owner's next session.
**What moves.** `InitPermanentBonuses`, `RefreshPerksFromWire`, `RecomputePerkBonuses`,
`MergeResearch`, `ApplyPermanentBonuses`, `ReportPerks`, and the fields `_perkData,
_permanentBonuses, _bonusTarget, _perkDiag, _perkOverride, _lastPerkSig` (Main.cs:1834-1969).
**Interface.** `PerkBonuses(pluginDir, config, log).Init()`, `.Tick(LocalPlayer)` (refresh +
apply), `.Report(reply)`. Main calls Init once and Tick per frame; the `perks` command calls
Report.
**DONE WHEN.** The `perks` command output is byte-identical; login auto-detect log lines unchanged.

### R3.4 Command routing out of the switch — [x] done 2026-09-25
`HandleCommand` is now parse + dictionary lookup; the switch body is gone. `BuildCommands()` (called
at the end of Init) fills `Dictionary<string, Action<Action<string>, string[]>>`; a local `Arg()`
reproduces the old switch's lowercased `parts[1]`. What moved where, all bodies and replies verbatim:
**PetController.Command** (+ ctor gains OwnerTracker) — the 20 pet commands plus their three helpers
(FindCharByName/ResolveHealSubject/HealWhoHint), which were pet-heal-specific all along;
**KnowledgeReports** (new, ctor ctx/support/owner) — class/whoami, nanos, active, learnable (+ the
learnable cache, was a Main field), stat, supplies/SupplyLine/FormatTime, autobuff/keepup's
ReportBuffPlans; **PathStore** (new, ctor pathsDir/log) — Save/Load/List for recorded paths (Load
still throws to the command's "Load failed" reply); **ResupplyController.Command/Survey** — the
resupply sub-switch and vendordebug. Staying in Main as closures over Main-owned state (per the
item's Careful note): modes, follow/stay/come, forward/run/zone, stand/sit, specials, missiondbg,
nanodump, catalog, buff/heal, record/savepath/path/paths, nav, status/pos/navdata, mission,
travelto, buffs (the bare-vs-Chewy bridge), shop/whompa stubs, help. `stat`/`Truncate` had already
found homes (KnowledgeReports/HelpPages in R3.3). HelpPages stays the usage catalog — no per-entry
usage strings in the table, one source of truth. **Verified:** build 0 errors, warning SET identical
before/after (stash-diffed, 66=66); command-coverage diff against HEAD's case list — all 75
top-level commands present (55 grep-visible keys + the 20-word pet loop), the only old words absent
are the 15 non-top-level ones (10 heal-subject + 5 sub-switch, now inside their owners). Main.cs
1660 → 1300. Reply spot-checks (`pets`, `pethealtarget`, `mission run status`, `navdata`,
`resupply machines`, `buffs plan`) ride the owner's next session.
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