using System;
using TypeGameMod;

internal static class ResupplyTests
{
    static int checks;
    static void Check(bool condition, string reason)
    {
        checks++;
        if (!condition)
            throw new Exception(reason);
    }
    static void Fill(RoleSkill skill, int words, double now)
    {
        for (int i = 0; i < words; i++)
            Check(skill.Match(skill.Word, now), "word accepted");
    }
    static void Pure()
    {
        foreach (int role in new[] { 0, 1 })
            foreach (int weapon in new[] { 1, 2 })
            {
                int cap = WeaponAmmo.Capacity(weapon), max = WeaponAmmo.ReserveCapacity(weapon);
                double load = WeaponAmmo.ReloadTime(weapon);
                var skill = new RoleSkill(role);
                var ammo = new WeaponAmmo();
                Check(!skill.Resupply(ammo, weapon, 0) && !ammo.Reload(weapon, 0),
                      "empty spawn has no free ammunition");
                Fill(skill, 2, 0);
                Check(!skill.Resupply(ammo, weapon, 0), "two energy cannot supply");
                Fill(skill, 1, 0);
                Check(!skill.Resupply(ammo, 0, 0) && skill.Energy == 3, "unarmed E costs nothing");
                Check(skill.Resupply(ammo, weapon, 0) && skill.Energy == 0 && ammo.Rounds[weapon] == cap &&
                          ammo.Reserve[weapon] == 0 && ammo.Reloading == 0 && ammo.Reloads[weapon] == 0 &&
                          ammo.CanFire(weapon, 0),
                      "empty E instantly grants a shootable magazine without any reload event");
                Fill(skill, 6, .1);
                Check(skill.Resupply(ammo, weapon, .1) && skill.Resupply(ammo, weapon, .2) &&
                          skill.Energy == 0 && ammo.Rounds[weapon] == cap && ammo.Reserve[weapon] == 2 * cap,
                      "full magazine E still accumulates two magazines of reserve");
                Fill(skill, 6, .3);
                Check(skill.Resupply(ammo, weapon, .3) && ammo.Reserve[weapon] == max && skill.Energy == 3,
                      "reserve cap stays bounded");
                Check(!skill.Resupply(ammo, weapon, .4) && skill.Energy == 3,
                      "both inventories full cannot waste energy");
                Check(ammo.Fire(weapon, 1), "loaded gun fires");
                Check(skill.Resupply(ammo, weapon, 1) && skill.Energy == 0 && ammo.Rounds[weapon] == cap &&
                          ammo.Reserve[weapon] == max && !ammo.CanFire(weapon, 1),
                      "full reserve still permits magazine refill, but E never resets fire cadence");

                // A magazine's worth is split between gun and reserve, not granted twice.
                int missing = cap / 3;
                ammo = new WeaponAmmo();
                ammo.Select(weapon, 2);
                ammo.Rounds[weapon] = cap - missing;
                ammo.Reserve[weapon] = cap;
                Fill(skill, 3, 2);
                Check(skill.Resupply(ammo, weapon, 2) && ammo.Rounds[weapon] == cap &&
                          ammo.Reserve[weapon] == 2 * cap - missing,
                      "partial magazine E prioritizes gun and puts only overflow in reserve");
                if (weapon == 1)
                    Check(ammo.Rounds[1] == 30 && ammo.Reserve[1] == 50, "AK 20/30 becomes 30/50");

                Check(ammo.Fire(weapon, 3) && ammo.Reload(weapon, 3.1), "R remains timed");
                int before = ammo.Rounds[weapon] + ammo.Reserve[weapon];
                Check(!ammo.CanFire(weapon, 3.2), "R blocks shooting during its timer");
                ammo.Tick(3.1 + load);
                Check(ammo.Rounds[weapon] == cap && ammo.Rounds[weapon] + ammo.Reserve[weapon] == before,
                      "R transfers reserve and preserves all leftover ammunition");
                Check(ammo.Fire(weapon, 6) && ammo.Reload(weapon, 6.1), "begin another manual reload");
                Fill(skill, 3, 6.2);
                Check(skill.Resupply(ammo, weapon, 6.2) && ammo.Reloading == 0 && ammo.Remaining(6.2) == 0 &&
                          ammo.CanRequest(weapon, 6.2),
                      "E during R immediately finishes supplying and clears the pending reload");
                before = ammo.Rounds[weapon] + ammo.Reserve[weapon];
                ammo.Tick(20);
                Check(ammo.Rounds[weapon] + ammo.Reserve[weapon] == before,
                      "cancelled reload cannot later duplicate or subtract ammunition");
                skill.Reset();
                ammo.Reset();
                Check(skill.Energy == 0 && ammo.Rounds[weapon] == 0 && ammo.Reserve[weapon] == 0,
                      "new round clears ammunition and energy");
            }
        var police = new RoleSkill(0);
        var armory = new WeaponAmmo();
        Fill(police, 3, 0);
        police.Hold(true, 1);
        Check(!police.Resupply(armory, 1, 1), "raised shield still blocks E");
        police.Hold(false, 1.5);
        Fill(police, 1, 1.5);
        Check(police.Resupply(armory, 1, 1.5) && Math.Abs(police.Energy - .5) < 1e-8,
              "instant E preserves fractional shield stock");
    }
    static void Network(int hostRole, int role, int weapon)
    {
        var pair = new EnergyTestPair(hostRole);
        var player = pair.Player(role);
        int cap = WeaponAmmo.Capacity(weapon);
        pair.Equip(role, weapon);
        pair.Charge(role, 6);
        player.SupplyKey(true);
        pair.Step();
        foreach (var peer in new[] { pair.Host, pair.Client })
            Check(peer.Skills[role].Energy == 3 && peer.Armories[role].Rounds[weapon] == cap &&
                      peer.Armories[role].Reserve[weapon] == 0 && peer.Armories[role].Reloading == 0,
                  "instant empty E converges without a reload wait on both peers");
        player.SupplyKey(true);
        pair.Step();
        Check(pair.Host.Skills[role].Energy == 0 && pair.Host.Armories[role].Reserve[weapon] == cap,
              "full-magazine E adds reserve");
        pair.Charge(role, 6);
        for (int i = 0; i < 2; i++)
        {
            player.SupplyKey(true);
            pair.Step();
        }
        Check(pair.Host.Armories[role].Reserve[weapon] == 3 * cap, "network reserve cap");
        pair.Charge(role, 3);
        player.SupplyKey(true);
        pair.Step();
        Check(pair.Host.Skills[role].Energy == 3 && pair.Client.Skills[role].Energy == 3,
              "full inventories preserve energy");
        pair.Host.Armories[role].Fire(weapon, pair.Now);
        pair.Exchange();
        player.SupplyKey(true);
        pair.Step();
        Check(pair.Host.Armories[role].Rounds[weapon] == cap &&
                  pair.Client.Armories[role].Rounds[weapon] == cap && pair.Host.Skills[role].Energy == 0,
              "full reserve does not prevent immediate refill of a missing magazine round");
        int clientRole = pair.Client.Role;
        pair.Client.Skills[clientRole].Energy = 6;
        pair.Client.Armories[clientRole].Reserve[weapon] = 0;
        pair.Exchange();
        Check(pair.Client.Skills[clientRole].Energy == pair.Host.Skills[clientRole].Energy &&
                  pair.Client.Armories[clientRole].Reserve[weapon] ==
                      pair.Host.Armories[clientRole].Reserve[weapon],
              "client cannot forge inventory or energy");

        pair = new EnergyTestPair(hostRole);
        role = pair.Client.Role;
        pair.Equip(role, weapon);
        pair.Charge(role, 3);
        pair.Client.SupplyKey(true);
        pair.Client.Observe(3, pair.Client.Poses[role], 450, false, false, pair.Now, .01);
        Check(pair.Host.Receive(pair.Client.Packet(), 2, pair.Now), "first E packet received");
        Check(pair.Host.Receive(pair.Client.Packet(), 2, pair.Now) &&
                  pair.Host.Armories[role].Rounds[weapon] == cap &&
                  pair.Host.Armories[role].Reserve[weapon] == 0 &&
                  pair.Host.Armories[role].Reloads[weapon] == 0 && pair.Host.Skills[role].Energy == 0,
              "retried E cannot duplicate its instant ammunition grant");
    }
    static void PendingAndShieldOrder(int hostRole)
    {
        var pair = new EnergyTestPair(hostRole);
        int role = pair.Client.Role;
        pair.Equip(role, 1);
        pair.Charge(role, 3);
        pair.Client.SupplyKey(true);
        pair.Client.Observe(3, pair.Client.Poses[role], 450, false, false, pair.Now, .01);
        pair.Client.SkillKeys(true, true);
        pair.Client.Observe(3, pair.Client.Poses[role], 450, false, false, pair.Now + .01, .01);
        DuelPacket pending = DuelProtocol.Decode(pair.Client.Packet(), pair.Now);
        Check(pending.Actions.Count == 1 && pending.Actions[0].Kind == SkillAction.SupplyUse,
              "unacknowledged E cannot also spend stale stock on Shift");
        pair.Step();
        pair.Client.SkillKeys(false, false);
        pair.Step();
        Check(pair.Host.Skills[role].Energy == 0 && !pair.Host.Skills[role].Shield &&
                  pair.Host.Armories[role].Rounds[1] == 30 && pair.Host.Armories[role].Reloading == 0,
              "pending E converges once");
        pair.Charge(role, 6);
        pair.Client.SkillKeys(true, true);
        pair.Client.SupplyKey(true);
        pair.Step();
        Check(pair.Host.Armories[role].Reserve[1] == 0 &&
                  (role == 0 ? pair.Host.Skills[role].Shield : pair.Host.Skills[role].Energy == 5),
              "simultaneous E/Shift prioritizes the role action");
        pair.Client.SkillKeys(false, false);
        pair.Step();

        // A client police can release, type, and raise before a packet is sent.
        // The host must apply those transitions in order, rather than blocking
        // the legitimate word based on a stale held flag.
        pair = new EnergyTestPair(1);
        pair.Charge(0, 3);
        var police = pair.Client;
        police.UpdateShield(true, pair.Now);
        police.UpdateShield(false, pair.Now + .1);
        Check(police.ConfirmSkillWord(police.Skills[0].Word, pair.Now + .1),
              "word after release accepted locally");
        police.UpdateShield(true, pair.Now + .1);
        string packet = police.Packet();
        Check(pair.Host.Receive(packet, 2, pair.Now + .1) && pair.Host.Skills[0].Shield &&
                  pair.Host.Skills[0].Cycle == 4 && pair.Host.Rejected == 0,
              "reliable release/word/raise batching keeps host authority and correct ordering");
        Check(pair.Host.Receive(police.Packet(), 2, pair.Now + .1) && pair.Host.Skills[0].Cycle == 4,
              "resending shield transitions and words cannot duplicate energy");
        police.Skills[0].Cycle += 100;
        police.UpdateShield(false, pair.Now + .2);
        Check(pair.Host.Receive(police.Packet(), 2, pair.Now + .2) && !pair.Host.Skills[0].Shield,
              "a shield release cannot get stuck behind a rejected word sequence");
    }
    static void Effects()
    {
        var power = new TypingPowerMode();
        var snapshot = new byte[ModSession.SnapshotBytes];
        for (int i = 0; i < 100; i++)
        {
            double now = i * .025;
            power.Stroke(true, now);
            power.WriteSnapshot(snapshot, now);
            int count = BitConverter.ToInt32(snapshot, SnapshotLayout.FireCount);
            Check(count > 0 && count <= 32, "flame pool remains bounded under fast typing");
            Check(Math.Abs(BitConverter.ToInt32(snapshot, SnapshotLayout.ShakeX)) <= 5 &&
                      Math.Abs(BitConverter.ToInt32(snapshot, SnapshotLayout.ShakeY)) <= 5,
                  "shake amplitude bounded");
            for (int p = 0; p < count; p++)
            {
                int at = SnapshotLayout.FireRects + p * 20;
                Check(BitConverter.ToInt32(snapshot, at + 8) > BitConverter.ToInt32(snapshot, at) &&
                          BitConverter.ToInt32(snapshot, at + 12) > BitConverter.ToInt32(snapshot, at + 4),
                      "flame rectangles have positive extent");
            }
        }
        power.WriteSnapshot(snapshot, 10);
        Check(BitConverter.ToInt32(snapshot, SnapshotLayout.FireCount) == 0 &&
                  BitConverter.ToInt32(snapshot, SnapshotLayout.ShakeX) == 0,
              "effects settle after typing");
        power.Stroke(false, 11);
        power.Clear();
        power.WriteSnapshot(snapshot, 11);
        Check(BitConverter.ToInt32(snapshot, SnapshotLayout.FireCount) == 0, "leaving match clears effects");
        Check(SkillIconFrames.BuildResupply().Length == 5 * 48 * 48 * 4, "five bounded pixel icon states");
        Check(AmmoHud.BuildReserve().Length == 91 * 4096, "finite reserve HUD bank");
    }

    internal static void RunSuite(string[] args)
    {
        try
        {
            Pure();
            foreach (int hostRole in new[] { 0, 1 })
                foreach (int role in new[] { 0, 1 })
                    foreach (int weapon in new[] { 1, 2 })
                        Network(hostRole, role, weapon);
            PendingAndShieldOrder(0);
            PendingAndShieldOrder(1);
            Effects();
            Console.WriteLine("PASS " + checks +
                              (" energy-priced resupply, finite ammo, shield ordering, authority/retry and " +
                               "typing effects checks"));
        }
        catch (Exception error)
        {
            Console.WriteLine("FAIL energy resupply after " + checks + ": " + error.Message);
            Environment.ExitCode = 1;
        }
    }
}
