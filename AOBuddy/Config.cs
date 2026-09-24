using System.Collections.Generic;

namespace AOBuddy
{
    /// <summary>
    /// A class of heal item and how it behaves, so the bot picks the right one per situation. RK stims and
    /// rechargers plus Shadowlands Coils and the reusable Veterans Healing Laboratory all describe here by
    /// name-match — no hardcoded ids. See NANO_BUFF_DESIGN / the heal notes.
    /// </summary>
    public class HealProfile
    {
        public string NameContains = "";
        public bool RestoresHealth;
        public bool RestoresNano;
        public bool UsableInCombat;   // can be used while fighting (stim yes; Coil of Health no)
        public bool RequiresSit;      // must sit to use (rechargers, Coil of Health)
        public bool OverTime;         // heals in ticks over a duration, not an instant spike (Coils)
        public bool Reusable;         // never consumes a charge (Veterans Healing Laboratory) — a refresh cooldown
    }

    /// <summary>
    /// Per-bot settings, loaded from config.json next to the plugin DLL.
    /// </summary>
    public class BuddyConfig
    {
        /// <summary>The one character allowed to command the bot, and whom it assists/follows.</summary>
        public string Owner = "";

        /// <summary>Assist | Solo | Idle.</summary>
        public string DefaultMode = "Assist";

        public bool AutoAcceptOwnerTeamInvite = true;

        // Fire weapon special attacks (Fast Attack, Brawl, Fling, Burst, Full Auto, Aimed Shot, …) on top of
        // auto-attack, auto-detected from what the server says this character can use. Toggle: 'specials'.
        public bool UseSpecials = true;

        // --- Pets (MP / Engineer / Crat) -------------------------------------
        // OFF by default so it never touches a non-pet character. Turn on for a pet class ('pets' command).
        public bool UsePets = true;                            // summon pets always (no-op for chars with no pet nanos)
        public bool AutoResummon = true;                       // replace pets that die (toggle: 'resummon')
        public List<int> PetSummonNanoIds = new List<int>();   // the summon nanos to keep up (auto-detect later)

        // Pet buffs: once the full pet complement is up, the best castable nano he knows in each line is kept
        // up. NanoLine names (or numbers). Which pet a nano may go on is still checked against its own target
        // requirements. NB: Newtonsoft APPENDS a config.json list to these defaults; duplicates are harmless.
        public bool BuffPets = true;                           // toggle: 'petbuffs'
        public List<string> AttackPetBuffLines = new List<string>
        {
            "PetShortTermDamageBuffs",   // Evocation of Unleashed Malice (205195)
            "MPPetDamageBuffs",          // Instill With Fury (116815)
            "MPPetInitiativeBuffs",      // Chant of Frenzied Blows (116820)
        };
        public List<string> AllPetsBuffLines = new List<string>();   // heal-delta, resist ... buffs every combat pet gets

        // Log what the server tells us about a mission's location/playfield (PlayfieldAnarchyF on entry +
        // the raw mission-terminal list). Toggle: 'missiondbg'. See [[aobuddy-mission-wire-data]].
        public bool MissionDebug = true;

        // --- Healing ---------------------------------------------------------
        // Stims: instant items, usable IN combat, on self or the owner (target).
        // Rechargers: items usable only OUT of combat and only while SITTING.
        // Nanos: optional profession heals (cast).
        public int HealOwnerBelowPercent = 60;     // stim/heal the owner at or below this HP%
        public int HealSelfBelowPercent = 55;      // stim/heal herself at or below this HP%
        public int SelfCriticalPercent = 40;       // SURVIVAL FLOOR: at/below this the bot stims ITSELF first,
                                                   // before the owner and even out of combat (never die healing you)
        // She auto-picks the BEST stim/recharger she can actually use: any inventory item whose
        // name contains the keyword AND whose use-requirement (First Aid for stims, Treatment for
        // rechargers) she meets, choosing the highest QL. No exact item names to configure.
        public string StimKeyword = "Stim";        // matches "Health and Nano Stim", etc.
        public string RechargerKeyword = "Recharger";
        public string StimItemName = "";           // optional: force one exact stim name (blank = auto by keyword)
        public int HealNanoId = 0;                 // optional single-target heal-other nano (0 = none)
        public int SelfHealNanoId = 0;             // optional self-heal nano (0 = none)
        public float StimOwnerRange = 12f;         // only stim the owner if within this range
        public float HealIntervalSec = 2.0f;       // min seconds between emergency heals (stim/nano cooldown)

        // Class-specific heals (set the alt profession's nano ids — find them with the
        // 'nanos'/'learnable' commands). WHEN to heal is decided by the state machine;
        // WHAT to cast is these ids. Doctor: team + single heals. Adventurer/others: single.
        public int TeamHealNanoId = 0;             // doctor-style team heal (0 = none)
        public int TeamHealBelowPercent = 65;      // fire team heal if a member is at/below this
        // Healing aura / HoT buffs (Keeper aura, etc.): cast once and kept running by the
        // buff-keepup, not spammed as reactive heals.
        public System.Collections.Generic.List<int> HealAuraNanoIds = new System.Collections.Generic.List<int>();
        public float AuraRecastSeconds = 240f;     // fallback: re-cast auras this often if their timer can't be read
        public float AuraRefreshMargin = 20f;      // if the aura's remaining time drops below this, recast it

        // Recharger (rest) — out of combat only, must sit.
        public string RechargerItemName = "";      // optional: force one exact recharger name (blank = auto by keyword)

        // Heal-item profiles for Shadowlands-readiness. EMPTY here = use the built-in defaults (RK Stim +
        // Recharger, SL Coil of Health / Coil of Nano Energy, Veterans Healing Laboratory, kits). RK behaviour
        // is UNCHANGED: the bot tries the Stim/Recharger keywords FIRST (identical to before) and only falls
        // back to these profiles when no stim/recharger is found (i.e. in Shadowlands). Override to add/tweak.
        public List<HealProfile> HealProfiles = new List<HealProfile>();
        public int RestBelowPercent = 80;          // when settled out of combat, sit & recharge below this HP%
        public int RestUntilPercent = 96;          // stand back up once healed to this HP%
        public float RestMaxSeconds = 8f;          // stand up after this long even if not fully healed (never sit forever)
        // Nano (casters need nano even at full HP; stims/rechargers restore both).
        public int StimNanoBelowPercent = 35;      // in combat, stim when nano at/below this (keep casting)
        public int RestNanoBelowPercent = 60;      // out of combat, sit & recharge when nano at/below this
        public int RestNanoUntilPercent = 90;      // nano target to stand at
        // Heal items have a USE TIMER. Cycle: sit, use ONCE, stand, wait out the timer, only then sit again.
        // These values come from the ITEM DATA (OmniCell items.ocp), the OnUse event's LockSkill(53033) call:
        //   Health and Nano Recharger (291082): LockSkill(skill 124, 15s)
        //   Health and Nano Stim      (291043): LockSkill(skill 123, 40s)
        // They lock DIFFERENT skills, so the two items are independent — using one does not block the other.
        // Not readable via AOSharp: its item pack has no OnUse events and drops RechargeDelay for non-nano items.
        // These are the FALLBACK: once the server's lock (SpecialUsed on First Aid / Treatment) arrives in
        // me.Cooldowns, its remaining time wins (see SupportController.HealItemReady).
        public double RechargerReuseSec = 15.0;
        public double StimReuseSec = 40.0;

        // Low-supply warnings (until she can resupply herself, she tells the owner to restock).
        // Low-supply warnings use real stack-aware unit counts (Item.Count).
        public int LowStimCount = 10;              // warn when usable stims fall to this many
        public int LowRechargerCount = 10;         // warn when usable rechargers fall to this many

        // --- Resupply (buying stims/rechargers in a shop; 'resupply' command) -------------------
        // What to buy, by exact name (a keyword would also catch Boosted Stim, Burst of Speed Stim...), and how
        // many to carry. Only the highest QL whose First Aid / Treatment requirement the bot meets is bought.
        public string ResupplyStimName = "Health and Nano Stim";
        public string ResupplyRechargerName = "Health and Nano Recharger";
        public int ResupplyStimTarget = 2;        // top usable stims back up to this many (times 25)
        public int ResupplyRechargerTarget = 3;   // top usable rechargers back up to this many (times 20)
        public float ResupplySearchRadius = 40f;   // terminals within this of the bot are considered
        public float ResupplyUseRange = 3f;        // walk this close to a terminal before using it
        public int ResupplyKeepFreeSlots = 2;      // never fill the last inventory slots with supplies
        public int ResupplyCashReserve = 0;        // credits never spent on supplies
        public float ResupplyNagSeconds = 120f;    // short of credits: remind the owner this often
        // Terminal names that suggest medical supplies are tried first; every other terminal in reach is
        // still tried after them, and what each one sells is remembered per playfield (resupply.json).
        public List<string> ResupplyMachineKeywords = new List<string> { "Medic", "Health", "Stim", "Recharg", "First Aid", "Treatment", "Pharma" };

        // Flee detection — when the owner runs off, break combat and follow.
        public float FleeSpeed = 3.0f;             // owner world-units/sec that counts as running away
        public float CombatBreakMeters = 28f;      // or if the owner gets this far, break combat & follow

        // Buffs (manual 'buff' command lists). Optional — the AUTO-BUFF system below keeps buffs up on its
        // own from what he has LEARNED, so these are only needed to force a one-off cast.
        public List<int> SelfBuffNanoIds = new List<int>();
        public List<int> OwnerBuffNanoIds = new List<int>();
        public List<int> TeamBuffNanoIds = new List<int>();

        // --- AUTO-BUFF (nano keep-up) --------------------------------------------
        // Fully auto-detected from the nanos he has learned (me.SpellList) — no hardcoded ids. Each learned
        // nano is classified from its OWN data: a keep-up BUFF = takes NCU + beneficial effect + castable on
        // a friendly target (probed via MeetsUseReqs). He keeps those up on whoever they're valid on, out of
        // combat, recasting before they expire, refilling nano with rechargers/stims as needed. See
        // NANO_BUFF_DESIGN.md. Only category toggles + margins here; the ids come from the wire.
        public bool AutoBuff = true;               // master switch for the auto keep-up
        public bool BuffSelf = true;
        public bool BuffOwner = false;             // OFF: owner asks for the buffs he wants ('buff <name>'), we don't push
        public bool BuffTeam = false;              // off by default — team buffing is heavier
        public bool BuffInCombat = false;          // buffs are an out-of-combat activity
        public float RebuffMarginSeconds = 60f;    // recast a buff when under this many seconds remain
        public float BuffAssumeSeconds = 1800f;    // if we can't see a target's buff timer (others are best-effort),
                                                   // assume a buff we cast lasts this long before re-casting (unless
                                                   // we have the same buff on ourselves, then we use its real timer)
        public List<int> ExcludeNanoIds = new List<int>();  // never auto-cast these (auto-detect got it wrong)

        /// <summary>Don't engage targets farther than this (assist safety leash).</summary>
        public float AssistMaxDistance = 40f;
        public float HuntRadius = 30f;             // 'hunt' with no radius: pets hunt mobs within this many metres of the bot

        // Solo missions ('mission roll' at a SOLO terminal). Sliders are the values the game sends: -100 to +100,
        // 0 = the middle (the terminal's default). -100 is the left end (credits on CreditsXp), +100 the right
        // (captures 20260923-120056 and -201746, where every slider was set to both ends). Difficulty is the
        // terminal's own value (captures: 1, 6, 11).
        public int MissionDifficulty = 6;
        public int MissionSliderGoodBad = 0;
        public int MissionSliderOrderChaos = 0;
        public int MissionSliderOpenHidden = 0;
        public int MissionSliderPhysicalMystical = 0;
        public int MissionSliderHeadonStealth = 0;
        public int MissionSliderCreditsXp = 0;
        public List<string> MissionZones = new List<string>();
        public int MissionFightBelowPercent = 40;                // mission run, blitz style: stop and fight only when hit below this HP % with no stim ready
        public string MissionStyle = "blitz";
        // Mission types the run takes (any of "find person", "repair", "find item"). Find item left out for now:
        // in the solo buildings the item never showed up as a loose item (2026-09-23).
        public List<string> MissionTypes = new List<string> { "find person", "repair" };                    // mission run: "blitz" (run, stim, fight only in an emergency) or "fight" (stop and fight anything that attacks)   // zone names or ids missions may be taken in; empty = any
        public float AttackRange = 8f;             // close to within this before attacking
        public float CombatLeashMeters = 18f;      // only assist mobs within this range of the OWNER (don't bolt after distant fights)
        public float CombatRestCooldownSec = 6f;   // don't sit to rest until combat has been over this long (multi-mob lull guard)

        // Follow: trace the owner's actual path (breadcrumbs), never teleport.
        public bool Follow = true;
        public float FollowResumeSlack = 1.5f;     // only set off again once he's this far PAST FollowDistance
        // (hysteresis — stops the stutter at the leash edge)
        // Turn speed. 0 = MOUSE-LOOK: the heading is written straight into the movement packet and the
        // bot faces anywhere in ONE update — the same thing right-click-drag does in the client, which is
        // why dragging spins you so much faster than the A/D turn keys (those drive a rate-limited turn
        // state). A clientless bot has no reason to pay the keyboard rate, so this is the default and no
        // follow step is ever spent waiting on a turn. Set >0 (e.g. 540) for a visible, keyboard-style
        // rate-limited swivel in degrees per second.
        public float FollowTurnDegPerSec = 0f;
        public float FollowTurnFirstDeg = 60f;     // keyboard mode only: if the target is more than this off
        // our nose, turn in place FIRST, then run (no sliding
        // sideways). Ignored in mouse-look mode.
        public float FollowFaceDeadzoneDeg = 8f;   // parked: only re-align onto his facing once it's this far
        // off ours (his idle heading jitter must not spam turns)
        public float FollowLostPushMeters = 10f;   // after reaching his last-seen spot out of view, lean this
        // far along his heading (rounds a corner / leans onto a line)
        public float FollowSpeed = 19f;            // move speed u/s. MUST stay under the char's RunSpeedBase
                                                   // (the bot's is 21) or the server rejects every move as
                                                   // too-fast and snaps him back to spawn. 19 confirms the
                                                   // fix; MoveSpeed() also caps at the char's run-speed stat.
        public float FollowDistance = 0f;          // stop catching up once this close to the owner. 0 = STACK:
        // stand on his exact spot with his exact facing (see FollowController.StackTick)
        public float FollowStackSlideMeters = 4f;  // stack mode: within this of his spot, slide onto it keeping HIS
        // facing (never turn round to walk back after an overshoot); farther, run facing him
        public bool FollowMirror = true;           // stack mode: once on his spot, replay HIS movement packets as ours
        // (start/stop, strafe, turn, jump) so the server moves us exactly as it moves him
        public float FollowMirrorBreakMeters = 2.5f; // drop the mirror and re-stack if we end up this far off him
        // Live-lead: when the breadcrumb trail is used up but the owner is still ahead ON FLAT GROUND, close
        // to him directly instead of idling until his next (delayed) position update — kills the stop-lurch
        // lag. Guarded to flat + short range so it never beelines up a ramp (ramps keep using the safe trail).
        public bool FollowLiveLead = false;        // coast along the owner's last direction during update gaps —
                                                   // OFF: it overshot (ran past the owner). Trail-only follow is the known-good.
        // OWNER INTERPOLATION: the server only relays the owner's position ~1/s (sparse keyframes), so the
        // bot's breadcrumb trail is choppy and lags. Predict the owner between keyframes from HIS OWN reported
        // heading/velocity (in his CharDCMove), freezing the instant his stop keyframe lands — like a real
        // client interpolating other players. Only horizontal, and only when his path is flat (no vertical
        // guess on ramps). This is the real lag fix; the crumbs it feeds stay wall-safe (his own motion).
        public bool OwnerInterp = true;
        public float OwnerInterpMaxSec = 1.5f;     // cap how long to extrapolate past a keyframe (covers ~1-2s server
                                                   // silences during a run; stop keyframes still freeze him promptly)
        public float OwnerInterpFlatY = 1.0f;      // only extrapolate when his vertical speed is under this (flat)
        public float FollowCoastMeters = 5f;       // max distance to coast on one blind gap before waiting
        public float FollowFlatThreshold = 2.5f;   // only coast when the owner's recent path is within this Y of us (flat)
        public float BreadcrumbSpacing = 0.3f;     // 'record'/'savepath' point spacing — saved paths stay dense
                                                   // (replayed blind later). Live follow uses the sparse
                                                   // waypoint queue in FollowController, not this.
        public float CrumbArrive = 1.0f;           // how close to a waypoint counts as reached (normal tier)
        public float StuckSeconds = 1.2f;          // if a waypoint can't be reached in this long, skip it (blocked)
        public float ZoneChaseMeters = 25f;        // extend one crumb past your last spot to cross a zone line
        public float ManualRunMeters = 25f;        // 'zone'/'forward' command distance

        // --- Permanent stats: PERK & RESEARCH bonuses -----------------------------
        // The clientless stream does NOT carry perk/research stat bonuses, so the bot
        // under-reads permanent stats vs retail. Fill in the perks this character has
        // trained and the bot folds their summed bonuses into every GetStat (abilities
        // boost dependent skills through trickle automatically).
        //
        // PerkLines: one entry per trained perk, "Perk display name:highest trained level".
        //   The name must match perks.sql (case/space-insensitive). Level 1..N sums levels
        //   1..N of that perk. Example for the +4-all-abilities / +3-Body-Dev perk at 2 levels:
        //     "PerkLines": [ "Enhance DNA:2" ]
        //   (Enhance DNA lvl1+lvl2 = +8 to each ability and +6 Body development.)
        //   Send the 'perks' command in-game to see what was parsed, applied, and any
        //   unknown perk names or unmapped skills.
        public List<string> PerkLines = new List<string>();

        // ResearchBonuses: RESEARCH has no known skill-bonus dataset yet (OmniCell's
        //   "PerkUpdateMessage" is research PROGRESS, not the stat table). Until one exists,
        //   set research stat bonuses by hand here: Stat enum name -> amount. Example:
        //     "ResearchBonuses": { "AddAllOff": 10, "MaxHealth": 200 }
        //   These are added on top of the perk bonuses. Names use the AOSharp Stat enum
        //   (same spellings the 'class' command shows), matched case/space-insensitively.
        public Dictionary<string, int> ResearchBonuses = new Dictionary<string, int>();

        // --- NAV (persistent per-playfield walkable memory) ----------------------
        // Records the OWNER's clean footsteps into nav/<playfieldId>.json so ground we've walked is never
        // guessed again, and, when lost, the bot can fall back to a known walked route instead of flailing
        // into a wall. Only owner-walked edges are stored (wall-safe by construction). See NAV_DESIGN.md.
        public bool NavRecord = true;              // master record switch
        public bool NavUse = true;                 // use a saved route as the lost-fallback (after zone-sweep)
        public float NavPointSpacing = 1.5f;       // clean-line thinning, metres (bigger than breadcrumb 0.3)
        public float NavSnapMeters = 15f;          // bot AND owner must be within this of the same recorded run to reuse it
        public float NavCatchupMeters = 18f;       // when this far behind the owner on recorded ground, replay the recorded
                                                   // route (correct ramp Y) instead of beelining sparse live crumbs up a ramp
        public float NavSegmentBreakMeters = 12f;  // a gap bigger than this in the owner track splits the run
        public float NavWeldMeters = 3f;           // join points from different runs within this distance (same walked junction)
        public float NavAutosaveSec = 20f;         // flush the file this often while dirty

        public int TickMs = 200;                   // decision interval (combat/heal/buff)
        public int SendIntervalMs = 100;           // movement packet send throttle
        public float MaxStep = 1.5f;               // hard cap on movement per frame (never warp, even on a lag spike)
        // Lockstep leash: cap how far the dictated body position may lead the server's last-confirmed one
        // while the server is actively correcting us (stops the catch-up rubberband; kept above the ~7m ramp
        // jitter so ordinary follow is untouched). Only enforced while a correction arrived within the window.
        public float MoveLeashMeters = 8f;
        public float MoveLeashWindowSec = 1.5f;
        public float ZoneJumpThreshold = 40f;      // a position jump bigger than this = we zoned/teleported → reset nav
    }
}
