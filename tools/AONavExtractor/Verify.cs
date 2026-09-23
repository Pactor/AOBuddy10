using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace AONavExtractor
{
    /// <summary>
    /// Casts every point the owner walked in a zone (AOBuddy's nav/&lt;pf&gt;.json: {"Segments": [[[x,y,z],...],...]})
    /// against the floor model, then against the unfiltered collision triangles, and counts what
    /// matched within a tolerance. This is the acceptance test the formats were derived with.
    /// </summary>
    public sealed class Verification
    {
        public int WalkedPoints, ExplainedByFloor, ExplainedByCollision, Unexplained;
        public double ToleranceM;
        public List<double[]> UnexplainedSamples = new List<double[]>();
        public double ExplainedPct => WalkedPoints == 0 ? 0 : Math.Round(100.0 * (WalkedPoints - Unexplained) / WalkedPoints, 1);

        public static List<double[]> LoadWalked(string navFile)
        {
            var pts = new List<double[]>();
            using (var doc = JsonDocument.Parse(File.ReadAllBytes(navFile)))
            {
                if (!doc.RootElement.TryGetProperty("Segments", out var segs)) return pts;
                foreach (var seg in segs.EnumerateArray())
                    foreach (var p in seg.EnumerateArray())
                    {
                        var a = new double[3]; int i = 0;
                        foreach (var v in p.EnumerateArray()) { if (i < 3) a[i] = v.GetDouble(); i++; }
                        pts.Add(a);
                    }
            }
            return pts;
        }

        public static Verification Run(List<double[]> pts, Ground ground, Dungeon dungeon, List<SurfaceRecord> recs, double tol = 1.0)
        {
            var v = new Verification { WalkedPoints = pts.Count, ToleranceM = tol };
            TriBuckets buckets = recs != null ? new TriBuckets(recs) : null;
            foreach (var p in pts)
            {
                double x = p[0], y = p[1], z = p[2];
                bool ok = false;
                if (ground != null)
                {
                    double h = ground.HeightAt(x, z);
                    ok = !double.IsNaN(h) && Math.Abs(h - y) <= tol;
                }
                else if (dungeon != null)
                {
                    foreach (var rm in dungeon.Rooms)
                    {
                        double h = dungeon.FloorHeight(rm, x, z);
                        if (!double.IsNaN(h) && Math.Abs(h - y) <= tol) { ok = true; break; }
                    }
                }
                if (ok) { v.ExplainedByFloor++; continue; }
                if (buckets != null)
                {
                    foreach (double h in buckets.HeightsUnder(x, z))
                        if (Math.Abs(h - y) <= tol) { ok = true; break; }
                }
                if (ok) { v.ExplainedByCollision++; continue; }
                v.Unexplained++;
                if (v.UnexplainedSamples.Count < 10) v.UnexplainedSamples.Add(new[] { Math.Round(x, 1), Math.Round(y, 2), Math.Round(z, 1) });
            }
            return v;
        }

        public void WriteJson(JsonOut w)
        {
            w.Obj();
            w.Key("walkedPoints").Num(WalkedPoints); w.Key("toleranceM").Num(ToleranceM);
            w.Key("explainedByFloor").Num(ExplainedByFloor); w.Key("explainedByCollision").Num(ExplainedByCollision);
            w.Key("unexplained").Num(Unexplained); w.Key("explainedPct").Num(ExplainedPct);
            w.Key("unexplainedSamples").Arr(); foreach (var s in UnexplainedSamples) { w.Arr(); foreach (var c in s) w.Num(c); w.End(); } w.End();
            w.End();
        }
    }

    /// <summary>Triangles bucketed on an 8 m XZ grid for point-under queries.</summary>
    public sealed class TriBuckets
    {
        readonly Dictionary<long, List<float[]>> _b = new Dictionary<long, List<float[]>>();
        static long Key(int a, int c) => ((long)a << 32) ^ (uint)c;

        public TriBuckets(List<SurfaceRecord> recs)
        {
            foreach (var r in recs)
            {
                float[] v = r.Verts;
                for (int t = 0; t + 9 <= v.Length; t += 9)
                {
                    var tri = new float[9]; Array.Copy(v, t, tri, 0, 9);
                    int x0 = (int)Math.Floor(Math.Min(tri[0], Math.Min(tri[3], tri[6])) / 8), x1 = (int)Math.Floor(Math.Max(tri[0], Math.Max(tri[3], tri[6])) / 8);
                    int z0 = (int)Math.Floor(Math.Min(tri[2], Math.Min(tri[5], tri[8])) / 8), z1 = (int)Math.Floor(Math.Max(tri[2], Math.Max(tri[5], tri[8])) / 8);
                    for (int a = x0; a <= x1; a++)
                        for (int c = z0; c <= z1; c++)
                        {
                            if (!_b.TryGetValue(Key(a, c), out var l)) _b[Key(a, c)] = l = new List<float[]>();
                            l.Add(tri);
                        }
                }
            }
        }

        public IEnumerable<double> HeightsUnder(double x, double z)
        {
            if (!_b.TryGetValue(Key((int)Math.Floor(x / 8), (int)Math.Floor(z / 8)), out var l)) yield break;
            foreach (var t in l)
            {
                double x0 = t[0], y0 = t[1], z0 = t[2], x1 = t[3], y1 = t[4], z1 = t[5], x2 = t[6], y2 = t[7], z2 = t[8];
                double d = (x1 - x0) * (z2 - z0) - (x2 - x0) * (z1 - z0);
                if (Math.Abs(d) < 1e-9) continue;
                double u = ((x - x0) * (z2 - z0) - (x2 - x0) * (z - z0)) / d;
                double w = ((x1 - x0) * (z - z0) - (x - x0) * (z1 - z0)) / d;
                if (u >= -1e-6 && w >= -1e-6 && u + w <= 1 + 1e-6) yield return y0 + u * (y1 - y0) + w * (y2 - y0);
            }
        }
    }
}
