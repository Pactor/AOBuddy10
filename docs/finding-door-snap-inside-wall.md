# Finding: mission door-snap puts route legs inside walls

For: Algorithman. Code: `SmoothFine` in `AOBuddy/MissionController.cs` (the block starting at the comment
"if cell touches a doorway, use the center of the doorway to go through", line ~2100 at 7c85d86).
Introduced in commit **c9d27b5** (2026-09-26, "Pathfinding in Missions: If a door is in 2m distance, use the
door's coordinates instead of the cell coordinates").

## Symptom

Mission 2224812 (AutocontentMidtech, find item in Borealis City, 2026-09-27). When the bot walked to room
`militaryot_endblock_one`, the server pinned him at (71.87, 5, 79.94) and pulled him back there 29 times
between 15:13:52 and 15:14:04. The blitz gave up twice with "the server kept stopping me short", and the
mission was skipped.

## Evidence

Log `Build/Plugins/AOBuddy/aobuddy.log`:

```
15:13:46.947 MISSION: Plan -> Walk: clearing (72.2% cleared): room 'militaryot_endblock_one' on floor 0: 51 m, 8 points
15:13:52.445 MISSION: server put me at (72,5,80), 3.4 m from where I thought I was; planning from there.
15:13:52.456 MISSION: Plan -> Walk: clearing (72.2% cleared): room 'militaryot_endblock_one' on floor 0: 43 m, 9 points
15:13:53.890 MISSION: giving up - the server kept stopping me short
15:14:03.748 MISSION: giving up - the server kept stopping me short
```

Recording `missions/records/rec-2224812-20260927-151031.pkt`, bot moves (CharDCMove) and server SetPos:

- From door (85, 80) the bot walked west at z 79.78 to 79.97 (for example (84.66, 79.78), then (75.21, 79.91), then (71.87, 79.94)).
- The server accepted every step up to x 71.87. It refused (71.33, 79.94) and snapped him back to (71.87, 5.0, 79.94).
- From there the server refused every move, including tiny ones in place ((71.84, 79.79)), moves back east ((72.10, 79.53), (72.65, 79.67)) and moves west ((70.78..68.13, 79.x)).

Geometry (walls.bin placed for this mission, zone-in `missions/zonein-pf2224812-20260927-151031.bin`):

- A wall slab 0.4 m thick runs along x at **z 79.8 to 80.2**, from y 5 to 13. It has end caps at x = 72.
- The pin point z 79.94 is inside the slab.
- The mission's doors on that wall, from the recording's door list, are at (65, 5, 80), (75, 5, 80) and (85, 5, 80). These are the doorway centres, in the wall's centre plane.
- The walls grid closes the 0.5 m cells at z 79.5 to 80 along that wall, except at the three doorways.

Offline repro (MissionGrid built from the zone-in, doors from the recording, goal is the room spot of
`militaryot_endblock_one` at (85, 5, 65)):

```
with doors from (84.81,78.29): 8 pts: (84.75,78.25) (85.00,80.00) (65.00,80.00) (65.00,80.00) (67.25,69.25) (69.25,67.25) (80.00,65.00) (85.25,65.25)
with doors from (71.87,79.94): 9 pts: (71.75,79.25) (75.00,80.00) (75.00,80.00) (65.00,80.00) (65.00,80.00) (67.25,69.25) (69.25,67.25) (80.00,65.00) (85.25,65.25)
no doors   from (84.81,78.29): 8 pts: (84.75,78.25) (84.75,80.75) (65.25,80.75) (65.25,79.25) (67.25,69.25) (69.25,67.25) (80.75,65.25) (85.25,65.25)
no doors   from (71.87,79.94): 9 pts: (71.75,79.25) (74.75,79.25) (74.75,80.75) (65.25,80.75) (65.25,79.25) (67.25,69.25) (69.25,67.25) (80.75,65.25) (85.25,65.25)
```

The point counts match the log ("51 m, 8 points" and "43 m, 9 points").

## Root cause

The walls grid routes correctly. It goes north through the door at 85, west along the corridor at z 80.75, and
south through the door at 65. SmoothFine then **replaces** each smoothed corner that lies within 2 m of a door
with the door's centre. Both corners, (84.75, 80.75) and (65.25, 80.75), become door centres on the same wall, so
the leg (85, 80) to (65, 80) runs 20 m **inside the wall slab**. Nobody checks that leg: ClearFine only checked the
cell-centre line, and the closed cells along the wall are never looked at.

The substitution has two more effects:

- It produces duplicate points such as (75,80) (75,80) and (65,80) (65,80). These are zero-length legs; see the DivideByZero at 15:14:03, guarded in 7c85d86.
- Every replan rebuilds the same in-wall leg, so the snap-back handling cannot steer him off it.

## Suggested fix (either)

1. **Drop the snap.** The walls grid already routes through doorways with the body-radius margin (see the "no doors" paths above).
2. **Insert, don't replace.** Keep the fine-cell centres on both sides of a doorway and put the door centre between them, only where the route actually crosses the door's wall. Never replace a corner with the door centre. Never place two doors on the same wall one after another. Skip a door point equal to the previous point.

Either way, a check that the final smoothed legs stay on open fine cells (ClearFine on the substituted points)
would have caught this.

---

## Second case: mission 2224863 (Grey Caves-Mines, 2026-09-27 21:36-21:42)

Pool "ACD Grey Caves-Mines", 13 rooms, exit door (300,35). Zone-in `missions/zonein-pf2224863-20260927-213559.bin`,
recording `missions/records/rec-2224863-20260927-213559.pkt` (13 doors, 0xC748 DoorFullUpdate, all at y 5):
(220,55) (225,50) (225,90) (230,35) (235,70) (240,15) (245,60) (255,10) (260,55) (270,65) (275,40) (290,35) (300,35).

Offline repro as before: `AOBuddyNav.LoadMission` + `MissionGrid.Build` with those doors, AOBuddy.dll built from
6deab19~1 (the build that ran). It reproduces the logged plans exactly, so these are the routes the bot walked:

| log | offline |
|---|---|
| 21:38:14 Small_16 (room 11), 14 m, 5 points | from (220.11,54.80): 14 m, 5 pts |
| 21:38:16 Small_07 (room 10, (225,95)), 69 m, 11 points | from (224.85,47.78): 69 m, 11 pts |
| 21:38:17 Small_07, 71 m, 10 points | from (220.11,54.80): 71 m, 10 pts |
| 21:38:37 Small_07, 66 m, 10 points | from (225.07,54.66): 66 m, 10 pts |
| 21:38:59 Small_07 71 m 12 pts / 21:39:12 exit 103 m 13 pts | from (223.36,50.64) no path; fallback via (225.07,54.66) + 10 / 11 pts |

### The door snap made in-wall legs again (verified)

Every route out of this area has at least one leg that the snap puts through a wall. "Inside" means samples on
closed 0.5 m cells.

- **Doorway (225,50)** (a 1.4 m tunnel through a 1.2 m slab, walls x 224.3 / 225.7, z 49.9 to 51.1). From inside Small_16 (224.85,47.78) to Small_07:
  - with snap: `(224.75,47.75) (225.00,50.00)=door (239.25,54.25) ...`. The leg door to (239.25,54.25) is 15.1 m, 39 samples inside, 0.00 m from the wall at (228.7,51.1). It runs diagonally through the slab east of the doorway.
  - without snap: `(224.75,47.75) (225.25,51.75) (239.25,54.25) ...`. It goes straight north through the tunnel, then turns. Minimum 0.52 m.
  - The recording shows he walked the snapped leg. At 21:38:16 he went north to (224.96,49.41), then ENE at 17 deg (the bearing of door to (239.25,54.25)) through (225.21,49.80), (225.73,49.96), (226.32,50.15) and (226.83,50.31), into the east jamb. That is exactly the owner's "the doorway is straight ahead": the snap moves the turn point into the middle of the slab, so the route leaves the tunnel's axis inside the wall.
- **The same doorway from the other side.** From (244.0,59.0) to Small_16 (the 21:37:51 "31 m, 7 points" plan) the route is `(244.75,58.75) (243.75,57.25) (234.75,51.75) (225.00,50.00)=door`. The last leg is 10.5 m and runs *along* the slab, with 75 samples inside. Without the snap it goes (225.25,51.75), then south, with 0.49 m clearance.
- **Doorway (220,55)** (a tunnel 1.4 m wide and 2 m long, x 219 to 221.1, jambs at z 54.3 / 55.7). From (220.11,54.80) to Small_16:
  - with snap: `(220.25,54.75) (220.00,55.00)=door (223.75,53.75) (225.00,50.00)=door`. The leg door to (223.75,53.75) cuts the SE jamb corner (221.1,54.3) at 0.32 m, and closed cells are 0.00 m away.
  - without snap: `(220.25,54.75) (221.75,54.75) (223.75,53.75) (224.75,51.25)`, at 0.45 / 0.80 m.
  - The walk of 21:38:14 was refused whole: SetPos went back to (220.11,5.0,54.80).
- **Doorway (245,60)**, on every route east:
  - `(239.25..240.75,54.25) (245,60)=door (245,60)=door (235,70)=door` (Small_07) or `... (245,60) (247.75,63.25)` (exit).
  - Legs: 7.4 to 8.5 m with 11 to 15 samples inside, a zero-length leg, and 14.1 m with 35 inside (Small_07) or 4.3 m with 12 inside (exit).
  - Without the snap: `(244.75,58.25) (244.75,61.75) (235.25,68.25)`, at 0.45 m or more.

### What the snap does NOT explain (not verified)

The long pin at (223-225, 51-55) from 21:38:37 on happened on legs the snap does not touch:

- At (225.07,5.2,54.66) (21:38:37-45, 37 SetPos) the server refused every move in every direction: east (225.6,54.64), a 0.15 m step (225.22,54.65), south-east (225.94,53.90) and north (225.56,55.80).
  - The planned first leg is (225.25,54.75) to (239.25,54.25), in open corridor.
  - The nearest wall triangle at body height is 1.25 m away: the leaning timber at x 222-224, z 52-54, from y 5 to 13.3.
- The 6 m back-off to the SSW was accepted, into (223.36,5.0,50.64). By walls.bin that point lies between the slab faces z 50.1 and z 51.1 (0.54 m from each). The grid has 5 open cells there and ReachFrom finds only those 5, so FindPath fails there, and PathFrom walks back to (225.07,54.66) through the slab face. The server refused that walk after about 4.5 m every time until 21:40:31.
- At (225.26,5.0,52.76) (21:41:49) a walk up the ramp to (233.9,7.6,52.7) was snapped back whole.

So walls.bin does not explain these refusals. Either the server collides with something that walls.bin lacks near
x 223-226, z 50-55, or it holds a position after a SetPos in a way that the immediate re-walk keeps triggering.

### Suggested fix (unchanged)

Same as above: drop the door snap, or insert the door point only between the two cells that straddle the doorway,
keeping them. Also run ClearFine on every leg after substitution.

Commit 6deab19 (wall-clearance cost) does not remove any of these legs. From (225,53) its routes still contain:
- (241.25,53.25) to (245,60): 7 samples inside;
- (237.25,65.75) to (235,70): 7 samples inside;
- from Small_16, (225,50) to (242.25,54.75): 17.9 m, 43 samples inside.

Its clearance does improve elsewhere: the corridor leg goes from 0.27-0.36 m to 1.25 m from closed cells, and the
grey_mh4 legs are over 1.75 m from closed cells.

Tools: scratch `m2224863\h` (harness), `doors.py` / `moves.py` (recording), `routes.png`, `routes-zoom-225-50.png`.
