using TypeGameMod;
using System;
using System.IO;
using System.Reflection;

internal static class SkillTests
{
    static int checks;
    static void Check(bool condition, string reason)
    {
        checks++;
        if (!condition)
            throw new Exception(reason);
    }
    static void Charge(RoleSkill skill, int words, double now)
    {
        for (int i = 0; i < words; i++)
            Check(skill.Match(skill.Word, now), "one native word adds one energy");
    }
    static void Pure()
    {
        var article = new ArticleProgress("One, two don't re-enter. Last!", 0, 0);
        Check(article.Observe(2, 0, true) == 0, "partial word cannot charge");
        Check(article.Observe(2, 0, true) == 0, "wrong/repeated character cannot charge");
        Check(article.Observe(3, 0, true) == 1, "first native word");
        Check(article.Observe(4, 1, true) == 0, "punctuation cannot charge");
        Check(article.Observe(8, 1, true) == 1, "second native word");
        Check(article.Observe(14, 2, true) == 1, "apostrophe is part of a word");
        Check(article.Observe(23, 3, true) == 1, "hyphen is part of a word");
        Check(article.Observe(29, 4, false) == 0, "blocked/full typing does not bank words");
        Check(article.Observe(0, 4, true) == 0 && article.Observe(23, 4, true) == 4,
              "article loop still counts complete original words");
        Check(article.Observe(23, 8, true) == 0, "cursor replay cannot charge twice");
        foreach (int role in new[] { 0, 1 })
        {
            var skill = new RoleSkill(role);
            Check(skill.Energy == 0 && !skill.Ready && !skill.SupplyReady, "empty spawn");
            Check(!skill.Match("wrong", 0), "wrong token rejected");
            Charge(skill, 1, 0);
            Check(skill.Energy == 1 && skill.Ready && !skill.SupplyReady && skill.CanCharge,
                  "first word enables role skill but not E, and allows more charge");
            Charge(skill, 5, 0);
            Check(skill.Energy == 6 && skill.Full && !skill.Match(skill.Word, 0), "six energy cap");
            skill.Tick(100);
            Check(skill.Energy == 6, "stored energy neither expires nor passively regenerates");
            skill.Reset();
            Check(skill.Energy == 0 && skill.Cycle == 0 && !skill.Shield, "round reset clears inventory");
        }
        var thief = new RoleSkill(1);
        Charge(thief, 3, 0);
        var origin = new DuelPose { X = 700, Y = 500, Heading = 2 };
        DuelPose target;
        Check(thief.Use(origin, 1, TestMap.Movement, out target) && thief.Energy == 2 && target.X == 910,
              "one blink costs one and keeps the approved distance");
        Check(!thief.Use(origin, 1.1, TestMap.Movement, out target) && thief.Energy == 2,
              "blink interval does not spend on rejection");
        Check(thief.Use(origin, 1.36, TestMap.Movement, out target) &&
                  thief.Use(origin, 10, TestMap.Movement, out target) && thief.Energy == 0,
              "three blinks can be spread over time");
        Check(!thief.Use(origin, 11, TestMap.Movement, out target), "empty thief cannot blink");
        for (int heading = 0; heading < 8; heading++)
        {
            Charge(thief, 1, 20 + heading);
            Check(thief.Use(new DuelPose { X = 200, Y = 200, Heading = (byte)heading }, 20 + heading,
                            TestMap.Movement, out target) &&
                      target.Valid,
                  "eight blink headings stay in world bounds");
        }
        var police = new RoleSkill(0);
        Charge(police, 3, 0);
        police.Hold(true, 1);
        police.Tick(1.75);
        Check(police.Shield && Math.Abs(police.Energy - 2.25) < 1e-8, "shield consumes only held time");
        Check(!police.Match(police.Word, 1.75), "raised shield cannot recharge from typing");
        police.Hold(false, 1.75);
        police.Tick(100);
        Check(police.Energy == 2.25 && !police.Shield, "lowered shield preserves fractional inventory");
        Charge(police, 1, 100);
        Check(police.Energy == 3.25, "one word adds to unspent fractional time");
        police.Hold(true, 101);
        police.Tick(104.25);
        Check(police.Energy == 0 && !police.Shield && !police.Ready, "shield stops exactly at zero");
        police.Hold(false, 104.25);
        Charge(police, 6, 104.25);
        police.Hold(true, 105);
        police.Hold(false, 105.1);
        Charge(police, 1, 105.1);
        Check(police.Energy == 6, "fractional recharge clamps to capacity");
        using (var stream = new MemoryStream())
        {
            police.Hold(true, 106);
            police.Tick(106.125);
            var writer = new BinaryWriter(stream);
            police.Write(writer, 106.125);
            stream.Position = 0;
            var replica = RoleSkill.Read(new BinaryReader(stream), 0, 500);
            Check(replica.Shield && Math.Abs(replica.Energy - 5.875) < 1e-10,
                  "wire preserves fractional energy");
            replica.Tick(500.5);
            Check(Math.Abs(replica.Energy - 5.375) < 1e-10, "replica interpolates only held shield time");
        }
        foreach (double invalid in new[] { -1.0, 6.01, double.NaN, double.PositiveInfinity })
        {
            using (var stream = new MemoryStream())
            {
                var skill = new RoleSkill(0) { Energy = invalid };
                skill.Write(new BinaryWriter(stream), 0);
                stream.Position = 0;
                bool rejected = false;
                try
                {
                    RoleSkill.Read(new BinaryReader(stream), 0, 0);
                }
                catch (ArgumentException)
                {
                    rejected = true;
                }
                Check(rejected, "invalid wire energy rejected");
            }
        }
    }
    static void Network()
    {
        foreach (int hostRole in new[] { 0, 1 })
        {
            var pair = new EnergyTestPair(hostRole);
            pair.Charge(1, 3);
            var thief = pair.Player(1);
            thief.SkillKeys(true, true);
            pair.Step();
            thief.SkillKeys(false, false);
            Check(pair.Host.Skills[1].Energy == 2 && pair.Client.Skills[1].Energy == 2 &&
                      pair.Host.Skills[1].BlinkSerial == 1,
                  "network blink costs one on both peers");
            pair.Step(1);
            thief.SkillKeys(true, true);
            pair.Step();
            thief.SkillKeys(false, false);
            Check(pair.Host.Skills[1].Energy == 1 && pair.Client.Skills[1].Energy == 1,
                  "blink inventory outlives the old three-second window");
            pair.Charge(1, 2);
            Check(pair.Host.Skills[1].Energy == 3 && pair.Client.Skills[1].Energy == 3,
                  "unused blink inventory stacks");
            pair.Charge(0, 3);
            var police = pair.Player(0);
            police.SkillKeys(true, true);
            pair.Step();
            police.SkillKeys(true, false);
            for (int i = 0; i < 25; i++)
                pair.Step();
            Check(pair.Host.Skills[0].Shield && pair.Client.Skills[0].Shield &&
                      pair.Host.Skills[0].Energy < 2.6,
                  "host/client police shield consumes held time");
            Check(!police.ConfirmSkillWord(police.Skills[0].Word, pair.Now),
                  "network shield prevents typing gains");
            police.SkillKeys(false, false);
            pair.Step();
            double heldRemainder = pair.Host.Skills[0].Energy;
            for (int i = 0; i < 80; i++)
                pair.Step();
            Check(!pair.Host.Skills[0].Shield && !pair.Client.Skills[0].Shield &&
                      Math.Abs(pair.Host.Skills[0].Energy - heldRemainder) < 1e-8,
                  "release preserves shield stock on both peers");
            pair.Charge(0, 2);
            Check(pair.Host.Skills[0].Energy > heldRemainder + 1.9,
                  "shield can stack new words onto a partial balance");
            string packet = pair.Client.Packet();
            byte[] old = Convert.FromBase64String(packet.Substring(DuelProtocol.Prefix.Length));
            old[4] = 9;
            Check(!pair.Host.Receive(DuelProtocol.Prefix + Convert.ToBase64String(old), 2, pair.Now),
                  "old seven-pellet wire v9 is explicitly incompatible");
            Check(pair.Host.Packet().Length <= 4093 && pair.Client.Packet().Length <= 4093,
                  "new state fits native transport");
        }
        var retry = new EnergyTestPair(0);
        var client = retry.Client;
        for (int i = 0; i < 6; i++)
            Check(client.ConfirmSkillWord(client.Skills[1].Word, retry.Now), "batched word accepted");
        string batch = client.Packet();
        Check(retry.Host.Receive(batch, 2, retry.Now) && retry.Host.Skills[1].Energy == 6,
              "batch fills exactly six");
        Check(!retry.Host.Receive(batch, 2, retry.Now), "same packet rejected");
        Check(retry.Host.Receive(client.Packet(), 2, retry.Now) && retry.Host.Skills[1].Energy == 6,
              "new packets resending unacknowledged words cannot duplicate energy");
        Check(client.Receive(retry.Host.Packet(), 1, retry.Now), "authoritative acknowledgement accepted");
    }
    static void FullTyping()
    {
        const BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic;
        foreach (int mode in new[] { -1, 0, 1 })
            foreach (int role in new[] { 0, 1 })
            {
                var pair = mode < 0 ? null : new EnergyTestPair(mode);
                var session = new ModSession(null, delegate(string message) {});
                var skill = pair == null ? new RoleSkill(role) : pair.Player(role).Skills[role];
                typeof(ModSession)
                    .GetField(pair == null ? "offlineSkill" : "networkMatch", fields)
                    .SetValue(session, pair == null ? (object)skill : pair.Player(role));
                var typing =
                    (TypingController) typeof(ModSession).GetField("typing", fields).GetValue(session);
                typing.HandleKey(9, 0, false, 0);
                var article = new ArticleProgress("one two three four five six seven eight nine ten", 0, 0);
                typing.SetArticle(article);
                MethodInfo observe = typeof(ModSession).GetMethod("ObserveArticleWords", fields);
                foreach (int position in new[] { 3, 7, 13, 18, 23, 27, 33, 39 })
                {
                    observe.Invoke(session,
                                   new object[] { (uint)role, position, pair == null ? 0 : pair.Now });
                    if (pair != null)
                        pair.Step();
                    Check(typing.Enabled, "normal article charging never auto-exits typing, even at six");
                }
                skill = pair == null ? skill : pair.Player(role).Skills[role];
                Check(skill.Energy == 6 && skill.Cycle == 6 && article.Position == 39,
                      "extra full-capacity words advance the article without overflow or banked gains");
                if (pair == null)
                    Check(skill.Resupply(new WeaponAmmo(), 1, 0), "offline spending after full typing");
                else
                {
                    pair.Equip(role, 1);
                    pair.Player(role).SupplyKey(true);
                    pair.Step();
                }
                observe.Invoke(session, new object[] { (uint)role, 39, pair == null ? 0 : pair.Now });
                skill = pair == null ? skill : pair.Player(role).Skills[role];
                Check(skill.Energy == 3, "full-capacity words cannot recharge after spending");
                observe.Invoke(session, new object[] { (uint)role, 44, pair == null ? 0 : pair.Now });
                if (pair != null)
                    pair.Step();
                skill = pair == null ? skill : pair.Player(role).Skills[role];
                Check(skill.Energy == 4 && skill.Cycle == 7 && typing.Enabled && article.Position == 44,
                      "next newly completed word replenishes one unit and keeps typing enabled");
            }
    }
    internal static void RunSuite(string[] args)
    {
        try
        {
            Pure();
            Network();
            FullTyping();
            Console.WriteLine("PASS " + checks +
                              " six-cell energy, blink, held shield, article and offline LAN checks");
        }
        catch (Exception error)
        {
            Console.WriteLine("FAIL energy after " + checks + ": " + error.Message);
            Environment.ExitCode = 1;
        }
    }
}
