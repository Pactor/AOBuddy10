using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AOBuddy
{
    /// <summary>
    /// The item data the want list matches rewards against (GameData/ItemWantData.bin, "AOWD" v1, made by
    /// tools/eng-gear-extractor --wantdata from items.ocp): which nano program each nano crystal uploads (its use
    /// event's Upload function 53019 - 'Nano Crystal (Shatter Bone)' 82011 -> 82007), and each template's ItemClass
    /// (stat 0x4C: 1 weapon, 2 armor incl. rings and wearables, 3 implant, 4 NPC-only equipment, 5 spirit).
    /// </summary>
    public static class WantData
    {
        public const int Weapon = 1, Armor = 2, Implant = 3, NpcEquip = 4, Spirit = 5;
        private static Dictionary<int, int> _crystalNano = new Dictionary<int, int>();
        private static Dictionary<int, byte> _class = new Dictionary<int, byte>();

        public static int CrystalCount => _crystalNano.Count;

        /// <summary>The nano program a crystal uploads, or 0 when the template is no nano crystal.</summary>
        public static int NanoOf(int crystal) => _crystalNano.TryGetValue(crystal, out int n) ? n : 0;
        public static bool IsCrystal(int template) => _crystalNano.ContainsKey(template);
        /// <summary>ItemClass (see the constants), 0 when unknown.</summary>
        public static int ClassOf(int template) => _class.TryGetValue(template, out byte c) ? c : 0;
        public static IEnumerable<KeyValuePair<int, int>> Crystals => _crystalNano;

        public static void Load(string pluginDir, Action<string> log)
        {
            string file = Path.Combine(pluginDir, "GameData", "ItemWantData.bin");
            try
            {
                using (var r = new BinaryReader(File.OpenRead(file)))
                {
                    if (Encoding.ASCII.GetString(r.ReadBytes(4)) != "AOWD" || r.ReadInt32() != 1) { log($"WANTDATA: {file} is not a version 1 table."); return; }
                    int n = r.ReadInt32();
                    var cn = new Dictionary<int, int>(n);
                    for (int i = 0; i < n; i++) { int c = r.ReadInt32(); cn[c] = r.ReadInt32(); }
                    int m = r.ReadInt32();
                    var cl = new Dictionary<int, byte>(m);
                    for (int i = 0; i < m; i++) { int id = r.ReadInt32(); cl[id] = r.ReadByte(); }
                    _crystalNano = cn; _class = cl;
                    log($"WANTDATA: {cn.Count} nano crystals, {cl.Count} item classes loaded.");
                }
            }
            catch (Exception ex) { log($"WANTDATA: couldn't load {file}: {ex.Message}"); }
        }
    }
}
