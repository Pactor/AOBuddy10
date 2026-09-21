using AOSharp.Common.GameData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AOSharp.Common.Unmanaged.Imports.Gamecode
{
    public class GamecodeVtblDelegates
    {
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        public delegate int GetStatDelegate(IntPtr pThis, Stat stat, int detail);

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        [return: MarshalAs(UnmanagedType.U1)]
        public delegate bool IsLockedDelegate(IntPtr pThis);
    }
}
