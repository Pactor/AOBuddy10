using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace AOSharp.Common.Unmanaged.DataTypes
{
    [StructLayout(LayoutKind.Sequential, Pack=0)]
    public unsafe struct StdObjList
    {
        private IntPtr pFirst;
        public int count;

        public List<IntPtr> ToList()
        {
            List<IntPtr> pointers = new List<IntPtr>();
            IntPtr pCurrent = pFirst;

            for(int i = 0; i < count; i++)
            {
                pCurrent = *(IntPtr*)pCurrent;
                pointers.Add(pCurrent);
            }

            return pointers;
        }
    }
}
