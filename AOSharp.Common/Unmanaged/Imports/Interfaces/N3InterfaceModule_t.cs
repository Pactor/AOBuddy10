using System;
using System.Runtime.InteropServices;
using AOSharp.Common.GameData;
using AOSharp.Common.Unmanaged.DataTypes;

namespace AOSharp.Common.Unmanaged.Imports
{
    public class N3InterfaceModule_t
    {
        [DllImport("Interfaces.dll", EntryPoint = "?GetInstance@N3InterfaceModule_t@@SAPAV1@XZ", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetInstance();

        [DllImport("Interfaces.dll", EntryPoint = "?GetClientInst@N3InterfaceModule_t@@QBEIXZ", CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetClientInst();

        [DllImport("Interfaces.dll", EntryPoint = "?ShutdownMessage@N3InterfaceModule_t@@CAXXZ", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ShutdownMessage();

        public static IntPtr GetPFName(int pfId) { return GetPFName(GetInstance(), pfId); }

        [DllImport("Interfaces.dll", EntryPoint = "?N3Msg_GetPFName@N3InterfaceModule_t@@QBEPBDI@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr GetPFName(IntPtr pThis, int pfId);


        [DllImport("Interfaces.dll", EntryPoint = "?N3Msg_CastNanoSpell@N3InterfaceModule_t@@QBEXABVIdentity_t@@0@Z", CallingConvention = CallingConvention.ThisCall)]
        public unsafe static extern void CastNanoSpell(IntPtr pThis, Identity* nano, Identity target);

        [DllImport("Interfaces.dll", EntryPoint = "?N3Msg_GetPerkProgress@N3InterfaceModule_t@@QBEMI@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern float GetPerkProgress(IntPtr pThis, uint perkId);

        [DllImport("Interfaces.dll", EntryPoint = "?N3Msg_GetDesc@N3InterfaceModule_t@@QBEPBDABVIdentity_t@@0@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr GetDesc(IntPtr pThis, ref Identity target, int unk);

        public unsafe delegate void DCastNanoSpell(IntPtr pThis, Identity* nanoIdentity, Identity targetIdentity);

        [return: MarshalAs(UnmanagedType.U1)]
        [DllImport("Interfaces.dll", EntryPoint = "?N3Msg_GetCompletedPersonalResearchGoals@N3InterfaceModule_t@@QAEXAAV?$vector@IV?$allocator@I@std@@@std@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern bool GetCompletedPersonalResearchGoals(IntPtr pThis, ref StdStructVector vector);

        [return: MarshalAs(UnmanagedType.U1)]
        [DllImport("Interfaces.dll", EntryPoint = "?N3Msg_PersonalResearchGoals@N3InterfaceModule_t@@QAEXAAV?$vector@U?$pair@I_N@std@@V?$allocator@U?$pair@I_N@std@@@2@@std@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern bool GetPersonalResearchGoals(IntPtr pThis, ref StdStructVector vector);

        [DllImport("Interfaces.dll", EntryPoint = "?SetCharID@Client_t@@SAXI@Z", CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetCharID(int charId);

        [DllImport("Interfaces.dll", EntryPoint = "?GetCharID@Client_t@@SAIXZ", CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetCharID();

        [DllImport("Interfaces.dll", EntryPoint = "?ProcessMessage@Client_t@@AAEHPAVMessage_t@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr ProcessMessage(IntPtr pThis, IntPtr pMsg);
    }
}
