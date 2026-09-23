using AOSharp.Common.GameData;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace AOSharp.Clientless
{
    public static class DynelManager
    {
        public static EventHandler<Dynel> DynelSpawned;
        public static EventHandler<Dynel> DynelDespawned;
        public static Action<Identity, Identity> DynelUsed;

        public static ReadOnlyCollection<Dynel> AllDynels => _dynels.Values.ToList().AsReadOnly();
        public static ReadOnlyCollection<NpcChar> Npcs => _dynels.Values.Where(x => x is NpcChar).Cast<NpcChar>().ToList().AsReadOnly();
        public static ReadOnlyCollection<PlayerChar> Players => _dynels.Values.Where(x => x is PlayerChar).Cast<PlayerChar>().ToList().AsReadOnly();

        public static ReadOnlyCollection<SimpleChar> Characters => _dynels.Values.Where(x => x is SimpleChar).Cast<SimpleChar>().ToList().AsReadOnly();
        public static ReadOnlyCollection<VendingMachine> VendingMachines => _dynels.Values.Where(x => x is VendingMachine).Cast<VendingMachine>().ToList().AsReadOnly();

        internal static LocalPlayerProxy LocalPlayerProxy = new LocalPlayerProxy();

        public static LocalPlayer LocalPlayer => LocalPlayerProxy.LocalPlayer;

        private static Dictionary<Identity, Dynel> _dynels = new Dictionary<Identity, Dynel>();

        // PETS THE SERVER HAS NAMED AS OURS (AddPet / RemovePet). This is the only statement of ownership
        // that always arrives: the per-update pet-master bit (Flags2 0x4) is absent on some pets entirely -
        // an MP's heal pet spawned with Flags2=0x2, so it never entered me.Pets, was never commanded, and
        // every post-fight recall reported "a pet we own is out of sight" for a pet standing right there.
        // Kept by INSTANCE because a pet's dynel identity and the identity in pet messages differ in Type.
        private static readonly HashSet<int> _serverOwnedPets = new HashSet<int>();

        /// <summary>The server said this pet is ours. Claim its dynel now if we have it, and remember the
        /// instance so a later (or rebuilt) full update is claimed too.</summary>
        internal static void OnPetAdded(Identity pet)
        {
            _serverOwnedPets.Add(pet.Instance);

            LocalPlayer me = LocalPlayer;
            if (me == null) return;
            foreach (NpcChar npc in Npcs)
                if (npc.Identity.Instance == pet.Instance)
                    npc.Owner = me.Identity;
        }

        /// <summary>The server said this pet is gone. Stop claiming it.</summary>
        internal static void OnPetRemoved(Identity pet)
        {
            _serverOwnedPets.Remove(pet.Instance);

            foreach (NpcChar npc in Npcs)
                if (npc.Identity.Instance == pet.Instance)
                    npc.Owner = null;
        }

        public static bool Find<T>(Identity identity, out T dynel) where T : Dynel
        {
            return (dynel = (T)AllDynels.FirstOrDefault(x => x is T && x.Identity == identity)) != null;
        }

        public static bool Find<T>(string name, out T dynel) where T : SimpleChar
        {
            return (dynel = (T)AllDynels.Cast<SimpleChar>().FirstOrDefault(x => x is T && x.Name == name)) != null;
        }

        internal static void OnDynelUsed(Identity user, Identity target)
        {
            DynelUsed?.Invoke(user, target);
        }

        internal static void OnDynelSpawned(VendingMachineFullUpdateMessage vendMachineMsg)
        {
            if (vendMachineMsg.Position != null)
                OnDynelSpawned(new VendingMachine(vendMachineMsg.Identity, vendMachineMsg.Position.Value, vendMachineMsg.Rotation.Value, vendMachineMsg.Stats));
        }

        internal static void OnDynelSpawned(SimpleCharFullUpdateMessage simpleCharMsg)
        {
            Dynel dynel;

            if (simpleCharMsg.Identity.Instance == Client.LocalDynelId)
            {
                LocalPlayerProxy.ApplySimpleCharFullUpdate(simpleCharMsg);
                dynel = LocalPlayer;
            }
            else if (simpleCharMsg.Flags.HasFlag(SimpleCharFullUpdateFlags.IsNpc))
            {
                var npc = new NpcChar(simpleCharMsg);
                bool existed = _dynels.TryGetValue(npc.Identity, out Dynel prev);

                // PET OWNERSHIP PERSISTENCE. A pet's master (Flags2 0x4, wire-proven in scfuwire.log) is only
                // carried on SOME full updates — the spawn announce, not steady-state ones (a re-summoned pet's
                // next update reads Flags2=0x2, no owner). Because every full update builds a BRAND-NEW NpcChar
                // that OVERWRITES the old one in _dynels, an owner-less update wiped the Owner set at spawn ->
                // me.Pets went empty -> the bot stopped commanding the pet and spammed resummon. Carry the known
                // owner forward when this update doesn't bring one, so ownership survives steady-state rebuilds.
                if (npc.Owner == null && _serverOwnedPets.Contains(npc.Identity.Instance) && LocalPlayer != null)
                {
                    // The server already named this one as ours; nothing about a later update revokes that.
                    npc.Owner = LocalPlayer.Identity;
                }
                else if (npc.Owner == null && existed && prev is NpcChar prevNpc && prevNpc.Owner != null)
                {
                    npc.Owner = prevNpc.Owner;
                }
                // PET OWNERSHIP BY SUMMON. When 0x4 never comes (some pets never send it), fall back to the
                // owner's rule: "if he summoned it, it's his." A brand-NEW NPC that spawns right next to us
                // while our pet-summon claim window is open (LocalPlayer.ExpectingPetUntilMs, set when we cast
                // a summon nano) is our pet — claim it so it enters me.Pets, gets commanded, and stops the
                // false resummon loop. Tight radius so a mob that happens to spawn during the window isn't mis-claimed.
                else if (npc.Owner == null && !existed)
                {
                    LocalPlayer me = LocalPlayer;
                    if (me != null && Environment.TickCount64 <= me.ExpectingPetUntilMs
                        && Vector3.Distance(npc.Transform.Position, me.Transform.Position) <= 8f)
                        npc.Owner = me.Identity;
                }
                dynel = npc;
            }
            else
            {
                dynel = new PlayerChar(simpleCharMsg);
            }

            OnDynelSpawned(dynel);
        }

        internal static void OnDynelSpawned(Dynel dynel)
        {
            // Overwrite rather than Add: a re-spawn of an existing dynel (common when
            // zoning / re-entering an area) would otherwise throw a duplicate-key
            // exception and kill packet processing.
            _dynels[dynel.Identity] = dynel;
            DynelSpawned?.Invoke(null, dynel);
        }

        internal static void OnDynelDespawned(Identity identity)
        {
            if (_dynels.TryGetValue(identity, out Dynel dynel))
            {
                DynelDespawned?.Invoke(null, dynel);
                _dynels.Remove(identity);
            }
        }

        internal static void OnDynelMovementChanged(Identity identity, Vector3 pos, Quaternion heading, MovementAction moveAction)
        {
            if (_dynels.TryGetValue(identity, out Dynel dynel))
            {
                dynel.Transform.Position = pos;
                dynel.Transform.Heading = heading;
            }
        }

        /// <summary>
        /// Fired when the server SetPos-corrects the LOCAL player's position. The argument is the
        /// server's authoritative coordinates the local player has been snapped to.
        /// </summary>
        public static EventHandler<Vector3> LocalPlayerCorrected;

        // The server sends SetPosMessage as a routine position correction. It is NOT safe to snap the
        // LOCAL player to it: the server emits these constantly while the bot climbs a ramp (ground
        // re-assertion), and applying them pins the bot back to the ramp base every ~0.5s = the
        // "bouncing on the ramp" bug. A clientless controller drives its own position (SetPose) and,
        // like the real client here, must NOT teleport itself back on every correction. So for the
        // local player we only NOTIFY (for diagnostics) and leave its position alone. Other dynels
        // (mobs/pets) are server-authoritative, so applying their corrections is fine.
        internal static void OnServerSetPos(Identity identity, Vector3 pos)
        {
            // DIAGNOSTIC ONLY. We never apply SetPos to anything (applying it to the local player is
            // what warps the bot). For the local player we only fire the observe event so the plugin
            // can log the server's position for a desync check; for other dynels we do nothing.
            LocalPlayer local = LocalPlayer;
            if (local != null && identity == local.Identity)
                LocalPlayerCorrected?.Invoke(null, pos);   // observe only — does NOT move the bot
        }

        internal static void OnOrgInfoPacket(OrgInfoPacketMessage orgInfoPacketMsg)
        {
            if (_dynels.TryGetValue(orgInfoPacketMsg.Identity, out Dynel dynel) && dynel is PlayerChar player)
            {
                //player.SetStat(Stat.Clan, orgInfoPacketMsg.OrgId);
                player.OrgName = orgInfoPacketMsg.Name;
            }
        }

        internal static void InitStaticDynels(PlayfieldId playfieldId)
        {
            if (!StaticDynelData.GetDynels(playfieldId, out List<StaticDynel> dynels))
                return;

            foreach (var dynel in dynels)
            {
                _dynels.Add(dynel.Identity, dynel);
            }
        }

        internal static void Reset()
        {
            _dynels.Clear();
        }
    }
}
