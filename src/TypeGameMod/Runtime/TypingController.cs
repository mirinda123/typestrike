namespace TypeGameMod
{
    // Native input is delivered by NativeTypingBridge. This controller has no
    // process handles: its mode, cursor and release gate can be exercised alone.
    internal sealed class TypingController
    {
        internal readonly TypingPowerMode Feedback = new TypingPowerMode();
        internal ArticleProgress Article { get; private set; }
        internal bool Enabled { get; private set; }
        internal bool WaitingForRelease { get; private set; }
        internal bool ControlsAllowed
        {
            get {
                return !Enabled && !WaitingForRelease;
            }
        }

        internal void SetArticle(ArticleProgress article)
        {
            Article = article;
        }

        internal void ResetArticle()
        {
            Article = null;
            Feedback.Clear();
        }

        internal void Reset()
        {
            Enabled = false;
            WaitingForRelease = false;
            ResetArticle();
        }

        internal void LeaveRound()
        {
            Enabled = false;
            Feedback.Clear();
        }

        internal void ReturnToMovement()
        {
            Enabled = false;
            WaitingForRelease = true;
        }

        // Called once for the sampled input frame. ControlsAllowed is a pure query.
        internal void UpdateReleaseGate(bool gameplayKeysHeld)
        {
            if (!gameplayKeysHeld)
                WaitingForRelease = false;
        }

        internal bool HandleKey(int key, int position, bool shield, double now)
        {
            if (key == 9)
            {
                if (shield)
                    return false;
                if (Enabled)
                    ReturnToMovement();
                else
                    Enabled = true;
            }
            if (Article == null)
                return false;
            if (key != 9 && Enabled)
                Feedback.Stroke(position != Article.Position, now);
            return true;
        }

        internal int ObserveWords(int position, RoleSkill skill)
        {
            // Full energy still advances the native article; it never banks words.
            return Article.Observe(position, skill.Cycle, skill.CanCharge);
        }
    }
}
