using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AOSharp.Common.GameData.UI
{
    public enum ButtonState
    {
        Raised = 0,
        Pressed = 1,
        Hover = 2
    }

    public enum WindowFlags
    {
        None = 0x0,
        IgnoreRaycast = 0x1,
        NoExit = 0x4,
        NoFade = 0x800,
        ManualScale = 0x900,
        AutoScale = 0x1000,
    }

    public enum WindowStyle
    {
        Default = 0,
        Popup = 2
    }
}
