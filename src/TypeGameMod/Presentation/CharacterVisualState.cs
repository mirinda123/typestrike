namespace TypeGameMod
{
    // A single role's visual inputs, resolved once from local or LAN state.
    internal struct CharacterVisualState
    {
        internal bool Active, Moving, Shield;
        internal int Weapon;
        internal uint AkShots, ShotgunShots;
        internal bool Unarmed
        {
            get {
                return Active && Weapon == GameRules.Unarmed && !Shield;
            }
        }

        internal bool Ak47
        {
            get {
                return Active && Weapon == GameRules.Ak47 && !Shield;
            }
        }

        internal bool Shotgun
        {
            get {
                return Active && Weapon == GameRules.Shotgun && !Shield;
            }
        }
    }
}
