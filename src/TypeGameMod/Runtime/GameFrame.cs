namespace TypeGameMod
{
    // One observation of the native scene. Role 2 means no playable minigame.
    internal sealed class GameFrame
    {
        internal uint Object, Scene, Role = 2;
        internal ushort Started;
        internal bool Online;
        internal byte[] Players;
        internal bool OfflinePlaying
        {
            get {
                return Scene == 3 && !Online && Role <= 1 && Players != null;
            }
        }

        internal bool HasNetworkPlayers
        {
            get {
                return Online && Role <= 1 && Players != null;
            }
        }
    }
}
