using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace AONavExtractor
{
    /// <summary>One type-1000013 record's triangles, world coordinates, as the client's reader returned them.</summary>
    public sealed class SurfaceRecord
    {
        public int Instance, TeleportDestPf, LocalizerType, LocalizerInstance;
        public float[] Verts;          // 9 floats per triangle
        public int Triangles => Verts.Length / 9;
    }

    /// <summary>
    /// Reads RDB type 1000013 (per-room / per-cell collision surfaces, a bit-packed KD surface
    /// we have not reversed) by calling the client's own reader: N3.dll's
    /// n3SurfaceResource_t::ReadBlob over a BinaryLStream on the framed record, then
    /// GetAllTriangles. The DLLs are 32-bit, hence this tool is x86. Every call is into code we
    /// do not own; a record that makes it fault takes the process down, so the driver runs this
    /// in a child process and retries with that record skipped (see Program.CollisionPhase).
    /// </summary>
    public sealed unsafe class ClientSurfaces : IDisposable
    {
        const int SURFACE_TYPE = 1000013;
        const int SLACK = 8192;

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)] static extern bool SetDllDirectoryW(string path);
        [DllImport("kernel32", CharSet = CharSet.Ansi, SetLastError = true)] static extern IntPtr LoadLibraryExA(string path, IntPtr file, uint flags);
        [DllImport("kernel32", CharSet = CharSet.Ansi)] static extern IntPtr GetProcAddress(IntPtr mod, string name);
        const uint LOAD_WITH_ALTERED_SEARCH_PATH = 8;

        delegate* unmanaged[Thiscall]<IntPtr, IntPtr, IntPtr> _surfCtor;       // n3SurfaceResource_t(const Identity_t&)
        delegate* unmanaged[Thiscall]<IntPtr, IntPtr, byte> _surfRead;         // bool ReadBlob(BinaryStream&)
        delegate* unmanaged[Thiscall]<IntPtr, IntPtr, void> _getAllTris;       // GetAllTriangles(vector<Vector3_t>&)
        delegate* unmanaged[Thiscall]<IntPtr, void> _surfDtor;
        delegate* unmanaged[Thiscall]<IntPtr, int> _teleDest, _teleLocType, _teleLocInst;
        delegate* unmanaged[Thiscall]<IntPtr, IntPtr, uint, IntPtr> _lsCtor;   // BinaryLStream(void*, uint)
        delegate* unmanaged[Thiscall]<IntPtr, void> _lsDtor;
        delegate* unmanaged[Cdecl]<IntPtr, void> _crtFree;

        readonly IntPtr _objBuf, _bsBuf, _vecBuf;
        public readonly List<string> Log = new List<string>();

        public ClientSurfaces(string clientDir)
        {
            if (IntPtr.Size != 4) throw new PlatformNotSupportedException("the client's DLLs are 32-bit; run the x86 build");
            SetDllDirectoryW(clientDir);
            IntPtr Load(string name)
            {
                IntPtr m = LoadLibraryExA(Path.Combine(clientDir, name), IntPtr.Zero, LOAD_WITH_ALTERED_SEARCH_PATH);
                if (m == IntPtr.Zero) Log.Add("failed to load " + name + " (err " + Marshal.GetLastWin32Error() + ")");
                return m;
            }
            Load("msvcr100.dll"); Load("msvcp100.dll");
            IntPtr bs = Load("BinaryStream.dll");
            Load("InstanceManager.dll"); Load("DatabaseController.dll"); Load("ResourceManager.dll");
            IntPtr gd = Load("GameData.dll");
            Load("Utils.dll"); Load("Collision.dll"); Load("PathFinder.dll");
            IntPtr n3 = Load("N3.dll");
            if (bs == IntPtr.Zero || gd == IntPtr.Zero || n3 == IntPtr.Zero)
                throw new DllNotFoundException("need BinaryStream.dll, GameData.dll and N3.dll from " + clientDir + "\n" + string.Join("\n", Log));
            IntPtr Sym(IntPtr m, string name)
            {
                IntPtr p = GetProcAddress(m, name);
                if (p == IntPtr.Zero) throw new EntryPointNotFoundException(name);
                return p;
            }
            _lsCtor = (delegate* unmanaged[Thiscall]<IntPtr, IntPtr, uint, IntPtr>)Sym(bs, "??0BinaryLStream@@QAE@PAXI@Z");
            _lsDtor = (delegate* unmanaged[Thiscall]<IntPtr, void>)Sym(bs, "??1BinaryLStream@@QAE@XZ");
            _surfCtor = (delegate* unmanaged[Thiscall]<IntPtr, IntPtr, IntPtr>)Sym(n3, "??0n3SurfaceResource_t@@QAE@ABVIdentity_t@@@Z");
            _surfRead = (delegate* unmanaged[Thiscall]<IntPtr, IntPtr, byte>)Sym(n3, "?ReadBlob@n3SurfaceResource_t@@UAE_NAAVBinaryStream@@@Z");
            _getAllTris = (delegate* unmanaged[Thiscall]<IntPtr, IntPtr, void>)Sym(n3, "?GetAllTriangles@n3SurfaceResource_t@@QAEXAAV?$vector@VVector3_t@@V?$allocator@VVector3_t@@@std@@@std@@@Z");
            _surfDtor = (delegate* unmanaged[Thiscall]<IntPtr, void>)Sym(n3, "??1n3SurfaceResource_t@@UAE@XZ");
            _teleDest = (delegate* unmanaged[Thiscall]<IntPtr, int>)Sym(n3, "?GetTeleportDestinationPlayfield@n3SurfaceResource_t@@QBEHXZ");
            _teleLocType = (delegate* unmanaged[Thiscall]<IntPtr, int>)Sym(n3, "?GetTeleportLocalizerType@n3SurfaceResource_t@@QBEHXZ");
            _teleLocInst = (delegate* unmanaged[Thiscall]<IntPtr, int>)Sym(n3, "?GetTeleportLocalizerInstance@n3SurfaceResource_t@@QBEHXZ");
            IntPtr en = GetProcAddress(n3, "?EnableInvisCheck@n3SurfaceResource_t@@SAXXZ");
            if (en != IntPtr.Zero) ((delegate* unmanaged[Cdecl]<void>)en)();
            IntPtr crt = LoadLibraryExA(Path.Combine(clientDir, "msvcr100.dll"), IntPtr.Zero, LOAD_WITH_ALTERED_SEARCH_PATH);
            IntPtr fr = crt != IntPtr.Zero ? GetProcAddress(crt, "free") : IntPtr.Zero;
            if (fr == IntPtr.Zero) throw new EntryPointNotFoundException("msvcr100 free");
            _crtFree = (delegate* unmanaged[Cdecl]<IntPtr, void>)fr;
            _objBuf = Marshal.AllocHGlobal(SLACK); _bsBuf = Marshal.AllocHGlobal(SLACK); _vecBuf = Marshal.AllocHGlobal(64);
        }

        static void Zero(IntPtr p, int n) { for (int i = 0; i < n; i += 8) Marshal.WriteInt64(p, i, 0); }

        /// <summary>Instances of type 1000013 grouped by playfield: instance = (playfield &lt;&lt; 16) | index.</summary>
        public static SortedDictionary<int, List<int>> ByPlayfield(Rdb rdb)
        {
            var d = new SortedDictionary<int, List<int>>();
            foreach (int inst in rdb.Instances(SURFACE_TYPE))
            {
                int pf = (inst >> 16) & 0xFFFF;
                if (!d.TryGetValue(pf, out var l)) d[pf] = l = new List<int>();
                l.Add(inst);
            }
            return d;
        }

        /// <summary>Parse one record with the client's reader. Null when the reader rejects it.</summary>
        public SurfaceRecord Read(Rdb rdb, int inst)
        {
            byte[] blob = rdb.ReadFramed(SURFACE_TYPE, inst);
            if (blob.Length == 0) return null;
            Zero(_objBuf, SLACK); Zero(_bsBuf, SLACK); Zero(_vecBuf, 64);
            IntPtr data = Marshal.AllocHGlobal(blob.Length);
            IntPtr ident = Marshal.AllocHGlobal(8);
            try
            {
                Marshal.Copy(blob, 0, data, blob.Length);
                Marshal.WriteInt32(ident, 0, SURFACE_TYPE); Marshal.WriteInt32(ident, 4, inst);
                _surfCtor(_objBuf, ident);
                _lsCtor(_bsBuf, data, (uint)blob.Length);
                bool ok = (_surfRead(_objBuf, _bsBuf) & 1) != 0;
                _lsDtor(_bsBuf);
                if (!ok) { _surfDtor(_objBuf); return null; }
                _getAllTris(_objBuf, _vecBuf);
                var rec = new SurfaceRecord { Instance = inst, TeleportDestPf = _teleDest(_objBuf), LocalizerType = _teleLocType(_objBuf), LocalizerInstance = _teleLocInst(_objBuf) };
                // MSVC10 std::vector: (first, last, end) pointers, possibly after an allocator word.
                IntPtr first = IntPtr.Zero; int n = 0;
                for (int o = 0; o <= 2 && first == IntPtr.Zero; o++)
                {
                    uint f = (uint)Marshal.ReadInt32(_vecBuf, o * 4), l = (uint)Marshal.ReadInt32(_vecBuf, o * 4 + 4), e = (uint)Marshal.ReadInt32(_vecBuf, o * 4 + 8);
                    if (f == 0 || l < f || e < l) continue;
                    uint bytes = l - f;
                    if (bytes == 0 || bytes % 12 != 0 || bytes / 12 > 4000000) continue;
                    first = (IntPtr)f; n = (int)(bytes / 12);
                }
                if (first != IntPtr.Zero)
                {
                    rec.Verts = new float[n * 3];
                    Marshal.Copy(first, rec.Verts, 0, n * 3);
                    _crtFree(first);                       // the client's heap, so the client's free
                }
                else rec.Verts = new float[0];
                _surfDtor(_objBuf);
                return rec;
            }
            finally { Marshal.FreeHGlobal(data); Marshal.FreeHGlobal(ident); }
        }

        public void Dispose() { Marshal.FreeHGlobal(_objBuf); Marshal.FreeHGlobal(_bsBuf); Marshal.FreeHGlobal(_vecBuf); }
    }

    /// <summary>The AOTRI1 interchange file (also what tools/navbridge writes) and the AOCL nav file.</summary>
    public static class TriFile
    {
        public static void Write(string path, int pf, List<SurfaceRecord> recs)
        {
            using (var f = new BinaryWriter(File.Create(path)))
            {
                f.Write(System.Text.Encoding.ASCII.GetBytes("AOTRI1\0\0"));
                f.Write(pf); f.Write(recs.Count);
                foreach (var r in recs)
                {
                    f.Write(r.Instance); f.Write(r.Verts.Length / 3); f.Write(r.TeleportDestPf); f.Write(r.LocalizerType); f.Write(r.LocalizerInstance);
                    foreach (float v in r.Verts) f.Write(v);
                }
            }
        }

        public static List<SurfaceRecord> Read(string path, out int pf)
        {
            var recs = new List<SurfaceRecord>();
            using (var f = new BinaryReader(File.OpenRead(path)))
            {
                byte[] magic = f.ReadBytes(8);
                if (System.Text.Encoding.ASCII.GetString(magic, 0, 6) != "AOTRI1") throw new InvalidDataException(path);
                pf = f.ReadInt32(); int n = f.ReadInt32();
                for (int i = 0; i < n; i++)
                {
                    var r = new SurfaceRecord { Instance = f.ReadInt32() };
                    int vc = f.ReadInt32();
                    r.TeleportDestPf = f.ReadInt32(); r.LocalizerType = f.ReadInt32(); r.LocalizerInstance = f.ReadInt32();
                    r.Verts = new float[vc * 3];
                    byte[] raw = f.ReadBytes(vc * 12);
                    Buffer.BlockCopy(raw, 0, r.Verts, 0, raw.Length);
                    recs.Add(r);
                }
            }
            return recs;
        }

        /// <summary>Largest per-axis span of the triangles [s, s+cnt).</summary>
        static double Extent(List<float> v, int s, int cnt)
        {
            var mn = new[] { float.MaxValue, float.MaxValue, float.MaxValue }; var mx = new[] { float.MinValue, float.MinValue, float.MinValue };
            for (int i = s * 9; i < (s + cnt) * 9; i++) { int a = i % 3; if (v[i] < mn[a]) mn[a] = v[i]; if (v[i] > mx[a]) mx[a] = v[i]; }
            return Math.Max(mx[0] - mn[0], Math.Max(mx[1] - mn[1], mx[2] - mn[2]));
        }

        public const float WalkNy = 0.5f;   // keep triangles within 60 degrees of horizontal
        public const float WallMinHeight = 1.0f;   // a wall triangle spans at least this much height

        /// <summary>AOCL: near-horizontal triangles, int16 centimetres relative to a per-chunk origin, zlib. Returns (kept triangles, chunks).</summary>
        public static (long kept, int chunks) WriteCollision(string path, List<SurfaceRecord> recs) => WriteAocl(path, recs, false);

        /// <summary>
        /// walls.bin, the same AOCL layout: the steep triangles collision.bin leaves out (|normal.y| &lt;= 0.5)
        /// that span at least WallMinHeight of height, so walls, doorframes and pillars stay and stair risers
        /// and kerbs do not. A doorway is the gap between them (subway pool pf 351: 1.6 m wide, lintel at 3 m).
        /// </summary>
        public static (long kept, int chunks) WriteWalls(string path, List<SurfaceRecord> recs) => WriteAocl(path, recs, true);

        static (long kept, int chunks) WriteAocl(string path, List<SurfaceRecord> recs, bool walls)
        {
            var body = new MemoryStream();
            var bw = new BinaryWriter(body);
            long kept = 0; int chunks = 0;
            var keep = new List<float>();
            foreach (var r in recs)
            {
                keep.Clear();
                float[] v = r.Verts;
                for (int t = 0; t + 9 <= v.Length; t += 9)
                {
                    float ax = v[t + 3] - v[t], ay = v[t + 4] - v[t + 1], az = v[t + 5] - v[t + 2];
                    float bx = v[t + 6] - v[t], by = v[t + 7] - v[t + 1], bz = v[t + 8] - v[t + 2];
                    float nx = ay * bz - az * by, ny = az * bx - ax * bz, nz = ax * by - ay * bx;
                    double len = Math.Sqrt((double)nx * nx + (double)ny * ny + (double)nz * nz);
                    if (len <= 1e-9) continue;
                    bool flat = Math.Abs(ny) / Math.Max(len, 1e-9) > WalkNy;
                    bool keepIt = !walls ? flat
                        : !flat && Math.Max(v[t + 1], Math.Max(v[t + 4], v[t + 7])) - Math.Min(v[t + 1], Math.Min(v[t + 4], v[t + 7])) >= WallMinHeight;
                    if (keepIt) for (int k = 0; k < 9; k++) keep.Add(v[t + k]);
                }
                int ntri = keep.Count / 9;
                kept += ntri;
                for (int s = 0; s < ntri; s += 2048)
                {
                    int cnt = Math.Min(2048, ntri - s);
                    // a chunk's offsets must fit int16 centimetres; a record is one cell, so this
                    // never triggers, but split finer if it ever does (same rule as exportnav.py)
                    int sub = Extent(keep, s, cnt) * 100.0 > 32767 ? 64 : cnt;
                    for (int s2 = s; s2 < s + cnt; s2 += sub)
                    {
                        int c2 = Math.Min(sub, s + cnt - s2);
                        float ox = float.MaxValue, oy = float.MaxValue, oz = float.MaxValue;
                        for (int i = 0; i < c2 * 9; i += 3)
                        {
                            ox = Math.Min(ox, keep[s2 * 9 + i]); oy = Math.Min(oy, keep[s2 * 9 + i + 1]); oz = Math.Min(oz, keep[s2 * 9 + i + 2]);
                        }
                        bw.Write(r.Instance); bw.Write(r.TeleportDestPf); bw.Write(r.LocalizerType); bw.Write(r.LocalizerInstance);
                        bw.Write(ox); bw.Write(oy); bw.Write(oz); bw.Write(c2);
                        for (int i = 0; i < c2 * 9; i += 3)
                        {
                            bw.Write((short)Math.Round((keep[s2 * 9 + i] - ox) * 100f));
                            bw.Write((short)Math.Round((keep[s2 * 9 + i + 1] - oy) * 100f));
                            bw.Write((short)Math.Round((keep[s2 * 9 + i + 2] - oz) * 100f));
                        }
                        chunks++;
                    }
                }
            }
            byte[] raw = body.ToArray();
            byte[] z = Util.ZlibCompress(raw);
            using (var f = new BinaryWriter(File.Create(path)))
            {
                f.Write(new byte[] { (byte)'A', (byte)'O', (byte)'C', (byte)'L' });
                f.Write(1); f.Write(chunks); f.Write(raw.Length); f.Write(z.Length); f.Write(z);
            }
            return (kept, chunks);
        }
    }
}
