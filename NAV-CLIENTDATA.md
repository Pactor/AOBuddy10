# Navigation from client data — what the RDB actually contains

Status 2026-09-23. Everything below was measured from `cd_image/data/db` with
`OmniCell/Tools/AssetDecoder/AssetDecoder.Cli/bin/Release/net10.0/AssetDecoder.Cli.exe`.
Nothing here is inferred from web lore. Open questions are marked OPEN.

## The find: RDB type 1000009 = `AnarchyGroundDataDB_t`

461 records. Keyed by playfield id. 336 of them are real playfields (type 1000001);
294 playfields have NO ground record — those are the indoor ones (subway pf 127,
mission buildings), which are built from Statels + meshes instead. See "Indoor" below.

Each record begins `CHGA`, then u32 size, u32 version, and carries its own schema as a
NUL-separated string table. The field names are literally in the record:

    __class_id__ version name map_width map_height map_modulo tiletexture_count
    heightmap_compressed_data heightmap_small_data tilemap_compressed_data
    buildingmap_compressed_data
    RTexture_t GroundTextureCreator_t flags width height pixelformat bitmap
    creator texture tile_type_data obj

So the container is self-describing — it can be parsed properly rather than guessed at.

### Header fields confirmed by offset
- `u16 @12` map_width, `u16 @14` map_height (cells)
- `f32 @16` = **4.0 on every zone checked** — world units per cell
- `f32 @20` = height scale, 0.2 or 0.4 depending on zone

Measured: pf800 Borealis 250x250 cells x 4 = 1000x1000 units. Borealis's Wall record
(type 1000021) puts the zone at X 76.7-923.4 by Z 76.7-841.1 — inside that box. Consistent.

### Payload = plain zlib, three block families per zone
Streams appear grouped, in schema order: all heightmap blocks, then all tilemap, then all
buildingmap. Block sizes match a patch size P that is 32 or 64 per zone:

| family      | size at P=64 | size at P=32 | meaning              |
|-------------|--------------|--------------|----------------------|
| heightmap   | 4225 = 65^2  | 1089 = 33^2  | 1 byte per sample    |
| tilemap     | 8192 = 64^2*2| 2048 = 32^2*2| u16 per cell         |
| buildingmap | 2048 = 64^2/2| 512 = 32^2/2 | 4 bits per cell      |

Observed: pf800 16/16/16 (P=64), pf730 9/9/9 (P=64), pf540 49/49/49 (P=32),
pf790 192/192/192, pf795 192/192/192, pf505 336/336/336.

## Heightmap encoding — CRACKED

The decompressed bytes are **signed per-row deltas**, not raw heights. Read each row of
P+1 bytes as int8 and prefix-sum along the row.

Proof (pf800, patch 5), mean |difference between horizontally adjacent samples|:

    raw bytes          81.27      <- indistinguishable from noise (~85)
    transposed         81.05
    8x8 swizzle        85.03
    signed row-delta    2.17      <- real terrain
    ...and vertically   1.04

pf730 and pf790/795 decode to 1.9-2.5 vertically the same way. A heightfield this smooth
at 4-unit spacing is terrain; nothing else produces those numbers.

## OPEN: patch grid layout for large zones

pf800 (250x250) gives 16 blocks = 4x4 = ceil(250/64)^2. Correct.
pf730 (160x160) gives 9 = 3x3. Correct.
pf790 (700x920) gives **192**, but a ceil grid predicts 11x15 = 165.
pf795 (1160x500) gives **192**, predicted 19x8 = 152.

So the grid is not a plain ceil() of map_width/map_height — `map_modulo` (a named field we
have not located an offset for yet) almost certainly sets a padded storage stride.
Until this is settled the world-space anchoring is unconfirmed: correlating decoded
heights against the bot's own recorded walk Y in `nav/800.json` only reaches r=+0.46,
which is not good enough to build on.

**Next step is to parse the container by its embedded schema** (string table at the
`AnarchyGroundDataDB_t` marker, then the typed record block that follows it) instead of
reading fixed offsets. That gives map_modulo and the real block indices, and the
correlation test against recorded nav Y becomes the acceptance test:
decoded height must match walked Y within a metre or two, or the decode is wrong.

## Indoor zones are a different problem

No ground record for pf 127 (subway) or any mission building. Those are:
- Statel (1000026, 630 records, one per playfield) — placed objects, X/Y/Z + heading + TemplateId
- Door (1000030, 31 records)
- Wall (1000021, 309) — zone boundary segments with destination playfield
- type 1000013 (237,877 records) keyed `(playfield << 16) | index` — per-playfield mesh
  instances, high-entropy payload, not yet identified. pf 655 has 5291, pf 127 has 46.

Walkable floor indoors needs actual mesh geometry, i.e. the mesh decoder project
(`own-asset-decoders`). Statel positions alone give obstacle centres, not footprints.

## Also on disk, outside the RDB
- `cd_image/data/shadows/<pf>.sdw` — container of 8-bit grayscale PNGs (first one in
  800.sdw is 128x256). Per-playfield shadow/light rasters.
- `cd_image/data/statels/<pf>.pf` — per-playfield statel file, offset table at the head.
- `cd_image/twk/Tweak_RubiKa_PF_Positions.txt`, `Tweak_RubiKa_Waypoints.txt`,
  `Tweak_Playfield_<id>.txt` — 51 RK playfield universe positions, named waypoints.

## Where recorded-path nav stands today (for contrast)

20 zones recorded in `Build/Plugins/AOBuddy/nav/`. Subway pf 127: 2390 points,
98.1% one connected component — routing works there. Outdoor is thin:
Stret West Bank 417 points in **60** components, Andromeda 302 in 12.
Missions each have 4 floor-button warps recorded but `RouteToward` only walks
`Segments`, so cross-floor routing always returns null.

---

# What a mesh decoder actually requires (measured 2026-09-23)

## Three layers, and only one of them is hard

### 1. Already readable — no decoder needed

`Statel` (1000026) parses today with OmniCell's struct layout. Verified by writing a parser
and checking the output against known zone extents:

| pf  | statels | X range   | Y range | Z range   | distinct templates |
|-----|---------|-----------|---------|-----------|--------------------|
| 127 | 60      | 64..347   | 73..116 | 43..319   | 2                  |
| 800 | 72      | 241..764  | 30..75  | 249..729  | 11                 |
| 655 | 128     | 486..4357 | 2..59   | 596..2611 | 37                 |

pf127's Y 73..116 matches the bot's own recorded walk Y of 79..116 in `nav/127.json`.
Layout: Identity(8) skip(4) Identity0(8) playfield(4) pos(12) heading(16) skip(4) templateId(4).

**But statels are NOT the building.** The subway has 60 of them across 2 templates — those
are doors and terminals. The walls and floors are elsewhere.

### 2. The mesh format is self-describing — this is the good news

**RDB type 1010001, 11,228 records, entropy 4.78 bits/byte = plaintext.** It uses the SAME
reflective container as `AnarchyGroundDataDB_t`: the class and field names are in the record.

    RRefFrame_t     __class_id__ anim_matrix grp_mask local_pos local_rot scale
                    anim conn chld_cnt              <- scene graph node
    RTriMesh_t
    FAFTriMeshData_t  anim_pos anim_rot num_meshes isdegen SimpleMesh
    TriList           triangles trilist vb_desc vertices mesh
    BVolume_t         sph_pos sph_radius min_pos max_pos      <- bounding volumes
    FAFMaterial_t / FAFTexture_t / AnarchyTexCreator_t (type, inst)  <- not needed for nav
    FAFAnim_t         tot_time loop num_rot_keys rot_keys ...  <- not needed for nav
    FAFPointLight_t                                            <- not needed for nav

Readable instance names in the first record: `Box16`, `Omni01`, `Material #44`,
`wallwith bars2`.

So the decoder is **one generic reader for this reflective format**, not a per-field guessing
exercise. That same reader is what closes the `map_modulo` gap in the ground data above — it
is one piece of work that unlocks both.

**For navigation you can stop at `BVolume_t`.** `min_pos`/`max_pos`/`sph_radius` give obstacle
footprints without touching triangles, materials, textures or animation.

### 3. The actual blocker: RDB type 1000013

237,877 records, keyed `(playfield << 16) | index`. **Entropy 7.97-7.98 bits/byte, all 256
byte values present, near-uniform.** Not zlib, not raw deflate (tested every wbits at offsets
0-63), not bz2, not lzma. Compressed with something proprietary, or encrypted.

What it is, from the counts:
- pf127 (subway) has **46** records, indices 0..45 contiguous — and the playfield record
  (1000001) for pf127 lists **45 named rooms plus the zone name**. 1:1. These are the rooms.
- pf800 has 264, indices 33..538 sparse = occupied cells of a coarse spatial grid.
- pf505 (Avalon) has 7,715; pf655 has 5,291. Scales with area.

The indoor playfield record itself is readable and lists the whole subway layout in order:
Ladies' Room, Mini to Dome, Men's Room, Shopping Dead-end, Grand Dome, Shopping Arcade,
Entrance Stairs, Mini Bridge Exit West/East, U-turn West/East, Escalator West/East,
Ticket Checkpoint, Transport Aceess Tunnel, Statue Ramp Connector, Ramp Exit West/East,
d.Subway Station, Sandy Stairs, Black Windows, Cathedral Connector, Cave Bridge,
Quintiple Bridge, Helix Descent, Slum Cathedral, Boss Connector, Abmouth Showdown.

**So a room GRAPH for indoor zones is available now**, from names and ordering in 1000001.
The room GEOMETRY is behind 1000013.

## Bottom line

| goal | needs | status |
|---|---|---|
| outdoor walkable surface | ground data (1000009) | encoding cracked, `map_modulo` open |
| obstacle footprints | 1010001 `BVolume_t` via a reflective-format reader | plaintext, not written |
| indoor room graph | 1000001 room list | readable now |
| indoor walkable floors | 1000013 | **opaque — unsolved** |

The first three are ordinary work. The fourth is a real reverse-engineering problem with no
guaranteed outcome, and it is the only thing standing between us and mission/dungeon navmeshes.
A useful hedge: `Malishade/aogltf` and `AO-Model-Viewer` decode client meshes via the closed
`AODB.dll`. Neither repo carries a license, so no code may be copied — but running one as a
black box on a known record tells us what 1000013 decodes TO, which is a strong check on any
codec theory. See [[own-asset-decoders]].

---

# The blocker is NOT a wall — the client exports the reader (2026-09-23)

The user's instinct was right: the client knows. It ships the decoder, the collision
geometry API, and a pathfinder, all as ordinary 32-bit C++ DLLs with mangled exports.
The mangled names are the format spec.

## It is not encrypted — it is bit-packed

Sampled 3,000 records of type 1000013 and measured entropy **per byte position**:

    byte 0: 9 distinct values, 80% are 0x11, entropy 1.04
    byte 1: 145 distinct, entropy 4.91
    byte 2: 202 distinct, entropy 5.80
    byte 6: mostly 0/1/2, entropy 3.05
    bytes 3,4,5,7: entropy ~7.9

Encrypted data is uniform at *every* offset. A low-entropy header followed by a
high-entropy body is a **bit-packed structure**. Whole-record entropy of 7.97 was
misleading me — bit-packing destroys byte alignment, which looks like noise in a
byte histogram. Sizes range 30 B to 41 KB, mean 503 B.

## The named chain, all of it exported

`Collision.dll`
- `KDSurfaceBuilder_t::WriteBlobV5(BitPack_c&)` — **"V5" + `BitPack_c`**, and the RDB record
  header field at offset 30 is uniformly **5** for type 1000013 and never 5 for anything else.
- `VolumeMesh_t::ReadBlob(BinaryStream&)`, `Face_t::ReadBlob(BinaryStream&)`,
  `Face_t::Face_t(BinaryStream&)` — non-virtual public thiscall, i.e. directly callable.
- `CellSurface_t` — the query API we actually want:
  `GetSurfaceForPos(Vector3_t&)`, `GetSurfaceForCell(int)`, `GetLineIntersection`,
  `GetSphereIntersection`, `CalculateClosestPoint`, `CalculateNormal`, `Trace`,
  **`VetoPosition(Vector3_t&, LocalitySource_t*, const Vector3_t&)`** — literally
  "is this position allowed".
- `CellSurface_t::CellSurface_t(int,int,float,float)` = (cells x, cells z, cell size x,
  cell size z). That is exactly why type 1000013 is keyed `(playfield << 16) | cellIndex`.

`N3.dll` — and **N3.dll is the only module that references the constant 1000013** (3 times),
which pins the ownership:
- `n3SurfaceResource_t::ReadBlob(BinaryStream&)` — **the decoder for type 1000013**
- `RDBPlayfield_t::ReadBlob`, `RDBTileMaterial_t::ReadBlob`, `DbObject_t::ReadBlob`
- `n3Playfield_t::GetSurface()`, `n3Zone_t::GetSurface()`, `GetZoneSurface(int)`

`PathFinder.dll` — the client ships a real navigation graph, serialized:
- `VisibilityGraph_t::ReadBlob/WriteBlob(BinaryStream&)`
- `VisibilityNode_t::ReadEdges(BinaryStream&, int, vector<VisibilityNode_t*>&)`
- `GraphPathFinder_t::ReadBlob`, `CreateFromData(const GameData::VisibilityGraphData_t&)`
- `AstarGraph_t`, `ANode_t`, `GridSpace_t(int,int,float,float)`
- `GameData::PathGraphData_t::ReadBlob` lives in N3.dll

## Two routes, both open

**(a) Write our own reader.** The exported signatures give the class model for free
(`VolumeMesh_t` holds `vector<Face_t>`; `Face_t` holds `Vector3_t*` + count + a `PlaneRep_t`).
Remaining work is the bit-field widths in V5, recovered by disassembling
`n3SurfaceResource_t::ReadBlob`. Matches the project rule: our own decoders.

**(b) Call the client's own DLLs as a bridge.** `Collision.dll`'s `ReadBlob`s are `QAE`
(non-virtual public thiscall) so they can be called from a small 32-bit harness over our
bytes. N3's are `UAE` (virtual) and need a constructed object, so they are harder.
This is the user's own installed client, not third-party source — no licensing problem as with
aogltf. Best used as the **oracle that validates (a)**: decode a record both ways and diff.

Caveat: everything here is 32-bit x86. Our tooling is .NET 10 x64, so route (b) needs a
separate 32-bit bridge process.

## What this means for the plan

Indoor walkable geometry moves from "unsolved, may stop us cold" to "a known, named,
bit-packed format with the reader shipped alongside it." That was the one thing that could
have made the rest of the navmesh work pointless. It doesn't.

---

# The prebaked visibility graph: it exists, and the engine LOADS it (2026-09-23)

## Proof it is loaded, not generated

`PathFinder.dll` exports both a baking API and a loading API. The question is which one the
engine actually uses. N3.dll is the only module that imports PathFinder.dll, and its import
list settles it. N3.dll imports:

    GraphPathFinder_t::CreateFromData(const GameData::VisibilityGraphData_t&)
    GraphPathFinder_t::TransformGraphNodes(const Vector3_t&, int, const Vector3_t&)
    GraphPathFinder_t::MergeAddRoomGraph(const GraphPathFinder_t&,
        const vector<pair<int,int>>&, const Vector3_t&, int, const Vector3_t&)
    GraphPathFinder_t::ConnectDoorNodes()
    GraphPathFinder_t::Configure(int,int,float,float)
    GraphPathFinder_t::SetSpaceForGraph(Space_i*)
    GraphPathFinder_t::FindPath(const Vector3_t& from, const Vector3_t& to,
        vector<Vector3_t>& out, LocalitySource_t*, bool, bool)
    RoomSpace_t::AddRoom(int, vector<BoxRegion_t>&, vector<int>)
    n3RoomSpace_t::SetPlayfield(n3Playfield_t*) / SetTilemap(n3Tilemap_t*)
    n3RoomSpace_t::GetInsideCell(const Vector3_t&) / GetCellFromPos(int, const Vector3_t&)

N3.dll does **NOT** import `GenerateGraph`, `GenerateGraphForRoom`, `CullUselessNodes` or
`RemoveSurfacePenetratingEdges`. Those are exported but unused at runtime — they are the
offline baking tools Funcom's exporter used.

So the runtime flow is:

    VisibilityGraphData_t (prebaked, per ROOM/model, LOCAL coords)
      -> CreateFromData
      -> TransformGraphNodes(offset, rotation, pivot)    place the room in the world
      -> MergeAddRoomGraph(...)                          stitch rooms together
      -> ConnectDoorNodes()                              link through doors
      -> FindPath(from, to, out)                         A*, returns a waypoint list

`RoomSpace_t::AddRoom(int, vector<BoxRegion_t>&, vector<int>)` also tells us rooms are
**lists of box regions with a neighbour list** — a room graph, free.

## Where the bytes are: NOT FOUND YET. What is ruled out

- The class name `VisibilityGraphData_t` / `PathGraph` appears in **no** RDB record.
  Sampled 250 records each of 1010001, 1010026, 1010027, 1000001 and all 39 of 1000008.
  Expected: `PathGraphData_t` is a `DbObject_t`, so it serialises as a **nameless BinaryStream
  blob**, not in the self-describing reflective format the meshes and ground data use.
- 1000046 (14,311 recs) and 1000047 (946 recs) are **every record exactly 16 bytes** — fixed
  size, cannot be a graph.
- 1010013 (363 recs, 2-3806 bytes) decodes as normalised direction vectors with intensity
  values (-1.0, 0.0, 16.0, 0.1) — lights/materials, not nodes.
- `cd_image/data/statels/<pf>.pf` (629 files) is the **statel list**, not a graph: entry at
  offset 16666 decodes to (683.80, 72.80, 541.60) and the RDB statel record for pf800 carries
  the same placements at (680, 73, 545) / (680, 73, 538). Same data, different container.
  `cd_image/data/shadows/<pf>.sdw` is 336 files — exactly the 336 outdoor playfields.
- The playfield record (1000001) remains the **strongest candidate**: `RDBPlayfield_t::ReadBlob`
  is in N3.dll, pf127 is 99,995 bytes for 45 named rooms, entropy 7.95. But scanning it for
  runs of >= 6 consecutive plausible float triples (both world-range and room-local-range)
  returns **zero** — so if the graph is in there it is packed, not a plain array.

## The cheap way in: don't find the format, harvest the built graph

Every call needed to read a graph out of a live `GraphPathFinder_t` is exported:

    GraphPathFinder_t::GetGraph() -> VisibilityGraph_t*        QBE  public non-virtual
    VisibilityGraph_t::ForAllNodes(LocalityWorker_i*)          QAE  public non-virtual
    VisibilityGraph_t::ForAllEdges(EdgeWorker_i*)              QAE
    VisibilityNode_t::GetPos() -> const Vector3_t&             UBE  virtual
    VisibilityNode_t::GetNumEdges()                            QAE
    VisibilityNode_t::GetBoundingBox() -> const BoxRegion_t&   UBE  virtual
    GraphPathFinder_t::ReadBlob(BinaryStream&)                 QAE  public non-virtual
    GraphPathFinder_t::FindPath(...)                           UBE  virtual

Three options, cheapest first:

1. **Harvest.** Let the client build the graph for a zone, then walk it with
   `GetGraph` + `ForAllNodes`/`ForAllEdges`/`GetPos` and dump nodes+edges to our own JSON,
   once per playfield. The bot then uses our file and never touches the client again.
2. **Call FindPath directly** from a bridge. No harvesting, no format work at all.
3. **Feed `GraphPathFinder_t::ReadBlob` candidate blobs** (it is public non-virtual, so it is
   directly callable) and see which RDB record parses. This finds the format by brute force
   rather than by disassembly.

All of it is 32-bit x86, so any of these needs a 32-bit bridge process; our tooling is
.NET 10 x64.

The alternative — disassembling `RDBPlayfield_t::ReadBlob` and `PathGraphData_t::ReadBlob`
to locate and specify the blob — is the "our own decoder" route and is a lot more work for
the same result. Worth doing later for independence, not first.

## CORRECTION to option 1: no running game needed

The earlier phrasing ("let the client build the graph for a zone") implied running the game
and visiting every playfield. That is not necessary. GameData.dll exports the whole data-side
chain, so a batch tool can parse straight from `ResourceDatabase.dat` with nothing running:

    DbObject_t::CreateObject(const Identity_t&)            SA   static factory (type+instance)
    DbObject_t::Register(TypeID_e, factory)                SA   the type -> class registry
    PathGraphData_t::PathGraphData_t(const Identity_t&)    QAE  public ctor
    DbObject_t::ReadBlob(BinaryStream&) -> bool            UAE  public virtual
    PathGraphData_t::GetData() -> const VisibilityGraphData_t*   QBE
    VisibilityGraphData_t::GetNodes() -> vector<VisibilityNodeData_t*>
    VisibilityNodeData_t::GetPos() -> const Vector3_t&
    VisibilityNodeData_t::GetEdges() -> vector<VisibilityEdgeData_t>
    GameData::operator>>(BinaryStream&, VisibilityNodeData_t&)   free function, exported

So: construct `PathGraphData_t`, hand it the record bytes, walk nodes and edges, write JSON.
No login, no client process, no zone visits. The bot then reads our JSON and never touches
the client again.

`ReadBlob` returning **bool** also gives a free acceptance test: we still do not know which RDB
record feeds `PathGraphData_t`, so the tool can try every candidate record and keep the ones
that parse with a sensible node count. The client validates its own format for us.

Risk to state plainly: if no RDB record parses, the graph is somewhere we have not looked
(possibly server-side only, since `DbObject_t` also has `ReadDbRow`/`WriteDbRow` for SQL
storage), and this route dies — back to disassembling `RDBPlayfield_t::ReadBlob`.

---

# navbridge built and run — the shortcut does not work as built (2026-09-23)

Tool: `tools/navbridge/` (navbridge.cpp + rdb.h + build.cmd, 32-bit MSVC).
It loads the client's own msvcr100/msvcp100/BinaryStream/InstanceManager/
DatabaseController/GameData DLLs, constructs each `DbObject_t` subclass in place over an
over-allocated buffer, wraps the RDB record bytes in a `BinaryStream`, and calls the
class's `ReadBlob`. Every call sits behind SEH. Nothing is written to the client install
and the game is not running.

## The harness is proven good

Two independent confirmations, which matters because a brute-force probe that finds
nothing proves nothing otherwise:

1. **Self-test.** The statel record for pf800 begins with a count, and we parsed 72
   statels from those same bytes in Python. `BinaryStream` returns 0x48000000 = 72
   byte-swapped. So the thiscall convention, in-place construction and buffer ctor all
   work — and `BinaryStream` is **big-endian**, AO's serialisation order.
2. **The client talks back.** `PlayfieldDistrictInfo_t::ReadBlob` prints its own
   diagnostics through our process:
   `ERROR: can't load districts for playfield 100663296: version 25888 is not supported,
   current version is 7!`  (100663296 = 0x06000000 = byte-swapped 6.) We are executing
   real client reader code, not a stub.

## The result: the positive control FAILED, so the negative is about framing

Probed 6 GameData `DbObject_t` subclasses against all 50 RDB types, 25 records each,
with **both** `BinaryStream` (big-endian) and `BinaryLStream` (little-endian):

| class | records parsed |
|---|---|
| PathGraphData_t | **0**, every type, both endians |
| PlayfieldDistrictInfo_t | **0** |
| PlayfieldAreaInfo_t | **0** |
| LandControlMap_t | **0** |
| PlayfieldDynelData_t | true on arbitrary junk — no validation, meaningless |
| RDBInfoObject_t | true on arbitrary junk — no validation, meaningless |

`PlayfieldDistrictInfo_t` was the positive control and it failed. Type 1000029 plainly IS
the district record — it contains the literal strings "Borealis", "Omni-Tek",
"Abandoned Mall", "Condemned Subway" — and its own reader will not accept it. Tallying
every version number the reader saw across the whole sweep: 0 (944x), 1 (532x), 256
(532x), then noise. **Never 7**, in either endianness.

So this is not "the graph is not in the RDB". It is: **`DbObject_t` blobs are not raw RDB
record payloads.** GameData.dll is shared with the server and `DbObject_t` also carries
`ReadDbRow`/`WriteDbRow` for SQL — these are the database/network representation, wrapped
in framing (or a different container) we are not supplying.

## Next step, which follows directly

The RDB-side readers live in **N3.dll**, not GameData.dll, and they are exported:

    n3SurfaceResource_t::ReadBlob(BinaryStream&)    <- type 1000013, the collision surface
    RDBPlayfield_t::ReadBlob(BinaryStream&)         <- type 1000001
    RDBTileMaterial_t::ReadBlob(BinaryStream&)      <- type 1000024

`RDBTileMaterial_t` vs 1000024 is the positive control for that side: we already know
1000024 holds the tile material names ("sand", "rock", "salt", "phos"). If it parses, the
harness is validated on the RDB path, and `n3SurfaceResource_t` on 1000013 hands us the
indoor collision geometry directly — which is what we wanted the graph for.

Point navbridge's candidate table at N3.dll. The tool already does everything else.

---

# SOLVED: the indoor collision geometry is out (2026-09-23)

Two things were wrong before, both now fixed:

1. **Wrong module.** The GameData.dll `DbObject_t` classes are the database/network side.
   The RDB readers live in **N3.dll** and are exported.
2. **Wrong framing.** The record's size field at header offset 18 counts **12 header bytes
   that precede the payload**, and those 12 bytes - (type, instance, flag) at header
   offset 22 - are part of the blob. Feeding the bare payload fails; feeding
   `header[22 .. 22+size]` works. And the RDB stream is **BinaryLStream (little-endian)**,
   not `BinaryStream` (big-endian, which is the network order).

`Rdb::ReadFramed` in `tools/navbridge/rdb.h` does this.

## What now parses

| reader (N3.dll) | RDB type | result |
|---|---|---|
| `RDBTileMaterial_t::ReadBlob` | 1000024 | PARSED - positive control, 1000024 is the tile material table |
| `RDBPlayfield_t::ReadBlob` | 1000001 | PARSED, little-endian only (big-endian fails, so it discriminates) |
| `n3SurfaceResource_t::ReadBlob` | **1000013** | **PARSED, every record tried** |

## The geometry, with ground truth

`n3SurfaceResource_t::GetAllTriangles(vector<Vector3_t>&)` is exported and public.
Calling it on the first six subway (pf 127) rooms:

    inst 8323072   3486 verts   X 171.0..195.0   Y 107.6..115.7   Z 217.8..230.1
    inst 8323073   7491 verts   X  81.8..110.1   Y 107.5..126.0   Z 227.8..256.0
    inst 8323074   3774 verts   X 171.5..195.5   Y 107.1..115.7   Z 243.9..256.2
    inst 8323075   6612 verts   X 141.8..168.1   Y 107.6..115.7   Z 283.9..310.2
    inst 8323076   7464 verts   X 138.0..160.0   Y 107.6..116.4   Z 258.0..288.1
    inst 8323077  15612 verts   X 262.0..302.2   Y 103.8..119.8   Z 269.8..314.2

**Validation, not assertion:** the bot's own recorded nav for pf 127
(`Build/Plugins/AOBuddy/nav/127.json`, 2390 points the owner walked) spans
X 64..347, Y 79..116, Z 43..319. Every extracted room sits inside that envelope, and the
floor plane at Y 107.6 matches both the walked Y and the statel Y in the playfield record.
The geometry is real and it is in world coordinates - no transform needed.

Note the MSVC10/MSVC17 STL ABI gap: you cannot pass your own `std::vector` to the client's
code. Hand it a zeroed 64-byte buffer (an all-zero vector IS a valid empty one), let it
push_back through its own allocator, then read the (first, last, end) triple back out.

## Free extras on the same object

    n3SurfaceResource_t::GetTeleportDestinationPlayfield() -> int
    n3SurfaceResource_t::GetTeleportalArea() -> const PortalArea_t*
    n3SurfaceResource_t::GetTeleportLocalizerType() / GetTeleportLocalizerInstance()
    n3SurfaceResource_t::GetLineIntersection / GetSphereIntersection
    n3SurfaceResource_t::ReadVersion3 / ReadVersion4 / ReadVersion5

So zone lines and portals come out of the same records, and the blob version matches the
record header's flag at offset 30 (which is 5 for type 1000013 - hence `ReadVersion5`).

## Still unfound: the prebaked path graph

`PathGraphData_t::ReadBlob` parses **nothing**, in any framing or endianness, against
1000029, 1000001 or 1000013. `PlayfieldDistrictInfo_t` also still rejects 1000029 even
framed, though its errors show it now reads a name and an NPC level range before failing,
so it is close.

**This no longer blocks anything.** With triangles in hand we can build our own navmesh, or
hand the surface to the client's own baker - `GraphPathFinder_t::GenerateGraph(vector<Vector3_t>&,
float, float, float, float, ProgressMonitor_i*, bool)` and `GenerateGraphForRoom(...)` are
exported, as is `VisibilityGraph_t::RemoveSurfacePenetratingEdges(const Surface_i*, ...)`.
The prebaked graph became a nice-to-have.

---

# Export pipeline built; geometry is exact but GetAllTriangles is not the floor (2026-09-23)

`tools/navbridge/` now has an `export` mode and `validate.py`.

    navbridge.exe "<client dir>" export <outdir> <pf,pf,...|all>
    python validate.py <outdir> <nav dir>

Export groups type 1000013 by playfield (instance = `playfield << 16 | index`), parses each
record with `n3SurfaceResource_t::ReadBlob` (framed + little-endian), pulls triangles with
`GetAllTriangles`, and writes one `<pf>.tri` per playfield plus an `index.csv`.
Format: `"AOTRI1\0\0"`, i32 playfield, i32 recordCount, then per record i32 instance,
i32 vertCount, i32 teleportDestPf, i32 localizerType, i32 localizerInstance, then
vertCount * 3 floats (triangle soup, world coordinates).

Memory discipline matters at this scale: the triangle buffer is the client's heap, so it is
released with msvcr100's `free`, and each surface object is destructed with
`~n3SurfaceResource_t`. Leaking either exhausts a 32-bit address space long before 237,877
records.

## Five test playfields

    pf 127     46 recs ->   46 parsed    594,396 verts    0 teleport records
    pf 655   5291 recs -> 5195 parsed    899,613 verts  206
    pf 790   3417 recs -> 3233 parsed    597,423 verts  304
    pf 795   3999 recs -> 3989 parsed    892,386 verts   40
    pf 800    264 recs ->  264 parsed     67,872 verts    2

pf800's exported bounds are X 76.74..934.01, Z 76.67..848.03 — the Wall record (type
1000021) independently puts Borealis at X 76.7..923.4, Z 76.7..841.1. Two different records,
same zone envelope.

## The decode is exact. The coverage is not.

`validate.py` casts down from every point the owner walked and compares to the mesh:

| pf | triangles | walked pts | had a floor | median \|dY\| |
|---|---|---|---|---|
| 127 | 198,132 | 2390 | 17.5% | **0.01** |
| 655 | 299,871 | 312 | 35.3% | **0.01** |
| 800 | 22,624 | 156 | 4.5% | **0.01** |

Where a surface exists under a walked point the height agrees to **one centimetre**. The
decode, the endianness, the framing and the coordinate system are all right.

But the coverage is wrong, and in a specific way. For pf 127, **100% of walked points have
some triangle at their XZ** — and 2364 of 2390 are *below* the nearest surface, clustering
at -3.8, -5.8, -7.9, -11.6, -15.9, -19.7. Sampling individual points:

    walked (69.9, 115.61, 318.5)   surfaces at that XZ: 131.50, 131.71
    walked (136.6, 107.61, 226.6)  surfaces at that XZ: 125.07, 125.51
    walked (91.8, 107.61, 246.6)   surfaces at that XZ: 115.50, 115.70
    walked (322.6, 102.81, 158.0)  surfaces at that XZ: 107.11, 107.13, 107.13

Two or three surfaces, all 4-16 m overhead. **`GetAllTriangles` is returning ceilings and
roofs, not the walkable floor.** Calling the static `EnableInvisCheck()` first changes
nothing (identical 594,396 verts), so it is not the invisible-collision layer either.

## Next: query the surface instead of dumping it

`n3SurfaceResource_t` implements `Surface_i`, and the real collision queries are exported:

    GetLineIntersection(const Vector3_t& from, const Vector3_t& to, Vector3_t& hit,
                        bool, LocalitySource_t*) -> bool
    GetSphereIntersection(const Vector3_t&, float, Vector3_t&, ...) -> bool

Cast a ray straight down from a walked point and see whether the surface reports a hit at
the walked Y. If it does, the floor is in the object and `GetAllTriangles` is simply a
filtered view — and the ray query, not the triangle dump, is the primitive to build the
navmesh from. Same harness; it is a new call, not new research.

Also still open: `GetInvisibleSurface()` and `GetTeleportalSurface()` each return a separate
`KDTreeSurface_c*`, so there are at least three surface layers per record and we have only
looked at one.

## Ray/sphere queries tried: type 1000013 is NOT the walkable floor

`navbridge.exe <client> ray <pf> <pointsfile> [above] [below] [sphere]` loads every surface
record in a playfield and queries each one at every point the owner walked.

- `GetLineIntersection(from, to, hit&, bool, LocalitySource_t*)` **kills the process**
  (exit 116, not catchable by SEH) on the first record. Passing a null `LocalitySource_t*`
  is the likely cause - it is probably dereferenced. Constructing a real one is possible
  (`LocalitySource_t::LocalitySource_t()` is exported from PathFinder.dll) but untried.
- `GetSphereIntersection(centre, radius, hit&)` - the 3-argument overload, no locality
  pointer - runs cleanly through all 46 subway records and returns **0 hits out of 2390
  walked points**, sphere centred 0.5 above the feet with radius 1.5.

`DbObject_t::DecodeBlob()` was the obvious "maybe it needs a second init step" candidate.
It is not overridden by `n3SurfaceResource_t` (only the base exists, in all three DLLs), so
that is not the missing call.

Taken with the triangle dump - ceilings 4-16 m overhead at 100% of walked XZ, nothing below -
the consistent reading is that **type 1000013 is a special surface layer, not the room
floor**. That fits the class's own API: it also exposes `GetInvisibleSurface()` and
`GetTeleportalSurface()`, and carries teleport destination/localizer fields. It looks like
the *extra* collision a room needs - invisible walls, portal volumes, ceilings - while the
walkable floor comes from the room's render geometry.

### Next hypothesis to test

The floor is the room's **render mesh**: RDB type 1010001 (11,228 records, the
self-describing `FAFTriMeshData_t` / `RTriMesh_t` / `BVolume_t` format), instanced per room
by the playfield record, with collision derived at load. Two ways in:

1. Parse 1010001 ourselves - it is plaintext and self-describing, no reverse engineering
   needed, and we already have its field list.
2. Find the N3 class that turns a room into a `CellSurface_t`. `Collision.dll` exports
   `CellSurface_t::SetSurfaceForCell/GetSurfaceForPos/GetSurfaceForCell` and
   `KDSurfaceBuilder_t::NewSurface(vector<VolumeMesh_t>&)`, and N3 has
   `n3Playfield_t::GetSurface()` / `n3Zone_t::GetSurface()` / `GetZoneSurface(int)` - those
   return the assembled per-playfield surface rather than one record's.

Option 2 is closer to what the client actually does and would hand us the finished surface,
but it needs a constructed `n3Playfield_t`, which is a much bigger object than a record
reader. Option 1 is more work but stays in the "our own decoder" lane.

---

# Option 2 attempted: n3Playfield_t gets close, then wants a running engine (2026-09-23)

`navbridge.exe <client> pf <playfield> [pointsfile]` tries to have N3 assemble the whole
playfield, which is what the client itself does:

    n3Playfield_t::n3Playfield_t(const PlayfieldProxy_t&)          exported ctor
    n3Playfield_t::SetPlayfieldResource(const RDBPlayfield_t&)
    n3Playfield_t::CreatePlayfieldFromResource(RDBPlayfield_t*)    <- builds the real thing
    n3Playfield_t::CalculateGroundPoint(Vector3_t&) -> bool        <- "where is the floor"
    n3Playfield_t::GetSurface() / GetPathfinder() / GetSpace()
    n3Playfield_t::PosToRoom / IsPosInside / LineOfSight / RoomCanSeeFromTo

## What worked

- `RDBPlayfield_t` for pf 127 parses (already known).
- The `n3Playfield_t` constructor runs. `PlayfieldProxy_t` has no exported constructor
  anywhere in the client, so it is a header-only value type; a zeroed buffer with the
  playfield identity at the front is enough to get past the ctor.
- **The resource manager can be stood up**, and this part is reusable:

        ResourceDatabase_t::ResourceDatabase_t()                   exported
        ResourceDatabase_t::Open(const std::string&, bool) -> 1    SUCCESS
        ResourceManager::Get() -> 0x03761928                       a real singleton
        ResourceManager::SetDatabase(...) / DisableAsyncLoading()   both fine

  MSVC10's `std::string` is a header template and is not exported, so it has to be laid out
  by hand. Empirically the union sits at **offset 0** (no allocator padding): 16-byte
  buffer-or-pointer, then `_Mysize`, then `_Myres`. Long strings need the pointer form with
  memory from the client's own `malloc`. `Open` returning 1 is what confirmed the layout.

## What did not

`CreatePlayfieldFromResource` still crashes - but it gets far enough to allocate
**46 `n3Room_t` objects** first:

    ERROR: the memory pool "n3Room_t" contains unfreed slots!
    ERROR: - deleting pool (slotsize=180): 46 of 1000 slots not freed!

46 is exactly the number of type-1000013 records for pf 127, and the playfield record lists
45 room names plus the zone name. So the playfield record really does drive room creation,
and the rooms are built before the failure. What it wants next is engine state we have not
provided - candidates are the InstanceManager, the `n3PlayfieldFactory_i` that
`n3EngineClientAnarchy_t::GetPlayfieldFactory(const PlayfieldProxy_t&)` hands out
(Interfaces.dll), or a correct `PlayfieldProxy_t` rather than our zeroed guess.

## Verdict

Option 2 is not a next step; it is "run the client headless". Getting past
`CreatePlayfieldFromResource` means standing up enough of N3, Interfaces and the instance
manager that we are effectively booting the engine without a window. That may well be
possible - every piece so far has been exported and callable - but it is its own project.

**Option 1 is unblocked and does not depend on any of this**: parse type 1010001 ourselves.
It is plaintext, self-describing, 11,228 records, and we already have its field list
(`FAFTriMeshData_t`, `TriList` with `triangles`/`vertices`, `BVolume_t` with
`min_pos`/`max_pos`). That is where the room floors should be.

Reusable regardless of which way we go: the resource-manager init above, and the finding
that MSVC10 `std::string` puts its union at offset 0.

---

# Option 1 started: the reflective schema is solved (2026-09-23)

`tools/navbridge/meshschema.py` parses the self-describing header of types 1010001, 1010026
and 1000009. The rule, verified against 1010001 instance 1445:

- 5 header ints, one spare byte, then a NUL-separated string pool.
- The pool reads as: the root class name, then **(type code, field name) pairs** - the code
  comes *before* the field it describes. An empty string ends a class; the next string names
  the next one.

Running it on 1010001:1445 (2268 bytes, 117 pool strings, 1611 bytes of tagged data) yields:

    class RTriMesh_t        __class_id__ anim_matrix grp_mask local_pos local_rot scale
                            anim conn chld_cnt prio enable_light delta_state
    class FAFTriMeshData_t  version name anim_pos anim_rot num_meshes isdegen
    class RDeltaState       version rst_count tstv_count tch_count tch_type
    class AnarchyTexCreator_t  type inst creator tch_text env_texture diff spec ambi
                            emis shin shin_str opac material
    class TriList           triangles trilist vb_desc vertices mesh
    class BVolume_t         sph_pos sph_radius min_pos max_pos bvol data is_cloned

Exactly the classes and fields the mangled names in N3.dll and Collision.dll imply. No
guessing was needed and none was done.

## The subway's geometry IS in 1010001

Scanned all 11,904 records of 1010001 / 1010026 / 1010027 for the subway's room names:
**22 records mention "subway", 2 mention "Helix"**. Samples:

    1010001:15643   6,700 bytes   omnisubway, sign_omnisubway
    1010001:70645   7,468 bytes   subway_toilets_back, subway_toilets_woman
    1010001:96283  30,394 bytes   Helix03

"Helix Descent" and "Helix" rooms are in the pf 127 room list. So the room geometry - and
with it the floors - is in type 1010001, which is the plaintext self-describing format. This
is the right lane.

## What is left

The tagged binary section after the pool. It starts:

    09 00 00 00  19 00 00 00  01 00 00 00  01 00 00 00  3a
    11 00 00 00  04 00 00 00  04 00 00 00  00 00 00 00  51 01 00 00
    01 00 00 00  0f 00 00 00  01
    03 00 00 00  04 00 00 00  04 00 00 00  00 00 00 00  02
    0f 00 00 00  40 00 00 00  40 00 00 00  00 00 80 3f ...

Single-byte tags (0x3a, 0x01, 0x02) separating int quads, with real values appearing
(0x40 = 64, 0x3f800000 = 1.0f). It is a tagged value stream keyed against the schema we can
now read. Decoding it to reach `TriList.vertices` / `triangles` is the remaining work, and
it is ordinary format work rather than a search - we know every field name and its type
code, and `BVolume_t.min_pos`/`max_pos` give a cheap sanity check on any decode.

---

# The reflective mesh format is decoded, and the dungeon floors turned out to be tiles (2026-09-23)

Tools, all in `tools/navbridge/` (pure Python, read `ResourceDatabase.dat` directly, no client
process): `rdb.py` (index reader, moved in from a scratchpad), `meshdecode.py` (reflective
record decoder), `meshgeom.py` (scene graph -> world triangles), `playfield.py` (dungeon room
list), `tilemap.py` (dungeon tilemap PNGs), `maptest.py` / `heighttest.py` (acceptance tests
against the owner's walked points).

## 1010001 / 1010026: the tagged value stream is fully decoded

Measured on 1010001:1445 and then run over every record:

- Header: `i32 version(3), i32 flags, i32 flags, i32 1, i32 nameCount, u8 0`, then the string
  pool. The pool ends after `nameCount` names (class and field names); reading it "until a
  non-printable byte" is wrong because the byte after the pool is the object count, which is
  sometimes printable (`0x61` = 97 objects read as the string `"a"`).
- Names are numbered in pool order and that number is the **1-byte tag** used in the data.
  A class name gets a number too, and `__class_id__` holds the number of the object's class.
- Then `i32 objectCount` and a flat list of objects: `i32 byteSize, i32 1, i32 entryCount`,
  entries `u8 tag, i32 valueType, i32 elemSize, i32 totalSize, u8[totalSize]`. Object 0 is a
  wrapper whose `obj` field points at the real root; the count excludes it.
- **Pointer fields (valueType 17) are 0-based indices into the object list.** That is how
  `RTriMesh_t.data -> FAFTriMeshData_t.mesh[] -> SimpleMesh.trilist -> TriList` and
  `SimpleMesh.material -> FAFMaterial_t.delta_state -> RDeltaState.tch_text -> FAFTexture_t.creator
  -> AnarchyTexCreator_t(type, inst)` chain together. `elemSize` < `totalSize` means an array
  (`rst_type` = 3 ints); `elemSize` 0 means a length-prefixed blob (strings, vertex/index data).
- Value types seen: 0 bool, 3 i32, 6 string, 9 raw, 10 f32, 12 vec3, 13 quat, 15 4x4 matrix
  (row-major, translation in row 3), 16 rgb, 17 object ref.
- `SimpleMesh.vb_desc` = `(16, flags, D3D FVF, vertexCount)`; every mesh in the database is
  FVF 0x112 (stride 32) or 0x152 (stride 36) and `len(vertices) == count * stride` for all
  8,618 meshes checked. Position is the first 12 bytes of a vertex. `TriList.triangles` is u16.
- `anim_matrix` on `RTriMesh_t` includes the 3ds-Max Z-up to Y-up swap; with row-vector
  convention (`world = local * M`, parent-chained through `chld`) the `BVolume_t` bounds
  match the transformed vertices, and Helix03 comes out 9.7 m tall and Y-up.

Result: 11,211 of 11,228 records of 1010001 and all 668 of 1010026 parse to the byte, with 0-15
bytes of padding at the end. The 17 that do not are a different sub-format (animation name
lists: "idle", "walk", "shotgun_reload"), not meshes.

**But the subway records in 1010001 are props** (signs, a helix staircase, toilets). No room
shell is in there. That sent the search to the playfield record.

## RDBPlayfield_t (1000001, version 10, dungeons): the room list is decoded

`tools/navbridge/playfield.py`. Header `0x60` bytes: version 10, playfield id, name[32],
**tilemap id** (126 for pf 127), 10, room count. Then per room:

    u16 flags        0x05xx. bits 0-1 = rotation in 90-degree steps, bit 7 = a name follows
    u16 x1,z1,x2,z2  rect in the tilemap, template space (unrotated)
    f32 x,y,z        world centre of the room; y = world height of its LOWEST floor cell
    u16 nDoors; nDoors x (u16, u16)           OPEN: semantics
    char name[32]    only if flags & 0x80 (pf 127 room 45 and all of Camelot have none)
    u32 lmBytes, u32 lmSamples; lmBytes = zlib lightmap (u16 samples) + u32 trailer
    u32 ?; 0..n polygon meshes: u32 nVerts, vec3[nVerts], u32 nTris, u16[3*nTris], u32 0

The lightmap trailer is `1009 * (1 + nPolys)` in all 223 rooms checked. The polygons are
horizontal triangle fans in room-local coordinates (Subway Station: a 13x8 m quad at y=1.1;
Helix Descent: two 42 m strips at y=3.5) - probably extra walkable decks, OPEN.

Cross-check that pins the whole thing down: **the 46 rooms are in the same order as the 46
type-1000013 collision records for pf 127, and each room's stored (x, z) is the centre of that
record's bounds** (room 0 at (184, 224) vs surface centre (183.0, 223.9); room 1 at (96, 242)
vs (95.9, 241.9); all 28 checked). So `(playfield << 16) | index` in 1000013 is the room index.

Generality: 272 playfield records are version 10. 169 of them are outdoor (zone list, a
different struct, flags word is not 0x05xx) and 103 are dungeons; **100 of the 103 parse**,
the three that do not (1892, 2064, 2073) are OPEN. Every dungeon references a tilemap that
exists in 1000009 as the `GNDA` variant below.

## 1000009 has two variants; the dungeon one is a stack of PNGs

Outdoor records start `CHGA` (250x250 cells x 4.0 for Borealis). Dungeon tilemaps start
`GNDA`: pf 127's is 280x280 cells x **2.0 m**, height scale 0.2, and after a 20-entry u16
tile-family table (each family lists 16 piece ids: an autotile neighbour mask) it holds
**ten 280x280 PNGs** - four 8-bit grey, six RGB - inside tagged sections (DCGA, DHGA, HCDA,
IRHA, TAHA, XTHA, SIDA, HIDA, HSTA). `tools/navbridge/tilemap.py` pulls them out with PIL.
The piece ids resolve to JPEG textures (types 1010006/1010021/1010022), so tiles are
textured heightfield cells, not meshes.

The 280x280 map is **not world space. It is an atlas of room templates**: each room's rect
addresses its own patch, unrotated; rooms 0 and 2 (Ladies'/Men's, same template) sit at the
same x range and differ only in z. Rooms are placed in the world by `pos` and `rot`.

What the channels mean, from the data:

| png | mode | meaning |
|---|---|---|
| 0 | L | tile id, 0 = no floor, 1-19 = index into the family table, bit 7 = flag |
| 1 | L | **floor height, 0.2 m units, absolute in template space** |
| 2 | L | small values on pillar/object blocks, not height (OPEN) |
| 3 | L | sparse flags at edges/doors (OPEN) |
| 4, 5 | RGB | per-cell tile variant / edge markers, not height (OPEN) |
| 6-9 | RGB | empty for pf 127 |

## The mapping, and the acceptance test it passes

World -> template cell for a room `r`:

    d = (x - r.x, z - r.z); rotate d by -rot * 90 degrees (dx, dz -> dz, -dx per step)
    cell = ((x1 + x2 + 1) / 2 + dx / 2, (z1 + z2 + 1) / 2 + dz / 2)
    floor height = r.y + (p1[cell] - min p1 over the room's floor cells) * 0.2

Scored against the 2,390 points the owner walked in pf 127, eight dihedral candidates: this
one puts **99.3% of points inside their room's rect and 94.5% on a non-zero tile**; the
others fall to 63-89%. Entrance Stairs (rot 1) is decisive: 192 of 192 vs 58 for the opposite
rotation sign. Where a room shares a template with another, the min-p1 rule is what makes
Ticket Checkpoint (min 20), Ramp Exit (min 18), Escalator East (20..44 -> 0..4.8 m, exact) and
Subway Station (min 2) all come out right.

Then the union test, tiles OR the type-1000013 collision triangles within 0.6 m:

    tile height explains        2061 points
    collision triangles explain  402 points (300 of them tiles could not)
    unexplained                    29 points (1.2%)

**98.8% of every walked point in the subway is explained by tile heights plus the already
exported collision surfaces.** The 29 leftovers are door cells at rect edges (tile id 0 on
both sides of a doorway) and the top step of the entrance. So the walkable floor of a dungeon
is: floor tiles at their p1 height, plus the 1000013 triangles for stairs, bridges and
mezzanines (Entrance Stairs' descent 8 -> 4 -> 0 m is not in the tiles at all, and the
collision export has it).

This closes the "1000013 is not the walkable floor" finding above: it is not the *tile*
floor, it is everything else, and the two together are the floor.

## What this means for the plan

- No mesh decoder is needed for dungeon navmeshes. Room shells are heightfield tiles.
- The room graph is free: rooms, rotation, world placement, and the per-room polygon decks.
- Remaining work is ordinary: emit each room's floor cells as world quads, add the collision
  triangles with an up-facing/slope filter, bridge the door cells between adjacent rooms, and
  build the navmesh from that. Then the same for mission templates.

OPEN: door pair semantics, PNG 2-5, the three unparsed dungeon records, the polygon decks.

---

# Outdoor heightmaps decoded from the client's own routine; nav data exported per playfield (2026-09-23)

## The "cracked" heightmap encoding above was wrong

The signed row-delta reading only ever passed a smoothness test. Fitting it against walked
points in Borealis gave a 7 m p90 error and per-block offsets that clustered at exactly 256
units, which is a modulo wrap, not a delta chain. The real routine is exported from
`DisplaySystem.dll` as `AnarchyGroundData_t::Patch_t::DecompressHeightMap` (RVA 0x35DF0);
disassembled with dumpbin it does, per patch of (P+1)^2 bytes:

1. zlib-inflate the block;
2. cumulative sum down each column, modulo 256 (unsigned);
3. cumulative sum along each row, modulo 256;
4. widen each byte to a u16 by multiplying by 256.

So the decompressed patch is a **mod-256 integral image**, the byte is the absolute height in
`hscale` units (0.4 m in Borealis, 0.2 m elsewhere), and there is no base or delta chain at all.
The function has a second path for 16-bit blocks ((P+1)^2 * 2 bytes, same two sums modulo
65536, no widening); those zones store `hscale` already divided by 256 (Coast of Peace:
1/512, range 128 m). `heightmap_small_data` is one 4-byte entry per patch (the patch's four
corner bytes, a LOD summary), not a base. Patches are laid out `bz * nx + bx` with x fastest
(the caller at RVA 0x3603B walks rows then columns and calls `SetHeightmapPatch(buf, col*P,
row*P, P)`), `map_modulo` is just the sample stride, and the `CHGA` record wraps an ordinary
reflective object (`AnarchyGroundDataDB_t`) whose `heightmap_compressed_data` field holds all
the patches as length-prefixed zlib blobs, so `meshdecode.py` reads it as-is.

Acceptance, nearest sample, against the owner's walked points:

| pf | zone | points | median abs dy | p90 |
|---|---|---|---|---|
| 800 | Borealis | 202 | 0.01 | 0.26 |
| 790 | Stret West Bank | 417 | 0.01 | 0.95 |
| 795 | The Longest Road | 132 | 0.01 | 0.67 |
| 655 | Andromeda | 325 | 0.01 | 0.81 |
| 730 | Rome Park | 21 | 0.01 | 0.30 |

Swapping the axes gives 2.8-4.6 m medians, so the orientation is pinned too. `DecompressTileMap`
is a plain zlib of P*P u16 tile ids. Patch size is 64 almost everywhere, 32 in a few zones
(Rome Park), 8 in the "Needed PF-stuff" test zones.

## AOBuddy/GameData/Nav/<pf>/ - the deliverable

`tools/navbridge/exportnav.py` writes one folder per playfield from the resource database plus
navbridge's collision export (`navbridge.exe <client> export out_all all`, 618 playfields with surfaces, 626 folders in all,
42.0 M triangles, 1.5 GB raw), and verifies each folder against `Build/Plugins/AOBuddy/nav/<pf>.json`
where the owner has walked. Formats are in `AOBuddy/GameData/Nav/README.md`; the csproj copies
the folder to the plugin output.

- `ground.bin` for 336 outdoor zones (u16 heights, tile ids, building nibbles, zlib).
- `rooms.json` for 100 dungeons (room placement, per-cell tile id / height / flags, doors, decks).
- `collision.bin` for every playfield that has type-1000013 records: triangles with |normal.y| >
  0.5 only (18.2 M of 42.0 M), int16 centimetres per chunk, zlib. Ceilings stay; a navmesh build
  needs its own clearance test.

Verification (1 m tolerance, tiles/heightfield first, then collision):

| pf | kind | walked points | explained |
|---|---|---|---|
| 127 Condemned Subway | dungeon | 2390 | 98.9% |
| 1931 Temple of Three Winds | dungeon | 224 | 97.3% |
| 1186 / 1187 Supermarket | dungeon | 12 / 6 | 100% |
| 800 Borealis | outdoor | 202 | 100% |
| 790 Stret West Bank | outdoor | 417 | 100% |
| 795 The Longest Road | outdoor | 132 | 98.5% |
| 655 Andromeda | outdoor | 325 | 99.7% |
| 566 / 647 / 665 / 730 | outdoor | 40 / 4 / 16 / 21 | 100% |
| 4530 Jobe Platform | outdoor | 12 | 100% (all from collision: the platform is a mesh) |

## What is still not covered

- 190 playfields have neither: 187 with a version 8 or 9 playfield record and no ground record: the old
  autocontent dungeons and mission templates (`SL ACG(dng)` pf 362 alone has 1.05 M collision
  triangles, the Pandemonium mazes, `ACD Grey Caves`, `ACD Subway - Ventil`). They get
  `collision.bin` only. Their floor model is OPEN: the v8/v9 record layout has not been read,
  and there is no `GNDA` tilemap for them, so where their tiles live is unknown.
- 16-bit heightfield zones are decoded per the disassembly but have no walked data to verify.
- Tile passability (RDB 1000024 descriptors), the building-map nibble order, dungeon door
  pairs and PNG channels 2-5 remain OPEN, as before.
- The 20 mission-instance nav recordings (ids like 2224273) cannot be verified: they are
  server instances, and the mapping to a template playfield is not in the recording.

`tools/AONavExtractor` is the C# port of all of it, for anyone with a client install: the RDB
reader, the reflective decoder, the CHGA/GNDA readers, the room list, and the collision step,
which hosts the client's 32-bit DLLs in a child process through unmanaged thiscall function
pointers (so the tool is x86). Its output is byte-identical to `exportnav.py`'s.

---

# The old dungeons and the mission room pools were never a different format (2026-09-23)

The 190 "collision only" playfields were a version check, nothing more. Version 8 and 9
playfield records carry the same room list as version 10; only the header differs (rooms start
at 0x34 in version 8, at 0x60 in 9 and 10), and their tilemap ids are exactly the 55 `GNDA`
records nothing had referenced. With that, **289 of the 290 room-list records parse, 2,247
rooms**, and the room trailer is finally read properly:

    after the lightmap:  nPolys x (u32 id, u32 nVerts, vec3[nVerts], u32 nTris, u16[3*nTris])
                         u32 nObjects, nObjects x (vec3 pos, quat rot, vec3 point, f32 radius)
    nPolys = lightmap trailer / 1009 - 1   (holds in all 2,247 rooms)

The polygons are room-local (decks, ramps); the 44-byte objects are in world coordinates and
look like door or blocker placements (position, rotation, a second point, a radius) - OPEN.
The version-10 "extra u32" read earlier was this object count. `rooms.json` now carries both.

What came in: the autocontent mission pools (320 Midtech 81 rooms, 321 HiTech 69, 324 Clan
55, 322 Cave 55, 331 tarm 42, 341 Grey Caves 103, 346 Omnilab 72, 351 Subway Ventil 81,
362 SL ACG 68, 382 Alien ACG 13), the Shadowlands temples and Pandemonium mazes, Bio MARE,
the city buildings and apartments. Only 4352 "Market" (1 collision record) is left without
a floor model. Python and C# outputs stay byte-identical on the new records.

## Missions: the pools are the geometry, the wire is the layout

A mission instance is not in the resource database; the server builds it from a pool. The
protocol (OmniCell's `BuildingGeneratorData`, carried in the zone-in `PlayfieldAnarchyF`
message) gives `TemplatePlayfield` (the pool, e.g. 320), `Width`, `Height`, `WorldHeight`,
and `Rooms[]` of `(Room, Floor, X, Z, Rotation)` - the same placement model our static rooms
use (an index into the pool's room list, a cell position, a floor, a 90-degree rotation). So
a mission's floors are: for each placed room, the pool room's tiles at that position and
rotation, plus the pool room's collision record. Nothing has to be recorded per roll.

What blocks it today: AOBuddy10's `PlayfieldAnarchyFMessage` does not parse the building
block (OmniCell's serializer does), and no mission run has logged it, so there is no ground
truth yet. The 20 mission-instance recordings (ids like 2224273) cannot be verified until a
run logs `TemplatePlayfield` and the room placements alongside the walk. That is the next
step: extend the SDK message, log it under mission debug, run one mission, then compose and
verify exactly as the static dungeons were.

## AOBuddyNav.cs - the bot-side reader, test-only

`AOBuddy/AOBuddyNav.cs` loads a playfield's folder (ground, rooms, collision) and answers
floor-under-point queries; `navdata` in chat runs it against the live character (`navdata`,
`navdata <x> <z>`, `navdata verify`, `navdata unload`) and logs the answer. No controller
calls it. Checked out of process on 127 (98.9%), 1931 (97.3%), 800 (100%) and the mission
pools; the Subway loads in 130 ms, Andromeda in 30 ms.
