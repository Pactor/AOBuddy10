using System;
using System.Text;
using System.Net;
using System.Numerics;
using System.Globalization;

namespace AOSharp.Clientless.Common
{
    public static class LoginEncryption
    {
        private const string Seed1 =
           "eca2e8c85d863dcdc26a429a71a9815ad052f6139669dd659f98ae159d313d13c6bf2838e10a69b6478b64a24bd054ba8248e8fa778703b418408249440b2c1edd28853e240d8a7e49540b76d120d3b1ad2878b1b99490eb4a2a5e84caa8a91cecbdb1aa7c816e8be343246f80c637abc653b893fd91686cf8d32d6cfe5f2a6f";

        private const string Seed2 =
            "9c32cc23d559ca90fc31be72df817d0e124769e809f936bc14360ff4bed758f260a0d596584eacbbc2b88bdd410416163e11dbf62173393fbc0c6fefb2d855f1a03dec8e9f105bbad91b3437d8eb73fe2f44159597aa4053cf788d2f9d7012fb8d7c4ce3876f7d6cd5d0c31754f4cd96166708641958de54a6def5657b9f2e92";

        private const int Seed3 = 5;

        private static Random _random = new Random();

        public static string sub_1001220D(BigInteger p1, BigInteger p2, BigInteger p3)
        {
            var v = BigInteger.ModPow(p2, p3, p1);
            return v.ToString("X").TrimStart('0');
        }

        public static string MakeRandomSeed(int size)
        {
            var buffer = new byte[size / 8];
            for (var i = 0; i < size / 8; i++)
            {
                buffer[i] = (byte)_random.Next();
            }

            return GetString(buffer);
        }

        public static string sub_1001232B(BigInteger p1, int p2, BigInteger p3)
        {
            var v = BigInteger.ModPow(new BigInteger(p2), p3, p1);
            return v.ToString("X").TrimStart('0');
        }

        public static string MakeChallengeResponse(Credentials credentials, byte[] salt)
        {
            var v3 = MakeRandomSeed(128);

            var boff_100318DC = BigInteger.Parse("00" + Seed1, NumberStyles.HexNumber);
            var boff_100318D8 = BigInteger.Parse("00" + Seed2, NumberStyles.HexNumber);
            var bsub_100123C9 = BigInteger.Parse("00" + v3, NumberStyles.HexNumber);

            var v10 = sub_1001220D(boff_100318DC, boff_100318D8, bsub_100123C9);
            var v12 = sub_1001232B(boff_100318DC, Seed3, bsub_100123C9);
            var v6 = string.Format("{0}|{1}|{2}", credentials.Username, Encoding.ASCII.GetString(salt), credentials.Password);
            var v11 = sub_1001254C(v10, v6);

            var v9 = string.Format("{0}-{1}", v12, v11);

            return v9.ToLower();
        }

        private static byte[] GetBytes(string str, int? len = null)
        {
            var count = len ?? str.Length;
            var buffer = new byte[count / 2];
            for (var i = 0; i < buffer.Length; i++)
            {
                var sub = str.Substring(i * 2, 2);
                buffer[i] = byte.Parse(sub, NumberStyles.HexNumber);
            }

            return buffer;
        }

        private static string GetString(byte[] bytes, int? len = null)
        {
            var sb = new StringBuilder();
            var count = len ?? bytes.Length;
            for (var index = 0; index < count; index++)
            {
                var b = bytes[index];
                sb.Append(b.ToString("X2"));
            }

            return sb.ToString();
        }

        private static string sub_1001254C(string a1, string a2)
        {
            if (a1.Length < 0x20)
            {
                return null;
            }

            var v21 = new byte[8];
            for (var i = 0; i < 8; i++)
            {
                v21[i] = (byte)_random.Next();
            }

            var v17 = GetBytes(a1, 32);
            var v6 = a2.Length;
            var v8 = 8 - ((v6 - 1 + 12) % 8) + v6 - 1 + 12;
            var v9 = new byte[v8];
            for (var index = 0; index < v9.Length; index++)
            {
                v9[index] = 32;
            }

            var v10 = IPAddress.HostToNetworkOrder(a2.Length);
            var v10Bytes = BitConverter.GetBytes(v10);
            var a2Bytes = Encoding.ASCII.GetBytes(a2);
            Array.Copy(a2Bytes, 0, v9, 12, a2Bytes.Length);
            Array.Copy(v21, 0, v9, 0, v21.Length);
            Array.Copy(v10Bytes, 0, v9, 8, v10Bytes.Length);

            var v22 = 0;
            uint v19 = 0;
            if (v8 > 0)
            {
                var v12 = 0; // v9 index
                uint v18 = 0;
                do
                {
                    if (v18 != 0)
                    {
                        var tmp = BitConverter.ToUInt32(v9, v12);
                        tmp ^= v18;
                        var tmpBytes = BitConverter.GetBytes(tmp);
                        Array.Copy(tmpBytes, 0, v9, v12, tmpBytes.Length);

                        var tmp2 = BitConverter.ToUInt32(v9, v22 + 4);
                        tmp2 ^= v19;
                        var tmp2Bytes = BitConverter.GetBytes(tmp2);
                        Array.Copy(tmp2Bytes, 0, v9, v22 + 4, tmp2Bytes.Length);
                    }

                    sub_100126C3(ref v12, v9, v17);
                    var v13 = BitConverter.ToUInt32(v9, v12);
                    v22 += 8;
                    v18 = v13;
                    var v14 = BitConverter.ToUInt32(v9, v12 + 4);
                    v12 += 8;
                    v19 = v14;
                }
                while (v22 < v8);
            }

            var v15 = GetString(v9);

            return v15;
        }

        private static int sub_100126C3(ref int a1i, byte[] a1, byte[] a2)
        {
            var result = 0;
            var v4 = a1i;
            var ecx = BitConverter.ToUInt32(a1, a1i);
            var edx = BitConverter.ToUInt32(a1, a1i + 4);
            var a2_4 = BitConverter.ToUInt32(a2, 4);
            var a2_0 = BitConverter.ToUInt32(a2, 0);
            var a2_12 = BitConverter.ToUInt32(a2, 12);
            var a2_8 = BitConverter.ToUInt32(a2, 8);
            uint arg_0 = 0;
            var var_4 = 32;

            do
            {
                arg_0 -= 1640531527;
                --var_4;

                var eax = edx;
                eax = eax << 4;
                eax = eax + a2_0;
                var edi = edx;
                edi = edi >> 5;
                edi = edi + a2_4;
                eax = eax ^ edi;
                edi = arg_0;
                edi = edi + edx;
                eax = eax ^ edi;
                ecx = ecx + eax;

                eax = ecx;
                eax = eax << 4;
                eax = eax + a2_8;
                edi = ecx;
                edi = edi >> 5;
                edi = edi + a2_12;
                eax = eax ^ edi;
                edi = arg_0;
                edi = edi + ecx;
                eax = eax ^ edi;
                edx = edx + eax;
            }
            while (var_4 > 0);

            var v3_bytes = BitConverter.GetBytes(ecx);
            Array.Copy(v3_bytes, 0, a1, v4, v3_bytes.Length);
            var v3_4_bytes = BitConverter.GetBytes(edx);
            Array.Copy(v3_4_bytes, 0, a1, v4 + 4, v3_4_bytes.Length);
            a1i = v4;

            return result;
        }
    }
}