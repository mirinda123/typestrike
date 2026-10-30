using TypeGameMod;
using System;
using System.IO;

internal static class HealthTests
{
    static int checks;
    static void Check(bool yes, string why)
    {
        checks++;
        if (!yes)
            throw new Exception(why);
    }
    sealed class Pair
    {
        internal DuelSession H, C;
        internal double T;
        internal readonly DuelPose[] P = { new DuelPose { X = 400, Y = 500, Heading = 2 },
                                           new DuelPose { X = 700, Y = 500, Heading = 6 } };
        internal Pair(int hostRole)
        {
            H = TestDuel.Create(true, hostRole, 450, TestMap.Empty);
            C = TestDuel.Create(false, 1 - hostRole, 450, TestMap.Empty);
            for (int i = 0; i < 12; i++)
                Step(.02);
            Check(H.Playing && C.Playing, "health test pair connected");
        }
        internal void Exchange()
        {
            Check(H.Receive(C.Packet(), 20, T), "accept client");
            Check(C.Receive(H.Packet(), 10, T), "accept host");
        }
        internal void Step(double dt)
        {
            T += dt;
            C.Observe(3, P[C.Role], 450, false, false, T, dt);
            Exchange();
            H.Observe(3, P[H.Role], 450, false, false, T, dt);
            Exchange();
        }
        internal void Contact(int owner, int pellets, int weapon)
        {
            for (int i = 0; i < pellets; i++)
                H.Shots.Add(new DuelShot { X = P[1 - owner].X + 30, Y = P[1 - owner].Y + 40, DX = 1, DY = 0,
                                           Owner = (byte)owner, Weapon = (byte)weapon });
            Step(.001);
        }
    }
    static void Basic()
    {
        var h = new DuelHealth();
        Check(h.Hearts[0] == 3 && h.Hearts[1] == 3, "both start with3hearts");
        Check(h.Hit(0, 10) && h.Hearts[0] == 2 && h.Hearts[1] == 3, "one bullet removes exactly one heart");
        Check(h.Remaining(0, 10) == 500, "grace exactly500ms");
        Check(!h.Hit(0, 10) && !h.Hit(0, 10.499) && h.Hearts[0] == 2, "same tick and499ms blocked");
        Check(h.Hit(1, 10.1) && h.Hearts[1] == 2, "invulnerability is per player");
        Check(h.Hit(0, 10.5) && h.Hearts[0] == 1, "damage returns exactly at500ms");
        Check(h.Hit(0, 11) && h.Hearts[0] == 0, "third hit depletes hearts");
        Check(!h.Hit(0, 12) && h.Hearts[0] == 0 && h.Remaining(0, 12) == 0, "dead cannot go negative");
        h.Reset();
        Check(h.Hearts[0] == 3 && h.Hearts[1] == 3 && h.Remaining(0, 0) == 0,
              "reset clears health and grace");
        Check(!h.Hit(-1, 0) && !h.Hit(2, 0), "invalid role rejected");
        h.Hit(1, 20);
        using (var m = new MemoryStream()) using (var w = new BinaryWriter(m))
        {
            h.Write(w, 20.2);
            m.Position = 0;
            var copy = DuelHealth.Read(new BinaryReader(m), 100);
            Check(copy.Hearts[0] == 3 && copy.Hearts[1] == 2, "HP roundtrip");
            Check(copy.Remaining(1, 100) >= 300 && copy.Remaining(1, 100) <= 301,
                  "remaining grace uses receiver clock");
        }
        foreach (byte[] bytes in new[] { new byte[] { 4, 0, 0, 3, 0, 0 }, new byte[] { 3, 245, 1, 3, 0, 0 },
                                         new byte[] { 0, 1, 0, 3, 0, 0 } })
        {
            bool rejected = false;
            try
            {
                DuelHealth.Read(new BinaryReader(new MemoryStream(bytes)), 0);
            }
            catch (FormatException)
            {
                rejected = true;
            }
            Check(rejected, "malformed health rejected");
        }
    }
    static void Network(int hostRole, int owner, int weapon)
    {
        var p = new Pair(hostRole);
        p.Contact(owner, weapon == 2 ? 6 : 1, weapon);
        int target = 1 - owner;
        Check(p.H.Health.Hearts[target] == 2 && p.C.Health.Hearts[target] == 2,
              "first contact mirrored, no one-shot kill");
        Check(p.H.Winner < 0 && p.C.Winner < 0 && p.H.Hits == 1 && p.C.Hits == 1,
              "first damage stays in round");
        Check(p.H.Shots.Count == 0 && p.C.Shots.Count == 0, "all contacting pellets consumed");
        p.Contact(owner, 6, 2);
        Check(p.H.Health.Hearts[target] == 2 && p.C.Health.Hearts[target] == 2,
              "burst in grace does no extra damage");
        for (int i = 0; i < 9; i++)
            p.Step(.05);
        p.Contact(owner, 1, 1);
        Check(p.H.Health.Hearts[target] == 2, "450ms still protected");
        p.Step(.05);
        p.Contact(owner, 1, 1);
        Check(p.H.Health.Hearts[target] == 1 && p.C.Health.Hearts[target] == 1,
              "second hit after500ms mirrored");
        for (int i = 0; i < 11; i++)
            p.Step(.05);
        p.Contact(owner, 6, 2);
        Check(p.H.Health.Hearts[target] == 0 && p.C.Health.Hearts[target] == 0,
              "third accepted damage reaches zero");
        Check(p.H.Winner == owner && p.C.Winner == owner, "authoritative winner matches on both peers");
        Check(p.H.RequestedScene == (p.H.Role == owner ? 4 : 5) &&
                  p.C.RequestedScene == (p.C.Role == owner ? 4 : 5),
              "existing role-aware result scenes");
        p.C.Ready();
        p.Exchange();
        p.H.Observe((uint)p.H.RequestedScene, p.P[p.H.Role], 450, false, true, p.T, .01);
        p.Exchange();
        Check(p.H.Round == 2 && p.C.Round == 2, "shared rematch starts round2");
        Check(p.H.Health.Hearts[0] == 3 && p.H.Health.Hearts[1] == 3 && p.C.Health.Hearts[0] == 3 &&
                  p.C.Health.Hearts[1] == 3,
              "rematch restores both players to3");
        Check(p.H.Health.Remaining(0, p.T) == 0 && p.H.Health.Remaining(1, p.T) == 0,
              "rematch clears invulnerability");
    }
    static void AuthorityAndShield()
    {
        var p = new Pair(0);
        p.C.Health.Hit(0, p.T);
        p.C.Health.Hit(1, p.T);
        p.Exchange();
        Check(p.H.Health.Hearts[0] == 3 && p.H.Health.Hearts[1] == 3, "client cannot author damage");
        Check(p.C.Health.Hearts[0] == 3 && p.C.Health.Hearts[1] == 3, "host corrects client HP");
        p.H.Skills[0].Energy = 4;
        p.H.SkillKeys(true, true);
        p.Step(.01);
        p.H.SkillKeys(true, false);
        p.Contact(1, 6, 2);
        Check(p.H.Blocks == 6 && p.H.Health.Hearts[0] == 3, "shield blocks every pellet without damage");
        Check(p.H.Health.Remaining(0, p.T) == 0, "shield blocks do not start grace");
        string packet = p.C.Packet();
        byte[] raw = Convert.FromBase64String(packet.Substring(9));
        raw[4] = 2;
        Check(!p.H.Receive("TGDUEL19:" + Convert.ToBase64String(raw), 20, p.T),
              "previous wire version2 rejected");
        Check(p.H.Packet().Length <= 4093, "new health protocol fits mailbox");
        byte[] bank = HealthHud.Build();
        Check(bank.Length == 0x10000, "heart bank size bounded");
        for (int i = 0; i < 16; i++)
        {
            int at = i * 4096, count = BitConverter.ToInt32(bank, at);
            Check(count > 0 && count <= 200, "heart state bounded");
            for (int n = 0; n < count; n++)
            {
                int s = at + 4 + n * 20, x0 = BitConverter.ToInt32(bank, s),
                    y0 = BitConverter.ToInt32(bank, s + 4), x1 = BitConverter.ToInt32(bank, s + 8),
                    y1 = BitConverter.ToInt32(bank, s + 12);
                Check(x0 >= 12 && x1 <= 98 && y0 >= 12 && y1 <= 54 && x1 > x0 && y1 - y0 == 2,
                      "transparent span inside HUD");
            }
        }
    }
    internal static void RunSuite(string[] args)
    {
        try
        {
            Basic();
            foreach (int h in new[] { 0, 1 })
                foreach (int o in new[] { 0, 1 })
                    foreach (int w in new[] { 1, 2 })
                        Network(h, o, w);
            AuthorityAndShield();
            Console.WriteLine("PASS " + checks +
                              (" health checks; 3HP,500ms grace, both roles/hosts/weapons, authority, " +
                               "shield, rematch and pixel HUD"));
        }
        catch (Exception e)
        {
            Console.WriteLine("FAIL after " + checks + " checks: " + e.Message);
            Environment.ExitCode = 1;
        }
    }
}
