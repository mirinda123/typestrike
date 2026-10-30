using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using TypeGameMod;

internal static class PoliceFireEffectsTests
{
    internal static void Run()
    {
        var export = new List<string>();
        bool maximumSeen = false;
        foreach (bool policeShotgun in new[] { false, true })
            foreach (bool shotgun in new[] { false, true })
                for (uint heading = 0; heading < 8; heading++)
                {
                    var police = new AkFireEffect();
                    var policeSg = new ShotgunFireEffect();
                    var thiefAk = new AkFireEffect();
                    var thiefShotgun = new ShotgunFireEffect();
                    double previous = 0;
                    double[] ages = { 0, .02, .035, .06, .12, .19, .31 };
                    for (int frame = 0; frame < ages.Length; frame++)
                    {
                        double dt = ages[frame] - previous;
                        previous = ages[frame];
                        police.Update(!policeShotgun, 1, 300, 180, heading, dt);
                        policeSg.Update(policeShotgun, 1, 300, 180, heading, dt);
                        thiefAk.Update(!shotgun, 1, 500, 200, (heading + 4) % 8, dt);
                        thiefShotgun.Update(shotgun, 1, 500, 200, (heading + 4) % 8, dt);
                        // Independent players can fire 20ms apart: the AK flash
                        // then overlaps the shotgun's late flash and first smoke.
                        if (shotgun && frame == 0)
                            thiefShotgun.Update(true, 1, 500, 200, (heading + 4) % 8, .02);
                        if (policeShotgun && shotgun && frame == 0)
                            policeSg.Update(true, 1, 300, 180, heading, .02);
                        var data = new byte[ModSession.SnapshotBytes];
                        for (int i = 0; i < data.Length; i++)
                            data[i] = 0x5a;
                        var effects = new FireEffectBuffer(data);
                        if (shotgun)
                            thiefShotgun.Append(effects, 500, 200, 0, 0);
                        else
                            thiefAk.Append(effects, 500, 200, 0, 0);
                        effects.Finish();
                        int before = BitConverter.ToInt32(data, SnapshotLayout.ShotgunFxCount);
                        var prefix = new byte[before * 20];
                        Buffer.BlockCopy(data, SnapshotLayout.ShotgunFxRects, prefix, 0, prefix.Length);
                        if (policeShotgun)
                            policeSg.Append(effects, 300, 180, 0, 0);
                        else
                            police.Append(effects, 300, 180, 0, 0);
                        effects.Finish();
                        int count = BitConverter.ToInt32(data, SnapshotLayout.ShotgunFxCount);
                        Check(count >= before && count <= 2 * ShotgunFireEffect.MaxRects,
                              "both players' effects fit the existing snapshot");
                        maximumSeen |= count == 22;
                        for (int i = 0; i < data.Length; i++)
                            if ((i < SnapshotLayout.ShotgunFxCount ||
                                 i >= SnapshotLayout.ShotgunFxCount + 4) &&
                                (i < SnapshotLayout.ShotgunFxRects ||
                                 i >= SnapshotLayout.ShotgunFxRects + count * 20))
                                Check(data[i] == 0x5a, "two-player effects preserve ammo, reload, typing, " +
                                                           "blink, health and unused bytes");
                        for (int i = 0; i < prefix.Length; i++)
                            Check(data[SnapshotLayout.ShotgunFxRects + i] == prefix[i],
                                  "police flash preserves every thief command");
                        for (int i = 0; i < count; i++)
                        {
                            int at = SnapshotLayout.ShotgunFxRects + i * 20;
                            int x0 = BitConverter.ToInt32(data, at), y0 = BitConverter.ToInt32(data, at + 4);
                            int x1 = BitConverter.ToInt32(data, at + 8),
                                y1 = BitConverter.ToInt32(data, at + 12);
                            Check(x0 >= 0 && y0 >= 0 && x1 <= 800 && y1 <= 500 && x0 < x1 && y0 < y1,
                                  "both effects stay above the typing article");
                        }
                        if (ages[frame] >= .31)
                            Check(count == 0, "both smoke trails expire");
                        var row =
                            new List<string> { ((policeShotgun ? 2 : 0) + (shotgun ? 1 : 0)).ToString(),
                                               heading.ToString(), frame.ToString(), "0", count.ToString() };
                        for (int i = 0; i < count * 5; i++)
                            row.Add(BitConverter.ToInt32(data, SnapshotLayout.ShotgunFxRects + i * 4)
                                        .ToString(CultureInfo.InvariantCulture));
                        export.Add(String.Join(",", row.ToArray()));
                    }
                    police.Update(false, 1, 0, 0, heading, .01);
                    police.Update(true, 1, 0, 0, heading, .01);
                    Check(Double.IsPositiveInfinity(police.Age),
                          "focus, shield or weapon restoration cannot replay an old flash");
                    police.Update(true, 2, 300, 180, heading, .01);
                    Check(police.Age == 0, "new police shot starts a fresh flash");
                    police.Update(true, 0, 300, 180, heading, .01);
                    Check(Double.IsPositiveInfinity(police.Age),
                          "new-round counter clears the police effect");
                    policeSg.Update(false, 1, 0, 0, heading, .01);
                    policeSg.Update(true, 1, 0, 0, heading, .01);
                    Check(Double.IsPositiveInfinity(policeSg.Age),
                          "shield, focus and switching cannot replay shotgun fire");
                    policeSg.Update(true, 2, 300, 180, heading, .01);
                    Check(policeSg.Age == 0, "accepted shotgun shot triggers fire");
                    policeSg.Update(true, 0, 300, 180, heading, .01);
                    Check(Double.IsPositiveInfinity(policeSg.Age), "new-round counter clears shotgun fire");
                }
        Check(maximumSeen && SnapshotLayout.ShotgunFxRects >= SnapshotLayout.ThiefInvulnerable + 4 &&
                  SnapshotLayout.ShotgunFxRects + 22 * 20 <= ModSession.SnapshotBytes &&
                  CaveLayout.Snapshot + ModSession.SnapshotBytes <= CaveLayout.RenderCache &&
                  CaveLayout.RenderCache + ModSession.SnapshotBytes <= CaveLayout.SendBstr,
              "full 22-command effect fits both native slots without touching gameplay or packet memory");
        string path = Environment.GetEnvironmentVariable("TYPEGAME_POLICE_FX_PREVIEW");
        if (!String.IsNullOrEmpty(path))
            File.WriteAllLines(path, export.ToArray());
        Console.WriteLine("PASS police AK/shotgun fire: 224 two-player frames, 22 commands, protected " +
                          "HUD/typing/blink/health fields, reset/switch safety");
    }

    static void Check(bool yes, string reason)
    {
        if (!yes)
            throw new Exception(reason);
    }
}
