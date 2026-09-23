# Nav - navigation data per playfield

One folder per playfield id, generated from the client's `ResourceDatabase.dat` by
`tools/AONavExtractor` (C#, self-contained: run it against any AO install) or, equivalently,
`tools/navbridge/exportnav.py` plus `navbridge.exe`. Collision surfaces come from the client's
own `n3SurfaceResource_t` reader, hosted from its 32-bit `N3.dll`, over the type-1000013 records.
`index.json` lists every folder's `info.json`. How the formats were found and checked is in
`NAV-CLIENTDATA.md` at the repository root.

Coordinates are AO's: x east, z north, y up, metres. Nothing is transformed.

## info.json

`kind` is `outdoor` (a `ground.bin` heightfield), `dungeon` (a `rooms.json` room list) or
`none` (only collision). `verification`, when present, is the result of casting every point the
owner has walked in that zone (`Build/Plugins/AOBuddy/nav/<pf>.json`) against this data:
`explainedByFloor` matched the floor model within `toleranceM`, `explainedByCollision` matched a
collision triangle instead, `unexplained` matched nothing (with samples).

## ground.bin - outdoor heightfield (RDB 1000009 'CHGA')

    char[4] "AONG"  i32 version=2  i32 samplesX  i32 samplesZ  f32 cell  f32 heightScale  i32 sourceBits
    i32 rawLength  i32 zlibLength  u8[zlibLength]   (zlib of the raw block below)

Raw block, in order (little-endian):

    u16 height[samplesZ][samplesX]     height = value * heightScale, sample at world (x*cell, z*cell)
    u16 tile[samplesZ-1][samplesX-1]   ground tile id per cell - OPEN: passability
    u8  building[samplesZ-1][samplesX-1]  building-map nibble per cell - OPEN: nibble order/meaning

The client stores heights per 64-cell patch (8, 16 or 32 in a few zones) as a modulo-256 integral
image of bytes (`AnarchyGroundData_t::Patch_t::DecompressHeightMap`: cumulative sum down each
column, then along each row). Here they are already summed and placed in the high byte of a
u16, with heightScale divided by 256 to match, so 8-bit and 16-bit zones read the same way.
`sourceBits` says which the zone was; the 16-bit zones (Shadowlands coasts, Neretva, Area X)
have no walked data yet, so their scale is taken from the record and not verified.

Height between samples is bilinear. Verified against walked points in Borealis (800),
Andromeda (655), Rome Park (730), Stret West Bank (790), The Longest Road (795), Newland City
(566), Broken Shores (665) and Greater Tir County (647): median error 0.01 m, 98.5-100% of points
within 1 m.

## rooms.json - dungeon rooms (RDB 1000001 versions 8/9/10 + 1000009 'GNDA')

    { playfield, name, tilemap, cell (2.0), heightScale (0.2), atlas [w, h], rooms: [ ... ] }

Each room:

    index        matches the type-1000013 collision record (playfield << 16 | index)
    name         may be ""
    rot          0..3, 90-degree steps
    rect         [x1, z1, x2, z2] inclusive cell rect in the template atlas
    pos          [x, y, z] world centre; y is the world height of the room's lowest floor cell
    heightBase   the minimum `height` over the room's floor cells (tile != 0)
    tile         rows[z1..z2] of cols[x1..x2]: tile id, 0 = no floor
    height       same shape: template height in heightScale units
    flags3       same shape: sparse edge/door flags - OPEN
    doors        list of [a, b] pairs - OPEN
    polys        [{id, verts, tris}] extra triangle meshes in room-local coordinates - OPEN (decks?)
    objects      [{pos, rot, point, radius}] placements in WORLD coordinates - OPEN (doors/blockers?)

World floor height of the cell under world point (x, z):

    dx, dz = x - pos.x, z - pos.z
    repeat rot times: (dx, dz) = (dz, -dx)         # i.e. rotate by -rot * 90 degrees
    a = floor((x1 + x2 + 1) / 2 + dx / cell)
    b = floor((z1 + z2 + 1) / 2 + dz / cell)
    if tile[b - z1][a - x1] == 0: no floor here
    y = pos.y + (height[b - z1][a - x1] - heightBase) * heightScale

Stairs, ramps between levels, bridges and mezzanines are not in the tiles; they are in
`collision.bin`. Tiles plus collision explained 98.9% of 2,390 walked points in the subway and
97.3% in the Temple of Three Winds.

289 playfields have a room list: the static dungeons, the city buildings and apartments, the
Shadowlands temples and mazes, and the autocontent mission pools (320 Midtech, 321 HiTech,
324 Clan, 322 Cave, 331 tarm, 341 Grey Caves, 346 Omnilab, 351 Subway Ventil, 362 SL ACG,
382 Alien ACG). A mission instance is the server placing rooms from one pool; the placement
arrives in the zone-in packet (`BuildingGeneratorData`: template playfield, slot grid, world
height, then room index, floor, x, z, rotation per room) and is not in this data. Composition,
verified on three live missions: the pool room with its rotation as sent, x origin = x * 10 m,
far z edge = (gridHeight - z) * 10 m, y = pool height + (floor - lowest floor) *
worldHeight (floors can be negative: Grey Caves sends 0, -1, -2); see
NAV-CLIENTDATA.md and AOBuddy/AOBuddyNav.cs LoadMission.

## collision.bin - near-horizontal collision triangles (RDB 1000013)

Only triangles whose normal is within 60 degrees of vertical are kept (|normal.y| > 0.5), so
walls are gone and floors, stairs, ramps, roofs and ceilings remain. Ceilings above a tile floor
are still present: a navmesh build needs a clearance test, this file does not do it for you.

    char[4] "AOCL"  i32 version=1  i32 chunks  i32 rawLength  i32 zlibLength  u8[zlibLength]

Raw block: `chunks` entries of

    i32 instance          the 1000013 record id (playfield << 16 | index)
    i32 teleportDestPf    from the record; 0 when none
    i32 localizerType, i32 localizerInstance
    f32 originX, originY, originZ
    i32 triangles
    i16 [triangles][3][3]  vertex = origin + value / 100   (centimetres)

A record is split into chunks of at most 2048 triangles, each with its own origin.
