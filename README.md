# AOBuddy10

A clientless Anarchy Online teammate bot, and the .NET 10 fork of the AOSharp clientless SDK it
runs on. It logs a second account into the game without the game client, follows you, fights what
you fight, heals, buffs, keeps pets up, and rides lifts and zone lines after you.

Everything it does is driven by what the server actually sends. Where a decision needs to know
something about the character — what a pet is for, which weapon specials are available, whether the
loadout is melee or ranged, which nanos are worth keeping up — it reads that from the wire or from
the item data, not from per-character configuration. Several long-standing guesses have been
replaced by the real field after checking it against recorded traffic; the comments say which
capture settled each one.

## Layout

| | |
| --- | --- |
| `AOBuddy/` | the bot itself — one controller per system, described below |
| `AOSharp.Clientless/` | the clientless SDK: connection, dynels, inventory, wire readers |
| `AOSharp.Common/` | game data types and the SmokeLounge AOtomation message library |
| `AOSharp.Clientless.Chat/` | the chat-server connection |
| `Ionic.Zlib/` | the stream compression the zone connection negotiates |
| `tools/` | the AODB reference web app |
| `Test/` | the launcher host |

The bot is split so that working on one behaviour cannot break another. Each controller owns its
own state and nothing else touches it:

- **`Movement.cs`** — the one place the body moves. Every mover calls it.
- **`FollowController.cs`** — records the owner's route as a waypoint queue and walks it. Owns all
  zone-line crossing.
- **`CombatController.cs`** — decides what to fight and issues the attack. Never moves the bot.
- **`PetController.cs`** — summons and commands pets, per pet and per role.
- **`SupportController.cs`** — heals, stims, resting, buffs, supplies. No movement.
- **`TravelController.cs`** — riding lifts, grids, whompas and mission terminals.
- **`NavController.cs`** — per-playfield memory of walked routes, crossings and objects.
- **`Main.cs`** — wiring: the tick loop, chat commands, and the two arbiters. One system moves the
  body each frame; the action sequence runs every tick without any step starving the next.

## Building and running

Needs the .NET 10 SDK. One command builds everything:

```
dotnet build AOBuddy10.slnx
```

Everything lands in `Build/`, and that is the only folder you need to look in:

```
Build/
  Test.exe                        the host
  AOSharp.*.dll, GameData/        the SDK and its data
  config.json                     <- your accounts
  Plugins/
    AOBuddy/
      AOBuddy.dll, GameData/
      config.json                 <- AOBuddy's settings
```

Two files to fill in, each sitting next to the thing it configures. Copy them from the
`config.example.json` the build drops beside each:

```
copy Build\config.example.json Build\config.json
copy Build\Plugins\AOBuddy\config.example.json Build\Plugins\AOBuddy\config.json
```

`Build/config.json` takes the account(s) to log in. It does **not** need to name the plugin: the
host loads every `Plugins/<name>/<name>.dll` it finds. List plugins explicitly only to load a
subset, and a listed path may be relative to `Build/`.

`Build/Plugins/AOBuddy/config.json` is the bot's own settings — at minimum `Owner`, the character
it takes orders from and follows.

`config.json`, `*.log` and `Build/` are all in `.gitignore` and must stay there: credentials and
play logs live in them.

The bot is commanded by sending it a private message in game; `help` lists what it understands.

## Tools

`tools/aodb` is a zero-dependency Python web app that browses the generated class reference data —
professions, breeds, skill caps, implants, equippable weapons with their specials, castable nanos:

```
tools\aodb\aodb.bat            http://localhost:8888
tools\aodb\aodb.bat 9000       another port, when 8888 is taken
```

It opens the browser for you and needs nothing but Python 3 — no packages.

It reads the JSON under `AOBuddy/GameData/profiles/`, generated from the game's own item data.
Every entry records where its numbers came from; sections with no data yet say so rather than
inventing any. The Meta-Physicist is complete and is the worked example for the rest.

## Game data

`AOSharp.Clientless/GameData/` holds the item and dynel databases the bot reads to answer questions
about items, nanos and requirements. They are committed because nothing works without them.

## Reading the protocol

`OmniCell/Tools/Capture/PcapDecode` (a separate repository) decodes a recorded session into named
messages. Pointing it at this repository's `AOSharp.Common.dll` instead of its own message library
decodes the same capture with *our* parser, which is how a field's meaning gets settled and how a
change to one is checked for not altering anything else.
