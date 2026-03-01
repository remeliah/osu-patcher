using System;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using _patcher.Constants;
using _patcher.Helpers;
using _patcher.Utils;

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

        private static readonly MethodBase BaseAddElement = ILPatch.FindMethodBySignature(Patterns.Options_AddElement);
    }

}
