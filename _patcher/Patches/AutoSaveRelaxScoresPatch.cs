using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using JetBrains.Annotations;
using _patcher.Constants;
using _patcher.Helpers;

namespace _patcher.Patches
{
    /// <summary>
    /// Patch to allow saving scores even when Relax mod is active.
    /// </summary>
    [HarmonyPatch]
    [UsedImplicitly]
    internal class PatchAutoSaveRelaxScores
    {
        [HarmonyTargetMethod]
        [UsedImplicitly]
        private static MethodBase Target() => ILPatch.FindMethodBySignature(Patterns.PatchAutoSaveRelaxScores_Target);

        [HarmonyTranspiler]
        [UsedImplicitly]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            codes.InsertRange(33, new[]
            {
                new CodeInstruction(OpCodes.Call, typeof(PatchAutoSaveRelaxScores)
                    .GetMethod(nameof(PatchRelax), BindingFlags.Public | BindingFlags.Static)),
                new CodeInstruction(OpCodes.And)
            });

            codes.InsertRange(21, new[]
            {
                new CodeInstruction(OpCodes.Call, typeof(PatchAutoSaveRelaxScores)
                    .GetMethod(nameof(PatchRelax), BindingFlags.Public | BindingFlags.Static)),
                new CodeInstruction(OpCodes.And)
            });

            return codes.AsEnumerable();
        }

        /// <summary>
        /// Checks if Relax patch is enabled.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool PatchRelax() => !Options.Options.Config.PatchRelax;
    }
}
