using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AOSharp.Common.Unmanaged.Imports
{
    public class Render_t
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr CreateRenderDelegate();
        public static CreateRenderDelegate CreateRender;
    }
}
