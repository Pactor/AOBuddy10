# AOBuddy — HARD RULES (read before every action)

These are binding instructions, not suggestions. They override my defaults. The user has
repeatedly caught me guessing and it has cost whole days. Follow the gates below on EVERY action.

## The gate — run this before any Edit / Write / Bash / build

1. **Cite evidence first.** Before changing code or claiming how the bot behaves, name the exact
   source that justifies it: a line in `aobuddy.log`, a decoded sniff under `E:\Funcom\captures`,
   or a specific file:line in the code. If I cannot cite it, I STOP and go look. No evidence = no action.
2. **Read before claiming.** Read the log / sniff before stating what the bot does or "fixing" it.
   Do not describe behavior from memory or assumption.
3. **Do not invent rules.** Only the rules the user actually stated go here. Do not attribute a rule
   to him that he did not give (e.g. "revert first" was never his rule — do not re-add it).

## Standing rules (from the user, non-negotiable)

- **No guessing, no hardcoding, no pretending, no half-measures.** Verify against the captures/sniffs
  (`E:\Funcom\captures`) or the running bot's `aobuddy.log`.
- **No per-character hardcoding** (perks, stats, nano ids). Solutions must work for ANY class/character
  via auto-detection from the wire.
- If it can't be done right yet, say so plainly and lay out the real options — don't fake it.
- **Pronoun for the user and the bot char: "he" / they. Never "she".**
- Distrust conclusions inherited from a compacted/prior session until re-verified against data.

## Environment facts (so I don't re-derive or assume them)

- Bot char = **Fourtoes** (Keeper). Owner = **Starn**. Live retail target.
- Plugin: `E:\Funcom\AOSharp.Clientless\AOBuddy` → `bin\Debug\AOBuddy.dll`. Log: `bin\Debug\aobuddy.log`.
- SDK: `E:\Funcom\AOSharp.Clientless\AOSharp.Clientless` → after building, COPY
  `AOSharp.Clientless.dll` to `Test\bin\Debug\` (the runtime loads it there).
- Build (bot MUST be closed — check `tasklist | grep PluginLoader` first, or read the log's last
  timestamp): MSBuild the `.csproj` with `-p:Configuration=Debug -p:BuildProjectReferences=false`.
## ARCHITECTURE — isolated systems (2026-09-19). EDIT ONE FILE PER SYSTEM.

The bot is split so working on one system can't break another. Each controller owns its own
state; nothing else touches it. When changing behavior, edit ONLY that system's file:

- `Movement.cs` — the ONE place the body moves (SetPose + client ForwardStart/Update/Stop cadence +
  a single step). Every mover calls it. Change HOW the body moves here; never elsewhere.
- `FollowController.cs` — FOLLOW. Owns `_trail`. Records the owner's footsteps (each carries his real
  X/**Y**/Z, so ramps are followed) and walks them; manual `come`/`zone`; saved-path replay;
  zone-crossing. **All zone-crossing work goes here** — it can't reach combat/travel.
- `CombatController.cs` — COMBAT. Owner-only targeting + Attack ONCE per target. Never moves the bot.
- `TravelController.cs` — use-object travel (lift/button/grid/whompa). Takes over movement while
  riding, hands back to follow when the owner reappears.
- `SupportController.cs` — stims (both), rest/recharger, auras, self-vitals, supplies. No movement.
- `Main.cs` — wiring only: tick loop, chat commands, perks/knowledge, `Walk()` (movement arbiter:
  cast/rest > travel > follow) and `Decide()` (ladder: castqueue > heal > fight > rest > follow/idle).
  `ClearNav()` resets every controller on a zone/teleport.

Invariants (do NOT change): FOLLOW runs during combat too (she stays at his side; combat is a
non-moving overlay). COMBAT never drives movement. Attack is issued once per target (re-issuing
resets the swing timer = the "never swings" bug). Never send StopAttack. Never apply SetPos.

## VERIFIED WORKING (2026-09-19) — stand-up + speed + auto-zone. DO NOT "fix"/revert these three.

Confirmed live by the user: the bot stands on login, stays visible/synced, follows, and **auto-zones by
following** (crosses zone lines with no command). These three are LOAD-BEARING and settled — do not
re-litigate them after a compaction (that churn is what kept breaking it). Change them only with fresh
wire evidence that contradicts what's below, and never as a side effect of touching another system.

1. STAND-UP (Main.OnUpdate). Read `Stat.CurrentMovementMode` (173) ONCE at login; send ONE stand-up
   (`MovementAction.LeaveSit` → `CharacterActionType.StandUp`, wire action 87) only if the login mode is
   seated (MoveModes: Sit=8, Sleep=11, Lounge=12). NEVER poll-and-resend: action 87 is a sit/stand
   TOGGLE and stat 173 is set from the login FullCharacter and never updates after — re-reading a stuck
   8 and re-toggling made him sit/stand in a LOOP; firing it blind on a standing bot SAT him (the freeze).
2. MOVE SPEED (FollowController.MoveSpeed). Use the game client's EXACT velocity formula (user-confirmed):
   `velocity u/s = 5.5 + RunSpeed/230`, hard cap 15.5 u/s (at RunSpeed 2300). RunSpeed = Exploring->Run
   Speed skill = Stat 156. So RunSpeed 36 -> ~5.66 u/s; training the skill raises it. This is exactly what
   the real client sends, so the server accepts it. Do NOT invent a coefficient (a 0.59/point guess was
   ~4x too fast; the old OmniCell `4 + rs/200` was close but wrong constants). The "24.8 u/s" seen in the
   heartbeat ospd was measurement noise (position glitches / zone jumps), NOT real velocity. Note: the
   speed was never the freeze — that was the seated-login bug; speed only affects keeping-up. Sweep speed
   is deliberately slower (3 u/s) to dwell in the zone-line band.
4. USE-TRAVEL (TravelController + Main OnDynelUsed). Floor buttons / lifts / mission & zone terminals are
   TELEPORT objects (wire-proven: a floor button jumps you e.g. (147,74,152)->(74,10,31), Y 74->10). The
   owner uses one via `GenericCmdMessage Action=Use` targeting the object identity (type ~0xC73D). The bot
   rides it AUTOMATICALLY — no command: OnDynelUsed captures the owner's used object, and when the owner
   then teleports (goes LOST within 5s) TravelController walks to that same object and sends Use. Confirmed
   riding floor buttons up AND down. This is why the owner never has to type (he gets aggro). Keep travel's
   shot ahead of the zone-sweep (sweep is guarded by !_travel.Active).

3. AUTO-ZONE (Main owner-loss + FollowController sweep). Do NOT sweep the instant the owner vanishes —
   let the breadcrumb trail walk the bot to the line (works when the owner outran him), then arm the
   sweep only when the trail is exhausted (`!_follow.HasWork`) while the owner is lost. Tight sweep:
   `SweepSpan=3`m each side, `SweepSpeed=3`. One attempt per loss episode (`_zoneSweepTried`, reset on
   reacquire). Guards: not combat, not travel, follow on, Assist. A POSITION JUMP (ClearNav) ends it.

## HOW THE FREEZE WAS FOUND (2026-09-19) — bot logs in SEATED and a blind stand-TOGGLE re-sits him.

Wire-proven (sniff `marked-20260919-165312` stream 9 decoded; bot = **Choppedbacon = 227558103**,
owner = **HiddenBacon = 227558104** — map names first). The freeze: the server holds the bot at its
login spawn and sends `SetPosMessage{Coordinates=spawn, StopMoving=1, UpdateLastAllowedPosition=1}`
on repeat — **85 of 85 SetPos in one run all target the IDENTICAL spawn coord**, i.e. the server never
accepted a single move. A **seated character cannot move**, so the server rejects every move packet and
re-pins him to where he sat; server-side he's never beside the owner → dropped from nearby clients =
"gone / invisible". Mechanism, exact:
- You log out by SITTING, so you can log back in seated; but Ctrl+C'ing the bot process leaves it
  STANDING, so the bot's login movement mode is **inconsistent** run-to-run.
- `Stat.CurrentMovementMode` (173) at login read **3 = Run** in the frozen run — i.e. he logged in
  STANDING. MoveModes enum (OmniCell `MoveModes.cs`): None=0 Rooted=1 Walk=2 **Run=3** Swim=4 Crawl=5
  Sneak=6 Fly=7 **Sit=8** SocialTemp=9 Nothing=10 **Sleep=11 Lounge=12**.
- The old `Main.OnUpdate` fired `ChangeMovement(LeaveSit)` **unconditionally, once, on entry**. That maps
  (SDK `LocalPlayerMovementComponent`) to `CharacterActionType.StandUp` = wire action **87**, which the
  SmokeLounge decoder shows is a sit/stand **TOGGLE** (decoded as `SitToggle`, seq 6 in the sniff). On a
  standing bot the toggle **SAT HIM DOWN** → frozen. This is why it "worked this morning" (that login was
  actually seated, so the toggle stood him) and failed after a Ctrl+C logout.

FIX (committed): stand-up is now STATE-AWARE in `Main.OnUpdate` — read `CurrentMovementMode` each frame,
send the StandUp toggle only while seated (Sit/Sleep/Lounge), debounced (`StandUpRetrySec=1.5`) so it
self-corrects (retries a dropped stand) without oscillating, and never toggles a standing bot. Heartbeat
now logs `movemode=`. Watch: on login he should read `movemode=3` (or 2) and stay; if seated he logs
`STAND: seated ... sent stand-up` then flips to standing and stops.

DISPROVEN earlier-session theory (do NOT revive): "the SDK ignores SetPos so he desyncs — fix by applying
SetPos." Applying SetPos (commit 7f8a5c7, `OnServerCorrectedMe` SetPose+Reset) did NOT fix it — the log
just changed from "walks 66m off" to "oscillates at spawn", because the real block was the seated state.
SetPos handling (VERIFIED — do not revert to either extreme). `OnServerCorrectedMe`:
- STOPPED → always apply (re-sync).
- MOVING → ignore corrections smaller than ResyncGap (10m) so ramp/Y jitter (~7m) doesn't rubber-band
  him down slopes; but APPLY anything >= 10m. Ignoring ALL corrections while moving (the first version of
  this gate) let the gap run away 10->38m and he VANISHED behind the owner mid-move; applying every
  correction rubber-banded him on ramps. The 10m cap is the middle ground: keeps the ramp fix, stops the
  vanish. Both failure modes are wire-proven — do not go back to pure-ignore or pure-apply.

RULED OUT (do not re-chase): SPEED — the bot moves ~21.7 u/s while a real running client moves ~5 u/s on
the wire (measured: 20m over 3989ms dead-reckon gaps, sniff `20260914-124401_s4`). Real speed is governed
by the Exploring→Run Speed skill (owner's is 42). This is a REAL bug to fix later, but it is NOT the
freeze (a seated char is rejected at any speed). Idle keepalive (position spam) — only made him blink,
removed.

## COMBAT VANISH — PARKED (2026-09-19). Intermittent; do NOT blind-fix while it works.

SYMPTOM: sometimes, when he's standing still next to the owner and combat starts, he VANISHES from
the owner's screen — while his own log shows him fighting fine at dist=1.0 (atk=True, pos steady).
INTERMITTENT: same build both works and fails across runs; happens on flat/"sane" terrain too. Combat
otherwise works (swings, stims). It is NOT the old zone-push walk-off (that was moving=True; this is
stationary, moving=False) and NOT the refactor (movement/attack packets are byte-identical; SDK is
current observe-only, same dll that was loaded when combat worked).

DIAGNOSIS (best, not yet wire-confirmed on a FAILING run): a server position desync. While he MOVES,
the owner's client redraws him every 100ms from his CharDCMove stream (so follow looks fine); the
instant he STOPS to fight, the owner's client reconciles him to the SERVER's position for him — and
the server's position lags where his client (SetPose) thinks he is. The `DESYNC CHECK` log line is a
REAL local-player SetPos correction (SDK fires LocalPlayerCorrected only when identity==local), not a
fixed-entity mis-attribution — earlier note calling it a "false alarm" was wrong. Gaps grow to 50m+.
A real client stands still 219s sending nothing and stays visible, so a keepalive is NOT the fix.

TO FIX RIGHT (needs a FAILING capture): run `OmniCell\Tools\Capture\capture-marked.bat`, reproduce
the vanish, type mark `vanished`, stop. Capture lands in `E:\Funcom\sniffs\pcap\marked-*.pcapng` (+
.marks.txt). Decode per-stream: tshark `-z follow,tcp,raw,<S>` (ports 7100-7999) -> FollowToCsv.exe
-> PcapDecode.exe. The owner's OWN client stream (sends LookAt/SetWantedDirection) carries, on its
/server side, the SERVER's broadcast of the bot's char (a CanbeAffected Identity) — at the `vanished`
mark, see if the server DespawnMessage's the bot, moves it far, or he stops being broadcast. That
names the fix (likely: accept SetPos ONLY while stopped so he stays where the owner sees him, then
re-walks back — safe from ramp-bounce which only happens applying corrections while moving).

## ZONE-CROSSING — MECHANISM FOUND in OmniCell server code (2026-09-19). Sweep the line.

A walked zone line is a WALL SEGMENT, not a statel/packet. From OmniCell (the CellAO reimpl of retail —
`Server/ZoneEngine/Core/Playfields/WallCollision.cs` + `Playfield.CheckWallCollision` called from
`Playfield.HeartBeatTimer`):
  - The server tests your position against the playfield's wall segments on a **HEARTBEAT (~0.25s)**,
    NOT per move packet.
  - It fires only if your **X,Z is within ~2 units (WallCollisionThreshold) perpendicular of the
    segment AND between its endpoints. Y is IGNORED.** On a hit it reads the wall's DestinationPlayfield
    and teleports you (landing = interpolated along the dest wall by the crossing factor, +8 units in).
  - There is NO client "change playfield" packet — server-initiated (confirmed on the wire too: the bot
    only sends CharDCMove; the server sends N3Teleport + ZoneRedirection).
WHY the bot failed every movement fix: a glide at run speed crosses the narrow ~2-unit band BETWEEN
heartbeat ticks and no snapshot catches him in it; a one-way creep drifts off the actual segment. This
is also why a HUMAN sometimes has to step back and forth on a line to trigger it (user confirmed).
FIX (general, no per-zone coords — needed for entering AND exiting every zone): on owner-loss ahead
while following and NOT in combat, the bot SWEEPS back and forth across the vanish spot along the
owner's travel axis (FollowController.StartZoneSweep / SweepTick), slow, until a heartbeat catches him
in the band and he zones (POSITION JUMP -> ClearNav ends it), owner returns (cancel), or timeout. The
'zone' command does a manual sweep too. Combat-guarded so it can't fire mid-fight.
NOTE: OmniCell's OWN statel/wall check is commented out (stubbed) — it does not implement walked zone
lines; the code above is the model/threshold, retail's exact radius may differ, tune SweepSpan/Speed.

## ZONE-CROSSING — WIRE-PROVEN (2026-09-19). Cause = reaching the trigger, not the SDK.

Capture `sniffs/pcap/marked-20260919-141424*.pcapng` (decode: tshark `-z follow,tcp,raw,<S>` ports
7100-7999 -> FollowToCsv -> PcapDecode --ordered) caught a SUCCESSFUL bot zone. The bot's own
connection (the stream with NO SetWantedDirection/LookAt; Fourtoes = Identity 227558104) received:
  N3TeleportMessage Destination=3305.96 35.34 837.14 ChangePlayfield=... , then
  ZoneRedirectionMessage 37.18.193.20:7501 , then the bot sent ZoneLoginMessage(cookie), got
  CharInPlayMessage + FullCharacterMessage, and walked at the far-side coords.
=> The SDK zone machinery WORKS end to end (NetworkSession.cs:418 ZoneRedirection handler reconnects,
   State.Zoning -> FullCharacter -> InPlay). Do NOT "fix" the SDK zoning.

The intermittency ("zones sometimes, never always") is purely whether the breadcrumb walk carries the
bot ONTO the trigger before the trail drains. The owner's dynel position updates less often than our
crumbs, so his last SEEN spot is a few metres short of the real trigger; if the bot stops there, no
teleport is ever sent. FIX (2026-09-19, FollowController.PushThroughZone + guarded arm in Main's
owner-lost branch): when the owner vanishes right ahead (<=ZoneLossMeters) while following and NOT in
combat and not use-travel, extend the path onto his vanish spot +ZonePushMeters(10) along his travel
direction so the walk hits the trigger. The combat guard (_combat.InCombat) is what the OLD +25m push
lacked -> that fired mid-fight and walked the bot off ("disappearing during combat"). Keep the guard.

## ZONE-CROSSING — earlier notes (superseded by the wire proof above)

STATUS: follow + fight + stims + use-travel all work (tag `known-good-follow-fight`). The one unsolved
thing is the bot following the owner through a **walked zone line** — it reaches the line but only
~30% of the time does the server actually zone it (user says this has always been flaky).

FALSE ALARM — DO NOT CHASE: the `DESYNC CHECK` log line I added shows a growing gap vs
`server=(144,108,222)`. That value is IDENTICAL every session → it is a FIXED entity's SetPos being
mis-attributed to the local player, NOT the bot being frozen. The bot IS synced and moving (user
watches it run into the zone in-game). The SetPos observe handler is diagnostic-only and never moves
the bot. Ignore the desync gap; the bot is not desynced.

EVIDENCE from a real client zone crossing (`E:\Funcom\captures\newchar_s16.csv`, decode with
`E:\Funcom\OmniCell\Tools\Capture\bin\PcapDecode.exe <csv> SmokeLounge.AOtomation.Messaging.dll --ordered`):
the client walks `CharDCMove` onto the line and the SERVER spontaneously sends `N3TeleportMessage`
(Destination + ChangePlayfield) then `ZoneRedirectionMessage` (new server ip:port). Key facts:
  - The **Y ramps UP into the line** (14.01 → 14.37 → 14.72 → 15.135); the trigger is at a specific
    elevation. The bot's crossing push uses its flat heading (Y stays ~constant) so it slides across at
    the wrong height and never enters the trigger volume.
  - It is **still MOVING** when it crosses (last packet MoveType=22, moving) with real, varied
    `MillisecondsSincePreviousMove` (199, 549, 519…). A bot that stops ON the spot doesn't cross.
NEXT STEP (agreed direction, not yet done): make the crossing follow the owner's EXACT recorded
footsteps — which carry his real X/Y/Z up into the line — THROUGH the trigger while moving, instead of
the current flat-heading straight push (in RecordTrail owner-loss: `_lastOwnerPos + heading.Forward*ZoneChaseMeters`).
Recent commits toward this: removed the far-reacquire trail-clear (caused a single-far-crumb beeline);
added a walk-through-the-crossing-spot push (still flat-Y — this is what needs to become his real path).

## KNOWN-GOOD BASE — follow + fight CONFIRMED WORKING 2026-09-19 (git tag `known-good-follow-fight`)

User confirmed: bot follows through doors AND fights + uses stims. This is the base. Do not re-open
these settled points by guessing — they were each earned over a long, painful debugging day:

- **FOLLOW = walk the owner's recorded footsteps (breadcrumbs).** Record his positions (they carry the
  correct terrain Y); walk that path in small per-frame steps (like the client's /follow sends CharDCMove).
  Do NOT beeline to his live position — the server terrain-validates and rejects it on slopes. On loss,
  keep the trail and keep walking it (through the door); a path ending on a zone line walks the bot onto
  the trigger and it zones. `RecordTrail` records; `Walk` follow branch walks `_trail`. Nothing else.
- **A door is NOT special** — it is geometry you walk through; it auto-opens. No door/zone-pursue/
  zone-attempt/snap/forward-push code. Retracing the recorded footsteps handles it.
- **`/follow` is NOT a sendable packet.** Proven from the sniff: the client sends `CharDCMove` (it walks
  itself); `FollowTargetMessage` is server→client only. Do NOT try to "send the follow command".
- **SetPos: NEVER apply it to the local player** — it warps/bounces the bot. The SDK has an observe-ONLY
  handler (logs a desync check, never moves the bot). Do not make it apply setpos.
- **FIGHT = issue Attack ONCE per target, then never again.** Gate on the TARGET CHANGING
  (`_attackedTarget`), NOT on `IsAttacking`. Re-issuing Attack restarts the weapon timer, so re-sending
  it (e.g. around each stim) makes the bot swing once then wait — the "never swings" bug. The server
  auto-swings once started.
- **NEVER send `StopAttack`/`StopFight` for heals or at fight end.** The game auto-stops combat when the
  fight is over. Using a stim is just `Item.Use()` — issue NO combat packet to heal (that resets the swing).
- **USE-TRAVEL stays** — owner uses a floor button / lift / grid / whompa → bot rides the same object
  (armed by an OBSERVED `DynelUsed`, handled in `HandleUseTravel`). This is the mission-travel fix.
