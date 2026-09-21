using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using AOSharp.Common.GameData;

namespace AOSharp.Common.Unmanaged.Imports.DatabaseController
{
    public class DatabaseController_t
    {
        [DllImport("DatabaseController.dll", EntryPoint = "?ErrNo@DatabaseController_t@@UAEHXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern int ErrorNo(IntPtr pThis);

        [DllImport("DatabaseController.dll", EntryPoint = "?ErrorStr@DatabaseController_t@@UAEPBDXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr ErrorStr(IntPtr pThis);
    }

    public class ResourceDatabase_t
    {
        [DllImport("DatabaseController.dll", EntryPoint = "??0ResourceDatabase_t@@QAE@XZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr Constructor(IntPtr pThis);

        [DllImport("DatabaseController.dll", EntryPoint = "?Open@ResourceDatabase_t@@QAEHABV?$basic_string@DU?$char_traits@D@std@@V?$allocator@D@2@@std@@_N@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern int Open(IntPtr pThis, IntPtr pPath, bool readOnly);

        [DllImport("DatabaseController.dll", EntryPoint = "?GetDbObject@ResourceDatabase_t@@UAEPAVDbObject_t@@ABVIdentity_t@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr GetDbObject(IntPtr pThis, ref DBIdentity identity);

        [DllImport("DatabaseController.dll", EntryPoint = "?PutDbBlob@ResourceDatabase_t@@QAEXABVIdentity_t@@PBXI@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void PutDbBlob(IntPtr pThis, ref DBIdentity identity, [MarshalAs(UnmanagedType.LPArray)] byte[] data, int size);

        [DllImport("DatabaseController.dll", EntryPoint = "?PutDbObject@ResourceDatabase_t@@UAEXPAVDbObject_t@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void PutDbObject(IntPtr pThis, IntPtr pDBObject);
    }
}

