# Shade - Nano sources

Where each of the 79 Shade-castable nanos comes from. Derived by locating each nano's CRYSTAL item in `items.ocp` (the item whose UploadNano function argument is the nano id) and reading that crystal's own criteria.

## Method

{
 "expansionGate": "Read the Expansion(stat 389) BitAnd requirement from the nano program's ToUse action AND from its nano-crystal item (items.ocp) use/wield criteria. bit1(2)=ShadowLands, bit3(8)=AlienInvasion, bit5(32)=LostEden (AOSharp ExpansionFlags). Authoritative from client data.",
 "crystalLink": "nano id -> crystal item id via scanning items.ocp for an UploadNano(53019) function whose argument is the nano id.",
 "vendorData": "Shop presence = crystal item id present in OmniCell research-output vendors.csv. NOTE: local vendor data currently covers only researched playfields (Temple of Three Winds), so general nano-shop buyability is NOT separable from local data; such low/mid nanos fall to the mission-roll default below.",
 "defaultRule": "AO default (cited, not invented): a non-shop, non-expansion profession nano crystal is mission-rollable. Applied only when no Expansion gate and no local shop entry exist.",
 "noInvention": "No vendor, mob, garden, or tier is asserted unless it comes from local data. Unknown SL sub-sources and tiers are left null."
}

## How counts

{"mission-roll": 77, "shadowlands": 2}

| id | Nano | how | where | expansion |
|---|---|---|---|---|
| 210321 | Fervor of the Henchman | mission-roll | None | - |
| 210323 | Fervor of the Devotee | mission-roll | None | - |
| 210325 | Fervor of the Minion | mission-roll | None | - |
| 210327 | Fervor of the Disciple | mission-roll | None | - |
| 210329 | Fervor of the Fanatic | mission-roll | None | - |
| 210331 | Fervor of the Zealot | mission-roll | None | - |
| 210353 | Ritualistic Touch | mission-roll | None | - |
| 210355 | Ritualistic Blow | mission-roll | None | - |
| 210357 | Ritualistic Grasp | mission-roll | None | - |
| 210359 | Ritualistic Caress | mission-roll | None | - |
| 210361 | Ritualistic Embrace | mission-roll | None | - |
| 210363 | Sacrificial Touch | mission-roll | None | - |
| 210365 | Sacrificial Blow | mission-roll | None | - |
| 210367 | Sacrificial Grasp | mission-roll | None | - |
| 210369 | Sacrificial Caress | mission-roll | None | - |
| 210371 | Sacrificial Embrace | mission-roll | None | - |
| 210376 | Quintessence of Incapacitation | mission-roll | None | - |
| 210378 | Quintessence of Paralyzation | mission-roll | None | - |
| 210380 | Quintessence of Petrification | mission-roll | None | - |
| 210387 | Rudimentary Dissipation | mission-roll | None | - |
| 210389 | Primitive Dissipation | mission-roll | None | - |
| 210391 | Intrinsic Dissipation | mission-roll | None | - |
| 210393 | Elemental Dissipation | mission-roll | None | - |
| 210395 | Primordial Dissipation | mission-roll | None | - |
| 210401 | Degeneration of Rapidity | mission-roll | None | - |
| 210407 | Degeneration of Celerity | mission-roll | None | - |
| 210732 | Slap of the Zombie | mission-roll | None | - |
| 210734 | Knock of the Poltergeist | mission-roll | None | - |
| 210736 | Kick of the Phantom | mission-roll | None | - |
| 210738 | Hand of the Shadow | mission-roll | None | - |
| 210740 | Touch of the Specter | mission-roll | None | - |
| 210742 | Voice of the Banshee | mission-roll | None | - |
| 210744 | Kiss of the Vampire | mission-roll | None | - |
| 210746 | Sneak | mission-roll | None | - |
| 210748 | Mugger | mission-roll | None | - |
| 210750 | Scrounger | mission-roll | None | - |
| 210752 | Prowler | mission-roll | None | - |
| 210787 | Footpad Apprentice | mission-roll | None | - |
| 210789 | Silent Dagger | mission-roll | None | - |
| 210791 | Dagger in the Back | mission-roll | None | - |
| 210793 | Backpiercer | mission-roll | None | - |
| 210795 | Shuffle of the Rogue | mission-roll | None | - |
| 210797 | Backstabber | mission-roll | None | - |
| 210804 | Dual Defender | mission-roll | None | - |
| 210806 | Double Cover | mission-roll | None | - |
| 210808 | Twice the Shield | mission-roll | None | - |
| 210810 | Double Fence | mission-roll | None | - |
| 210812 | Duplex Wall | mission-roll | None | - |
| 211142 | Puncture of the Tarasque | mission-roll | None | - |
| 211144 | Master Piercer | mission-roll | None | - |
| 211146 | Probe of Death | mission-roll | None | - |
| 211148 | Piercing Gash | mission-roll | None | - |
| 211150 | Piercing Tooth | mission-roll | None | - |
| 211152 | Sharpen Dagger | mission-roll | None | - |
| 211154 | Whetstone Effect | mission-roll | None | - |
| 224159 | Primal Dissipation | mission-roll | None | - |
| 224161 | Chthonic Dissipation | mission-roll | None | - |
| 224163 | Ceremonial Grasp | mission-roll | None | - |
| 224165 | Ceremonial Caress | mission-roll | None | - |
| 224167 | Ceremonial Embrace | mission-roll | None | - |
| 224169 | Quintessence of Transfixion | mission-roll | None | - |
| 224171 | Quintessence of Stupefication | mission-roll | None | - |
| 224177 | Degeneration of Haste | mission-roll | None | - |
| 225281 | Symbiosis | mission-roll | None | - |
| 266300 | Shade's Caress | mission-roll | None | Lost Eden |
| 266310 | Pierce Nerves | mission-roll | None | Lost Eden |
| 270804 | Improved Puncture of the Tarasque | mission-roll | None | - |
| 272371 | Faster than your Shadow | mission-roll | None | - |
| 273390 | Sneaking Health Drain | mission-roll | None | - |
| 273393 | Improved Prowler | mission-roll | None | - |
| 273395 | Shadow in the Night | mission-roll | None | - |
| 275839 | Nanite Depravation | mission-roll | None | - |
| 275841 | Basic Nanite Depravation | mission-roll | None | - |
| 275843 | Triple Wall | mission-roll | None | - |
| 281239 | Release Me Now | mission-roll | None | Lost Eden |
| 297342 | Spirit Siphon | shadowlands | None | Shadowlands |
| 301160 | Smoke Bomb | shadowlands | None | Shadowlands |
| 301593 | Winding Serpent | mission-roll | None | Lost Eden |
| 301895 | Dissolving Vitality | mission-roll | None | - |

## Sources

- local:nanos.ocp (nano program ToUse Expansion criteria)
- local:items.ocp (nano-crystal UploadNano link + crystal Expansion criteria)
- local:itemnames.sql (names)
- local:OmniCell/research-output vendors.csv (shop presence, researched playfields only)
- rule:AO default - non-shop non-expansion profession nano crystals are mission-rollable
