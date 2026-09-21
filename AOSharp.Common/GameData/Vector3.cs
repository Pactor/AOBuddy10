using System;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

namespace AOSharp.Common.GameData
{
    [StructLayout(LayoutKind.Sequential, Size = 0xC)]
    public struct Vector3
    {
        public const float ZeroTolerance = 1e-6f;

        [AoMember(0)]
        public float X { get; set; }

        [AoMember(1)]
        public float Y { get; set; }

        [AoMember(2)]
        public float Z { get; set; }

        [JsonIgnore]
        public Vector2 XY
        {
            get { return new Vector2(X, Y); }
        }

        [JsonIgnore]
        public Vector2 XZ
        {
            get { return new Vector2(X, Z); }
        }

        [JsonIgnore]
        public Vector2 YX
        {
            get { return new Vector2(Y, X); }
        }

        [JsonIgnore]
        public Vector2 YZ
        {
            get { return new Vector2(Y, Z); }
        }

        [JsonIgnore]
        public Vector2 ZX
        {
            get { return new Vector2(Z, X); }
        }

        [JsonIgnore]
        public Vector2 ZY
        {
            get { return new Vector2(Z, Y); }
        }

        public static readonly Vector3 Zero = new Vector3(0, 0, 0);
        public static readonly Vector3 Forward = new Vector3(0, 0, 1);
        public static readonly Vector3 Right = new Vector3(1, 0, 0);
        public static readonly Vector3 Up = new Vector3(0, 1, 0);

        public Vector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Vector3(double x, double y, double z)
        {
            X = (float)x;
            Y = (float)y;
            Z = (float)z;
        }

        public Vector3(double x, double y)
        {
            X = (float)x;
            Y = (float)y;
            Z = 0;
        }

        [JsonIgnore]
        public float Magnitude
        {
            get { return Mathf.Sqrt(Mathf.Pow(X, 2) + Mathf.Pow(Y, 2) + Mathf.Pow(Z, 2)); }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("value", value,
                                                          "The magnitude of a Vector must be positive or 0.");
                }
                else if (Magnitude == 0)
                {
                    throw new DivideByZeroException("Can not set the magnitude of a Vector with no direction");
                }
                else
                {
                    float factor = value / Magnitude;
                    X = (X * factor);
                    Y = (Y * factor);
                    Z = (Z * factor);
                }
            }
        }

        [JsonIgnore]
        public float SqrMagnitude { get { return X * X + Y * Y + Z * Z; } }

        public static float Angle(Vector3 from, Vector3 to)
        {
            return Mathf.Acos(Vector3.Dot(from.Normalize(), to.Normalize())) * 57.29578f;
        }

        public static float Distance(Vector3 from, Vector3 to)
        {
            return Mathf.Sqrt(Mathf.Pow(Mathf.Abs(from.X - to.X), 2) + Mathf.Pow(Mathf.Abs(from.Y - to.Y), 2) + Mathf.Pow(Mathf.Abs(from.Z - to.Z), 2));
        }

        public float DistanceFrom(Vector3 pos)
        {
            return Mathf.Sqrt(Mathf.Pow(Mathf.Abs(X - pos.X), 2) + Mathf.Pow(Mathf.Abs(Y - pos.Y), 2) + Mathf.Pow(Mathf.Abs(Z - pos.Z), 2));
        }

        public float Distance2DFrom(Vector3 pos)
        {
            return Mathf.Sqrt(Mathf.Pow(Mathf.Abs(X - pos.X), 2) + Mathf.Pow(Mathf.Abs(Z - pos.Z), 2));
        }

        public Vector3 Translate(Vector2 vec)
        {
            return new Vector3(X + vec.X, Y, Z + vec.Y);
        }

        public static Vector3 Rotate(Vector3 pivot, Vector3 localPos, float angle)
        {
            localPos.X -= pivot.X;
            localPos.Z -= pivot.Z;
            float dist = Mathf.Sqrt(localPos.X * localPos.X + localPos.Z * localPos.Z);

            float ca = Mathf.Atan2(localPos.Z, localPos.X) * 180 / Mathf.PI;
            float na = ((ca + (360 - angle)) % 360) * Mathf.PI / 180;

            Vector3 newVertexPos = new Vector3(0, localPos.Y, 0);
            newVertexPos.X = (pivot.X + dist * Mathf.Cos(na));
            newVertexPos.Z = (pivot.Z + dist * Mathf.Sin(na));

            return newVertexPos;
        }

        public Vector3 PointOnLine(Vector3 start, Vector3 end)
        {
            Vector3 lineDelta = end - start;
            Vector3 pointDelta = this - start;
            float lineSquareLength = lineDelta.SqrMagnitude;
            float dot = (float)lineDelta.Dot(pointDelta);
            float percent = dot / lineSquareLength;

            if (percent < 0f || percent > 1f)
                return Zero;

            return start + (lineDelta * percent);
        }

        public Vector3 Randomize(float magnitude)
        {
            Random rnd = new Random();
            return this + new Vector3(rnd.Next((int)-magnitude, (int)magnitude), 0, rnd.Next((int)-magnitude, (int)magnitude));
        }

        [Obsolete("This property is obsolete. Use Magnitude instead.")]
        public float Length()
        {
            return Mathf.Sqrt((X * X) + (Y * Y) + (Z * Z));
        }

        [Obsolete("This property is obsolete. Use SqrMagnitude instead.")]
        public float LengthSquared()
        {
            return (X * X) + (Y * Y) + (Z * Z);
        }

        /// <summary>
        /// Returns the Absolute value of the Vector
        /// </summary>
        /// <param name="v1">Vector</param>
        public static double Abs(Vector3 v1)
        {
            return v1.Magnitude;
        }

        /// <summary>
        /// Returns the Absolute value of the Vector
        /// </summary>
        public double Abs()
        {
            return Magnitude;
        }

        /// <summary>
        /// Returns true if the Vector is a Unit Vector (ie, is of magnitude 1)
        /// </summary>
        /// <param name="v1">Vector</param>
        public static bool IsUnitVector(Vector3 v1)
        {
            return Mathf.Abs(v1.Magnitude - 1) <= double.Epsilon;
        }

        /// <summary>
        /// Returns true if the Vector is a Unit Vector (ie, is of magnitude 1)
        /// </summary>
        public bool IsUnitVector()
        {
            return IsUnitVector(this);
        }

        /// <summary>
        /// Returns the Normalized Vector
        /// </summary>
        /// <param name="v1">Vector</param>
        public static Vector3 Normalize(Vector3 v1)
        {
            if (v1.Magnitude == 0)
            {
                throw new DivideByZeroException("Can not normalize a Vector with no direction");
            }
            else
            {
                Vector3 UnitVector = v1;

                UnitVector.Magnitude = 1;

                return UnitVector;
            }
        }

        /// <summary>
        /// Returns the Normalized Vector
        /// </summary>
        public Vector3 Normalize()
        {
            return Normalize(this);
        }

        /// <summary>
        /// Returns the Cross Product of two Vectors
        /// </summary>
        /// <param name="vLeft">Vector 1</param>
        /// <param name="vRight">Vector 2</param>
        public static Vector3 Cross(Vector3 vLeft, Vector3 vRight)
        {
            return new Vector3(vLeft.Y * vRight.Z - vLeft.Z * vRight.Y, vLeft.Z * vRight.X - vLeft.X * vRight.Z,
                               vLeft.X * vRight.Y - vLeft.Y * vRight.X);
        }

        /// <summary>
        /// Returns the Cross Product of two Vectors
        /// </summary>
        /// <param name="vRight">Other Vector</param>
        public Vector3 Cross(Vector3 vRight)
        {
            return Cross(this, vRight);
        }

        /// <summary>
        /// Returns the Dot Product of two Vectors
        /// </summary>
        /// <param name="v1">Vector 1</param>
        /// <param name="v2">Vector 2</param>
        public static float Dot(Vector3 v1, Vector3 v2)
        {
            return v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z;
        }

        /// <summary>
        /// Returns the Dot Product of two Vectors
        /// </summary>
        /// <param name="v1">Other Vector</param>
        public float Dot(Vector3 v1)
        {
            return Dot(this, v1);
        }

        public override string ToString()
        {
            return $"({X}, {Y}, {Z})";
        }

        public Vector2 ToVector2()
        {
            return new Vector2(X, Y);
        }

        public static Vector3 operator *(Vector3 v, float mag)
        {
            return new Vector3(v.X * mag, v.Y * mag, v.Z * mag);
        }

        public static Vector3 operator *(float mag, Vector3 v)
        {
            return new Vector3(v.X * mag, v.Y * mag, v.Z * mag);
        }

        public static Vector3 operator *(Vector3 v1, Vector3 v2)
        {
            return new Vector3(v1.X * v2.X, v1.Y * v2.Y, v1.Z * v2.Z);
        }

        public static Vector3 operator +(Vector3 v1, Vector3 v2)
        {
            return new Vector3(v1.X + v2.X, v1.Y + v2.Y, v1.Z + v2.Z);
        }

        public static Vector3 operator -(Vector3 v1, Vector3 v2)
        {
            return new Vector3(v1.X - v2.X, v1.Y - v2.Y, v1.Z - v2.Z);
        }

        public static Vector3 operator -(Vector3 a)
        {
            return new Vector3(-a.X, -a.Y, -a.Z);
        }

        public static Vector3 operator /(Vector3 v, float mag)
        {
            return new Vector3(v.X / mag, v.Y / mag, v.Z / mag);
        }

        public static Vector3 operator /(Vector3 v1, Vector3 v2)
        {
            return new Vector3(v1.X / v2.X, v1.Y / v2.Y, v1.Z / v2.Z);
        }

        public static bool operator ==(Vector3 v1, Vector3 v2)
        {
            if (v1.X == v2.X && v1.Y == v2.Y && v1.Z == v2.Z)
                return true;
            else
                return false;
        }

        public static bool operator !=(Vector3 v1, Vector3 v2)
        {
            if (v1.X != v2.X || v1.Y != v2.Y || v1.Z != v2.Z)
                return true;
            else
                return false;
        }

        public override int GetHashCode()
        {
            return X.GetHashCode() ^ (Y.GetHashCode() << 2) ^ (Z.GetHashCode() >> 2);
        }

        public bool Equals(Vector3 pos)
        {
            return pos == this;
        }

        public override bool Equals(object other)
        {
            if (other is Vector3 v)
                return Equals(v);
            return false;
        }
    }
}