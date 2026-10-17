using System;

namespace Switchboard.Domain
{
    /// <summary>Builds every domain service in dependency order and owns the tick order (Design 4.4).</summary>
    public sealed class GameSession
    {
        private readonly GameClock clock = new GameClock();
        private readonly CallSpawner spawner;

        public GameSession(IGameContent content, IRandom random, ISwitchboard switchboard)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));
            Directory = new PhoneDirectory(content.Directory);
            Wallet = new Wallet(content.StartingCents);
            Calls = new CallService();
            Earnings = new EarningsService(Calls, Wallet);
            spawner = new CallSpawner(Calls, new CallFactory(random), Directory, content.Callers, random);
            Desk = new OperatorDesk(Calls, new ConnectionRule(Directory), switchboard);
        }

        public IGameClock Clock => clock;
        public PhoneDirectory Directory { get; }
        public Wallet Wallet { get; }
        public CallService Calls { get; }
        public EarningsService Earnings { get; }
        public OperatorDesk Desk { get; }

        public void Tick(double dt)
        {
            if (!(dt > 0)) return;
            clock.Advance(dt);
            Earnings.Tick(dt);
            Calls.Tick(dt);
            spawner.Tick(dt);
        }
    }
}
