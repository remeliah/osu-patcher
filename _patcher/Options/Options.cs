using System;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using _patcher.Utils;
using _patcher.Helpers;

namespace _patcher.Options
{
    [UsedImplicitly]
    internal class Options
    {
        public static readonly Config Config = Config._load();
        public static void InitializeOptions(object instance)
        {
            CheckBox alwaysShowMisses = new CheckBox("Patch Relax/Autopilot",
                "Removes relax/autopilot limitation, Allows you to see miss, Combo break sound and ranking panel.",
                Config.PatchRelax,
                Config.TogglePatchRelax);

            CheckBox transitionTime = new CheckBox("Faster Transition time",
                "Control how fast the screen fades in and out. Turn this on for quicker transitions.",
                Config.TransitionTime,
                Config.ToggleTransitionTime);
            
            Array optionsChildren = Element.CreateArray(
                alwaysShowMisses,
                transitionTime);

            Section section = new Section("Patches");
            section.SetChildren(optionsChildren);
            
            Array sectionChildren = Element.CreateArray(section);
            
            Category category = new Category(FontAwesome.Moon);
            category.SetChildren(sectionChildren);

            // add to elements
            Add(instance, category);
        }

        // private static void ReloadElements(object instance, bool jumpToTop = true)
        //    => BaseReloadElements.Invoke(instance, new object[] { jumpToTop });

        private static void Add(object instance, Element element)
            => BaseAddElement.Invoke(instance, new[] { element.V });

        #region optsign
        /*
        private static readonly MethodBase BaseReloadElements = ILPatch.FindMethodBySignature(new[]
        {
            OpCodes.Ldarg_0,
            OpCodes.Ldfld,
            OpCodes.Brtrue_S,
            OpCodes.Ret,
            OpCodes.Ldarg_0,
            OpCodes.Ldfld,
            OpCodes.Ldsfld,
            OpCodes.Dup,
            OpCodes.Brtrue_S,
            OpCodes.Pop,
            OpCodes.Ldsfld,
            OpCodes.Ldftn,
            OpCodes.Newobj,
            OpCodes.Dup,
            OpCodes.Stsfld,
            OpCodes.Callvirt,
            OpCodes.Ldarg_0,
            OpCodes.Ldfld,
            OpCodes.Callvirt,
            OpCodes.Ldarg_0,
            OpCodes.Ldfld,
            OpCodes.Callvirt,
            OpCodes.Ldarg_0,
            OpCodes.Ldfld,
            OpCodes.Ldc_I4_1,
            OpCodes.Callvirt,
            OpCodes.Ldarg_0,
            OpCodes.Call,
            OpCodes.Ldarg_0,
            OpCodes.Call,
            OpCodes.Ret
        }); 
        */

        private static readonly MethodBase BaseAddElement = ILPatch.FindMethodBySignature(new[]
        {
            OpCodes.Ldarg_0,
            OpCodes.Ldfld,
            OpCodes.Ldarg_1,
            OpCodes.Callvirt,
            OpCodes.Ldarg_1,
            OpCodes.Callvirt,
            OpCodes.Brfalse_S,
            OpCodes.Ldarg_1,
            OpCodes.Callvirt,
            OpCodes.Callvirt,
            OpCodes.Stloc_0,
            OpCodes.Br_S,
            OpCodes.Ldloc_0,
            OpCodes.Callvirt,
            OpCodes.Stloc_1,
            OpCodes.Ldloc_1,
            OpCodes.Ldarg_0,
            OpCodes.Ldftn,
            OpCodes.Newobj,
            OpCodes.Callvirt,
            OpCodes.Ldarg_0,
            OpCodes.Ldloc_1,
            OpCodes.Call,
            OpCodes.Ldloc_0,
            OpCodes.Callvirt,
            OpCodes.Brtrue_S,
            OpCodes.Leave_S,
            OpCodes.Ldloc_0,
            OpCodes.Brfalse_S,
            OpCodes.Ldloc_0,
            OpCodes.Callvirt,
            OpCodes.Endfinally,
            OpCodes.Ldarg_1,
            OpCodes.Isinst,
            OpCodes.Stloc_2,
            OpCodes.Ldloc_2,
            OpCodes.Brfalse_S,
            OpCodes.Ldloc_2,
            OpCodes.Ldarg_0,
            OpCodes.Ldftn,
            OpCodes.Newobj,
            OpCodes.Newobj,
            OpCodes.Stloc_3,
            OpCodes.Ldarg_0,
            OpCodes.Ldfld,
            OpCodes.Ldloc_3,
            OpCodes.Callvirt,
            OpCodes.Ldarg_0,
            OpCodes.Ldfld,
            OpCodes.Ldloc_3,
            OpCodes.Ldfld,
            OpCodes.Callvirt,
            OpCodes.Ldarg_0,
            OpCodes.Ldfld,
            OpCodes.Ldarg_1,
            OpCodes.Ldfld,
            OpCodes.Callvirt,
            OpCodes.Ret,
        });
        #endregion
    }

    [HarmonyPatch]
    [UsedImplicitly]
    internal class PatchOptionsMenu
    {
        private static readonly OpCode[] Signature = new OpCode[]
        {
            OpCodes.Ldarg_0,
            OpCodes.Ldfld,
            OpCodes.Ldc_I4_S,
            OpCodes.Call,
            OpCodes.Callvirt,
            OpCodes.Ldarg_0,
            OpCodes.Ldfld,
            OpCodes.Ldc_I4_S,
            OpCodes.Call,
            OpCodes.Callvirt,
        };

        [HarmonyTargetMethod]
        [UsedImplicitly]
        private static MethodBase Target() => ILPatch.FindMethodBySignature(Signature);

        [HarmonyPostfix]
        [UsedImplicitly]
        // @formatter:off
        // ReSharper disable InconsistentNaming
        private static void Postfix(object __instance)
        {
            try
            {
                Options.InitializeOptions(__instance);

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