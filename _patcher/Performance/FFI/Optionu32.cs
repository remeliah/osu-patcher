using System;
using System.Runtime.InteropServices;

namespace _patcher.Performance.FFI
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public partial struct Optionu32
    {
        private uint Value;
        private byte IsSome;
    }

    public partial struct Optionu32
    {
        public static Optionu32 FromNullable(uint? nullable)
        {
            var result = new Optionu32();
            if (!nullable.HasValue)
                return result;

            result.IsSome = 1;
            result.Value = nullable.Value;

            return result;
        }

        public uint? ToNullable()
        {
            return IsSome == 1 ? (uint?)Value : null;
        }
    }
}
