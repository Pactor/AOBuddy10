using AOSharp.Common.GameData;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AOSharp.Common
{
    public static class AOMath
    {
        public static int SellPrice(int value, int compLit, float shopModifier = 4f)
        {
            int clModifier = compLit / 40;
            return (int)(value * shopModifier * (100 + clModifier) / 2500);
        }
    }
}
