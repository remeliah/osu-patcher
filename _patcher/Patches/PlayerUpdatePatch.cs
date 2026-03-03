using _patcher.Constants;
using _patcher.Helpers;
using _patcher.Resolver;
using _patcher.Utils;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace _patcher.Patches
{
    [HarmonyPatch]
    internal class PlayerUpdatePatch
    {
        private static FieldInfo _currentScore;
        private static MethodInfo _getAccuracy;
        private static MethodInfo _getTotalHits;
        private static FieldInfo _maxCombo;
        private static FieldInfo _playMode;


        [HarmonyTargetMethod]
        private static MethodBase Target()
            => ILPatch.FindMethodBySignature(Patterns.PlayerUpdate_Target);

        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            var codes = new List<CodeInstruction>(instructions);

            var br = codes[775];

            // handle brfalse to the end of the if. we want to insert our code before that
            if (br.operand is Label oldLabel)
            {
                var targetIndex = codes.FindIndex(c => c.labels.Contains(oldLabel));
                if (targetIndex != -1 && targetIndex + 2 < codes.Count)
                {
                    var newLabel = il.DefineLabel();
                    codes[targetIndex + 1].labels.Add(newLabel);

                    br.operand = newLabel;
                }
            }

            codes.Insert(776, new CodeInstruction(OpCodes.Ldarg_0));
            codes.Insert(777, new CodeInstruction(
                OpCodes.Call,
                typeof(PlayerUpdatePatch)
                .GetMethod(nameof(OnUpdate), BindingFlags.Public | BindingFlags.Static)
            ));

            return codes;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void OnUpdate(object playerInstance)
        {
            if (!Options.Options.Config.PerformanceCalculator)
                return;

            EnsureScoreFieldResolved(playerInstance);

            if (_currentScore == null)
                return;

            var score = _currentScore.GetValue(null);
            if (score == null)
                return;

            var scoreType = score.GetType();

            if (_getAccuracy == null)
                _getAccuracy = scoreType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .FirstOrDefault(m =>
                        m.ReturnType == typeof(float) &&
                        m.GetParameters().Length == 0 &&
                        m.IsVirtual);

            if (_getAccuracy == null)
                return;

            // todo: is total hits really needed? maybe we can just calculate it from count300 + count100 + count50 + countMiss
            if (_getTotalHits == null)
                _getTotalHits = scoreType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .FirstOrDefault(m =>
                        m.ReturnType == typeof(int) &&
                        m.GetParameters().Length == 0 &&
                        m.IsVirtual);
            
            if (_getTotalHits == null)
                return;

            if (_maxCombo == null)
                _maxCombo = scoreType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .FirstOrDefault(f => f.FieldType == typeof(int));
            if (_maxCombo == null)
                return;

            if (_playMode == null)
                _playMode = scoreType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .FirstOrDefault(f => f.FieldType.IsEnum);
            if (_playMode == null)
                return;

            float accuracy = (float)_getAccuracy.Invoke(score, null) * 100f;
            int totalHits = (int)_getTotalHits.Invoke(score, null);
            int maxCombo = (int)_maxCombo.GetValue(score);
            int playMode = (int)_playMode.GetValue(score);

            PerformanceCalculationPatch.QueueCalculation(score, accuracy, totalHits, maxCombo, playMode);
        }

        private static void EnsureScoreFieldResolved(object playerInstance)
        {
            if (_currentScore != null)
                return;

            _currentScore = Score.GetCurrentScoreField(playerInstance);
        }
    }
}
