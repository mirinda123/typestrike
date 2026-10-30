using TypeGameMod;
using System;
using System.IO;
class MusicTests
{
    static int checks;
    static void Check(bool yes, string why)
    {
        checks++;
        if (!yes)
            throw new Exception(why);
    }
    internal static void RunSuite(string[] args)
    {
        try
        {
            var p = new MusicPolicy();
            Check(!p.Observe(false, true, true, true), "native music until first weapon");
            Check(!p.Observe(true, true, false, true) && p.Triggered,
                  "remember weapon trigger while decoder prepares");
            Check(p.Observe(false, true, true, true), "holster while loading still enters Paris");
            for (int i = 0; i < 20; i++)
            {
                Check(!p.Observe(false, true, true, false) && p.ReplacesNative && p.Triggered,
                      "results/menu pause playback while retaining selected music and native mute");
                Check(p.Observe(false, true, true, true) && p.ReplacesNative,
                      "next round resumes Paris without equipping a gun again");
            }
            Check(!p.Observe(false, false, true, false) && p.Triggered && !p.ReplacesNative,
                  "explicit disable on results restores native music");
            Check(!p.Observe(false, true, true, false) && p.ReplacesNative,
                  "re-enabling on results keeps the selected track paused");
            Check(p.Observe(false, true, true, true), "re-enable latched music with gun holstered");
            var disabled = new MusicPolicy();
            Check(!disabled.Observe(true, false, true, true) && !disabled.Triggered,
                  "no accidental trigger while disabled");
            Check(!new MusicPolicy().Triggered, "new mod session starts fresh");
            var e = new MusicEnvelope();
            Check(e.Level(1000) == MusicEnvelope.Base, "normal music balance");
            e.Trigger(1000);
            Check(e.Level(1000) == MusicEnvelope.Low && e.Level(1109) == MusicEnvelope.Low,
                  "shot ducks music immediately for110ms");
            Check(e.Level(1200) > MusicEnvelope.Low && e.Level(1200) < MusicEnvelope.Base, "smooth release");
            Check(e.Level(1290) == MusicEnvelope.Base && e.Level(4000) == MusicEnvelope.Base,
                  "full level returns");
            e.Trigger(int.MaxValue - 50);
            Check(e.Level(unchecked(int.MinValue + 30)) == MusicEnvelope.Low, "tick wrap safe");
            int previous = 0;
            for (int i = 0; i <= 600000; i += 100)
            {
                int v = CombatLimiter.Apply(i);
                Check(v >= previous && v < 32767, "overload stays monotonic and below full scale");
                Check(CombatLimiter.Apply(-i) == -v, "symmetric saturation");
                previous = v;
            }
            foreach (string file in new[] { "ak47-fire.wav", "shotgun-fire.wav" })
            {
                short[] a = CombatAudio.Load(Path.Combine(args[0], file));
                double energy = 0;
                int count = Math.Min(3528, a.Length), max = 0;
                for (int i = 0; i < count; i++)
                {
                    double x = CombatLimiter.Apply(a[i]) / 32768.0;
                    energy += x * x;
                    max = Math.Max(max, Math.Abs((int)a[i]));
                }
                Check(Math.Sqrt(energy / count) > .23, "shot has sustained audible body in first80ms");
                Check(max < 30000, "single effect has peak headroom");
            }
            Check(CombatAudio.Frames * 4 * 1000 / CombatAudio.Rate == 40, "bounded40ms output buffering");
            Console.WriteLine("PASS " + checks +
                              " sticky music, duck envelope, polyphonic limiter and punchy PCM checks");
        }
        catch (Exception ex)
        {
            Console.WriteLine("FAIL after " + checks + " checks: " + ex);
            Environment.ExitCode = 1;
        }
    }
}
