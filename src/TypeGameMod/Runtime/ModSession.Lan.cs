using System;

namespace TypeGameMod
{
    internal sealed partial class ModSession
    {
        DuelSession networkMatch;
        uint networkObject, networkPlayerId, receivedSequence, sentSequence, poseMailboxSequence,
            lastNetworkRound;
        int lastNetworkWinner = -1;
        double lastNetworkSend = -100;
        bool hadNetworkPeer;
        volatile string networkStatus = "等待另一台电脑运行相同版本 MOD";
        internal bool OwnsSpeed
        {
            get {
                DuelSession match = networkMatch;
                return match == null || match.Host;
            }
        }

        void EndLan()
        {
            ResetWeaponWheel();
            ResetCombatAudio();
            typing.Reset();
            nativeSkills.ResetBlinkTracking();
            skillRound = 0;
            skillRole = -1;
            nativeSkills.SetPoliceShield(false);
            networkMatch = null;
            networkObject = networkPlayerId = 0;
            hadNetworkPeer = false;
            equippedWeapon = 0;
            lastNetworkRound = 0;
            lastNetworkWinner = -1;
            Write(ControlBase + Control.PeerCompatible, BitConverter.GetBytes(0u));
            Write(ControlBase + Control.SceneRequested, BitConverter.GetBytes(0u));
            Write(caveBase + CaveLayout.PoseMailbox, new byte[64]);
        }

        byte[] TickNetwork(GameFrame frame, InputFrame input, double now, double dt)
        {
            uint gameObject = frame.Object, scene = frame.Scene, role = frame.Role, pose = input.Heading;
            byte[] players = frame.Players;
            int x = input.X, y = input.Y, select = input.Selection;
            bool fire = input.Fire, focused = input.Focused, slower = input.Slower, faster = input.Faster,
                 reloadPressed = input.Reload;
            byte[] snapshot = new byte[SnapshotBytes];
            BinaryFields.Write(snapshot, SnapshotLayout.Focused, focused ? 1u : 0u);
            BinaryFields.Write(snapshot, SnapshotLayout.Heading, pose);
            byte[] network = Read(ControlBase + Control.NetworkObject, 84);
            uint net = BitConverter.ToUInt32(network, 0), id = BitConverter.ToUInt32(network, 80);
            bool host = BitConverter.ToUInt32(network, 4) != 0,
                 connected = BitConverter.ToUInt32(network, 12) != 0;
            if (net != 0)
            {
                byte[] live = Read(net + 0x3c, 0x18);
                connected = BitConverter.ToUInt16(live, 2) != 0;
                host = BitConverter.ToUInt16(live, 20) != 0;
                id = BitConverter.ToUInt32(live, 16);
            }
            if (!connected || net == 0 || id == 0)
            {
                if (networkMatch != null)
                    EndLan();
                equippedWeapon = 0;
                Write(ControlBase + Control.VelocityX, new byte[16]);
                networkStatus = "使用原版多人游戏菜单建房或加入房间";
                return snapshot;
            }
            if (networkMatch == null &&
                BitConverter.ToUInt32(Read(ControlBase + Control.SceneRequested, 4), 0) != 0)
            {
                Write(ControlBase + Control.VelocityX, new byte[16]);
                return snapshot;
            }
            if (networkMatch == null || networkObject != net || networkPlayerId != id ||
                networkMatch.Role != (int)role || networkMatch.Host != host)
            {
                EndLan();
                networkMatch = new DuelSession(host, (int)role, SelectedBulletSpeed, LocateShotOrigin);
                networkObject = net;
                networkPlayerId = id;
                receivedSequence = BitConverter.ToUInt32(Read(ControlBase + Control.ReceiveSequence, 4), 0);
                sentSequence = BitConverter.ToUInt32(Read(ControlBase + Control.SendAcknowledged, 4), 0);
                lastNetworkSend = -100;
                lastNetworkRound = 0;
                log("DirectPlay duel session " + (host ? "host" : "client") + " role=" + role +
                    " player=" + id);
            }
            ReceiveLan(now);
            bool compatible = networkMatch.Connected(now);
            if (compatible && !hadNetworkPeer)
            {
                hadNetworkPeer = true;
                log("Compatible LAN mod peer connected over original DirectPlay.");
            }
            if (!compatible && hadNetworkPeer)
            {
                EndLan();
                QueueScene(gameObject, scene, host ? 1 : 2);
                Write(ControlBase + Control.VelocityX, new byte[16]);
                networkStatus = "对方连接中断，返回原版房间界面";
                return snapshot;
            }
            if (focused && host && slower)
                SelectedBulletSpeed = Math.Max(100, SelectedBulletSpeed - GameRules.BulletSpeedStep);
            if (focused && host && faster)
                SelectedBulletSpeed = Math.Min(2400, SelectedBulletSpeed + GameRules.BulletSpeedStep);
            bool ready = input.ConfirmResult;
            int offset = (int)role * 56;
            var own = new DuelPose { X = BitConverter.ToSingle(players, offset + 4),
                                     Y = BitConverter.ToSingle(players, offset + 8),
                                     Heading = (byte)pose,
                                     Weapon = (byte)equippedWeapon,
                                     Moving = (byte)((x != 0 || y != 0) ? 1 : 0),
                                     Phase = (byte)(players[offset + 28] & 3) };
            if (!own.Valid)
                own = DuelPose.Spawn((int)role);
            if (skillRound != networkMatch.Round || skillRole != (int)role)
            {
                skillRound = networkMatch.Round;
                skillRole = (int)role;
                typing.Reset();
                nativeSkills.ResetBlinkTracking();
            }
            ReadSkillCharacters(scene == GameScene.Playing && networkMatch.Playing, gameObject, role, input,
                                now);
            if (!typing.ControlsAllowed)
            {
                x = y = 0;
                fire = false;
                own.Moving = 0;
                Write(ControlBase + Control.VelocityX, new byte[16]);
            }
            int desiredWeapon = ChooseWeapon(select, scene == GameScene.Playing && networkMatch.Playing &&
                                                         compatible && focused && typing.ControlsAllowed &&
                                                         networkMatch.Round == lastNetworkRound);
            if (desiredWeapon != equippedWeapon)
            {
                equippedWeapon = desiredWeapon;
                log("LAN local weapon=" + equippedWeapon);
            }
            own.Weapon = (byte)equippedWeapon;
            nativeSkills.ApplyPendingPose(ref own, now);
            networkMatch.SkillKeys(focused && input.ShiftHeld && typing.ControlsAllowed,
                                   focused && input.ShiftPressed && typing.ControlsAllowed);
            networkMatch.ReloadKey(focused && typing.ControlsAllowed && reloadPressed);
            networkMatch.SupplyKey(focused && typing.ControlsAllowed && input.Supply);
            networkMatch.Observe(scene, own, SelectedBulletSpeed, focused && fire, ready, now, dt);
            SelectedBulletSpeed = networkMatch.Speed;
            compatible = networkMatch.Connected(now);
            nativeSkills.QueueBlink(gameObject, role, networkMatch.Skills[role], now);
            if (networkMatch.Round != lastNetworkRound)
            {
                ResetWeaponWheel();
                ResetCombatAudio();
                lastNetworkRound = networkMatch.Round;
                equippedWeapon = 0;
                lastNetworkWinner = -1;
                log("LAN round=" + networkMatch.Round + " shared speed=" + networkMatch.Speed);
            }
            if (networkMatch.Winner >= 0 && lastNetworkWinner != networkMatch.Winner)
            {
                lastNetworkWinner = networkMatch.Winner;
                log("LAN authoritative winner=" + networkMatch.Winner + " round=" + networkMatch.Round);
            }
            QueueScene(gameObject, scene, networkMatch.RequestedScene);
            bool playing = networkMatch.Playing && compatible;
            if (!playing)
                Write(ControlBase + Control.VelocityX, new byte[16]);
            PublishPoses(gameObject, playing);
            Write(ControlBase + Control.PeerCompatible, BitConverter.GetBytes(compatible ? 1u : 0u));
            SendLan(now);
            if (!compatible)
                networkStatus = "已进入原版房间；等待对方运行兼容的 MOD（弹药协议）";
            else if (networkMatch.Winner >= 0)
                networkStatus = (networkMatch.Winner == role ? "你获胜了！" : "对方获胜了。") +
                                "双方按 Enter 开始下一局";
            else if (playing)
                networkStatus = "局域网对战 · 第 " + networkMatch.Round + " 局 · " +
                                (host ? "房主" : "玩家") + " · " + networkMatch.Speed + " px/s（房主设置）";
            else
                networkStatus = "MOD 已连通；由房主开始游戏，等待双方进入本局";
            WriteNetworkSnapshot(frame, input, snapshot, compatible, playing, now);
            return snapshot;
        }
    }
}
