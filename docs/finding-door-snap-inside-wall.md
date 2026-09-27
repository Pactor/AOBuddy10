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
