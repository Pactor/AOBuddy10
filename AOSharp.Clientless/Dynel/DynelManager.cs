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
            else
            {
                dynel = simpleCharMsg.Flags.HasFlag(SimpleCharFullUpdateFlags.IsNpc) ? new NpcChar(simpleCharMsg) : (Dynel)new PlayerChar(simpleCharMsg);
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
