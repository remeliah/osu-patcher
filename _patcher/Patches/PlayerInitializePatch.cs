using _patcher.Constants;
using _patcher.Helpers;
using _patcher.Performance;
using _patcher.Resolver;
using _patcher.Utils;
using HarmonyLib;
using JetBrains.Annotations;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace _patcher.Patches
{
    [HarmonyPatch]
    [UsedImplicitly]
    internal class PlayerInitializePatch
    {
        private static Type _beatmapType;
        private static FieldInfo _beatmapField;
        private static MethodInfo _getBeatmapStreamMethod;

        [HarmonyTargetMethod]
        [UsedImplicitly]
        private static MethodBase Target()
            => ILPatch.FindMethodBySignature(Patterns.PlayerInitialize_Target);

        [HarmonyPostfix]
        [UsedImplicitly]
        private static void Postfix(object __instance)
        {
            try
            {
                EnsureResolved(__instance);

                if (_beatmapField == null || _getBeatmapStreamMethod == null)
                    return;

                var beatmap = _beatmapField.GetValue(__instance);
                if (beatmap == null)
                    return;

                var currentScoreField = Score.GetCurrentScoreField(__instance);
                if (currentScoreField == null)
                    return;
                var currentScore = currentScoreField.GetValue(null);

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

                var calculator = new Calculator(beatmap, _getBeatmapStreamMethod, mods);
                PerformanceCalculationPatch.SetCalculator(calculator);
            }
            catch (Exception ex)
            {
                Logger.log(ex.ToString());
            }
        }

        private static void EnsureResolved(object playerInstance)
        {
            if (_beatmapType != null)
                return;

            ResolveBeatmapType();
            ResolveBeatmapField(playerInstance);
            ResolveGetBeatmapStream();
        }

        private static void ResolveBeatmapType()
        {
            _beatmapType = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch { return Array.Empty<Type>(); }
                })
                .FirstOrDefault(t =>
                {
                    var methods = t.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic);

                    return methods.Any(m =>
                               m.ReturnType == typeof(Stream) &&
                               m.GetParameters().Length == 0)
                           &&
                           methods.Any(m =>
                               m.ReturnType == typeof(Stream) &&
                               m.GetParameters().Length == 1 &&
                               m.GetParameters()[0].ParameterType == typeof(string));
                });
        }

        private static void ResolveBeatmapField(object playerInstance)
        {
            if (_beatmapType == null)
                return;

            _beatmapField = playerInstance
                .GetType()
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .FirstOrDefault(f => _beatmapType.IsAssignableFrom(f.FieldType));
        }

        private static void ResolveGetBeatmapStream()
        {
            if (_beatmapType == null)
                return;

            _getBeatmapStreamMethod = _beatmapType
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                .FirstOrDefault(m =>
                    m.ReturnType == typeof(Stream) &&
                    m.GetParameters().Length == 0);
        }
    }
}
