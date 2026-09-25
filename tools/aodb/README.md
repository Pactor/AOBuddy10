# AODB — Anarchy Online character reference (AOBuddy10)

`app.py` is a zero-dependency Python 3 web app. It reads the JSON under `AOBuddy/GameData/profiles/`.

```
aodb.bat            serves http://localhost:8888
aodb.bat 9000       or any other port
```

## Rollable tab (mission-reward research)

Every profession page has a **Rollable** tab. It lists each nano crystal the profession can use, grouped by the
profile's nano categories where the profession has them, and by nano line where it doesn't. Each row shows:

- the icon
- the nano, and the crystals that carry it
- QL
- NCU
- the level and nano-skill requirements (LVL MM BM PM MC TS SI)
- the offline **rule** from `<class>-nano-sources.json`
- what the bots have **seen** at the mission terminal

The **seen** column comes from each bot's own files:

| File | What it holds |
|---|---|
| `Build/Plugins/AOBuddy/offered.json` (alt), `Build-Main/Plugins/AOBuddy/offered.json` (main) | every reward offered, by crystal template; rolls counted per mission QL; nanos judged not rollable |
| `…/wants.json` | crystals collected by want runs (`got`) |

The badges mean:

- **offered N×**: seen as a mission reward N times, so it is rollable.
- **never in N rolls**: never offered in N rolls whose mission QL was within 8 of the crystal's QL. The badge turns yellow at 500.
- **not rollable**: dropped by the bot after `WantUnseenRolls` rolls without a sighting, or by hand with `mission run want drop <name>`.
- **collected**: a want run got it.
- **no rolls at its QL yet**: no evidence either way.

Every roll either bot makes adds to this evidence, in want runs and normal runs alike.

### The data behind it

`profiles/rollable-<slug>.json` holds all 14 professions and is committed. `tools/rolldata` writes it from the bot's
own item data: `Build/GameData/ItemData.bin`, plus `ItemWantData.bin` for the crystal → nano link through the Upload
function.

Regenerate it when the item data changes. Build to a scratch folder, because a bot that is running locks `Build\`:

```
cd tools\rolldata
dotnet build -o %TEMP%\rolldata
xcopy /e /y ..\..\Build\GameData %TEMP%\rolldata\GameData\
%TEMP%\rolldata\rolldata.exe %TEMP%\rolldata ..\..\AOBuddy\GameData\profiles
```

The third argument, the path to `itemnames.sql` (names and icon ids), is optional. It defaults to
`E:\Funcom\attic\extracted-client-data\itemnames.sql`.

## Icons: how to add the missing ones

The icons are **Funcom art from the AO client**. They are **not in git**: `tools/aodb/icons/` is ignored. Every
machine exports its own copy from a local AO install. Without them the page still works, and it just shows no
icons.

**You need:**

1. An **AO client install**, the classic client with `cd_image\data\db\ResourceDatabase.*`, for example
   `E:\Funcom\Anarchy Online`.
2. An **OmniCell checkout** next to this repo, at `..\OmniCell` relative to `AOBuddy10`. `tools/aodb-icons`
   references its `Tools\AssetDecoder\AssetDecoder\AssetDecoder.csproj`, which holds the resource database reader
   and the icon decoder. If your OmniCell is somewhere else, edit the `ProjectReference` path in
   `tools/aodb-icons/aodb-icons.csproj`.
3. The .NET 10 SDK.

**Export the icons:**

```
cd tools\aodb-icons
dotnet run -- "E:\Funcom\Anarchy Online" ..\..\AOBuddy\GameData\profiles ..\aodb\icons
```

The first argument is the AO client folder. The second is the profiles folder: the tool reads every `"icon"` and
`"nanoIcon"` id in `rollable-*.json`. The third is where the PNGs go.

It prints something like `717 icon ids: 533 written, 184 already there, 0 not in the client`. Files that are already
there are skipped, so running it again only adds what is missing. That covers new `rollable-*.json` data, a new
profession, or a folder you deleted.

**Check it:**

- `tools\aodb\icons\` holds `<iconId>.png` files, for example `12227.png` (the Ward crystal).
- Restart AODB and open any profession, then the **Rollable** tab. The icons are served from `/icons/<id>.png`.

**If some are still missing:**

- *"N not in the client"*: that icon id is not in your client's resource database. The id comes from
  `itemnames.sql`, so re-extract that file from the same client version.
- Only some icons are missing after new data: run the export again. It adds the new ids.
- None of the icons show: check that the folder is `tools\aodb\icons` and not `tools\aodb-icons\...`. Check that
  AODB was restarted. Look for 404s on `/icons/...` in the browser's network tab.

Each icon is read from record type `1010008` in the client's resource database, and its keyed background is made
transparent. OmniCell's `IconCatalog` does the same.
