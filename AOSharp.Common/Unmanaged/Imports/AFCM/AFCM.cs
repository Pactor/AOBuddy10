using AOSharp.Common.GameData;
using System;
using System.Runtime.InteropServices;

namespace AOSharp.Common.Unmanaged.Imports
{
    public class AFCM
    {
        [DllImport("AFCM.dll", EntryPoint = "?GetInstance@AFCM@@SAPAV1@XZ", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetInstance();
    }
}
