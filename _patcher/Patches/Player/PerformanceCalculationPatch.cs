using System.Threading;
using _patcher.Play;

namespace _patcher.Patches.Player
{
    /// <summary>
    /// PerformanceCalculationPatch class.
    /// </summary>
    internal class PerformanceCalculationPatch
    {
        private static Performance _performance;
        private static int _ppQueued = 0;

        public static void SetPerformance(Performance performance)
        {
            _performance = performance;
        }

        public static void QueueCalculation(object score, float accuracy, int totalHits, int maxCombo, int playMode)
        {
            if (_performance == null)
                return;

            if (Interlocked.Exchange(ref _ppQueued, 1) == 0)
            {
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    try
                    {
                        _performance.UpdatePerformance(score, accuracy, totalHits, maxCombo, playMode);
                    }
                    finally
                    {
                        Interlocked.Exchange(ref _ppQueued, 0);
                    }
                });
            }
        }
    }
}
