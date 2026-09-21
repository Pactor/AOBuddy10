using AOSharp.Common.Unmanaged.Imports;
using System;
using System.Linq;
using System.Text;

namespace AOSharp.Common.Helpers
{
    public static class Utils
    {
        public static unsafe string UnsafePointerToString(IntPtr pointer)
        {
            if (pointer == IntPtr.Zero)
                return string.Empty;

            byte* pStr = (byte*)pointer.ToPointer();

            int cLen = 0;
            while (pStr[cLen] != 0)
                cLen++;

            char[] buffer = new char[cLen];

            fixed (char* pBuffer = buffer)
            {
                Encoding.ASCII.GetChars(pStr, cLen, pBuffer, cLen);
            }

            return new string(buffer);
        }

        public static unsafe bool Compare(byte* pData, byte[] pattern, bool[] mask)
        {
            for (int i = 0; i < pattern.Length; pData++, i++)
                if (mask[i] && *pData != pattern[i])
                    return false;

            return true;
        }

        public static unsafe IntPtr FindPattern(string module, string pattern)
        {
            IntPtr moduleHandle = Kernel32.GetModuleHandle(module);

            if (moduleHandle == IntPtr.Zero)
                return IntPtr.Zero;

            Psapi.MODULEINFO moduleInfo;
            if (!Psapi.GetModuleInformation(Kernel32.GetCurrentProcess(), moduleHandle, out moduleInfo, sizeof(Psapi.MODULEINFO)))
                return IntPtr.Zero;

            uint moduleStart = (uint)moduleInfo.lpBaseOfDll;
            uint moduleLen = moduleInfo.SizeOfImage;

            string[] patternStrParts = pattern.Split(' ');
            bool[] mask = patternStrParts.Select(x => x != "?").ToArray();
            byte[] patternBytes = patternStrParts.Select(x => (byte)((x != "?") ? (byte)(Convert.ToInt32(x, 16)) : 0)).ToArray();

            for (uint i = 0; i < moduleLen - patternBytes.Length; i++)
                if (Compare((byte*)(moduleStart + i), patternBytes, mask))
                    return new IntPtr(moduleStart + i);

            return IntPtr.Zero;
        }

        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }
    }
}
