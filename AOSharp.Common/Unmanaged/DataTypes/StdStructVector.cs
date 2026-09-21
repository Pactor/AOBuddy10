using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace AOSharp.Common.Unmanaged.DataTypes
{
    [StructLayout(LayoutKind.Sequential, Pack=0)]
    public unsafe struct StdStructVector
    {
        private IntPtr pFirst;
        private IntPtr pLast;
        private IntPtr Unk;

        public List<IntPtr> ToList(int size)
        {
            List<IntPtr> pointers = new List<IntPtr>();

            for (IntPtr pCurrent = pFirst; pCurrent.ToInt32() < pLast.ToInt32(); pCurrent += size)
                pointers.Add(pCurrent);

            return pointers;
        }

        public List<T> ToList<T>() where T : unmanaged
        {
            List<T> list = new List<T>();

            for (IntPtr pCurrent = pFirst; pCurrent.ToInt32() < pLast.ToInt32(); pCurrent += sizeof(T))
                list.Add(*(T*)pCurrent);

            return list;
        }
    }
}
