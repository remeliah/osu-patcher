using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using _patcher.Helpers;

namespace _patcher.Patch
{
    [HarmonyPatch]
    [UsedImplicitly]
    public class LocalisationManager
    {
        // LocalisationManager.GetString(OsuString stringType)
        private static readonly OpCode[] Signature =
        {
            OpCodes.Ldsfld,
            OpCodes.Brtrue_S,
            OpCodes.Ldsfld,
            OpCodes.Call,
            OpCodes.Pop,
            OpCodes.Nop,
            OpCodes.Ldsfld,
            OpCodes.Ldarg_0,
            OpCodes.Callvirt,
            OpCodes.Stloc_0,
            OpCodes.Leave_S,
            OpCodes.Pop,
            OpCodes.Ldsfld,
            OpCodes.Stloc_0,
            OpCodes.Leave_S,
            OpCodes.Ldloc_0,
            OpCodes.Ret,
        };

        [HarmonyTargetMethod]
        [UsedImplicitly]
        private static MethodBase Target() => ILPatch.FindMethodBySignature(Signature);

        [HarmonyTranspiler]
        [UsedImplicitly]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            var codes = new List<CodeInstruction>(instructions);
            var skip = il.DefineLabel();
            
            codes[0].labels.Add(skip);
            
            codes.InsertRange(0, new[]
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldc_I4, 0x507), // todo: use constant variable from Category.cs
                new CodeInstruction(OpCodes.Ble_S, skip),
                new CodeInstruction(OpCodes.Ldstr, "Re;FX"),
                new CodeInstruction(OpCodes.Ret)
            });
            
            return codes.AsEnumerable();
        }
    }
}
