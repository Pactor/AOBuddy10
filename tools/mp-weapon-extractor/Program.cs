using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

// MP weapon extractor.
//
// Reads the full converted OmniCell item pack (Datafiles/items.ocp, format
// "OMNICELL-CONTENT" v3 as defined by OmniCell.Core.Content.OmniCellContentPack).
// This parser replicates that reader byte-for-byte so no external DLLs are needed.
//
// A weapon's wield requirements (wield skill, allowed special attacks, profession,
// level) live in its ToWield action (OmniCell.Enums.ActionType.ToWield == 8).
// Operator.GreaterThan == 2 means "stat >= Value". No data is invented; every field
// is read from items.ocp and names are joined from itemnames.sql.

class Program
{
    // ActionType (OmniCell.Enums.ActionType)
    const int ToWield = 8;
    const int ToWear = 6;
    const int ToUse = 3;

    // Operator (OmniCell.Enums.Operator)
    const int OpEqualTo = 0, OpLessThan = 1, OpGreaterThan = 2, OpOr = 3, OpAnd = 4,
              OpBitAnd = 22, OpUnequal = 24, OpNot = 42, OpNotBitAnd = 107;

    static readonly Dictionary<int, string> WieldSkills = new()
    {
        {0x64,"MartialArts"},{0x65,"MultiMelee"},{0x66,"1hBlunt"},{0x67,"1hEdged"},
        {0x68,"MeleeEnergy"},{0x69,"2hEdged"},{0x6A,"Piercing"},{0x6B,"2hBlunt"},
        {0x6C,"SharpObject"},{0x6D,"Grenade"},{0x6E,"HeavyWeapons"},{0x6F,"Bow"},
        {0x70,"Pistol"},{0x71,"Rifle"},{0x72,"MGSMG"},{0x73,"Shotgun"},
        {0x74,"AssaultRifle"},{0x85,"RangedEnergy"},{0x86,"MultiRanged"},
    };

    // On-demand special-attack stats (the 9 the player trains to use a weapon's specials).
    static readonly Dictionary<int, string> Specials = new()
    {
        {0x8E,"Brawl"},{0x90,"Dimach"},{0x92,"SneakAttack"},{0x93,"FastAttack"},
        {0x94,"Burst"},{0x96,"FlingShot"},{0x97,"AimedShot"},{0xA7,"FullAuto"},
        {0x1E9,"Backstab"},
    };

    // Every trainable skill stat (AOSharp.Common.GameData.Stat). Used to build the
    // COMPLETE "skills you must train to equip" list from ToWield, minus the specials
    // above. Includes MultiMelee(101) and MultiRanged(134) for dual-wieldable weapons.
    static readonly Dictionary<int, string> AllSkills = new()
    {
        {0x64,"MartialArts"},{0x65,"MultiMelee"},{0x66,"1hBlunt"},{0x67,"1hEdged"},
        {0x68,"MeleeEnergy"},{0x69,"2hEdged"},{0x6A,"Piercing"},{0x6B,"2hBlunt"},
        {0x6C,"SharpObject"},{0x6D,"Grenade"},{0x6E,"HeavyWeapons"},{0x6F,"Bow"},
        {0x70,"Pistol"},{0x71,"Rifle"},{0x72,"MGSMG"},{0x73,"Shotgun"},
        {0x74,"AssaultRifle"},{0x75,"VehicleWater"},{0x76,"MeleeInit"},{0x77,"RangedInit"},
        {0x78,"PhysicalInit"},{0x79,"BowSpecialAttack"},{0x7A,"SensoryImprovement"},
        {0x7B,"FirstAid"},{0x7C,"Treatment"},{0x7D,"MechanicalEngineering"},
        {0x7E,"ElectricalEngineering"},{0x7F,"MaterialMetamorphosis"},{0x80,"BiologicalMetamorphosis"},
        {0x81,"PsychologicalModification"},{0x82,"MaterialCreation"},{0x83,"SpaceTime"},
        {0x84,"NanoPool"},{0x85,"RangedEnergy"},{0x86,"MultiRanged"},{0x87,"TrapDisarm"},
        {0x88,"Perception"},{0x89,"Adventuring"},{0x8A,"Swimming"},{0x8B,"VehicleAir"},
        {0x8C,"MapNavigation"},{0x8D,"Tutoring"},{0x8F,"Riposte"},{0x91,"Parry"},
        {0x95,"NanoCInit"},{0x98,"BodyDevelopment"},{0x99,"DuckExp"},{0x9A,"DodgeRanged"},
        {0x9B,"EvadeClsC"},{0x9C,"RunSpeed"},{0x9D,"QuantumFT"},{0x9E,"WeaponSmithing"},
        {0x9F,"Pharmaceuticals"},{0xA0,"NanoProgramming"},{0xA1,"ComputerLiteracy"},
        {0xA2,"Psychology"},{0xA3,"Chemistry"},{0xA4,"Concealment"},{0xA5,"BreakingEntry"},
        {0xA6,"VehicleGround"},{0xA8,"NanoResist"},
    };

    const int StatProfession = 0x3C; // 60
    const int StatLevel = 0x36;      // 54
    const int StatExpansion = 0x185; // 389 (ExpansionFlags bitfield)

    // AOSharp.Common ExpansionFlags / OmniCell.Enums.Expansions (authoritative bit map).
    static readonly (int bit, string name)[] ExpansionBits = new[]
    {
        (1<<0,"NotumWars"),(1<<1,"Shadowlands"),(1<<2,"ShadowlandsPreOrder"),
        (1<<3,"AlienInvasion"),(1<<4,"AlienInvasionPreOrder"),(1<<5,"LostEden"),
        (1<<6,"LostEdenPreOrder"),(1<<7,"LegacyOfTheXan"),(1<<8,"LegacyOfTheXanPreOrder"),
    };
    static string ExpansionName(int value)
    {
        var hits = ExpansionBits.Where(b => (value & b.bit) == b.bit).Select(b => b.name).ToList();
        return hits.Count > 0 ? string.Join("+", hits) : $"raw:{value}";
    }

    static readonly Dictionary<int, string> ProfNames = new()
    {
        {1,"Soldier"},{2,"MartialArtist"},{3,"Engineer"},{4,"Fixer"},{5,"Agent"},
        {6,"Adventurer"},{7,"Trader"},{8,"Bureaucrat"},{9,"Enforcer"},{10,"Doctor"},
        {11,"NanoTechnician"},{12,"Metaphysicist"},{13,"Monster"},{14,"Keeper"},{15,"Shade"},
    };

    static readonly Dictionary<int,int> UnclassifiedToWieldStats = new();

    class Req { public int ChildOperator, Operator, Statnumber, Target, Value; }

    class Weapon
    {
        public int Id, Ql;
        public string Name, ItemType;
        public List<(string skill, int req)> Wield = new();
        public List<(string special, int req)> SpecialsReq = new();
        public List<string> ProfReqs = new();
        public bool MpUsable = true;
        public int LevelReq;
        public string PrimaryWield;
        public string ExpansionReq; // authoritative: ExpansionFlags required to wield, or null
    }

    static void Main(string[] args)
    {
        string ocp = @"E:\Funcom\OmniCell\OmniCell\Datafiles\items.ocp";
        string namesPath = @"E:\Funcom\attic\extracted-client-data\itemnames.sql";
        string outPath = args.Length > 0 ? args[0]
            : Path.Combine(Path.GetTempPath(), "mp_weapons_raw.json");

        var names = LoadNames(namesPath);
        Console.WriteLine($"names loaded: {names.Count}");

        var weapons = new List<Weapon>();
        var profCombos = new Dictionary<string, int>();
        int total = 0;

        using (var fs = File.OpenRead(ocp))
        using (var gz = new GZipStream(fs, CompressionMode.Decompress))
        using (var r = new BinaryReader(gz, Encoding.UTF8))
        {
            string magic = r.ReadString();
            if (magic != "OMNICELL-CONTENT") throw new InvalidDataException("bad magic: " + magic);
            int version = r.ReadInt32();
            byte kind = r.ReadByte(); // 1 = Items
            int count = r.ReadInt32();
            Console.WriteLine($"pack: magic='{magic}' version={version} kind={kind} count={count}");

            for (int i = 0; i < count; i++)
            {
                int id = r.ReadInt32();
                int flags = r.ReadInt32();
                int itemType = r.ReadInt32();
                int multipleCount = r.ReadInt32();
                int nothing = r.ReadInt32();
                int quality = r.ReadInt32();
                SkipDict(r);              // Attack
                SkipDict(r);              // Defend
                SkipDict(r);              // Stats
                SkipIntList(r);           // Relations
                var actions = ReadActions(r);
                ReadEvents(r, version);   // parse to stay aligned
                if (version >= 2) ReadRecordData(r, version);
                total++;

                // Weapon = has a ToWield action whose requirements include a WEAPON wield skill.
                var toWield = actions.FirstOrDefault(a => a.type == ToWield);
                if (toWield.reqs == null) continue;
                var reqs = toWield.reqs;
                var weaponSkillReqs = reqs.Where(x => WieldSkills.ContainsKey(x.Statnumber)).ToList();
                if (weaponSkillReqs.Count == 0) continue;

                var w = new Weapon { Id = id, Ql = quality };
                if (names.TryGetValue(id, out var nm)) { w.Name = nm.name; w.ItemType = nm.type; }
                else { w.Name = "(no name)"; w.ItemType = "?"; }

                // Complete "skills you must train to equip": every ToWield requirement whose
                // stat is a trainable skill, EXCLUDING the special-attack stats (those go to
                // `specials`). Preserves item order.
                foreach (var x in reqs.Where(x => AllSkills.ContainsKey(x.Statnumber) && !Specials.ContainsKey(x.Statnumber)))
                    w.Wield.Add((AllSkills[x.Statnumber], x.Value));

                // Primary = highest-value WEAPON wield skill.
                var pw = weaponSkillReqs.OrderByDescending(x => x.Value).First();
                w.PrimaryWield = WieldSkills[pw.Statnumber];

                // Audit anything in ToWield we did not classify as skill/special/prof/level.
                foreach (var x in reqs.Where(x => !AllSkills.ContainsKey(x.Statnumber)
                        && !Specials.ContainsKey(x.Statnumber)
                        && x.Statnumber != StatProfession && x.Statnumber != StatLevel && x.Statnumber != 0))
                    UnclassifiedToWieldStats[x.Statnumber] = UnclassifiedToWieldStats.GetValueOrDefault(x.Statnumber) + 1;

                foreach (var x in reqs.Where(x => Specials.ContainsKey(x.Statnumber)))
                    w.SpecialsReq.Add((Specials[x.Statnumber], x.Value));

                foreach (var x in reqs.Where(x => x.Statnumber == StatProfession))
                {
                    string op = OpName(x.Operator);
                    string p = ProfNames.TryGetValue(x.Value, out var pn) ? pn : x.Value.ToString();
                    w.ProfReqs.Add($"Profession {op} {p}");
                    profCombos[$"{op} {p} (val {x.Value})"] = profCombos.GetValueOrDefault($"{op} {p} (val {x.Value})") + 1;
                    // MP exclusion heuristics (12 = Metaphysicist):
                    if (x.Operator == OpEqualTo && x.Value != 12) w.MpUsable = false;
                    if (x.Operator == OpUnequal && x.Value == 12) w.MpUsable = false;
                    if (x.Operator == OpNot && x.Value == 12) w.MpUsable = false;
                }

                var lvls = reqs.Where(x => x.Statnumber == StatLevel).Select(x => x.Value);
                w.LevelReq = lvls.Any() ? lvls.Max() : 0;

                var exp = reqs.FirstOrDefault(x => x.Statnumber == StatExpansion);
                if (exp != null) w.ExpansionReq = ExpansionName(exp.Value);

                weapons.Add(w);
            }
        }

        Console.WriteLine($"templates read: {total}   weapons found: {weapons.Count}");
        Console.WriteLine("--- non-skill/non-special/non-prof/non-level stats seen in ToWield (audit) ---");
        foreach (var kv in UnclassifiedToWieldStats.OrderByDescending(x => x.Value).Take(30))
            Console.WriteLine($"  stat {kv.Key} (0x{kv.Key:X}) : {kv.Value}");
        int mmCount = weapons.Count(w => w.Wield.Any(k => k.skill == "MultiMelee"));
        int mrCount = weapons.Count(w => w.Wield.Any(k => k.skill == "MultiRanged"));
        Console.WriteLine($"  weapons with MultiMelee in wield: {mmCount}   with MultiRanged in wield: {mrCount}");
        Console.WriteLine("--- distinct profession criteria on weapons ---");
        foreach (var kv in profCombos.OrderByDescending(x => x.Value))
            Console.WriteLine($"  {kv.Key} : {kv.Value}");

        var bb = weapons.FirstOrDefault(x => x.Id == 271437);
        Console.WriteLine("--- verify id 271437 (Bow-Blaster - 402) ---");
        Console.WriteLine(bb == null ? "  NOT FOUND"
            : $"  name='{bb.Name}' ql={bb.Ql} wield=[{Fmt(bb.Wield)}] specials=[{FmtS(bb.SpecialsReq)}] profReq=[{string.Join(";",bb.ProfReqs)}] lvl={bb.LevelReq}");

        Console.WriteLine("--- weapon counts per primary wield skill (all professions) ---");
        foreach (var g in weapons.GroupBy(x => x.PrimaryWield).OrderByDescending(g => g.Count()))
            Console.WriteLine($"  {g.Key} : {g.Count()}  (MP-usable: {g.Count(x => x.MpUsable)})");

        var opts = new JsonSerializerOptions { WriteIndented = true };
        var dump = weapons.OrderBy(x => x.PrimaryWield).ThenBy(x => x.Ql).Select(w => new
        {
            w.Id, w.Name, w.ItemType, w.Ql, w.PrimaryWield,
            Wield = w.Wield.Select(k => new { skill = k.skill, req = k.req }),
            Specials = w.SpecialsReq.Select(k => new { special = k.special, req = k.req }),
            ProfReqs = w.ProfReqs, MpUsable = w.MpUsable, LevelReq = w.LevelReq, ExpansionReq = w.ExpansionReq
        });
        File.WriteAllText(outPath, JsonSerializer.Serialize(dump, opts));
        Console.WriteLine($"raw dump written: {outPath}  ({weapons.Count} weapons)");
    }

    static string Fmt(List<(string skill,int req)> l) => string.Join(",", l.Select(k => k.skill + ">=" + k.req));
    static string FmtS(List<(string special,int req)> l) => string.Join(",", l.Select(k => k.special + ">=" + k.req));

    static string OpName(int op) => op switch
    {
        OpEqualTo => "EqualTo", OpLessThan => "LessThan", OpGreaterThan => "GreaterThan",
        OpUnequal => "Unequal", OpBitAnd => "BitAnd", OpNotBitAnd => "NotBitAnd", OpNot => "Not",
        OpOr => "Or", OpAnd => "And", _ => $"Op{op}"
    };

    // ---- format readers (mirror OmniCellContentPack) ----
    static void SkipDict(BinaryReader r) { int c = r.ReadInt32(); for (int i = 0; i < c; i++) { r.ReadInt32(); r.ReadInt32(); } }
    static void SkipIntList(BinaryReader r) { int c = r.ReadInt32(); for (int i = 0; i < c; i++) r.ReadInt32(); }
    static List<int> ReadIntList(BinaryReader r) { int c = r.ReadInt32(); var l = new List<int>(c); for (int i = 0; i < c; i++) l.Add(r.ReadInt32()); return l; }
    static byte[] ReadBytes(BinaryReader r) { int c = r.ReadInt32(); return r.ReadBytes(c); }

    static List<Req> ReadRequirements(BinaryReader r)
    {
        int c = r.ReadInt32();
        var l = new List<Req>(c);
        for (int i = 0; i < c; i++)
            l.Add(new Req { ChildOperator = r.ReadInt32(), Operator = r.ReadInt32(), Statnumber = r.ReadInt32(), Target = r.ReadInt32(), Value = r.ReadInt32() });
        return l;
    }

    static List<(int type, List<Req> reqs)> ReadActions(BinaryReader r)
    {
        int c = r.ReadInt32();
        var l = new List<(int, List<Req>)>(c);
        for (int i = 0; i < c; i++) { int t = r.ReadInt32(); l.Add((t, ReadRequirements(r))); }
        return l;
    }

    static void ReadEvents(BinaryReader r, int version)
    {
        int c = r.ReadInt32();
        for (int i = 0; i < c; i++)
        {
            r.ReadInt32(); // EventType
            int fns = r.ReadInt32();
            for (int j = 0; j < fns; j++) ReadFunction(r, version);
        }
    }

    static void ReadFunction(BinaryReader r, int version)
    {
        r.ReadInt32(); r.ReadInt32(); r.ReadInt32(); r.ReadUInt32(); r.ReadBoolean(); // FunctionType,Target,TickCount,TickInterval,dolocalstats
        ReadRequirements(r);
        int args = r.ReadInt32();
        for (int i = 0; i < args; i++)
        {
            byte kind = r.ReadByte();
            if (kind == 1) r.ReadInt32();
            else if (kind == 2) r.ReadSingle();
            else if (kind == 3) r.ReadString();
            else throw new InvalidDataException("bad arg kind " + kind);
        }
        if (version >= 2 && r.ReadBoolean())
        {
            r.ReadInt32(); r.ReadInt32(); r.ReadInt32(); r.ReadInt32(); // LeadingZeroWords,H1,H2,H3
            SkipIntList(r);   // RequirementTriples
            ReadBytes(r);     // Arguments
        }
    }

    static void ReadRecordData(BinaryReader r, int version)
    {
        if (!r.ReadBoolean()) return;
        r.ReadInt32(); r.ReadInt32(); r.ReadInt32(); // HeaderA,B,C
        r.ReadString();                              // Description
        SkipIntList(r);                              // BlockOrder
        int groups = r.ReadInt32();
        for (int i = 0; i < groups; i++) { r.ReadInt32(); SkipIntList(r); }
        SkipIntList(r);                              // Block6Pairs
        int sets = r.ReadInt32();
        for (int i = 0; i < sets; i++)
        {
            r.ReadInt32(); r.ReadInt32();            // BlockKey,Value
            int entries = r.ReadInt32();
            for (int j = 0; j < entries; j++) { r.ReadInt32(); SkipIntList(r); }
        }
        int actions = r.ReadInt32();
        for (int i = 0; i < actions; i++) { r.ReadInt32(); SkipIntList(r); }
        int shops = r.ReadInt32();
        for (int i = 0; i < shops; i++)
        {
            r.ReadInt32();                           // EventType
            int entries = r.ReadInt32();
            for (int j = 0; j < entries; j++) ReadBytes(r);
        }
        if (version >= 3)
        {
            int fns = r.ReadInt32();
            for (int i = 0; i < fns; i++) ReadFunction(r, version);
        }
    }

    static Dictionary<int, (string name, string type)> LoadNames(string path)
    {
        var d = new Dictionary<int, (string, string)>(150000);
        string text = File.ReadAllText(path);
        var rx = new Regex(@"\(\s*(\d+)\s*,\s*'((?:[^']|'')*)'\s*,\s*'((?:[^']|'')*)'", RegexOptions.Compiled);
        foreach (Match m in rx.Matches(text))
        {
            int id = int.Parse(m.Groups[1].Value);
            d[id] = (m.Groups[2].Value.Replace("''", "'"), m.Groups[3].Value.Replace("''", "'"));
        }
        return d;
    }
}
