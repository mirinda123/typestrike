namespace TypeGameMod
{
    // Scene numbers are part of the original game's interface.
    internal static class GameScene
    {
        internal const int HostLobby = 1;
        internal const int JoinLobby = 2;
        internal const int Playing = 3;
        internal const int Victory = 4;
        internal const int Defeat = 5;
        internal static bool IsLobby(uint scene)
        {
            return scene == HostLobby || scene == JoinLobby;
        }

        internal static bool IsResult(uint scene)
        {
            return scene == Victory || scene == Defeat;
        }
    }
}
