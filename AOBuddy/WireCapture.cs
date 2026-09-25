using System;
using System.IO;
using SmokeLounge.AOtomation.Messaging.Messages;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

namespace AOBuddy
{
    /// <summary>
    /// WIRE CAPTURE / MISSION DIAGNOSTICS (R3.5, moved verbatim from Main's message handler).
    /// Everything gated behind 'missiondbg on': the zone-in packet log + missions/*.bin captures
    /// (offline decoding of the room-placement list the SDK does not parse), and the raw probe for
    /// the untyped mission-terminal list (sig 0x5C436609). Reads MissionDebug per message so the
    /// command's toggle takes effect live. The raw zone-in packet itself is kept by MAIN
    /// (OnZoneIn): the 'navdata' command's mission-layout fallback reads it.
    /// </summary>
    public class WireCapture
    {
        private readonly BuddyConfig _config;
        private readonly string _pluginDir;
        private readonly Action<string> _log;

        public WireCapture(BuddyConfig config, string pluginDir, Action<string> log)
        {
            _config = config;
            _pluginDir = pluginDir;
            _log = log;
        }

        public void OnMessage(Message m)
        {
            if (!_config.MissionDebug || m == null) return;

            // MISSION DEBUG — how much the server tells us about a mission's location/playfield.
            // PlayfieldAnarchyF fires on zone-in (incl. entering a mission): playfield id, our landing
            // coords, and the placed dynels (mobs/objects) = the mission layout the server hands us.
            if (m.Body is PlayfieldAnarchyFMessage pfm)
            {
                _log($"MISSIONDBG: PlayfieldAnarchyF pf={pfm.PlayfieldId1.Instance} land=({pfm.CharacterCoordinates.X:0},{pfm.CharacterCoordinates.Y:0},{pfm.CharacterCoordinates.Z:0}) dynels={(pfm.Dynels != null ? pfm.Dynels.Length : 0)}");
                // The same packet carries the instanced building's room placement list
                // (BuildingGeneratorData: template playfield, then room/floor/x/z/rotation per room),
                // which the SDK does not parse yet. Keep the raw bytes so it can be decoded offline
                // against the walk recorded in the mission. See NAV-CLIENTDATA.md.
                if (m.RawPacket != null)
                {
                    try
                    {
                        string dir = Path.Combine(_pluginDir, "missions");
                        Directory.CreateDirectory(dir);
                        string file = Path.Combine(dir, $"zonein-pf{pfm.PlayfieldId1.Instance}-{DateTime.Now:yyyyMMdd-HHmmss}.bin");
                        File.WriteAllBytes(file, m.RawPacket);
                        _log($"MISSIONDBG: zone-in packet saved ({m.RawPacket.Length} bytes) to {file}");
                    }
                    catch (Exception ex) { _log("MISSIONDBG: could not save zone-in packet: " + ex.Message); }
                }
            }

            // The mission-terminal list (type 0x5C436609) is NOT SDK-typed — read it raw. Per mission
            // it carries the destination playfield + entrance X/Z (see aobuddy-mission-wire-data).
            if (m.RawPacket != null && m.RawPacket.Length >= 0x33)
            {
                byte[] raw = m.RawPacket;
                uint sig = (uint)((raw[16] << 24) | (raw[17] << 16) | (raw[18] << 8) | raw[19]);
                if (sig == 0x5C436609u)
                    _log($"MISSIONDBG: mission-list raw bytes={raw.Length} missions={raw[0x32]} diff={raw[0x1E]} (playfield + entrance per mission are in here; decode via ClickSaver offsets).");
            }
        }
    }
}
