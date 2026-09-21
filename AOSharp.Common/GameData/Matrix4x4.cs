using System.Globalization;
using System.Runtime.CompilerServices;
using System;

namespace AOSharp.Common.GameData
{
    public struct Matrix4x4
    {
        public float m00;
        public float m10;
        public float m20;
        public float m30;
        public float m01;
        public float m11;
        public float m21;
        public float m31;
        public float m02;
        public float m12;
        public float m22;
        public float m32;
        public float m03;
        public float m13;
        public float m23;
        public float m33;

        public Matrix4x4(Vector4 column0, Vector4 column1, Vector4 column2, Vector4 column3)
        {
            this.m00 = column0.X; this.m01 = column1.X; this.m02 = column2.X; this.m03 = column3.X;
            this.m10 = column0.Y; this.m11 = column1.Y; this.m12 = column2.Y; this.m13 = column3.Y;
            this.m20 = column0.Z; this.m21 = column1.Z; this.m22 = column2.Z; this.m23 = column3.Z;
            this.m30 = column0.W; this.m31 = column1.W; this.m32 = column2.W; this.m33 = column3.W;
        }

        public float this[int row, int column]
        {
            get
            {
                return this[row + column * 4];
            }

            set
            {
                this[row + column * 4] = value;
            }
        }

        public float this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return m00;
                    case 1: return m10;
                    case 2: return m20;
                    case 3: return m30;
                    case 4: return m01;
                    case 5: return m11;
                    case 6: return m21;
                    case 7: return m31;
                    case 8: return m02;
                    case 9: return m12;
                    case 10: return m22;
                    case 11: return m32;
                    case 12: return m03;
                    case 13: return m13;
                    case 14: return m23;
                    case 15: return m33;
                    default:
                        throw new IndexOutOfRangeException("Invalid matrix index!");
                }
            }

            set
            {
                switch (index)
                {
                    case 0: m00 = value; break;
                    case 1: m10 = value; break;
                    case 2: m20 = value; break;
                    case 3: m30 = value; break;
                    case 4: m01 = value; break;
                    case 5: m11 = value; break;
                    case 6: m21 = value; break;
                    case 7: m31 = value; break;
                    case 8: m02 = value; break;
                    case 9: m12 = value; break;
                    case 10: m22 = value; break;
                    case 11: m32 = value; break;
                    case 12: m03 = value; break;
                    case 13: m13 = value; break;
                    case 14: m23 = value; break;
                    case 15: m33 = value; break;

                    default:
                        throw new IndexOutOfRangeException("Invalid matrix index!");
                }
            }
        }

        public Quaternion ToQuaterion()
        {
            Quaternion q = new Quaternion();
            float trace = m00 + m11 + m22;
            if (trace > 0)
            {
                float s = 0.5f / (float)Math.Sqrt(trace + 1.0f);
                q.W = 0.25f / s;
                q.X = (m21 - m12) * s;
                q.Y = (m02 - m20) * s;
                q.Z = (m10 - m01) * s;
            }
            else
            {
                if (m00 > m11 && m00 > m22)
                {
                    float s = 2.0f * (float)Math.Sqrt(1.0f + m00 - m11 - m22);
                    q.W = (m21 - m12) / s;
                    q.X = 0.25f * s;
                    q.Y = (m01 + m10) / s;
                    q.Z = (m02 + m20) / s;
                }
                else if (m11 > m22)
                {
                    float s = 2.0f * (float)Math.Sqrt(1.0f + m11 - m00 - m22);
                    q.W = (m02 - m20) / s;
                    q.X = (m01 + m10) / s;
                    q.Y = 0.25f * s;
                    q.Z = (m12 + m21) / s;
                }
                else
                {
                    float s = 2.0f * (float)Math.Sqrt(1.0f + m22 - m00 - m11);
                    q.W = (m10 - m01) / s;
                    q.X = (m02 + m20) / s;
                    q.Y = (m12 + m21) / s;
                    q.Z = 0.25f * s;
                }
            }
            return q;
        }

        public Vector4 GetColumn(int index)
        {
            switch (index)
            {
                case 0: return new Vector4(m00, m10, m20, m30);
                case 1: return new Vector4(m01, m11, m21, m31);
                case 2: return new Vector4(m02, m12, m22, m32);
                case 3: return new Vector4(m03, m13, m23, m33);
                default:
                    throw new IndexOutOfRangeException("Invalid column index!");
            }
        }

        public Vector3 MultiplyPoint3x4(Vector3 point)
        {
            return new Vector3
            {
                X = this.m00 * point.X + this.m01 * point.Y + this.m02 * point.Z + this.m03,
                Y = this.m10 * point.X + this.m11 * point.Y + this.m12 * point.Z + this.m13,
                Z = this.m20 * point.X + this.m21 * point.Y + this.m22 * point.Z + this.m23,
            };
        }

        public Vector3 MultiplyPoint(Vector3 point)
        {
            Vector3 res = Vector3.Zero;
            float w;
            res.X = this.m00 * point.X + this.m01 * point.Y + this.m02 * point.Z + this.m03;
            res.Y = this.m10 * point.X + this.m11 * point.Y + this.m12 * point.Z + this.m13;
            res.Z = this.m20 * point.X + this.m21 * point.Y + this.m22 * point.Z + this.m23;
            w = this.m30 * point.X + this.m31 * point.Y + this.m32 * point.Z + this.m33;

            w = 1F / w;
            res.X *= w;
            res.Y *= w;
            res.Z *= w;
            return res;
        }

        public static Matrix4x4 Scale(Vector3 vector)
        {
            Matrix4x4 m;
            m.m00 = vector.X; m.m01 = 0F; m.m02 = 0F; m.m03 = 0F;
            m.m10 = 0F; m.m11 = vector.Y; m.m12 = 0F; m.m13 = 0F;
            m.m20 = 0F; m.m21 = 0F; m.m22 = vector.Z; m.m23 = 0F;
            m.m30 = 0F; m.m31 = 0F; m.m32 = 0F; m.m33 = 1F;
            return m;
        }

        public static Matrix4x4 Translate(Vector3 vector)
        {
            Matrix4x4 m;
            m.m00 = 1F; m.m01 = 0F; m.m02 = 0F; m.m03 = vector.X;
            m.m10 = 0F; m.m11 = 1F; m.m12 = 0F; m.m13 = vector.Y;
            m.m20 = 0F; m.m21 = 0F; m.m22 = 1F; m.m23 = vector.Z;
            m.m30 = 0F; m.m31 = 0F; m.m32 = 0F; m.m33 = 1F;
            return m;
        }

        public static Matrix4x4 Rotate(Quaternion q)
        {
            float x = q.X * 2.0F;
            float y = q.Y * 2.0F;
            float z = q.Z * 2.0F;
            float xx = q.X * x;
            float yy = q.Y * y;
            float zz = q.Z * z;
            float xy = q.X * y;
            float xz = q.X * z;
            float yz = q.Y * z;
            float wx = q.W * x;
            float wy = q.W * y;
            float wz = q.W * z;

            // Calculate 3x3 matrix from orthonormal basis
            Matrix4x4 m;
            m.m00 = 1.0f - (yy + zz); m.m10 = xy + wz; m.m20 = xz - wy; m.m30 = 0.0F;
            m.m01 = xy - wz; m.m11 = 1.0f - (xx + zz); m.m21 = yz + wx; m.m31 = 0.0F;
            m.m02 = xz + wy; m.m12 = yz - wx; m.m22 = 1.0f - (xx + yy); m.m32 = 0.0F;
            m.m03 = 0.0F; m.m13 = 0.0F; m.m23 = 0.0F; m.m33 = 1.0F;
            return m;
        }

        public static Matrix4x4 operator *(Matrix4x4 lhs, Matrix4x4 rhs)
        {
            Matrix4x4 res;
            res.m00 = lhs.m00 * rhs.m00 + lhs.m01 * rhs.m10 + lhs.m02 * rhs.m20 + lhs.m03 * rhs.m30;
            res.m01 = lhs.m00 * rhs.m01 + lhs.m01 * rhs.m11 + lhs.m02 * rhs.m21 + lhs.m03 * rhs.m31;
            res.m02 = lhs.m00 * rhs.m02 + lhs.m01 * rhs.m12 + lhs.m02 * rhs.m22 + lhs.m03 * rhs.m32;
            res.m03 = lhs.m00 * rhs.m03 + lhs.m01 * rhs.m13 + lhs.m02 * rhs.m23 + lhs.m03 * rhs.m33;

            res.m10 = lhs.m10 * rhs.m00 + lhs.m11 * rhs.m10 + lhs.m12 * rhs.m20 + lhs.m13 * rhs.m30;
            res.m11 = lhs.m10 * rhs.m01 + lhs.m11 * rhs.m11 + lhs.m12 * rhs.m21 + lhs.m13 * rhs.m31;
            res.m12 = lhs.m10 * rhs.m02 + lhs.m11 * rhs.m12 + lhs.m12 * rhs.m22 + lhs.m13 * rhs.m32;
            res.m13 = lhs.m10 * rhs.m03 + lhs.m11 * rhs.m13 + lhs.m12 * rhs.m23 + lhs.m13 * rhs.m33;

            res.m20 = lhs.m20 * rhs.m00 + lhs.m21 * rhs.m10 + lhs.m22 * rhs.m20 + lhs.m23 * rhs.m30;
            res.m21 = lhs.m20 * rhs.m01 + lhs.m21 * rhs.m11 + lhs.m22 * rhs.m21 + lhs.m23 * rhs.m31;
            res.m22 = lhs.m20 * rhs.m02 + lhs.m21 * rhs.m12 + lhs.m22 * rhs.m22 + lhs.m23 * rhs.m32;
            res.m23 = lhs.m20 * rhs.m03 + lhs.m21 * rhs.m13 + lhs.m22 * rhs.m23 + lhs.m23 * rhs.m33;

            res.m30 = lhs.m30 * rhs.m00 + lhs.m31 * rhs.m10 + lhs.m32 * rhs.m20 + lhs.m33 * rhs.m30;
            res.m31 = lhs.m30 * rhs.m01 + lhs.m31 * rhs.m11 + lhs.m32 * rhs.m21 + lhs.m33 * rhs.m31;
            res.m32 = lhs.m30 * rhs.m02 + lhs.m31 * rhs.m12 + lhs.m32 * rhs.m22 + lhs.m33 * rhs.m32;
            res.m33 = lhs.m30 * rhs.m03 + lhs.m31 * rhs.m13 + lhs.m32 * rhs.m23 + lhs.m33 * rhs.m33;

            return res;
        }

        public override string ToString()
        {
            return ToString(null, null);
        }

        public string ToString(string format)
        {
            return ToString(format, null);
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            if (string.IsNullOrEmpty(format))
                format = "F5";
            if (formatProvider == null)
                formatProvider = CultureInfo.InvariantCulture.NumberFormat;
            return String.Format("{0}\t{1}\t{2}\t{3}\n{4}\t{5}\t{6}\t{7}\n{8}\t{9}\t{10}\t{11}\n{12}\t{13}\t{14}\t{15}\n",
                m00.ToString(format, formatProvider), m01.ToString(format, formatProvider), m02.ToString(format, formatProvider), m03.ToString(format, formatProvider),
                m10.ToString(format, formatProvider), m11.ToString(format, formatProvider), m12.ToString(format, formatProvider), m13.ToString(format, formatProvider),
                m20.ToString(format, formatProvider), m21.ToString(format, formatProvider), m22.ToString(format, formatProvider), m23.ToString(format, formatProvider),
                m30.ToString(format, formatProvider), m31.ToString(format, formatProvider), m32.ToString(format, formatProvider), m33.ToString(format, formatProvider));
        }
    }
}
