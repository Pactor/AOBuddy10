using System;
using Newtonsoft.Json;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

namespace AOSharp.Common.GameData
{
    public struct Quaternion
    {
        #region Variables

        [AoMember(0)]
        public float X { get; set; }

        [AoMember(1)]
        public float Y { get; set; }

        [AoMember(2)]
        public float Z { get; set; }

        [AoMember(3)]
        public float W { get; set; }

        #endregion

        private const float radToDeg = (180.0f / Mathf.PI);
   
        private const float degToRad = (Mathf.PI / 180.0f);

        [JsonIgnore]
        public Vector3 XYZ
        {
            set
            {
                X = value.X;
                Y = value.Y;
                Z = value.Z;
            }
            get
            {
                return new Vector3(X, Y, Z);
            }
        }

        public float this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return this.X;
                    case 1:
                        return this.Y;
                    case 2:
                        return this.Z;
                    case 3:
                        return this.W;
                    default:
                        throw new IndexOutOfRangeException("Invalid Quaternion index!");
                }
            }
            set
            {
                switch (index)
                {
                    case 0:
                        this.X = value;
                        break;
                    case 1:
                        this.Y = value;
                        break;
                    case 2:
                        this.Z = value;
                        break;
                    case 3:
                        this.X = value;
                        break;
                    default:
                        throw new IndexOutOfRangeException("Invalid Quaternion index!");
                }
            }
        }

        [JsonIgnore]
        public Vector3 EulerAngles
        {
            get
            {
                return Quaternion.Internal_ToEulerRad(this) * radToDeg;
            }
            set
            {
                this = Quaternion.Internal_FromEulerRad(value * degToRad);
            }
        }

        [JsonIgnore]
        public Vector3 Forward => this * Vector3.Forward;

        public static Quaternion Identity => new Quaternion(0, 0, 0, 1f);

        [JsonIgnore]
        public float Magnitude
        {
            get
            {
                return (float)Mathf.Sqrt(X * X + Y * Y + Z * Z + W * W);
            }
        }

        /// <summary>
        /// Gets the square of the quaternion length (magnitude).
        /// </summary>
        [JsonIgnore]
        public float SqrMagnitude
        {
            get
            {
                return X * X + Y * Y + Z * Z + W * W;
            }
        }
        #region Constructor

        public Quaternion(double x, double y, double z, double w)
        {
            this.X = (float)x;
            this.Y = (float)y;
            this.Z = (float)z;
            this.W = (float)w;
        }

        public Quaternion(float x, float y, float z, float w)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
            this.W = w;
        }

        public Quaternion(Vector3 v, float angle)
        {
            double sinAngle;
            Vector3 vNormalized;

            vNormalized = v.Normalize();

            sinAngle = Mathf.Sin(angle / 2);
            X = (float)(vNormalized.X * sinAngle);
            Y = (float)(vNormalized.Y * sinAngle);
            Z = (float)(vNormalized.Z * sinAngle);

            W = (float)Mathf.Cos(angle / 2);
        }

        public Quaternion(Vector3 v)
        {
            X = v.X;
            Y = v.Y;
            Z = v.Z;
            W = 0;
        }
        #endregion

        #region Methods

        public void Update(float x, float y, float z, float w)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
            this.W = w;
        }

        [JsonIgnore]
        public float Yaw
        {
            get
            {
                float yaw = Mathf.Atan2((2 * Y * W) - (2 * X * Z), 1 - (2 * Y * Y) - (2 * Z * Z));
                if (yaw < 0) // So we get a positive number
                    yaw += 2 * Mathf.PI;
                return yaw;
            }
        }

        [JsonIgnore]
        public float Pitch
        {
            get { return -2 * Mathf.Atan2((2 * X * W) - (2 * Y * Z), 1 - (2 * X * Y) - (2 * Z * Z)); }
        }

        [JsonIgnore]
        public float Roll
        {
            get
            {
                return Mathf.Asin((2 * X * Y) + (2 * Z * W));
            }
        }


        public static Quaternion Conjugate(Quaternion q1)
        {
            return new Quaternion(-q1.X, -q1.Y, -q1.Z, q1.W);
        }

        public Quaternion Conjugate()
        {
            return Conjugate(this);
        }

        public static Quaternion LookRotation(Vector3 forward, Vector3 up)
        {

            forward = Vector3.Normalize(forward);
            Vector3 right = Vector3.Normalize(Vector3.Cross(up, forward));
            up = Vector3.Cross(forward, right);
            var m00 = right.X;
            var m01 = right.Y;
            var m02 = right.Z;
            var m10 = up.X;
            var m11 = up.Y;
            var m12 = up.Z;
            var m20 = forward.X;
            var m21 = forward.Y;
            var m22 = forward.Z;


            float num8 = (m00 + m11) + m22;
            var quaternion = new Quaternion();
            if (num8 > 0f)
            {
                var num = Mathf.Sqrt(num8 + 1f);
                quaternion.W = num * 0.5f;
                num = 0.5f / num;
                quaternion.X = (m12 - m21) * num;
                quaternion.Y = (m20 - m02) * num;
                quaternion.Z = (m01 - m10) * num;
                return quaternion;
            }
            if ((m00 >= m11) && (m00 >= m22))
            {
                var num7 = Mathf.Sqrt(((1f + m00) - m11) - m22);
                var num4 = 0.5f / num7;
                quaternion.X = 0.5f * num7;
                quaternion.Y = (m01 + m10) * num4;
                quaternion.Z = (m02 + m20) * num4;
                quaternion.W = (m12 - m21) * num4;
                return quaternion;
            }
            if (m11 > m22)
            {
                var num6 = Mathf.Sqrt(((1f + m11) - m00) - m22);
                var num3 = 0.5f / num6;
                quaternion.X = (m10 + m01) * num3;
                quaternion.Y = 0.5f * num6;
                quaternion.Z = (m21 + m12) * num3;
                quaternion.W = (m20 - m02) * num3;
                return quaternion;
            }
            var num5 = Mathf.Sqrt(((1f + m22) - m00) - m11);
            var num2 = 0.5f / num5;
            quaternion.X = (m20 + m02) * num2;
            quaternion.Y = (m21 + m12) * num2;
            quaternion.Z = 0.5f * num5;
            quaternion.W = (m01 - m10) * num2;
            return quaternion;
        }

        public static Quaternion FromTo(Vector3 u, Vector3 v)
        {
            return LookRotation(v - u, Vector3.Up);
        }

        public static Quaternion Hamilton(Quaternion vLeft, Quaternion vRight)
        {
            double w = (vLeft.W * vRight.W) - (vLeft.X * vRight.X) - (vLeft.Y * vRight.Y) - (vLeft.Z * vRight.Z);
            double x = (vLeft.W * vRight.X) + (vLeft.X * vRight.W) + (vLeft.Y * vRight.Z) - (vLeft.Z * vRight.Y);
            double y = (vLeft.W * vRight.Y) - (vLeft.X * vRight.Z) + (vLeft.Y * vRight.W) + (vLeft.Z * vRight.X);
            double z = (vLeft.W * vRight.Z) + (vLeft.X * vRight.Y) - (vLeft.Y * vRight.X) + (vLeft.Z * vRight.W);

            return new Quaternion(x, y, z, w);
        }

        public void Rotate(float heading, float attitude, float bank)
        {
            // Assuming the angles are in radians.
            float c1 = Mathf.Cos(heading / 2);
            float s1 = Mathf.Sin(heading / 2);
            float c2 = Mathf.Cos(attitude / 2);
            float s2 = Mathf.Sin(attitude / 2);
            float c3 = Mathf.Cos(bank / 2);
            float s3 = Mathf.Sin(bank / 2);
            float c1c2 = c1 * c2;
            float s1s2 = s1 * s2;
            W = (c1c2 * c3 - s1s2 * s3);
            X = (c1c2 * s3 + s1s2 * c3);
            Y = (s1 * c2 * c3 + c1 * s2 * s3);
            Z = (c1 * s2 * c3 - s1 * c2 * s3);
        }


        public static Quaternion CreateFromAxisAngle(Vector3 axis, double a)
        {
            return CreateFromAxisAngle(axis, (float)a);
        }

        public static Quaternion CreateFromAxisAngle(Vector3 axis, float a)
        {
            return CreateFromAxisAngle(axis.X, axis.Y, axis.Z, a);
        }

        public static Quaternion CreateFromAxisAngle(double xx, double yy, double zz, double a)
        {
            return CreateFromAxisAngle((float)xx, (float)yy, (float)zz, (float)a);
        }

        public static Quaternion CreateFromAxisAngle(float xx, float yy, float zz, float a)
        {
            // Here we calculate the sin( theta / 2) once for optimization
            float result = Mathf.Sin(a / 2.0f);

            // Calculate the x, y and z of the quaternion
            float x = xx * result;
            float y = yy * result;
            float z = zz * result;

            // Calculate the w value by cos( theta / 2 )
            float w = Mathf.Cos(a / 2.0f);

            return new Quaternion(x, y, z, w).Normalize();
        }

        public static Quaternion AngleAxis(float degress, Vector3 axis)
        {
            if (axis.Magnitude == 0.0f)
                return Identity;

            Quaternion result = Identity;
            float radians = degress * (Mathf.PI / 180.0f);
            radians *= 0.5f;
            axis.Normalize();
            axis = axis * Mathf.Sin(radians);
            result.X = axis.X;
            result.Y = axis.Y;
            result.Z = axis.Z;
            result.W = Mathf.Cos(radians);

            return Normalize(result);
        }

        public void ToAngleAxis(out float angle, out Vector3 axis)
        {
            Quaternion.Internal_ToAxisAngleRad(this, out axis, out angle);
            angle *= radToDeg;
        }

        private static void Internal_ToAxisAngleRad(Quaternion q, out Vector3 axis, out float angle)
        {
            if (Mathf.Abs(q.W) > 1.0f)
                q.Normalize();

            angle = 2.0f * Mathf.Acos(q.W); // angle
            float den = Mathf.Sqrt(1.0f - q.W * q.W);
            if (den > 0.0001f)
            {
                axis = q.XYZ / den;
            }
            else
            {
                // This occurs when the angle is zero. 
                // Not a problem: just set an arbitrary normalized axis.
                axis = new Vector3(1, 0, 0);
            }
        }

        public Quaternion Hamilton(Quaternion vRight)
        {
            return Hamilton(this, vRight);
        }

        public static Quaternion Normalize(Quaternion q1)
        {
            double mag = q1.Magnitude;

            return new Quaternion(q1.X / mag, q1.Y / mag, q1.Z / mag, q1.W / mag);
        }

        public Quaternion Normalize()
        {
            return Normalize(this);
        }

        public static float Dot(Quaternion a, Quaternion b)
        {
            return a.X * b.X + a.Y * b.Y + a.Z * b.Z + a.W * b.W;
        }

        public static Quaternion Euler(double x, double y, double z)
        {
            return Quaternion.Internal_FromEulerRad(new Vector3((float)x, (float)y, (float)z) * degToRad);
        }
        /// <summary>
        ///   <para>Returns a rotation that rotates z degrees around the z axis, x degrees around the x axis, and y degrees around the y axis (in that order).</para>
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public static Quaternion Euler(float x, float y, float z)
        {
            return Quaternion.Internal_FromEulerRad(new Vector3(x, y, z) * degToRad);
        }
        /// <summary>
        ///   <para>Returns a rotation that rotates z degrees around the z axis, x degrees around the x axis, and y degrees around the y axis (in that order).</para>
        /// </summary>
        /// <param name="euler"></param>
        public static Quaternion Euler(Vector3 euler)
        {
            return Quaternion.Internal_FromEulerRad(euler * degToRad);

        }

        private static Quaternion Internal_FromEulerRad(Vector3 euler)
        {
            var yaw = euler.X;
            var pitch = euler.Y;
            var roll = euler.Z;
            float rollOver2 = roll * 0.5f;
            float sinRollOver2 = Mathf.Sin(rollOver2);
            float cosRollOver2 = Mathf.Cos(rollOver2);
            float pitchOver2 = pitch * 0.5f;
            float sinPitchOver2 = Mathf.Sin(pitchOver2);
            float cosPitchOver2 = Mathf.Cos(pitchOver2);
            float yawOver2 = yaw * 0.5f;
            float sinYawOver2 = Mathf.Sin(yawOver2);
            float cosYawOver2 = Mathf.Cos(yawOver2);
            Quaternion result = new Quaternion();
            result.X = cosYawOver2 * cosPitchOver2 * cosRollOver2 + sinYawOver2 * sinPitchOver2 * sinRollOver2;
            result.Y = cosYawOver2 * cosPitchOver2 * sinRollOver2 - sinYawOver2 * sinPitchOver2 * cosRollOver2;
            result.Z = cosYawOver2 * sinPitchOver2 * cosRollOver2 + sinYawOver2 * cosPitchOver2 * sinRollOver2;
            result.W = sinYawOver2 * cosPitchOver2 * cosRollOver2 - cosYawOver2 * sinPitchOver2 * sinRollOver2;
            return result;

        }

        private static Vector3 Internal_ToEulerRad(Quaternion rotation)
        {
            float sqw = rotation.W * rotation.W;
            float sqx = rotation.X * rotation.X;
            float sqy = rotation.Y * rotation.Y;
            float sqz = rotation.Z * rotation.Z;
            float unit = sqx + sqy + sqz + sqw; // if normalised is one, otherwise is correction factor
            float test = rotation.X * rotation.W - rotation.Y * rotation.Z;
            Vector3 v = new Vector3();

            if (test > 0.4995f * unit)
            { // singularity at north pole
                v.Y = 2f * Mathf.Atan2(rotation.Y, rotation.X);
                v.X = Mathf.PI / 2;
                v.Z = 0;
                return NormalizeAngles(v * Mathf.Rad2Deg);
            }
            if (test < -0.4995f * unit)
            { // singularity at south pole
                v.Y = -2f * Mathf.Atan2(rotation.Y, rotation.X);
                v.X = -Mathf.PI / 2;
                v.Z = 0;
                return NormalizeAngles(v * Mathf.Rad2Deg);
            }
            Quaternion q = new Quaternion(rotation.W, rotation.Z, rotation.X, rotation.Y);
            v.Y = (float)Mathf.Atan2(2f * q.X * q.W + 2f * q.Y * q.Z, 1 - 2f * (q.Z * q.Z + q.W * q.W));     // Yaw
            v.X = (float)Mathf.Asin(2f * (q.X * q.Z - q.W * q.Y));                             // Pitch
            v.Z = (float)Mathf.Atan2(2f * q.X * q.Y + 2f * q.Z * q.W, 1 - 2f * (q.Y * q.Y + q.Z * q.Z));      // Roll
            return NormalizeAngles(v * Mathf.Rad2Deg);
        }

        private static Vector3 NormalizeAngles(Vector3 angles)
        {
            angles.X = NormalizeAngle(angles.X);
            angles.Y = NormalizeAngle(angles.Y);
            angles.Z = NormalizeAngle(angles.Z);
            return angles;
        }

        private static float NormalizeAngle(float angle)
        {
            float modAngle = angle % 360.0f;

            if (modAngle < 0.0f)
                return modAngle + 360.0f;
            else
                return modAngle;
        }

        /// <summary>
        ///   <para>Returns the Inverse of /rotation/.</para>
        /// </summary>
        /// <param name="rotation"></param>
        public static Quaternion Inverse(Quaternion rotation)
        {
            float lengthSq = rotation.SqrMagnitude;
            if (lengthSq != 0.0)
            {
                float i = 1.0f / lengthSq;
                return new Quaternion(rotation.XYZ * -i, rotation.W * i);
            }
            return rotation;
        }

        public static Quaternion Lerp(Quaternion a, Quaternion b, float t)
        {
            if (t > 1) t = 1;
            if (t < 0) t = 0;
            return INTERNAL_CALL_Slerp(ref a, ref b, t); // TODO: use lerp not slerp, "Because quaternion works in 4D. Rotation in 4D are linear" ???
        }

        /// <summary>
        ///   <para>Interpolates between /a/ and /b/ by /t/ and normalizes the result afterwards. The parameter /t/ is not clamped.</para>
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="t"></param>
        public static Quaternion LerpUnclamped(Quaternion a, Quaternion b, float t)
        {
            return INTERNAL_CALL_Slerp(ref a, ref b, t);
        }

        public static Quaternion SlerpUnclamped(Quaternion a, Quaternion b, float t)
        {
            return Quaternion.INTERNAL_CALL_SlerpUnclamped(ref a, ref b, t);
        }

        public static Quaternion Slerp(Quaternion a, Quaternion b, float t)
        {
            return Quaternion.INTERNAL_CALL_Slerp(ref a, ref b, t);
        }

        private static Quaternion INTERNAL_CALL_Slerp(ref Quaternion a, ref Quaternion b, float t)
        {
            if (t > 1) t = 1;
            if (t < 0) t = 0;
            return INTERNAL_CALL_SlerpUnclamped(ref a, ref b, t);
        }

        private static Quaternion INTERNAL_CALL_SlerpUnclamped(ref Quaternion a, ref Quaternion b, float t)
        {
            // if either input is zero, return the other.
            if (a.SqrMagnitude == 0.0f)
            {
                if (b.SqrMagnitude == 0.0f)
                {
                    return Identity;
                }
                return b;
            }
            else if (b.SqrMagnitude == 0.0f)
            {
                return a;
            }


            float cosHalfAngle = a.W * b.W + Vector3.Dot(a.XYZ, b.XYZ);

            if (cosHalfAngle >= 1.0f || cosHalfAngle <= -1.0f)
            {
                // angle = 0.0f, so just return one input.
                return a;
            }
            else if (cosHalfAngle < 0.0f)
            {
                b.XYZ = -b.XYZ;
                b.W = -b.W;
                cosHalfAngle = -cosHalfAngle;
            }

            float blendA;
            float blendB;
            if (cosHalfAngle < 0.99f)
            {
                // do proper slerp for big angles
                float halfAngle = Mathf.Acos(cosHalfAngle);
                float sinHalfAngle = Mathf.Sin(halfAngle);
                float oneOverSinHalfAngle = 1.0f / sinHalfAngle;
                blendA = Mathf.Sin(halfAngle * (1.0f - t)) * oneOverSinHalfAngle;
                blendB = Mathf.Sin(halfAngle * t) * oneOverSinHalfAngle;
            }
            else
            {
                // do lerp if angle is really small.
                blendA = 1.0f - t;
                blendB = t;
            }

            Quaternion result = new Quaternion(blendA * a.XYZ + blendB * b.XYZ, blendA * a.W + blendB * b.W);
            if (result.SqrMagnitude > 0.0f)
                return Normalize(result);
            else
                return Identity;
        }

        public static Vector3 RotateVector3(Quaternion q1, Vector3 v2)
        {
            Quaternion QuatVect = new Quaternion(v2.X, v2.Y, v2.Z, 0);
            Quaternion QuatNorm = q1.Normalize();
            Quaternion Result = Hamilton(Hamilton(QuatNorm, QuatVect), QuatNorm.Conjugate());
            return new Vector3(Result.X, Result.Y, Result.Z);
        }

        public Vector3 RotateVector3(Vector3 v1)
        {
            return RotateVector3(this, v1);
        }

        public static Vector3 VectorRepresentation(Quaternion q1)
        {
            return new Vector3(q1.X, q1.Y, q1.Z);
        }

        public Vector3 VectorRepresentation()
        {
            return VectorRepresentation(this);
        }

        public override string ToString()
        {
            return $"X: {X} | Y: {Y} | Z: {Z} | W: {W}";
        }

        public static Quaternion operator *(Quaternion lhs, Quaternion rhs)
        {
            return new Quaternion(lhs.W * rhs.X + lhs.X * rhs.W + lhs.Y * rhs.Z - lhs.Z * rhs.Y, lhs.W * rhs.Y + lhs.Y * rhs.W + lhs.Z * rhs.X - lhs.X * rhs.Z, lhs.W * rhs.Z + lhs.Z * rhs.W + lhs.X * rhs.Y - lhs.Y * rhs.X, lhs.W * rhs.W - lhs.X * rhs.X - lhs.Y * rhs.Y - lhs.Z * rhs.Z);
        }

        public static Vector3 operator *(Quaternion rotation, Vector3 point)
        {
            float num = rotation.X * 2f;
            float num2 = rotation.Y * 2f;
            float num3 = rotation.Z * 2f;
            float num4 = rotation.X * num;
            float num5 = rotation.Y * num2;
            float num6 = rotation.Z * num3;
            float num7 = rotation.X * num2;
            float num8 = rotation.X * num3;
            float num9 = rotation.Y * num3;
            float num10 = rotation.W * num;
            float num11 = rotation.W * num2;
            float num12 = rotation.W * num3;
            return new Vector3
            {
                X = (1f - (num5 + num6)) * point.X + (num7 - num12) * point.Y + (num8 + num11) * point.Z,
                Y = (num7 + num12) * point.X + (1f - (num4 + num6)) * point.Y + (num9 - num10) * point.Z,
                Z = (num8 - num11) * point.X + (num9 + num10) * point.Y + (1f - (num4 + num5)) * point.Z
            };
        }

        #endregion
    }
}