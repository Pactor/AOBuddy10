# Meta-Physicist — Armor & Gear (non-weapon, non-symbiant)

Profession 12. Companion to `metaphysicist-gear.json`. Weapons and symbiants/implants are covered
in the separate `metaphysicist-weapons.*` and `metaphysicist-implants.json` profiles and are
excluded here.

## What an MP actually gears for

The MP is a pure nano/pet caster. It tanks with its pets, not its body, so armor is picked for
**casting output first, ACs last**. Priority stats (Stat.cs ids for AOBuddy):

- **Nano skills** — MM/BM/PM/MC/TS/SI (`MaterialMetamorphosis 0x7F` … `SpaceTime 0x83`,
  `SensoryImprovement 0x7A`). These gate which pets/nukes/buffs it can cast.
- **Intelligence + Psychic** (`0x13` / `0x15`) — raise all the nano skills; Nanomage-favoured.
- **Nano Init** — `NanoCInit 0x95` — faster casts / recasts.
- **Max Nano / nano pool** (`MaxNanoEnergy 0xDD`, `NanoPool 0x84`) and **nano-cost reduction**
  (`NPCostModifier 0x13E`) — sustain.
- **Max NCU** (`0xB5`) — to hold its own buffs plus wrangles/team buffs.

## Leveling armor (Rubi-Ka → Shadowlands)

| Band | Set | Why |
|------|-----|-----|
| ~20–75 | **Carbonum Armor** | cheap tradeskilled AC filler; no nano bonuses |
| ~25–100 | **Living Cyber Armor** | first cheap nano-skill set |
| ~30–100 | **Sekutek Chilled Plasteel** | STR/AGI/INT to make equip/travel reqs |
| ~60–130 | **Loren's / Azure Reveries** | budget nano-skill + nano-pool set |
| ~60–205 | **Metaphysicist Suit (SL)** → **Chosen / Faithful** | free no-drop profession quest set; upgradeable First→Second Tier→Chosen(Sanctuary)/Faithful(Redeemed); has Back/Wings + Shoulder; Max Nano ~150 (QL100) → ~300 (QL160) on First Tier |

The Shadowlands **Metaphysicist Suit** line is the recommended leveling backbone once in SL: it's
free, self-only, and upgrades with you until you can afford the endgame sets.

## Endgame armor (pick / mix)

- **Miy's Nano Armor** *(high — froob-friendly)* — the nano variety: Int ~44 / Psychic ~43,
  Max Nano ~375, NCU ~44, **Nano Init ~50**, all six nano skills ~+16 (QL300). Biggest Int/Psychic
  on the **Head and Arm** pieces. Drops from Medusae. Still competitive at endgame and often mixed
  with Combined pieces.
- **Combined Officer's Armor** *(high — endgame BiS)* — the caster alien armor, tradeskilled from
  **Arithmetic + Spiritual**. QL300 example: NanoPool ~350, Max Nano ~1400, Max NCU ~105,
  Nano-cost ~-21%, every nano **and** trade skill ~+210. QL you can wear is capped by the Alien
  Technology Expertise perk (~QL225 with AT Expertise 2). Requires AI raids + high tradeskills.
- **Ofab Metaphysicist Armor** *(high — convenience)* — profession Battlestation set bought/upgraded
  with Victory Points (Ofab → Improved → Penultimate), plus a matching Shoulder Wear piece. Easy
  full set; good alternative/supplement to Combined/Miy's.

A common endgame mix is **Combined body/legs/feet** for the heavy bonuses + **Miy's head/arms** for
the concentrated Int/Psychic/Nano-Init, with **De'Valos Sleeves** (+30 all nano skills) as a swap.

## Key nano-focus pieces (headwear / HUD / rings)

- **Head:** Miy's Nano Helmet (Int/Psychic/nano skills), Ofab Metaphysicist Headgear (id 264359),
  Helmet of Azure Reveries (id 165307, budget).
- **HUD:** **Nano Targeting Helper** (id 269184 — +nuke damage), **Metaphysicist NICS** (id 215235 —
  profession HUD). Notum Splice (id 204649 — util; verify effect).
- **Rings:** **Custom-Made Metaphysicist Ring of Focus** (id 163658) and **Ring of the Occult**
  (id 163660) — MP profession rings; **Ring of the Nucleus Basalis** (id 202717) / **Superior**
  variant — nano-skill ring.
- **NCU / belt:** Belt Component Platform (id 303993 = 300X) + Accelerated NCU Memory chips
  (id 164603). Grid Armor Mk I–IV (id 155172) is a situational full-body for NCU/run/evades but
  blocks the Body/Back/Arm nano-skill slots — not typical MP BiS.
- **Arms swap:** **De'Valos Sleeves** — up to +30 to all six nano skills (AI tradeskill).

## Verification & unverified flags

IDs were checked against the **local** `itemnames.sql` extract where present (Ofab MP set, SL suits,
Azure Reveries, Sekutek, Living Cyber, Carbonum, the two MP rings, Ring of the Nucleus Basalis,
Metaphysicist NICS, Nano Targeting Helper, Notum Splice, Belt Component Platform, Accelerated NCU
Memory, Grid Armor). That extract is **partial** — **Miy's Nano, Combined Officer's, and De'Valos
Sleeves are NOT in it**; their ids/stats come from aoitems/auno/aodb-wiki and are cited.

Flagged unverified (see `_unverified` in the JSON): exact per-QL stat lines for the Ofab MP set and
the SL suits; the Notum Splice / Metaphysicist NICS stat blocks; and several **Funcom-forum-summary
back-slot cloaks** (Neleb's Nano-circuit/Nano-master Robe, Reanimator's Cloak / Cloak of the
Reanimated Summoner) and other names (Nano Formula Recompiler, Phasing Nano Input Hood, Spirit-Infused
Yuttos NCU, XtremTech's Ring of Casting) that could not be confirmed and were **kept out of the main
lists** — verify on aoitems before adding.

## Sources

- wiki.aodb.us — Meta-Physicist:Armor; Miy's Nano Armor; Combined Officer's Armor; First Tier
  Metaphysicist Suit
- ao-universe.com — classic-ao & alien-invasion armor/tradeskill guides (leveling progression;
  De'Valos Sleeves; Alien Armor Process and List)
- anarchyonline.fandom.com — Alien Armor / Armorsets
- forums.funcom.com/t/mp-meta-physicist-guide/74897 (leads only; unconfirmed names flagged)
- aoitems.com / auno.org — id verification (Miy's Nano Body Armor 268837 / 268836)
- LOCAL: `E:\Funcom\attic\extracted-client-data\itemnames.sql`;
  `E:\Funcom\AOBuddy10\AOSharp.Common\GameData\Stat.cs` & `EquipSlot.cs`
