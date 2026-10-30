using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.Serialization;
using TypeGameMod;

internal static class EnergyHudTests
{
    static int checks;
    static void Check(bool condition, string reason)
    {
        checks++;
        if (!condition)
            throw new Exception(reason);
    }
    static Dictionary<int, uint> Pixels(byte[] bank, int state)
    {
        var result = new Dictionary<int, uint>();
        int at = state * EnergyHud.StateBytes, count = BitConverter.ToInt32(bank, at);
        Check(count > 0 && count <= 204, "native rectangle-list bounds");
        for (int i = 0; i < count; i++)
        {
            int span = at + 4 + i * 20;
            int x0 = BitConverter.ToInt32(bank, span), y0 = BitConverter.ToInt32(bank, span + 4);
            int x1 = BitConverter.ToInt32(bank, span + 8), y1 = BitConverter.ToInt32(bank, span + 12);
            uint colour = BitConverter.ToUInt32(bank, span + 16);
            Check(x0 >= 684 && x1 <= 790 && y0 >= 480 && y1 <= 512 && x0 < x1 && y0 < y1,
                  "HUD stays under icons and above original article");
            for (int y = y0; y < y1; y++)
                for (int x = x0; x < x1; x++)
                    result[y * 1024 + x] = colour;
        }
        return result;
    }
    internal static void Run()
    {
        byte[] bank = EnergyHud.Build();
        Shine();
        Check(bank.Length == 122 * 4096, "both roles and all 61 partial-energy states are prebuilt");
        for (int role = 0; role < 2; role++)
            for (int step = 0; step <= 60; step++)
            {
                int green = 0;
                foreach (uint colour in Pixels(bank, role * 61 + step).Values)
                    if (colour == 0x005de16eu || colour == 0x0095ffc0u || colour == 0x002b8a3bu)
                        green++;
                Check(green == step * 11, "partial shield drain changes fill area, not only brightness");
            }
        Check(EnergyHud.Step(0) == 0 && EnergyHud.Step(.01) == 1 && EnergyHud.Step(2.999) == 29 &&
                  EnergyHud.Step(3) == 30 && EnergyHud.Step(6) == 60,
              "zero/partial/full display boundaries");
        foreach (int role in new[] { 0, 1 })
            foreach (double energy in new[] { 0.0, .1, .9, 1, 2.9, 3, 4, 6 })
                foreach (int weapon in new[] { 0, 1, 2 })
                {
                    var skill = new RoleSkill(role) { Energy = energy };
                    var ammo = new WeaponAmmo();
                    var data = new byte[ModSession.SnapshotBytes];
                    EnergyHud.WriteSnapshot(data, skill, ammo, weapon, 0);
                    bool ready = role == 0 ? energy > 0 : energy >= 1;
                    bool supply = energy >= 3 && weapon != 0;
                    Check(BitConverter.ToUInt32(data, SnapshotLayout.SkillDarkRows) == (ready ? 0u : 64u),
                          "role icon is fully lit whenever inventory permits use");
                    Check(BitConverter.ToUInt32(data, SnapshotLayout.SupplyReady) == (supply ? 1u : 0u) &&
                              BitConverter.ToUInt32(data, SnapshotLayout.SupplyIconState) ==
                                  (supply ? 4u : 0u),
                          "E icon requires three energy and room for reserve in the equipped gun");
                    if (weapon == 0)
                        continue;
                    ammo.Rounds[weapon] = WeaponAmmo.Capacity(weapon);
                    EnergyHud.WriteSnapshot(data, skill, ammo, weapon, 0);
                    Check(BitConverter.ToUInt32(data, SnapshotLayout.SupplyReady) == (supply ? 1u : 0u),
                          "a full magazine can still receive reserve");
                    ammo.Reserve[weapon] = WeaponAmmo.ReserveCapacity(weapon);
                    EnergyHud.WriteSnapshot(data, skill, ammo, weapon, 0);
                    Check(BitConverter.ToUInt32(data, SnapshotLayout.SupplyReady) == 0,
                          "both magazine and reserve full keep E dark");
                    ammo.Rounds[weapon]--;
                    EnergyHud.WriteSnapshot(data, skill, ammo, weapon, 0);
                    Check(BitConverter.ToUInt32(data, SnapshotLayout.SupplyReady) == (supply ? 1u : 0u),
                          "full reserve still allows instant supply into a partial magazine");
                    if (role == 0 && ready)
                    {
                        skill.Hold(true, 0);
                        ammo.Rounds[weapon] = 0;
                        EnergyHud.WriteSnapshot(data, skill, ammo, weapon, 0);
                        Check(BitConverter.ToUInt32(data, SnapshotLayout.SupplyReady) == 0,
                              "raised shield keeps E unavailable");
                    }
                }
        Console.WriteLine(
            "PASS " + checks +
            " six-green-cell HUD, count/time badge and actual icon-availability publisher checks");
    }
    static void Shine()
    {
        // UpdateMusic publishes the complete nine-DWORD block every poll, not
        // just the DWORD named MusicTelemetry in layout.json.
        var music = (WeaponMusic)FormatterServices.GetUninitializedObject(typeof(WeaponMusic));
        byte[] telemetry = music.Telemetry(), control = new byte[1024];
        const uint pointer = 0x12345000;
        BinaryFields.Write(control, Control.EnergyShineSpans, pointer);
        Buffer.BlockCopy(telemetry, 0, control, Control.MusicTelemetry, telemetry.Length);
        Check(BitConverter.ToUInt32(control, Control.EnergyShineSpans) == pointer,
              "real music telemetry publication cannot overwrite the shine bank address");
        Check(Control.EnergyShineSpans + 4 <= Control.MusicTelemetry ||
                  Control.EnergyShineSpans >= Control.MusicTelemetry + telemetry.Length,
              "shine address lies outside the whole music telemetry range");
        byte[] shine = EnergyHud.BuildShine();
        Check(shine.Length <= AssetLayout.ResultHud - AssetLayout.EnergyShine,
              "shine reuses the retired typing-meter space without enlarging allocation");
        for (int step = 0; step <= 60; step++)
        {
            var lit = new HashSet<int>();
            for (int frame = 0; frame < EnergyHud.ShineFrames; frame++)
            {
                int at = (step * EnergyHud.ShineFrames + frame) * EnergyHud.ShineStateBytes;
                int count = BitConverter.ToInt32(shine, at);
                Check(count >= 0 && count <= 6 && 4 + count * 20 <= EnergyHud.ShineStateBytes,
                      "shine has at most six tiny native rectangles");
                for (int i = 0; i < count; i++)
                {
                    int p = at + 4 + i * 20;
                    int x0 = BitConverter.ToInt32(shine, p), y0 = BitConverter.ToInt32(shine, p + 4);
                    int x1 = BitConverter.ToInt32(shine, p + 8), y1 = BitConverter.ToInt32(shine, p + 12);
                    Check(x0 < x1 && y0 < y1, "nonempty shine rectangle");
                    for (int x = x0; x < x1; x++)
                        for (int y = y0; y < y1; y++)
                        {
                            int cell = (x - 686) / 18, rows = Math.Min(10, step - cell * 10);
                            Check(cell >= 0 && cell < 6 && x >= 686 + cell * 18 && x < 697 + cell * 18 &&
                                      y >= 511 - rows && y < 511,
                                  "shine cannot illuminate empty cells, gaps or unfilled rows");
                            lit.Add((y << 10) | x);
                        }
                }
            }
            Check(lit.Count == step * 11, "sweep reaches every filled pixel and no empty pixel");
        }
        var skill = new RoleSkill(1) { Energy = 1 };
        var phases = new HashSet<int>();
        for (int i = 0; i < 60; i++)
            phases.Add(EnergyHud.ShineFrame(skill, i * .03 + .001));
        Check(phases.Count == 21 && phases.Contains(20), "moving sweep has a quiet pause");
        Check(EnergyHud.ShineFrame(skill, 0.061) == EnergyHud.ShineFrame(skill, 1.861),
              "visual cycle repeats independently of frame rate");
        skill.NextBlink = 1;
        Check(EnergyHud.ShineFrame(skill, .2) == 20, "blink cooldown does not advertise immediate use");
        skill.Energy = 0;
        Check(EnergyHud.ShineFrame(skill, 1.2) == 20, "empty energy has no shine");
        skill = new RoleSkill(0) { Energy = 2 };
        skill.Hold(true, 0);
        var snapshot = new byte[ModSession.SnapshotBytes];
        EnergyHud.WriteSnapshot(snapshot, skill, new WeaponAmmo(), 1, .2);
        Check(BitConverter.ToUInt32(snapshot, SnapshotLayout.EnergyShineFrame) == 20,
              "active shield shows its drain without an availability sweep");
        skill.Hold(false, .2);
        EnergyHud.WriteSnapshot(snapshot, skill, new WeaponAmmo(), 1, .2);
        Check(BitConverter.ToUInt32(snapshot, SnapshotLayout.EnergyShineFrame) < 20,
              "stored usable shield time restores the sweep");
    }
    internal static void Export(string folder)
    {
        Directory.CreateDirectory(folder);
        byte[] bank = EnergyHud.Build();
        File.WriteAllBytes(Path.Combine(folder, "energy-bank.bin"), bank);
        File.WriteAllBytes(Path.Combine(folder, "shine-bank.bin"), EnergyHud.BuildShine());
        File.WriteAllBytes(Path.Combine(folder, "police-icon.bin"),
                           SkillIconFrames.Build("TypeGameWASD.PoliceShieldIcon"));
        File.WriteAllBytes(Path.Combine(folder, "thief-icon.bin"),
                           SkillIconFrames.Build("TypeGameWASD.ThiefBlinkIcon"));
        File.WriteAllBytes(Path.Combine(folder, "supply-icon.bin"), SkillIconFrames.BuildResupply());
        foreach (int role in new[] { 0, 1 })
            foreach (int step in new[] { 0, 5, 10, 25, 30, 60 })
                using (var bitmap = new Bitmap(800, 600))
                {
                    using (Graphics graphics = Graphics.FromImage(bitmap))
                        graphics.Clear(Color.FromArgb(37, 43, 44));
                    foreach (var pixel in Pixels(bank, role * 61 + step))
                        bitmap.SetPixel(pixel.Key & 1023, pixel.Key >> 10,
                                        Color.FromArgb((int)(pixel.Value & 255),
                                                       (int)((pixel.Value >> 8) & 255),
                                                       (int)(pixel.Value >> 16)));
                    bitmap.Save(Path.Combine(folder, "energy-" + role + "-" + step + ".png"),
                                ImageFormat.Png);
                }
    }
}
