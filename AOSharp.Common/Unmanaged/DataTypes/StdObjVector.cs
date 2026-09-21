using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace AOSharp.Common.Unmanaged.DataTypes
{
    [StructLayout(LayoutKind.Sequential, Pack=0)]
    public unsafe struct StdObjVector
    {
        private IntPtr pFirst;
        private IntPtr pLast;

        public List<IntPtr> ToList()
        {
            List<IntPtr> pointers = new List<IntPtr>();

            for (IntPtr pCurrent = pFirst; pCurrent.ToInt32() < pLast.ToInt32(); pCurrent += 4)
                pointers.Add(*(IntPtr*)pCurrent);

            return pointers;
        }
    }
}
