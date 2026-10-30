using System;
using System.IO;
using TypeGameMod;

internal static class BundleTests
{
    internal static void Run()
    {
        string root = Path.Combine(Path.GetTempPath(), "TypeGame bundle " + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        int checks = 0;
        try
        {
            Action<bool, string> check = delegate(bool condition, string message)
            {
                checks++;
                if (!condition)
                    throw new Exception("Bundle: " + message);
            };
            LauncherOptions source = LauncherOptions.Parse(new string[0], root);
            check(!source.Start && !source.Portable, "source build opens the normal launcher");
            Directory.CreateDirectory(Path.Combine(root, GameBundle.DirectoryName));
            LauncherOptions partial = LauncherOptions.Parse(new string[0], root);
            check(partial.Start && partial.Portable,
                  "partial extraction cannot silently use another install");
            check(partial.GamePath == GameBundle.Executable(root), "portable path includes spaces unchanged");
            File.WriteAllText(GameBundle.Executable(root), "invalid game");
            bool rejected = false;
            try
            {
                GameBundle.Validate(partial.GamePath);
            }
            catch (InvalidOperationException)
            {
                rejected = true;
            }
            check(rejected, "an unsupported bundled executable is rejected before launching");
            LauncherOptions settings = LauncherOptions.Parse(new[] { "--launcher" }, root);
            check(settings.Portable && settings.ShowLauncher && !settings.Start,
                  "settings do not auto-start");
            LauncherOptions external =
                LauncherOptions.Parse(new[] { "--game", "C:\\Games\\TypeGame.exe" }, root);
            check(!external.Portable && !external.Start && external.GamePath == "C:\\Games\\TypeGame.exe",
                  "an explicit external game keeps the installed-game workflow");
            LauncherOptions attached =
                LauncherOptions.Parse(new[] { "--attach", "1234", "--instance", "lab" }, root);
            check(!attached.Portable && !attached.Start && attached.AttachPid == 1234,
                  "the approved attach workflow is unaffected by a bundled game");
            foreach (string[] arguments in new[] { new[] { "--launcher", "--start" },
                                                   new[] { "--launcher", "--attach", "1234" } })
            {
                rejected = false;
                try
                {
                    LauncherOptions.Parse(arguments, root);
                }
                catch (ArgumentException)
                {
                    rejected = true;
                }
                check(rejected, "contradictory startup modes are reported");
            }
            string nested = Path.Combine(root, "logs", "session.log");
            new SessionLog(nested).Write("portable startup");
            check(File.ReadAllText(nested).Contains("portable startup"),
                  "logging prepares its own directory");
            Console.WriteLine(
                "PASS " + checks +
                " portable startup: bundle, settings, paths, explicit overrides and validation");
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }
}
