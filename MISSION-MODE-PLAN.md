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
| Roll request | the client sends `QuestAlternativeMessage` (same id 0x5C436609 as the reply): VersionId=4, Difficulty (byte, 6/9/11 seen), the six sliders (0 here), Seed=0, Originator=NeutralBooth, MissionTerminalIdentity=56001:<terminal instance>. The server answers with the same message type holding 5 QuestInfos. Every roll is one such message; the terminal is `GenericCmd Use`d once before the first roll, not before each roll. | `20260910-200346_s2.txt` client seq 37 (Use terminal 56001:-1073741169), 38/41/46/48 (four rolls), server seq 2514/5356/8951/11472 (the lists) |
| Accept | `CreateQuestMessage QuestIdentity=Quest:<id from the list>`; the server replies `QuestFullUpdateMessage AnnounceAsNew=1` with a NEW quest identity (list id ...C9CB became ...C9D0). Delete is `QuestMessage Version=1 QuestIdentity=<new id>`. | same stream, client seq 50 and 54, server seq 11850 |
| Repair objective | `GenericCmdMessage Action=UseItemOnItem Target=[2]`: first the inventory item (type 0x68, slot), then the object (type 0xC73D). Server echoes it with Verification=1, then Cash stat, reward item to the overflow window (`TemplateAction 87` + `ContainerAddItem`), `FeedbackMessage MessageId=108871108`, `CharacterAction MissionChanged`, `QuestMessage` (mission removed). | `20260910-203534_s37.txt` client seq 1292, server seq 19742-19749 |
| Mission zone-in | `PlayfieldAnarchyF` version 4 with BuildingGeneratorData, then `QuestFullUpdateMessage` with the mission text (objective name is in the text: "use the Targeted Radiation Extractor to heal the Fire and Radiation Chamber"). | same stream, server seq 2 and 85 |
| Player-to-player trade | `TradeMessage Version=2`: open = Action=None Target=<other>, credits = Action=7 Target=None:<amount>, accept = Action=3, confirm = Action=End; the other side accepts with Action=3 then End; server closes with Action=4 (Unknown) to both. `AOSharp.Clientless/Trade.cs` exists. | `20260911-061635_s6.txt` / `_s8.txt` (both accounts) |
| Floor buttons | items of type 0xC73D, recognised by template id in their `SimpleItemFullUpdate` (stats 701-703): 159863 `Button (down)`, 159869 `Button (up)`, 159864 `Button (boss)` (straight to the boss room). 159862 and 159865-159868 are `Platform 1`-`Platform 5`, standing on the same spots; not used. Riding: client `GenericCmd Use` on the button, server echoes it, sends `CharacterAction 170 p1=54 p2=10` (stat 54 = Level locked for 10 s) and `N3TeleportMessage` in the same playfield within ~0.3 s. The destination is the position of the paired button on the other floor. `CharacterAction 164 p2=54` arrives 10 s later: that is "Level skill available"; the bot waits for it before the next Use. Same pair 170/164 locks and releases every skill (p1=142 brawl 15 s, 123 first aid 40 s). | `20260923-114223_s4.tsv` / `_s5.tsv` (timed, both accounts): four rides each, e.g. Use 11:45:08.161, lock 11:45:08.270, teleport 11:45:08.321, release 11:45:18.380. The 0xC76A objects in the 2026-09-19 notes are corpses, not buttons. |
| Find person objective | target the named NPC: client `CharacterAction InfoRequest` (0x69) plus `LookAt` on its identity. The mission completed 0.47 s later with no attack by that account. The name is in the mission text (`QuestFullUpdate`: "...Malik Cratty is helping mutants...") and on the NPC's `SimpleCharFullUpdate`. Completion is the same sequence as Repair: reward items to the overflow window (`TemplateAction 87` + `ContainerAddItem`), `FeedbackMessage 108871108`, `CharacterAction MissionChanged` (mission holder only), `QuestMessage`. The teammate got the reward and the removal too. | `20260923-114223_s5.tsv` 11:50:35.558 target, 11:50:36.036 completion; `_s4.tsv` 11:50:35.787 teammate |

## What is still not known, and how each gets known

| unknown | how to settle it |
|---|---|
| Picking an item off the floor (Find item / Return item) | no capture has one. `GenericCmdAction.Get=1` is the likely wire form. One capture of a Find-item mission: pick the item up, mark it. |
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

Follow, combat, stand-up, move speed, use-travel and the zone sweep (CLAUDE.md "VERIFIED
WORKING" and "KNOWN-GOOD BASE"). Mission mode adds a movement source to the arbiter in
`Main.Walk()` below cast/rest and beside travel; it never sends StopAttack, never applies SetPos,
and hands control back to follow the moment the owner turns it off or is lost.
