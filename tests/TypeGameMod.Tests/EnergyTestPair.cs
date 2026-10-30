using System;
using TypeGameMod;

// Real protocol/state-machine pair, entirely in memory; never opens a socket.
internal sealed class EnergyTestPair
{
    internal readonly DuelSession Host, Client;
    internal double Now;
    internal EnergyTestPair(int hostRole) : this(hostRole, TestMap.Movement)
    {
    }
    internal EnergyTestPair(int hostRole, MovementMap movement)
    {
        Host = TestDuel.Create(true, hostRole, 450, TestMap.Empty, movement);
        Client = TestDuel.Create(false, 1 - hostRole, 450, TestMap.Empty, movement);
        for (int i = 0; i < 20; i++)
            Step();
        if (!Host.Playing || !Client.Playing)
            throw new Exception("Energy fixture did not start");
    }
    internal DuelSession Player(int role)
    {
        return role == Host.Role ? Host : Client;
    }
    internal void Exchange()
    {
        if (!Host.Receive(Client.Packet(), 2, Now) || !Client.Receive(Host.Packet(), 1, Now))
            throw new Exception("Energy fixture packet rejected");
    }
    internal void Step(double seconds = .02)
    {
        Now += seconds;
        Client.Observe(3, Client.Poses[Client.Role], 450, false, false, Now, seconds);
        if (!Host.Receive(Client.Packet(), 2, Now))
            throw new Exception("Energy client packet rejected");
        Host.Observe(3, Host.Poses[Host.Role], 450, false, false, Now, seconds);
        if (!Client.Receive(Host.Packet(), 1, Now))
            throw new Exception("Energy host packet rejected");
    }
    internal void Equip(int role, int weapon)
    {
        DuelSession player = Player(role);
        DuelPose pose = player.Poses[role];
        pose.Weapon = (byte)weapon;
        player.Poses[role] = pose;
        Step();
    }
    internal void Charge(int role, int words)
    {
        for (int i = 0; i < words; i++)
        {
            DuelSession player = Player(role);
            if (!player.ConfirmSkillWord(player.Skills[role].Word, Now))
                throw new Exception("Energy fixture word rejected");
            Step();
        }
    }
}
