using _patcher.Performance;
using _patcher.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace _patcher.Patches
{
    internal class PerformanceCalculationPatch
    {
        private static Calculator _calculator;
        private static int _ppQueued = 0;
        private static double _currentPP = 0.0;

        public static void SetCalculator(Calculator calculator)
        {
            _calculator = calculator;
            _currentPP = 0.0;
        }

        public static void QueueCalculation(object score, float accuracy, int totalHits, int maxCombo, int playMode)
        {
            if (_calculator == null)
                return;

            if (Interlocked.Exchange(ref _ppQueued, 1) == 0)
            {
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    try
                    {
                        double pp = _calculator.CalculateScore(score, accuracy, totalHits, maxCombo, playMode);
                        _currentPP = Math.Round(pp, 2);
                        Logger.log($"[PP] Current PP: {_currentPP}");

                        // todo: update ui
                    }
                    catch (Exception ex)
                    {
                        Logger.log($"[PP] Error: {ex}");
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
