using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using TypeGameMod;

internal static class RefactorTests
{
    static int checks;
    static void Check(bool condition, string reason)
    {
        checks++;
        if (!condition)
            throw new Exception(reason);
    }

    internal static void RunSuite(string[] args)
    {
        var keys = new HashSet<int>();
        var keyboard = new KeyboardInput(keys.Contains);
        keys.Add(0x57);
        keys.Add(0x44);
        keys.Add(0x31);
        keys.Add(0x52);
        InputFrame input = keyboard.Read(true, true);
        Check(input.X == 1 && input.Y == -1 && input.Heading == 1, "diagonal WASD heading");
        Check(input.Selection == 1 && input.Reload, "weapon and reload rising edges");
        input = keyboard.Read(true, true);
        Check(input.Selection == 0 && !input.Reload, "held keys do not repeat actions");
        input = keyboard.Read(true, false);
        Check(input.X == 0 && input.Y == 0 && !input.Fire && input.Heading == 1,
              "typing keeps heading and blocks movement");
        keys.Clear();
        keyboard.Read(true, true);
        keys.Add(0x10);
        Check(keyboard.Read(true, true).ShiftPressed, "Shift activates once");
        Check(!keyboard.Read(true, true).ShiftPressed, "holding Shift does not repeat blink");
        Check(!keyboard.Read(false, false).ShiftHeld, "unfocused game cannot hold a shield");

        keys.Clear();
        keyboard.Read(true, true);
        keys.Add(0x20);
        Check(!keyboard.Read(true, true).Fire, "article separator never fires a weapon");
        keys.Clear();
        keys.Add(0x0d);
        Check(keyboard.Read(true, true).Fire, "Enter fires during movement");
        Check(keyboard.Read(true, true).Fire, "held Enter supports the weapon's normal cadence");
        Check(!keyboard.Read(true, false).Fire, "Enter cannot fire in typing or results");
        Check(!keyboard.Read(true, true).Fire, "held typing/restart Enter cannot leak into play");
        keys.Clear();
        keyboard.Read(true, true);
        keys.Add(0x0d);
        Check(keyboard.Read(true, true).Fire, "a fresh Enter press fires after releasing restart");
        Check(!keyboard.Read(false, false).Fire && !keyboard.Read(true, true).Fire,
              "refocus also requires releasing the held trigger");

        {
            // Both result scenes pass through this same input path, offline and online.
            keyboard = new KeyboardInput(keys.Contains);
            keys.Clear();
            keyboard.Read(true, true);
            keys.Add(0x0d);
            Check(keyboard.Read(true, true).Fire, "last shot uses Enter");
            for (int repeat = 0; repeat < 40; repeat++)
                Check(!keyboard.Read(true, false, true).ConfirmResult,
                      "held final shot cannot confirm a result");
            keys.Clear();
            Check(!keyboard.Read(true, false, true).ConfirmResult, "release only arms confirmation");
            keys.Add(0x0d);
            Check(keyboard.Read(true, false, true).ConfirmResult, "fresh result press confirms once");
            Check(!keyboard.Read(true, false, true).ConfirmResult, "holding confirmation cannot repeat");
            Check(!keyboard.Read(true, true).Fire, "held confirmation cannot fire in the next round");
            keys.Clear();
            keyboard.Read(true, true);
            keys.Add(0x0d);
            Check(keyboard.Read(true, true).Fire, "release after rematch restores shooting");
            keys.Clear();
            keyboard.Read(true, false, true);
            keys.Add(0x0d);
            Check(!keyboard.Read(false, false, true).ConfirmResult &&
                      !keyboard.Read(true, false, true).ConfirmResult,
                  "refocusing a result screen with held Enter cannot confirm");
        }

        foreach (int weapon in new[] { 1, 2 })
            foreach (int role in new[] { 0, 1 })
            {
                var match = new OfflineMatch(TestMap.Empty);
                match.Reset(200, 500);
                match.Select(weapon, 0);
                match.Ammo.Rounds[weapon] = WeaponAmmo.Capacity(weapon);
                match.Ammo.Reserve[weapon] = 1;
                Check(match.Fire(2, 180, 535, 0), "first shot accepted");
                Check(match.Projectiles.Count == (weapon == 2 ? 6 : 1),
                      "one shell emits one or six projectiles");
                Check(match.Ammo.Rounds[weapon] == WeaponAmmo.Capacity(weapon) - 1,
                      "one round consumed per trigger");
                Check(!match.Fire(2, 180, 535, .01), "cadence belongs to the magazine model");
                match.Advance(1 - role, 200, 500, 2400, .05, .05);
                Check(match.Health.Hearts[1 - role] == 2 && match.Hits == 1,
                      "pellet contacts deal one heart with grace");
                Check(match.Select(0, .1) && match.Projectiles.Count == 0, "holster clears projectiles");
                match.Select(weapon, .2);
                Check(match.Ammo.Reload(weapon, .2), "partially empty magazine can reload");
                match.Select(3 - weapon, .3);
                match.Select(weapon, .4);
                Check(match.Ammo.Reloading == 0 &&
                          match.Ammo.Rounds[weapon] == WeaponAmmo.Capacity(weapon) - 1,
                      "switch cancels reload without granting ammunition");
                for (int hit = 0; hit < 2; hit++)
                {
                    double now = 1 + hit;
                    Check(match.Fire(2, 180, 535, now), "next shot after grace");
                    bool defeated = match.Advance(1 - role, 200, 500, 2400, now, .05);
                    Check(defeated == (hit == 1), "only the third heart ends the match");
                }
                Check(match.Health.Hearts[1 - role] == 0 && match.Projectiles.Count == 0,
                      "victory removes remaining projectiles");
                match.Reset(200, 500);
                Check(match.Weapon == 0 && match.Hits == 0 && match.Health.Hearts[0] == 3 &&
                          match.Health.Hearts[1] == 3,
                      "new round resets both hearts and combat state");
            }
        var range = new OfflineMatch(TestMap.Empty);
        range.Reset(1400, 1000);
        range.Select(2, 0);
        range.Ammo.Rounds[2] = WeaponAmmo.Capacity(2);
        range.Fire(2, 100, 100, 0);
        range.Advance(1, 1400, 1000, 2400, .2, .2);
        Check(range.Projectiles.Count == 0, "shotgun pellets stop at their finite range");

        LauncherOptions options =
            LauncherOptions.Parse(new[] { "--start", "--instance", "lab_2", "--game",
                                          "C:\\Games\\TypeGame.exe", "--attach", "1234" });
        Check(options.Start && options.Instance == "lab_2" && options.AttachPid == 1234,
              "command-line options are parsed once");
        foreach (string[] bad in new[] { new[] { "--game" }, new[] { "--instance", "../bad" },
                                         new[] { "--attach", "0" }, new[] { "--unknown" } })
        {
            bool rejected = false;
            try
            {
                LauncherOptions.Parse(bad);
            }
            catch (ArgumentException)
            {
                rejected = true;
            }
            Check(rejected, "invalid command line reports an explicit argument error");
        }

        string folder =
            Path.Combine(Path.GetTempPath(), "TypeGameMod-LogTest-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        string file = Path.Combine(folder, "session.log");
        try
        {
            var log = new SessionLog(file);
            var workers = new List<Thread>();
            for (int i = 0; i < 4; i++)
            {
                var worker = new Thread(delegate() {
                    for (int line = 0; line < 50; line++)
                        log.Write("entry " + line);
                });
                workers.Add(worker);
                worker.Start();
            }
            foreach (Thread worker in workers)
                worker.Join();
            Check(File.ReadAllLines(file).Length == 200,
                  "concurrent UI/music/PCM logs preserve all complete lines");
        }
        finally
        {
            File.Delete(file);
            Directory.Delete(folder);
        }
        Console.WriteLine(
            "PASS " + checks +
            " extracted offline combat, input edges, launcher options and concurrent log checks");
    }
}
