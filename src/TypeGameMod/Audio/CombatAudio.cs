using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace TypeGameMod
{
    internal sealed partial class CombatAudio : IDisposable
    {
        internal const int Rate = 44100, Frames = 441, MaximumVoices = 16;
        readonly Queue<int> queue = new Queue<int>();
        readonly object sync = new object();
        readonly Thread worker;
        readonly string directory;
        readonly Action<string> log;
        volatile bool stopping;
        internal volatile bool Enabled;
        internal volatile int Ready, Started, Queued, LastError, Voices;
        sealed class Voice
        {
            internal short[] Samples;
            internal int At;
        }

        internal static readonly string[] Files = { "ak47-fire.wav", "shotgun-fire.wav", "ak47-reload.wav",
                                                    "shotgun-reload.wav" };
        internal CombatAudio(string folder, Action<string> logger)
        {
            directory = folder;
            log = logger;
            worker = new Thread(Run) { IsBackground = true, Name = "TypeGame weapon PCM mixer" };
            worker.Start();
        }

        internal void Play(int effect)
        {
            lock (sync)
            {
                if (Enabled && !stopping && effect >= 0 && effect < 4 && queue.Count < 32)
                {
                    queue.Enqueue(effect);
                    Queued++;
                }
            }
        }

        internal static short[] Load(string path)
        {
            using (var r = new BinaryReader(File.OpenRead(path)))
            {
                if (r.ReadUInt32() != 0x46464952u)
                    throw new InvalidDataException("Not RIFF");
                uint size = r.ReadUInt32();
                if (size > 2000000 || r.ReadUInt32() != 0x45564157u)
                    throw new InvalidDataException("Invalid WAVE");
                bool format = false;
                short[] samples = null;
                while (r.BaseStream.Position + 8 <= r.BaseStream.Length)
                {
                    uint kind = r.ReadUInt32(), length = r.ReadUInt32();
                    long end = r.BaseStream.Position + length;
                    if (end > r.BaseStream.Length)
                        throw new InvalidDataException("Truncated WAVE");
                    if (kind == 0x20746d66u)
                    {
                        if (length < 16 || r.ReadUInt16() != 1 || r.ReadUInt16() != 1 ||
                            r.ReadUInt32() != Rate || r.ReadUInt32() != Rate * 2 || r.ReadUInt16() != 2 ||
                            r.ReadUInt16() != 16)
                            throw new InvalidDataException("Expected mono44.1kHz16bit PCM");
                        format = true;
                    }
                    else if (kind == 0x61746164u)
                    {
                        if (length == 0 || length > Rate * 2 * 4 || (length & 1) != 0)
                            throw new InvalidDataException("Invalid PCM length");
                        byte[] bytes = r.ReadBytes((int)length);
                        samples = new short[length / 2];
                        Buffer.BlockCopy(bytes, 0, samples, 0, bytes.Length);
                    }
                    r.BaseStream.Position = end + (length & 1);
                }
                if (!format || samples == null)
                    throw new InvalidDataException("Missing PCM data");
                return samples;
            }
        }

        static void Check(uint result)
        {
            if (result != 0)
                throw new InvalidOperationException("waveOut error " + result);
        }

        void Run()
        {
            IntPtr device = IntPtr.Zero;
            IntPtr[] buffers = new IntPtr[4], headers = new IntPtr[4];
            bool[] prepared = new bool[4];
            uint headerSize = (uint)Marshal.SizeOf(typeof(Header));
            int flagsAt = (int)Marshal.OffsetOf(typeof(Header), "Flags");
            try
            {
                var samples = new short [4][];
                for (int i = 0; i < 4; i++)
                    samples[i] = Load(Path.Combine(directory, Files[i]));
                var format =
                    new Format { Tag = 1, Channels = 1, Rate = Rate, Bytes = Rate * 2, Align = 2, Bits = 16 };
                Check(waveOutOpen(out device, uint.MaxValue, ref format, IntPtr.Zero, IntPtr.Zero, 0));
                for (int i = 0; i < 4; i++)
                {
                    buffers[i] = Marshal.AllocHGlobal(Frames * 2);
                    headers[i] = Marshal.AllocHGlobal((int)headerSize);
                    Marshal.StructureToPtr(new Header { Data = buffers[i], Length = Frames * 2 }, headers[i],
                                           false);
                    Check(waveOutPrepareHeader(device, headers[i], headerSize));
                    prepared[i] = true;
                }
                var voices = new List<Voice>();
                var mixed = new short[Frames];
                bool[] submitted = new bool[4];
                Ready = 1;
                log("Weapon SFX ready: 4 licensed PCM clips, waveOut 44.1kHz mono,16 voices");
                while (!stopping)
                {
                    if (!Enabled)
                    {
                        voices.Clear();
                        lock (sync) queue.Clear();
                    }
                    for (int i = 0; i < 4; i++)
                    {
                        if (submitted[i] && (Marshal.ReadInt32(headers[i], flagsAt) & 1) == 0)
                            continue;
                        lock (sync) while (queue.Count > 0)
                        {
                            int effect = queue.Dequeue();
                            if (!Enabled)
                                continue;
                            if (voices.Count == MaximumVoices)
                                voices.RemoveAt(0);
                            voices.Add(new Voice { Samples = samples[effect] });
                            Started++;
                        }
                        for (int n = 0; n < Frames; n++)
                        {
                            int value = 0;
                            foreach (var voice in voices)
                                if (voice.At < voice.Samples.Length)
                                    value += voice.Samples[voice.At++];
                            mixed[n] = CombatLimiter.Apply(value);
                        }
                        voices.RemoveAll(delegate(Voice v) { return v.At >= v.Samples.Length; });
                        Voices = voices.Count;
                        Marshal.Copy(mixed, 0, buffers[i], Frames);
                        Check(waveOutWrite(device, headers[i], headerSize));
                        submitted[i] = true;
                    }
                    Thread.Sleep(3);
                }
            }
            catch (Exception ex)
            {
                LastError = 1;
                log("Weapon SFX unavailable: " + ex.Message);
            }
            finally
            {
                Ready = 0;
                Voices = 0;
                if (device != IntPtr.Zero)
                    waveOutReset(device);
                for (int i = 0; i < 4; i++)
                {
                    if (prepared[i])
                        waveOutUnprepareHeader(device, headers[i], headerSize);
                    if (headers[i] != IntPtr.Zero)
                        Marshal.FreeHGlobal(headers[i]);
                    if (buffers[i] != IntPtr.Zero)
                        Marshal.FreeHGlobal(buffers[i]);
                }
                if (device != IntPtr.Zero)
                    waveOutClose(device);
                log("Weapon SFX mixer closed; wave buffers released");
            }
        }

        public void Dispose()
        {
            Enabled = false;
            stopping = true;
            worker.Join(2000);
        }
    }
}
