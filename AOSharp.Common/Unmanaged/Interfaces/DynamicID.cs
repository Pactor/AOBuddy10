using System;
using System.Collections.Generic;
using AOSharp.Common.GameData;
using AOSharp.Common.Helpers;
using AOSharp.Common.Unmanaged.Imports;

namespace AOSharp.Common.Unmanaged.Interfaces
{
    public class DynamicID
    {
        public static Dictionary<string, int> DynamicIDOverrides = new Dictionary<string, int>();

        public static int GetID(string name, bool unk)
        {
            if(DynamicIDOverrides.TryGetValue(name, out int id))
                return id;

            IntPtr pDynamicID = DynamicID_t.GetInstance();

            if (pDynamicID == IntPtr.Zero)
                return 0;

            return DynamicID_t.GetID(pDynamicID, name, unk);
        }

        public static void Add(string name, int id)
        {
            DynamicIDOverrides.Add(name, id);
        }
    }
}
