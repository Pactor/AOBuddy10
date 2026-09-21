using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AOSharp.Common.Unmanaged.Imports
{
    public class BrowserWindow_c
    {
        [DllImport("GUI.dll", EntryPoint = "??0BrowserWindow_c@@QAE@W4BrowserMode_e@0@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void CreateBrowserWindow(IntPtr pThis, int browserMode = 0);
    }
}
