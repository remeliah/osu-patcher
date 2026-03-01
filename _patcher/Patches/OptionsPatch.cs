using System;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using _patcher.Constants;
using _patcher.Helpers;
using _patcher.Utils;

namespace _patcher.Patches
{
    /// <summary>
    /// Patches the options menu to include custom patcher options.
    /// </summary>
    [HarmonyPatch]
    [UsedImplicitly]
    internal class OptionsPatch
    {
        [HarmonyTargetMethod]
        [UsedImplicitly]
        private static MethodBase Target() => ILPatch.FindMethodBySignature(Patterns.PatchOptionsMenu_Target);

        [HarmonyPostfix]
        [UsedImplicitly]
        // @formatter:off
        // ReSharper disable InconsistentNaming
        private static void Postfix(object __instance)
        {
            try
            {
                Options.Options.InitializeOptions(__instance);

                // TODO: fix crashes the game?
                // maybe need to sleep for a few seconds or
                // maybe need to call this on a different thread
                // Options.ReloadElements(true);
            }
            catch (Exception ex)
            {
                Logger.log(ex.ToString());
            }
        }
        // ReSharper restore InconsistentNaming
        // @formatter:on
    }
}
