using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using AOSharp.Common.GameData;

namespace AOSharp.Common.Unmanaged.Imports
{
    public class N3DatabaseHandler_t
    {
        [DllImport("N3.dll", EntryPoint = "?Initialize@n3DatabaseHandler_t@@SAXXZ", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Initialize();

        [DllImport("N3.dll", EntryPoint = "?Get@n3DatabaseHandler_t@@SAAAV1@XZ", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Get();

        [DllImport("N3.dll", EntryPoint = "?GetResourceDatabase@n3DatabaseHandler_t@@QBEAAVResourceDatabase_t@@XZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr GetResourceDatabase(IntPtr pThis);
    }
}
