using System.Collections.Generic;
using NUnit.Framework;

namespace Switchboard.Domain.Tests
{
    /// <summary>The quick-connect tip (D59): connecting within a few seconds of taking a card pays a tip once.</summary>
    public sealed class QuickConnectTipTests
    {
        private static readonly CordId Cord = new CordId(0);
        private const double Window = 5;
        private const long Tip = 5;

        private TestWorld world;
        private CallService calls;
        private Wallet wallet;
        private EarningsService earnings;
        private OperatorDesk desk;
        private List<QuickConnectTipPaidArgs> tips;
        private double multiplier;

        [SetUp]
        public void SetUp() => Build(new QuickConnectTip(Window, Tip));

        private void Build(QuickConnectTip tip)
        {
            world = new TestWorld();
            calls = new CallService();
            wallet = new Wallet();
            multiplier = 1;
            earnings = new EarningsService(calls, wallet, () => multiplier, tip);
            desk = new OperatorDesk(calls, new ConnectionRule(), new FakeSwitchboard());
            tips = new List<QuickConnectTipPaidArgs>();
            earnings.QuickConnectTipPaid += e => tips.Add(e);
        }

        // 10¢ fee, no per-second pay, so the wallet shows exactly the fee and the tip.
        private Call AddAdaWantsBasil() => calls.Add(world.AdaWantsBasil(centsPerSecond: 0, durationSeconds: 20, connectionFeeCents: 10));

        private Call TakeAdaWantsBasil()
        {
            Call call = AddAdaWantsBasil();
            Assert.That(desk.TryTake(call.Id), Is.True);
            return call;
        }

        private void Advance(double seconds)
        {
            const double frame = 0.05;
            for (double elapsed = 0; elapsed < seconds - 1e-9; elapsed += frame)
            {
                earnings.Tick(frame);
                calls.Tick(frame);
            }
        }

        private void ConnectRightPair() => desk.OnCircuitClosed(Cord, TestWorld.A1, TestWorld.B2);

        [Test]
        public void AQuickConnectionPaysTheTipOnTopOfTheFee()
        {
            Call call = TakeAdaWantsBasil();
            Advance(3);
            ConnectRightPair();

            Assert.That(call.State, Is.EqualTo(CallState.Connected));
            Assert.That(wallet.Cents, Is.EqualTo(10 + Tip));
            Assert.That(call.TipCents, Is.EqualTo(Tip));
            Assert.That(call.CentsEarned, Is.EqualTo(10 + Tip), "the card's earnings include the tip");
            Assert.That(earnings.TotalPaidCents, Is.EqualTo(10 + Tip), "and so do the career earnings");
            Assert.That(tips.Count, Is.EqualTo(1));
            Assert.That(tips[0].Call, Is.SameAs(call));
            Assert.That(tips[0].Cents, Is.EqualTo(Tip));
        }

        [Test]
        public void TheWindowEndIsStillQuick()
        {
            Call call = TakeAdaWantsBasil();
            Advance(Window);
            Assert.That(call.SecondsInHand, Is.EqualTo(Window).Within(1e-6));
            ConnectRightPair();
            Assert.That(tips.Count, Is.EqualTo(1));
        }

        [Test]
        public void ASlowConnectionPaysOnlyTheFee()
        {
            Call call = TakeAdaWantsBasil();
            Advance(Window + 0.5);
            ConnectRightPair();

            Assert.That(call.State, Is.EqualTo(CallState.Connected));
            Assert.That(wallet.Cents, Is.EqualTo(10));
            Assert.That(call.TipCents, Is.EqualTo(0));
            Assert.That(tips, Is.Empty);
        }

        [Test]
        public void TheWindowStartsWhenTheCardIsTakenNotWhenItRings()
        {
            Call call = AddAdaWantsBasil();
            Advance(30);
            Assert.That(call.SecondsInHand, Is.EqualTo(0), "a waiting card's time doesn't count");
            Assert.That(desk.TryTake(call.Id), Is.True);
            Advance(1);
            ConnectRightPair();
            Assert.That(tips.Count, Is.EqualTo(1));
        }

        [Test]
        public void HalfPatchingDoesNotStopTheClock()
        {
            Call call = TakeAdaWantsBasil();
            desk.OnPlugInserted(Cord, TestWorld.A1);
            Assert.That(call.State, Is.EqualTo(CallState.Connecting));
            Advance(Window + 1);
            ConnectRightPair();
            Assert.That(tips, Is.Empty);
        }

        [Test]
        public void ACallThatConnectsAsItIsTakenGetsTheTip()
        {
            ConnectRightPair();
            Call call = TakeAdaWantsBasil();

            Assert.That(call.State, Is.EqualTo(CallState.Connected), "the circuit was already plugged in (D45)");
            Assert.That(call.SecondsInHand, Is.EqualTo(0));
            Assert.That(tips.Count, Is.EqualTo(1));
            Assert.That(wallet.Cents, Is.EqualTo(10 + Tip));
        }

        [Test]
        public void ReconnectingAfterHoldPaysNoSecondTip()
        {
            Call call = TakeAdaWantsBasil();
            ConnectRightPair();
            Assert.That(tips.Count, Is.EqualTo(1));

            desk.OnCircuitOpened(Cord, TestWorld.A1, TestWorld.B2);
            Assert.That(call.State, Is.EqualTo(CallState.OnHold));
            Advance(1);
            ConnectRightPair();

            Assert.That(call.State, Is.EqualTo(CallState.Connected));
            Assert.That(tips.Count, Is.EqualTo(1));
            Assert.That(wallet.Cents, Is.EqualTo(10 + Tip));
        }

        [Test]
        public void ASlowCallStaysTiplessAfterHold()
        {
            Call call = TakeAdaWantsBasil();
            Advance(Window + 1);
            ConnectRightPair();
            desk.OnCircuitOpened(Cord, TestWorld.A1, TestWorld.B2);
            ConnectRightPair();

            Assert.That(call.State, Is.EqualTo(CallState.Connected));
            Assert.That(tips, Is.Empty);
        }

        [Test]
        public void AWrongNumberPaysNoTip()
        {
            Call call = TakeAdaWantsBasil();
            desk.OnCircuitClosed(Cord, TestWorld.A1, TestWorld.C3, TestWorld.A1);

            Assert.That(call.EndReason, Is.EqualTo(CallEndReason.WrongConnection));
            Assert.That(tips, Is.Empty);
            Assert.That(wallet.Cents, Is.EqualTo(0));
        }

        [Test]
        public void TheIncomeMultiplierMultipliesTheTipLikeTheFee()
        {
            multiplier = 4;
            TakeAdaWantsBasil();
            ConnectRightPair();

            Assert.That(tips[0].Cents, Is.EqualTo(Tip * 4));
            Assert.That(wallet.Cents, Is.EqualTo(10 * 4 + Tip * 4));
        }

        [Test]
        public void AFreeCallStillGetsTheTip()
        {
            Call call = calls.Add(world.AdaWantsBasil(centsPerSecond: 0, connectionFeeCents: 0));
            desk.TryTake(call.Id);
            ConnectRightPair();
            Assert.That(tips.Count, Is.EqualTo(1));
            Assert.That(wallet.Cents, Is.EqualTo(Tip));
        }

        [TestCase(0, 5)]
        [TestCase(5, 0)]
        public void AZeroWindowOrTipTurnsTheTipOff(double seconds, long cents)
        {
            Build(new QuickConnectTip(seconds, cents));
            Assert.That(earnings.QuickTip.IsOn, Is.False);
            TakeAdaWantsBasil();
            ConnectRightPair();
            Assert.That(tips, Is.Empty);
            Assert.That(wallet.Cents, Is.EqualTo(10));
        }

        [Test]
        public void WithoutATipTheServiceNeverPaysOne()
        {
            calls = new CallService();
            wallet = new Wallet();
            earnings = new EarningsService(calls, wallet);
            desk = new OperatorDesk(calls, new ConnectionRule(), new FakeSwitchboard());
            Assert.That(earnings.QuickTip.IsOn, Is.False, "the default is off");
            TakeAdaWantsBasil();
            ConnectRightPair();
            Assert.That(wallet.Cents, Is.EqualTo(10));
        }

        // The pause menu sets Time.timeScale to 0, so the game loop ticks the session with 0 seconds (D56).
        [Test]
        public void ThePausedGameDoesNotRunTheWindowDown()
        {
            world.Content.QuickConnectSeconds = Window;
            world.Content.QuickConnectTipCents = Tip;
            var session = new GameSession(world.Content, new FakeSwitchboard());
            session.Tick(0.1);
            Call call = session.Calls.Live[0];
            Assert.That(session.Desk.TryTake(call.Id), Is.True);
            for (int i = 0; i < 40; i++) session.Tick(0.1);
            for (int i = 0; i < 1000; i++) session.Tick(0);
            Assert.That(call.SecondsInHand, Is.EqualTo(4).Within(1e-6), "only game time counts");

            long before = session.Wallet.Cents;
            session.Desk.OnCircuitClosed(Cord, TestWorld.A1, TestWorld.B2);
            Assert.That(call.TipCents, Is.EqualTo(Tip), "the session reads the window and the tip from the content");
            Assert.That(session.Wallet.Cents - before, Is.EqualTo(10 + Tip));
        }
    }
}
