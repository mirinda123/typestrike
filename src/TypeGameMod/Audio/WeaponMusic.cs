using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace TypeGameMod
{
    internal sealed partial class WeaponMusic : IDisposable
    {
        readonly MusicEnvelope envelope = new MusicEnvelope();
        internal void Punch()
        {
            envelope.Trigger(Environment.TickCount);
        }

        readonly Thread worker;
        readonly string path;
        readonly Func<bool> nativeMuted;
        readonly Action<string> log;
        volatile bool stopping, wanted, playbackAllowed, ready;
        volatile int lastUpdate, state, plays, pauses, loops, position, bytes, lastError, nativeState,
            duration;
        internal bool Ready
        {
            get {
                return ready;
            }
        }

        internal int State
        {
            get {
                return state;
            }
        }

        internal int DurationMilliseconds
        {
            get {
                return duration;
            }
        }

        internal WeaponMusic(string file, Func<bool> isMuted, Action<string> logger)
        {
            path = file;
            nativeMuted = isMuted;
            log = logger;
            lastUpdate = Environment.TickCount;
            worker = new Thread(Run);
            worker.IsBackground = true;
            worker.Name = "TypeGame compressed weapon music";
            worker.Start();
        }

        internal void Update(bool keepTrack, bool allowPlayback)
        {
            playbackAllowed = allowPlayback;
            wanted = keepTrack;
            lastUpdate = Environment.TickCount;
        }

        internal byte[] Telemetry()
        {
            var b = new byte[36];
            int[] values = { state,     plays,         pauses, loops,      position,
                             lastError, ready ? 1 : 0, bytes,  nativeState };
            for (int i = 0; i < values.Length; i++)
                Buffer.BlockCopy(BitConverter.GetBytes(values[i]), 0, b, i * 4, 4);
            return b;
        }

        void Check(int error, string operation)
        {
            if (error < 0)
            {
                lastError = error;
                throw new COMException(
                    operation + " failed (0x" + unchecked((uint)error).ToString("X8") + ")", error);
            }
        }

        void ReadEvents(Events events, ref bool loaded, ref bool ended)
        {
            MediaEvent e;
            while (events.Queue.TryDequeue(out e))
            {
                Check(e.Error, "MFPlay event " + e.Type);
                if (e.Type == 6)
                    loaded = true;
                if (e.Type == 11)
                    ended = true;
            }
        }

        void Run()
        {
            MediaPlayer player = null;
            IntPtr item = IntPtr.Zero;
            Events events = null;
            bool com = false, mf = false, queued = false, paused = false, loaded = false, ended = false;
            float level = -1;
            Guid clock = Guid.Empty;
            try
            {
                var file = new FileInfo(path);
                if (!file.Exists)
                    throw new FileNotFoundException("Music M4A missing", path);
                if (file.Length == 0 || file.Length > 160000000)
                    throw new InvalidDataException("Invalid music file size");
                bytes = (int)file.Length;
                Check(CoInitializeEx(IntPtr.Zero, 0), "CoInitializeEx");
                com = true;
                Check(MFStartup(0x20070, 0), "MFStartup");
                mf = true;
                events = new Events();
                Check(MFPCreateMediaPlayer(null, false, 1, events, IntPtr.Zero, out player),
                      "MFPCreateMediaPlayer");
                Check(player.CreateMediaItemFromURL(file.FullName, true, UIntPtr.Zero, out item),
                      "Open compressed music");
                Check(player.SetMediaItem(item), "SetMediaItem");
                Marshal.Release(item);
                item = IntPtr.Zero;
                int begin = Environment.TickCount;
                while (!loaded && !stopping)
                {
                    ReadEvents(events, ref loaded, ref ended);
                    if (unchecked((uint)(Environment.TickCount - begin)) > 10000)
                        throw new TimeoutException("Music preparation timed out");
                    if (!loaded)
                        Thread.Sleep(20);
                }
                if (stopping)
                    return;
                PropValue length;
                Check(player.GetDuration(ref clock, out length), "GetDuration");
                if ((length.Type != 20 && length.Type != 21) || length.Value <= 0 ||
                    length.Value / 10000 > int.MaxValue)
                    throw new InvalidDataException("Invalid music duration");
                duration = (int)(length.Value / 10000);
                Check(player.SetVolume(MusicEnvelope.Base), "Initial music volume");
                level = MusicEnvelope.Base;
                ready = true;
                state = 1;
                log("Weapon music ready: " + file.Name + " compressed bytes=" + bytes +
                    " duration_ms=" + duration + " backend=Windows MFPlay (no WAV)");
                while (!stopping)
                {
                    ReadEvents(events, ref loaded, ref ended);
                    bool arm = wanted && unchecked((uint)(Environment.TickCount - lastUpdate)) < 600;
                    bool audible = false;
                    if (arm && playbackAllowed)
                    {
                        try
                        {
                            audible = nativeMuted();
                        }
                        catch (System.ComponentModel.Win32Exception)
                        {
                            audible = false; // Target can exit between the liveness check and memory read.
                        }
                    }
                    float desired = envelope.Level(Environment.TickCount);
                    if (Math.Abs(level - desired) > .002f)
                    {
                        Check(player.SetVolume(desired), "Weapon/music balance");
                        level = desired;
                    }
                    if (!arm && queued)
                    {
                        Check(player.Stop(), "Stop");
                        queued = false;
                        paused = false;
                        ended = false;
                        position = 0;
                        state = 1;
                        log("Weapon music stopped; native BGM restored");
                    }
                    else if (audible && !queued)
                    {
                        Check(player.Play(), "Play");
                        queued = true;
                        paused = false;
                        ended = false;
                        plays++;
                        state = 2;
                        log("Weapon music playing: M.O.O.N. - Paris (loop)");
                    }
                    else if (queued && audible && ended)
                    {
                        // Stop resets the cursor; MFPlay queues these operations in order.
                        Check(player.Stop(), "Loop reset");
                        Check(player.Play(), "Loop play");
                        ended = false;
                        paused = false;
                        loops++;
                        position = 0;
                        state = 2;
                    }
                    else if (queued && !audible && !paused)
                    {
                        if (!ended)
                            Check(player.Pause(), "Pause");
                        paused = true;
                        pauses++;
                        state = 3;
                        log("Weapon music paused");
                    }
                    else if (queued && audible && paused)
                    {
                        Check(player.Play(), "Resume");
                        paused = false;
                        state = 2;
                        log("Weapon music resumed");
                    }
                    int observedState;
                    Check(player.GetState(out observedState), "GetState");
                    nativeState = observedState;
                    if (queued)
                    {
                        PropValue cursor;
                        Check(player.GetPosition(ref clock, out cursor), "GetPosition");
                        if (cursor.Type == 20 || cursor.Type == 21)
                            position = (int)(cursor.Value / 10000);
                    }
                    Thread.Sleep(20);
                }
            }
            catch (Exception ex)
            {
                if (lastError == 0)
                    lastError = Marshal.GetHRForException(ex);
                ready = false;
                state = 0;
                log("Weapon music unavailable: " + ex.Message + "; keeping original music.");
            }
            finally
            {
                ready = false;
                if (item != IntPtr.Zero)
                    Marshal.Release(item);
                if (player != null)
                {
                    player.Shutdown();
                    Marshal.FinalReleaseComObject(player);
                }
                GC.KeepAlive(events);
                if (mf)
                    MFShutdown();
                if (com)
                    CoUninitialize();
                nativeState = 4;
                state = 4;
            }
        }

        public void Dispose()
        {
            stopping = true;
            wanted = false;
            if (worker != Thread.CurrentThread)
                worker.Join();
        }
    }
}
