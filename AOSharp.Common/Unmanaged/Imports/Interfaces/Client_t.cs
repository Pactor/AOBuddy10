using AOSharp.Common.GameData;
using System;
using System.Runtime.InteropServices;

namespace AOSharp.Common.Unmanaged.Imports
{
    public class Client_t
    {
        [DllImport("Interfaces.dll", EntryPoint = "?GetInstanceIfAny@Client_t@@SAPAV1@XZ", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetInstanceIfAny();

        [DllImport("Interfaces.dll", EntryPoint = "?SendVicinityMessage@Client_t@@QAEXPADIABVIdentity_t@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void SendVicinityMessage(IntPtr pThis, [MarshalAs(UnmanagedType.LPStr)] string message, int length, ref Identity unk);

        [DllImport("Interfaces.dll", EntryPoint = "?GetCookies@Client_t@@SAXAAI0@Z", CallingConvention = CallingConvention.Cdecl)]
        public static extern void GetCookies(ref uint cookie1, ref uint cookie2);

        [DllImport("Interfaces.dll", EntryPoint = "?GetServerID@Client_t@@QBEIXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern int GetServerID(IntPtr pThis);
    }
}
