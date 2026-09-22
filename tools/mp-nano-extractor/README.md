# mp-nano-extractor

Extracts every nano a Meta-Physicist (profession id 12) can cast from the local
OmniCell client data and writes the AOBuddy10 profile files.

## What it does
1. Loads `nanos.ocp` through `OmniCell.Core` `NanoLoader.CacheAllNanos` (net10 DLL,
   resolved from `E:\Funcom\OmniCell\OmniCell\Libraries\Source\OmniCell.Core\bin\Release\net10.0`).
2. Keeps a nano when a cast Action (`ActionType.ToUse` = 3) has an `EqualTo`
   requirement `Profession(60)==12` **or** `VisualProfession(368)==12`.
3. Joins names from `itemnames.sql`, reads each nano's `NanoStrain` (stat 75) →
   `NanoLine` enum name, summarises `Events->Functions`, and categorises by function.
4. Writes:
   - `E:\Funcom\AOBuddy10\AOBuddy\GameData\profiles\metaphysicist-nanos.json`
   - `E:\Funcom\AOBuddy10\AOBuddy\GameData\profiles\metaphysicist-nanos.md`

All ids/names/levels come only from local client data (`nanos.ocp` + `itemnames.sql`).
No web data is used for any id, name, or level.

## Run
```
dotnet run -c Release -- "E:\Funcom\OmniCell\OmniCell\Datafiles\nanos.ocp" "<scratch-out-dir>"
```
The scratch out-dir receives diagnostic dumps (entries-flat.json, review.tsv,
fn-all.txt, strain-histogram.txt); the two profile files are always written to the
fixed profile path above.

Diagnostic mode — dump requirements for specific nano ids:
```
dotnet run -c Release -- "<nanos.ocp>" x "125741,125742,225902"
```

## Notes
- `nanos.dat` in `attic\extracted-client-data` is the legacy zlib+MessagePack
  extractor cache and is NOT readable by the current net10 `NanoLoader`; use the
  converted `nanos.ocp` pack (same 18.8.50_EP1 client extraction).
- `MsgPack.Cli` 1.0.1 is referenced only to satisfy `OmniCell.Core`'s dependency
  at runtime; sibling OmniCell DLLs are resolved from the bin dir at load time.
