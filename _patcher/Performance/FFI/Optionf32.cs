using System;
using System.Runtime.InteropServices;

namespace _patcher.Performance.FFI
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public partial struct Optionf64
    {
        ///Element that is maybe valid.
        double t;
        ///Byte where `1` means element `t` is valid.
        byte is_some;
    }

    public partial struct Optionf64
    {
        public static Optionf64 FromNullable(double? nullable)
        {
            var result = new Optionf64();
            if (nullable.HasValue)
            {
                result.is_some = 1;
                result.t = nullable.Value;
            }

            return result;
        }

        public double? ToNullable()
        {
            return this.is_some == 1 ? this.t : (double?)null;
        }
    }

}
