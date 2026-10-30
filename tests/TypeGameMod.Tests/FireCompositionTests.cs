using System;
using System.Collections.Generic;
using TypeGameMod;

internal static class FireCompositionTests
{
    static List<string> Render(bool policeFirst)
    {
        var police = new ShotgunFireEffect();
        var thief = new AkFireEffect();
        police.Update(true, 1, 200, 200, 2, 0);
        thief.Update(true, 1, 600, 200, 6, 0);
        var data = new byte[ModSession.SnapshotBytes];
        var buffer = new FireEffectBuffer(data);
        if (policeFirst)
            police.Append(buffer, 200, 200, 0, 0);
        thief.Append(buffer, 600, 200, 0, 0);
        if (!policeFirst)
            police.Append(buffer, 200, 200, 0, 0);
        buffer.Finish();
        int count = BitConverter.ToInt32(data, SnapshotLayout.ShotgunFxCount);
        var commands = new List<string>();
        for (int i = 0; i < count; i++)
            commands.Add(Convert.ToBase64String(data, SnapshotLayout.ShotgunFxRects + i * 20, 20));
        commands.Sort(StringComparer.Ordinal);
        return commands;
    }

    internal static void Run()
    {
        List<string> first = Render(true), second = Render(false);
        if (first.Count != 14 || String.Join(";", first.ToArray()) != String.Join(";", second.ToArray()))
            throw new Exception("Either actor may append first without erasing another actor's effects");
        var data = new byte[ModSession.SnapshotBytes];
        BinaryFields.Write(data, SnapshotLayout.ShotgunFxCount, 22u);
        new FireEffectBuffer(data).Finish();
        if (BitConverter.ToUInt32(data, SnapshotLayout.ShotgunFxCount) != 0)
            throw new Exception("A new frame owns its empty effect list without calling either actor");
        Console.WriteLine(
            "PASS fire composition: either actor order preserves both effects; frame resets once");
    }
}
