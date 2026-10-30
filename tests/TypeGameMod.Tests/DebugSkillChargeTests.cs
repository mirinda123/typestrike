using System;
using System.Collections.Generic;
using System.Reflection;
using TypeGameMod;

internal static class DebugSkillChargeTests
{
    static int checks;
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool condition, string reason)
    {
        checks++;
        if (!condition)
            throw new Exception(reason);
    }
    static object Get(ModSession session, string name)
    {
        return typeof(ModSession).GetField(name, Private).GetValue(session);
    }
    static void Set(ModSession session, string name, object value)
    {
        typeof(ModSession).GetField(name, Private).SetValue(session, value);
    }
    static bool Charge(ModSession session, uint role, double now, bool active = true, bool focused = true,
                       bool pressed = true, InputFrame input = null)
    {
        if (input == null)
            input = new InputFrame();
        input.Focused = focused;
        input.DebugCharge = pressed;
        return (bool)typeof(ModSession)
            .GetMethod("DebugChargeSkill", Private)
            .Invoke(session, new object[] { active, role, input, now });
    }
    internal static void Run()
    {
        for (int mask = 0; mask < 8; mask++)
        {
            var keys = new HashSet<int>();
            if ((mask & 1) != 0)
                keys.Add(0x11);
            if ((mask & 2) != 0)
                keys.Add(0x12);
            if ((mask & 4) != 0)
                keys.Add(0x78);
            var keyboard = new KeyboardInput(keys.Contains);
            InputFrame input = keyboard.Read(true, false);
            Check(input.DebugCharge == (mask == 7), "only complete chord triggers, including typing");
            Check(!input.Fire && !input.ShiftPressed && !input.Supply, "chord does not spend or fire");
            Check(!keyboard.Read(true, true).DebugCharge, "held chord does not repeat");
            Check(!keyboard.Read(false, false).DebugCharge && !keyboard.Read(true, false).DebugCharge,
                  "held chord cannot trigger on refocus");
            keys.Remove(0x78);
            keyboard.Read(true, true);
            keys.Add(0x78);
            Check(keyboard.Read(true, true).DebugCharge == ((mask & 3) == 3), "release rearms chord");
        }
        foreach (int role in new[] { 0, 1 })
            for (int stock = 0; stock <= 6; stock++)
            {
                var session = new ModSession(null, null);
                var skill = new RoleSkill(role) { Energy = stock };
                Set(session, "offlineSkill", skill);
                var article = new ArticleProgress("one two three four", 5, skill.Cycle);
                var typing = (TypingController)Get(session, "typing");
                typing.SetArticle(article);
                typing.HandleKey(9, 5, false, 0);
                var input = new InputFrame { ShiftPressed = true };
                Check(!Charge(session, (uint)role, 0, false) && skill.Energy == stock,
                      "menus/results cannot charge");
                Check(!Charge(session, (uint)role, 0, true, false) && skill.Energy == stock,
                      "background cannot charge");
                Check(!Charge(session, (uint)role, 0, true, true, false) && skill.Energy == stock,
                      "ordinary frames cannot charge");
                Check(Charge(session, (uint)role, 0, input: input) && skill.Full && !skill.Shield &&
                          skill.Cycle == 6 - stock,
                      "debug tops inventory up to six through real word confirmations");
                Check(!typing.Enabled && typing.WaitingForRelease && !input.ShiftPressed,
                      "typing exits with held-action release gate");
                Check(ReferenceEquals(typing.Article, article) && article.Position == 5,
                      "article and cursor untouched");
                var ammo = new WeaponAmmo();
                Check(skill.Resupply(ammo, role + 1, .1) && skill.Energy == 3 && skill.Ready,
                      "debug stock can fund E and retain three role units");
                if (role == 0)
                {
                    skill.Hold(true, .2);
                    Check(!Charge(session, 0, .7) && Math.Abs(skill.Energy - 2.5) < 1e-8,
                          "debug cannot replenish a raised shield");
                    skill.Hold(false, .7);
                    Check(Charge(session, 0, .8) && skill.Full, "debug fills a paused fractional balance");
                }
                else
                {
                    DuelPose target;
                    Check(skill.Use(DuelPose.Spawn(1), .2, TestMap.Movement, out target) && skill.Energy == 2,
                          "one debug-funded blink costs one");
                    Check(Charge(session, 1, .3) && skill.Full,
                          "no obsolete three-second blink window blocks recharge");
                }
            }
        foreach (int hostRole in new[] { 0, 1 })
            foreach (int role in new[] { 0, 1 })
            {
                var pair = new EnergyTestPair(hostRole);
                pair.Equip(role, role + 1);
                var session = new ModSession(null, null);
                Set(session, "networkMatch", pair.Player(role));
                Check(Charge(session, (uint)role, pair.Now), "both hosts/roles can debug charge");
                pair.Step();
                Check(pair.Host.Skills[role].Energy == 6 && pair.Client.Skills[role].Energy == 6 &&
                          pair.Host.Skills[1 - role].Energy == 0,
                      "only own inventory fills and authority converges");
                pair.Player(role).SupplyKey(true);
                pair.Step();
                Check(pair.Host.Armories[role].Reloading == 0,
                      "debug-funded first supply skips the reload timer");
                Check(pair.Host.Skills[role].Energy == 3 && pair.Client.Skills[role].Energy == 3 &&
                          pair.Host.Armories[role].Rounds[role + 1] == WeaponAmmo.Capacity(role + 1),
                      "network E uses exactly three debug-funded energy");
            }
        {
            var session = new ModSession(null, null);
            var police = new RoleSkill(0) { Energy = 6 };
            Set(session, "offlineSkill", police);
            var typing = (TypingController)Get(session, "typing");
            typing.ReturnToMovement();
            var input = new InputFrame { Focused = true, ShiftHeld = true };
            var apply = typeof(ModSession).GetMethod("OfflineSkillInput", Private);
            apply.Invoke(session, new object[] { 0u, 0u, 100.0, 100.0, 0u, input, 0.0 });
            Check(!police.Shield && police.Energy == 6,
                  "held capital-letter Shift cannot auto-raise after typing exits");
            typing.UpdateReleaseGate(false);
            apply.Invoke(session, new object[] { 0u, 0u, 100.0, 100.0, 0u, input, 0.0 });
            Check(police.Shield, "fresh gameplay Shift raises shield normally");
        }
        Console.WriteLine("PASS " + checks + " hidden six-energy input, runtime and offline LAN checks");
    }
}
