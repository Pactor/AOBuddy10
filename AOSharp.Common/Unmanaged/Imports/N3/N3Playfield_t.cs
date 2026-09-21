using System;
using System.Runtime.InteropServices;
using AOSharp.Common.GameData;
using AOSharp.Common.Unmanaged.DataTypes;

namespace AOSharp.Common.Unmanaged.Imports
{
    public class N3Playfield_t
    {
        [return: MarshalAs(UnmanagedType.U1)]
        [DllImport("N3.dll", EntryPoint = "?LineOfSight@n3Playfield_t@@QBE_NABVVector3_t@@0H_N@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern unsafe bool LineOfSight(IntPtr pThis, Vector3* pos1, Vector3* pos2, int zoneCell, bool unknown);

        [DllImport("N3.dll", EntryPoint = "?AddChildDynel@n3Playfield_t@@QAEXPAVn3Dynel_t@@ABVVector3_t@@ABVQuaternion_t@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void AddChildDynel(IntPtr pThis, IntPtr pDynel, IntPtr pos, IntPtr rot);
        [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Unicode, SetLastError = true)]
        public delegate void DAddChildDynel(IntPtr pThis, IntPtr pDynel, IntPtr pos, IntPtr rot);

        [return: MarshalAs(UnmanagedType.U1)]
        [DllImport("N3.dll", EntryPoint = "?IsDungeon@n3Playfield_t@@QBE_NXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern bool IsDungeon(IntPtr pThis);

        [return: MarshalAs(UnmanagedType.U1)]
        [DllImport("N3.dll", EntryPoint = "?IsBattleStation@n3Playfield_t@@QBE_NXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern bool IsBattleStation(IntPtr pThis);

        [DllImport("N3.dll", CharSet = CharSet.Ansi, EntryPoint = "?GetName@n3Playfield_t@@UBEPBDXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr GetName(IntPtr pThis);

        [DllImport("N3.dll", EntryPoint = "?GetIdentity@n3Playfield_t@@QBEABVIdentity_t@@XZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern unsafe Identity* GetIdentity(IntPtr pThis);

        [DllImport("N3.dll", EntryPoint = "?GetModelID@n3Playfield_t@@QBEABVIdentity_t@@XZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern unsafe Identity* GetModelID(IntPtr pThis);

        [DllImport("N3.dll", CharSet = CharSet.Ansi, EntryPoint = "?GetTilemap@n3Playfield_t@@QBEPBVn3Tilemap_t@@XZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr GetTilemap(IntPtr pThis);

        [DllImport("N3.dll", CharSet = CharSet.Ansi, EntryPoint = "?GetSurface@n3Playfield_t@@QBEPBVSurface_i@@XZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr GetSurface(IntPtr pThis);

        [DllImport("N3.dll", CharSet = CharSet.Ansi, EntryPoint = "?GetZone@n3Playfield_t@@QAEPAVn3Zone_t@@H@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr GetZone(IntPtr pThis, int id);

        [DllImport("N3.dll", CharSet = CharSet.Ansi, EntryPoint = "?GetZones@n3Playfield_t@@QBEABV?$vector@PAVn3Zone_t@@V?$allocator@PAVn3Zone_t@@@std@@@std@@XZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern unsafe StdObjVector* GetZones(IntPtr pThis);

        [return: MarshalAs(UnmanagedType.U1)]
        [DllImport("N3.dll", EntryPoint = "?IsDoorOpenBetweenRooms@n3Playfield_t@@QBE_NFF@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern bool IsDoorOpenBetweenRooms(IntPtr pThis, short roomId1, short roomId2);
    }
}
