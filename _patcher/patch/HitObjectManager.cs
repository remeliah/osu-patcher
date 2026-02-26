using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using JetBrains.Annotations;
using _patcher.Helpers;
using _patcher.Constants;

namespace _patcher.Patch
{
    /// <summary>
    /// patch relax miss
    /// </summary>
    [HarmonyPatch]
    [UsedImplicitly]
    internal class PatchRelaxMiss
    {
        [HarmonyTargetMethod]
        [UsedImplicitly]
        private static MethodBase Target() => ILPatch.FindMethodBySignature(Patterns.PatchRelaxMiss_Target);

        [HarmonyTranspiler]
        [UsedImplicitly]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            codes.RemoveAt(664);
            codes.InsertRange(665, new CodeInstruction[]
            {
                new CodeInstruction(OpCodes.Or),
                new CodeInstruction(OpCodes.Call,
                    typeof(PatchRelaxMiss)
                    .GetMethod(nameof(PatchRelax), BindingFlags.Public | BindingFlags.Static)),
                new CodeInstruction(OpCodes.And)
            });

            return codes.AsEnumerable();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool PatchRelax() => !Options.Options.Config.PatchRelax;
    }
}
