using System;
using System.Collections.Generic;
using TypeGameMod;

internal static class TypingControllerTests
{
    static void Check(bool condition, string reason)
    {
        if (!condition)
            throw new Exception(reason);
    }

    internal static void Run()
    {
        var typing = new TypingController();
        var article = new ArticleProgress("one two three four five six seven eight", 0, 0);
        typing.SetArticle(article);
        Check(typing.ControlsAllowed && typing.HandleKey(9, 0, false, 0) && typing.Enabled,
              "Tab enters typing while preserving the native article");
        for (int sample = 0; sample < 20; sample++)
            Check(!typing.ControlsAllowed && typing.Enabled, "reading controls never changes state");
        Check(typing.HandleKey('o', 1, false, .01), "typing delivers accepted cursor movement");
        var feedback = new byte[ModSession.SnapshotBytes];
        typing.Feedback.WriteSnapshot(feedback, .01);
        Check(BitConverter.ToUInt32(feedback, SnapshotLayout.FireCount) > 0,
              "typing feedback follows cursor observations");
        typing.HandleKey(9, 1, false, .02);
        Check(!typing.Enabled && typing.WaitingForRelease && !typing.ControlsAllowed,
              "leaving typing gates held gameplay keys");
        typing.UpdateReleaseGate(true);
        Check(!typing.ControlsAllowed, "holding the final W/A/S/D/R/E/Shift/Enter/Tab cannot leak");
        typing.UpdateReleaseGate(false);
        Check(typing.ControlsAllowed, "a sampled release enables movement");
        Check(!typing.HandleKey(9, 1, true, .03) && !typing.Enabled, "a raised shield cannot enter typing");
        typing.ReturnToMovement();
        typing.Reset();
        Check(typing.ControlsAllowed && !typing.Enabled && typing.Article == null,
              "new rounds clear article and stale mode gates");
        var keys = new HashSet<int>();
        var keyboard = new KeyboardInput(keys.Contains);
        foreach (int key in new[] { 0x57, 0x41, 0x53, 0x44, 0x0d, 0x31, 0x32, 0x52, 0x45, 0x10, 0x09 })
        {
            keys.Clear();
            keys.Add(key);
            Check(keyboard.Read(true, false).GameplayKeysHeld,
                  "gate sees keys even when gameplay is blocked");
            Check(keyboard.Read(false, false).GameplayKeysHeld, "focus changes cannot invent a release");
        }
        keys.Clear();
        Check(!keyboard.Read(true, false).GameplayKeysHeld, "all gameplay keys released");
        // Gate release must precede trigger decoding or the first fresh Enter is lost.
        typing.ReturnToMovement();
        keys.Add(0x45);
        Check(!keyboard.Read(true, true, false, typing).Fire && typing.WaitingForRelease,
              "held final article key keeps the gate closed");
        keys.Clear();
        keyboard.Read(true, true, false, typing);
        keys.Add(0x0d);
        Check(keyboard.Read(true, true, false, typing).Fire,
              "the first fresh Enter after the sampled release fires normally");
        Console.WriteLine(
            "PASS typing controller: pure queries, sampled release, shield exclusion and feedback");
    }
}
