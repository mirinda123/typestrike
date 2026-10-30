using TypeGameMod;
using System;
using System.IO;
using System.Threading;

internal static class AmmoTests
{
    static int checks;
    static void Check(bool ok, string why)
    {
        checks++;
        if (!ok)
            throw new Exception(why);
    }
    sealed class Pair
    {
        internal DuelSession H, C;
        internal double Now;
        internal readonly DuelPose[] P = { new DuelPose { X = 400, Y = 500, Heading = 0, Weapon = 1 },
                                           new DuelPose { X = 1000, Y = 500, Heading = 0, Weapon = 1 } };
        internal Pair(int hostRole)
        {
            H = TestDuel.Create(true, hostRole, 450, TestMap.Empty);
            C = TestDuel.Create(false, 1 - hostRole, 450, TestMap.Empty);
            for (int i = 0; i < 12; i++)
                Step(.01, false, false, false, false);
            Check(H.Playing && C.Playing, "ammo pair connected");
            TestLoadout.Fill(H, Now);
            Exchange();
        }
        internal void Exchange()
        {
            Check(H.Receive(C.Packet(), 20, Now), "client wire6 accepted");
            Check(C.Receive(H.Packet(), 10, Now), "host wire6 accepted");
        }
        internal void Step(double dt, bool policeFire, bool thiefFire, bool policeReload, bool thiefReload)
        {
            Now += dt;
            C.ReloadKey(C.Role == 0 ? policeReload : thiefReload);
            C.Observe(3, P[C.Role], 450, C.Role == 0 ? policeFire : thiefFire, false, Now, dt);
            Exchange();
            H.ReloadKey(H.Role == 0 ? policeReload : thiefReload);
            H.Observe(3, P[H.Role], 450, H.Role == 0 ? policeFire : thiefFire, false, Now, dt);
            Exchange();
        }
        internal void Wait(double seconds, bool pFire = false, bool tFire = false)
        {
            for (int i = 0; i < (int)Math.Round(seconds * 100); i++)
                Step(.01, pFire, tFire, false, false);
        }
    }
    static void Basic()
    {
        var a = new WeaponAmmo();
        Check(a.Rounds[1] == 0 && a.Rounds[2] == 0, "initial magazines empty");
        Check(!a.Fire(1, 0) && !a.Reload(1, 0), "empty start has no free R refill");
        a.Rounds[1] = 30;
        a.Rounds[2] = 8;
        a.Reserve[1] = 3;
        a.Reserve[2] = 2;
        a.Select(1, 0);
        Check(!a.Reload(1, 0), "full magazine cannot reload");
        Check(a.Fire(1, 0) && a.Rounds[1] == 29, "one shot consumes one round");
        Check(!a.Fire(1, .17999) && a.Fire(1, .18), "AK180ms boundary");
        a.Select(2, .18);
        Check(!a.Fire(2, .181), "swapping cannot bypass global cooldown");
        a.Select(1, .19);
        Check(a.Rounds[1] == 28, "switching cannot refill");
        Check(a.Reload(1, .2) && a.Remaining(.2) == 1800, "AK reload starts1800ms");
        Check(!a.Fire(1, 1.9999) && !a.Reload(1, .4), "reload blocks firing and cannot restart");
        a.Tick(1.9999);
        Check(a.Rounds[1] == 28, "not complete early");
        a.Tick(2);
        Check(a.Rounds[1] == 30 && a.Reloading == 0, "reload completes at deadline");
        a.Fire(1, 2);
        a.Reload(1, 2.01);
        a.Select(2, 2.2);
        a.Tick(10);
        Check(a.Rounds[1] == 29 && a.Reloading == 0, "switch cancels unfinished reload");
        Check(a.Fire(2, 10) && a.Rounds[2] == 7, "shotgun blast consumes one shell");
        Check(!a.Fire(2, 10.69999) && a.Fire(2, 10.7), "shotgun700ms boundary");
        Check(a.Reload(2, 11) && a.Remaining(11) == 2400, "shotgun reload2400ms");
        a.Tick(13.4);
        Check(a.Rounds[2] == 8, "shotgun refilled");
        a.Reset();
        a.Rounds[1] = 30;
        for (int i = 0; i < 30; i++)
            Check(a.Fire(1, i * .18), "consume complete AK magazine");
        Check(!a.Fire(1, 100) && a.Rounds[1] == 0 && a.Fired[1] == 30, "empty stops indefinitely");
        Check(!a.Reload(1, 100), "R cannot manufacture ammunition");
        Check(a.Supply(1, 101.8) && a.Fire(1, 101.8), "E supplies instantly shootable rounds");
        for (int i = 0; i < 10; i++)
        {
            a.Supply(1, 104 + i * 2);
            a.Reload(1, 104 + i * 2);
            a.Tick(105.8 + i * 2);
            Check(a.Rounds[1] == 30 && a.Reserve[1] <= 90, "earned resupply remains bounded");
            a.Fire(1, 105.8 + i * 2);
        }
        a.Reset();
        Check(a.Rounds[1] == 0 && a.Rounds[2] == 0 && a.Reserve[1] == 0 && a.Fired[1] == 0 &&
                  a.Reloads[1] == 0,
              "new round resets ammo and sound events");
        a.Rounds[1] = 30;
        a.Reserve[1] = 1;
        a.Fire(1, 1);
        a.Reload(1, 1.1);
        using (var m = new MemoryStream()) using (var w = new BinaryWriter(m))
        {
            a.Write(w, 1.5);
            m.Position = 0;
            var b = WeaponAmmo.Read(new BinaryReader(m), 100);
            Check(b.Rounds[1] == 29 && b.Reloading == 1 && b.Remaining(100) == 1400,
                  "wire uses receiver clock");
            b.Tick(101.4);
            Check(b.Rounds[1] == 30, "receiver reload deadline");
            byte[] raw = m.ToArray();
            foreach (int at in new[] { 0, 1, 2, 3, 4, 5, 7, 9 })
            {
                byte old = raw[at];
                raw[at] = 255;
                bool invalid = false;
                try
                {
                    WeaponAmmo.Read(new BinaryReader(new MemoryStream(raw)), 0);
                }
                catch (FormatException)
                {
                    invalid = true;
                }
                Check(invalid, "malformed bounded ammo state");
                raw[at] = old;
            }
        }
    }
    static void Network(int hostRole, int weapon)
    {
        var p = new Pair(hostRole);
        p.P[0].Weapon = p.P[1].Weapon = (byte)weapon;
        p.Step(.01, false, false, false, false);
        double duration = weapon == 1 ? 6 : 7;
        p.Wait(duration, true, true);
        int cap = WeaponAmmo.Capacity(weapon);
        for (int role = 0; role < 2; role++)
        {
            Check(p.H.Armories[role].Rounds[weapon] == 0 && p.C.Armories[role].Rounds[weapon] == 0,
                  "both views empty");
            Check(p.H.Armories[role].Fired[weapon] == cap && p.C.Armories[role].Fired[weapon] == cap,
                  "exactly magazine capacity accepted per role");
        }
        Check(p.H.ShotsFired == 2 * cap, "no extra shots at empty");
        uint fired = p.H.ShotsFired;
        p.Wait(.8, true, true);
        Check(p.H.ShotsFired == fired, "holding fire at empty stays silent");
        p.Step(.01, false, false, true, true);
        Check(p.H.Armories[0].Reloading == weapon && p.H.Armories[1].Reloading == weapon,
              "both reload commands accepted");
        p.Wait(weapon == 1 ? 1.7 : 2.3, true, true);
        Check(p.H.ShotsFired == fired, "reload suppresses all shooting");
        p.Wait(.15);
        for (int role = 0; role < 2; role++)
            Check(p.H.Armories[role].Rounds[weapon] == cap && p.C.Armories[role].Rounds[weapon] == cap,
                  "refill synchronized");
        p.Wait(.2, true, true);
        Check(p.H.ShotsFired > fired, "can shoot after reloading");
        int other = 1 - p.C.Role;
        int before = p.H.Armories[p.C.Role].Rounds[weapon];
        p.C.Armories[p.C.Role].Rounds[weapon] = cap;
        p.Exchange();
        Check(p.H.Armories[p.C.Role].Rounds[weapon] == before &&
                  p.C.Armories[p.C.Role].Rounds[weapon] == before,
              "client cannot author ammo");
        string text = p.C.Packet();
        byte[] bytes = Convert.FromBase64String(text.Substring(9));
        bytes[4] = 3;
        Check(!p.H.Receive("TGDUEL19:" + Convert.ToBase64String(bytes), 20, p.Now),
              "old wire3 rejected explicitly");
        Check(p.H.Packet().Length < 4094, "ammo protocol fits native mailbox");
    }
    static void RetryAndCadence()
    {
        var p = new Pair(0);
        p.C.Observe(3, p.P[1], 450, true, false, p.Now, .01);
        string fire = p.C.Packet();
        Check(p.H.Receive(fire, 20, p.Now) && p.H.Armories[1].Rounds[1] == 29, "remote fire consumes once");
        Check(!p.H.Receive(fire, 20, p.Now), "duplicate packet rejected");
        Check(p.H.Receive(p.C.Packet(), 20, p.Now) && p.H.Armories[1].Rounds[1] == 29,
              "new snapshot repeats command idempotently");
        p.Exchange();
        p.C.ReloadKey(true);
        p.C.Observe(3, p.P[1], 450, false, false, p.Now, .01);
        // First packet is intentionally dropped. Next packet retries the command.
        p.C.Packet();
        Check(p.H.Receive(p.C.Packet(), 20, p.Now) && p.H.Armories[1].Reloading == 1,
              "lost reload request retried");
        uint reloads = p.H.Armories[1].Reloads[1];
        Check(p.H.Receive(p.C.Packet(), 20, p.Now) && p.H.Armories[1].Reloads[1] == reloads,
              "reload retry cannot extend duration");
        p.Exchange();
        p.C.Observe(3, p.P[1], 450, true, false, p.Now + .01, .01);
        p.H.Receive(p.C.Packet(), 20, p.Now + .01);
        Check(p.H.Armories[1].Rounds[1] == 29 && p.H.Armories[1].Fired[1] == 1,
              "host rejects fire during reload");
        p.P[1].Weapon = 2;
        p.Step(.01, false, false, false, false);
        Check(p.H.Armories[1].Reloading == 0 && p.H.Armories[1].Rounds[1] == 29,
              "LAN switch cancels reload with no refill");
        p.P[1].Weapon = 1;
        p.Wait(.3);
        p.C.Observe(3, p.P[1], 450, true, false, p.Now, .01);
        Check(p.H.Receive(p.C.Packet(), 20, p.Now), "fire accepted snapshot");
        // A hostile client can reset its local timer, but not the host cadence.
        p.C.Armories[1] = new WeaponAmmo();
        p.C.Observe(3, p.P[1], 450, true, false, p.Now + .001, .001);
        p.H.Receive(p.C.Packet(), 20, p.Now + .001);
        Check(p.H.Armories[1].Fired[1] <= 2, "host owns shot cadence");
    }
    static void HudAndAudio(string directory)
    {
        byte[][] banks = { AmmoHud.Build(), AmmoHud.BuildReload(), AmmoHud.BuildReserve() };
        int[] strides = { 4096, 1024, 4096 };
        for (int n = 0; n < banks.Length; n++)
            for (int at = 0; at < banks[n].Length; at += strides[n])
            {
                int spans = BitConverter.ToInt32(banks[n], at);
                Check(spans <= 200 && spans * 20 + 4 <= strides[n], "ammo spans bounded");
                for (int i = 0; i < spans; i++)
                {
                    int a = at + 4 + i * 20, x = BitConverter.ToInt32(banks[n], a),
                        y = BitConverter.ToInt32(banks[n], a + 4), xx = BitConverter.ToInt32(banks[n], a + 8),
                        yy = BitConverter.ToInt32(banks[n], a + 12);
                    Check(x >= 723 && xx <= 787 && y >= 16 && yy <= 56 && xx > x && yy > y,
                          "all ammo ink inside upper-right HUD");
                }
            }
        foreach (string file in CombatAudio.Files)
        {
            short[] s = CombatAudio.Load(Path.Combine(directory, file));
            Check(s.Length > 4000 && s.Length < 44100 * 3, "short native PCM loads");
            long energy = 0;
            foreach (short v in s)
                energy += (long)v * v;
            Check(energy > 1000000, "effect not silent");
        }
        Console.WriteLine("PASS " + checks +
                          (" ammo checks; magazines, empty, reload, switch/cadence, both LAN " +
                           "hosts/weapons, retries/authority, pixel HUD and PCM"));
    }
    internal static void ExportHud(string folder)
    {
        Directory.CreateDirectory(folder);
        File.WriteAllBytes(Path.Combine(folder, "ammo-bank.bin"), AmmoHud.Build());
        File.WriteAllBytes(Path.Combine(folder, "reserve-bank.bin"), AmmoHud.BuildReserve());
        File.WriteAllBytes(Path.Combine(folder, "reload-bank.bin"), AmmoHud.BuildReload());
        File.WriteAllBytes(Path.Combine(folder, "weapon-bank.bin"), WeaponHud.Build());
    }
    sealed class Delivery
    {
        internal double At;
        internal string Packet;
        internal bool Host;
    }
    static void Jitter()
    {
        var h = TestDuel.Create(true, 0, 450, TestMap.Empty);
        var c = TestDuel.Create(false, 1, 450, TestMap.Empty);
        var messages = new System.Collections.Generic.List<Delivery>();
        var police = new DuelPose { X = 400, Y = 500, Heading = 0, Weapon = 1 };
        var thief = new DuelPose { X = 1000, Y = 500, Heading = 0, Weapon = 1 };
        double nextH = 0, nextC = 0, first = -1, last = -1;
        uint seen = 0;
        bool loaded = false;
        for (int i = 0; i < 4000; i++)
        {
            double t = i * .0025;
            bool fire = t >= 1;
            if (!loaded && h.Playing && c.Playing)
            {
                TestLoadout.Fill(h, t + 1000);
                loaded = true;
            }
            if (i % 2 == 0)
                h.Observe(3, police, 450, fire, false, t + 1000, .005);
            if (i % 3 == 0)
                c.Observe(3, thief, 450, fire, false, t + 37, .0075);
            if (t >= nextH)
            {
                messages.Add(
                    new Delivery { At = t + .003 + (i % 7) * .002, Packet = h.Packet(), Host = true });
                nextH = t + .04;
            }
            if (t >= nextC)
            {
                messages.Add(new Delivery { At = t + .004 + (i % 5) * .003, Packet = c.Packet() });
                nextC = t + .04;
            }
            for (int n = messages.Count - 1; n >= 0; n--)
                if (messages[n].At <= t)
                {
                    var m = messages[n];
                    Check(m.Host ? c.Receive(m.Packet, 10, t + 37) : h.Receive(m.Packet, 20, t + 1000),
                          "delayed packet accepted with independent clocks");
                    messages.RemoveAt(n);
                }
            if (h.Armories[1].Fired[1] != seen)
            {
                if (first < 0)
                    first = t;
                last = t;
                seen = h.Armories[1].Fired[1];
            }
        }
        Check(h.Armories[0].Fired[1] == 30 && h.Armories[1].Fired[1] == 30,
              "40ms packets and jitter consume both clips");
        Check(c.Armories[0].Rounds[1] == 0 && c.Armories[1].Rounds[1] == 0,
              "delayed ammo snapshots converge");
        Check(last - first >= 29 * .18 - .045 - .01 && last - first < 5.8,
              "jitter credit cannot increase sustained cadence or stall client");
        var a = new WeaponAmmo();
        a.Rounds[1] = 30;
        int fired = 0;
        for (int i = 0; i < 1000; i++)
            if (a.Fire(1, i * .001, .045))
                fired++;
        Check(fired <= 6, "rapid malicious requests cannot accumulate jitter credit");
    }
    internal static void RunSuite(string[] args)
    {
        try
        {
            Basic();
            foreach (int host in new[] { 0, 1 })
                foreach (int weapon in new[] { 1, 2 })
                    Network(host, weapon);
            RetryAndCadence();
            Jitter();
            HudAndAudio(args[0]);
        }
        catch (Exception e)
        {
            Console.WriteLine("FAIL after " + checks + " checks: " + e);
            Environment.ExitCode = 1;
        }
    }
}
