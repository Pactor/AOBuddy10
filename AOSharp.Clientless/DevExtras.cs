using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AOSharp.Clientless
{
    public static class DevExtras
    {
        public static void Update(float deltaTime)
        {
            Client.Update(deltaTime);
        }
    }
}
