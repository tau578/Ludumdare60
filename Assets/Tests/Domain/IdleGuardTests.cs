using System.Collections.Generic;
using NUnit.Framework;

namespace Switchboard.Domain.Tests
{
    /// <summary>Walking away never drains money: after a minute without input, calls that carry fines stop (D16).</summary>
    public sealed class IdleGuardTests
    {
        private EmergencyWorld world;
        private GameSession session;
        private List<FineAppliedArgs> fines;
        private int nextCord;

        [SetUp]
        public void SetUp()
        {
            // Emergencies are as likely as regular calls, so the guard has plenty to hold back.
            world = new EmergencyWorld(count: 12, regularWeight: 1, emergencyWeight: 1);
            session = new GameSession(world.Content, new SeededRandom(16), new FakeSwitchboard { CordCount = 6 });
            fines = new List<FineAppliedArgs>();
            session.Penalties.FineApplied += fines.Add;
        }

        // Connecting through the desk is not input: the guard only hears ReportInput.
        private void Advance(double seconds, bool input, bool connectRegular = true)
        {
            int ticks = (int)System.Math.Round(seconds / 0.05);
            for (int i = 0; i < ticks; i++)
            {
                if (input) session.Idle.ReportInput();
                session.Tick(0.05);
                if (!connectRegular) continue;
                for (int c = session.Calls.Live.Count - 1; c >= 0; c--)
                {
                    Call call = session.Calls.Live[c];
                    if (call.State != CallState.Incoming || call.Request.Type != world.Regular) continue;
                    session.Desk.TryTake(call.Id);
                    session.Desk.OnCircuitClosed(new CordId(nextCord++), session.Directory.JackOf(call.Request.CallerPerson),
                        session.Directory.JackOf(call.Request.Recipient));
                }
            }
        }

        [Test]
        public void TheGuardTripsAfterTheTimeoutAndInputResetsIt()
        {
            var guard = new IdleGuard(60);
            guard.Tick(59.9);
            Assert.That(guard.IsIdle, Is.False);
            guard.Tick(0.2);
            Assert.That(guard.IsIdle, Is.True);
            Assert.That(guard.Blocks(world.Emergency), Is.True);
            Assert.That(guard.Blocks(world.Regular), Is.False, "types without fines carry on");

            guard.ReportInput();
            Assert.That(guard.IsIdle, Is.False);
            Assert.That(guard.Blocks(world.Emergency), Is.False);
        }

        [Test]
        public void ATimeoutOfZeroNeverTrips()
        {
            var guard = new IdleGuard(0);
            guard.Tick(100000);
            Assert.That(guard.IsIdle, Is.False);
        }

        [Test]
        public void TheSessionUsesThePacingTimeout()
        {
            world.Content.Pacing = new PacingSettings(2, 8, 0.3, 0.5, idleTimeoutSeconds: 10);
            var quick = new GameSession(world.Content, new SeededRandom(1), new FakeSwitchboard());
            for (int i = 0; i < 199; i++) quick.Tick(0.05);
            Assert.That(quick.Idle.IsIdle, Is.False);
            quick.Tick(0.1);
            Assert.That(quick.Idle.IsIdle, Is.True);
        }

        [Test]
        public void NoEmergenciesRingAfterAMinuteWithoutInputButRegularCallsDo()
        {
            int emergenciesLate = 0, regularLate = 0;
            session.Calls.CallAdded += e =>
            {
                if (session.Clock.Now < 60) return;
                if (e.Call.Request.Type == world.Emergency) emergenciesLate++;
                else regularLate++;
            };

            Advance(10 * 60, input: false);

            Assert.That(emergenciesLate, Is.EqualTo(0));
            Assert.That(regularLate, Is.GreaterThan(20));
        }

        [Test]
        public void InputBringsEmergenciesBack()
        {
            Advance(5 * 60, input: false);
            int emergencies = 0;
            session.Calls.CallAdded += e => { if (e.Call.Request.Type == world.Emergency) emergencies++; };

            Advance(5 * 60, input: true);

            Assert.That(emergencies, Is.GreaterThan(0));
        }

        [Test]
        public void DueEmergencyCallbacksWaitWhileIdle()
        {
            // No regular calls, so the board has room for the callback the moment the player is back.
            world.Regular.SpawnWeight = 0;
            Advance(61, input: false);
            Assert.That(session.Idle.IsIdle, Is.True);
            // A wrong patch from just before the player left: it is due 20 to 40 s later, while the guard is up.
            session.ClearCalls();
            Call wrong = session.Calls.Add(world.EmergencyRequest(0));
            session.Desk.TryTake(wrong.Id);
            var wrongCord = new CordId(nextCord++);
            session.Desk.OnCircuitClosed(wrongCord, session.Directory.JackOf(wrong.Request.CallerPerson),
                session.Directory.JackOf(world.People[5]), session.Directory.JackOf(wrong.Request.CallerPerson));
            Assert.That(session.Callbacks.CountPending(world.Emergency), Is.EqualTo(1));

            Advance(120, input: false);
            Assert.That(session.Calls.CountLive(world.Emergency), Is.EqualTo(0));
            Assert.That(session.Callbacks.CountPending(world.Emergency), Is.EqualTo(1));

            Advance(0.1, input: true);
            Assert.That(session.Calls.CountLive(world.Emergency), Is.EqualTo(1));
        }

        [Test]
        public void TwoIdleMinutesWithAnEmergencyAlreadyWaitingCostAtMostOneFine()
        {
            // The player leaves at the start. Just before the guard trips, an emergency is ringing.
            Advance(59.5, input: false);
            if (session.Calls.CountLive(world.Emergency) == 0)
            {
                session.ClearCalls();
                Assert.That(session.Spawner.TrySpawn(world.Emergency), Is.True);
            }
            fines.Clear();

            Advance(120, input: false, connectRegular: false);

            Assert.That(fines.Count, Is.EqualTo(1));
            Assert.That(fines[0].Reason, Is.EqualTo(CallEndReason.Missed));
            Assert.That(fines[0].TakenCents, Is.EqualTo(200));
        }
    }
}
