using _patcher.Performance.FFI;
using _patcher.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using static System.Net.Mime.MediaTypeNames;

namespace _patcher.Performance
{
    public sealed class Calculator
    {
        [DllImport("rosu-pp", CallingConvention = CallingConvention.Cdecl)]
        private static extern CalculateResult calculate_akatsuki_from_bytes(
            Sliceu8 beatmap_bytes,
            uint mode,
            uint mods,
            uint max_combo,
            float accuracy,
            uint miss_count,
            Optionu32 passed_objects);

        private static MethodInfo _getBeatmapStream;
        private readonly object _beatmap;
        private readonly object _mods;

        private byte[] _cachedBeatmap;
        private GCHandle _pinnedHandle;
        private Sliceu8 _beatmapSlice;
        private bool _disposed;

        public Calculator(object beatmap, MethodInfo getBeatmapStream, object mods)
        {
            _beatmap = beatmap;
            _getBeatmapStream = getBeatmapStream;
            _mods = mods;
            InitializeBeatmapData();
        }

        private void InitializeBeatmapData()
        {
            using (Stream stream = (Stream)_getBeatmapStream.Invoke(_beatmap, null))
            {
                if (stream == null) return;

                if (stream.CanSeek)
                {
                    _cachedBeatmap = new byte[stream.Length];
                    stream.Read(_cachedBeatmap, 0, (int)stream.Length);
                }
                else
                {
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        stream.CopyTo(memoryStream);
                        _cachedBeatmap = memoryStream.ToArray();
                    }
                }
            }

            if (_cachedBeatmap != null && _cachedBeatmap.Length != 0)
            {
                _pinnedHandle = GCHandle.Alloc(_cachedBeatmap, GCHandleType.Pinned);
                _beatmapSlice = new Sliceu8(_pinnedHandle.AddrOfPinnedObject(), (ulong)((long)_cachedBeatmap.Length));
            }
        }

        public double CalculateScore(object score, float accuracy, int totalHits, int maxCombo, int playMode)
        {
            if (_disposed)
                throw new ObjectDisposedException("Calculator");

            if (_cachedBeatmap == null || _cachedBeatmap.Length == 0)
                return 0.0;

            var ushortFields = score.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(f => f.FieldType == typeof(ushort)).ToArray();

            ushort countMiss = (ushort)ushortFields[5].GetValue(score);

            Optionu32 passedObjects = Optionu32.FromNullable(new uint?((uint)totalHits));

            CalculateResult result = calculate_akatsuki_from_bytes(
                _beatmapSlice,
                (uint)playMode,
                (uint)Convert.ToInt32(_mods),
                (uint)maxCombo,
                accuracy,
                (uint)Convert.ToInt32(countMiss),
                passedObjects);

            return result.pp;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            if (_pinnedHandle.IsAllocated)
                _pinnedHandle.Free();

            _disposed = true;
            GC.SuppressFinalize(this);
        }

        ~Calculator()
        {
            Dispose();
        }
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct CalculateResult
    {
        public double pp;
        public double stars;
    }
}
