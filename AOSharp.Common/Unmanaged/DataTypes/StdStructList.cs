using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace AOSharp.Common.Unmanaged.DataTypes
{
    [StructLayout(LayoutKind.Sequential, Pack=0)]
    public unsafe struct StdStructList
    {
        private IntPtr _pArray;
        public int Count;

        public IEnumerable<T> ToList<T>() where T : unmanaged
        {
            List<T> structs = new List<T>();

            for (int i = 0; i < Count; i++)
                structs.Add(*(T*)(_pArray + i * sizeof(T)));

            return structs;
        }
    }
}
