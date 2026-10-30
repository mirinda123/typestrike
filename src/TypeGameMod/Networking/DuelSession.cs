using System;
using System.Collections.Generic;

namespace TypeGameMod
{
    internal sealed partial class DuelSession
    {
        // Public match state consumed by the runtime and packet writer.
        internal readonly bool Host;
        internal int Role;
        internal uint Round;
        internal int Winner = -1, Speed = GameRules.DefaultBulletSpeed, RequestedScene = -1;
        internal uint ShotsFired, PelletsFired, Hits, PacketsReceived, Rejected, Blocks;
        internal readonly DuelHealth Health = new DuelHealth();
        internal readonly WeaponAmmo[] Armories = { new WeaponAmmo(), new WeaponAmmo() };
        internal readonly RoleSkill[] Skills = { new RoleSkill(0), new RoleSkill(1) };
        internal readonly DuelPose[] Poses = { DuelPose.Spawn(0), DuelPose.Spawn(1) };
        internal readonly List<DuelShot> Shots = new List<DuelShot>();

        readonly Func<int, DuelPose, ShotOrigin> locateShot;

        internal readonly Guid Token = Guid.NewGuid();
        Guid peerToken = Guid.Empty;
        bool acknowledged;
        uint peerId, peerSequence, sequence, speedSequence;
        uint peerRound, localScene, peerScene;
        double lastSeen = -100, observationTime;
        bool localReady, remoteReady;
        bool inLobby = true, abortRequested, everPlaying;

        readonly List<DuelFire> pendingFireActions = new List<DuelFire>();
        readonly List<SkillAction> pendingSkillActions = new List<SkillAction>();
        uint fireSequence, remoteFireAck, localFireAck, skillSequence, skillAck;
        double lastFire = -100;
        bool reloadPressed, supplyPressed, shiftHeld, shiftPressed, shieldRequested;

        readonly DuelPose[] previousPoses = { DuelPose.Spawn(0), DuelPose.Spawn(1) };
        readonly MapCover cover;
        readonly MovementMap movement;

        internal bool Compatible
        {
            get {
                return acknowledged && peerToken != Guid.Empty;
            }
        }

        internal bool Playing
        {
            get {
                return Compatible && Round != 0 && Winner < 0 && peerRound == Round &&
                       peerScene == GameScene.Playing && localScene == GameScene.Playing;
            }
        }

        internal bool Connected(double now)
        {
            return Compatible && now - lastSeen < 3;
        }

        internal void ReloadKey(bool pressed)
        {
            reloadPressed = pressed;
        }

        internal DuelSession(bool host, int role, int speed, Func<int, DuelPose, ShotOrigin> locateShot)
            : this(host, role, speed, locateShot, MapCover.City, MovementMap.City)
        {
        }

        internal DuelSession(bool host, int role, int speed, Func<int, DuelPose, ShotOrigin> locateShot,
                             MapCover cover, MovementMap movement)
        {
            Host = host;
            Role = role;
            Speed = GameRules.ClampBulletSpeed(speed);
            this.cover = cover;
            this.movement = movement;
            this.locateShot = locateShot;
        }

        internal void Observe(uint scene, DuelPose pose, int speed, bool fire, bool ready, double now,
                              double dt)
        {
            observationTime = now;
            Skills[0].Tick(now);
            Skills[1].Tick(now);
            Armories[0].Tick(now);
            Armories[1].Tick(now);
            bool reload = reloadPressed;
            reloadPressed = false;
            bool supply = supplyPressed;
            supplyPressed = false;
            uint beforeScene = localScene;
            localScene = scene;
            if (pose.Valid)
                Poses[Role] = pose;
            if (ready)
                Ready();
            if (Host && speed != Speed)
            {
                Speed = GameRules.ClampBulletSpeed(speed);
                speedSequence++;
            }
            if (!Connected(now))
            {
                Skills[0].Hold(false, now);
                Shots.Clear();
                pendingFireActions.Clear();
                return;
            }
            if (Host && Round != 0 && GameScene.IsLobby(scene))
            {
                Lobby();
                abortRequested = false;
                RequestedScene = -1;
            }
            if (Host && abortRequested)
            {
                RequestedScene = GameScene.HostLobby;
                return;
            }
            if (Host && inLobby && Round != 0 && scene == GameScene.Playing && GameScene.IsLobby(beforeScene))
                ResetRound(Round + 1);
            if (Host && Round == 0 && scene == GameScene.Playing)
                ResetRound(1);
            if (Host && Winner >= 0 && localReady && remoteReady)
            {
                ResetRound(Round + 1);
                RequestedScene = GameScene.Playing;
            }
            if (Winner >= 0)
            {
                UpdateShield(false, now);
                RequestedScene = Winner == Role ? GameScene.Victory : GameScene.Defeat;
                return;
            }
            if (!Playing)
            {
                UpdateShield(false, now);
                return;
            }
            everPlaying = true;
            UpdateShield(shiftHeld, now);
            ProcessLocalActions(pose, reload, fire, supply, now);
            if (Host)
                AdvanceProjectiles(now, dt);
        }
    }
}
