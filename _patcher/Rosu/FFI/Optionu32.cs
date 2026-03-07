using System;
using System.Runtime.InteropServices;

namespace _patcher.Rosu.FFI
{
    /// <summary>
    /// Optionu32 struct.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public partial struct Optionu32
    {
        ///Element that is maybe valid.
        uint t;
        ///Byte where `1` means element `t` is valid.
        byte is_some;
    }
    /// <summary>
    /// Optionu32 struct.
    /// </summary>

    public partial struct Optionu32
    {
        public static Optionu32 FromNullable(uint? nullable)
        {
            var result = new Optionu32();
            if (nullable.HasValue)
            {
                result.is_some = 1;
                result.t = nullable.Value;
            }

            return result;
        }

        public uint? ToNullable()
        {
            return this.is_some == 1 ? this.t : (uint?)null;
        }
    }
}
