using NUnit.Framework;

namespace Switchboard.Domain.Tests
{
    public sealed class GameSessionTests
    {
        private TestWorld world;
        private FakeSwitchboard board;
        private GameSession session;

        [SetUp]
        public void SetUp()
        {
            world = new TestWorld();
            world.Content.StartingCents = 7;
            board = new FakeSwitchboard();
            session = new GameSession(world.Content, new SeededRandom(21), board);
        }

        private void Advance(double seconds)
        {
            for (double elapsed = 0; elapsed < seconds - 1e-9; elapsed += 0.05) session.Tick(0.05);
        }

        [Test]
        public void StartsWithTheConfiguredMoneyAndAQuietBoard()
        {
            Assert.That(session.Wallet.Cents, Is.EqualTo(7));
            Assert.That(session.Calls.Live, Is.Empty);
            Assert.That(session.Directory.ListedEntries.Count, Is.EqualTo(3));
        }

        [Test]
        public void FirstCallRingsAboutTwoSecondsAfterStart()
        {
            Advance(1.9);
            Assert.That(session.Calls.Live, Is.Empty);
            Advance(0.2);
            Assert.That(session.Calls.Live.Count, Is.EqualTo(1));
            Assert.That(session.Clock.Now, Is.EqualTo(2.1).Within(1e-6));
        }

        [Test]
        public void PatchingTheRightPairPaysUntilTheCallFinishes()
        {
            Advance(2.1);
            Call call = session.Calls.Live[0];
            Assert.That(session.Desk.TryTake(call.Id), Is.True);
            session.Desk.OnCircuitClosed(new CordId(0),
                session.Directory.JackOf(call.Request.Recipient),
                session.Directory.JackOf(call.Request.CallerPerson));
            Assert.That(call.State, Is.EqualTo(CallState.Connected));

            Advance(call.Request.DurationSeconds + 0.5);

            long expected = 7 + (long)(call.Request.ConnectionFeeCents + call.Request.CentsPerSecond * call.Request.DurationSeconds);
            Assert.That(call.Request.ConnectionFeeCents, Is.GreaterThan(0));
            Assert.That(call.EndReason, Is.EqualTo(CallEndReason.Finished));
            Assert.That(session.Wallet.Cents, Is.EqualTo(expected));
            Assert.That(board.Returned, Is.EqualTo(new[] { new CordId(0) }));
        }
    }
}
