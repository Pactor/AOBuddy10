using AOSharp.Common.GameData;
using AOSharp.Common.Unmanaged.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace AOSharp.Clientless
{
    public class Utils
    {
        public static long SetExpireTimeInTicks(float timeInSeconds) =>  DateTime.Now.AddSeconds(timeInSeconds).Ticks;
        public static double GetRemainingTimeInSeconds(long expireTimeTicks) => TimeSpan.FromTicks(expireTimeTicks - DateTime.Now.Ticks).TotalSeconds;
    }
}
