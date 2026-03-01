using System;
using System.Linq;
using HarmonyLib;
using _patcher.Utils;
using _patcher.Wrappers;

namespace _patcher
{
    public class Main
    {
        private static readonly Harmony Harmony = new Harmony("osu_patcher.ano");
        
        public static int Initialize(string st)
        {
            try
            {
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
