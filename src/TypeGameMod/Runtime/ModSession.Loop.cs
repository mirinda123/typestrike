using System;
using System.Diagnostics;
using System.Threading;

namespace TypeGameMod
{
    internal sealed partial class ModSession
    {
        readonly OfflineMatch offlineMatch = new OfflineMatch();
        GameFrame previousFrame = new GameFrame();
        volatile string offlineStatus = "等待游戏";
        // One worker owns input, gameplay, audio events and snapshot publication.
        // COM rendering and native scene changes remain on the game's UI thread.
        void Poll()
        {
            var keyboard = new KeyboardInput(Down);
            var clock = Stopwatch.StartNew();
            double previousTime = clock.Elapsed.TotalSeconds;
            double idleTime = 0;
            uint sequence = 0;
            try
            {
                while (!worker.StopRequested && IsAlive)
                {
                    double now = clock.Elapsed.TotalSeconds;
                    double dt = Math.Max(0, Math.Min(GameRules.MaximumStepSeconds, now - previousTime));
                    previousTime = now;
                    GameFrame frame = ReadGameFrame();
                    bool focused = Win32.GetForegroundWindow() == Win32.Address(gameWindowHandle);
                    if (focused && frame.Scene == GameScene.Playing)
                        idleTime += dt;
                    Write(ControlBase + Control.IdleTick, BitConverter.GetBytes((uint)(idleTime * 15)));
                    InputFrame input = keyboard.Read(focused, focused && frame.Scene == GameScene.Playing,
                                                     GameScene.IsResult(frame.Scene), typing);
                    PublishMovement(input);
                    byte[] snapshot = frame.HasNetworkPlayers ? TickNetwork(frame, input, now, dt)
                                                              : TickOffline(frame, input, now, dt);
                    bool inRound = frame.Scene == GameScene.Playing &&
                                   BitConverter.ToUInt32(snapshot, SnapshotLayout.GameObject) != 0;
                    UpdateAnimations(frame, input, snapshot, inRound, dt);
                    UpdateMusic(inRound && equippedWeapon != 0, inRound, focused);
                    Publish(snapshot, ref sequence);
                    uint visual = VisualWeapon(frame.Role);
                    Write(ControlBase + Control.VisualWeapon, BitConverter.GetBytes(visual));
                    Write(ControlBase + Control.Armed, BitConverter.GetBytes(visual != 0 ? 1u : 0u));
                    bool shooting =
                        input.Fire || (!frame.HasNetworkPlayers && offlineMatch.Projectiles.Count > 0);
                    Write(ControlBase + Control.SuppressPhysicalCatch,
                          BitConverter.GetBytes(equippedWeapon != 0 && shooting ? 1u : 0u));
                    previousFrame = frame;
                    Write(ControlBase + Control.Heartbeat,
                          BitConverter.GetBytes(unchecked((uint)Environment.TickCount)));
                    Thread.Sleep(8);
                }
            }
            finally
            {
                if (music != null)
                    music.Update(false, false);
                if (combatAudio != null)
                    combatAudio.Enabled = false;
            }
        }

        internal string DescribeStatus()
        {
            return networkMatch != null ? networkStatus : offlineStatus;
        }
    }
}
