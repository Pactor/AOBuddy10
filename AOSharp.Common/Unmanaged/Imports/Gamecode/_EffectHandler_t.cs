using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using AOSharp.Common.GameData;

namespace AOSharp.Common.Unmanaged.Imports
{
    public class _EffectHandler_t
    {
        [DllImport("Gamecode.dll", EntryPoint = "?GetInstance@_EffectHandler_t@@SAPAV1@XZ", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetInstance();


        [DllImport("Gamecode.dll", EntryPoint = "?CreateEffect2@_EffectHandler_t@@QAEIH@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern uint CreateEffect2(IntPtr pThis, int effect);

        [DllImport("Gamecode.dll", EntryPoint = "?CreateEffect2@_EffectHandler_t@@QAEIHABVVector3_t@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern uint CreateEffect2(IntPtr pThis, int effect, Vector3 pos);

        [DllImport("Gamecode.dll", EntryPoint = "?CreateEffect2@_EffectHandler_t@@QAEIHABVn3Dynel_t@@H@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern uint CreateEffect2(IntPtr pThis, int effect, IntPtr pDynel, int unk);

        [DllImport("Gamecode.dll", EntryPoint = "?GetEffectByID@_EffectHandler_t@@QAEPAV_GfxControl_t@@I@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr GetEffectByID(IntPtr pThis, uint effectId);

        [DllImport("Gamecode.dll", EntryPoint = "?SetDuration@_EffectHandler_t@@QAEXIM@Z")]
        public static extern void SetDuration(IntPtr pThis, uint hEffect, float duration);
    }
}
