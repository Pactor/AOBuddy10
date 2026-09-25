# Doctor — Endgame

Profession ID **10** · Symbiant unit type: **SUPPORT** · Level cap 220 (+ AI title levels, + LE research levels).

## A Doctor is a required raid role, not a flexible slot

This is the single biggest difference between this file and the pet-class equivalent. The MP endgame file's honest conclusion was *"MP is a flexible utility/DPS slot in raids, not a required role like tank or doc."* This file is the other side of that sentence: **every raid below needs at least one Doctor and usually two or three, and the raid stops when they die.** Raids are planned around how many Doctors are online.

### What a raid actually wants, in order

1. **Complete Healing** (28650) / **Improved Complete Healing** (270747) held for the tank's spike; **Alpha and Omega** (42409) held for the raid's. These are **resets, not rotation buttons** — AOWiki says complete heals *"always heal for 10001 health, ignoring heal modifying buffs"*, so they are worth the most on someone nearly dead and almost nothing on someone at 80%.
2. **The top team HP buff** (Superior Omni-Med Enhancement 95709) up before every pull and re-cast after every wipe.
3. **An unbreakable init debuff on the boss** (UBT 99577, else Rapid Palsy 301845). Unbreakable means it *"lasts the full duration of the nano or until the target dies, regardless of damage dealt"* — a **raid-wide damage reduction dressed as a debuff**. Land it *before* you start healing.
4. **Improved Instinctive Control** (222856, +350 Nano C. Init) handed to every other caster in the raid. One cast each, biggest non-healing contribution you make.
5. **Iron Circle** (42400) and **Superior First Aid** (28675) as standing team buffs.
6. **The best Vaccine** (204431) in any fight with drains.
7. **A rolling HoT** (Team-Enhanced Deathless Blessing 269455) between real casts.

Everything else — the three DoT strains, Malpractice (275701), the pistol — is what you do in the gaps.

## Content

| Content | Doctor-relevant loot | Role |
|---|---|---|
| **The Xan (12-man)** / Hall of the Elders, 4h lockout | **Xan SUPPORT Betas** across ear/eye/chest/thigh/right arm/right hand/feet — the first half of every Alpha. Plus spirits, Xan Belt Tuning / Weapon Upgrade / Nanodeck Activation devices, "The Awakening" 5h buff. | **Primary healer, and the encounter targets you for it.** The guide: healers *"must be on their toes because he hits hard"*; the Left Hand's fear makes the team flee and is *"unavoidable unless you have a keeper/fixer"*; **doctors get morphed into white birds or silvertails** (you cannot heal); and below 50% the Deranged Xan **mind-controls doctors** — a controlled Doctor can be made to cast UBT on its own raid. |
| **Pandemonium** (needs a **+10 Pande ring**) | The Night Heart drops **Intelligent QL300 Support symbiants** — the second half of every Alpha. | Primary healer across 18 bosses / 4 islands; nano management matters as much as heal size. |
| **The Beast** (wiki: level 300, 9h respawn, in Pandemonium) | **STAR OF RECOVERY (244699)** — the Doctor-locked Beast star, **13.46%** over 104 recorded kills. Verified from items.ocp: QL250, NODROP, Profession = Doctor, CompLit 1499 + Level 199, gives **BM +30, Treatment +30, Stamina +30, Pharma Tech +60** — and fits a huge slot list (Neck/Head/Back/Shoulder/Body/Finger/HUD1-3/Utils1-3). | Primary healer. One of the few pieces of endgame loot that exists **solely** for this class. |
| **Dust Brigade DB1 / DB2 / DB3** | Enhanced DB Armor, and the two **bracer lines** that are among the best Doctor wrist items in the game: right-wrist Infused (274541 → 274551, up to **+4 Heal Multiplier / +1200 HP / +1200 nano**) and left-wrist (292566 → 292564, **+2 / +1200 / +1200**). **Different wrists — run both.** | Primary healer; the team's stated minimum is literally "tank plus doc". DB3 is a **nano-endurance** fight. |
| **Alien City Raids** | Kyr'Ozch bio-material and viralbots → **Combined Paramedic's** (246631-246656): **+200 HP, +50 Body Dev, +200 nano, +50 Nano Pool, −3% nano cost per piece**. | Primary healer on long AoE-heavy content — what the team HP buff and team HoT were made for. |
| **Sector 7** (gates ~every 7h for ~10 min) | 16 NODROP profession-restricted Special Edition weapons — including the **Special Edition Kyr'Ozch Pistol (288293)**, whose lock set includes Doctor. VP items QL160-270. | Team healer. Check every SE drop against the Doctor lock set (23 Doctor-locked weapon models exist, 17 of them pistols). |
| **Sectors 10 / 13 / 28 / 35** | Alien loot, bio-material, VP. | Raid healer. |
| **Sector 42 (APF / Zodiac)**, level 210+ | APF nano items; the Artillery Commander needs three bosses killed simultaneously. | The raid **splits**, so multiple Doctors are mandatory — one per group, each alone with its group. |
| **Battlestation** | **Victory Points → the Ofab Doctor set** (264646-264681) plus the back (267933: +50 Treatment, **+2 Heal Multiplier**) and shoulder (301697: **+20 BM**), and the Special Edition helmet (267352). VP also buys the QL300 ring, SE head and Ofab back at 220. | PvP healer — the most valuable **and** most focused target on the field. That is what Improved Nano Repulsor (222823), the Vaccine line and **Misdiagnosis (266306, Heal Multiplier −50 on an enemy healer)** are for. |
| **Inferno missions / pocket bosses** | 9 QL220-255 patterns for the Galahad Garden key; tokens. | Team healer; solos slowly by DoT-and-outlast. |
| **Penumbra missions** | Shadowbreed gear, tokens, step toward Pande access. | Team healer. |
| **LE / alien dailies** | Tokens and research/AI XP — which matters disproportionately here (see step 5 below). | Healer on fast runs. |

## Progression order

1. **Nano skill foundation — BM and MM above everything.** The rungs: Complete Healing BM 794 / MM 722 · Alpha and Omega BM 873 / MM 786 · UBT BM 861 / PM 861 · Superior Omni-Med Enhancement BM 852 / MM 769 / SpaceTime 769 · Improved Complete Healing BM 2153 / MM 2153. *A Doctor with better gear and lower BM heals for less than one with worse gear and higher BM.*
2. **Treatment ladder → QL200+ implants → SUPPORT symbiants** toward a QL240+ baseline. Support is the **only** unit line a Doctor can wear — verified from items.ocp: every Support symbiant's ToWear locks stat 60 to {MartialArtist, Fixer, Adventurer, Trader, **Doctor**, Meta-Physicist, Keeper}, and Doctor appears in no other line's set.
3. **Ofab Doctor via Battlestation VP** (Ofab → Improved → Penultimate), running Chosen/Faithful Doctor Suit meanwhile. Ofab is the only set giving Treatment + First Aid + Heal Multiplier + Max NCU + all four relevant nano skills at once — and it is bought with *time*, not RNG.
4. **Heal Multiplier items.** Ancient Resorative Fungus (302925, +5, level-200-only) · Nano Assistant (269187, +5, no lock) · Sacred Chalice (305523, +7) · Sheffy's Micro Coil (267563, +5 plus +20 BM/+20 MM/+100 Nano C. Init) · Doctor's Left Hand of Grace (267618, +5) · Star of Recovery (244699) · Cloak of the Reanimated Healer 5/5 (274707, +3 plus all six nano skills +18) · both DB bracer lines. **Only 66 items in the whole Doctor-wearable dump grant Heal Multiplier, and no other class competes for them.**
5. **Perks and research to completion.** Assault Force Medic (10) · Nano Surgeon (6) · Specialist Healer (10) · Nano Doctorate (10) · **Champion of Nano Combat (10)** · **Embrace (10)**; LE research to L9-10; AI 24-30. Between them: roughly **+285 BM and +285 MM, +100 Treatment, +70 First Aid, +21% healing**, plus Underground Doctor's 5% healing efficiency — permanent, no drop RNG, **larger than most gear upgrades**.
6. **Combined Paramedic's** (six pieces = +1200 HP, +300 Body Dev., +1200 nano, +300 Nano Pool, −18% nano cost). Mix with Ofab per slot rather than wearing one set whole.
7. **Pande access ring (+10)** → **Xan Support Betas** from the Xan 12-man (the only source; ear and thigh Betas are adaptable between unit types).
8. **Intelligent QL300 Support symbiants** from the Night Heart. Few drop per kill — this is the bottleneck.
9. **Xan Support Alphas** (crafted on yourself, NODROP): Brain **278996**, Ocular 278997, Ear 278998, Chest 278999, Left Arm 279000, Left Hand 279001, Left Wrist 279002, Right Arm 279003, Right Hand 279004, Right Wrist 279005, Waist 279006, Thigh 279007, Feet 279008. *The Brain Alpha alone is +144 BM and +144 Treatment.*
10. **Dust Brigade chain, Star of Recovery, Arid Rift / Neretva.** The long tail.

## Notes

- **Nano endurance decides long fights, not heal size.** Ofab Doctor Body +1000 Max Nano · Combined Paramedic's +200 and −3% per piece · Sacred Chalice +1000 and −5% · Ancient Resorative Fungus +1000 and −10%. Carry nano kits — Treatment is cost **1.0** for you, so you can use the highest QL in the game.
- **NCU is a real endgame constraint.** You carry the team HP buff, Iron Circle, Improved Instinctive Control, Improved Nano Repulsor, a Vaccine and a heal-delta buff *before anyone else offers anything*. Memory of Future Events (305516, +200), Memory NCU 6/6 (278782, +192) and Spirit Infused Yuttos Modified NCU (267797, +125) exist for exactly this.
- **Gate items:** +10 Pande ring before the Intelligent QL300 grind · Level 210 before rolling 210+ APF items · Level 219 for Sheffy's Micro Coil · Level 220 for Bodily Invigoration (223299) · `Specialization BitAnd 8` on every top SL heal, so finish the spec chain.
- **Plan for the Xan 12-man disabling you.** Bring a keeper or fixer for the fear, and expect to be morphed or mind-controlled at the worst moment.

## Where a Doctor differs from the MP template

- A **`raidRoleSummary`** block exists because the answer to "what does a raid want from this class" is concrete and rankable — the MP file's honest answer was that an MP is not a required role.
- **Every content row's role says "healer."** That is the finding, not laziness. Where the role genuinely differs it is stated as a mechanic (Sector 42 splits the raid; DB3 is nano endurance; the Xan 12-man actively disables you).
- **Symbiant line is SUPPORT**, with the verified evidence named rather than asserted.
- **Combined Paramedic's is an HP/nano set with no nano-skill bonuses** — the opposite profile to the MP's nano-skill Combined.
- **A new progression step exists: Heal Multiplier items** — a distinct farm from armor or symbiants, for a stat no other class contests.
- **Step 1 is nano skill toward named heal rungs quoted from local castReqs**, not a twink toward a pet ceiling quoted from a forum post.
- **The Beast entry names a fully verified Doctor item** (Star of Recovery, read from items.ocp) where the MP file could only flag "Star of Moral" as unverified.

## Flagged / unverified

- The Beast's **13.46%** is the wiki's figure over 104 kills; the **item** is verified from items.ocp, the **rate** is not. The wiki page gives no encounter mechanics, group size or gear thresholds, so none are stated.
- "The Beast is level 300" refers to the **mob**, not a player requirement (player cap is 220).
- **Carried over from `metaphysicist-endgame.json` (not re-fetched):** Pandemonium's structure and the Night Heart, the Alpha crafting rule, Sector 7's gate timing and boss count, Sector 42's three-simultaneous-boss mechanic, DB1/DB2/DB3 naming and floors, the Galahad pattern count, and the ~84k VP figure. That file cites wiki.aodb.us, ao-universe.com and athenpaladins.org for them. **The Doctor-specific loot and role columns were written fresh against local data.**
- Per-sector loot for 10/13/28/35 is not enumerated from a loot table — inherited gap.
- The Xan 12-man Doctor mechanics **were** fetched this pass. Note that the same fetch glossed "UBT" as *"Uplifting Burst of Technology"* — **that is wrong and is not recorded**; UBT is Uncontrollable Body Tremors, id 99577, verified locally.
- Battlestation VP costs per Ofab tier, and the VP prices of the 220 purchases, are not sourced.
- **Heal Multiplier's unit and stacking were not derived** — step-4 items are ranked by raw integer bonus, which may not be how they combine in game.
- That the Special Edition Kyr'Ozch Pistol drops specifically from Sector 7 bosses is a carried-over attribution, not a verified drop table. Its **Doctor lock set** is verified from `doctor-weapons.json`.
