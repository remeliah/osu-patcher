using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using _patcher.Constants;
using _patcher.Helpers;

namespace _patcher.Patches.Relax
{
    /// <summary>
    /// patch relax miss
    /// </summary>
    [HarmonyPatch]
    internal class PatchRelaxMiss : BasePatch
    {
        [HarmonyTargetMethod]
        private static MethodBase Target() => TargetMethodBySignature(Patterns.PatchRelaxMiss_Target);

        [HarmonyTranspiler]
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
