using TypeGameMod;
using System;
class DuelTests
{
    static int checks;
    static void Check(bool ok, string text)
    {
        checks++;
        if (!ok)
            throw new Exception(text);
    }
    static void Exchange(DuelSession h, DuelSession c, double t)
    {
        c.Receive(h.Packet(), 10, t);
        h.Receive(c.Packet(), 20, t);
    }
    static void Start(DuelSession h, DuelSession c, ref double t)
    {
        for (int i = 0; i < 8; i++)
        {
            h.Observe(3, DuelPose.Spawn(h.Role), h.Speed, false, false, t, .02);
            c.Observe(3, DuelPose.Spawn(c.Role), c.Speed, false, false, t, .02);
            Exchange(h, c, t);
            t += .02;
        }
        Check(h.Playing && c.Playing, "handshake and round acknowledgement");
        TestLoadout.Fill(h, t);
        c.Receive(h.Packet(), 10, t);
    }
    static DuelPose P(float x, float y, int heading, int weapon)
    {
        return new DuelPose { X = x, Y = y, Heading = (byte)heading, Weapon = (byte)weapon };
    }
    static void Match(int hostRole, int owner, int weapon, int speed)
    {
        var h = TestDuel.Create(true, hostRole, speed, TestMap.Empty);
        var c = TestDuel.Create(false, 1 - hostRole, speed, TestMap.Empty);
        double t = 0;
        Start(h, c, ref t);
        DuelPose police = P(500, 500, 2, weapon), thief = P(700, 500, 6, weapon);
        for (int i = 0; i < 1500 && h.Winner < 0; i++)
        {
            DuelPose hp = hostRole == 0 ? police : thief, cp = hostRole == 0 ? thief : police;
            c.Observe(3, cp, speed, c.Role == owner, false, t, .01);
            Exchange(h, c, t);
            h.Observe(3, hp, speed, h.Role == owner, false, t, .01);
            Exchange(h, c, t);
            t += .01;
        }
        Check(h.Winner == owner && c.Winner == owner,
              "winner both views " + hostRole + "/" + owner + "/" + weapon + "/" + speed);
        Check(h.RequestedScene == (hostRole == owner ? 4 : 5) &&
                  c.RequestedScene == (c.Role == owner ? 4 : 5),
              "native role-aware victory/loss");
        Check(h.Shots.Count == 0 && c.Shots.Count == 0, "clear projectiles after result");
        Check(c.Speed == speed, "host shared projectile speed");
        uint round = h.Round;
        h.Observe((uint)h.RequestedScene, police, speed, false, true, t, .01);
        c.Observe((uint)c.RequestedScene, thief, speed, false, false, t, .01);
        Exchange(h, c, t);
        Check(h.Round == round, "one ready does not restart");
        c.Ready();
        Exchange(h, c, t);
        h.Observe((uint)h.RequestedScene, police, speed, false, false, t, .01);
        Exchange(h, c, t);
        Check(h.Round == round + 1 && c.Round == round + 1, "both ready restart one shared round");
        Check(h.RequestedScene == 3 && c.RequestedScene == 3, "both native scene3 reset requests");
    }
    static void Run()
    {
        foreach (int host in new[] { 0, 1 })
            foreach (int owner in new[] { 0, 1 })
                foreach (int gun in new[] { 1, 2 })
                    foreach (int speed in new[] { 100, 450, 900, 2400 })
                        Match(host, owner, gun, speed);
        var h = TestDuel.Create(true, 0, 900, TestMap.Empty);
        var c = TestDuel.Create(false, 1, 900, TestMap.Empty);
        double t = 0;
        Start(h, c, ref t);
        string packet = c.Packet();
        Check(h.Receive(packet, 20, t), "valid state");
        Check(!h.Receive(packet, 20, t), "reject replayed sequence");
        Check(!h.Receive(c.Packet(), 99, t), "reject unexpected DirectPlay sender");
        Check(!h.Receive("TGDUEL19:AAAA", 20, t), "reject truncated packet");
        Check(!h.Receive("TGDUEL19:!!!!", 20, t), "reject malformed base64");
        var wrong = TestDuel.Create(false, 0, 900, TestMap.Empty);
        Check(!h.Receive(wrong.Packet(), 20, t), "reject equal role");
        var impostor = TestDuel.Create(false, 1, 900, TestMap.Empty);
        Check(!h.Receive(impostor.Packet(), 20, t), "reject replaced session nonce");
        Check(!h.Connected(t + 3.1), "peer timeout");
        h.Observe(3, P(500, 500, 2, 1), 900, false, false, t + 3.1, .02);
        Check(h.Shots.Count == 0, "timeout clears projectiles");
        // Repeated client fire commands are acknowledged once, even when snapshots repeat.
        h = TestDuel.Create(true, 0, 900, TestMap.Empty);
        c = TestDuel.Create(false, 1, 900, TestMap.Empty);
        t = 0;
        Start(h, c, ref t);
        c.Observe(3, P(1000, 400, 0, 2), 900, true, false, t, .01);
        string fire = c.Packet();
        h.Receive(fire, 20, t);
        uint pellets = h.PelletsFired;
        h.Receive(fire, 20, t);
        h.Receive(c.Packet(), 20, t);
        Check(pellets == 6 && h.PelletsFired == 6, "exactly once six-pellet remote fire");
        Exchange(h, c, t);
        Check(c.Packet().Length < 4095, "bounded packet");
        // A host snapshot with all 64 tracers fits inside the original transport mailbox.
        h.Shots.Clear();
        for (int i = 0; i < 64; i++)
            h.Shots.Add(new DuelShot { X = 700, Y = 400, DX = 1, DY = 0, Owner = 0, Weapon = 1 });
        Check(h.Packet().Length < 4095, "maximum projectile snapshot bound");
        // Escape returns both players to the existing room instead of reopening a result.
        c.Observe(1, DuelPose.Spawn(1), 900, false, false, t, .01);
        Exchange(h, c, t);
        Check(h.RequestedScene == 1, "client Escape returns host to room");
        h.Observe(1, DuelPose.Spawn(0), 900, false, false, t, .01);
        Exchange(h, c, t);
        Check(c.RequestedScene == 1 && h.Winner == -1 && h.Shots.Count == 0,
              "shared lobby clears result and projectiles");
        uint beforeRound = h.Round;
        h.Observe(3, DuelPose.Spawn(0), 900, false, false, t, .01);
        Exchange(h, c, t);
        c.Observe(3, DuelPose.Spawn(1), 900, false, false, t, .01);
        Exchange(h, c, t);
        Check(h.Round == beforeRound + 1 && c.Round == h.Round,
              "host can start a fresh round after Escape: before=" + beforeRound + " host=" + h.Round +
                  " client=" + c.Round);
        Console.WriteLine("PASS " + checks +
                          (" checks; 32 host/role/weapon/speed matches; shared rematches, session " +
                           "validation, timeout, duplicate fire, packet bounds."));
    }
    internal static void RunSuite(string[] args)
    {
        try
        {
            Run();
        }
        catch (Exception e)
        {
            Console.WriteLine("FAIL: " + e.Message);
            Environment.ExitCode = 1;
        }
    }
}
