# Shade - Build (profession 15)

SHADOWLANDS-ONLY. A Shade cannot be created on a free (froob) account, so there is no free-account Shade path at any level. Supported by the data: the profession's signature nano Spirit Siphon (id 297342) carries an Expansion(389) BitAnd 2 = Shadowlands requirement in its ToUse action, and the Shade's implant-slot items (Spirits) carry the same Shadowlands bit on their ToWear action.

## Playstyle

**Role.** Dual-wield Piercing melee striker. No pets, no implants, no symbiants, no taunt and no team heal - the Shade is pure single-target damage that survives on evades, Add-All-Def and a wall of weapon procs.

The Shade (profession 15) is one of the two Shadowlands-only professions. It wields two Piercing weapons, opens from behind with Sneak Attack (skill 146, the Backstab-style opener), sustains with Fast Attack and Dimach, and leeches health back with proc and drain nanos. It has the lowest possible IP cost (1.0) on Piercing, Multi Melee, Sneak Attack, Dimach, Evade-ClsC, Melee Init., Concealment, Run Speed and Tutoring, and the maximum cost (4.0) on EVERY ranged weapon skill and on every other melee weapon skill except Martial Arts (1.6) - so the weapon choice is not a preference, it is the only affordable one. Its 79 castable nanos (shade-nanos.json) are almost entirely self-buffs and offensive procs; it gives no buffs to other players. In the implant slots it wears Spirits, and for armour it wears Tattoos, Body Wraps and Ofab Shade armour.

## Strengths

- Highest burst opener in the game from behind a target (Sneak Attack, IP cost 1.0); on every Shade-locked weapon line the Sneak Attack requirement is exactly half the Piercing requirement.
- Evade-ClsC, Duck-Exp, Concealment and Run Speed are all cheap (1.0-1.2), and the perk lines Lithe, Acrobat, Shadow, Careful in Battle and Sublime Rapport all stack evades on top of that.
- 21 offensive weapon procs in the ShadeProcBuff nano line alone plus two health-drain nanos, which is how a Shade sustains without a heal.
- Spirits (its implant substitute) need no surgery clinic and no Treatment ladder, so the Shade skips the entire implant-twinking game every other profession has to play.
- Self-buffs its own weapon skill hard: the ShadePiercingBuff line tops out at Puncture of the Tarasque (id 211142, +140 Piercing) and Improved Puncture of the Tarasque (id 270804, +200 Piercing, +30 Multi Melee, +140 Sharp Objects).

## Weaknesses

- Locked to one weapon skill. Every ranged skill and 1hB/1hE/2hB/2hE/Melee Energy cost 4.0 IP - the maximum in the table - so there is no viable second weapon build.
- No pets, no taunt, no team heal and no buff to give. The Shade brings damage and nothing else.
- Cannot use implants at all, so it cannot twink equip requirements the way every other profession does; and a Spirit cannot be over-equipped, so Spirit QL is hard-capped by level and ability.
- Very expensive defensive skills where it matters: Body Dev. 2.6 (the highest of any profession in reference/skillcaps.json) and Dodge-Rng 2.4, so HP is thin and ranged attacks hurt.
- Nano Pool 2.5, Nano C. Init 2.8, Matter Creation and Matter Met 3.2, Nano Programming 4.0 - it is not a caster in any sense and cannot build its own implants or clusters either.
- Needs to be BEHIND the target for its opener, which makes it fragile the moment a mob turns around, and useless as an off-tank.

## Tips

- Keep Piercing at the top of the IP list at all times - it is both your damage skill and the wield requirement on every Shade weapon; the QL of weapon you can hold IS your damage.
- Multi Melee (cost 1.0) is what lets you hold the off-hand weapon. Counterweight, Acrobat and Stiletto Mastery all add to it.
- Keep an offensive proc up at all times. The ShadeProcBuff families (Ritualistic/Sacrificial/Ceremonial, Dissipation, Quintessence, Degeneration) are separate nano strains, so several can run together.
- Psychological Modification gates 73 of your 79 nanos (max requirement 1,840) and costs only 1.4 - it is the one nano school worth real IP. Matter Creation (3.2) gates exactly one nano; skip it.
- Spirits do not accept over-equipment (client description text on Whispering Spirit id 218576), so plan Spirit QL against your real Agility/Sense and level, not against a buffed number.
- THE SHADE DANCE: only the first perk of a chain needs you behind the target (patch 17.9), and chains check the user, not the target - so reposition once per chain and let the rest fire from the front, even if the chain finishes on a different mob.
- BACKSTAB needs four things at once: Sneak Attack 100+, a weapon that carries the Sneak Attack special in its ToWield, you behind the target, and the target already engaged with another combatant - so in a team it fires off the tank's aggro.
- PICK THE PROC FOR THE FIGHT: ao-universe says stun proc for PvP and ordinary mobs, damage proc for team damage, and the INITIATIVE-DEBUFF proc (Degeneration of Rapidity/Celerity/Haste) for solo aliens and bosses, which are immune to stuns - it interrupts their nukes.
- Survivability is LIFESTEAL, not healing: the Ritual/Devour perks, Sneaking Health Drain, the Siphon Being and Sap Life procs, Shade's Caress, Diffuse, Exultation and Dimach. A Funcom-forums Shade says Deflect is not worth IP.
- KITING is a real Shade leveling technique: ao-universe/wiki suggest 2 points in Sublime Rapport plus 2 in Totemic Rites (damage plus recovery), or DoT perks with 2 in Spirit Phylactery, and say the trick is finding a clean kiting circle rather than using the standard movement keys. It lets a Shade kill well above its level without twinked gear.
- Computer Literacy is DARK BLUE for a Shade and is the real gate on NCU and on the Pandemonium belt (around 1,750 Comp. Lit. at level 200). Budget for it early - Apotheosis 1 alone is +50.

## Breed

**Opifex.** Derived from reference/breeds.json plus reference/skillcaps.json, not from a guide. Every skill a Shade lives on keys off Agility and Sense: Piercing is 20% Str / 50% Agi / 30% Sta, Multi Melee 30/60/10, Sneak Attack 80% Agi / 20% Sen, Fast Attack 60% Agi / 40% Sen, Dimach 100% Sense, Riposte 50/50 Agi/Sen, Concealment 30% Agi / 70% Sen, and all three evades 50% Agi / 20% Sta / 30% Psy. Opifex has the highest trained caps in exactly those two abilities (Agility 544, Sense 512, against Solitus 480/480) and the highest creation base (Agi 15, Sen 10). Nothing in the Shade kit wants Intelligence or Psychic enough to pay for a Nanomage.

The Opifex pick derived here from ability caps vs skill dependencies is what ao-universe recommends independently: "The most popular breed for Shade is Opifex. The slight-framed homo opifex has highly developed senses and is very agile, and these two attributes are central to the fighting style of the shade." Its breed-statistics page adds that Opifex has the highest evades and "very easy equipping of... shades' spirits". Solitus is called the balanced choice; Atrox is recommended specifically for PvP-with-AI players (best health per point, Mongo Rage) with low Sense as the cost; Nanomage is a differentiation pick with low Agility and low health, whose one draw is the Nanomage-only Dreadloch Endurance Booster. Shadowbreeds unlock at level 205/210/215 and are per-BREED and per-faction, not per-profession.

| Alternative | Note |
|---|---|
| Solitus | Even 480 caps across the board. Loses ~64 Agility and ~32 Sense of headroom against Opifex but gains Strength and Stamina, which matter for the Piercing (30% Sta) and Melee Init. (60% Sta) components and for Body Dev. - the Shade's most expensive survivability skill at cost 2.6. |
| Atrox | Most HP per Body Dev. point, which partly offsets the Shade's 2.6 Body Dev. cost, and the highest Strength/Stamina caps. Costs you the Agility and Sense the whole kit is built on. A survivability pick, not an optimum. |
| Nanomage | Worst fit. Its advantage is Intelligence/Psychic, and the Shade's only Int/Psy-gated content is its own self-buff nanos, whose top requirement is 1,840 Psy Mod - reachable on any breed with gear. Lowest HP of the four breeds on a class that already has the most expensive Body Dev. in the game. |

## Skill priorities

IP cost factors are from `reference/skillcaps.json`, Shade = `_professionOrder` index 11.

| Skill | Target | Why |
|---|---|---|
| Piercing | cap | IP cost 1.0 - the cheapest it can be. Both the damage skill and the wield requirement on all 89 Shade-locked Piercing weapons; the top one (Bloodlust QL300, id 247069) needs Piercing 2,000. |
| Multi Melee | cap | IP cost 1.0. Gates the OFF-HAND weapon. Only 6 templates in items.ocp carry a Multi Melee ToWield criterion and none is Shade-usable, so this is a character skill, not a weapon requirement. |
| Sneak Attack | cap | IP cost 1.0. The opener. On the Shade-locked lines the Sneak Attack requirement is exactly half the Piercing requirement (Bloodlust QL300: Piercing 2,000 / Sneak Attack 1,000). |
| Evade-ClsC | cap | IP cost 1.0 and the evade that matters against melee mobs. Perk lines Lithe, Acrobat, Shadow, Shade Touch and Sublime Rapport all add to it. |
| Melee Init. | cap | IP cost 1.0. Sets your attack speed against the weapon attack/recharge. (Physic. Init costs 3.4 - do not confuse the two.) |
| Dimach | cap | IP cost 1.0, 100% Sense. An allowed special on most Shade weapon lines, buffed by the Sublime Rapport perk line and by the MartialArtsBuff nano line (Kiss of the Vampire, id 210744, +140 Dimach). |
| Concealment | cap | IP cost 1.0. Shadow in the Night (id 273395) alone gives +750. Needed to set up the opener and to move through Shadowlands camps. |
| Run Speed | cap | IP cost 1.0, and Faster than your Shadow (id 272371) gives +250. Positioning is the Shade defence. |
| Duck-Exp | high | IP cost 1.2 - the second-cheapest evade. |
| Fast Attack | high | IP cost 1.4. The sustained-damage special; the Killing Blows research line adds to it. |
| Riposte | high | IP cost 1.4. Passive in combat, but a hard ToWield requirement on the Sigd, Bloodlust, Bronto Vet Lancet, Thousand Stings and Nippy John Stiletto lines (typically 40% of the Piercing requirement) - so it is a WIELD gate, not just a defence. |
| Psychological Modification | to 1,840 | IP cost 1.4 and the gate on 73 of the 79 Shade nanos (shade-nanos.json), topping out at 1,840. The only nano school worth real IP. |
| Nano Resist | high | IP cost 1.5. Shadowlands and endgame content is full of casting mobs and a Shade has no defensive nanos beyond Release Me Now (id 281239, snare/root resist). |
| Treatment | moderate | IP cost 1.5. A Shade needs NO Treatment for its implant slots (Spirits need none), so this is only for first-aid kits and stims - a real IP saving versus every other profession. |
| Sensory Improvement | to 1,243 | IP cost 1.6. Second nano school by usage: a cast requirement on 43 of 79 nanos. |
| Martial Arts | situational | IP cost 1.6. Only 2 Shade-locked Martial Arts weapons exist (Huzzum's Iron Fist id 303055, Special Edition Kyr'Ozch Nunchacko id 288291) and the Shade Touch perk line buffs it. A fallback, not a build. |
| Sharp Objects | situational | IP cost 1.6. Buffed +140 by Improved Puncture of the Tarasque (id 270804), but no Shade-usable weapon in items.ocp has Sharp Objects as its primary wield skill. |
| Biological Metamorphosis | to 1,579 | IP cost 1.9. Cast requirement on 32 of 79 nanos. |
| Time & Space | to 1,569 | IP cost 1.9. Cast requirement on 33 of 79 nanos. |
| Dodge-Rng | as IP allows | IP cost 2.4 - expensive, but it is the evade against ranged mobs and a Shade has no other answer to them. |
| Body Dev. | as IP allows | IP cost 2.6 - the HIGHEST Body Dev. cost of any profession in reference/skillcaps.json. Thin HP is a structural Shade weakness; buy HP from Spirits and perks rather than IP where you can. |
| Matter Creation | skip | IP cost 3.2 and a cast requirement on exactly ONE of the 79 Shade nanos (max 799). Do not spend IP here. |
| Physic. Init | skip | IP cost 3.4. Melee Init (1.0) is the initiative a Piercing weapon uses. Physical Init appears only as a requirement on gear (the Xan Spirit lines) - meet it from items, not IP. |
| every ranged weapon skill | skip entirely | Pistol, Rifle, Bow, MG/SMG, Assault Rifle, Shotgun, Ranged Energy, Grenade, Heavy Weapons, Multi Ranged, Ranged Init., Fling Shot, Aimed Shot, Burst, Full Auto and Bow Special Attack are ALL IP cost 4.0 for a Shade - the maximum in the table. |
| 1h Blunt / 1h Edged / 2h Blunt / 2h Edged / Melee Energy / Brawling | skip entirely | All IP cost 4.0. There is exactly one Shade-locked Melee Energy weapon (Special Edition Kyr'Ozch Energy Rapier, id 288287) and it is not worth a 4.0 skill. |
| Nano Programming | skip entirely | IP cost 4.0. A Shade builds no implants and no clusters, so this skill has no use at all for the class. |

## Perk and research lines (every line, every rank, from items.ocp)

All 17 lines below were read out of `items.ocp` as slot-less ToWear templates whose name matches the AOSharp `PerkLine` enum. Each row's rank count, per-rank level requirement, per-rank item id and per-rank stat bonus is client data, not a guide.

| Line | Track | Ranks | Levels | Priority | Full-line bonus |
|---|---|---|---|---|---|
| Ambushing [id 262078 = Ambushing] | LE research | 10 | 1-200 | med | Concealment +115, SneakAttack +50, Agility +20, Sense +20 |
| Assassin's Awareness [id 262018 = Assassin's Awareness] | LE research | 10 | 1-200 | low | PsychologicalModification +90, Intelligence +20, Psychic +15, ComputerLiteracy +15, SpaceTime +15, BiologicalMetamorphosis +15 |
| Honed Senses [id 262028 = Honed Senses] | LE research | 10 | 1-200 | low | MaxHealth +150, Sense +30, BodyDevelopment +25, ComputerLiteracy +20, Stamina +20 |
| Killing Blows [id 262038 = Killing Blows] | LE research | 10 | 1-200 | med | FastAttack +130, Stamina +20, Strength +15, Agility +10, HealDelta +10 |
| Lithe [id 262068 = Lithe] | LE research | 10 | 1-200 | med | EvadeClsC +95, Agility +50, DodgeRanged +25, Riposte +15, Strength +10 |
| Malicious Forethought [id 262058 = Malicious Forethought] | LE research | 10 | 1-200 | low | BiologicalMetamorphosis +50, SpaceTime +50, Intelligence +15, Strength +15, Parry +10, MaxNCU +4, XPModifier +2 |
| Piercing Mastery [id 211967 = Piercing Mastery] | SL | 10 | 10-208 | high | Piercing +200 |
| Shade Touch [id 212144 = Shade Touch] | SL | 7 | 20-205 | med | MartialArts +100, DuckExp +30, DodgeRanged +30, EvadeClsC +30 |
| Shadow [id 248115 = Shadow] | AI | 10 | 15-210 | med | Concealment +100, DuckExp +100, DodgeRanged +100, EvadeClsC +100, SneakAttack +50 |
| Spirit Phylactery [id 212049 = Spirit Phylactery] | SL | 10 | 20-203 | high | MaxHealth +400, AddAllDef +150, SneakAttack +100, AddAllOff +60 |
| Stiletto Mastery [id 262048 = Stiletto Mastery] | LE research | 10 | 1-200 | high | Piercing +115, MultiMelee +85, MeleeInit +75 |
| Sublime Rapport [id 212151 = Sublime Rapport] | SL | 10 | 10-203 | high | Dimach +200, Parry +40, DuckExp +30, DodgeRanged +30, EvadeClsC +30 |
| Totemic Rites [id 212097 = Totemic Rites] | SL | 10 | 10-204 | high | MeleeDamageModifier +208, PoisonDamageModifier +208, FireDamageModifier +208, NanoDamageModifier +208, ColdDamageModifier +208, ProjectileDamageModifier +208, EnergyDamageModifier +208, ChemicalDamageModifier +208, RadiationDamageModifier +208, SneakAttack +100, MartialArts +40 |
| Acrobat [id 211655 = Acrobat] | SL | 4 | 30-140 | med | DuckExp +190, DodgeRanged +190, EvadeClsC +190, Agility +30, MultiMelee +30, MultiRanged +30 |
| Careful in Battle [id 211738 = Careful in Battle] | SL | 10 | 40-219 | med | AddAllDef +300, CritialResistance +80 |
| Counterweight [id 252403 = Counterweight] | AI | 6 | 15-125 | med | MultiRanged +100, MultiMelee +100 |
| Spatial Displacement [id 212123 = Spatial Displacement] | SL | 10 | 70-212 | low | No stat bonus on any rank in items.ocp - every rank of this line grants a perk ACTION (a clickable special) rather than a passive stat. |

### Perk specials

| Line | Special | At perk level | What |
|---|---|---|---|
| Piercing Mastery | Stab | 10 | direct damage |
| Piercing Mastery | Double Stab | 30 | DD + DoT; needs the Stab flag |
| Piercing Mastery | Perforate | 90 | DD |
| Piercing Mastery | Lacerate | 130 | DD + DoT; needs Perforate |
| Piercing Mastery | Impale | 190 | DD |
| Piercing Mastery | Gore | 201 | DD + DoT; needs Impale |
| Piercing Mastery | Hecatomb | 208 | DD + DoT; needs Gore. The wiki ranks the chain Low/Low/Moderate/Moderate/High/High/Insane, so Hecatomb is the Shade finisher. |
| Shade Touch | Atrophy | 20 | DD + DoT + initiative debuff |
| Shade Touch | Consume the Soul | 60 | usable ONLY when the target is under 15% health |
| Shade Touch | Doom Touch | 130 | DD + AC debuff |
| Shade Touch | Spirit Dissolution | 205 | requires the Ritual of Spirit flag from Totemic Rites |
| Shadow | Blur | 15 | AMS + DMS debuff |
| Shadow | Diffuse | 105 | life drain |
| Shadow | Chaos Ritual | 210 | DD |
| Spirit Phylactery | Capture Vigor | 20 | AMS/DMS leech plus a detaunt |
| Spirit Phylactery | Unsealed Blight | 40 | HP and nano DoT |
| Spirit Phylactery | Capture Essence | 100 |  |
| Spirit Phylactery | Unsealed Pestilence | 130 |  |
| Spirit Phylactery | Capture Spirit | 160 |  |
| Spirit Phylactery | Unsealed Contagion | 202 |  |
| Spirit Phylactery | Capture Vitality | 203 |  |
| Sublime Rapport | Exultation | 10 | life drain + DD |
| Sublime Rapport | Ethereal Touch | 30 | AC debuff |
| Sublime Rapport | Dimensional Fist | 90 | needs the Ethereal Touch flag |
| Sublime Rapport | Disorientate | 150 | DD + stun |
| Sublime Rapport | Convulsive Tremor | 190 | DD + DoT + initiative debuff |
| Sublime Rapport | Symbiosis | 203 | upgrades the Totemic Rites "Ritual" specials |
| Totemic Rites | Ritual of Devotion | 10 | DD plus a self add-damage buff |
| Totemic Rites | Devour Vigor | 40 | life drain + DD |
| Totemic Rites | Ritual of Zeal | 90 |  |
| Totemic Rites | Devour Essence | 120 |  |
| Totemic Rites | Ritual of Spirit | 170 |  |
| Totemic Rites | Devour Vitality | 190 |  |
| Totemic Rites | Ritual of Blood | 204 |  |
| Acrobat | Limber | 60 | evade buff |
| Acrobat | Dance of Fools | 140 | evade buff |
| Careful in Battle | Evasive Stance | 40 | scales with how many perks in the line are trained |
| Counterweight | Full Frontal | 55 | ranged-init and run-speed debuff |
| Counterweight | Confinement | 125 | DD + taunt |
| Spatial Displacement | Removal 1 | 90 | reduces snare duration |
| Spatial Displacement | Removal 2 | 160 | reduces root duration |
| Spatial Displacement | Purge 1 | 200 | both |
| Spatial Displacement | Purge 2 | 204 | both |
| Spatial Displacement | Great Purge | 212 | both, plus a short root/snare resistance buff |

### LE research procs

THE RULE THAT MATTERS FOR A BOT: Shade procs split into two mutually exclusive groups and only ONE of each group can be active at a time. Type 1 = Blackheart, Devious Spirit, Drain Essence, Elusive Spirit, Sap Life, Siphon Being, Twisted Caress. Type 2 = Blackened Legacy, Concealed Surprise, Misdirection, Shadowed Gift, Toxic Confusion. So a Shade runs at most two research procs plus one nano proc. Patch 18.7 moved several procs between the two types, so this reflects the current client, not the original assignment.

| Research line | At research level | Proc | Stacking type |
|---|---|---|---|
| Ambushing | 125 | Drain Essence | type 1 |
| Assassin's Awareness | 150 | Shadowed Gift | type 2 |
| Assassin's Awareness | 190 | Siphon Being | type 1 |
| Honed Senses | 175 | Twisted Caress | type 1 |
| Honed Senses | 200 | Blackheart | type 1 |
| Killing Blows | 1 | Devious Spirit | type 1 |
| Killing Blows | 50 | Misdirection | type 2 |
| Lithe | 1 | Sap Life | type 1 |
| Malicious Forethought | 75 | Toxic Confusion | type 2 |
| Malicious Forethought | 100 | Elusive Spirit | type 1 |
| Malicious Forethought | 200 | Blackened Legacy | type 2 |
| Stiletto Mastery | 75 | Concealed Surprise | type 2 |

### Apotheosis

The game-wide Apotheosis research line (added in patch 18.8.15) applies to a Shade like any other profession. 100,000 SK per level. Listed because its level-200 and level-210 rewards are load-bearing for the two things a Shade struggles with: Computer Literacy for the Pandemonium belt, and Concealment / Run Speed.

| Character level | Reward |
|---|---|
| 200 | +50 to all trade and repair skills |
| 201 | +500 health, +500 nano, +5 to all abilities |
| 203 | +4% heal efficiency, +4% DND efficiency, +2% critical chance |
| 205 | +100 Deflect, +20 Add All Def, +25 Nano Resist |
| 207 | +20 to all evades, +20 Add All Off |
| 210 | +10 to all abilities, +30 First Aid/Treatment, +50 Concealment, +75 Run Speed |
| 213 | +20 to all damage types, +20 nano delta and heal delta |
| 215 | +35 to all nano skills, -5% nano cost, +50 Nano Init, +10 nano range |
| 218 | +30 to all melee and ranged skills |
| 220 | +30 to all specials |

## Perk mechanics

A SHADE IS A PERK-DAMAGE CLASS. ao-universe: "Shades are meant to do most damage through their perk specials", and a 220 Shade on the Funcom forums puts it at "Shades damage is 80% perks at end game". That is why the perk tables in this file matter more than the weapon tables do.

Shade perk-special damage scales off BUFFED PIERCING divided by 2, read as a perk item QL, in three tiers at QL 1-500 / 501-1000 / 1001-1500+. Every Shade perk special runs off Piercing EXCEPT the Shade Touch line, and you must be wielding at least one Piercing weapon for them to fire.

Since patch 17.9 only the FIRST perk of a chain requires you to be behind the target, and chains check the USER rather than the target - so a chain started on one mob carries over to the next. ao-universe/wiki call this the "Shade Dance": reposition once per chain, not once per perk.

SL perks: 1 per 10 levels to 200 plus 1 per shadowlevel = 40 at level 220. AI perks: 1 per Alien Level, max AI 30. The ao-universe guide gives no single ideal path - it says to take two lines to 200, a third with what is left, and a fourth in shadowlevels.

## Cross-profession perk-chain triggers

- All three Spirit Phylactery "Unsealed" perks gain a bonus from a Trader's Bloodletting.
- Sublime Rapport's Ethereal Touch is the bonus trigger for Tremor Hand in the any-profession Kung Fu Master line.

## Where the IP-derived and the sourced skill lists disagree

- BODY DEV: both web sources call it critical; the IP table says it is the Shade's single most expensive skill (2.6, the highest Body Dev cost of any profession). Both are true - it is critical AND expensive, which is exactly why a Shade buys HP from Spirits, perks and Apotheosis rather than from IP.
- DIMACH: the IP table says 1.0 (as cheap as it gets) while ao-universe calls it low priority because perk specials out-damage it. The two are reconcilable: cheap to raise, weak as an attack, but it is a gear requirement (Mystery of Pisces) and Spirit Siphon is Dimach-based.
- COMPUTER LITERACY: neither the IP table (2.4) nor the guides make this optional - ao-universe calls it DARK BLUE for a Shade and it is the gate on NCU and on the Pandemonium belt (~1,750 Comp. Lit. needed at level 200). It is under-weighted in the IP-derived list above.
- BACKSTAB DOES NOT NEED CONCEALMENT. ao-universe's breed page states Backstab does not require you to be concealed, so Concealment is not mandatory despite being a green (cost 1.0) skill.

## General (unlocked) perk lines a Shade can also take

84 lines in `items.ocp` carry no profession lock at all. Full table in the JSON.

## Where the Shade breaks the class-profile template

- NO PETS. shade-pets.json is deliberately absent: there is no SummonPet function in any of the 79 Shade-castable nanos in shade-nanos.json. The Pets tab will correctly show "not generated yet".
- NO IMPLANTS. Verified from the data: 44,060 implant templates (ItemClass 3) exist in items.ocp and NOT ONE of them carries a profession lock, so the prohibition is a client/server rule, not an item criterion. The evidence is the client's own description text on Whispering Spirit (id 218576). See shade-implants.json.
- NO SYMBIANTS. With --prof 15 the symbiant extractor returns 14 usable templates out of 1,462 wearable ones, and 12 of those 14 are the known unlocked Xan Artillery anomaly (ids 278908-278919) plus two unlocked one-offs (245306, 245895). A Shade wears SPIRITS (ItemClass 5, 844 templates, 842 locked to profession 15). See shade-symbiants.json, which holds the Spirits and says so on the page.
- PERKS COME FROM THE CLIENT DATA HERE, NOT FROM THE WEB. The metaphysicist-build.json perk block was written from web guides with prose effects. For the Shade, every perk line, its rank count, each rank's level requirement, each rank's item id and each rank's exact stat bonus is read out of items.ocp (slot-less ToWear templates whose name matches the AOSharp PerkLine enum). This is a strictly better method and should be back-ported to the other classes - the same extraction is a --prof change.
- ONE WEAPON SKILL, NOT A LIST. The MP template assumes a profession picks among several weapon skills. A Shade has exactly one affordable skill (Piercing, 1.0) with Martial Arts (1.6) as a distant second; every other weapon skill in the game costs it 4.0. usableWeaponTypes in shade-weapons.json is therefore a very short list and the otherWeaponSkillsInData block is the interesting part.
- RIPOSTE AND PARRY ARE WIELD REQUIREMENTS, NOT JUST PASSIVES. The playbook §3 encoding note says Riposte/Parry are passive and not on-demand specials - true - but on the Shade-locked weapon lines (Sigd, Bloodlust, Bronto Vet Lancet, Thousand Stings, Nippy John Stiletto) they appear as hard ToWield skill requirements at ~40% of the Piercing requirement. A Shade that ignores them cannot equip its own weapons.
- NO BUFFS GIVEN TO OTHERS. Every one of the 79 castable nanos targets self (shade-nanos.json). The givesToOthers block in the buffs schema is genuinely empty for this class.
- TREATMENT IS NOT A LADDER SKILL. Every other profile treats Treatment as the implant-ladder gate. For a Shade it gates nothing it wears - Spirits need no Treatment and no surgery clinic - so its 1.5 cost factor is misleading if read against the implant model.

## Deliberately unverified

- Breed availability for the Shade profession is a character-creation rule and is not encoded in items.ocp; the recommendation above is derived from ability caps vs skill dependencies, not from a fetched guide.
- Perk RANK COSTS (how many perk points each rank costs) are not in items.ocp and are not stated here.
- Which LE research LINE grants each LEProcShade* proc perk is not encoded in the local data. The procs are listed by name only, with no line assignment.
- Perk bonuses recorded with amount 0 in the rank tables are exactly what the client stores - those ranks grant a perk ACTION rather than a stat. The action itself is not in this extraction.
- The "target" column in skillPriorities is a judgement about IP allocation, not a sourced number. The IP COST FACTORS it is derived from are data (reference/skillcaps.json, Shade = _professionOrder index 11).
- generalPerkLinesAlsoAvailable means "has no profession lock in items.ocp". Some entries (vehicle lines, Shadowbreed lines, breed genome lines) are gated by things other than profession - breed, AI title level, expansion - which this extraction does not resolve.
- The ability-synergy percentages quoted in the breed reasoning come from reference/skillcaps.json, where several are themselves marked "(unverified)" by that file. The COST factors, which carry the argument, are the cross-checked part.
- The numeric effect, proc chance and trigger of all 12 Shade LE research procs are not published on any allowed source - only the names, the lines that grant them and their stacking type.
- SOURCE CONFLICT: wiki.aodb.us/wiki/Research gives the research-level character requirements as 1/50/75/100/125/150/175/190/190/200 (matching ao-universe) while wiki.aodb.us/wiki/Player_Level gives 1/25/50/100/... The Research-page ladder is recorded here.
- SOURCE CONFLICT: ao-universe lists Malicious Forethought research level 10 as Sap Life; wiki.aodb.us/wiki/Shade:Research_Lines lists it as Blackened Legacy. The wiki reading is recorded because the ao-universe page grants Sap Life twice and never grants Blackened Legacy.
- AI PERK LINES: the ao-universe Comprehensive Shade Guide names only Shadow and Counterweight as Shade AI lines. The five "Champion of ..." lines are marked "(Any)" profession on wiki.aodb.us/wiki/Perk_Chains, which implies but does not state Shade access. Starfall is explicitly NOT a Shade line (listed Crat/Doc/MP/NT). Champion of Vigor, Atrox Regeneration, Bio Shielding, Genius, Extremism and Assassin could not be confirmed for a Shade.
- ao-universe claims patch 18.7 let Shades "wear all items that are open to other professions such as token boards, helmets and back items". That sentence does NOT appear in the wiki.aodb.us Patch 18.7 notes, which list only nano/perk/proc changes for the Shade. Single-sourced.
- SOURCE QUALITY: wiki.aodb.us's Shade pages are largely 2006-era stubs - Shade:Weapons contains only Tango Dirk and Improved Tango Dirk, Shade:Perks has empty headings for Sublime Rapport and Shade Touch, and Shade:Armor has empty Rubi-Ka and Alien Invasion sections. The load-bearing web source for this class is the ao-universe Comprehensive Shade Guide.

## Sources

- LOCAL E:/Funcom/OmniCell/OmniCell/Datafiles/items.ocp (client 18.8.50_EP1) via tools/eng-gear-extractor --prof 15: every perk line, rank, level requirement, item id and stat bonus above.
- LOCAL E:/Funcom/attic/extracted-client-data/itemnames.sql: every id -> name in this file was re-verified against it.
- LOCAL E:/Funcom/AOBuddy10/AOSharp.Common/GameData/PerkLine.cs: the 272-entry perk-line enum used to separate real perk lines from other slot-less wearables.
- LOCAL E:/Funcom/AOBuddy10/AOSharp.Common/GameData/PerkHash.cs: the 12 LEProcShade* research-proc perk names.
- LOCAL AOBuddy/GameData/profiles/reference/skillcaps.json (Shade = _professionOrder index 11): every IP cost factor quoted above.
- LOCAL AOBuddy/GameData/profiles/reference/breeds.json: ability caps and creation bases behind the breed pick.
- LOCAL AOBuddy/GameData/profiles/shade-nanos.json: the nano-school cast requirements (Psy Mod on 73 of 79 nanos to 1,840; Sense Imp 43 to 1,243; Time & Space 33 to 1,569; Bio Met 32 to 1,579; Matter Creation 1 to 799).
- LOCAL AOBuddy/GameData/profiles/shade-weapons.json and shade-weapons-endgame.json: the weapon wield/special requirement figures quoted in skillPriorities.
- https://www.ao-universe.com/guides/shadowlands/professions-guides/comprehensive-shade-guide
- https://wiki.aodb.us/wiki/Shade:Perks
- https://wiki.aodb.us/wiki/Shade:Research_Lines
- https://wiki.aodb.us/wiki/Perk_Chains
- https://wiki.aodb.us/index.php?title=Shade:Breed_and_Skills&action=raw
- https://wiki.aodb.us/wiki/Shade:Tips_and_Tricks
- https://wiki.aodb.us/wiki/Patch_17.9
- http://wiki.aodb.us/wiki/Patch_18.7
- https://wiki.aodb.us/wiki/Patch_18.8
- https://wiki.aodb.us/wiki/Research
- https://wiki.aodb.us/wiki/Shade
- https://www.ao-universe.com/guides/classic-ao/gameplay-guides-6/breed-statistics
- https://www.ao-universe.com/guides/shadowlands/gameplay-guides-5/shadowbreeds
- https://forums.funcom.com/t/new-shade-coming-back/120089
- https://forums.funcom.com/t/shade-endgame-soloing-and-deflect-returning-player/30753
- https://forums.funcom.com/t/shade-equiping-pande-belt-at-200/79965
