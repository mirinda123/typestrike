using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using TypeGameMod;

// Compile this identical deterministic driver against both delivered assemblies.
// It never launches a game, opens a socket or accesses another process.
internal static partial class BehaviorProbe
{
    const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    static BinaryWriter output;
    static object Get(object target, string field)
    {
        return target.GetType().GetField(field, Fields).GetValue(target);
    }
    static void Set(object target, string field, object value)
    {
        target.GetType().GetField(field, Fields).SetValue(target, value);
    }
    static object Call(object target, string method, params object[] args)
    {
        return target.GetType().GetMethod(method, Fields).Invoke(target, args);
    }
    static void Bytes(byte[] bytes)
    {
        output.Write(bytes.Length);
        output.Write(bytes);
    }
    static void Save(string name, Action run)
    {
        using (var stream = new MemoryStream())
        {
            output = new BinaryWriter(stream);
            run();
            output.Flush();
            using (var sha = SHA256.Create()) Console.WriteLine(
                name + " " + stream.Length + " " +
                BitConverter.ToString(sha.ComputeHash(stream.ToArray())).Replace("-", "").ToLowerInvariant());
        }
    }
    static int Main()
    {
        Save("ammo-20000-actions", Ammo);
        Save("skills-12000-actions", Skills);
        Save("offline-16000-steps", Offline);
        Save("duel-12000-exchanges", Network);
        Save("directional-sprite-uploads", Sprites);
        Save("animation-4800-frames", Animations);
        Save("skill-view-2000-frames", SkillViews);
        Save("muzzle-directions-poses", Muzzles);
        Save("embedded-resources", Resources);
        Save("native-two-relocation-bases", Native);
        return 0;
    }
    static DuelSession NewDuel(bool host, int role, int speed)
    {
#if CURRENT
        return new DuelSession(host, role, speed, delegate(int owner, DuelPose pose) {
            double x, y;
            WeaponMuzzle.Offset(owner, pose.Weapon, pose.Heading, pose.Moving != 0, 0, out x, out y);
            return new ShotOrigin(pose.X + x, pose.Y + y);
        });
#else
        return new DuelSession(host, role, speed);
#endif
    }

    static void Muzzles()
    {
        for (int role = 0; role < 2; role++)
            for (int weapon = 1; weapon <= 2; weapon++)
                for (int heading = 0; heading < 8; heading++)
                    foreach (bool moving in new[] { false, true })
                        for (int frame = 0; frame < (moving        ? 24
                                                     : role == 0   ? 9
                                                     : weapon == 1 ? 29
                                                                   : 33);
                             frame++)
                        {
                            double x, y;
#if CURRENT
                            WeaponMuzzle.Offset(role, weapon, heading, moving, frame, out x, out y);
#else
                            Ballistics.Muzzle((uint)role, weapon == 2, !moving,
                                              moving ? heading * 4 + 3 : heading, frame, frame, frame, frame,
                                              out x, out y);
#endif
                            output.Write(x);
                            output.Write(y);
                        }
    }

    static void SkillViews()
    {
        var session = animationSession;
        Set(session, "networkMatch", null);
        double now = 0;
        for (int index = 0; index < 2000; index++)
        {
            now += .008;
            uint role = (uint)(index / 100 % 2);
            int weapon = index / 7 % 3;
            var skill = new RoleSkill((int)role) { Energy = role == 1 ? index % 7 : index % 61 / 10.0,
                                                   BlinkSerial = (uint)index,
                                                   BlinkX = 840,
                                                   BlinkY = 500,
                                                   GhostX = 730,
                                                   GhostY = 460,
                                                   BlinkHeading = (byte)(index % 8),
                                                   FlashUntil = now + .2 };
            skill.Hold(role == 0 && index % 9 == 0, now);
            Set(session, "offlineSkill", skill);
            Set(session, "equippedWeapon", weapon);
            var article = new ArticleProgress("one two three four five six", index % 20, 0);
#if CURRENT
            var typing = (TypingController)Get(session, "typing");
            typing.SetArticle(article);
            if (typing.Enabled != (index % 3 == 0))
                typing.HandleKey(9, article.Position, false, now);
            var power = typing.Feedback;
#else
            Set(session, "articleProgress", article);
            ((TypingMode)Get(session, "skillTyping")).Enabled = index % 3 == 0;
            var power = (TypingPowerMode)Get(session, "typingPower");
#endif
            if (index % 4 == 0)
                power.Stroke(index % 5 != 0, now);
            var snapshot = new byte[ModSession.SnapshotBytes];
            Call(session, "SkillSnapshot", snapshot, role, index % 21 != 0, 700.0, 490.0, now);
            Bytes(snapshot);
            var control = new byte[1024];
            Marshal.Copy(Win32.Address(session.ControlBase), control, 0, control.Length);
            Bytes(control);
        }
    }
    static void Resources()
    {
        Assembly assembly = typeof(ModSession).Assembly;
        string[] names = assembly.GetManifestResourceNames();
        Array.Sort(names, StringComparer.Ordinal);
        foreach (string name in names)
        {
            output.Write(name);
            using (Stream resource = assembly.GetManifestResourceStream(name)) using (
                var reader = new BinaryReader(resource)) Bytes(reader.ReadBytes((int)resource.Length));
        }
    }
    static void Native()
    {
        Bytes(NativePayload.Build(0x30000000, 0x400000));
        Bytes(NativePayload.Build(0x25000000, 0x1400000));
    }
    static void Ammo()
    {
        var ammo = new WeaponAmmo();
        var random = new Random(1812);
        double now = 0;
        for (int index = 0; index < 20000; index++)
        {
            now += random.Next(1, 500) / 1000.0;
            int weapon = random.Next(3);
            switch (random.Next(7))
            {
            case 0:
                output.Write(ammo.Fire(weapon, now, .03));
                break;
            case 1:
                output.Write(ammo.Reload(weapon, now));
                break;
            case 2:
                output.Write(ammo.Supply(weapon, now));
                break;
            case 3:
                ammo.Select(weapon, now);
                break;
            case 4:
                output.Write(ammo.CanFire(weapon, now));
                break;
            case 5:
                output.Write(ammo.CanRequest(weapon, now));
                break;
            case 6:
                if (index % 97 == 0)
                    ammo.Reset();
                break;
            }
            ammo.Write(output, now);
            output.Write(ammo.Progress(now));
        }
    }
    static void Skills()
    {
        var random = new Random(256);
        for (int role = 0; role < 2; role++)
        {
            var skill = new RoleSkill(role);
            var ammo = new WeaponAmmo();
            double now = 0;
            DuelPose pose = DuelPose.Spawn(role);
            for (int index = 0; index < 6000; index++)
            {
                now += random.Next(1, 200) / 1000.0;
                pose.Heading = (byte)random.Next(8);
                switch (random.Next(6))
                {
                case 0:
                    output.Write(skill.Match(skill.Word, now));
                    break;
                case 1:
                    skill.Hold(random.Next(2) == 1, now);
                    break;
                case 2:
                    DuelPose target;
                    output.Write(skill.Use(pose, now, MovementMap.City, out target));
                    pose = target;
                    break;
                case 3:
                    output.Write(skill.Resupply(ammo, random.Next(1, 3), now));
                    break;
                case 4:
                    output.Write(skill.Match("wrong", now));
                    break;
                case 5:
                    if (index % 37 == 0)
                        skill.Reset();
                    break;
                }
                skill.Write(output, now);
                output.Write(pose.X);
                output.Write(pose.Y);
            }
        }
    }
    static void Offline()
    {
        var random = new Random(7331);
        var match = new OfflineMatch();
        match.Reset(1140, 360);
        double now = 0;
        for (int index = 0; index < 16000; index++)
        {
            double dt = random.Next(1, 50) / 1000.0;
            now += dt;
            if (index % 300 == 0)
                match.Reset(1140, 360);
            if (index % 100 == 0)
                match.Select(random.Next(1, 3), now);
            if (index % 19 == 0)
                match.Ammo.Supply(match.Weapon, now);
            if (index % 71 == 0)
                match.Ammo.Reload(match.Weapon, now);
            output.Write(
                match.Fire((uint)random.Next(8), 1000 + random.Next(200), 400 + random.Next(300), now));
            output.Write(
                match.Advance(index & 1, 1140 + random.Next(30), 360 + random.Next(300), 450, now, dt));
            match.Ammo.Write(output, now);
            match.Health.Write(output, now);
            output.Write(match.ShotsFired);
            output.Write(match.Hits);
            output.Write(match.PelletsFired);
            output.Write(match.Projectiles.Count);
            foreach (OfflineMatch.Projectile shot in match.Projectiles)
            {
                output.Write(shot.X);
                output.Write(shot.Y);
                output.Write(shot.DX);
                output.Write(shot.DY);
                output.Write(shot.Age);
                output.Write(shot.Distance);
            }
        }
    }
    static void Network()
    {
        for (int hostRole = 0; hostRole < 2; hostRole++)
        {
            var host = NewDuel(true, hostRole, 450);
            var client = NewDuel(false, 1 - hostRole, 450);
            Set(host, "Token", new Guid("11111111-1111-1111-1111-111111111111"));
            Set(client, "Token", new Guid("22222222-2222-2222-2222-222222222222"));
            var random = new Random(100 + hostRole);
            double now = 0;
            for (int index = 0; index < 6000; index++)
            {
                now += .02;
                foreach (DuelSession session in new[] { host, client })
                {
                    int role = session.Role;
                    var pose = new DuelPose { X = role == 0 ? 1000 : 1140, Y = 450,
                                              Heading = role == 0 ? (byte)2 : (byte)6,
                                              Weapon = (byte)(1 + index / 200 % 2),
                                              Moving = (byte)(index / 40 % 2) };
                    if (index % 9 == 0)
                        output.Write(session.ConfirmSkillWord(session.Skills[role].Word, now));
                    session.SkillKeys(index % 300 < 30, index % 31 == 0);
                    session.SupplyKey(index % 27 == 0);
                    session.ReloadKey(index % 113 == 0);
                    uint scene = index % 1000 < 10    ? 1u
                                 : session.Winner < 0 ? 3u
                                                      : (uint)(session.Winner == role ? 4 : 5);
                    session.Observe(scene, pose, 100 + random.Next(24) * 100, index % 3 == 0, index % 7 == 0,
                                    now, .02);
                }
                string a = host.Packet(), b = client.Packet();
                output.Write(a);
                output.Write(b);
                if (index % 11 != 0)
                    output.Write(client.Receive(a, 10, now));
                if (index % 13 != 0)
                    output.Write(host.Receive(b, 20, now));
                if (index % 17 == 0)
                    output.Write(host.Receive(b, 20, now));
                // PelletsFired is a derived local diagnostic, never a packet field.
                // Its old client 7->6 reconstruction is checked separately.
                output.Write(host.Hits);
                output.Write(client.Hits);
                output.Write(host.Blocks);
                output.Write(host.RequestedScene);
                output.Write(client.RequestedScene);
            }
        }
    }
    static ModSession AllocateSession(bool assets)
    {
        var session = new ModSession(null, delegate(string message) {});
        Set(session, "processHandle", new IntPtr(-1));
        if (Win32.VirtualAllocEx(new IntPtr(-1), new IntPtr(0x20000000), 65536, 0x3000, 4).ToInt32() !=
            0x20000000)
            throw new Exception("Cannot allocate own fixed cave");
        Set(session, "caveBase", 0x20000000u);
        if (assets)
        {
            if (Win32
                    .VirtualAllocEx(new IntPtr(-1), new IntPtr(0x30000000), AssetLayout.AllocationBytes,
                                    0x3000, 4)
                    .ToInt32() != 0x30000000)
                throw new Exception("Cannot allocate own fixed asset buffer");
            Set(session, "assetBase", 0x30000000u);
        }
#if CURRENT
        Call(session, "InitializeNativeBridges");
#endif
        return session;
    }
    static void Sprites()
    {
        var session = AllocateSession(true);
        var initial = new byte[1024];
        foreach (string name in new[] { "PreparePoliceRun", "PreparePoliceShieldRun",
                                        "PreparePoliceShotgunRun", "PreparePoliceAkRun", "PrepareThiefRun",
                                        "PrepareThiefIdle", "PrepareThiefAkPose", "PrepareThiefAkRun",
                                        "PrepareThiefShotgunRun", "PrepareThiefShotgunPose" })
        {
            if (name == "PreparePoliceAkRun")
                Call(session, name);
            else
                Call(session, name, initial);
        }
        Bytes(initial);
        var bytes = new byte[AssetLayout.AllocationBytes];
        Marshal.Copy(new IntPtr(0x30000000), bytes, 0, bytes.Length);
        Bytes(bytes);
        // Reuse this cave for animation checks; all writes are to our own process.
        animationSession = session;
    }
    static ModSession animationSession;
    static void Animations()
    {
        var session = animationSession;
        var random = new Random(2408);
        var network = NewDuel(true, 0, 450);
        Set(session, "networkMatch", network);
        var offline = (OfflineMatch)Get(session, "offlineMatch");
        var skill = (RoleSkill)Get(session, "offlineSkill");
        uint[] counters = new uint[4];
        for (int index = 0; index < 4800; index++)
        {
            bool online = index / 800 % 2 == 1;
            var frame = new GameFrame { Scene = index % 101 == 0 ? 4u : 3u, Online = online,
                                        Role = (uint)(index / 400 % 2), Players = new byte[112] };
            var input = new InputFrame { Focused = index % 19 != 0, X = index / 70 % 2, Y = index / 91 % 2 };
            var snapshot = new byte[3584];
            BinaryFields.Write(snapshot, SnapshotLayout.GameObject, frame.Scene == 3 ? 1u : 0u);
            BinaryFields.Write(snapshot, SnapshotLayout.Typing, index / 31 % 2 == 0 ? 0u : 1u);
            for (int role = 0; role < 2; role++)
            {
                var pose = new DuelPose { X = 700 + role * 100, Y = 500, Weapon = (byte)(index / 60 % 3),
                                          Heading = (byte)(index / 29 % 8), Moving = (byte)(index / 83 % 2) };
                network.Poses[role] = pose;
                for (int weapon = 1; weapon <= 2; weapon++)
                {
                    if (index % (17 + role * 3 + weapon) == 0)
                        counters[role * 2 + weapon - 1]++;
                    network.Armories[role].Fired[weapon] = counters[role * 2 + weapon - 1];
                    if (role == frame.Role)
                        offline.Ammo.Fired[weapon] = counters[role * 2 + weapon - 1];
                }
                int offset = role * 56;
                BinaryFields.Write(frame.Players, offset + 4, pose.X);
                BinaryFields.Write(frame.Players, offset + 8, pose.Y);
                frame.Players[offset + 28] = (byte)(pose.Heading * 4 + 3);
            }
            skill.Shield = network.Skills[0].Shield = index / 23 % 2 == 0;
            Set(session, "equippedWeapon", index / 60 % 3);
            double dt = random.Next(1, 50) / 1000.0;
#if CURRENT
            Call(session, "UpdateAnimations", frame, input, snapshot, frame.Scene == 3, dt);
#else
            BaselineAnimation(session, frame, input, snapshot, frame.Scene == 3, dt);
#endif
            Bytes(snapshot);
            var control = new byte[1024];
            Marshal.Copy(Win32.Address(session.ControlBase), control, 0, control.Length);
            Bytes(control);
        }
    }
}
