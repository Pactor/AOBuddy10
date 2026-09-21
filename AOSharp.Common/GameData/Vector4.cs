using System;
using System.Runtime.InteropServices;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

namespace AOSharp.Common.GameData
{
    [StructLayout(LayoutKind.Sequential, Size=0x10)]
    public struct Vector4
    {
        public const float ZeroTolerance = 1e-6f;

        [AoMember(0)]
        public float X { get; set; }

        [AoMember(1)]
        public float Y { get; set; }

        [AoMember(2)]
        public float Z { get; set; }

        [AoMember(3)]
        public float W { get; set; }

        public Vector4(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public override string ToString()
        {
            return $"({X}, {Y}, {Z}, {W})";
        }
    }
}
