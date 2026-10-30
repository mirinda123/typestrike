using System;
using System.IO;
using TypeGameMod;

internal static class TestRunner
{
    static int Main(string[] args)
    {
        if (args.Length == 1 && args[0] == "--inspect-lan")
        {
            LanDiscovery target = LanInterfaces.Discover();
            Console.WriteLine(target == null ? "No active physical LAN"
                                             : target.Adapter.Name + " " + target.Adapter.Address + " -> " +
                                                   target.Broadcast + " index=" + target.Adapter.Index);
            return target == null ? 1 : 0;
        }
        if (args.Length == 2 && args[0] == "--export-movement")
        {
            WorldCollisionTests.Export(args[1]);
            return 0;
        }
        if (args.Length == 2 && args[0] == "--export-ammo")
        {
            AmmoTests.ExportHud(args[1]);
            return 0;
        }
        if (args.Length == 2 && args[0] == "--export-energy")
        {
            EnergyHudTests.Export(args[1]);
            return 0;
        }
        if (args.Length == 2 && args[0] == "--export-police-shield-run")
        {
            Directory.CreateDirectory(args[1]);
            for (int heading = 0; heading < 8; heading++)
                for (int bank = 0; bank < 2; bank++)
                {
                    string name = "PoliceShieldRun" + heading + "_" + bank;
                    File.WriteAllBytes(Path.Combine(args[1], name + ".dib"), IdleAnimationTests.Source(name));
                }
            return 0;
        }
        if (args.Length == 2 && args[0] == "--export-police-shield-idle")
        {
            IdleAnimationTests.ExportPoliceShield(args[1]);
            return 0;
        }
        if (args.Length == 2 && args[0] == "--export-police-weapons")
        {
            PoliceWeaponOriginsTests.Export(args[1]);
            return 0;
        }
        if (args.Length == 2 && args[0] == "--export-police-shotgun-idle")
        {
            Directory.CreateDirectory(args[1]);
            var points = new System.Collections.Generic.List<string>();
            for (int heading = 0; heading < 8; heading++)
            {
                string name = "PoliceShotgunIdle" + heading;
                File.WriteAllBytes(Path.Combine(args[1], name + ".dib"), IdleAnimationTests.Source(name));
                for (int frame = 0; frame < PoliceShotgunIdle.Frames; frame++)
                {
                    double x, y;
                    WeaponMuzzle.Offset(0, GameRules.Shotgun, heading, false, frame, out x, out y);
                    points.Add(String.Format(System.Globalization.CultureInfo.InvariantCulture,
                                             "{0},{1},{2:R},{3:R},{4}", heading, frame, x, y,
                                             PoliceShotgunIdle.DrawOffsetX(heading)));
                }
            }
            File.WriteAllLines(Path.Combine(args[1], "muzzles.csv"), points.ToArray());
            return 0;
        }
        if (args.Length == 2 && args[0] == "--export-police-run")
        {
            Directory.CreateDirectory(args[1]);
            for (int heading = 0; heading < 8; heading++)
                for (int bank = 0; bank < 2; bank++)
                {
                    string name = "PoliceRun" + heading + "_" + bank;
                    File.WriteAllBytes(Path.Combine(args[1], name + ".dib"), IdleAnimationTests.Source(name));
                }
            return 0;
        }
        if (args.Length == 2 && args[0] == "--export-police-shotgun-run")
        {
            Directory.CreateDirectory(args[1]);
            var points = new System.Collections.Generic.List<string>();
            for (int heading = 0; heading < 8; heading++)
            {
                for (int bank = 0; bank < 2; bank++)
                {
                    string name = "PoliceShotgunRun" + heading + "_" + bank;
                    File.WriteAllBytes(Path.Combine(args[1], name + ".dib"), IdleAnimationTests.Source(name));
                }
                for (int frame = 0; frame < PoliceShotgunRun.Frames; frame++)
                {
                    double x, y;
                    WeaponMuzzle.Offset(0, GameRules.Shotgun, heading, true, frame, out x, out y);
                    points.Add(String.Format(System.Globalization.CultureInfo.InvariantCulture,
                                             "{0},{1},{2:R},{3:R},{4}", heading, frame, x, y,
                                             PoliceShotgunRun.DrawOffsetX(heading)));
                }
            }
            File.WriteAllLines(Path.Combine(args[1], "muzzles.csv"), points.ToArray());
            return 0;
        }
        if (args.Length == 2 && args[0] == "--export-police-ak-run")
        {
            Directory.CreateDirectory(args[1]);
            var points = new System.Collections.Generic.List<string>();
            for (int heading = 0; heading < 8; heading++)
            {
                for (int bank = 0; bank < 2; bank++)
                {
                    string name = "PoliceAkRun" + heading + "_" + bank;
                    File.WriteAllBytes(Path.Combine(args[1], name + ".dib"), IdleAnimationTests.Source(name));
                }
                for (int frame = 0; frame < PoliceAkRun.Frames; frame++)
                {
                    double x, y;
                    WeaponMuzzle.Offset(0, GameRules.Ak47, heading, true, frame, out x, out y);
                    points.Add(String.Format(System.Globalization.CultureInfo.InvariantCulture,
                                             "{0},{1},{2:R},{3:R}", heading, frame, x, y));
                }
            }
            File.WriteAllLines(Path.Combine(args[1], "muzzles.csv"), points.ToArray());
            return 0;
        }
        if (args.Length == 2 && args[0] == "--export-police-idle")
        {
            Directory.CreateDirectory(args[1]);
            for (int heading = 0; heading < 8; heading++)
                File.WriteAllBytes(Path.Combine(args[1], "PoliceIdle" + heading + ".dib"),
                                   IdleAnimationTests.Source("PoliceIdle" + heading));
            return 0;
        }
        if (args.Length == 2 && args[0] == "--export-typing")
        {
            TypingPromptTests.Export(args[1]);
            return 0;
        }
        if (args.Length == 3 && args[0] == "--export-idle")
        {
            IdleAnimationTests.Export(args[1], args[2]);
            return 0;
        }
        string audio = args.Length == 0
                           ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "audio", "weapons")
                           : args[0];
        Action<string[]>[] suites = { WeaponTests.RunSuite,   HealthTests.RunSuite,  SkillTests.RunSuite,
                                      DuelTests.RunSuite,     AmmoTests.RunSuite,    MusicTests.RunSuite,
                                      RefactorTests.RunSuite, ResupplyTests.RunSuite };
        foreach (Action<string[]> run in suites)
        {
            Environment.ExitCode = 0;
            run(new[] { audio });
            if (Environment.ExitCode != 0)
                return Environment.ExitCode;
        }
        if (args.Length > 1)
            CompatibilityTests.Run(args[1]);
        try
        {
            BundleTests.Run();
            LanDiscoveryTests.Run();
            SessionWorkerTests.Run();
            TypingControllerTests.Run();
            FireCompositionTests.Run();
            NativeBridgeTests.Run();
            LakeCollisionTests.Run();
            WaterCollisionTests.Run();
            FlowerbedCollisionTests.Run();
            WorldCollisionTests.Run();
            MapCoverTests.Run();
            ShotgunBalanceTests.Run();
            DebugSkillChargeTests.Run();
            IdleAnimationTests.Run();
            TypingPromptTests.Run();
            ThiefAkAnimationTests.Run();
            ThiefAkRunTests.Run();
            ThiefShotgunPoseTests.Run();
            ThiefShotgunRunTests.Run();
            ShotgunEffectsTests.Run();
            AkFireEffectsTests.Run();
            ThiefRunAnimationTests.Run();
            ThiefIdleAnimationTests.Run();
            PoliceIdleAnimationTests.Run();
            PoliceRunTests.Run();
            PoliceShieldRunTests.Run();
            PoliceAkIdleTests.Run();
            PoliceAkRunTests.Run();
            PoliceFireEffectsTests.Run();
            PoliceShotgunIdleTests.Run();
            PoliceShotgunRunTests.Run();
            PoliceShotgunRuntimeTests.Run();
            PoliceWeaponOriginsTests.Run();
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error.GetType().Name + ": " + error.Message);
            return 1;
        }
        return 0;
    }
}
