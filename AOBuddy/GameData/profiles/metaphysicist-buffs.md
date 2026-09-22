# Meta-Physicist — Buffs Reference (AOBuddy10)

Profession 12. Companion to `metaphysicist-buffs.json`. Self-buff and gives-to-others IDs are
read from the local extraction `metaphysicist-nanos.json` (nanos.ocp, client 18.8.50_EP1).
Cross-profession and general/consumable buffs come from reputable AO sources (cited at bottom);
their IDs are `null` because they are other professions' nanos, not in the local MP data.

The MP is squishy, pet-based, and is itself **the game's premier nano-skill buffer**. Its whole
buff story is: (1) stack nano skills + attributes to twink gear and cast big pets, (2) beg a
Trader Wrangle, (3) hand out Mochams to everyone else.

---

## 1. Self-buffs — keep-up list and cast order

Cast in this order (each later buff is easier to land once the earlier attribute/skill buffs are up):

1. **Odin's Other Eye** (`273379`) — Int +103, Psy +103, and PsyMod/SenseImp/SpaceTime/MatCrea +100
   each (+Nano Resist). Base attribute + nanoskill engine.
   *Trap:* shares nanoline **576** with **Odin's Missing Eye** (`29309`) and **Neuronal Stimulator**
   (`220345`) — only the best applies; never stack them.
2. **All-nano-skill +140** — pick ONE approach:
   - Six single-school **Mocham's Gift**: BioMet `29299`, MatCrea `29300`, SpaceTime `29301`,
     MatMet `29302`, PsyMod `29303`, SenseImp `29304` (+140 each, six different nanolines → all stack), or
   - One **Composite Mochams** `220337`/`220339`/`220341`/`220343` (+140 to all six in a single NCU slot;
     level req 185/205/215/219).
   - *Trap:* Composite Mochams lives in nanoline **165 (SenseImp)** → it CANNOT coexist with
     **Mocham's Gift: SenseImp** `29304` (or the SenseImp Mastery/Infuse/Teachings). One occupant of line 165.
3. **Mocham's Neural Interface-Web** (`95409`) — best self nano-cost reduction; enables chain-casting.
   Lesser: Neuron-Notum Interface `29307`, Coherent Notum Web `95411`, Notum Attunement `95408`,
   Ease of Execution `95410`.
4. **One Mind, One Purpose** (`95522`) — best interrupt modifier (land casts under fire).
   Lesser: Dedication of Thought `95525`, Ignore External Events `95524`, Engrossing Activity `95523`,
   Internal Focus `95521`.
5. **Improved Anticipation of Retaliation** (`302188`) — evades +150 (Dodge/Evade/Duck). Solo/PvP.
   Lesser: Anticipation of Retaliation `29272` (+60).
6. **Advanced Symbol Manipulation** (`29327`) — NanoProgramming +92 (keep up when twinking/equipping).
   Lesser: Symbol Helper `29113` (+20).

**Situational (not keep-up):** Aggressive Construct Empowerment `275020` (AddAllOff +300),
Mesmerizing Construct Empowerment `263298` (+80 PsyMod/SenseImp), Construct Empowerment/Healing
`260771` (HealMultiplier +12), Quantum Wings `120499` (RunSpeed +600, travel),
Shadowland Soul Fetter/Awaken `233847`/`233849` (bind/recall).

**False/Assume/Mimic Profession: Meta-Physicist** (`32036`/`117219`/`117208`) — NanoProgramming
+150/+70/+15 at a % nanoskill penalty; a twink tool to equip off-profession items. *Present in the
MP-castable data but the target/use for an MP self-casting its own false profession is unclear —
flagged; do not auto-cast until confirmed.*

---

## 2. Buffs the MP WANTS from other professions

| Buff | From | Effect | Use |
|---|---|---|---|
| **Wrangle** (single) | Trader | +all weapon **and** nano skills (up to ~+132), 3 min, self-debuffs Trader | **twink** #1 |
| **Umbral Wrangler (aura) / Team Wrangle** | Trader | Sustained team weapon+nanoskill, no Trader debuff | raid/cast |
| **Trade-skill Wrangles** (Apprentice/Journeyman/Maestro) | Trader | +40/+80/+125 to a trade skill (Nano Programming etc.), no level limit | twink (self-build implants) |
| **Essence of Behemoth** line | Enforcer | Str + Sta together (Improved ~+54 each) + big Max Health | twink (gear ability reqs) |
| **Iron Circle** | Doctor | Str +20, Sta +20 | twink |
| **Instinctive Control** (Nano Init) | Doctor | Nano Init +200 (Improved ~+350) → faster casts | cast |
| **Max Health / HoT / heals** | Doctor | Large HP + heal-over-time / complete heal | raid survivability |
| **Feline Grace** | Agent | Agility +25 | twink |
| **Enhanced Senses** | Agent | Sense +15 | twink |
| **Chemistry & Pharmaceuticals** | Agent | ~+50 tradeskill | twink (build implants) |
| **False Profession** | Agent | Appear as another profession for item reqs | twink (off-prof items) |
| **Perception/Treatment + Run speed, Cocoon** | Adventurer | Eagle Eye (+240 Perc), Robust Treatment (+60), fast run, Cocoon immunity | twink/travel |
| **NCU (Grid Armor) + Run speed + HoT** | Fixer | More NCU to hold the buff stack; team run speed | raid/travel |
| **Team auras** (run speed, heal, +AAO/damage, reflect) | Keeper | Passive team uptime | raid |
| **Composite Attribute Boost** | Any (SL nanocrystal) | All six attributes +12 | twink |

**Twink priority for the MP:** Trader Wrangle first (unlocks higher symbiants/implants/nanos), then
its own stacked nanoskill buffs count toward symbiant/nano nanoskill reqs, then attribute buffs
(Enforcer Essence, Doctor Iron Circle, Agent Feline Grace/Enhanced Senses, Composite Attribute Boost)
for the ability reqs on implants/symbiants/armor.

*Note:* Nano-Technician is largely a **self**-buffer here — the task-listed NT "Matrix"/"Humidity
Extractor" names were **not** confirmed in sources and are flagged; NT is not a major cross-prof
buffer for an MP. Crat/Fixer exact nano names are likewise not pinned (flagged in JSON `_unverified`).

---

## 3. General / consumable / city buffs

- **Composite Attribute Boost** — all six attributes +12 (Nano-Can "Composite Attribute Improvement" +20).
  Self-castable SL nanocrystal, froob-available. (aoitems 223372 / 303383.)
- **Composite Nano / Utility / Melee / Ranged / (Special)** — SL composite nanocrystals, self-castable by
  all professions; grouped skill boosts. *Exact contents/ids not pinned — flagged.* (Watch nanoline overlap
  with the MP's own Composite line.)
- **Essence / Behemoth** — Str/Sta + HP (mainly Enforcer-cast; see §2).
- **Expertise buffs** — group skill boosts; **stack** with Mochams and attribute boosts (AO-U guide).
- **IntelPredators / Predator line, Pep / Wen (First Aid/Treatment) consumables** — temporary stat windows
  for reqs / self-heal / building. *Exact items not pinned — flagged.*
- **City buffs** (org city) and **Tower / Notum-War buffs** (AAO/AAD from controlled land) — *sets not pinned.*

---

## 4. Buffs the MP GIVES to others (its signature role)

The MP is the go-to **nano-skill** buffer. Wanted by every nano-using profession to cast higher nanos
and to equip nanoskill-req symbiants/implants.

**All-six-nanoskill (Composite) line — one cast, all schools:**

| Nano | ID | Effect | Level |
|---|---|---|---|
| Composite Teachings | `220331` | all nanoskills +25 | 25 |
| Composite Mastery | `220333` | +50 | 50 |
| Composite Infuse With Knowledge | `220335` | +90 | 100 |
| Composite Mochams (1h/2h/4h/8h) | `220337`/`220339`/`220341`/`220343` | +140 | 185/205/215/219 |

**Single-school lines** (give when a target needs one school stacked, or to top a target already running
a Composite in a different line):

- **Mocham's Gift +140:** BioMet `29299`, MatCrea `29300`, SpaceTime `29301`, MatMet `29302`, PsyMod `29303`, SenseImp `29304`
- **Infuse With Knowledge +90:** SenseImp `151757`, SpaceTime `151758`, MatMet `151759`, PsyMod `151760`, BioMet `151761`, MatCrea `151762`
- **Mastery +50:** BioMet `29275`, MatCrea `29294`, SpaceTime `29295`, MatMet `29296`, PsyMod `29312`, SenseImp `29315`
- **Teachings +25:** PsyMod `151763`, SenseImp `151764`, MatCrea `151765`, SpaceTime `151766`, MatMet `151767`, BioMet `151768`

**Team utility:** Channel Notum Vein — Elysium `234993`, Scheol `234995`, Adonis `234997` (team nano/notum
sustain, level 75+).

*Stacking:* within each nanoskill line the tiers (Teachings < Mastery < Infuse < Mocham) don't stack —
only the highest applies. Composite (nanoline 165) collides with the SenseImp single/tiers, so don't give
both a Composite and a SenseImp single to the same target.

---

## Sources

- Local: `AOBuddy/GameData/profiles/metaphysicist-nanos.json` (nanos.ocp, 18.8.50_EP1) — all self/gives IDs
- [AO-Universe Buffing Guide](https://www.ao-universe.com/guides/classic-ao/gameplay-guides-6/buffing-guide)
- [AODB Who Buffs What](https://wiki.aodb.us/wiki/Who_Buffs_What) / [Fandom mirror](https://anarchyonline.fandom.com/wiki/Who_buffs_what)
- [aoitems: Composite Attribute Boost 223372](https://aoitems.com/item/223372/composite-attribute-boost/)
- [Funcom Forums: MP twinking guide](https://forums.funcom.com/t/mp-25-nanomage-twinking-guide-on-rubi-ka/107122)
- [AODB Meta-Physicist Nano Programs](http://wiki.aodb.us/wiki/Meta-Physicist_Nano_Programs) / [aoguide MP nano list](https://www.aoguide.org/nano/metaphysicist)

**Unverified / flagged:** Doctor Nano-Init exact name/value; NT "Matrix"/"Humidity Extractor" (not
confirmed — NT is a self-buffer here); Crat/Fixer exact nano names; general Composite/IntelPredator/
pep/wen/city/tower exact ids; the MP-castable False Profession family's intended use. See JSON `_unverified`.
