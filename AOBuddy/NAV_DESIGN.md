# AOBuddy — Persistent Navigation (per-playfield walkable memory)

Status: **FIRST CUT (2026-09-20) — recording + minimal "try to use". Testing, expect to iterate.**
Owner design conversation, agreed shape. Grounded in client data (`E:\Funcom\Anarchy Online\cd_image`)
and the running bot's `aobuddy.log`. See `CLAUDE.md` for the hard rules; OmniCell is REFERENCE ONLY.

## The problem this solves

The bot follows by retracing the owner's live breadcrumbs. That works because every breadcrumb carries
the owner's **server-correct Y** (his client computed the ground height). The bot rubberbands only when it
**deviates** onto ground it never recorded (the forward-sweep when it loses sight, corner-cutting) — it is
clientless, so it has **no collision/heightmap loaded** and cannot compute Y itself, so it guesses wrong and
the server snaps it back. That is the ramp problem in one line: *deviation onto un-recorded ground.*

So: **remember the ground we've already walked, per playfield, and reuse it** — never guess where we've been
before, and when lost, fall back to known walkable routes instead of flailing into a wall.

## Hard principle — wall-safety by construction

We have **no collision data** (statel footprints need a mesh decoder we don't have; the Wall record is only
the zone boundary). So neither we nor the code can verify an arbitrary line clears a building. Therefore:

- **The graph contains ONLY edges the owner actually walked.** A recorded trace cannot pass through a wall
  because the owner didn't. Dense points, consecutive = a real step.
- **No synthetic shortcuts.** Never connect two points across space the owner didn't traverse. No aggressive
  straightening that cuts a corner.
- Teleports (grid/whompa/door/zone line) are stored as **transitions**, never as a walkable straight line.

Consequence, stated plainly: **the bot travels only ground the owner has personally walked** (stitched over
time). "Map it all" = walk the corridors the bot will use, once, across as many sessions as it takes.

## What gets recorded (the truth source)

We record the **OWNER's** track, not the bot's — the owner doesn't break, so no flailing is ever written.
Recording is **paused** the moment following isn't clean, so the break / stop / go-retrieve-him episode never
touches the file:

Recording is ON only while: mode = Assist, follow on, owner visible, **and** NOT (in combat / resting /
zone-sweeping / riding travel). Points are thinned to a clean line (`NavPointSpacing`, not every 0.3 m wiggle).

A **segment** is one continuous clean-follow run (consecutive points = real walked edges = wall-safe). A new
segment starts on: playfield change, a big gap in the owner's track (blink/teleport), or recording pausing.

## File format — one JSON per playfield: `nav/<playfieldId>.json`

Keyed by `AOSharp.Clientless.Playfield.ModelId` (updates on every zone-in). Borealis = **800**.

```json
{
  "playfield": 800,
  "name": "Borealis",
  "updated": "2026-09-20T14:00:00",
  "segments": [ [ [x,y,z], [x,y,z], ... ], ... ],   // each = one walked run, ordered
  "transitions": [ { "x":.., "y":.., "z":.., "toPf": 790, "kind": "zone", "name": "" } ]
}
```

- `segments` — the walkable memory. Consecutive points in a segment are wall-safe walked edges.
- `transitions` — where the owner crossed to another playfield (zone line / door / grid). `toPf` = the
  playfield he ended up in. `kind` starts generic ("zone"); mission-entrance tagging comes later.

## How it's used (behaviours)

1. **Don't guess where we've been.** In an area already recorded, prefer replaying the saved run over
   sweeping/guessing — no rubberband on known ground. *(First cut: single-segment replay when lost; full
   in-area preference is a later iteration.)*
2. **Lost → known paths, not a wall.** After the proven zone-sweep attempt concludes and the owner is still
   gone, if the bot is standing **on** a recorded route, it replays that run toward the owner's last-seen
   spot until he's back in range — instead of freaking out. Zone-sweep keeps priority so auto-zoning (which
   is load-bearing and confirmed working) is never disturbed.
3. **Transitions build a cross-playfield map.** Every recorded crossing (from pf, at this spot, → that pf)
   accumulates a whompa/door/zone graph, so eventually he can traverse many playfields to reach a target one.

### Feeds FOLLOW, never moves the body itself

`NavController` only *supplies routes*; the body is still moved exclusively by `FollowController` via the
proven `LoadReplay(points)` walker (small steps, correct run speed, stuck-skip). So nav can never break
follow/combat/travel/zoning — it's an overlay that hands FOLLOW a list of already-walked points.

## The mission goal (where this is headed — NOT in the first cut)

The bot will eventually roll its own in-town missions (terminal slider kept in-zone/solo-able), get a target
location uploaded to its map, pick the **recorded door nearest that coordinate**, path to it over walked
segments, and zone in. If no recorded door is near / reachable → **drop the mission and re-roll** (never
wander to an un-walked door). Doors' positions are extractable from client data (RDB statel record, verified),
so the bot won't have to walk to *find* doors — only to record the walkable route *to* them.

## First-cut scope (today) vs. later

IN NOW: per-playfield recording of clean owner segments + zone transitions → `nav/<pf>.json`; autosave;
`nav` command; minimal lost-fallback single-segment replay.

LATER: junction graph across segments (cross-segment A* pathing, still walked-edges-only); in-area "prefer
saved route" while owner visible; mission-door matching + terminal rolling; heightmap/walkmesh R&D (optional,
would reduce walking — reverse-engineering an unidentified RDB type, never a dependency).

## Config (Config.cs)

- `NavRecord` (true) — master record switch.
- `NavUse` (true) — enable the lost-fallback route replay.
- `NavPointSpacing` (1.5) — clean-line thinning, metres.
- `NavSnapMeters` (15) — bot must be within this of a recorded route to reuse it.
- `NavSegmentBreakMeters` (12) — a gap bigger than this in the owner track splits the segment.
- `NavAutosaveSec` (20) — flush the file this often when dirty.

## Commands

`nav` — status (pf, segments, points, transitions, recording/use flags). `nav save` — flush now.
`nav on` / `nav off` — recording. `nav use` — toggle the lost-fallback.
