using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AOSharp.Common.GameData
{
    public struct Rect
    {
        public float MinX;
        public float MinY;
        public float MaxX;
        public float MaxY;

        public static Rect Default => new Rect()
        {
            MinX = 0,
            MinY = 0,
            MaxX = 99999,
            MaxY = 99999
        };

        public Rect(float minX, float minY, float maxX, float maxY)
        {
            MinX = minX;
            MinY = minY;
            MaxX = maxX;
            MaxY = maxY;
        }

        public bool Contains(Vector3 Pos)
        {
            return Pos.X > MinX && Pos.X < MaxX && Pos.Z > MinY && Pos.Z < MaxY;
        }

        public override string ToString()
        {
            return $"({MinX}, {MinY}, {MaxX}, {MaxY})";
        }
    }
}
