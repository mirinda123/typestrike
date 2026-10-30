using System;
using System.Diagnostics;
using System.IO;

namespace TypeGameMod
{
    internal static class GameInstallation
    {
        internal static bool Compatible(Process process)
        {
            foreach (ProcessModule m in process.Modules)
                if (String.Equals(m.ModuleName, "dciman32.dll", StringComparison.OrdinalIgnoreCase) &&
                    GameIdentity.Hash(m.FileName) == GameIdentity.CompatHash)
                    return true;
            return false;
        }

        static void CopyGame(string source, string target)
        {
            Directory.CreateDirectory(target);
            foreach (string file in Directory.GetFiles(source))
            {
                string name = Path.GetFileName(file);
                // IDA keeps its database beside the executable and locks its working
                // files. Analysis metadata is not a game dependency or runtime asset.
                string extension = Path.GetExtension(name).ToLowerInvariant();
                if (name.StartsWith("TypeGame.exe.", StringComparison.OrdinalIgnoreCase) &&
                    (extension == ".i64" || extension == ".idb" || extension == ".id0" ||
                     extension == ".id1" || extension == ".id2" || extension == ".nam" ||
                     extension == ".til"))
                    continue;
                if (name.StartsWith("DDrawCompat", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("dciman32.dll", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("ddraw.dll", StringComparison.OrdinalIgnoreCase))
                    continue;
                File.Copy(file, Path.Combine(target, name), true);
            }
            foreach (string dir in Directory.GetDirectories(source))
            {
                if ((File.GetAttributes(dir) & FileAttributes.ReparsePoint) != 0)
                    continue;
                CopyGame(dir, Path.Combine(target, Path.GetFileName(dir)));
            }
        }

        internal static string Prepare(string original, bool portable, string instance)
        {
            original = Path.GetFullPath(original);
            GameBundle.Validate(original);
            string source = Path.GetDirectoryName(original);
            string root =
                portable
                    ? source
                    : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                   "TypeGameWASD", ProjectInfo.Version, instance, "Classic");
            string exe = Path.Combine(root, "TypeGame.exe"),
                   marker = Path.Combine(root, "TypeGameWASD.prepared");
            bool ready =
                File.Exists(marker) && File.Exists(exe) &&
                GameIdentity.Hash(exe) == GameIdentity.ExecutableHash &&
                File.Exists(Path.Combine(root, "game", "GameRes.dll")) &&
                GameIdentity.Hash(Path.Combine(root, "game", "GameRes.dll")) == GameIdentity.ResourceHash;
            if (!portable && !ready)
            {
                CopyGame(source, root);
                File.WriteAllText(marker,
                                  "TypeGameWASD " + ProjectInfo.Version + "\r\nSource: " + original + "\r\n");
            }
            string bundle = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "compat"),
                   dll = Path.Combine(bundle, "dciman32.dll");
            if (!File.Exists(dll) || GameIdentity.Hash(dll) != GameIdentity.CompatHash)
                throw new InvalidOperationException("缺少图形兼容组件，请完整解压 MOD 压缩包。");
            string localDll = Path.Combine(root, "dciman32.dll");
            if (!File.Exists(localDll) || GameIdentity.Hash(localDll) != GameIdentity.CompatHash)
                File.Copy(dll, localDll, true);
            File.Copy(Path.Combine(bundle, "DDrawCompat.ini"), Path.Combine(root, "DDrawCompat.ini"), true);
            File.Copy(Path.Combine(bundle, "TypeGame.exe.manifest"),
                      Path.Combine(root, "TypeGame.exe.manifest"), true);
            File.Copy(Path.Combine(bundle, "Microsoft.VC80.CRT.manifest"),
                      Path.Combine(root, "Microsoft.VC80.CRT.manifest"), true);
            return exe;
        }
    }
}
