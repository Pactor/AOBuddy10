using System;
using System.Runtime.InteropServices;
using AOSharp.Common.GameData;

namespace AOSharp.Common.Unmanaged.Imports
{
    public class N3EngineClient_t
    {
        [DllImport("N3.dll", EntryPoint = "?OpenClient@n3EngineClient_t@@QAEXPAVResourceDatabase_t@@I@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void OpenClient(IntPtr pThis, IntPtr pResourceDatabase, int clientInst);

        [DllImport("N3.dll", EntryPoint = "?GetPlayfield@n3EngineClient_t@@SAPAVn3Playfield_t@@XZ", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetPlayfield();

        [DllImport("N3.dll", EntryPoint = "?GetActiveCamera@n3EngineClient_t@@QBEPAVn3Camera_t@@XZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr GetActiveCamera(IntPtr pThis);

        [DllImport("N3.dll", EntryPoint = "?GetClientControlDynel@n3EngineClient_t@@QBEPAVn3VisualDynel_t@@XZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr GetClientControlDynel(IntPtr pThis);

        //GetClientInst
        [DllImport("N3.dll", EntryPoint = "?GetClientInst@n3EngineClient_t@@QBEIXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern int GetClientInst(IntPtr pThis);

        [DllImport("N3.dll", EntryPoint = "?SendIIRToServer@n3EngineClient_t@@QBEXABVn3InfoItemRemote_t@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void SendIIRToServer(IntPtr pThis, IntPtr pIIR);
    }
}
