namespace Switchboard.Domain
{
    public sealed class GameClock : IGameClock
    {
        public double Now { get; private set; }

        public void Advance(double dt)
        {
            if (dt > 0) Now += dt;
        }
    }
}
