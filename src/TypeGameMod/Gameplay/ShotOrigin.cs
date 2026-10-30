namespace TypeGameMod
{
    // World coordinates supplied by the runtime, independent of animation data.
    internal struct ShotOrigin
    {
        internal readonly double X, Y;
        internal ShotOrigin(double x, double y)
        {
            X = x;
            Y = y;
        }
    }
}
