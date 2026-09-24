using System;
using System.Collections.Generic;
using System.Linq;
using AOSharp.Clientless;
using AOSharp.Common.GameData;

namespace AOBuddy
{
    /// <summary>
    /// HUNT — off until the owner says `hunt`. The BOT stays where it is (with the owner); its PETS do the
    /// hunting. While on, and while the owner has no fight of his own, it picks the nearest huntable mob within
    /// a radius of the bot, targets it and sends the attack pets (PetController.EngageTarget, the same command
    /// an owner fight uses). When that mob is dead it picks the next; when none are left the pets are called
    /// back as after any fight; and it keeps looking, so mobs that respawn are taken again.
    ///
    /// The radius is measured from the bot because that is what a pet is leashed to: a pet that strays too far
    /// from its master stops fighting and returns (the owner's account of the mechanic), so a mob past it would
    /// only be abandoned half-fought. A mob the pets never get onto (no way through, or the leash) is set aside
    /// for a while instead of being ordered again and again.
    /// </summary>
    public class HuntController
    {
        private readonly BotContext _ctx;
        private readonly Func<bool> _inMission;

        public bool Active { get; private set; }
        public float Radius { get; private set; }
        public SimpleChar Target { get; private set; }

        private Identity? _current;            // the mob the pets were sent at
        private double _sentAt;                // clock when they were sent
        private int _hpAtSend = -1;
        private readonly Dictionary<Identity, double> _setAside = new Dictionary<Identity, double>();   // until this clock time
        private double _clock;
        private int _kills;
        private double _idleLog;
        private bool _noPetLogged;

        private const float LeashSlack = 5f;           // metres past the radius before a mob is given up
        private const double EngageSeconds = 20.0;     // pets must be on it (or it must be losing health) by then
        private const double SetAsideSeconds = 60.0;

        public HuntController(BotContext ctx, Func<bool> inMission)
        {
            _ctx = ctx; _inMission = inMission;
            Radius = ctx.Config.HuntRadius;
        }

        public void Command(string args, Action<string> reply)
        {
            string a = (args ?? "").Trim().ToLowerInvariant();
            if (a == "off" || a == "stop") { Stop("owner said stop"); reply("Hunt off."); return; }
            if (a == "status")
            {
                reply(Active
                    ? $"Pets hunting within {Radius:0} m of me; {(_current.HasValue ? "on '" + (Target?.Name ?? "?") + "'" : "nothing in range")}; {_kills} down; {_setAside.Count(kv => kv.Value > _clock)} set aside."
                    : "Hunt is off.");
                return;
            }
            if (a.Length > 0)
            {
                if (!float.TryParse(a, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float r) || r < 5f || r > 100f)
                { reply("Usage: hunt [radius 5-100] | hunt off | hunt status"); return; }
                Radius = r;
            }
            Active = true; _kills = 0; _setAside.Clear(); _noPetLogged = false;
            _ctx.Log($"HUNT: on, pets hunt within {Radius:0} m of the bot");
            reply($"Sending my pets at everything within {Radius:0} m of me. 'hunt off' to stop.");
        }

        public void Stop(string why)
        {
            if (!Active) return;
            Active = false;
            _current = null; Target = null;
            _ctx.Log($"HUNT: off ({why}), {_kills} down");
        }

        /// <summary>Called on the decision tick when the owner has no fight. Returns the mob the attack pets
        /// should be on, or null (nothing to hunt, or no attack pet).</summary>
        public SimpleChar Tick(LocalPlayer me, PlayerChar owner, double dt)
        {
            _clock += dt;
            if (!Active || me == null) return null;

            var pets = new HashSet<Identity>(me.Pets.Where(p => p.Role == PetType.Attack || p.Role == PetType.Support).Select(p => p.Identity));
            if (pets.Count == 0)
            {
                if (!_noPetLogged) { _noPetLogged = true; _ctx.Log("HUNT: no attack pet up; waiting for one."); }
                return null;
            }
            _noPetLogged = false;

            Vector3 centre = me.Transform.Position;
            SimpleChar mob = _current.HasValue ? DynelManager.Characters.FirstOrDefault(c => c.Identity == _current.Value) : null;

            if (_current.HasValue && (mob == null || !IsAlive(mob)))
            {
                _kills++;
                _ctx.Log($"HUNT: '{Target?.Name}' done ({_kills} down)");
                _current = null; mob = null;
            }
            else if (mob != null && !OnUs(mob, me, owner, pets) && Flat(mob.Transform.Position, centre) > Radius + LeashSlack)
            {
                _ctx.Log($"HUNT: '{mob.Name}' went past the {Radius:0} m leash; leaving it.");
                SetAside(mob.Identity, 15);
                _current = null; mob = null;
            }
            else if (mob != null && _clock - _sentAt > EngageSeconds && !OnPets(mob, pets) && !Losing(mob))
            {
                _ctx.Log($"HUNT: the pets never got onto '{mob.Name}' in {EngageSeconds:0} s; setting it aside for {SetAsideSeconds:0} s.");
                SetAside(mob.Identity, SetAsideSeconds);
                _current = null; mob = null;
            }

            if (mob == null)
            {
                mob = DynelManager.Npcs
                    .Where(n => Flat(n.Transform.Position, centre) <= Radius
                                && !(_setAside.TryGetValue(n.Identity, out double until) && until > _clock)
                                && IsHuntable(n, me, owner, _inMission(), pets))
                    .OrderBy(n => OnUs(n, me, owner, pets) ? 0 : 1)      // anything already on us or the pets first
                    .ThenBy(n => me.DistanceFrom(n))
                    .FirstOrDefault();
                if (mob == null)
                {
                    Target = null;
                    if ((_idleLog += dt) > 30) { _idleLog = 0; _ctx.Log($"HUNT: nothing huntable within {Radius:0} m"); }
                    return null;
                }
                _current = mob.Identity; _sentAt = _clock; _idleLog = 0;
                _hpAtSend = mob.TryGetStat(Stat.Health, out int hp) ? hp : -1;
                _ctx.Log($"HUNT: pets -> '{mob.Name}' L{mob.Level} id={mob.Identity.Instance} {me.DistanceFrom(mob):0} m away");
            }
            Target = mob;
            return mob;
        }

        // Which NPCs are mobs, from 33 retail captures (6,979 NPCs, 1,312 of them fought; study 2026-09-23,
        // scratchpad mobsignal/). NEVER attacked, with no fought mob among them:
        //   Flags 0x200000 (sells items) - 61/61 vendors, 0 fought
        //   Flags 0x800000               - 28/29 NPCs talked to (quest givers, Colonists), 0 fought
        //   Flags 0x8000000, an Owner, or a PetTypeId - pets, 8 of 1,312 fought (Zix only)
        // What is left still includes guards and civilians (ICC Peacekeepers, Dockworkers, Unicorn Guards),
        // whose data looks like a mob's except for Side: 95% of fought mobs are Side 3 (Monster), and no
        // vendor, quest giver or pet is. So outside missions only Side 3 is hunted. The 5% lost are faction
        // mobs, and mission mobs (sides 0-2). Inside a mission building every NPC left is taken: UNVERIFIED -
        // the captures do not show whether a mission can hold a friendly NPC. Anything already fighting us,
        // the owner or our pets is fair game whatever its side.
        private const int FlagSells = 0x200000, FlagTalk = 0x800000, FlagPet = 0x8000000;
        private const int SideMonster = 3;

        /// <summary>Whether a mob may be hunted.</summary>
        public static bool IsHuntable(NpcChar n, LocalPlayer me, PlayerChar owner, bool inMission, HashSet<Identity> pets)
        {
            if (n == null || !IsAlive(n)) return false;
            int flags = (int)n.Flags;
            if ((flags & (FlagSells | FlagTalk | FlagPet)) != 0 || n.Owner.HasValue || n.PetTypeId != 0) return false;
            // Someone else's fight: a mob fighting anyone other than us, the owner, a teammate or our pets.
            if (n.FightingIdentity.HasValue)
            {
                var f = n.FightingIdentity.Value;
                return f == me.Identity || (owner != null && f == owner.Identity) || pets.Contains(f) || Team.Members.Any(m => m.Identity == f);
            }
            return inMission || (int)n.Side == SideMonster;
        }

        private static bool OnUs(SimpleChar n, LocalPlayer me, PlayerChar owner, HashSet<Identity> pets) =>
            n.FightingIdentity.HasValue && (n.FightingIdentity.Value == me.Identity || (owner != null && n.FightingIdentity.Value == owner.Identity) || pets.Contains(n.FightingIdentity.Value));

        private static bool OnPets(SimpleChar n, HashSet<Identity> pets) =>
            (n.FightingIdentity.HasValue && pets.Contains(n.FightingIdentity.Value))
            || DynelManager.Characters.Any(c => pets.Contains(c.Identity) && c.FightingIdentity.HasValue && c.FightingIdentity.Value == n.Identity);

        private bool Losing(SimpleChar n) => _hpAtSend > 0 && n.TryGetStat(Stat.Health, out int hp) && hp < _hpAtSend;

        private static bool IsAlive(SimpleChar c) => !c.TryGetStat(Stat.Health, out int hp) || hp > 0;

        private static float Flat(Vector3 a, Vector3 b) { float dx = a.X - b.X, dz = a.Z - b.Z; return (float)Math.Sqrt(dx * dx + dz * dz); }

        private void SetAside(Identity id, double seconds) => _setAside[id] = _clock + seconds;
    }
}
