// navbridge - find and read the AO client's prebaked path graph.
//
// The client loads a visibility/path graph at zone-in via
// GraphPathFinder_t::CreateFromData(const GameData::VisibilityGraphData_t&).
// The data side of that chain is exported from the client's own GameData.dll:
//
//   PathGraphData_t::PathGraphData_t(const Identity_t&)   public ctor
//   PathGraphData_t::ReadBlob(BinaryStream&) -> bool      exported, callable
//   PathGraphData_t::GetData() -> const VisibilityGraphData_t*
//   VisibilityGraphData_t::GetNodes() -> const vector<VisibilityNodeData_t*>&
//
// We do not know WHICH RDB record feeds it, so this hands candidate records to the
// client's own reader and keeps the ones that parse. ReadBlob returns bool, so the
// client validates its own format for us.
//
// A probe that finds nothing proves nothing unless the harness is known good, so we
// probe SIX DbObject subclasses, not one. PlayfieldDistrictInfo_t is the positive
// control: RDB type 1000029 is plainly the district record (it contains "Borealis",
// "Omni-Tek", "Abandoned Mall"), so that pairing must light up. If it does and
// PathGraphData_t stays dark, the negative is real.
//
// 32-bit, because the client is. Nothing is written to the client install and the
// game does not need to be running.

#include "rdb.h"
#include <stdio.h>
#include <stdlib.h>
#include <string>
#include <vector>
#include <set>
#include <algorithm>

struct Identity_t { int type; int instance; };   // AO's (Type, Instance) pair

typedef void* (__thiscall *BsCtorBuf)(void* self, void* buf, unsigned int len);
typedef void  (__thiscall *BsDtor)(void* self);
typedef void* (__thiscall *BsGetInt)(void* self, int* v);
typedef void* (__thiscall *ObjCtor)(void* self, const Identity_t* id);
typedef bool  (__thiscall *ReadBlobFn)(void* self, void* stream);
typedef const void* (__thiscall *GetDataFn)(void* self);
typedef const void* (__thiscall *GetNodesFn)(const void* self);

// BinaryStream is BIG-endian (the self-test proves it: a statel count of 72 comes back
// as 0x48000000). RDB records are little-endian. BinaryStream.dll also exports
// BinaryLStream - the little-endian variant - so we try both and let the readers say
// which one they want.
static BsCtorBuf  g_bsCtor;
static BsDtor     g_bsDtor;
static BsCtorBuf  g_lsCtor;
static BsDtor     g_lsDtor;
static bool       g_useLittle = false;
static BsGetInt   g_bsGetInt;
static GetDataFn  g_getData;
static GetNodesFn g_getNodes;

// Objects whose size we do not know: allocate far more than any of them can need
// and construct in place. Over-allocating is safe; guessing a size is not.
static const size_t SLACK = 8192;
static const int SURFACE_TYPE = 1000013;   // per-room / per-cell collision surface

// mod: 'G' = GameData.dll (the database/network side), 'N' = N3.dll (the RDB side).
// The GameData readers were probed first and rejected every raw RDB payload, including
// the one that is obviously theirs - see the write-up. N3 owns the RDB readers.
struct Candidate
{
    const char* name;
    char        mod;
    const char* ctorSym;
    const char* readSym;
    ObjCtor     ctor;
    ReadBlobFn  read;
    bool        isPathGraph;
};

static Candidate g_cands[] = {
    // --- N3.dll: the RDB-side readers. RDBTileMaterial_t vs type 1000024 is the
    // positive control here: we know 1000024 holds the tile material names.
    { "n3SurfaceResource_t", 'N',
      "??0n3SurfaceResource_t@@QAE@ABVIdentity_t@@@Z",
      "?ReadBlob@n3SurfaceResource_t@@UAE_NAAVBinaryStream@@@Z", 0, 0, false },
    { "RDBTileMaterial_t", 'N',
      "??0RDBTileMaterial_t@@QAE@ABVIdentity_t@@@Z",
      "?ReadBlob@RDBTileMaterial_t@@UAE_NAAVBinaryStream@@@Z", 0, 0, false },
    { "RDBPlayfield_t", 'N',
      "??0RDBPlayfield_t@@IAE@ABVIdentity_t@@@Z",
      "?ReadBlob@RDBPlayfield_t@@MAE_NAAVBinaryStream@@@Z", 0, 0, false },
    // --- GameData.dll: kept so the contrast stays visible in every run.
    { "PathGraphData_t", 'G',
      "??0PathGraphData_t@GameData@@QAE@ABVIdentity_t@@@Z",
      "?ReadBlob@PathGraphData_t@GameData@@MAE_NAAVBinaryStream@@@Z", 0, 0, true },
    { "PlayfieldDistrictInfo_t", 'G',
      "??0PlayfieldDistrictInfo_t@GameData@@QAE@ABVIdentity_t@@@Z",
      "?ReadBlob@PlayfieldDistrictInfo_t@GameData@@MAE_NAAVBinaryStream@@@Z", 0, 0, false },
};
static const int NCAND = sizeof(g_cands) / sizeof(g_cands[0]);

static void* Sym(HMODULE m, const char* name, const char* what)
{
    void* p = (void*)GetProcAddress(m, name);
    if (!p) printf("  MISSING %s (%s)\n", what, name);
    return p;
}

static HMODULE LoadClientDll(const std::string& dir, const char* name)
{
    std::string p = dir + "\\" + name;
    HMODULE m = LoadLibraryExA(p.c_str(), 0, LOAD_WITH_ALTERED_SEARCH_PATH);
    if (!m) printf("  failed to load %s (err %lu)\n", name, GetLastError());
    return m;
}

// A std::vector<T*> from MSVC10 is three pointers (first, last, end), possibly
// preceded by an empty allocator member. Rather than assume the offset, find the
// triple that is self-consistent. Returns element count, or -1.
static int VectorCount(const void* vec)
{
    if (!vec) return -1;
    const unsigned int* w = (const unsigned int*)vec;
    for (int o = 0; o <= 2; ++o)
    {
        unsigned int first = w[o], last = w[o+1], end = w[o+2];
        if (first == 0 && last == 0) return 0;
        if (first == 0 || last < first || end < last) continue;
        unsigned int bytes = last - first;
        if (bytes % 4) continue;
        unsigned int n = bytes / 4;
        if (n > 500000) continue;
        if (IsBadReadPtr((void*)first, bytes ? bytes : 4)) continue;
        return (int)n;
    }
    return -1;
}

// Every call into the client's readers sits behind SEH - they will access-violate on
// data that is not theirs, which is the whole point of a brute-force probe. Kept free
// of C++ objects because MSVC will not mix __try with object unwinding.
static void SelfTestRaw(void* bs, void* blob, unsigned int len, int* got, int* threw)
{
    *got = -1; *threw = 0;
    __try
    {
        g_bsCtor(bs, blob, len);
        int v = -1;
        g_bsGetInt(bs, &v);
        *got = v;
        g_bsDtor(bs);
    }
    __except (EXCEPTION_EXECUTE_HANDLER) { *threw = 1; }
}

typedef void (__thiscall *GetAllTrisFn)(void* self, void* vecOut);
typedef void (__thiscall *SurfDtorFn)(void* self);
typedef int  (__thiscall *SurfGetIntFn)(const void* self);
typedef void (__cdecl    *CrtFreeFn)(void* p);
static GetAllTrisFn g_getAllTris;
static SurfDtorFn   g_surfDtor;
static SurfGetIntFn g_teleDestPf;
static SurfGetIntFn g_teleLocType;
static SurfGetIntFn g_teleLocInst;
static CrtFreeFn    g_crtFree;   // the client's CRT free - the vector buffer is its heap, not ours

// We cannot pass our own std::vector to code built against MSVC10's STL, so hand it a
// zeroed buffer - an all-zero vector IS a valid empty one - let it push_back through its
// own allocator, then read the (first,last,end) triple back out. The memory it allocated
// is leaked deliberately; this is a probe.
struct Tris { int count; const float* first; float minx, miny, minz, maxx, maxy, maxz; int threw; };

static void GetTrisRaw(void* obj, void* vecBuf, Tris* out)
{
    out->count = -1; out->first = 0; out->threw = 0;
    __try
    {
        g_getAllTris(obj, vecBuf);
        const unsigned int* w = (const unsigned int*)vecBuf;
        for (int o = 0; o <= 2; ++o)
        {
            unsigned int first = w[o], last = w[o+1], end = w[o+2];
            if (first == 0 || last < first || end < last) continue;
            unsigned int bytes = last - first;
            if (bytes % 12 || bytes == 0) continue;
            unsigned int n = bytes / 12;
            if (n > 4000000) continue;
            if (IsBadReadPtr((void*)first, bytes)) continue;
            const float* f = (const float*)first;
            out->minx = out->maxx = f[0];
            out->miny = out->maxy = f[1];
            out->minz = out->maxz = f[2];
            for (unsigned int k = 0; k < n; ++k)
            {
                float x = f[k*3], y = f[k*3+1], z = f[k*3+2];
                if (x < out->minx) out->minx = x;  if (x > out->maxx) out->maxx = x;
                if (y < out->miny) out->miny = y;  if (y > out->maxy) out->maxy = y;
                if (z < out->minz) out->minz = z;  if (z > out->maxz) out->maxz = z;
            }
            out->count = (int)n;
            out->first = f;
            return;
        }
    }
    __except (EXCEPTION_EXECUTE_HANDLER) { out->count = -1; out->first = 0; out->threw = 1; }
}

// The vector buffer came from the client's heap, and the surface object holds a KD tree
// it allocated there too. Over a quarter of a million records, leaking either exhausts a
// 32-bit address space, so give both back.
static void FreeTriBuffer(Tris* tr)
{
    if (!tr->first || !g_crtFree) return;
    __try { g_crtFree((void*)tr->first); } __except (EXCEPTION_EXECUTE_HANDLER) {}
    tr->first = 0;
}

static void DestructSurface(void* obj)
{
    if (!g_surfDtor) return;
    __try { g_surfDtor(obj); } __except (EXCEPTION_EXECUTE_HANDLER) {}
}

struct Vec3 { float x, y, z; };
typedef bool (__thiscall *LineHitFn)(const void* self, const Vec3* from, const Vec3* to,
                                     Vec3* hit, bool flag, void* locality);
static LineHitFn g_lineHit;

// Simpler probe with no LocalitySource pointer to get wrong:
//   bool GetSphereIntersection(const Vector3_t& centre, float radius, Vector3_t& contact)
typedef bool (__thiscall *SphereHitFn)(const void* self, const Vec3* centre, float radius, Vec3* hit);
static SphereHitFn g_sphereHit;

static void CastSphere(void* obj, const Vec3* c, float r, Vec3* hit, int* ok)
{
    *ok = 0;
    hit->x = hit->y = hit->z = 0.0f;
    __try { *ok = g_sphereHit(obj, c, r, hit) ? 1 : 0; }
    __except (EXCEPTION_EXECUTE_HANDLER) { *ok = 0; }
}

// The authoritative "is there ground here" query, straight from the client's collision
// code. Behind SEH like every other call into it.
static void CastRay(void* obj, const Vec3* from, const Vec3* to, Vec3* hit, int* ok)
{
    *ok = 0;
    hit->x = hit->y = hit->z = 0.0f;
    __try { *ok = g_lineHit(obj, from, to, hit, false, 0) ? 1 : 0; }
    __except (EXCEPTION_EXECUTE_HANDLER) { *ok = 0; }
}

// ---- option 2: let N3 assemble the whole playfield -------------------------------
// n3Playfield_t::CreatePlayfieldFromResource(RDBPlayfield_t*) builds the real thing from a
// parsed type-1000001 record, which we can already produce. Then CalculateGroundPoint is
// the client's own "where is the floor here" answer, and GetSurface/GetPathfinder hand
// back the assembled surface and the A* pathfinder for the zone.
typedef void* (__thiscall *PfCtorFn)(void* self, const void* proxy);
typedef void  (__thiscall *PfSetResFn)(void* self, const void* rdbPf);
typedef bool  (__thiscall *PfCreateFn)(void* self, void* rdbPf);
typedef bool  (__thiscall *PfGroundFn)(const void* self, Vec3* inout);
typedef void* (__thiscall *PfGetPtrFn)(const void* self);

static PfCtorFn   g_pfCtor;
static PfSetResFn g_pfSetRes;
static PfCreateFn g_pfCreate;
static PfGroundFn g_pfGround;
static PfGetPtrFn g_pfSurface;
static PfGetPtrFn g_pfPathfinder;

// Each step separately guarded and separately reported: if the engine needs singletons we
// have not initialised, the failure should name the call that wanted them.
static int PfBuild(void* pfObj, const void* proxy, void* rdbPf, int* stage)
{
    *stage = 0;
    __try { g_pfCtor(pfObj, proxy); }          __except (EXCEPTION_EXECUTE_HANDLER) { return 0; }
    *stage = 1;
    if (g_pfSetRes)
    {
        __try { g_pfSetRes(pfObj, rdbPf); }    __except (EXCEPTION_EXECUTE_HANDLER) { return 0; }
    }
    *stage = 2;
    bool ok = false;
    __try { ok = g_pfCreate(pfObj, rdbPf); }   __except (EXCEPTION_EXECUTE_HANDLER) { return 0; }
    *stage = 3;
    return ok ? 1 : -1;
}

static void PfGround(void* pfObj, Vec3* v, int* ok)
{
    *ok = 0;
    __try { *ok = g_pfGround(pfObj, v) ? 1 : 0; } __except (EXCEPTION_EXECUTE_HANDLER) { *ok = 0; }
}

static void* PfPtr(void* pfObj, PfGetPtrFn fn)
{
    if (!fn) return 0;
    __try { return fn(pfObj); } __except (EXCEPTION_EXECUTE_HANDLER) { return 0; }
}

// ---- engine init: give the client its resource manager back ----------------------
// CreatePlayfieldFromResource builds the rooms and then goes looking for each room's
// resources. With no ResourceManager behind it, that is where it dies. The pieces are all
// exported: ResourceDatabase_t::Open(const std::string&, bool), ResourceManager::Get()
// (a static singleton accessor), SetDatabase and DisableAsyncLoading.
typedef void* (__thiscall *DbCtorFn)(void* self);
typedef int   (__thiscall *DbOpenFn)(void* self, const void* stdString, bool flag);
typedef void* (__cdecl    *RmGetFn)();
typedef void  (__thiscall *RmSetDbFn)(void* self, void* db);
typedef void  (__thiscall *RmVoidFn)(void* self);
typedef void* (__cdecl    *CrtMallocFn)(size_t n);

static DbCtorFn    g_dbCtor;
static DbOpenFn    g_dbOpen;
static RmGetFn     g_rmGet;
static RmSetDbFn   g_rmSetDb;
static RmVoidFn    g_rmNoAsync;
static CrtMallocFn g_crtMalloc;

// MSVC10's std::string is not exported (it is a header template), so lay one out by hand:
// an allocator-sized pad, then a 16-byte union that is either the inline buffer or a
// pointer, then _Mysize and _Myres. The pad size is the only real unknown, so build it at
// a caller-chosen offset and let Open() say which one was right.
static void MakeStdString(void* buf, const char* text, int bxOffset)
{
    memset(buf, 0, 64);
    size_t n = strlen(text);
    unsigned char* p = (unsigned char*)buf;
    if (n < 16)
    {
        memcpy(p + bxOffset, text, n + 1);
        *(unsigned int*)(p + bxOffset + 16) = (unsigned int)n;
        *(unsigned int*)(p + bxOffset + 20) = 15;
    }
    else
    {
        char* heap = (char*)(g_crtMalloc ? g_crtMalloc(n + 1) : 0);
        if (!heap) return;
        memcpy(heap, text, n + 1);
        *(char**)(p + bxOffset) = heap;
        *(unsigned int*)(p + bxOffset + 16) = (unsigned int)n;
        *(unsigned int*)(p + bxOffset + 20) = (unsigned int)n;
    }
}

static bool CallDbCtor(void* dbObj)
{
    __try { g_dbCtor(dbObj); return true; } __except (EXCEPTION_EXECUTE_HANDLER) { return false; }
}

static int OpenRdb(void* dbObj, const void* str, int* threw)
{
    *threw = 0;
    __try { return g_dbOpen(dbObj, str, false); }
    __except (EXCEPTION_EXECUTE_HANDLER) { *threw = 1; return -1; }
}

static void* InitResourceManager(void* dbObj, int* threw)
{
    *threw = 0;
    __try
    {
        void* rm = g_rmGet();
        if (rm && g_rmSetDb) g_rmSetDb(rm, dbObj);
        if (rm && g_rmNoAsync) g_rmNoAsync(rm);
        return rm;
    }
    __except (EXCEPTION_EXECUTE_HANDLER) { *threw = 1; return 0; }
}

typedef void (__cdecl *VoidFn)();
static bool CallVoid(VoidFn fn)
{
    if (!fn) return false;
    __try { fn(); return true; } __except (EXCEPTION_EXECUTE_HANDLER) { return false; }
}

static void ReadTeleport(void* obj, int* dest, int* ltype, int* linst)
{
    *dest = *ltype = *linst = 0;
    __try
    {
        if (g_teleDestPf)  *dest  = g_teleDestPf(obj);
        if (g_teleLocType) *ltype = g_teleLocType(obj);
        if (g_teleLocInst) *linst = g_teleLocInst(obj);
    }
    __except (EXCEPTION_EXECUTE_HANDLER) { *dest = *ltype = *linst = 0; }
}

struct Probe { int parsed; int nodes; int threw; };

static void ProbeRaw(Candidate* c, void* obj, void* bs, void* blob, unsigned int blobLen,
                     const Identity_t* id, Probe* out)
{
    out->parsed = 0; out->nodes = -1; out->threw = 0;
    __try
    {
        c->ctor(obj, id);
        if (g_useLittle) g_lsCtor(bs, blob, blobLen); else g_bsCtor(bs, blob, blobLen);
        bool ok = c->read(obj, bs);
        out->parsed = ok ? 1 : 0;
        if (ok && c->isPathGraph && g_getData && g_getNodes)
        {
            const void* data = g_getData(obj);
            if (data) out->nodes = VectorCount(g_getNodes(data));
        }
        if (g_useLittle) g_lsDtor(bs); else g_bsDtor(bs);
    }
    __except (EXCEPTION_EXECUTE_HANDLER) { out->parsed = 0; out->nodes = -1; out->threw = 1; }
}

// The full cross product runs the client's readers over data that is not theirs, and a
// reader that hits a garbage count will spin instead of returning - the first sweep
// burned 10 minutes of CPU that way. "focus" mode probes only the pairings we have a
// reason to believe in, printing each one BEFORE the call so a hang names itself.
struct Pairing { const char* cls; int type; int count; int from; const char* why; };
static Pairing g_focus[] = {
    { "RDBTileMaterial_t",       1000024, 1, 0, "positive control: 1000024 holds the tile material names" },
    { "n3SurfaceResource_t",     1000013, 6, (127<<16), "subway pf127 rooms - the target: per-room/cell collision surface" },
    { "RDBPlayfield_t",          1000001, 2, 0, "playfield record" },
    { "PlayfieldDistrictInfo_t", 1000029, 3, 0, "control that failed on bare payloads: 1000029 IS the district record" },
    { "PathGraphData_t",         1000029, 2, 0, "does the path graph ride along with the districts?" },
    { "PathGraphData_t",         1000001, 3, 0, "or inside the playfield record?" },
    { "PathGraphData_t",         1000013, 3, (127<<16), "or alongside each room's surface?" },
};
static const int NFOCUS = sizeof(g_focus) / sizeof(g_focus[0]);

int main(int argc, char** argv)
{
    std::string client = argc > 1 ? argv[1] : "E:\\Funcom\\Anarchy Online";
    std::string db = client + "\\cd_image\\data\\db";
    bool focus = (argc > 2 && strcmp(argv[2], "focus") == 0);
    bool doExport = (argc > 2 && strcmp(argv[2], "export") == 0);
    bool doRay    = (argc > 2 && strcmp(argv[2], "ray") == 0);   // ray <pf> <pointsfile>
    bool doPf     = (argc > 2 && strcmp(argv[2], "pf") == 0);    // pf <pf> [pointsfile]
    int perType = (argc > 2 && !focus && !doExport) ? atoi(argv[2]) : 25;

    // export <outdir> [pf,pf,...|all]
    std::string outDir = (doExport && argc > 3) ? argv[3] : "out";
    std::set<int> wanted;
    if (doExport && argc > 4 && strcmp(argv[4], "all") != 0)
    {
        std::string list = argv[4];
        size_t a = 0;
        while (a <= list.size())
        {
            size_t b = list.find(',', a);
            if (b == std::string::npos) b = list.size();
            if (b > a) wanted.insert(atoi(list.substr(a, b - a).c_str()));
            a = b + 1;
        }
    }

    setvbuf(stdout, 0, _IONBF, 0);   // so a hang still shows us how far we got
    printf("client: %s\n", client.c_str());
    SetDllDirectoryA(client.c_str());

    LoadClientDll(client, "msvcr100.dll");
    LoadClientDll(client, "msvcp100.dll");
    HMODULE hBs = LoadClientDll(client, "BinaryStream.dll");
    LoadClientDll(client, "InstanceManager.dll");
    HMODULE hDbc = LoadClientDll(client, "DatabaseController.dll");
    HMODULE hRm  = LoadClientDll(client, "ResourceManager.dll");
    if (hDbc)
    {
        g_dbCtor = (DbCtorFn)Sym(hDbc, "??0ResourceDatabase_t@@QAE@XZ", "ResourceDatabase_t()");
        g_dbOpen = (DbOpenFn)Sym(hDbc, "?Open@ResourceDatabase_t@@QAEHABV?$basic_string@DU?$char_traits@D@std@@V?$allocator@D@2@@std@@_N@Z", "ResourceDatabase_t::Open");
    }
    if (hRm)
    {
        g_rmGet     = (RmGetFn)  Sym(hRm, "?Get@ResourceManager@@SAAAV1@XZ", "ResourceManager::Get");
        g_rmSetDb   = (RmSetDbFn)Sym(hRm, "?SetDatabase@ResourceManager@@QAEXPAVDatabaseController_t@@@Z", "ResourceManager::SetDatabase");
        g_rmNoAsync = (RmVoidFn) Sym(hRm, "?DisableAsyncLoading@ResourceManager@@QAEXXZ", "ResourceManager::DisableAsyncLoading");
    }
    HMODULE hGd = LoadClientDll(client, "GameData.dll");
    if (!hBs || !hGd) { printf("cannot continue without BinaryStream.dll + GameData.dll\n"); return 2; }

    g_bsCtor   = (BsCtorBuf) Sym(hBs, "??0BinaryStream@@QAE@PAXI@Z", "BinaryStream(void*,uint)");
    g_bsDtor   = (BsDtor)    Sym(hBs, "??1BinaryStream@@QAE@XZ", "~BinaryStream");
    g_lsCtor   = (BsCtorBuf) Sym(hBs, "??0BinaryLStream@@QAE@PAXI@Z", "BinaryLStream(void*,uint)");
    g_lsDtor   = (BsDtor)    Sym(hBs, "??1BinaryLStream@@QAE@XZ", "~BinaryLStream");
    g_bsGetInt = (BsGetInt)  Sym(hBs, "??5BinaryStream@@QAEAAV0@AAH@Z", "BinaryStream::operator>>(int&)");
    g_getData  = (GetDataFn) Sym(hGd, "?GetData@PathGraphData_t@GameData@@QBEPBVVisibilityGraphData_t@2@XZ", "PathGraphData_t::GetData");
    g_getNodes = (GetNodesFn)Sym(hGd, "?GetNodes@VisibilityGraphData_t@GameData@@QBEABV?$vector@PAVVisibilityNodeData_t@GameData@@V?$allocator@PAVVisibilityNodeData_t@GameData@@@std@@@std@@XZ", "VisibilityGraphData_t::GetNodes");
    if (!g_bsCtor || !g_bsDtor) { printf("missing BinaryStream exports\n"); return 3; }

    // N3.dll is the RDB side. It pulls in the renderer's DLLs, but we only ever call
    // its blob readers - nothing initialises graphics.
    LoadClientDll(client, "Utils.dll");
    LoadClientDll(client, "ResourceManager.dll");
    LoadClientDll(client, "Collision.dll");
    LoadClientDll(client, "PathFinder.dll");
    HMODULE hN3 = LoadClientDll(client, "N3.dll");
    if (!hN3) printf("  (N3.dll unavailable - RDB-side readers will be skipped)\n");
    if (hN3)
    {
        g_getAllTris = (GetAllTrisFn)Sym(hN3,
            "?GetAllTriangles@n3SurfaceResource_t@@QAEXAAV?$vector@VVector3_t@@V?$allocator@VVector3_t@@@std@@@std@@@Z",
            "n3SurfaceResource_t::GetAllTriangles");
        g_surfDtor   = (SurfDtorFn)  Sym(hN3, "??1n3SurfaceResource_t@@UAE@XZ", "~n3SurfaceResource_t");
        g_teleDestPf = (SurfGetIntFn)Sym(hN3, "?GetTeleportDestinationPlayfield@n3SurfaceResource_t@@QBEHXZ", "GetTeleportDestinationPlayfield");
        g_teleLocType= (SurfGetIntFn)Sym(hN3, "?GetTeleportLocalizerType@n3SurfaceResource_t@@QBEHXZ", "GetTeleportLocalizerType");
        g_teleLocInst= (SurfGetIntFn)Sym(hN3, "?GetTeleportLocalizerInstance@n3SurfaceResource_t@@QBEHXZ", "GetTeleportLocalizerInstance");
        g_lineHit    = (LineHitFn)   Sym(hN3, "?GetLineIntersection@n3SurfaceResource_t@@UBE_NABVVector3_t@@0AAV2@_NPAVLocalitySource_t@@@Z", "GetLineIntersection");
        g_sphereHit  = (SphereHitFn) Sym(hN3, "?GetSphereIntersection@n3SurfaceResource_t@@UBE_NABVVector3_t@@MAAV2@@Z", "GetSphereIntersection");
        g_pfCtor       = (PfCtorFn)  Sym(hN3, "??0n3Playfield_t@@QAE@ABVPlayfieldProxy_t@@@Z", "n3Playfield_t(PlayfieldProxy_t)");
        g_pfSetRes     = (PfSetResFn)Sym(hN3, "?SetPlayfieldResource@n3Playfield_t@@QAEXABVRDBPlayfield_t@@@Z", "SetPlayfieldResource");
        g_pfCreate     = (PfCreateFn)Sym(hN3, "?CreatePlayfieldFromResource@n3Playfield_t@@UAE_NPAVRDBPlayfield_t@@@Z", "CreatePlayfieldFromResource");
        g_pfGround     = (PfGroundFn)Sym(hN3, "?CalculateGroundPoint@n3Playfield_t@@QBE_NAAVVector3_t@@@Z", "CalculateGroundPoint");
        g_pfSurface    = (PfGetPtrFn)Sym(hN3, "?GetSurface@n3Playfield_t@@QAEPAVSurface_i@@XZ", "n3Playfield_t::GetSurface");
        g_pfPathfinder = (PfGetPtrFn)Sym(hN3, "?GetPathfinder@n3Playfield_t@@QBEPAVPathfinder_i@@XZ", "n3Playfield_t::GetPathfinder");
        // Static toggle. The client ships invisible collision geometry
        // (invisible_collide_triangle.abiff is in the resource name table); with the check
        // off, those triangles may be skipped. Turn it on before reading anything.
        VoidFn en = (VoidFn)Sym(hN3, "?EnableInvisCheck@n3SurfaceResource_t@@SAXXZ", "EnableInvisCheck");
        if (CallVoid(en)) printf("  invisible-collision check enabled\n");
    }
    {
        HMODULE hCrt = GetModuleHandleA("msvcr100.dll");
        if (hCrt) g_crtFree = (CrtFreeFn)GetProcAddress(hCrt, "free");
        if (hCrt) g_crtMalloc = (CrtMallocFn)GetProcAddress(hCrt, "malloc");
        if (!g_crtFree) printf("  (no msvcr100 free - triangle buffers will leak)\n");
    }

    int usable = 0;
    for (int c = 0; c < NCAND; ++c)
    {
        HMODULE m = (g_cands[c].mod == 'N') ? hN3 : hGd;
        if (!m) continue;
        g_cands[c].ctor = (ObjCtor)   Sym(m, g_cands[c].ctorSym, g_cands[c].name);
        g_cands[c].read = (ReadBlobFn)Sym(m, g_cands[c].readSym, g_cands[c].name);
        if (g_cands[c].ctor && g_cands[c].read) ++usable;
    }
    printf("exports resolved (%d/%d classes usable)\n", usable, NCAND);

    Rdb rdb;
    if (!rdb.Open(db)) return 4;
    printf("rdb open: %u types\n\n", (unsigned)rdb.offsets.size());

    // Harness self-test. The statel record for a playfield begins with a count, and
    // pf800 Borealis has 72 statels (parsed independently, in Python, from the same
    // bytes). BinaryStream reads BIG-endian - AO's serialisation order throughout -
    // so we compare against the byte-swapped value.
    {
        std::vector<unsigned char> st;
        if (rdb.Read(1000026, 800, st) && st.size() > 4)
        {
            static std::vector<unsigned char> bsBuf(SLACK);
            memset(&bsBuf[0], 0, SLACK);
            int got = -1, threw = 0;
            SelfTestRaw(&bsBuf[0], &st[0], (unsigned int)st.size(), &got, &threw);
            int le = st[0] | (st[1] << 8) | (st[2] << 16) | ((int)st[3] << 24);
            int be = (st[0] << 24) | (st[1] << 16) | (st[2] << 8) | st[3];
            printf("self-test: statel pf800 first int -> stream %d (0x%08X); LE %d, BE %d; threw=%d  [%s]\n\n",
                   got, (unsigned)got, le, be,
                   threw, (!threw && (got == le || got == be)) ? "HARNESS OK" : "HARNESS SUSPECT");
        }
    }

    static std::vector<unsigned char> objBuf(SLACK), bsBuf(SLACK);

    if (doPf)
    {
        int pf = argc > 3 ? atoi(argv[3]) : 127;
        const char* ptsPath = argc > 4 ? argv[4] : 0;
        if (!g_pfCtor || !g_pfCreate) { printf("n3Playfield_t entry points unavailable\n"); return 6; }

        // 0. Stand the resource manager up first, or CreatePlayfieldFromResource dies the
        // moment it asks for a room's resources. The std::string layout is unknown, so try
        // each plausible offset for the union and keep whichever Open() accepts.
        if (g_dbCtor && g_dbOpen && g_rmGet)
        {
            static std::vector<unsigned char> dbBuf(8192), strBuf(64);
            int opened = -1, usedOffset = -1;
            for (int bx = 0; bx <= 8 && opened <= 0; bx += 4)
            {
                memset(&dbBuf[0], 0, dbBuf.size());
                int threw = 0;
                if (!CallDbCtor(&dbBuf[0])) continue;
                MakeStdString(&strBuf[0], db.c_str(), bx);
                opened = OpenRdb(&dbBuf[0], &strBuf[0], &threw);
                if (opened > 0) usedOffset = bx;
                printf("  ResourceDatabase_t::Open(bx offset %d) -> %d%s\n", bx, opened, threw ? " (threw)" : "");
            }
            if (opened > 0)
            {
                int threw = 0;
                void* rm = InitResourceManager(&dbBuf[0], &threw);
                printf("  ResourceManager::Get -> %p%s (string union at offset %d)\n",
                       rm, threw ? " (threw)" : "", usedOffset);
            }
            else printf("  could not open the resource database - the build will likely still fail\n");
        }

        // 1. The playfield record, parsed by the client's own reader.
        std::vector<unsigned char> blob;
        if (!rdb.ReadFramed(1000001, pf, blob) || blob.empty()) { printf("no playfield record %d\n", pf); return 5; }
        g_useLittle = true;
        static std::vector<unsigned char> rdbPfBuf(SLACK);
        memset(&rdbPfBuf[0], 0, SLACK);
        memset(&bsBuf[0], 0, SLACK);
        Identity_t id; id.type = 1000001; id.instance = pf;
        Probe pr;
        ProbeRaw(&g_cands[2], &rdbPfBuf[0], &bsBuf[0], &blob[0], (unsigned int)blob.size(), &id, &pr);
        printf("RDBPlayfield_t %d: %s\n", pf, pr.parsed ? "parsed" : "FAILED");
        if (!pr.parsed) return 5;

        // 2. PlayfieldProxy_t has no exported constructor anywhere in the client, so it is
        // a header-only value type. Give it a zeroed buffer with the playfield identity at
        // the front - if the layout is wrong the ctor will tell us by failing.
        static std::vector<unsigned char> proxyBuf(256);
        memset(&proxyBuf[0], 0, 256);
        ((int*)&proxyBuf[0])[0] = 1000001;
        ((int*)&proxyBuf[0])[1] = pf;
        ((int*)&proxyBuf[0])[2] = pf;

        // 3. n3Playfield_t is a large object of unknown size - be generous.
        static std::vector<unsigned char> pfBuf(256 * 1024);
        memset(&pfBuf[0], 0, pfBuf.size());

        int stage = -1;
        int built = PfBuild(&pfBuf[0], &proxyBuf[0], &rdbPfBuf[0], &stage);
        const char* stageName[] = { "ctor", "SetPlayfieldResource", "CreatePlayfieldFromResource", "done" };
        printf("build reached stage %d (%s); result %s\n", stage,
               stageName[stage < 0 ? 0 : (stage > 3 ? 3 : stage)],
               built == 1 ? "TRUE" : (built == -1 ? "false" : "CRASHED"));
        if (built != 1) return 7;

        void* surf = PfPtr(&pfBuf[0], g_pfSurface);
        void* pathf = PfPtr(&pfBuf[0], g_pfPathfinder);
        printf("GetSurface -> %p    GetPathfinder -> %p\n", surf, pathf);

        if (ptsPath && g_pfGround)
        {
            FILE* fin = fopen(ptsPath, "r");
            if (!fin) { printf("cannot read %s\n", ptsPath); return 0; }
            std::vector<Vec3> pts;
            Vec3 v;
            while (fscanf(fin, "%f %f %f", &v.x, &v.y, &v.z) == 3) pts.push_back(v);
            fclose(fin);
            int hits = 0;
            std::vector<float> errs;
            for (size_t k = 0; k < pts.size(); ++k)
            {
                Vec3 q = pts[k];
                int ok = 0;
                PfGround(&pfBuf[0], &q, &ok);
                if (!ok) continue;
                ++hits;
                float d = pts[k].y - q.y;
                errs.push_back(d < 0 ? -d : d);
                if (hits <= 6)
                    printf("   (%.1f, %.2f, %.1f) -> ground %.2f\n", pts[k].x, pts[k].y, pts[k].z, q.y);
            }
            std::sort(errs.begin(), errs.end());
            printf("CalculateGroundPoint: %d / %u points answered (%.1f%%)\n",
                   hits, (unsigned)pts.size(), pts.empty() ? 0.0 : 100.0 * hits / pts.size());
            if (!errs.empty())
                printf("|walkedY - groundY|  median %.3f  p90 %.3f  worst %.3f\n",
                       errs[errs.size()/2], errs[(size_t)(errs.size()*0.9)], errs.back());
        }
        return 0;
    }

    if (doRay)
    {
        // GetAllTriangles hands back ceilings and roofs, not the floor. The surface's own
        // collision query is the authoritative view, so ask it directly: drop a ray from
        // just above each point the owner walked and see whether the floor is really there.
        if (!g_lineHit) { printf("GetLineIntersection unavailable\n"); return 6; }
        int pf = argc > 3 ? atoi(argv[3]) : 127;
        const char* ptsPath = argc > 4 ? argv[4] : "points.txt";
        // "ray" casts a line; "sphere" uses the 3-argument overload, which takes no
        // LocalitySource pointer - one fewer unknown in the ABI.
        bool useSphere = (argc > 7 && strcmp(argv[7], "sphere") == 0);
        float above = argc > 5 ? (float)atof(argv[5]) : (useSphere ? 0.5f : 2.0f);
        float below = argc > 6 ? (float)atof(argv[6]) : (useSphere ? 1.5f : 6.0f);
        if (useSphere && !g_sphereHit) { printf("GetSphereIntersection unavailable\n"); return 6; }

        std::vector<Vec3> pts;
        FILE* pf_in = fopen(ptsPath, "r");
        if (!pf_in) { printf("cannot read %s\n", ptsPath); return 6; }
        Vec3 v;
        while (fscanf(pf_in, "%f %f %f", &v.x, &v.y, &v.z) == 3) pts.push_back(v);
        fclose(pf_in);
        printf("pf %d: %u points from %s (ray from y+%.1f down to y-%.1f)\n",
               pf, (unsigned)pts.size(), ptsPath, above, below);

        std::map<int, std::map<int, long long> >::iterator ti = rdb.offsets.find(SURFACE_TYPE);
        if (ti == rdb.offsets.end()) return 5;
        g_useLittle = true;

        // Best (highest) hit per point, over every surface record in the playfield.
        std::vector<float> bestY(pts.size(), -1e30f);
        std::vector<char>  gotHit(pts.size(), 0);
        int records = 0;
        std::map<int, long long>::iterator ii = ti->second.lower_bound(pf << 16);
        for (; ii != ti->second.end() && ((ii->first >> 16) & 0xFFFF) == pf; ++ii)
        {
            std::vector<unsigned char> blob;
            if (!rdb.ReadFramed(SURFACE_TYPE, ii->first, blob) || blob.empty()) continue;
            memset(&objBuf[0], 0, SLACK);
            memset(&bsBuf[0], 0, SLACK);
            Identity_t id; id.type = SURFACE_TYPE; id.instance = ii->first;
            Probe pr;
            ProbeRaw(&g_cands[0], &objBuf[0], &bsBuf[0], &blob[0], (unsigned int)blob.size(), &id, &pr);
            if (!pr.parsed) { DestructSurface(&objBuf[0]); continue; }
            ++records;
            printf("  record %d (inst %d, %u bytes) ...", records, ii->first, (unsigned)blob.size());
            fflush(stdout);
            for (size_t k = 0; k < pts.size(); ++k)
            {
                Vec3 hit;
                int ok = 0;
                if (useSphere)
                {
                    Vec3 c = pts[k];
                    c.y += above;                       // sphere centred just above the feet
                    CastSphere(&objBuf[0], &c, below, &hit, &ok);
                }
                else
                {
                    Vec3 from = pts[k], to = pts[k];
                    from.y += above;
                    to.y   -= below;
                    CastRay(&objBuf[0], &from, &to, &hit, &ok);
                }
                if (ok && hit.y > bestY[k]) { bestY[k] = hit.y; gotHit[k] = 1; }
            }
            printf(" ok\n");
            DestructSurface(&objBuf[0]);
        }

        int hits = 0;
        std::vector<float> errs;
        for (size_t k = 0; k < pts.size(); ++k)
        {
            if (!gotHit[k]) continue;
            ++hits;
            float d = pts[k].y - bestY[k];
            errs.push_back(d < 0 ? -d : d);
        }
        std::sort(errs.begin(), errs.end());
        printf("records used: %d\n", records);
        printf("points with a floor beneath: %d / %u  (%.1f%%)\n",
               hits, (unsigned)pts.size(), pts.empty() ? 0.0 : 100.0 * hits / pts.size());
        if (!errs.empty())
            printf("|walkedY - floorY|  median %.3f   p90 %.3f   worst %.3f\n",
                   errs[errs.size()/2], errs[(size_t)(errs.size()*0.9)], errs.back());
        for (size_t k = 0; k < pts.size() && k < 8; ++k)
            printf("   (%.1f, %.2f, %.1f) -> %s%.2f\n", pts[k].x, pts[k].y, pts[k].z,
                   gotHit[k] ? "floor " : "no hit ", gotHit[k] ? bestY[k] : 0.0f);
        return 0;
    }

    if (doExport)
    {
        CreateDirectoryA(outDir.c_str(), 0);

        // Group the surface records by playfield: instance is (playfield << 16) | index.
        std::map<int, std::vector<int> > byPf;
        std::map<int, std::map<int, long long> >::iterator ti = rdb.offsets.find(SURFACE_TYPE);
        if (ti == rdb.offsets.end()) { printf("no type %d in this RDB\n", SURFACE_TYPE); return 5; }
        std::map<int, long long>::iterator ii;
        for (ii = ti->second.begin(); ii != ti->second.end(); ++ii)
            byPf[(ii->first >> 16) & 0xFFFF].push_back(ii->first);
        printf("type %d: %u records across %u playfields\n",
               SURFACE_TYPE, (unsigned)ti->second.size(), (unsigned)byPf.size());

        std::string idxPath = outDir + "\\index.csv";
        FILE* idx = fopen(idxPath.c_str(), "w");
        if (idx) fprintf(idx, "playfield,records,parsed,verts,minx,miny,minz,maxx,maxy,maxz,teleports,file\n");

        g_useLittle = true;   // the RDB stream is little-endian
        int pfDone = 0;
        std::map<int, std::vector<int> >::iterator p;
        for (p = byPf.begin(); p != byPf.end(); ++p)
        {
            int pf = p->first;
            if (!wanted.empty() && wanted.find(pf) == wanted.end()) continue;

            char path[MAX_PATH];
            sprintf(path, "%s\\%d.tri", outDir.c_str(), pf);
            FILE* out = fopen(path, "wb");
            if (!out) { printf("cannot write %s\n", path); continue; }

            // AOTRI1: magic, playfield, recordCount(placeholder), then per record
            // instance, vertCount, teleportDestPf, teleportLocType, teleportLocInst,
            // then vertCount * 3 floats in world coordinates.
            unsigned int recCount = 0, vertTotal = 0, teleports = 0;
            fwrite("AOTRI1\0\0", 1, 8, out);
            fwrite(&pf, 4, 1, out);
            long countAt = ftell(out);
            fwrite(&recCount, 4, 1, out);

            float mn[3] = { 1e30f, 1e30f, 1e30f }, mx[3] = { -1e30f, -1e30f, -1e30f };
            for (size_t k = 0; k < p->second.size(); ++k)
            {
                int inst = p->second[k];
                std::vector<unsigned char> blob;
                if (!rdb.ReadFramed(SURFACE_TYPE, inst, blob) || blob.empty()) continue;

                memset(&objBuf[0], 0, SLACK);
                memset(&bsBuf[0], 0, SLACK);
                Identity_t id; id.type = SURFACE_TYPE; id.instance = inst;
                Probe pr;
                ProbeRaw(&g_cands[0], &objBuf[0], &bsBuf[0], &blob[0], (unsigned int)blob.size(), &id, &pr);
                if (!pr.parsed) { DestructSurface(&objBuf[0]); continue; }

                static std::vector<unsigned char> vecBuf(64);
                memset(&vecBuf[0], 0, 64);
                Tris tr;
                GetTrisRaw(&objBuf[0], &vecBuf[0], &tr);
                int dest = 0, ltype = 0, linst = 0;
                ReadTeleport(&objBuf[0], &dest, &ltype, &linst);
                if (dest) ++teleports;

                if (tr.count > 0 && tr.first)
                {
                    unsigned int vc = (unsigned int)tr.count;
                    fwrite(&inst, 4, 1, out);
                    fwrite(&vc, 4, 1, out);
                    fwrite(&dest, 4, 1, out);
                    fwrite(&ltype, 4, 1, out);
                    fwrite(&linst, 4, 1, out);
                    fwrite(tr.first, 12, vc, out);
                    ++recCount; vertTotal += vc;
                    if (tr.minx < mn[0]) mn[0] = tr.minx;  if (tr.maxx > mx[0]) mx[0] = tr.maxx;
                    if (tr.miny < mn[1]) mn[1] = tr.miny;  if (tr.maxy > mx[1]) mx[1] = tr.maxy;
                    if (tr.minz < mn[2]) mn[2] = tr.minz;  if (tr.maxz > mx[2]) mx[2] = tr.maxz;
                }
                FreeTriBuffer(&tr);
                DestructSurface(&objBuf[0]);
            }
            fseek(out, countAt, SEEK_SET);
            fwrite(&recCount, 4, 1, out);
            fclose(out);

            if (recCount == 0) { DeleteFileA(path); }
            printf("pf %-8d %5u recs -> %5u parsed  %9u verts  %s\n",
                   pf, (unsigned)p->second.size(), recCount, vertTotal,
                   recCount ? "" : "(nothing - file removed)");
            if (idx && recCount)
                fprintf(idx, "%d,%u,%u,%u,%.2f,%.2f,%.2f,%.2f,%.2f,%.2f,%u,%d.tri\n",
                        pf, (unsigned)p->second.size(), recCount, vertTotal,
                        mn[0], mn[1], mn[2], mx[0], mx[1], mx[2], teleports, pf);
            if (idx) fflush(idx);
            ++pfDone;
        }
        if (idx) fclose(idx);
        printf("\nwrote %d playfields to %s\n", pfDone, outDir.c_str());
        return 0;
    }

    if (focus)
    {
        for (int f = 0; f < NFOCUS; ++f)
        {
            Candidate* c = 0;
            for (int k = 0; k < NCAND; ++k)
                if (strcmp(g_cands[k].name, g_focus[f].cls) == 0) c = &g_cands[k];
            if (!c || !c->ctor || !c->read) { printf("%s: not usable\n", g_focus[f].cls); continue; }
            printf("\n--- %s vs type %d  (%s)\n", g_focus[f].cls, g_focus[f].type, g_focus[f].why);
            std::map<int, std::map<int, long long> >::iterator t = rdb.offsets.find(g_focus[f].type);
            if (t == rdb.offsets.end()) { printf("    type absent\n"); continue; }
            int n = 0;
            std::map<int, long long>::iterator i = t->second.lower_bound(g_focus[f].from);
            for (; i != t->second.end() && n < g_focus[f].count; ++i, ++n)
            {
                // Two framings: the bare payload, and the payload with the 12 header
                // bytes (type, instance, flag) that the record's own size field counts.
                for (int framing = 0; framing < 2; ++framing)
                {
                    std::vector<unsigned char> blob;
                    bool got = framing ? rdb.ReadFramed(g_focus[f].type, i->first, blob)
                                       : rdb.Read(g_focus[f].type, i->first, blob);
                    if (!got || blob.empty()) continue;
                    for (int pass = 0; pass < 2; ++pass)
                    {
                        g_useLittle = (pass == 1);
                        if (g_useLittle && (!g_lsCtor || !g_lsDtor)) break;
                        printf("    inst %-10d %6u bytes  %-7s %s ... ", i->first, (unsigned)blob.size(),
                               framing ? "framed" : "payload", g_useLittle ? "LE" : "BE");
                        memset(&objBuf[0], 0, SLACK);
                        memset(&bsBuf[0], 0, SLACK);
                        Identity_t id; id.type = g_focus[f].type; id.instance = i->first;
                        Probe p;
                        ProbeRaw(c, &objBuf[0], &bsBuf[0], &blob[0], (unsigned int)blob.size(), &id, &p);
                        printf("%s%s", p.parsed ? "PARSED" : "no", p.threw ? " (threw)" : "");
                        if (p.parsed && g_getAllTris && strcmp(c->name, "n3SurfaceResource_t") == 0)
                        {
                            static std::vector<unsigned char> vecBuf(64);
                            memset(&vecBuf[0], 0, 64);
                            Tris tr;
                            GetTrisRaw(&objBuf[0], &vecBuf[0], &tr);
                            if (tr.count > 0)
                                printf("  -> %d verts  X %.1f..%.1f  Y %.1f..%.1f  Z %.1f..%.1f",
                                       tr.count, tr.minx, tr.maxx, tr.miny, tr.maxy, tr.minz, tr.maxz);
                            else if (tr.threw) printf("  -> GetAllTriangles threw");
                            else printf("  -> no triangles");
                        }
                        printf("\n");
                    }
                }
            }
        }
        printf("\ndone\n");
        return 0;
    }

    for (int pass = 0; pass < 2; ++pass)
    {
    g_useLittle = (pass == 1);
    if (g_useLittle && (!g_lsCtor || !g_lsDtor)) { printf("\nno BinaryLStream exports - skipping little-endian pass\n"); break; }
    printf("\n===== stream: %s =====\n", g_useLittle ? "BinaryLStream (little-endian)" : "BinaryStream (big-endian)");
    printf("%-9s %-8s  %s\n", "type", "n", "classes that parsed (parsed/tried, threw)");
    std::map<int, std::map<int, long long> >::iterator t;
    for (t = rdb.offsets.begin(); t != rdb.offsets.end(); ++t)
    {
        std::string line;
        char buf[256];
        for (int c = 0; c < NCAND; ++c)
        {
            if (!g_cands[c].ctor || !g_cands[c].read) continue;
            int tried = 0, parsed = 0, threw = 0, bestNodes = -1;
            std::map<int, long long>::iterator i;
            for (i = t->second.begin(); i != t->second.end() && tried < perType; ++i)
            {
                std::vector<unsigned char> blob;
                if (!rdb.Read(t->first, i->first, blob) || blob.empty()) continue;
                ++tried;
                memset(&objBuf[0], 0, SLACK);
                memset(&bsBuf[0], 0, SLACK);
                Identity_t id; id.type = t->first; id.instance = i->first;
                Probe p;
                ProbeRaw(&g_cands[c], &objBuf[0], &bsBuf[0], &blob[0],
                         (unsigned int)blob.size(), &id, &p);
                parsed += p.parsed;
                threw += p.threw;
                if (p.nodes > bestNodes) bestNodes = p.nodes;
            }
            if (parsed)
            {
                if (bestNodes >= 0) sprintf(buf, "  %s(%d/%d, threw %d, nodes<=%d)", g_cands[c].name, parsed, tried, threw, bestNodes);
                else                sprintf(buf, "  %s(%d/%d, threw %d)", g_cands[c].name, parsed, tried, threw);
                line += buf;
            }
        }
        if (!line.empty())
            printf("%-9d %-8u %s\n", t->first, (unsigned)t->second.size(), line.c_str());
    }
    }
    printf("\ndone\n");
    return 0;
}
