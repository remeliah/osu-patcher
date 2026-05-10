using System;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using HoLLy.ManagedInjector;
using Newtonsoft.Json.Linq;

namespace patchershit
{
    internal class Program
    {
        private static readonly string ConfigPath = Path.GetFullPath("conf.db");
        private const string DefaultServer = "refx.online";
        private const string PatcherUrl = "https://updater.refx.online/patcher";
        
        public static void Main(string[] args)
        {
            var headless = args.Length > 0;

            try
            {
                var options = Options.Parse(args);
                var osuPath = GetOsuPath(options.OsuPath);
                var patcherPath = GetPatcherPath(options.PatcherPath);
                var server = string.IsNullOrWhiteSpace(options.Server)
                    ? DefaultServer
                    : options.Server.Trim();

                var osuProc = Process.Start(new ProcessStartInfo
                {
                    FileName = osuPath,
                    Arguments = $"-devserver {server}",
                    UseShellExecute = false
                });
                
                if (osuProc == null)
                    throw new Exception("failed to start osu!");
                
                osuProc.WaitForInputIdle();
                Thread.Sleep(2000);
                
                using (var proc = new InjectableProcess((uint)osuProc.Id))
                    proc.Inject(patcherPath, "_patcher.Main", "Initialize");
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                if (!headless && Environment.UserInteractive)
                    Console.Read();

                Environment.ExitCode = 1;
            }
        }

        private static string GetPatcherPath(string providedPath)
        {
            if (!string.IsNullOrWhiteSpace(providedPath))
            {
                var path = Path.GetFullPath(providedPath.Trim('"'));
                if (!File.Exists(path))
                    throw new FileNotFoundException("_patcher.dll path invalid.", path);

                return path;
            }

            var patcherPath = Path.GetFullPath("_patcher.dll");
            var harmonyPath = Path.GetFullPath("0Harmony.dll"); // just for consistency

            var json = new WebClient().DownloadString(PatcherUrl);
            var data = JObject.Parse(json);

            if (!File.Exists(harmonyPath) || !HashMatches(harmonyPath, (string)((JObject)data["0Harmony.dll"])["hash_md5"]))
                harmonyPath = DownloadPatcher(data, "0Harmony.dll");

            if (!File.Exists(patcherPath) || !HashMatches(patcherPath, (string)((JObject)data["_patcher.dll"])["hash_md5"]))
                patcherPath = DownloadPatcher(data, "_patcher.dll");

            return patcherPath;
        }
        
        /// <summary>
        /// retrieves the stored osu!.exe path from <c>ConfigPath</c> / prompts the user to enter it
        /// </summary>
        /// <returns>
        /// absolute file path to <c>osu!.exe</c>
        /// </returns>
        /// <exception cref="FileNotFoundException">
        /// throws when stored path doesnt point to an existing file
        /// </exception>
        private static string GetOsuPath(string providedPath)
        {
            if (!string.IsNullOrWhiteSpace(providedPath))
            {
                var providedOsuPath = Path.GetFullPath(providedPath.Trim('"'));
                if (!File.Exists(providedOsuPath))
                    throw new FileNotFoundException("osu!.exe path invalid.", providedOsuPath);

                File.WriteAllText(ConfigPath, providedOsuPath);
                return providedOsuPath;
            }

            if (File.Exists(ConfigPath))
            {
                var savedPath = File.ReadAllText(ConfigPath).Trim();
                if (File.Exists(savedPath))
                    return savedPath;

                Console.WriteLine("saved osu! path not found, re-entering...");
            }

            Console.Write("enter full path to osu!.exe (ex: D:\\osu!\\osu!.exe): ");
            var path = Console.ReadLine()?.Trim('"');

            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                throw new FileNotFoundException("osu!.exe path invalid.", path);

            File.WriteAllText(ConfigPath, path);
            
            return path;
        }

        private sealed class Options
        {
            public string OsuPath { get; private set; }
            public string PatcherPath { get; private set; }
            public string Server { get; private set; }

            public static Options Parse(string[] args)
            {
                var options = new Options();

                for (var i = 0; i < args.Length; i++)
                {
                    switch (args[i])
                    {
                        case "--osu":
                            options.OsuPath = ReadValue(args, ref i, "--osu");
                            break;

                        case "--patcher":
                            options.PatcherPath = ReadValue(args, ref i, "--patcher");
                            break;

                        case "--server":
                        case "--devserver":
                        case "-devserver":
                            options.Server = ReadValue(args, ref i, "--server");
                            break;
                    }
                }

                return options;
            }

            private static string ReadValue(string[] args, ref int index, string name)
            {
                if (index + 1 >= args.Length)
                    throw new ArgumentException($"{name} requires a value");

                index++;
                return args[index];
            }
        }
        
        /// <summary>
        /// grabs the dll link from the provided data and downloads it
        /// </summary>
        /// <param name="data">json object containing the download url</param>
        /// <param name="k">key to look up the url</param>
        /// <returns>path to the downloaded file</returns>
        /// <exception cref="Exception">throws if the key is missing / empty</exception>
        private static string DownloadPatcher(JObject data, string k)
        {
            var obj = data[k] as JObject;
            if (obj == null)
                throw new Exception($"'{k}' not found");

            var url = (string)obj["url"];
            if (string.IsNullOrEmpty(url))
                throw new Exception($"no 'url' field for '{k}'");

            var filePath = Path.GetFullPath(k);
            Console.WriteLine($"downloading {k} from {url}...");

            new WebClient().DownloadFile(url, filePath);

            return filePath;
        }
        
        /// <summary>
        /// checks if hash matches
        /// </summary>
        /// <param name="filePath">path of the file</param>
        /// <param name="expectedHash">expected hash</param>
        /// <returns></returns>
        private static bool HashMatches(string filePath, string expectedHash)
        {
            if (string.IsNullOrEmpty(expectedHash) || !File.Exists(filePath))
                return false;

            using (var md5 = MD5.Create())
            using (var stream = File.OpenRead(filePath))
            {
                byte[] hash = md5.ComputeHash(stream);
                string actual = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                
                return actual == expectedHash.ToLowerInvariant();
            }
        }
    }
}
