# AOBuddy — Solo Play Milestones

Roadmap for turning the teammate bot (Mixia / dano45) into a bot that can run
**solo**: know its own kit, prep nanos, run mission terminals, travel the whompa/grid,
and buy/sell to save for upgrades. Written 2026-09-17. The owner (main) still issues
every command by private tell; nothing here changes the "commands from one owner only,
never public chat" rule.

Legend:  ✅ done · 🟡 partial/scaffolded · ⬜ not started

---

## Milestone 1 — Profession & nano knowledge  ✅ (built 2026-09-17)

"Find out what class they are, then find all the possible nanos they can learn and know
what nanos they can cast."

**What the game/SDK gives us (verified in code):**
- `DynelManager.LocalPlayer.Profession` → `Profession` enum (from `Stat.Profession`).
- `LocalPlayer.SpellList` (= `FullCharacterMessage.UploadedNanoIds`) → nanos currently
  **uploaded / castable right now**.
- Offline nano+item DB shipped with the bot: `GameData/ItemData.bin` + `.idx`
  (`ItemData.Find<NanoItem>(id, out ni)` gives Name, NanoLine, NanoSchool, NCU, Cost,
  cast requirements). This DB is already copied to `bin/Debug/GameData`.
- `ReqChecker` evaluates a nano's `UseCriteria` (profession + level + skill gates)
  against the character's live stats.

**Added this milestone:**
- SDK: `ItemBase.MeetsUseReqs(target=null, ignoreTargetReqs=false)` — "can I use/cast this?"
- SDK: `ItemData.AllNanoIds()` — enumerate every nano template id in the offline DB.
- Commands (private tell):
  - `class` / `whoami` — profession, level, breed, HP, NCU used/max, abilities, #uploaded.
  - `nanos [filter]` — uploaded (castable-now) nanos, optional name filter.
  - `learnable [filter]` — nanos the character **qualifies to cast but hasn't uploaded**
    = the buy/roll shopping list. First call scans the whole DB (brief hitch), then cached;
    `learnable refresh` rebuilds (e.g. after leveling / new skills).

**Test tomorrow:** tell her `class`, `nanos`, `learnable`, `learnable heal`. Confirm the
profession is right and `learnable` returns a sane list (not other professions' nanos).

**Next refinements (⬜):** group `learnable` by NanoSchool/NanoLine; show QL + NCU + Cost;
tag each nano's **source** (vendor-buyable vs mission-reward-roll vs tradeskill) so the
shopping planner (M2) knows where to get it.

---

## Milestone 2 — Nano acquisition planner  🟡

"They will need to purchase, or roll for that nano."

Goal: given a target nano (or "next upgrade in my main lines"), decide **how** to get it
and **what it costs**, then feed M3/M4/M5.

- ⬜ Per-nano source table: general nano shops sell many; some drop / are mission-rolled;
  froob vs paid. Start from the `learnable` list, add a source classifier.
- ⬜ Priority list: which nano lines matter for this profession's solo survival
  (heal, damage, init/AC buffs, HP/nano buffs, pets for pet profs). Config-driven so the
  owner can set goals: `goal <nanoline>` / `goal implant <slot>` / `goal ncu`.
- ⬜ Credit budget tracking (see M4) → "saving for X, need N credits".

---

## Milestone 3 — Mission terminals  ⬜

"Run a mission terminal … a place I pick … map out all the possible doors, like in Borealis
you can roll missions in Borealis but often they are different doors."

**Model (AO):** a mission terminal is a usable dynel. Rolling produces a mission whose
destination is a **door** somewhere in (or near) the chosen city playfield; the door set is
finite but the roll picks among them. You walk to that door to enter the mission playfield
(a zone line, exactly like our existing zoning).

**Plan:**
- ⬜ **Owner picks the base** (e.g. a Borealis terminal). Save the terminal's location as a
  named path/waypoint (reuse the saved-path system).
- ⬜ **Door atlas per city**: record every possible mission-door position (walk to each, save
  as `borealis-door-<n>`), same recorder we already have. This is the "map out all the doors"
  step — do it once per city, on foot, with the owner.
- ⬜ **Roll packets**: capture the mission-terminal use + roll + accept sequence
  (need a live sniff — same method as the zone/login captures). Decode which field carries
  the destination so we can match it to the nearest door in the atlas.
- ⬜ **Runner loop**: use terminal → roll (optionally re-roll for map/reward) → read
  destination → route to the matching door (saved path + zone push we already have) →
  clear mission → return.

Blocking dependency: a mission-roll capture. Everything else reuses existing movement.

---

## Milestone 4 — Buy / sell  ⬜

"Learn to buy and sell … sell, then if saving up for a nano/implant/NCU upgrade they would
buy it and use it."

**What exists:** `Trade.cs` handles **player-to-player** trade (KnuBot window: Open/AddItem/
Accept/Confirm, credit tracking). That is NOT the shop/vendor path.

**Plan:**
- ⬜ **Vendor shop packets** (open shop terminal, list, buy, sell). Not in the SDK yet —
  capture from a live shop session and add a `Shop` static like `Trade`. Decode buy/sell +
  price + credit balance.
- ⬜ **Sell loop**: walk to a chosen shop/NPC, open, dump flagged inventory (junk loot),
  read credits gained.
- ⬜ **Buy loop**: if credits ≥ target cost, buy the planned nano/implant/NCU and **use it**
  (upload nano / equip implant / plug NCU). Uploading a nano = same cast/CharacterAction we
  already use; equipping uses inventory move-to-slot.
- ⬜ Credit tracking persisted so "saving up" survives restarts.

Blocking dependency: a vendor-shop capture.

---

## Milestone 5 — Whompa / grid travel routing  ⬜

"Run using the whompa system to the terminal/NPC of their choice."

**Plan:**
- ⬜ **Travel graph**: nodes = whompa booths + grid exits + key NPCs/terminals; edges =
  saved on-foot paths between them (reuse recorder) and whompa/grid transitions (which are
  just zone lines → our existing zone push handles them).
- ⬜ **Grid terminal**: capture the grid-enter/exit use packets (grid is a hub playfield;
  entering/leaving are zone events).
- ⬜ **Router**: BFS/Dijkstra over the graph → sequence of (walk path, take whompa/grid) →
  execute with the movement engine. `travel <destination>` command.

Reuses: saved-path replay, zone push, zone-jump nav reset (all already working).

---

## Cross-cutting (needed for real solo)  ⬜

- ⬜ **Solo combat loop**: pick target (already `GetAssistTarget`/solo picker), engage, keep
  range, heal self, use damage nanos from the known kit, disengage on low HP / adds.
- ⬜ **Looting** after kills; inventory full → trigger M4 sell run.
- ⬜ **Decision state machine**: Idle → TravelToTerminal → RollMission → RunMission → Loot →
  (inventory/credits check) → SellRun → BuyUpgrade → repeat. Config-driven goals.
- ⬜ **Safety leashes**: don't wander, recall/return-to-owner command, stuck detection
  (partly done in movement).

---

## Suggested order of attack
1. ✅ M1 knowledge (done) → verify in-game.
2. M1 refinements (school grouping, QL/NCU/cost, source tags) — pure offline DB work, no capture needed.
3. Capture sessions (owner + me): **mission roll**, **vendor buy/sell**, **grid/whompa use**.
   These three sniffs unblock M3/M4/M5. Same capture method as the zone/login work.
4. Build M3 door atlas + runner, then M4 shop, then M5 router, then stitch the state machine.

Nothing here needs anti-detection (owner is online, own alt) — same stance as the rest of the bot.
