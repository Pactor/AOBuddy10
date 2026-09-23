# Mission mode for AOBuddy - plan (2026-09-23)

Goal, in the owner's words: send him to a mission terminal, have him roll the closest mission
(Borealis itself preferred), tell us the rewards, run it as a blitz - fastest way to the up/down
buttons, fight nothing, reach the top room, target the person or pick up the item - then come back
and hand us the reward as proof.

This file is the hand-off for a new session. Every claim below names its evidence. Rules that
apply throughout are in `AOBuddy/CLAUDE.md`: no guessing, verify on the wire or in `aobuddy.log`,
one file per system, and never touch follow / combat / travel / zone-crossing as a side effect.
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
- Riding a floor button is wire-proven and works today (`TravelController`, CLAUDE.md
  "USE-TRAVEL"): the button is a teleport object, used with `GenericCmd Use` on its identity;
  the server answers with `N3TeleportMessage` in the same playfield.
- The terminal's mission list is `QuestAlternative` (0x5C436609), decoded per mission by
  `ClickSaver2026.Core/Missions/MissionListParser.cs`: type code at 0x2C, destination playfield
  at 0xAC, entrance x/z at 0xB8/0xC0 (offsets from the end of the reward items), difficulty,
  the six sliders, terminal identity, and the reward items. Mission types (ClickSaver
  `MissionTypes.FromCode`): 0x2C4E Repair, 0x2C41 Return item, 0x2C47 Find person,
  0x2C49 Find item, 0x2C42 Kill person.
- Accepting a listed mission is `CreateQuestMessage(QuestIdentity)` (OmniCell messaging,
  `N3MessageType.CreateQuest` 0x291F361B). `GiveQuestToMember` (0x77230927) exists: the owner
  can hand a rolled mission to a team member, which lets everything from step 3 on be tested
  before rolling works.
- Terminal and mission-door positions per playfield are in `AOBuddy/GameData/Zoning.json`
  (Algorithman's extract: statels with teleport functions and ACGEntrance doors).

## What is not known yet, and how each gets known

| unknown | how to settle it |
|---|---|
| The request the client sends when you press roll at a terminal | one capture of the owner rolling twice (`OmniCell\Tools\Capture\capture-marked.bat`, mark `roll`), decode per stream, find the client->server message between the Use of the terminal and the next `QuestAlternative` |
| The "Level skill" lock after a floor button (5-8 s, then "Level skill available") | same capture, ride a button up and down with marks; look for the lock (a `StatMessage`, `CharacterActionMessage` or a feedback text) and the release; the bot times its next Use on the release message, not on a fixed delay |
| Picking an item off the floor (Find item / Return item) | capture the owner picking up a mission item; check what `AOSharp.Clientless` exposes for it before writing anything |
| Using an inventory item on an object (Repair) | capture the owner repairing once; same check |
| Handing an item to the owner (trade) | capture one trade; this is last |
| Where the up/down buttons stand inside a room | they arrive as dynels once he is in range; compare their positions with the pool room's `objects` list (`rooms.json`, 44-byte placements, meaning OPEN) - if they match, the buttons are known before he sees them |

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

Follow, combat, stand-up, move speed, use-travel and the zone sweep (CLAUDE.md "VERIFIED
WORKING" and "KNOWN-GOOD BASE"). Mission mode adds a movement source to the arbiter in
`Main.Walk()` below cast/rest and beside travel; it never sends StopAttack, never applies SetPos,
and hands control back to follow the moment the owner turns it off or is lost.
