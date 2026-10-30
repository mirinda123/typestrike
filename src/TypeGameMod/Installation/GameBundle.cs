using System;
using System.IO;

namespace TypeGameMod
{
    // Release layout shared by default startup and portable preparation.
    internal static class GameBundle
    {
        internal const string DirectoryName = "Classic";

        internal static string Executable(string applicationDirectory)
        {
            return Path.Combine(applicationDirectory, DirectoryName, "TypeGame.exe");
        }

        internal static void Validate(string executable)
        {
            if (!File.Exists(executable) || GameIdentity.Hash(executable) != GameIdentity.ExecutableHash)
                throw new InvalidOperationException("游戏版本不匹配或文件缺失，请完整解压发布包。");
            string directory = Path.GetDirectoryName(executable);
            string resources = Path.Combine(directory, "game", "GameRes.dll");
            if (!File.Exists(resources) || GameIdentity.Hash(resources) != GameIdentity.ResourceHash)
                throw new InvalidOperationException("游戏资源版本不匹配，请完整解压发布包。");
            foreach (string relative in new[] { "dx7vb.dll", "game\\GameSnd.dll" })
                if (!File.Exists(Path.Combine(directory, relative)))
                    throw new FileNotFoundException("游戏运行文件缺失：" + relative);
        }
    }
}
