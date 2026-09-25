using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AOSharp.Clientless;
using AOSharp.Clientless.Logging;
using AOSharp.Common.GameData;

namespace AOBuddy
{
    /// <summary>
    /// PERMANENT STAT BONUSES — the perk/research layer folded into every stat read (R3.3, moved
    /// verbatim from Main). An AO character's final stats include permanent bonuses the clientless
    /// bot cannot read off the wire as one number:
    ///   * trained PERKS — perk lines grant stacking stat bonuses per level. The login FullCharacter
    ///     carries the trained perk IDS; GameData/perks.sql + Perks.xml map ids -> lines -> bonuses.
    ///     Either the config names the lines (PerkLines override) or they are auto-detected from the
    ///     wire at login (RefreshPerks' id-signature check makes that once-per-change, not per frame).
    ///   * RESEARCH — hand-maintained in config.json as ResearchBonuses (name -> number).
    /// The merged dictionary is pushed onto LocalPlayer's permanent-bonus layer (SetPermanentBonuses),
    /// which the SDK folds into TryGetStat — so heal/support/combat decisions see honest final values.
    /// The SDK re-creates LocalPlayer on full updates, so the layer is re-applied whenever the
    /// INSTANCE changes (ReferenceEquals on the last target; a recompute nulls it to force re-apply).
    /// </summary>
    public class PerkBonuses
    {
        private readonly string _pluginDir;
        private readonly BuddyConfig _config;
        private readonly Action<string> _log;

        private readonly PerkData _perkData = new PerkData();
        private Dictionary<Stat, int> _permanentBonuses = new Dictionary<Stat, int>();
        private LocalPlayer _bonusTarget;
        private string _perkDiag = "Permanent bonuses not initialised.";
        private bool _perkOverride;
        private string _lastPerkSig = null;

        public PerkBonuses(string pluginDir, BuddyConfig config, Action<string> log)
        {
            _pluginDir = pluginDir;
            _config = config;
            _log = log;
        }

        /// <summary>Once at plugin Init: load perks.sql/Perks.xml and compute the first bonus set
        /// (config PerkLines override, or research-only while wire auto-detect waits for login).</summary>
        public void Init()
        {
            try
            {
                string sqlPath = Path.Combine(_pluginDir, "GameData", "perks.sql");
                if (!File.Exists(sqlPath)) sqlPath = Path.Combine("GameData", "perks.sql");
                string xmlPath = Path.Combine(_pluginDir, "GameData", "Perks.xml");
                if (!File.Exists(xmlPath)) xmlPath = Path.Combine("GameData", "Perks.xml");

                bool sqlOk = _perkData.Load(sqlPath);
                bool xmlOk = _perkData.LoadPerkXml(xmlPath);

                if (!sqlOk)
                {
                    _perkDiag = $"perks.sql not loaded (looked in {sqlPath}). Perk bonuses OFF.";
                    Logger.Warning("AOBuddy: " + _perkDiag);
                    _log(_perkDiag);
                    return;
                }

                _perkOverride = _config.PerkLines != null && _config.PerkLines.Count > 0;

                if (_perkOverride)
                {
                    RecomputePerkBonuses(_config.PerkLines, "config override");
                }
                else
                {
                    _permanentBonuses = MergeResearch(new Dictionary<Stat, int>(), out _);
                    _perkDiag =
                        $"perks.sql {(sqlOk ? _perkData.PerkCount + " perks" : "FAIL")}, " +
                        $"Perks.xml {(xmlOk ? _perkData.PerkXmlCount + " ids" : "FAIL — auto-detect OFF")}. " +
                        $"Auto-detecting trained perks from wire at login.";
                }

                Logger.Information("AOBuddy: " + _perkDiag);
                _log(_perkDiag);
            }
            catch (Exception ex)
            {
                _perkDiag = "Permanent-bonus init failed: " + ex.Message;
                Logger.Error("AOBuddy: " + _perkDiag);
                _log(_perkDiag);
            }
        }

        /// <summary>Per frame (Main.OnUpdate, same spot the two calls used to sit): re-detect perk
        /// lines from the wire when the id signature changes, then keep the bonus layer applied to
        /// the current LocalPlayer instance.</summary>
        public void Tick(LocalPlayer me)
        {
            RefreshPerksFromWire(me);
            ApplyPermanentBonuses(me);
        }

        private void RefreshPerksFromWire(LocalPlayer me)
        {
            if (_perkOverride || !_perkData.PerkXmlLoaded) return;

            var perks = me.Perks;
            if (perks == null || perks.Length == 0) return;

            var ids = perks.Select(p => p.SkillId).Where(id => id != 0).OrderBy(id => id).ToList();
            if (ids.Count == 0) return;

            string sig = string.Join(",", ids);
            if (sig == _lastPerkSig) return;
            _lastPerkSig = sig;

            var lines = _perkData.DetectPerkLines(ids, out var unresolved);
            RecomputePerkBonuses(lines, $"wire ({ids.Count} ids, {unresolved.Count} unresolved)");
        }

        private void RecomputePerkBonuses(IEnumerable<string> perkLines, string source)
        {
            var bonuses = _perkData.ComputeBonuses(perkLines, out var unknown, out var unmapped, out var applied);
            bonuses = MergeResearch(bonuses, out var researchUnknown);

            _permanentBonuses = bonuses;
            _bonusTarget = null;

            string sumStr = bonuses.Count == 0 ? "(none)" :
                string.Join(", ", bonuses.OrderBy(b => b.Key.ToString()).Select(b => $"{b.Key}+{b.Value}"));
            _perkDiag =
                $"Perks [{source}]: [{(applied.Count > 0 ? string.Join(", ", applied) : "none")}]. " +
                $"Bonuses: {sumStr}." +
                (unknown.Count > 0 ? $" UNKNOWN perk names: [{string.Join(", ", unknown)}]." : "") +
                (unmapped.Count > 0 ? $" UNMAPPED skills: [{string.Join(", ", unmapped)}]." : "") +
                (researchUnknown.Count > 0 ? $" UNKNOWN research stats: [{string.Join(", ", researchUnknown)}]." : "");
            Logger.Information("AOBuddy: " + _perkDiag);
            _log(_perkDiag);
        }

        private Dictionary<Stat, int> MergeResearch(Dictionary<Stat, int> bonuses, out List<string> researchUnknown)
        {
            researchUnknown = new List<string>();
            if (_config.ResearchBonuses != null)
            {
                foreach (var kv in _config.ResearchBonuses)
                {
                    if (kv.Value == 0) continue;
                    if (PerkData.TryResolveStat(kv.Key, out Stat stat))
                        bonuses[stat] = (bonuses.TryGetValue(stat, out int cur) ? cur : 0) + kv.Value;
                    else
                        researchUnknown.Add(kv.Key);
                }
            }
            return bonuses;
        }

        private void ApplyPermanentBonuses(LocalPlayer me)
        {
            if (ReferenceEquals(_bonusTarget, me)) return;
            me.SetPermanentBonuses(new Dictionary<Stat, int>(_permanentBonuses));
            _bonusTarget = me;
            _log($"PERM BONUSES applied to LocalPlayer ({_permanentBonuses.Count} stats).");
        }

        /// <summary>The 'perks' command: the diagnostic line, live stats with the perm layer, and the
        /// wire-id -> perk-line detection. Output is byte-identical to the old Main.ReportPerks.</summary>
        public void Report(Action<string> reply)
        {
            reply(HelpPages.Truncate(_perkDiag, 440));

            LocalPlayer me = DynelManager.LocalPlayer;
            if (me != null)
            {
                me.TryGetStat(Stat.Strength, out int str);
                me.TryGetStat((Stat)123, out int fa);
                int strBonus = me.PermanentBonuses.TryGetValue(Stat.Strength, out int sb) ? sb : 0;
                reply($"Now: Strength={str} (perm +{strBonus}), FirstAid={fa}. Perm layer holds {me.PermanentBonuses.Count} stats.");

                var perks = me.Perks;
                if (perks != null && perks.Length > 0)
                {
                    var ids = perks.Select(p => p.SkillId).Where(id => id != 0).OrderBy(id => id).ToList();
                    var lines = _perkData.DetectPerkLines(ids, out var unresolved);
                    reply(HelpPages.Truncate($"wire perk ids [{ids.Count}] -> detected: [{(lines.Count > 0 ? string.Join(", ", lines) : "none")}]" +
                        (unresolved.Count > 0 ? $"; {unresolved.Count} unresolved ids (research/unknown): {string.Join(",", unresolved.Take(20))}" : ""), 440));
                }
                else
                {
                    reply("wire perks: empty (no FullCharacter yet, or none trained). " +
                        (_perkOverride ? "Manual override active (config PerkLines)." : "Auto-detect waiting."));
                }
            }
        }
    }
}
