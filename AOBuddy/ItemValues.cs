using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AOBuddy
{
    /// <summary>
    /// Item Value (stat 74) per item template, and each shop terminal template's Sell/BuyModifier (427/426),
    /// none of which the SDK's item pack carries. Extracted once from OmniCell's items.ocp into
    /// GameData/ItemValues.bin (tools/mp-itemvalues), version 2:
    ///   "AOBV", 2, count, per template int id, int ql, int value
    ///   count, per terminal template int id, int sellModifier, int buyModifier
    ///
    /// Between its low and high template an item's Value follows the SQUARE of how far its QL is along the
    /// range — unlike its requirements, which are linear.
    ///
    /// What a terminal charges (ShopPrice) is Value x SellModifier/100 x the buyer's Computer Literacy
    /// discount, 1% per full 40 CL. Checked against eight Health and Nano Stim prices at ICC Fair Trade,
    /// QL30-200 over the Basic (SellModifier 105) and Advanced (505) pharmacy: the buyer's CL put the
    /// discount at 4%, and every price matched to within 2 credits. The terminal's own spawn packet does
    /// NOT carry the modifier — only its template (StaticInstance) does.
    /// </summary>
    public static class ItemValues
    {
        private static Dictionary<int, (int Ql, int Value)> _values;
        private static Dictionary<int, (int Sell, int Buy)> _shops;

        public static int Count => _values?.Count ?? 0;

        // NODROP templates: item Flags (stat 0) bit 26 (OmniCell ItemFlags.NoDrop / ItemTemplate.IsNoDrop),
        // extracted from items.ocp by tools/eng-gear-extractor --nodrop into GameData/ItemNoDrop.bin
        // ("AOND", 1, count, ids). 11,050 of 120,842 templates (quest items, access cards, pet shells...).
        private static HashSet<int> _noDrop;
        public static bool IsNoDrop(int lowId, int highId) => _noDrop != null && (_noDrop.Contains(lowId) || _noDrop.Contains(highId));

        // IMPLANTS: ItemClass (stat 0x4C) = 3, from items.ocp by tools/eng-gear-extractor --implants into
        // GameData/ItemImplants.bin ("AOIM", 1, count, ids). For the owner's keep-and-bank rule by QL.
        private static HashSet<int> _implants;
        public static bool IsImplant(int lowId, int highId) => _implants != null && (_implants.Contains(lowId) || _implants.Contains(highId));

        private static HashSet<int> ReadIds(string file, string magic, Action<string> log, string what)
        {
            try
            {
                using (var r = new BinaryReader(File.OpenRead(file)))
                {
                    if (Encoding.ASCII.GetString(r.ReadBytes(4)) != magic || r.ReadInt32() != 1) { log($"ITEMVALUES: {file} is not a version 1 {what} table."); return null; }
                    int n = r.ReadInt32();
                    var set = new HashSet<int>();
                    for (int i = 0; i < n; i++) set.Add(r.ReadInt32());
                    log($"ITEMVALUES: {set.Count} {what} templates loaded.");
                    return set;
                }
            }
            catch (Exception ex) { log($"ITEMVALUES: couldn't load {file}: {ex.Message}"); return null; }
        }

        public static void Load(string pluginDir, Action<string> log)
        {
            _values = new Dictionary<int, (int, int)>();
            _shops = new Dictionary<int, (int, int)>();
            string file = Path.Combine(pluginDir, "GameData", "ItemValues.bin");
            try
            {
                using (var r = new BinaryReader(File.OpenRead(file)))
                {
                    if (Encoding.ASCII.GetString(r.ReadBytes(4)) != "AOBV" || r.ReadInt32() != 2)
                    {
                        log($"ITEMVALUES: {file} is not a version 2 value table.");
                        return;
                    }
                    int count = r.ReadInt32();
                    for (int i = 0; i < count; i++)
                    {
                        int id = r.ReadInt32(), ql = r.ReadInt32(), value = r.ReadInt32();
                        _values[id] = (ql, value);
                    }
                    count = r.ReadInt32();
                    for (int i = 0; i < count; i++)
                    {
                        int id = r.ReadInt32(), sell = r.ReadInt32(), buy = r.ReadInt32();
                        _shops[id] = (sell, buy);
                    }
                }
                log($"ITEMVALUES: {_values.Count} item values, {_shops.Count} shop terminal modifiers loaded.");
            }
            catch (Exception ex) { log($"ITEMVALUES: couldn't load {file}: {ex.Message}"); }
            _noDrop = ReadIds(Path.Combine(pluginDir, "GameData", "ItemNoDrop.bin"), "AOND", log, "NODROP");
            _implants = ReadIds(Path.Combine(pluginDir, "GameData", "ItemImplants.bin"), "AOIM", log, "implant");
        }

        public static bool TryGet(int lowId, int highId, int ql, out int value)
        {
            value = 0;
            if (_values == null || !_values.TryGetValue(lowId, out var low)) return false;
            if (highId == lowId || !_values.TryGetValue(highId, out var high) || high.Ql == low.Ql)
            {
                value = low.Value;
                return true;
            }
            double t = (double)(ql - low.Ql) / (high.Ql - low.Ql);
            value = (int)Math.Round(low.Value + (high.Value - low.Value) * t * t);
            return true;
        }

        // A terminal template's markups, in percent. The terminal names its template in StaticInstance.
        public static bool TryGetShopModifiers(int terminalTemplate, out int sellModifier, out int buyModifier)
        {
            sellModifier = buyModifier = 0;
            if (_shops == null || !_shops.TryGetValue(terminalTemplate, out var m)) return false;
            sellModifier = m.Sell; buyModifier = m.Buy;
            return true;
        }

        // Computer Literacy knocks 1% off a shop's price per full 40 points.
        public static double ClDiscount(int computerLiteracy) => 1.0 - (computerLiteracy / 40) / 100.0;

        // What a terminal charges this buyer for one item of this Value.
        public static int ShopPrice(int value, int sellModifier, int computerLiteracy)
            => (int)Math.Round(value * (sellModifier / 100.0) * ClDiscount(computerLiteracy));
    }
}
