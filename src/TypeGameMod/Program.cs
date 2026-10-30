using System;
using System.Threading;
using System.Windows.Forms;

namespace TypeGameMod
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            LauncherOptions options;
            try
            {
                options = LauncherOptions.Parse(args);
            }
            catch (ArgumentException error)
            {
                MessageBox.Show(error.Message, "TypeGameWASD 参数错误");
                return;
            }
            bool first;
            using (var mutex = new Mutex(true, "TypeGameWASD_" + ProjectInfo.Version + "_" + options.Instance,
                                         out first))
            {
                if (!first)
                {
                    MessageBox.Show("WASD 启动器已经在运行，请使用已打开的启动器。");
                    return;
                }
                Application.Run(new LauncherForm(options));
            }
        }
    }
}
