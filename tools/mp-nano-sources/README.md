# mp-nano-sources

Companion to `mp-nano-extractor`. Builds
`E:\Funcom\AOBuddy10\AOBuddy\GameData\profiles\metaphysicist-nano-sources.json` —
a "where to get" record for every nano id in `metaphysicist-nanos.json`.

## Authoritative signals (local data only)
- **Expansion gate:** reads the `Expansion` (stat 389) `BitAnd` (op 22) requirement
  from the nano program's `ToUse` action AND from its nano-crystal item's use/wield
  criteria in `items.ocp`. bit1(2)=ShadowLands, bit3(8)=AlienInvasion,
  bit5(32)=LostEden (AOSharp `ExpansionFlags`). SL nanos are not mission-rollable.
- **Crystal link:** nano id -> crystal item id by scanning `items.ocp` for an
  `UploadNano` (FunctionType 53019) whose argument is the nano id.
- **Shop presence:** crystal item id present in OmniCell `research-output/**/vendors.csv`.
  Local vendor data currently covers only researched playfields (Temple of Three
  Winds) and is empty of nano crystals, so general nano-shop buyability is NOT
  separable from local data.
- **Default rule (cited, not invented):** a non-shop, non-expansion profession nano
  crystal is mission-rollable — applied only when no Expansion gate and no local shop
  entry exist.

No vendor, mob, garden, or SL tier is asserted unless it comes from local data;
unknown SL sub-sources and tiers are left `null`.

## Run
```
dotnet run -c Release
```
(paths are hardcoded to the OmniCell Datafiles and the profile produced by
mp-nano-extractor.)
