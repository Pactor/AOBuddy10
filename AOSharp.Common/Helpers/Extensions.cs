using System;
using System.Linq;
using System.Text;
using AOSharp.Common.GameData;

namespace AOSharp.Common
{
    public static class Extensions
    {
        public static string ToHexString(this byte[] data)
        {
            return BitConverter.ToString(data).Replace("-", "");
        }

        public static string ToString(this PerkHash perkHash)
        {
            return Encoding.ASCII.GetString(BitConverter.GetBytes((uint)perkHash).Reverse().ToArray());
        }
    }
}
