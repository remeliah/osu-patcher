using _patcher.Constants;
using _patcher.Helpers;
using _patcher.Graphics;
using _patcher.Graphics.Sprites;
using _patcher.Graphics.Skinning;
using _patcher.Resolver;
using _patcher.Wrappers;
using _patcher.Play;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.IO;

namespace _patcher.Patches
{
    [HarmonyPatch]
    internal class PlayerOnLoadCompletePatch
    {
        [HarmonyTargetMethod]
        private static MethodBase Target() => ILPatch.FindMethodBySignature(Patterns.PlayerOnLoadComplete_Target);

        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            codes.Insert(24, new CodeInstruction(OpCodes.Ldarg_0));
            codes.Insert(25, new CodeInstruction(
                OpCodes.Call,
                typeof(PlayerOnLoadCompletePatch)
                .GetMethod(nameof(OnComplete), BindingFlags.Public | BindingFlags.Static)
            ));

            return codes.AsEnumerable();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void OnComplete(object playerInstance)
        {
            if (!Options.Options.Config.PerformanceCalculator)
                return;

            var ppText = new pSpriteText("0", "score", 0, 
                Fields.TopLeft, Origins.Custom, Clocks.Game, 0, 130, 0.92f, true, Color.White, true, SkinSource.All);
            ppText.TextConstantSpacing = true;
            ppText.Scale = 1.1f;
            ppText.RefreshTexture();

            SpriteManager.AddToWidescreen(playerInstance, ppText.Instance);

            var beatmapField = playerInstance.GetType()
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .FirstOrDefault(f => typeof(Stream).IsAssignableFrom(f.FieldType) ||
                                    f.FieldType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                                    .Any(m => m.ReturnType == typeof(Stream) && m.GetParameters().Length == 0));

            var currentScoreField = Score.GetCurrentScoreField(playerInstance);
            if (currentScoreField == null || beatmapField == null) return;

            var currentScore = currentScoreField.GetValue(null);
            var beatmap = beatmapField.GetValue(playerInstance);

            if (currentScore == null || beatmap == null) return;

            var enabledModsField = currentScore.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
               .FirstOrDefault(f => f.FieldType.IsGenericType);

            var enabledMods = enabledModsField.GetValue(currentScore);
            var obfuscatedType = enabledMods.GetType();
            var generic = obfuscatedType.GetGenericArguments()[0];

            var modsValue = obfuscatedType
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(m =>
                    m.ReturnType == generic &&
                    m.GetParameters().Length == 0);

            if (modsValue == null)
                return;

            int mods = (int)modsValue.Invoke(enabledMods, null);

            var getBeatmapStreamMethod = beatmap.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                .Where(m => m.ReturnType == typeof(Stream) && m.GetParameters().Length == 0)
                .Skip(1) // first one is for audio.
                .FirstOrDefault();

            var performance = new Performance(beatmap, getBeatmapStreamMethod, (uint)mods, new[] { ppText });
            PerformanceCalculationPatch.SetPerformance(performance);
        }
    }
}
