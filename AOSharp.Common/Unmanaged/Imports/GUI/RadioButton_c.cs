using System;
using System.Text;
using System.Runtime.InteropServices;
using AOSharp.Common.Unmanaged.DataTypes;
using AOSharp.Common.GameData;

namespace AOSharp.Common.Unmanaged.Imports
{
    public class RadioButton_c
    {
        [DllImport("GUI.dll", EntryPoint = "??0RadioButton_c@@QAE@ABVRect@@ABVString@@1HII@Z", CallingConvention = CallingConvention.ThisCall)]
        internal static extern IntPtr Constructor(IntPtr pThis, IntPtr pName, IntPtr pLabel, int unk1, uint unk2, uint unk3);

        [DllImport("GUI.dll", EntryPoint = "??1RadioButton_c@@UAE@XZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern int Deconstructor(IntPtr pThis);

        [return: MarshalAs(UnmanagedType.U1)]
        [DllImport("GUI.dll", EntryPoint = "?GetState@RadioButton_c@@QBE_NXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern bool GetState(IntPtr pThis);

        [DllImport("GUI.dll", EntryPoint = "?SetState@RadioButton_c@@QAEX_N@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void SetState(IntPtr pThis, bool state);

        public static IntPtr Create(string name, string labelText, int unk1, uint unk2, uint unk3)
        {
            StdString nameStr = StdString.Create(name);
            StdString labelStr = StdString.Create(labelText);
            IntPtr pView = Constructor(MSVCR100.New(0x168), nameStr.Pointer, labelStr.Pointer, unk1, unk2, unk3);

            return pView;
        }
    }
}
