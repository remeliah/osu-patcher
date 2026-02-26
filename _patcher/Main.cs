using System;
using _patcher.Wrappers;
using _patcher.Utils;
using HarmonyLib;
using System.Linq;

namespace _patcher
{
    public class Main
    {
        private static readonly Harmony Harmony = new Harmony("osu_patcher.ano");
        
        public static int Initialize(string st)
        {
            try
            {
#if DEBUG
                // server checks so they don't get banned on bancho
                // NOTE: on debug since this already being handled on https://github.com/refx-online/patcher-cli
                var args = Environment.GetCommandLineArgs();
                if (!args.Contains("-devserver") || string.IsNullOrEmpty(args.SkipWhile(arg => arg != "-devserver")
                    .Skip(1)
                    .FirstOrDefault()) ||
                    args.SkipWhile(arg => arg != "-devserver")
                        .Skip(1)
                        .First()
                        .Contains("ppy.sh"))
                {
                    // can just use Environment.Exit(-1); but this is better
                    GameBase.BeginExit();
                    return 1;
                }
#endif

                // now patchall
                Harmony.PatchAll(typeof(Main).Assembly);

                NotificationManager.ShowMessageMassive("osu! patched!",
                    5000,
                    NotificationManager.NotificationType.Warning
                );
            }
            catch (Exception e)
            {
                Logger.log(e.ToString());
            }

            return 0;
        }
    }
}
