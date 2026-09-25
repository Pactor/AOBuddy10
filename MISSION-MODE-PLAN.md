# Mission mode for AOBuddy - plan (2026-09-23)

Goal, in the owner's words: send him to a mission terminal, have him roll the closest mission
(Borealis itself preferred), tell us the rewards, run it as a blitz - fastest way to the up/down
buttons, fight nothing, reach the top room, target the person or pick up the item - then come back
and hand us the reward as proof.

This file is the hand-off for a new session. Every claim below names its evidence. Rules that
apply throughout: no guessing, verify on the wire or in `aobuddy.log`, one file per system, and
never touch follow / combat / travel / zone-crossing as a side effect.
Mission mode goes in its own file, `AOBuddy/MissionController.cs`, wired through `Main.cs`
only (a command, a tick call, and a slot in the movement arbiter). It must be off unless the
owner turns it on, and every step below ends with a test the owner can watch.

## What is already true (do not re-derive)

Mission geometry is solved and verified. Read `NAV-CLIENTDATA.md` (last three sections) before
touching any of it.

- `AOBuddy/GameData/Nav/<pf>/rooms.json` holds every dungeon and every autocontent mission
  pool (320 Midtech, 321 HiTech, 322 Cave, 324 Clan, 331 tarm, 341 Grey Caves, 346 Omnilab,
  351 Subway Ventil, 362 SL ACG, 382 Alien ACG): per room the floor tiles, heights, rotation,
  doors, and the pool room names (`elevator`, `militaryot_elevator`, `hitech_bossroom2`, ...).
- The zone-in packet (`PlayfieldAnarchyF`, version 4) carries `BuildingGeneratorData`: template
  pool, 30x30 slot grid, worldHeight 64, and (room, floor, x, z, rotation) per placed room.
  `AOBuddyNav.DecodeZoneIn` reads it; `AOBuddyNav.LoadMission` composes the instance. Verified
  on three live missions this morning: 100%, 98.3%, 97.1% of walked points on the composed
  floor within 1 m. `Main` keeps the last zone-in packet in `_lastZoneInPacket`; `navdata`
  in a mission answers from the composed rooms. `tools/navbridge/composemission.py` grades any
  saved packet (`Build/Plugins/AOBuddy/missions/`) against its walk (`nav/<instance>.json`).
- Floor height is `pool room y + floor * worldHeight`: he walked at 5, 69 and 129.
- Riding a floor button is wire-proven and works today (`TravelController`): the button is a
  teleport object, used with `GenericCmd Use` on its identity;
  the server answers with `N3TeleportMessage` in the same playfield.
- The terminal's mission list is `QuestAlternative` (0x5C436609), decoded per mission by
  `ClickSaver2026.Core/Missions/MissionListParser.cs`: type code at 0x2C, destination playfield
  at 0xAC, entrance x/z at 0xB8/0xC0 (offsets from the end of the reward items), difficulty,
  the six sliders, terminal identity, and the reward items. Mission types (ClickSaver
  `MissionTypes.FromCode`): 0x2C4E Repair, 0x2C41 Return item, 0x2C47 Find person,
  0x2C49 Find item, 0x2C42 Kill person.
- Accepting a listed mission is `CreateQuestMessage(QuestIdentity)` (OmniCell messaging,
  `N3MessageType.CreateQuest` 0x291F361B). A mission rolled at a TEAM terminal goes to every
  team member automatically (owner's word, 2026-09-23; the find-person capture shows the
  teammate receiving the reward and the quest removal). A mission from a solo terminal cannot
  be handed over; copying the key only lets a teammate enter, with no quest updates. So the
  owner rolls at a team terminal with the bot in team, and steps 3 and 4 can be tested before
  step 5 exists.
- Terminal and mission-door positions per playfield are in `AOBuddy/GameData/Zoning.json`
  (Algorithman's extract: statels with teleport functions and ACGEntrance doors).

## What the existing captures already answer (checked 2026-09-23)

Decoded with `OmniCell\Tools\Capture\bin\PcapDecode.exe <csv> ... --ordered`; the decoded
streams named below are in `E:\Funcom\sniffs\` (csv + txt), the raw recordings in
`E:\Funcom\captures\pcap\` and `E:\Funcom\sniffs\pcap\`.

| question | answer | evidence |
|---|---|---|
| Roll request | the client sends `QuestAlternativeMessage` (same id 0x5C436609 as the reply): VersionId=4, Difficulty (byte, 6/9/11 seen), the six sliders (0 here), Seed=0, Originator=NeutralBooth, MissionTerminalIdentity=56001:<terminal instance>. The server answers with the same message type holding 5 QuestInfos. Every roll is one such message; the terminal is `GenericCmd Use`d once before the first roll, not before each roll. Slider bytes are signed, percent mapped to (percent - 50) * 2: 50% = 0, 0% = -100 (0x9C), 100% = +100. Byte order: GoodBad, ControlledLackingControl (order/chaos), OpenHidden, PhysicalMystical, ExplosivePatient (head-on/stealth), MoneyExperience; 0% credits/xp = -100 = all credits. Originator 1 = NeutralBooth (solo), 2 = NeutralBoothTeam (team terminal). The server keeps the sliders between rolls: eight rerolls sent identical bytes. | `20260910-200346_s2.txt` client seq 37 (Use terminal 56001:-1073741169), 38/41/46/48 (four rolls), server seq 2514/5356/8951/11472 (the lists); `20260923-120056_s1.tsv` 12:02:18-12:05:11 (team terminal, sliders 0,0,-100,0,0,-100 for the owner's 50/50/0/50/50/0) |
| Accept | `CreateQuestMessage QuestIdentity=Quest:<id from the list>`; the server replies `QuestFullUpdateMessage AnnounceAsNew=1` with a NEW quest identity (list id ...C9CB became ...C9D0). Delete is `QuestMessage Version=1 QuestIdentity=<new id>`. | same stream, client seq 50 and 54, server seq 11850 |
| Repair objective | `GenericCmdMessage Action=UseItemOnItem Target=[2]`: first the inventory item (type 0x68, slot), then the object (type 0xC73D). Server echoes it with Verification=1, then Cash stat, reward item to the overflow window (`TemplateAction 87` + `ContainerAddItem`), `FeedbackMessage MessageId=108871108`, `CharacterAction MissionChanged`, `QuestMessage` (mission removed). | `20260910-203534_s37.txt` client seq 1292, server seq 19742-19749 |
| Mission zone-in | `PlayfieldAnarchyF` version 4 with BuildingGeneratorData, then `QuestFullUpdateMessage` with the mission text (objective name is in the text: "use the Targeted Radiation Extractor to heal the Fire and Radiation Chamber"). | same stream, server seq 2 and 85 |
| Player-to-player trade | `TradeMessage Version=2`: open = Action=None Target=<other>, credits = Action=7 Target=None:<amount>, accept = Action=3, confirm = Action=End; the other side accepts with Action=3 then End; server closes with Action=4 (Unknown) to both. `AOSharp.Clientless/Trade.cs` exists. | `20260911-061635_s6.txt` / `_s8.txt` (both accounts) |
| Floor buttons | items of type 0xC73D, recognised by template id in their `SimpleItemFullUpdate` (stats 701-703): 159863 `Button (down)`, 159869 `Button (up)`, 159864 `Button (boss)` (straight to the boss room). 159862 and 159865-159868 are `Platform 1`-`Platform 5`, standing on the same spots; not used. Riding: client `GenericCmd Use` on the button, server echoes it, sends `CharacterAction 170 p1=54 p2=10` (stat 54 = Level locked for 10 s) and `N3TeleportMessage` in the same playfield within ~0.3 s. The destination is the position of the paired button on the other floor. `CharacterAction 164 p2=54` arrives 10 s later: that is "Level skill available"; the bot waits for it before the next Use. Same pair 170/164 locks and releases every skill (p1=142 brawl 15 s, 123 first aid 40 s). | `20260923-114223_s4.tsv` / `_s5.tsv` (timed, both accounts): four rides each, e.g. Use 11:45:08.161, lock 11:45:08.270, teleport 11:45:08.321, release 11:45:18.380. The 0xC76A objects in the 2026-09-19 notes are corpses, not buttons. |
| Find person objective | target the named NPC: client `CharacterAction InfoRequest` (0x69) plus `LookAt` on its identity. The mission completed 0.47 s later with no attack by that account. The name is in the mission text (`QuestFullUpdate`: "...Malik Cratty is helping mutants...") and on the NPC's `SimpleCharFullUpdate`. Completion is the same sequence as Repair: reward items to the overflow window (`TemplateAction 87` + `ContainerAddItem`), `FeedbackMessage 108871108`, `CharacterAction MissionChanged` (mission holder only), `QuestMessage`. The teammate got the reward and the removal too. | `20260923-114223_s5.tsv` 11:50:35.558 target, 11:50:36.036 completion; `_s4.tsv` 11:50:35.787 teammate. Second run: `20260923-120056_s9.tsv` targeted Twinger Scorpiod and Jamar Borroel (nothing), then Marquis Forejt, the name in the text, at 12:10:47.972; MissionChanged at 12:10:48.452. Only the named NPC completes it. |
| Find item objective | the same as find person: select the item, nothing is picked up. The client sent one `LookAt` on the floor item (type 0xC73D, template 100341 `Urgent Sensitive Information`, the name in the mission text) with ReturnInfo=0, and no GenericCmd. The mission completed 150 ms later with the usual sequence; the item stayed on the floor until he left the room. For people the client sends `InfoRequest` + `LookAt` with ReturnInfo=1; for the item only `LookAt` with ReturnInfo=0. The SDK's `Targeting.SetTarget` always sends ReturnInfo=0, which matches the item case; for a person the bot should send what the client sends. The mission item is found by template name against the mission text, like the person. | `20260923-125821_s4.tsv` LookAt 13:04:11.152, completion 13:04:11.304 (owner mark 13:04:09 "picking up item"). Again on live, 2026-09-24 (`20260924-074524_s9`, owner mark 08:01:45): quest record `0x2C49` then `0xC73D:58360C1A` (same template 100341); the item was sent at zone-in 56 m from the entrance; one `LookAt` ReturnInfo=0 from 2.7 m at 08:01:45.575, completion 165 ms later, no other client message in between. Corpses (0xC76A) and skulls (0xC749) opened on the way held only loot; a container's full update carries no contents, only the `InventoryUpdate` answering its Use does. |

## Built (2026-09-23, evening) - not yet run live

`AOBuddy/MissionController.cs`, wired through `Main.cs` only (message feed, the `mission` command,
a slot in `Walk()` after resupply and before travel, stop on death, and the owner-lost zone sweep and
nav catch-up held off while a blitz runs). Off unless the owner types `mission blitz`.

What the captures settled while building it:

- **Floor heights count up from the lowest floor.** Grey Caves numbers its floors 0, -1, -2 and they
  were walked at y 133, 69, 0; the old `pool y + floor * 64` put every negative floor underground.
  `AOBuddyNav.LoadMission` now uses `pool y + (floor - lowest floor) * worldHeight`. All eight saved
  missions still fit (the ones starting at floor 0 are unchanged).
- **Walkable rule.** Each room's last row and column is the cell shared with its neighbour and holds only
  0 or 0x80. A cell is floor when ANY room covering it has a tile; cells where every covering room says 0
  are walls, kept only as a costly fallback. Scored on eight walked missions: 99.6% of 1,011 walked steps
  connect (with the fallback), every button leg of the three 2026-09-23 runs routes. Steps may climb 2 m.
- **Buttons are sent per floor on arrival**, up to 270 m away, so the bot never searches for them.
  `Button (boss)` joins the boss room to the floor next to it (the boss room was on top in HiTech and
  Midtech, at the bottom in Grey Caves); up/down join the ordinary floors. The planner: ride toward the
  boss room's floor, take `Button (boss)` when it is on this floor, take the one button in the boss room
  to leave. Replayed against the three runs it picked the owner's next button 9 times out of 9, out and back.
- **The target is in the quest record.** `QuestFullUpdate` holds, per mission: holder identity, type
  code (0x2C47 find person, 0x2C49 find item, 0x2C4E repair), then the target identity (repair: tool,
  then object), and about 230 bytes on the building identity `(0xC79F, instance)` the zone-in packet
  carries - so the bot picks the record for the building it is in. A team member's copy has the type and
  building but NO target (capture -114223 s4); then the target is the NPC or item whose name is in the
  mission text. Boss-room targets are sent with the ride into the boss room; the Sep 10 repair chamber
  (single floor, no boss room) was sent at 88 m, so without a boss room the bot searches room by room.

Checked offline with a harness that loads the real captures through the C# code (kept outside the
repository at `E:\Funcom\sniffs\missiontest`: `Harness.exe` for grid/routes/records,
`Harness.exe planner` for the button choices).

### Live test, in this order (bot in team, owner rolls at a TEAM terminal so the bot gets the mission)

1. Enter the mission with the bot. `mission status`: pool, floors, boss room and floor, the bot's floor
   and room, the buttons it has seen, the mission type. Compare with what you see.
2. `mission route`: the next hop (which button, or the target), metres and the rooms it passes. Walk it
   by hand if you want to confirm.
3. `mission blitz` with you watching. The log lines to read afterwards all start with `MISSION:`.
   `mission stop` ends it at any point and hands him back to follow.

Not built yet: rolling at a terminal (step 5; the wire format is known), return item and kill person,
the trade at the end, walking out through the exit door (the blitz stops at the entrance).

## What is still not known, and how each gets known

| unknown | how to settle it |
|---|---|
| Where the buttons stand inside a room | they arrive as `SimpleItemFullUpdate` with position once in range, so the bot reads them live. Still worth comparing with the pool room's `objects` list in `rooms.json` so the route can aim at a button before it is seen: the capture has four button positions to test against (e.g. `Button (down)` at (47.3, 133.4, 22.8), `Button (boss)` at (265.9, 69.0, 107.6)). |

## The steps, each with its test

1. **MissionController skeleton and the `mission` command.** `mission on/off/status`,
   `mission blitz`, nothing else. Off by default. Reads `_lastZoneInPacket` through
   `AOBuddyNav.LoadMission` when he is in an instance. Test: `mission status` inside a mission
   prints the pool, the floor count, the elevator rooms and the top room by name, and his own
   room.

2. **In-mission routing over the composed rooms.** A* on the 2 m cell grid: floor tiles are
   walkable, rooms connect through the shared door cells on slot boundaries (the one-cell
   overlap), floors connect only through the elevator rooms. Path = list of world points with
   the composed heights. No movement yet. Test: `mission route` from where he stands to the top
   room prints the room-by-room path and the two buttons it will use; owner walks it by hand and
   confirms it is the real way.

3. **Walking the route.** Feed the path to `Movement.cs` the way follow feeds breadcrumbs (small
   steps, real Y from the composed floor). Stop at the button, `Use` it (the existing travel
   code path), wait for the level-skill release from step 0's capture, continue on the new
   floor. Guards: owner not lost, no combat (combat stays a non-moving overlay). Test: in a
   mission the owner rolled and gave him, `mission blitz` takes him from the entrance to the top
   room and back to the entrance with the owner watching. Log every room entered.

4. **Objective in the top room.** Find person / Kill person: target the named NPC (targeting
   exists in `CombatController`; for a blitz he targets and does not attack). Find item / Return
   item: walk onto the item and pick it up (needs the capture from the table). Repair: use the
   inventory item on the object (needs its capture). Test each type once with the owner
   watching; log the completion message.

5. **Rolling at a terminal.** Walk to the terminal (outdoor A* over `ground.bin` + collision,
   terminal position from Zoning.json), Use it, send the roll request found in the capture,
   parse `QuestAlternative`, pick the closest destination (entrance x/z vs terminal, Borealis
   first), report type, destination and rewards in chat, accept with `CreateQuest`. Test:
   `mission roll` at the Borealis terminal reports a list that matches what the owner sees.

6. **The full loop and the proof.** `mission run`: roll, walk to the door, enter, blitz, complete,
   walk out, walk back, report the reward. Trade to the owner last, once its capture exists.

Steps 1 and 2 need no captures and no game time beyond one mission to check against; they are
the place to start. Steps 3 and 5 each wait on one capture. Step 4 waits on two.

## Things that are settled elsewhere and must not be reopened

Follow, combat, stand-up, move speed, use-travel and the zone sweep (all wire-verified,
known-good — do not reopen them). Mission mode adds a movement source to the arbiter in
`Main.Walk()` below cast/rest and beside travel; it never sends StopAttack, never applies SetPos,
and hands control back to follow the moment the owner turns it off or is lost.

## Solo mission run (2026-09-23, night) - running live

The bot now runs missions on its own from a solo terminal, in a loop, until the owner stops it. Live tonight
in Borealis: missions completed end to end (roll, travel, door, blitz, walk out, reward into a backpack, back
to the terminal). It drives the existing controllers only through their commands and `Active` flags, so
blitz (`MissionController`) and travel (`OverlandController`) behave exactly as before.

### Commands (tell the bot)

| command | what it does |
|---|---|
| `mission run` | start the loop. Standing at a mission terminal: that terminal is used from now on (saved to `missionterminal.json`). Anywhere else: the saved terminal. Started inside a mission building: finishes that mission first, or walks out if there is nothing left to do. |
| `mission run status` | the step it is on, missions done, the mission in hand |
| `mission run skip` | delete the mission in hand, walk out if inside, carry on rolling. With no run going it only deletes. |
| `mission run new` | start without resuming a held mission |
| `mission run stop` / `stop` | the only thing that ends the run |
| `mission roll` / `mission list` / `mission accept n` | roll by hand at the terminal, show the list, accept one |

### Config (`config.json`, read at start-up)

`MissionDifficulty` (terminal value, captures show 1/6/11, default 6); the six sliders `MissionSliderGoodBad`,
`...OrderChaos`, `...OpenHidden`, `...PhysicalMystical`, `...HeadonStealth`, `...CreditsXp` as the wire values
-100..+100 (0 = middle = terminal default; -100 = left end, e.g. all credits); `MissionZones` = zone names or ids
missions may be taken in (empty = any zone).

### The loop (`AOBuddy/MissionRun.cs`, `AOBuddy/MissionRoll.cs`)

1. **Roll** (`MissionRoll`): Use the terminal once, then `QuestAlternative` with difficulty + all six sliders on
   every roll. In a team he rolls Scope Team at a team terminal, alone Scope Solo at a solo terminal (a terminal is a
   team terminal when its name says 'Team'); `mission run` switches to the terminal of the right kind standing by the
   saved one when his team status changes, and says so and waits when there is none. The bot's serializer reproduces the owner's client bytes (capture 20260923-201746).
   The answer's 5 `MissionInfo`s carry type (`MissionIcon` 0x2C47/49/4E/41/42), destination playfield, door
   x/y/z, credits, XP, reward item. It takes the first mission blitz can do (find person / find item / repair)
   in an allowed zone, the cheapest trip to its door first (`Zoning.FindRoute` cost, the route `Zoning.Waypoints`
   gives travel, with travel's options; a door with no route is skipped); rerolls otherwise.
2. **Accept**: `CreateQuest(mission id)`. Saved to `missionrun.json` (type, zone, door, reward ids).
3. **Travel to the door**: `travelto x z pf` through `OverlandController.Command`.
4. **Enter**: walking in is all it takes (capture: no client message, the server moves you). The door's
   position comes from `Zoning.json` `missionEntrances`, but not which way it faces, so it tries each side:
   travel to a spot 5 m out, then walk straight through the centre to the far side; next side if not in.
5. **Judge at the entrance**: `mission route`. No walkable path to the target, or no way to find it: drop it
   right there (delete + walk out + roll again).
6. **Blitz**: `mission blitz`. One retry if it stops short, then the mission is dropped.
7. **Walk out**: blitz's own exit; if that stops short, `backoutside` again.
8. **Stash**: open each backpack (`GenericCmd Use` Flag 0 on the bag; the server answers `InventoryUpdate`, the
   SDK's `Container`), move that mission's reward item in with `ClientContainerAddItem`, skip full bags.
   Only the reward item is ever moved. Proven live 21:28.
9. **Back to the terminal**: travel, then the last metres on foot (the terminal's body keeps you ~5 m out).

### Recovery (the run never stops itself)

- **Stopped short inside a building** (blitz or walking out): turn round and run 6 m back, retry; after that,
  walk the bot's own clean trail back 15 m (further each time, max 60) - the trail is only positions the server
  accepted (no correction in the last 2 s) - and retry.
- **Blitz fails twice / door fails from every side / can't reach the door / 20 min in a mission / outside but
  not complete**: `QuestMessage Delete` on the held mission (copied from capture 20260910-200346 client seq 54;
  only quests whose quest-log entry names a mission terminal, read from the raw `QuestFullUpdate` because the
  SDK's decode came back empty live), walk out, roll another.
- **Death**: after the reclaim, travel to the terminal, wait out rez sickness + 8 s of rebuffs there, then back
  to the open mission (still in the quest log) or roll.
- **Travel gives up or waits on Scotty > 15 s**: take the zone route from `Zoning.FindRoute` without Scotty and
  walk straight to its first exit (step on / Use it), then hand back to travel in the next zone.
- **Resume after a restart**: the quest log carries each mission's destination the same way the terminal list
  does (`Identity(0x9C50, pf)`, 8 bytes, x/y/z floats - checked on capture 20260923-201746), so `mission run`
  finds the held mission there. `missionrun.json` only adds type and reward ids when it matches.

### Also changed tonight (outside the run)

- `Main.cs`: the movement leash anchor is dropped on every position jump. A correction taken inside a mission
  building leashed the first steps outside back to mission coordinates and walked the bot to (21,260) in
  Borealis (21:11). Tells from anyone but the owner are now logged (`TELL (not obeyed) from ...`) so Scotty's
  answers show.
- `MissionController.cs` (the repair step, our original code): the tool is found in the bags by the identity the
  quest record names or by its name in the mission text; it used to look among items lying in the building.
  When a mission's record has no target, the raw quest update is saved to `missions/questupdate-b*.bin` and the
  items in sight are logged.

### Night fixes (2026-09-23, after the list below was written)

- 3 + 4 (find item): the target was a CONTAINER (quest record target 0xC74E:FF866). The record parser only took
  0xC73D/0xC350 identities, and the SDK dropped every ChestFullUpdate (its reader throws). Now: containers are
  passed on raw (`Client.ChestFullUpdateRaw`), blitz reads identity (offset 20) and position (floats at 41) into
  its item table, and the parser accepts 0xC749/0xC74E targets. Not a floor problem: that building is 1 floor.
- 6 (Scotty): `NameToIdMap` was case-sensitive; a tell to "scty" waited forever on the server's "Scty". Fixed.
- 7 (HP): HealthDamage's HP belongs to the message Identity (the receiver); the field called Target is the source
  (capture 20260910-200346: one healer, three receivers, three HPs). The SDK and VitalsTracker wrote it into the
  healer: every stim the bot gave the owner set its own HP to his. Fixed in both.
- 1 (exit): the owner's client stops ON the door's spot and is moved out 0.2 s later (entry the same, 0.4 s). The
  run now walks onto the exit door blitz names and stands still before backing off; entry does the same.
- 5 (ICC): the whompa sits in a pocket the 4 m grid seals; ground around the reclaim connects for 60 m. The run's
  walk-to-exit fallback now routes on the grid to the nearest reachable ground, then straight.
- 2 (snap-backs): no wall in the data at the Midtech spot; it is a room boundary. Likely a door that opens as you
  come near (the client never Uses doors: 0 of 61 Uses in all captures) being run into before it opens.

### Open problems for Algorithman (in blitz / travel, not touched)

1. **Exit push**: "walked 8 m through the exit door at (300,75) / (0,185) / (300,265) and did not leave the
   building" several times. Same as the entry door: one push direction. The entry side-by-side approach (step 4)
   might carry over.
2. **Server snaps back mid-route**: e.g. Midtech repair 2224329, snapped to (5,5,185) every try just east of the
   landing (2,5,185), cells 3,92 / 4,92 blocked, then "no walkable path". Another building: snapped to
   (258,5,101) repeatedly on the way out, 58 m from the exit. Zone-ins are saved in `missions/`.
3. **Find item on another floor**: Omnilab 2224336 composed as 1 floor; the owner says the item is on the top or
   bottom floor. The bot searched every room of floor 0.
4. **Quest record says "not the holder"** for a find-item mission the bot rolled itself, so no target identity;
   raw quest update is now saved for the next one.
5. **ICC (pf 655) grid, 4 m cells**: the reclaim spot (3231,35,915) and the whompa to Newland (3173,866) both
   "walled off: no open ground"; the Grid proxy (3179,881) too. Travel then falls back to Scotty.
6. ~~Scotty has never warped this bot~~ Resolved (Algorithman, 2026-09-24): Scotty answers; the warp comes ~20 s after the tell. The run now gives it 40 s before walking.
   **But RubiKa2019 has no Scotty** (owner, 2026-09-24; the owner's bot logs in there, `Client.Dimension`). That is
   why Scotty never answered him. Travel should plan without Scotty there:
   `UseScotty = Client.Dimension != Dimension.RubiKa2019` in `OverlandController.Options`. The run already walks
   at once on 2019 when travel falls back to Scotty.
7. **The bot's own HP reading** (`SupportController` predicted HP) sat at 51% while the owner saw full HP.
8. **Snares read as "unreadable"** (`BotContext.RunVelocity`): a mob's run-speed debuff put Stat 156 at -289
   (23:00:47, also -286/-153/-131 in earlier fights). `rs >= 0` drops every negative value and keeps the last
   good 141, so the walker ran at ~6.1 u/s and the server snapped him back ~17 m every 3 s for minutes
   (Borealis, 615,467 -> 644,475), with overland "routing round" a wall that wasn't there. Only -1 means
   unreadable; a negative skill probably slows him (5.5 + rs/230 gives 4.2 u/s, but the server let him
   advance slower than that, so the formula for negative values is unverified). **Changed in your
   `RunVelocity` with the owner's OK:** any reading but -1 counts, speed floored at 1.5 u/s. A snared char
   still moves, just slower (owner, 23:10); the snare timer (~3 min) doesn't run while logged off.
   Separately, the run stands still 15 s when the server pulls him back >5 m twice in 8 s (roots).
   Also worth making "snapped back to the same spot N times" read as a speed problem, not an obstacle.
9. **ICC Newland whompa: stop ON its centre, at the pad's height.** Owner capture 20260923-234203 (23:43:59):
   his client walked in from (3165.6,35.91,868.0) and stopped at (3173.45,**36.175**,865.94). That is 0.1 m from
   the exit's centre in Zoning.json (3173.47,35.89,866.01), and **0.285 m above** the height the data gives it (the pad's top).
   The server zoned him right after the stop (MoveType 2). There was **no Use**. The bot failed every time it:
   - stood 3.5 m off the centre (travel's `ArriveWalk` "on the pad" at (3170,866), 23:17 and 23:37);
   - ran across the centre without stopping (the hike, 23:36);
   - Used the Door object.
   The trips that did work tonight were follow copying the owner's own packets. The run's hike now walks onto the
   centre and stops (4 tries, alternating the data's height and +0.285), and logs which one takes him.
   **Confirmed live 2026-09-24 00:14:** at 0.3 m from the centre and height 35.74 (our data's height), nothing
   happened for 12 s. At 0.2 m and height 36.05 (the pad's top), he zoned 0.6 s later. **The height decides it.**
   `ArriveWalk` needs the same: stop within ~0.5 m of the centre, **on the pad's top surface**. Our data's height
   (35.89) is the ground under the pad; the pad here is 0.285 m higher. The Newland -> Borealis whompa worked
   through travel on the same run.
   The owner's route: reclaim -> the Newland whompa at the back of the whompa row -> in Newland turn right
   into the Borealis whompa. The Grid needs Computer Literacy, and some Grid exits are over the bot's skill,
   so the whompa is the route of choice.

10. **Coarse grid "walled off" at small goals** (2026-09-24). On big zones the grid gets 4 m cells (MaxCells cap),
    and `Search` only looks `reach` (3 m) round a blocked goal: the ICC terminal (3233,921) and the whompa pockets
    come out "walled off: no open ground within 3 m" though the bot had just walked there. Suggest: when the goal
    cell is blocked, search a ring of ~1.5 cells (not a fixed 3 m), or plan the last 20-30 m on a fine local grid.
    The run works round it by walking itself on the hike grid (rings out to 24 m, then straight).
11. **"searched 1500000 cells without reaching it"** (Lush Fields 2825,2856, 2048x2048 cells of 2 m). `MaxExpand`
    can't tell "no way" from "a long way". A flood fill from the goal when it gives up would say which (the goal's
    region size, and whether it joins the start's) - then the cap can be raised only where it helps.
12. **Travel detours through a zone it can't leave: Galway Shire (687) on RubiKa2019** (09:46-10:45, 2026-09-24).
    In Galway County (1414,1086) travel planned County -> Shire at (1037,1023) -> back to County at (1000,1045).
    The first crossing worked (arrived at 996,16,1045). After that, the server pulled him back at every exit:
    - the County border at x ~989-1003 (19 segments, both travel and the hike, 15 m snap-backs to the same spot);
    - short of the Rome Blue border at x ~308-323, while the line is at x=159.
    Inside the Shire the server's ground is up to 10 m above our heightmap: (919,1055) server 37 vs our 27,
    (929,1054) 33 vs 25. The Shire likely has walls or buildings that aren't in our terrain/walls data, or its
    RK2019 layout differs. The run was stopped and the owner walks him out.
    Suggest for travel: a playfield avoid list (`Options().Filter`), with Galway Shire on it for RK2019. Also, a
    detour through another zone that ends back in the same zone should cost more than walking round.
13. **Find person that never completes** (Lush Fields, building 2186128, 09:05, 2026-09-24). The quest record's
    target (Eldridge Vallegos, SimpleChar:EA3FAD5) was found at 2.7 m and selected six times, and no
    MissionChanged came. The same mission type completed before. One difference: the target was selected at
    09:04:48, the same moment a fight started. Not guessed further; a capture of a manual find-person hand-in
    would settle it.
14. **Mutant Domain packs on travel routes** (09:33 and 09:39): Hammer Broodlings (26-29) and Minibulls (30) in
    groups of 4 at (550-620, 1100-1170) killed the level 36 bot twice on the way to one mission door. The run now
    flees (outside missions, under 40% HP and still being hit) and leaves a zone alone for an hour after a death
    there. Travel routing round known mob camps would be better.

15. **Water: the walker runs at run speed while swimming** (the owner's 220 main, Deep Artery Valley, 13:56-14:00,
    2026-09-24). Run skill 1912 -> 13.8 u/s, but the server moved him ~5 u/s (1346 -> 1426 -> 1514 over ~15 s
    each) and snapped him back 30-38 m every ~10 s. He held a constant y=9 while our ground there is 2.7-6.3
    (1346,695 / 1426,685 / 1514,674): a river surface. On land (y 21-25) his height matched the ground. The
    grid has no water, so routes go down rivers. Suggest: water extents from the client data (or 'server keeps
    y flat above ground' as a live signal) -> swim speed while in water, and a cost for water in the grid.
16. **Scotty inside one zone never warps** (the main, Deep Artery Valley, 'tell scty Ljotur', 2 tries x3, 13:45-14:00).
    Travel keeps planning it first. A same-zone Scotty leg that failed once could be dropped for the trip.
17. **Fair Trade bag purchase**: ResupplyController's container kind visited 16 terminals in Fair Trade and none
    offered 'Large Backpack' (13:24:43-13:25:19, 2026-09-24). Which terminal sold it in the owner's capture is
    not in our notes - to be checked against capture 20260924 before changing anything.

18. **The Longest Road deaths were FALLS off the raised road, not mobs** (owner: nothing there attacks him unless
    he attacks). 12:33: height 38 -> 34 -> 28 -> 22 -> 16 in 3 s, a Bloodcreeper had taken him only to 96%.
    13:52: combat=False the whole time, walking at y=37 over ground at 15 (4390,1590), dead a second later.
    12:14: 100% -> 47% in one second with no combat, then dead. The hike walks the grid's route as straight
    legs; the grid knows ground and walls but not a road deck above the valley floor, so a leg that cuts a bend
    walks off the edge. Needs the walkable floor surfaces (collision data) - the navmesh case.
19. **Raised mission doors** (the main, Omni-1 HQ, 14:27-14:32): door at y 23-24, the server held him at 17 - the
    way up (stairs/ramp) isn't in our walking; 16 door-side tries, then dropped. Same need as 18.

### Run changes, 2026-09-24 (ours, `MissionRun.cs`)
- The hike walks **every** zone crossing (not only the first) and skips the Grid; in-zone teleporters count as crossed.
- Zone lines are crossed 10 m past, alternating sides every 12 s. Pulled back at a line = that line failed.
  Two failed segments of a border close the whole border (session only).
- "Walk to it myself" when travel finds no way to a goal under 120 m (hike grid, then straight).
- Flee from a losing fight outside missions. A zone is left alone for `dangermins` after a death there, an
  unreachable door, or a skip.
- `mission run tune <name> <value>` (tune.json): 26 distances/waits of our walking, logged with each crossing.

### Later (owner, 2026-09-23)
- **Out of room (done):** he always needs **4 free inventory slots** (not bags, not items) to pull mission keys
  and rewards. The run stops and tells the owner when fewer are free at the terminal, after the stash has
  filled what bags it could.
- **Shops (later, if Algorithman's shop code works for this):** configure a shop area. Shops sell different
  things but almost always containers.
  - **Room:** with plenty of credits, buy the cheapest container and carry on. The 4 free slots still hold.
  - **Selling:** to start with, sell every reward that isn't a nano crystal. Later, a keep-list of items that are
    never sold.
- **Evidence already captured:** 20260923-234203, with marks from 23:46:12 to 23:49:18. The owner walks into
  Fair Trade, buys the cheapest bag at the container terminal, and opens the bank. He then puts an item in and
  takes it out, and uses his way of filling the bank with nanos: take the bag out, fill it in the inventory, and
  put it back. That is Fair Trade stream s8, decoded in order to `sniffs/work/decoded/20260923-234203_s8_shop.txt`.
  It is not analysed yet. First look:
  - the purchase is a Use on the VendingMachine, then ShopUpdate, then Trade messages with a TempBag, and
    ClientContainerAddItem into the IncomingTradeWindow;
  - bank moves appear as `Bank:0` containers, and a bag comes out of the bank to inventory slot 111.
- **Housekeeping design (owner + Algorithman OK, 2026-09-24).** On `mission run`, and whenever the stash
  finds no room:
  1. Are his bags full? What are they full of?
  2. Sell everything that is not a nano crystal and not on the keep list (`KeepItems`, to be written).
  3. If he holds nano crystals and has no bag in the bank with room: buy the cheapest bag, keeping a credit
     reserve for more missions. Fill it with the nanos and put it in the bank.
  4. If he still needs room to keep running, buy one more bag.
- **Wire evidence for it (capture 20260923-234203, Fair Trade stream s8, marks 23:46-23:49):**
  - Fair Trade = playfield **1187 "Neutral Supermarket Advanced"**. It is entered by the proxy 51016:-1072299232
    at (650.25,68.49,612.86) in Borealis, which is in Zoning.json, so travel routes there. The exit puts you at
    about (660.6,72.8,559.9) in Borealis, next to the mission terminal.
  - **Buy:** LookAt the VendingMachine (42685979, the container terminal at (199,5,129)), then GenericCmd Use on
    it. The server answers with ShopUpdate (62 lines) and Trade Open with a TempBag. The client sends
    `Trade AddItem(machine, container 0x6F:1)` (stock line 1, the cheapest bag), then `Trade End` (target None).
    The server replies with InventoryUpdate: the new bag 51017:27906825 in slot 112 (Open=0), plus
    ChestItemFullUpdate and Trade op 4. This is the same protocol ResupplyController uses.
  - **Open the bank:** GenericCmd Use on the bank terminal **C73D:0EE5BBFF**. The server answers
    `BankMessage Contents=[...]` (his was empty).
  - **Item into the bank:** `ClientContainerAddItem Container=0xDEAD:<own char id>` (IncomingTradeWindow), with
    `Item=Inventory:<slot>`. The server confirms with ContainerAddItem Inventory:slot -> IncomingTradeWindow.
  - **Out of the bank:** `MoveItem Source=Bank:<bank slot> Destination=111`. The server confirms with
    ContainerAddItem Bank:n -> char, placement 111.
  - **Nano into a bag:** GenericCmd Use on the bag's inventory slot (Inventory:69) to open it, then
    `ClientContainerAddItem Container=<bag identity 51017:x> Item=Inventory:<nano slot>`. The server confirms
    with ContainerAddItem and ActionMessage 102.
  - **Bag back into the bank:** GenericCmd Use on the bag identity, then
    `ClientContainerAddItem Container=0xDEAD:<me> Item=Inventory:<bag slot>`.
  - **Selling: NOT captured yet.** Needs one capture of selling an item at a shop terminal.
- **Built as a test switch (2026-09-24, `MissionShop`, off by default; `mission run shop on|off|now`).** When the
  stash finds no bag with room, or fewer than 4 slots are free before a roll, he travels to Fair Trade (1187)
  and stands at the owner's spot. He opens the bank and puts his nano crystals into a bank bag: he takes it
  out, opens it, moves the nanos in until one is refused, and puts it back. If no bank bag has room he buys a
  Large Backpack for them, keeping `MissionCashReserve` (20,000). If he is still under 4 free slots he buys one
  more bag to carry. He leaves the way he came in: back to where he landed and on through it (owner).
  - Selling is not done (not captured).
  - **For Algorithman:** `ResupplyController` got a `Container` supply kind and `StartContainers(me, n, reply)`.
    It matches by exact name `ResupplyContainerName`, "Large Backpack" (template 143832, the owner's buy), and
    takes the cheapest line with no skill check. The machine ranking prefers names containing "Container".
    Your buy sends `Trade Accept` with the **machine** as target; the owner's client sent Accept (0x01) with
    **Target None** (capture seq 16). Unchanged; the first live bag purchase will show whether it matters.
  - Unverified: the bank terminal id C73D:0EE5BBFF is used as captured, since the server never sends it as a
    dynel. Also unverified: the Temp4 flag on the Use of the bag before banking it.
  - Our zoning data has no exit from 1187.
  - **There is one Fair Trade (Algorithman): playfield 1187**, confirmed by the owner's zone-in
    `N3Teleport Playfield=51102:1187`. Each city's door leads to it on a different game server. Doors in Zoning.json:
    Borealis (650,613), Newland City (296,323), Newland Desert (2211,1567), Stret West Bank (1115,2764),
    Pleasant Meadows (1150,2356), Mort (2830,1892). The run travels to pf 1187 itself, so the planner picks the
    cheapest door; the spots inside are the same. Unverified: whether the bank terminal's id (C73D:0EE5BBFF) is
    the same on every game server.
- **Selling (capture 20260924-074329, owner, 07:44-07:45):** LookAt + GenericCmd Use on the shop terminal. The
  server sends ShopUpdate + Trade open. The client sends `Trade AddItem` with **Target = own char (0xC350:me)** and
  **Container = Inventory:<slot>**, one per item (two in one trade), then `Trade Accept (0x01)` with **Target None**.
  The server pays (Stat Cash) and closes the window (Trade op 4). The next batch must Use the terminal again. An item
  can't be sold from a backpack; it is moved to the inventory first, a few at a time (owner). Built: the SDK's
  `MoveItemToInventory(Backpack:(handle<<16|slot))`, unverified live.
  **Owner's sell rules:** sell everything in the inventory and bags except: bags; nano crystals (banked); stims
  and rechargers; ammo ("Ammo: Box of ..." in the item data) when ranged, since a melee loadout sells it; anything
  in a bag marked personal; the keep list (exact names: config KeepItems + `mission run keep add <name>`,
  keepitems.json). Also kept for safety: names containing "key" or "mission". Equipped items are never
  considered. Commands: `mission run shop list|bags|personal <n>`, `mission run keep`.
- **Bank (later):** learn to check his bank. Nano crystals are always kept: buy containers, put them in the
  bank, and fill them with nanos.

10. **`travelto` gives up where the run would back off** (owner, 2026-09-24 07:05): from (3191,886) in ICC toward
    the Newland whompa, 'no progress ... routing round it' 4 times at (3190,885), then "stuck ... something is in
    the way". Same spot where the server pulled the run back at 07:02. The mission run backs off, walks its clean
    trail back and replans; plain travel could do the same before giving up (owner: "backtrack to the last
    known good position and try again").

11. **Stret West Bank (790): the grid seals the inner town** (probe 2026-09-24, `docs/img/stret790_town.png`:
    dark = blocked, green = reachable from the Borealis whompa at (1276,2884), pink = open but unreachable,
    blue = the bot's own walked segments from `nav/790.json`). The town is two walled octagons with a ring road
    between. The ring road and the country outside connect, but the **whole inner octagon is cut off**. The bot has
    walked through the inner wall on the NE side, where the grid has no gap: an arch narrower than a 2 m cell,
    or its overhead stone stamped as wall (StampWalls blocks any cell with geometry at body height). Ideas:
    open every cell on a walked segment (walked = walkable), and/or skip wall samples with ground clearance
    above the body.
    Separately, the door at (1047,2704) that live travel "searched 1500000 cells without reaching": on the plain
    grid, FindPath from (1283,2891) finds it (26 points, snap 8). So the live failure came from travel's own
    state (extra blocked cells or its snap), not the map. Probe: scratchpad navprobe (OverlandGrid.Build + IsOpen).

20. **Static objects in an instanced zone have live ids (bank fixed, 2026-09-24).** Capture 20260924-192208 (the alt,
    Borealis and Newland Fair Trade): the zone-in PlayfieldAnarchyF (flags C77D) carries a table of
    `{type, start, count, first instance}` records over the zone's object list. Vending machines and doors are also
    sent by the server with those ids, but terminals never are, so the bot used the static id (C00104A3) and the
    server refused every Use (GenericCmd echo Verification 2; accepted = 1). `Playfield.LiveIdentity` maps a static
    id (type index = (id >> 16) & 0x3FFF) through the table: bank C00104A2/3 -> 0EE4CB08 (Borealis FT), 0EE73632
    (Newland FT), both what his client sent. Any other static terminal in an instanced zone needs the same mapping
    (Zoning/whompa uses in instanced zones, if any). Both captured Fair Trades were model 1186, not 1187.

21. **For Algorithman: `backoutside` (GoOutside) clears `_blocked` and `_replans`.** 19:55-20:02 (2026-09-24), ACD
    building: snapped back at (53,5,237) on the walk out; our 15 s 'held' pause restarted the walk with backoutside
    each time, which wiped the blocked cells, so the same 69 m route was planned 18 times and the 12-re-plan give-up
    never came. MissionRun now holds only once per spot in a mission and leaves repeats to the blitz. Keeping the
    blocks across a GoOutside of the same instance would make that safe from other callers too.

22. **For Algorithman: Holes in the Wall detour.** 20:32 (2026-09-24): from (197,623) to a mission door at (267,434),
    about 200 m straight, the grid route was 26 points and 3161 m; it took 9 minutes. The first straight try was pulled
    back 10.5 m at (198,47,619), so there is a real obstacle, but a 15x detour points at the 2 m grid closing a gap
    (see finding 10).

## Want list: rolling for items (owner, 2026-09-25)

**Goal.** Roll missions for chosen rewards: only implants, only gear, only nanos, only one profession's nanos, a QL
band ("engineer nanos QL 20-30"), or a list of exact item names. Either a standing filter or a list to finish:
when everything on it is collected (or found unrollable) he says **done** and stops; `mission run` resumes normal
missions.

**Decisions (owner).** Learned nanos do NOT count as had (we roll for other classes); only items held (inventory,
bags, bank) or recorded as got count. One want list per bot. At the end: say done and stop. The list can be changed
while he runs (commands, or editing wants.json - re-read before every roll).

**Data, verified 2026-09-25:**
- Every roll sends difficulty + all six sliders (MissionRoll.cs, QuestAlternative) - the QL lever is ours.
- Offered rewards carry LowId/HighId/Ql (MissionItemReward) - exact template matching, no names.
- Gear/implant rewards in one roll all share one QL, the mission QL, set by level and difficulty (alt log: diff 1 ->
  QL25, diff 5 -> QL32-35 at level 36-39, diff 6 -> QL36).
- Nano crystal rewards come from a window around the mission QL, about +-9 (mission QL25 -> nanos 16-34, QL32 ->
  24-42, QL35 -> 27-44).
- A crystal names its nano in its use event: 'Nano Crystal (Shatter Bone)' 82011 -> event 0, function 53019
  (Upload), arg 82007 = the nano program (items.ocp, extractor --probe). The nano program carries its professions
  and QL (NanoCatalog). So "engineer nanos QL20-30" expands to exact crystal templates from data.

**Design.**
1. wants.json per bot: ordered entries - exact name, or a query {kind: nano|implant|weapon|armor|any, profession,
   qlMin, qlMax}; mode always|list.
2. At load each query expands to concrete templates (extractor table: crystal -> nano -> professions, QL; item
   class for gear/implants). `want status` = collected / remaining / unrollable.
3. Roll: keep only missions whose reward template is still wanted; among them the cheapest trip. None: roll again.
4. QL targeting: record the reward QLs seen per difficulty setting; move difficulty to centre the mission QL on the
   wanted band (nanos: +-9 window). Band out of reach at the slider limits, or not seen in N rolls: report it
   unrollable and drop it.
5. Wanted items are always kept and banked (never sold); the keep/bank files fold into one rules file later.
6. Commands: `mission run want add <name|query>`, `want remove`, `want list`, `want mode always|list`,
   `want status`, and `mission run want` to start a want run.

**Built 2026-09-25** (steps 1-4): WantData (ItemWantData.bin), WantList (wants.json), 'mission run want ...' commands
and the want run, QL targeting (qlmap.json: level:difficulty -> mission QL; difficulty 1-11 searched toward the
wanted band; a band no setting reaches is reported and dropped). Not yet tried live.

