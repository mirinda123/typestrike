using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace TypeGameMod
{
    internal sealed partial class LauncherForm : Form
    {
        readonly string instance;
        readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        readonly TextBox path = new TextBox();
        readonly Label status = new Label();
        readonly Label telemetry = new Label();
        readonly Button start = new Button();
        readonly Button end = new Button();
        readonly NumericUpDown bulletSpeed = new NumericUpDown();
        readonly CheckBox weaponMusic = new CheckBox();
        readonly bool portable;
        readonly bool closeWithGame;
        ModSession mod;
        readonly SessionLog sessionLog =
            new SessionLog(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "TypeGameWASD.log"));
        internal LauncherForm(LauncherOptions options)
        {
            instance = options.Instance;
            portable = options.Portable;
            closeWithGame = options.Portable && options.Start;
            Text = "生死时速 · 双人枪战 " + ProjectInfo.Version;
            ClientSize = new Size(660, 370);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            if (options.Start || options.AttachPid != 0)
                WindowState = FormWindowState.Minimized;
            Font = new Font("Microsoft YaHei UI", 10);
            StartPosition = FormStartPosition.CenterScreen;
            var title = new Label { Text = "生死时速 · 双人枪战",
                                    Font = new Font(Font.FontFamily, 17, FontStyle.Bold),
                                    Location = new Point(20, 16), AutoSize = true };
            Controls.Add(title);
            Controls.Add(
                new Label { Text = "WASD 移动 / 滚轮切枪 / 1、2 切枪 / Tab 打字 / Shift 技能 / 回车射击",
                            Location = new Point(22, 59), AutoSize = true });
            path.SetBounds(22, 92, 473, 28);
            path.Text = options.GamePath;
            Controls.Add(path);
            var browse =
                new Button { Text = "选择…", Location = new Point(502, 91), Size = new Size(66, 30) };
            Controls.Add(browse);
            browse.Click += delegate
            {
                using (var dialog = new OpenFileDialog {
                    Filter = "TypeGame.exe|TypeGame.exe", FileName = "TypeGame.exe"
                }) if (dialog.ShowDialog() == DialogResult.OK) path.Text = dialog.FileName;
            };
            Controls.Add(
                new Label { Text = "子弹速度（像素/秒）", Location = new Point(22, 136), AutoSize = true });
            bulletSpeed.SetBounds(185, 133, 100, 28);
            bulletSpeed.Minimum = 100;
            bulletSpeed.Maximum = 2400;
            bulletSpeed.Increment = GameRules.BulletSpeedStep;
            bulletSpeed.Value = GameRules.DefaultBulletSpeed;
            Controls.Add(bulletSpeed);
            bulletSpeed.ValueChanged += delegate
            {
                if (mod != null)
                    mod.SelectedBulletSpeed = (int)bulletSpeed.Value;
            };
            Controls.Add(new Label { Text = "单人 [ / ] 调整；对战由房主调整", Location = new Point(305, 137),
                                     AutoSize = true });
            weaponMusic.Enabled =
                File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "audio", "MOON-Paris.m4a"));
            weaponMusic.Text = weaponMusic.Enabled ? "首次装备枪后持续播放 Paris" : "武器配乐（未安装）";
            weaponMusic.Checked = weaponMusic.Enabled;
            weaponMusic.AutoSize = true;
            weaponMusic.Location = new Point(22, 178);
            Controls.Add(weaponMusic);
            weaponMusic.CheckedChanged += delegate
            {
                if (mod != null)
                    mod.WeaponMusicEnabled = weaponMusic.Checked;
            };
            start.Text = "启动游戏";
            start.SetBounds(22, 220, 190, 37);
            Controls.Add(start);
            start.Click += delegate
            {
                Begin(0);
            };
            end.Text = "停用并恢复";
            end.SetBounds(225, 220, 150, 37);
            end.Enabled = false;
            Controls.Add(end);
            end.Click += delegate
            {
                Stop();
            };
            status.SetBounds(22, 274, 616, 26);
            status.Text = "两人进入 多人游戏 → 创建 / 加入游戏；单人模式也可使用";
            Controls.Add(status);
            telemetry.SetBounds(22, 305, 616, 48);
            telemetry.ForeColor = Color.DimGray;
            telemetry.Text = "Tab 打字，每个正确单词加一格能量；E 补弹，Shift 瞬移或举盾。";
            Controls.Add(telemetry);
            timer.Interval = 500;
            timer.Tick += delegate
            {
                RefreshSession();
            };
            timer.Start();
            FormClosing += delegate
            {
                Stop();
            };
            Shown += delegate
            {
                if (options.AttachPid != 0)
                    Begin(options.AttachPid);
                else if (options.Start)
                    Begin(0);
            };
        }

        void RefreshSession()
        {
            if (mod == null)
                return;
            if (!mod.IsAlive)
            {
                Stop();
                status.Text = "游戏已退出";
                if (closeWithGame)
                    Close();
                return;
            }
            if (mod.Failure != null)
            {
                string reason = mod.Failure.Message;
                Stop();
                WindowState = FormWindowState.Normal;
                status.Text = "MOD 已停止：" + reason;
                telemetry.Text = "请查看 logs/TypeGameWASD.log；可重新启动 MOD。";
                return;
            }
            telemetry.Text = mod.DescribeStatus();
            bulletSpeed.Value = mod.SelectedBulletSpeed;
            bulletSpeed.Enabled = mod.OwnsSpeed;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                timer.Dispose();
            base.Dispose(disposing);
        }

        void Log(string text)
        {
            sessionLog.Write(text);
        }

        void Stop()
        {
            StopWheelInput();
            if (mod != null)
            {
                mod.Dispose();
                mod = null;
            }
            start.Enabled = true;
            end.Enabled = false;
            bulletSpeed.Enabled = true;
            status.Text = "MOD 已停用；重新开局即可使用原打字玩法";
        }

        void Begin(int attachPid)
        {
            if (mod != null)
                return;
            start.Enabled = false;
            try
            {
                Process target = attachPid == 0 ? null : Process.GetProcessById(attachPid);
                // The old DirectDraw game needs the foreground while creating its display.
                // Hide our window before launching it so the helper cannot steal that focus.
                WindowState = FormWindowState.Minimized;
                if (target == null)
                {
                    status.Text = "正在准备游戏…";
                    bool useBundle =
                        portable &&
                        String.Equals(Path.GetFullPath(path.Text),
                                      GameBundle.Executable(AppDomain.CurrentDomain.BaseDirectory),
                                      StringComparison.OrdinalIgnoreCase);
                    string runtime = GameInstallation.Prepare(path.Text, useBundle, instance);
                    foreach (Process p in Process.GetProcessesByName("TypeGame"))
                    {
                        try
                        {
                            if (String.Equals(p.MainModule.FileName, runtime,
                                              StringComparison.OrdinalIgnoreCase) &&
                                Win32.GameWindow(p.Id) != IntPtr.Zero)
                            {
                                target = p;
                                break;
                            }
                        }
                        catch (InvalidOperationException error)
                        {
                            Log("Game process exited while enumerating: " + error.Message);
                        }
                        catch (Win32Exception error)
                        {
                            Log("Cannot inspect game process: " + error.Message);
                        }
                    }
                    if (target == null)
                    {
                        foreach (Process p in Process.GetProcessesByName("TypeGame"))
                        {
                            if (instance == "default" && Win32.GameWindow(p.Id) != IntPtr.Zero)
                                throw new InvalidOperationException(
                                    "请先关闭正在运行的游戏，再从本启动器启动。");
                        }
                        target = Process.Start(
                            new ProcessStartInfo(runtime) { WorkingDirectory = Path.GetDirectoryName(runtime),
                                                            UseShellExecute = true });
                    }
                }
                status.Text = "正在连接游戏…";
                start.Enabled = false;
                int count = 0;
                while (Win32.GameWindow(target.Id) == IntPtr.Zero && !target.HasExited && count++ < 100)
                {
                    Thread.Sleep(100);
                }
                mod = new ModSession(target, Log);
                mod.SelectedBulletSpeed = (int)bulletSpeed.Value;
                mod.WeaponMusicEnabled = weaponMusic.Checked;
                mod.Attach();
                StartWheelInput();
                end.Enabled = true;
                status.Text = "双枪与技能已启用：Tab 打字充能，Shift 技能，E 补弹";
                IntPtr gameWindow = Win32.GameWindow(target.Id);
                Win32.FocusGame(gameWindow);
            }
            catch (Exception ex)
            {
                Log("Error: " + ex);
                Stop();
                WindowState = FormWindowState.Normal;
                status.Text = "连接失败：" + ex.Message;
                MessageBox.Show(this, ex.Message, "WASD MOD", MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
            }
        }
    }
}
