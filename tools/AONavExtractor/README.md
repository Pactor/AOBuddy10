# AONavExtractor

Writes one folder of navigation data per playfield from an Anarchy Online client install:
the same layout AOBuddy ships in `AOBuddy/GameData/Nav` (formats in the `README.md` there,
which the tool also drops into its output folder).

    dotnet build -c Release
    bin\Release\AONavExtractor.exe "<AO install dir>" <out dir> [options]

    --nav <dir>       AOBuddy's walked-path recordings (Build\Plugins\AOBuddy\nav\<pf>.json);
                      when given, every zone he has walked gets a verification block in info.json
    --only 127,800    just these playfield ids
    --no-collision    skip the collision surfaces (no client DLLs are loaded then)
    --tri <dir>       reuse .tri files from tools/navbridge instead of extracting collision
    --keep-tri        keep the intermediate <out>\.tri\<pf>.tri files

Needs only the install itself: `cd_image\data\db\ResourceDatabase.*` for everything, and the
client's own `N3.dll`, `BinaryStream.dll`, `GameData.dll` (plus what they import) for the
collision step. The game does not have to be running; the files are opened shared.

What it reads and how:

- **Outdoor ground** (`ground.bin`): RDB 1000009 `CHGA` records. The heightmap is decoded the
  way `DisplaySystem.dll`'s `AnarchyGroundData_t::Patch_t::DecompressHeightMap` does it
  (per patch: inflate, cumulative sum down columns then along rows modulo 256/65536).
- **Dungeon rooms** (`rooms.json`): RDB 1000001 version-10 room lists joined with their `GNDA`
  tilemap, a PNG-layer template atlas (own PNG decoder, 8-bit grey/RGB).
- **Collision** (`collision.bin`): RDB 1000013 is a bit-packed KD surface nobody has reversed,
  so the tool hosts the client's 32-bit `N3.dll` and calls `n3SurfaceResource_t::ReadBlob`
  and `GetAllTriangles` on each record - which is why the tool is built x86. That runs in a
  child process; a record that faults the reader is skipped and the child restarted, and the
  run says so.
  For dungeons and outdoor zones it also writes `walls.bin` (not committed to the repository - this tool is how you get them): the steep triangles (|normal.y| <= 0.5) spanning at least
  1 m of height, in the same format, for the bot's mission routing.

Output is byte-identical to `tools/navbridge/exportnav.py`, the Python pipeline the formats
were worked out with (checked on 127, 800, 655, 1186, 4530 and 556). How the formats were
found and verified against walked ground truth is in `NAV-CLIENTDATA.md` at the repository
root.
