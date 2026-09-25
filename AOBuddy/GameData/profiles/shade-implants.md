# Shade - Implants (there are none)

THERE IS NO SHADE IMPLANT BUILD. The Shade is the only profession in Anarchy Online that cannot equip implants at all; in all 13 implant slots it wears SPIRITS (client ItemClass 5). This file exists to say that on the page, to prove it from the client data, and to show what goes in each slot instead - the full Spirit catalogue, ladder and farm plan is in shade-symbiants.json. The practical consequences are large and mostly in the Shade's favour: (1) no surgery clinic and no Treatment ladder - a Spirit's ToWear carries NO Treatment requirement at all, only Level, Agility, Sense, Profession and an expansion bit, where a QL200 implant needs ~951 Treatment; (2) no cluster building, so Nano Programming (IP cost 4.0 for a Shade, the maximum) is worth exactly nothing to the class; (3) no laddering and no over-equipping - a Spirit's tier is a hard level/ability gate you must genuinely meet, so you cannot buff your way into a higher one; and (4) no shiny/bright/faded choices, so none of the per-slot cluster conflicts that dominate every other profession's implant file apply here. What replaces the ladder is the TIER LADDER: 34 steps from QL1/level 1/ability 3 (Bitter) to QL300/level 219/ability 1,276 (Wistful), each step gated purely on character level and on Agility and Sense.

## Coverage

| | |
|---|---|
| implantsEquippableByAShade | 0 of 44,060 implant templates (ItemClass 3) in items.ocp |
| symbiantsEquippableByAShade | 14 of 1,462 wearable symbiant templates - and 12 of the 14 are the unlocked Xan Artillery-unit client-data anomaly (ids 278908-278919), the other two unlocked one-offs (245306, 245895). No Shade symbiant line exists. |
| spiritsEquippableByAShade | 844 of 844 ItemClass-5 templates; 842 carry an explicit ToWear Profession EqualTo 15 lock |
| slotsCovered | 13 of 13 implant slots take a Spirit |
| whereTheDetailIs | shade-symbiants.json - full 34-tier ladder, the Xan Beta/Alpha families, per-slot acquisition and the farming runs grouped by location |

## What goes in each implant slot instead

| Slot | Implant | Spirit worn instead | id | QL | Spirit types that exist here | Ordinary-tier Spirits |
|---|---|---|---|---|---|---|
| Eye/Ocular | none - a Shade cannot equip one | Xan Spirit of Stealth - Alpha | 279194 | 300 | Spirit of Discerning Weakness, Spirit of Essence, Spirit of True Seeing | 36 |
| Head/Brain | none - a Shade cannot equip one | Xan Brain Spirit of Power - Alpha | 279190 | 300 | Brain Spirit of Computer Skill, Brain Spirit of Offence, Essence Brain Spirit, Spirit of Clear Thought | 51 |
| Ear | none - a Shade cannot equip one | Xan Spirit of Stealth Whispered - Alpha | 279200 | 300 | Spirit of Essence Whispered, Spirit of Knowledge Whispered, Spirit of Strength Whispered | 53 |
| Right Arm | none - a Shade cannot equip one | Xan Right Limb Spirit of Power - Alpha | 279217 | 300 | Right Limb Spirit of Essence, Right Limb Spirit of Strength, Right Limb Spirit of Weakness | 49 |
| Chest/Body | none - a Shade cannot equip one | Xan Heart Spirit of Power - Alpha | 279207 | 300 | Heart Spirit of Essence, Heart Spirit of Knowledge, Heart Spirit of Strength, Heart Spirit of Weakness | 65 |
| Left Arm | none - a Shade cannot equip one | Xan Left Limb Spirit of Power - Alpha | 279209 | 300 | Left Limb Spirit of Essence, Left Limb Spirit of Strength, Left Limb Spirit of Understanding, Left Limb Spirit of Weakness | 70 |
| Right Wrist | none - a Shade cannot equip one | Xan Spirit of Right Wrist  Power - Alpha | 279226 | 300 | Spirit of Right Wrist Offence, Spirit of Right Wrist Weakness | 34 |
| Waist | none - a Shade cannot equip one | Xan Midriff Spirit of Power - Alpha | 279228 | 300 | Midriff Spirit of Essence, Midriff Spirit of Knowledge, Midriff Spirit of Strength, Midriff Spirit of Weakness | 70 |
| Left Wrist | none - a Shade cannot equip one | Xan Spirit of Left Wrist  Power - Alpha | 279215 | 300 | Spirit of Left Wrist Defense, Spirit of Left Wrist Strength | 36 |
| Right Hand | none - a Shade cannot equip one | Xan Right hand Spirit of Will - Alpha | 279224 | 300 | Right Hand Defencive Spirit, Right Hand Strength Spirit, Spirit of Insight - Right Hand | 50 |
| Legs/Thigh | none - a Shade cannot equip one | Xan Spirit of Power - Alpha | 279232 | 300 | Spirit of Defense, Spirit of Essence | 36 |
| Left Hand | none - a Shade cannot equip one | Xan Left Hand Spirit of Power - Alpha | 279213 | 300 | Left Hand Spirit of Defence, Left Hand Spirit of Strength | 34 |
| Feet | none - a Shade cannot equip one | Xan Spirit of Feet Strength - Alpha | 279172 | 300 | Spirit of Feet Defense, Spirit of Feet Strength | 36 |

## The ladder that replaces the Treatment ladder

THE TREATMENT LADDER DOES NOT EXIST FOR A SHADE. reference/implants.json gives the implant equip model as Treatment = trunc(4.723717064 * QL + 6.767295257) plus a governing ability at QL*2+4 - so a QL200 implant needs ~951 Treatment and ~404 of one ability, and you ladder up to it by equipping low slots first and stacking Treatment buffs. None of that applies here. Read from items.ocp, a Spirit's ToWear is: Profession EqualTo 15, Level > (tier level - 1), Agility > (tier ability - 1), Sense > (tier ability - 1), Expansion BitAnd 2 (Shadowlands) - or BitAnd 128 (Legacy of the Xan) on the Xan Spirits. There is no Treatment term, and ao-universe states plainly that Spirits "cannot be over equipped" because the prefix name IS the level requirement. So the only way to wear a better Spirit is to gain levels and to raise Agility and Sense - which is exactly why the recommended breed is Opifex (highest trained caps in those two abilities: Agility 544, Sense 512). The one thing a Shade CAN do that no implant user can: because no surgery clinic is needed, you can hot-swap a nano-skill Spirit in, cast the buff, and swap back out in the field. What you actually "ladder" is the Spirit-farming loop: siphon mobs around the QL you want, open Soul Capsules, and move up the 34-step tier list as your level and abilities allow.

## Quality

For every other profession the QL question is "how high an implant can I ladder into". For a Shade it is "which tier of Spirit does my level and my Agility/Sense allow", and the answer is exact and un-twinkable: see catalog.tiers in shade-symbiants.json, which carries the level and ability requirement of all 34 steps read from items.ocp. Examples from the data: QL100 (Lonely) needs level 73 and 226 in the governing ability; QL200 (Rejected) level 146 and 451; QL250 (Tragic) level 183 and 1,063; QL300 (Wistful) level 219 and 1,276. Note the jump at QL210 - the ability requirement goes from 451 to 683 in one step, then to 1,063 at QL250.

## Grades

Not applicable. Shiny/bright/faded is an implant concept and a Shade equips no implants. A Spirit is a single item with a single bonus set, and the choice inside a slot is by Spirit TYPE (Essence, Strength, Weakness, Knowledge, Defense, Offence, Insight, Clear Thought, True Seeing, Understanding), not by cluster grade. The shiny/bright/faded columns below are filled with "n/a - no implant" on purpose, and the generic cluster reference is carried per slot under _clusterReferenceForThisSlotIfItCouldUseOne only so the page stays comparable with the other classes.

## Where the Shade breaks the class-profile template

- THE WHOLE FILE IS A DEVIATION. Every other [slug]-implants.json is a 13-slot shiny/bright/faded cluster plan with per-slot conflicts. A Shade has none of that, so this file is a proof plus a pointer to shade-symbiants.json. The renderer needs no change: renderImplants() prints build[].shiny/bright/faded, which here read "n/a - no implant", and the summary paragraph above it carries the explanation.
- THE PROHIBITION IS NOT IN THE ITEM CRITERIA. 44,060 implant templates exist in items.ocp and not one carries a profession lock, so a criteria-only check would wrongly report every implant in the game as Shade-usable. This is a trap for any future automated "what can profession X equip" pass: the Shade implant ban is a client/server rule, and the only evidence inside the client data is the description text on Whispering Spirit (id 218576). Any equippable-catalogue extraction for this class MUST exclude ItemClass 3 by hand.
- TREATMENT IS NOT A GATE. Treatment costs a Shade 1.5 IP, which in every other profile means "spend, it is your implant ladder". For a Shade it gates nothing it wears. Spend it only for stims and kits.
- NANO PROGRAMMING IS WORTH ZERO. Cost factor 4.0, and a Shade never builds a cluster. It is the one skill in the table with literally no use for the class.
- NO OVER-EQUIPPING. Every other implant file assumes twinking: equip above your skills with buffs, then drop the buffs. A Spirit refuses this outright, so a Shade's slot power is a pure function of level plus Agility and Sense.
- THE SLOT NAMES DIFFER BETWEEN EXTRACTORS. The implant/Spirit slot bitmask lives in stat 298 and decodes as Imp_Eye 2 / Imp_Head 4 / Imp_Ear 8 / Imp_RightArm 16 / Imp_Body 32 / Imp_LeftArm 64 / Imp_RightWrist 128 / Imp_Waist 256 / Imp_LeftWrist 512 / Imp_RightHand 1024 / Imp_Legs 2048 / Imp_LeftHand 4096 / Imp_Feet 8192. tools/eng-gear-extractor reads the SAME stat as a CLOTHING mask, so in a gear dump every Spirit shows up with nonsense cloth/weapon slots (e.g. "Bitter Spirit of Feet Defense" as RightFinger/Deck5). Filter ItemClass 5 out of any gear pass for this class.

## Deliberately unverified

- No per-slot Shade Spirit sheet from a reputable source was fetched; ao-universe gives a slot -> Spirit TYPE -> buffed-skills table but no "wear this exact id" list. The best-in-slot picks in shade-symbiants.json are therefore derived from the item bonuses against the Shade IP cost table, and are labelled as a stated judgement there.
- The exact Treatment and ability formulas quoted for ordinary implants come from reference/implants.json and are reproduced here only as the contrast case; they are not Shade data.
- Whether any Shade content ever grants a temporary implant slot or an implant-like item outside ItemClass 5 was not investigated beyond the ItemClass filter.

## Sources

- LOCAL E:/Funcom/OmniCell/OmniCell/Datafiles/items.ocp (client 18.8.50_EP1), read three ways: tools/mp-symbiant-extractor --prof 15 (14 usable symbiants of 1,462), --prof 15 --match Implant (44,060 ItemClass-3 templates, 0 with any profession lock), and --prof 15 --itemclass 5 (844 Spirits, 842 Shade-locked).
- LOCAL items.ocp id 218576 "Whispering Spirit" description: the client's own statement that Shades cannot use implants and use captured spirits instead, and that Spirits do not accept over-equipping.
- LOCAL E:/Funcom/attic/extracted-client-data/itemnames.sql: id -> name verification.
- LOCAL AOBuddy/GameData/profiles/reference/implants.json: the cluster-per-slot reference carried under _clusterReferenceForThisSlotIfItCouldUseOne, and the implant Treatment/ability equip model used as the contrast case.
- LOCAL AOBuddy/GameData/profiles/reference/skillcaps.json (Shade = index 11): Treatment 1.5, Nano Programming 4.0.
- LOCAL AOBuddy/GameData/profiles/shade-symbiants.json: the Spirit catalogue, tier ladder and per-slot best-in-slot rows this file points at.
- https://www.ao-universe.com/guides/shadowlands/professions-guides/comprehensive-shade-guide (Shades cannot use standard implants; no surgery clinic; Spirits cannot be over-equipped; the QL/level/ability prefix ladder).
- http://wiki.aodb.us/wiki/Shade (in place of implants "you will find trapped Spirits"; no surgery clinic needed).
