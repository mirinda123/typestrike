using TypeGameMod;

internal static class TestDuel
{
    internal static DuelSession Create(bool host, int role, int speed, int frame = 0)
    {
        return Create(host, role, speed, MapCover.City, MovementMap.City, frame);
    }

    internal static DuelSession Create(bool host, int role, int speed, MapCover cover, int frame = 0)
    {
        return Create(host, role, speed, cover, new MovementMap(cover), frame);
    }

    internal static DuelSession Create(bool host, int role, int speed, MapCover cover, MovementMap movement,
                                       int frame = 0)
    {
        return new DuelSession(host, role, speed, delegate(int owner, DuelPose pose) {
            double x, y;
            WeaponMuzzle.Offset(owner, pose.Weapon, pose.Heading, pose.Moving != 0, frame, out x, out y);
            return new ShotOrigin(pose.X + x, pose.Y + y);
        }, cover, movement);
    }
}
