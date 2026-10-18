using System;

namespace Switchboard.Game
{
    /// <summary>How many characters of a line show while it types out; the first one shows at once.</summary>
    public sealed class TypingReveal
    {
        private float progress;

        public int Total { get; private set; }
        public int Visible { get; private set; }
        public bool IsTyping => Visible < Total;

        public void Restart(int characterCount)
        {
            Total = Math.Max(0, characterCount);
            progress = Math.Min(1, Total);
            Visible = (int)progress;
        }

        public int Advance(float seconds, float charactersPerSecond)
        {
            if (!IsTyping) return Visible;
            progress += Math.Max(0, seconds) * Math.Max(0, charactersPerSecond);
            Visible = progress >= Total ? Total : (int)progress;
            return Visible;
        }
    }
}
