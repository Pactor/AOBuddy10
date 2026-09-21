# AOBuddy — Nano / Buff System Design (2026-09-19)

STATUS: IMPLEMENTED 2026-09-20 in `SupportController` (`KeepBuffs`, `RebuildBuffPlansIfNeeded`,
`NanoRefillForCast`, cost/afford helpers) + `Main.Decide` wiring + `Config` auto-buff toggles + the
`autobuff`/`keepup` chat command (reports what was classified: name, NCU, cost, target, up/DOWN). Follow /
Combat / Travel untouched. NOT yet done (phase 2, flagged below): cast-failure/resist backoff, casting-
while-moving (currently casts standing only, out of combat), same-line "best learned per line" pruning.

Grounded in verified SDK/wire data (no guessing).

## Goals (from the user)
1. Know which nanos are ACTIVE and how long is left on each → rebuff before they drop.
2. Classify nanos: self vs any-target (owner/team). (Hostile debuffs are a later phase.)
3. Know which we currently have enough nano (mana) to cast.
4. Build a cast QUEUE and cast each as mana allows.
5. Use stims to refill nano between casts, so a whole buff sequence flows without manual help.

## Verified data sources (checked in SDK — do not re-derive)
Active buffs (self AND others):
- `me.Buffs` / `SimpleChar.Buffs` → `IReadOnlyList<Buff>`. Each `Buff`: `Id`, `NanoItem`, `Cooldown.RemainingTime` (seconds left).
- Duration is server-sourced: full updates set it from `ActiveNanos[i].Time2/100` (`SimpleChar.cs:132`); a nano landing sets it via `SetExpireTime(param2/100)` (`Client.cs:692`).
- Event `BuffStatus.BuffChanged` (Removed / Refreshed) fires on add/refresh/remove — react immediately, don't only poll.
- CAVEAT: our OWN buffs update in real time; OTHERS' (owner/team) buffs refresh only when the server sends their SimpleCharFullUpdate, so their RemainingTime can be stale between updates. Treat owner/team buff timers as best-effort; self is authoritative.

Per-nano metadata via `ItemData.Find(id, out NanoItem)`:
- `Cost` (BASE nano cost), `NanoLine`, `NanoSchool`, `NCU`, `StackingOrder`, `Range`.
- `AttackDelay` (cast time s), `RechargeDelay` (recharge s), `TotalTime`.
- Requirements via `ItemBase.MeetsUseReqs(me)` / `ReqChecker` (skills, char state, target type, buff present/absent).

Breed-adjusted cost (from `Buff.GetCost`, currently commented):
- `cost = baseCost * max(breedFloor, NPCostModifier) / 100`, breed floors: Nanomage 45, Atrox 55, Solitus/Opifex/other 50. Read `Stat.NPCostModifier`.

Vitals / capacity stats (all confirmed present):
- `Stat.CurrentNano`, `Stat.MaxNanoEnergy` (mana).
- `Stat.CurrentNCU`, `Stat.MaxNCU` (buff capacity).

Cast API & state:
- `me.Cast(nanoId)` (self), `me.Cast(target, nanoId)` (other). `me.IsCasting` = busy.
- Success = the buff appears/refreshes in `Buffs`. Failure/interrupt = `CharacterActionType.InterruptNanoCasting` (handled in `Client.OnCharacterAction`).

## Discovery & auto-classification (NO hardcoded nano ids, NO manual lists)
Goal: works for any nano he ever learns (loots/buys) with zero config. Pipeline:

1. CASTABLE-BY-CLASS set: `ItemData.AllNanoIds()` filtered by `MeetsUseReqs(null, ignoreTargetReqs:true)`
   — every nano whose SELF requirements (profession/level/skills) he meets. (This is exactly what the
   `learnable` command already computes.)
2. LEARNED set: `me.SpellList` (uploaded nano ids, live from the wire).
3. CASTABLE-NOW = the nano metadata for each id in `me.SpellList` (via `ItemData.Find`). We only ever cast
   from this set — "only those we have learned". The class-castable set (1) is just for reporting/planning
   ("here's what you could learn").

For each learned nano, classify from ITS OWN DATA (no ids baked in):
- EFFECT & CATEGORY from `UseModifiers` (`Modifiers[SpellListType.Use]`, Stat→amount):
  - modifies `Health`/`MaxHealth` upward, no lasting duration → HEAL.
  - beneficial stat mods (positive on abilities/AC/skills/offense) AND has duration/NCU → BUFF (keep-up).
  - harmful mods (negative on target's stats) → DEBUFF. harmful instant health → NUKE (combat, later phase).
  - net sign of the modifiers decides beneficial vs harmful.
- TARGET by PROBING `MeetsUseReqs(candidate)` (empirical, no criteria parsing):
  - `MeetsUseReqs(me)` true → self-castable.
  - `MeetsUseReqs(owner)` true → castable on the owner (friendly-other).
  - `MeetsUseReqs(member)` true → castable on a teammate.
  - only harmful + target-hostile passes → debuff/nuke (hostile).
  A nano can be valid on several (self+other) — policy then picks who to cast it on.
- So: BUFFS = learned nanos that are beneficial + have duration/NCU; their targets = whoever they probe
  valid on (self / owner / team). No `SelfBuffNanoIds` etc. needed — those config lists become optional
  manual OVERRIDES/exclusions only.

Optional refinement later: use `NanoLine`/`StackingOrder` to group a "line" and only keep the best learned
nano per line per target (don't cast three strength buffs of the same line).

## Core model — "desired buffs" keep-up
Desired set = the auto-classified BUFFS from the learned set (beneficial + lasting), each paired with the
targets it probed valid on (self / owner / each team member). Built once and refreshed when `me.SpellList`
changes (he learned something) or team composition changes. Auras fold in here too.
Each decision tick, for every desired buff × valid target:
1. Resolve target (self / owner / member). If target not present/visible, skip.
2. Look at target's `Buffs`:
   - MISSING → needs cast.
   - Present but `Cooldown.RemainingTime < RebuffMarginSeconds` → needs recast.
   - Present and healthy → skip.
3. Stacking guard: if a same-`NanoLine` buff with a higher `StackingOrder` is already active, skip (never downgrade).
4. NCU guard: if casting it would exceed the target's `MaxNCU` (`CurrentNCU + NanoItem.NCU > MaxNCU`), skip + warn (can't fit).
5. Req guard: `MeetsUseReqs` must pass (skills/state), else skip + warn once.

## Cast queue + mana-aware draining
- Enqueue `CastRequest{ target, nanoId, label }` for each needed buff not already queued (reuse existing `_castQueue`, `QueueCast`, `TryDrainCast`).
- Order by priority: self-survival buffs first, then owner, then team (configurable order later).
- Drain when SAFE: not `IsCasting`, not resting, and per-config either out-of-combat only or allowed in combat (`BuffInCombat`). Movement: pause draining while actively pathing if casting-while-moving is unreliable (TBD — test).
- Before each cast compute `adjustedCost` (breed formula). Then:
  - `adjustedCost <= CurrentNano` → cast; wait `AttackDelay` (+ small margin) and confirm the buff landed (Buffs change / BuffChanged) before the next drain. Do not fire the next cast while `IsCasting`.
  - `adjustedCost > CurrentNano` → go to the refill step (below), do NOT dequeue; retry after refill.

## Stim-refill for a fluid sequence — BOTH kinds
When the next queued cast costs more nano than we have (and we're safe / not in combat):
- Prefer the SITTING RECHARGER (fills the most): sit, recharge nano up to a threshold, stand, continue the
  queue. Reuse `RestTick` sit/stand machinery so it plays nice with follow/idle. Best when there's a batch
  of buffs to get through — one sit refills a lot.
- Use the STANDING nano STIM (fills a little) for a quick top-up when only slightly short, or when sitting
  isn't worth it (about to move, or just need one more cast). Respect `HealIntervalSec` cooldown.
- Picker is the existing best-QL-usable logic (item whose First Aid [stim] / Treatment [recharger] use-req
  is met); works for whatever he loots/buys, no item names hardcoded.
- If nothing usable and nano is short → warn the owner (low supplies) and hold the queue rather than
  spam-failing casts.

## Rebuff loop (timer-driven)
- Each tick scan desired buffs' `RemainingTime`; enqueue any under `RebuffMarginSeconds`.
- Also subscribe to `BuffStatus.BuffChanged`: on Removed for a desired buff, enqueue immediately (don't wait for the poll).
- Net effect: buffs stay up continuously; the queue + refill keep casting whenever mana allows.

## Settled decisions (user, 2026-09-19)
- AUTO-DETECT everything (target + category) from nano data — no hardcoded ids, so it works for any nano he
  learns/loots/buys later. Set it up data-driven from the start.
- Buffs OUT OF COMBAT only.
- Real-time self timers are fine; owner/team best-effort accepted.
- Rebuff when < 60s remaining.
- Use BOTH refills: sitting recharger (fills most) + standing stim (small top-up).
- Only cast LEARNED nanos (`me.SpellList`); but be able to READ data for every nano the class can cast.

## Config additions (proposed)
- Category toggles, not id lists: `BuffSelf` / `BuffOwner` / `BuffTeam` (bool, default on); auto-detection
  supplies the ids.
- `RebuffMarginSeconds` = 60.
- `BuffInCombat` = false.
- Refill thresholds: `RechargerNanoBelowPercent` (sit-refill trigger) + reuse `StimNanoBelowPercent` (quick stim).
- Optional manual OVERRIDES only: `ExcludeNanoIds` (never cast), `ForceNanoIds` (always keep up) — for edge
  cases the auto-detect gets wrong; empty by default.

## Policy notes (user)
- SELF-BUFF PREFERENCE: some professions can learn either an HP buff or a nano buff in the same slot/line.
  When both are learned, ALWAYS keep the HP buff (prefer the nano whose Use modifiers boost Health/MaxHealth
  over the one boosting Nano). These are self buffs. Implement as a same-line tiebreak, no hardcoded ids.

## Open questions to settle before/while coding
1. Owner/team buff staleness — how often does the server resend their ActiveNanos? May need to accept best-effort timers or re-request a full update. (Self is real-time.)
2. Casting while moving — does the live server accept a cast mid-move, or must we stop? Test on the wire before relying on it.
3. Cast-failure handling — resisted / interrupted / out-of-range: detect via InterruptNanoCasting + buff-not-landed, then backoff/retry (avoid infinite requeue).
4. NCU accounting — confirm `CurrentNCU` reflects live used NCU, or compute used = sum of active buff `NCU`.
5. Stacking semantics — confirm higher `StackingOrder` = "better, don't overwrite with lower".

## Isolation / do-not-break
- Everything above is added inside `SupportController`; the decision ladder in `Main.Decide` already puts the cast queue near the top (castqueue > heal > fight > rest > follow). Buff-keepup only ENQUEUES in safe states so it can never walk the bot off (follow/zone) or interrupt combat targeting.
