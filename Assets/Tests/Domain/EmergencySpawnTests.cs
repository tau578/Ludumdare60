using NUnit.Framework;

namespace Switchboard.Domain.Tests
{
    /// <summary>Which type rings (Design 5.2): weights, the one-emergency limit and who may call whom (D37).</summary>
    public sealed class EmergencySpawnTests
    {
        private static int nextCord;

        private static void Connect(GameSession session, Call call, IPerson to)
        {
            session.Desk.TryTake(call.Id);
            session.Desk.OnCircuitClosed(new CordId(nextCord++), session.Directory.JackOf(call.Request.CallerPerson),
                session.Directory.JackOf(to));
        }

        [Test]
        public void AboutOneCallInSixIsAnEmergency()
        {
            var world = new EmergencyWorld();
            var session = new GameSession(world.Content, new SeededRandom(6), new FakeSwitchboard());
            int emergencies = 0;
            const int spawns = 6000;

            for (int i = 0; i < spawns; i++)
            {
                Assert.That(session.Spawner.TrySpawn(), Is.True);
                if (session.Calls.Live[0].Request.Type == world.Emergency) emergencies++;
                session.ClearCalls();
            }

            Assert.That(emergencies / (double)spawns, Is.InRange(0.14, 0.19));
        }

        [Test]
        public void AtMostOneEmergencyIsOnTheBoardOrWaitingToCallBack()
        {
            // Emergencies weigh as much as regular calls here, so the limit is what holds them back.
            var world = new EmergencyWorld(count: 12, regularWeight: 1, emergencyWeight: 1);
            var session = new GameSession(world.Content, new SeededRandom(12), new FakeSwitchboard { CordCount = 6 });
            int emergencies = 0, wrong = 0, maxSeen = 0;
            session.Calls.CallAdded += e =>
            {
                ICallerType type = e.Call.Request.Type;
                IPerson caller = e.Call.Request.CallerPerson, recipient = e.Call.Request.Recipient;
                Assert.That(caller.Tags, Is.Empty, "services never call");
                if (type == world.Emergency)
                {
                    emergencies++;
                    Assert.That(recipient.HasTag(FakeCallerType.ServiceTag), Is.True, "emergencies go to services");
                }
                else Assert.That(recipient.Tags, Is.Empty, "regular calls never go to a service");
            };

            for (int tick = 0; tick < 30 * 60 * 20; tick++)
            {
                session.Idle.ReportInput();
                session.Tick(0.05);
                int taken = session.Calls.CountLive(world.Emergency) + session.Callbacks.CountPending(world.Emergency);
                Assert.That(taken, Is.LessThanOrEqualTo(1));
                if (taken > maxSeen) maxSeen = taken;

                for (int i = session.Calls.Live.Count - 1; i >= 0; i--)
                {
                    Call call = session.Calls.Live[i];
                    if (call.State != CallState.Incoming) continue;
                    if (call.Request.Type != world.Emergency) Connect(session, call, call.Request.Recipient);
                    // Emergencies are patched right, patched wrong (a callback) or left to ring out, in turn.
                    else if (call.Id.Value % 3 == 0) Connect(session, call, call.Request.Recipient);
                    else if (call.Id.Value % 3 == 1 && call.ResponseSecondsLeft < 25)
                    {
                        Connect(session, call, world.People[0] == call.Request.CallerPerson ? world.People[1] : world.People[0]);
                        wrong++;
                    }
                }
            }

            Assert.That(emergencies, Is.GreaterThan(20));
            Assert.That(wrong, Is.GreaterThan(5));
            Assert.That(maxSeen, Is.EqualTo(1));
        }

        [Test]
        public void ADueEmergencyCallbackWaitsWhileAnotherEmergencyIsLive()
        {
            var world = new EmergencyWorld();
            var clock = new GameClock();
            var calls = new CallService();
            var callbacks = new CallbackScheduler(calls, clock, new SeededRandom(2), () => 6);
            calls.End(calls.Add(world.EmergencyRequest(0)).Id, CallEndReason.WrongConnection);
            Call other = calls.Add(world.EmergencyRequest(1, service: 1));

            clock.Advance(60);
            callbacks.Tick();
            Assert.That(calls.Live.Count, Is.EqualTo(1));
            Assert.That(callbacks.Pending.Count, Is.EqualTo(1));

            calls.End(other.Id, CallEndReason.Finished);
            callbacks.Tick();
            Assert.That(calls.Live.Count, Is.EqualTo(1));
            Assert.That(calls.Live[0].Request.Attempt, Is.EqualTo(2));
        }

        [Test]
        public void ATypeWithNoWeightOnlyRingsWhenAskedFor()
        {
            var world = new EmergencyWorld(emergencyWeight: 0);
            var session = new GameSession(world.Content, new SeededRandom(3), new FakeSwitchboard());
            for (int i = 0; i < 300; i++)
            {
                session.Spawner.TrySpawn();
                Assert.That(session.Calls.Live[0].Request.Type, Is.SameAs(world.Regular));
                session.ClearCalls();
            }

            Assert.That(session.Spawner.TrySpawn(world.Emergency), Is.True);
            Assert.That(session.Calls.Live[0].Request.Type, Is.SameAs(world.Emergency));
            Assert.That(session.Spawner.TrySpawn(world.Emergency), Is.False, "at most one at a time");
        }

        [Test]
        public void TheSpawnerKnowsEveryTypeItsCallersCanPlace()
        {
            var world = new EmergencyWorld();
            var session = new GameSession(world.Content, new SeededRandom(3), new FakeSwitchboard());

            Assert.That(session.Spawner.Types, Is.EqualTo(new ICallerType[] { world.Regular, world.Emergency }));
        }
    }
}
