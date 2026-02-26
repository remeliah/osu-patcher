using System;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using _patcher.Utils;
using _patcher.Helpers;
using _patcher.Constants;

namespace _patcher.Options
{
    /// <summary>
    /// Manages the configuration options for the patcher.
    /// </summary>
    [UsedImplicitly]
    internal class Options
    {
        public static readonly Config Config = Config._load();

        /// <summary>
        /// Initializes the options menu with checkboxes and categories.
        /// </summary>
        /// <param name="instance">The instance of the options menu.</param>
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

        private static void Add(object instance, Element element)
            => BaseAddElement.Invoke(instance, new[] { element.V });

        #region optsign

        private static readonly MethodBase BaseAddElement = ILPatch.FindMethodBySignature(Patterns.Options_AddElement);
        #endregion
    }

    /// <summary>
    /// Patches the options menu to include custom patcher options.
    /// </summary>
    [HarmonyPatch]
    [UsedImplicitly]
    internal class PatchOptionsMenu
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
