using System;
using System.IO;
using System.Text.RegularExpressions;

namespace TypeGameMod
{
    internal sealed class LauncherOptions
    {
        internal string Instance = "default";
        internal string GamePath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TypeEasyGame",
                         "Classic", "TypeGame.exe");
        internal int AttachPid;
        internal bool Start;
        internal bool Portable;
        internal bool ShowLauncher;
        internal static LauncherOptions Parse(string[] args)
        {
            return Parse(args, AppDomain.CurrentDomain.BaseDirectory);
        }

        internal static LauncherOptions Parse(string[] args, string applicationDirectory)
        {
            var result = new LauncherOptions();
            bool explicitGame = false;
            for (int i = 0; i < args.Length; i++)
            {
                string option = args[i];
                if (option == "--start")
                {
                    result.Start = true;
                    continue;
                }
                if (option == "--launcher")
                {
                    result.ShowLauncher = true;
                    continue;
                }
                if (option != "--instance" && option != "--game" && option != "--attach")
                    throw new ArgumentException("Unknown option: " + option);
                if (++i == args.Length)
                    throw new ArgumentException("Missing value for " + option);
                switch (option)
                {
                case "--instance":
                    result.Instance = args[i];
                    break;
                case "--game":
                    result.GamePath = args[i];
                    explicitGame = true;
                    break;
                case "--attach":
                    if (!int.TryParse(args[i], out result.AttachPid) || result.AttachPid <= 0)
                        throw new ArgumentException("--attach requires a positive process ID.");
                    break;
                }
            }
            if (!Regex.IsMatch(result.Instance, "^[a-zA-Z0-9_-]{1,32}$"))
                throw new ArgumentException("--instance accepts 1 to 32 letters, digits, '-' or '_'.");
            if (result.ShowLauncher && (result.Start || result.AttachPid != 0))
                throw new ArgumentException("--launcher cannot be combined with --start or --attach.");
            // A partial extraction must report a bundle error, rather than silently
            // opening another installed game from this computer.
            if (!explicitGame && result.AttachPid == 0 &&
                Directory.Exists(Path.Combine(applicationDirectory, GameBundle.DirectoryName)))
            {
                result.GamePath = GameBundle.Executable(applicationDirectory);
                result.Portable = true;
                result.Start = !result.ShowLauncher;
            }
            return result;
        }
    }
}
