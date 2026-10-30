using System;
using System.IO;

namespace TypeGameMod
{
    internal sealed partial class DuelSession
    {
        internal bool Receive(string text, uint sender, double now)
        {
            if (sender == 0)
                return false;
            DuelPacket packet;
            try
            {
                packet = DuelProtocol.Decode(text, now);
            }
            catch (EndOfStreamException)
            {
                Rejected++;
                return false;
            }
            catch (FormatException)
            {
                Rejected++;
                return false;
            }
            catch (ArgumentException)
            {
                Rejected++;
                return false;
            }
            if (packet == null || packet.Host == Host || packet.Role != 1 - Role ||
                (packet.AcknowledgedToken != Guid.Empty && packet.AcknowledgedToken != Token))
                return false;
            return ApplyPacket(packet, sender, now);
        }

        bool ApplyPacket(DuelPacket packet, uint sender, double now)
        {
            if (peerToken != Guid.Empty && (peerToken != packet.Token || peerId != sender))
                return false;
            if (packet.Sequence <= peerSequence)
                return false;
            peerToken = packet.Token;
            peerId = sender;
            peerSequence = packet.Sequence;
            acknowledged = packet.AcknowledgedToken == Token;
            lastSeen = now;
            observationTime = now;
            PacketsReceived++;
            peerScene = packet.Scene;
            peerRound = packet.Round;
            if (!Compatible)
                return true;
            if (Host)
                ApplyClientActions(packet, now);
            else
                ApplyHostSnapshot(packet, now);
            return true;
        }

        bool CanApplyPeerActions(uint peerScene)
        {
            return Round != 0 && Winner < 0 && peerScene == GameScene.Playing &&
                   localScene == GameScene.Playing;
        }

        void ApplyClientActions(DuelPacket packet, double now)
        {
            if (packet.Round != Round)
                return;
            if (packet.Scene == GameScene.Playing)
            {
                Poses[packet.Role] = packet.Pose;
                Armories[packet.Role].Select(packet.Pose.Weapon, now);
            }
            Skills[packet.Role].Tick(now);
            foreach (SkillAction action in packet.Actions)
            {
                if (action.Round != Round || action.Id <= skillAck || !CanApplyPeerActions(packet.Scene))
                    continue;
                skillAck = action.Id;
                if (!ApplySkillAction(action, packet.Role, now))
                    Rejected++;
            }
            if (packet.Scene != GameScene.Playing || localScene != GameScene.Playing || Winner >= 0)
                Skills[packet.Role].Hold(false, now);
            remoteReady = packet.Ready;
            if (GameScene.IsLobby(packet.Scene) && everPlaying &&
                (localScene == GameScene.Playing || GameScene.IsResult(localScene)))
            {
                Lobby();
                abortRequested = true;
                RequestedScene = GameScene.HostLobby;
                return;
            }
            foreach (DuelFire action in packet.Fires)
            {
                if (action.Round != Round || action.Id <= remoteFireAck || !CanApplyPeerActions(packet.Scene))
                    continue;
                remoteFireAck = action.Id;
                if (!ApplyFireAction(action, packet.Role, now))
                    Rejected++;
            }
        }

        void ApplyHostSnapshot(DuelPacket packet, double now)
        {
            if (packet.Round < Round)
                return;
            if (packet.Round > Round)
            {
                ResetRound(packet.Round);
                if (GameScene.IsResult(localScene))
                    RequestedScene = GameScene.Playing;
            }
            Poses[packet.Role] = packet.Pose;
            Speed = packet.Speed;
            Winner = packet.Winner;
            Blocks = packet.Blocks;
            remoteReady = packet.Ready;
            Health.CopyFrom(packet.Health);
            Hits = packet.Hits;
            Armories[0] = packet.Armories[0];
            Armories[1] = packet.Armories[1];
            UpdateShotStatistics();
            pendingSkillActions.RemoveAll(action => action.Id <= packet.SkillAck || action.Round != Round);
            Skills[packet.Role] = packet.Skills[packet.Role];
            // Keep local skill prediction until the host acknowledges its actions.
            if (pendingSkillActions.Count == 0)
                Skills[Role] = packet.Skills[Role];
            if (GameScene.IsLobby(packet.Scene) && Round != 0)
            {
                Lobby();
                RequestedScene = GameScene.HostLobby;
                return;
            }
            if (Winner >= 0)
                RequestedScene = Winner == Role ? GameScene.Victory : GameScene.Defeat;
            localFireAck = packet.FireAck;
            pendingFireActions.RemoveAll(action => action.Id <= packet.FireAck || action.Round != Round);
            Shots.Clear();
            Shots.AddRange(packet.Shots);
        }

        void UpdateShotStatistics()
        {
            uint akShots = Armories[0].Fired[GameRules.Ak47] + Armories[1].Fired[GameRules.Ak47];
            uint shotgunShots = Armories[0].Fired[GameRules.Shotgun] + Armories[1].Fired[GameRules.Shotgun];
            ShotsFired = akShots + shotgunShots;
            PelletsFired = akShots + GameRules.ShotgunPellets * shotgunShots;
        }
    }
}
