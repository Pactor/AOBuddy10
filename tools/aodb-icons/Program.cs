using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AssetDecoder.Icons;
using AssetDecoder.Rdb;

// aodb-icons <client dir> <profiles dir> <out dir>
// Every icon id named in profiles/rollable-*.json, read from the client's ResourceDatabase (record type 1010008,
// OmniCell's IconCatalog) and written as <out>/<id>.png with its keyed background made transparent.
class Program
{
    const int IconRecordType = 1010008;

    static int Main(string[] args)
    {
        if (args.Length < 3) { Console.WriteLine("aodb-icons <client dir> <profiles dir> <out dir>"); return 1; }
        var ids = new HashSet<int>();
        var rx = new Regex(@"""(?:icon|nanoIcon)"":(\d+)");
        foreach (var f in Directory.GetFiles(args[1], "rollable-*.json"))
            foreach (Match m in rx.Matches(File.ReadAllText(f))) ids.Add(int.Parse(m.Groups[1].Value));
        Directory.CreateDirectory(args[2]);
        int written = 0, missing = 0, kept = 0;
        using (var db = new ResourceDatabase(Path.Combine(args[0], "cd_image", "data", "db")))
            foreach (int id in ids.OrderBy(x => x))
            {
                string path = Path.Combine(args[2], id + ".png");
                if (File.Exists(path)) { kept++; continue; }
                if (id == 0 || !db.Contains(IconRecordType, id)) { missing++; continue; }
                File.WriteAllBytes(path, IconImage.ToTransparentPng(db.Read(IconRecordType, id)));
                written++;
            }
        Console.WriteLine($"{ids.Count} icon ids: {written} written, {kept} already there, {missing} not in the client");
        return 0;
    }
}
