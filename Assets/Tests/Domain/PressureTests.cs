using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Switchboard.Content;
using UnityEditor;

namespace Switchboard.Domain.Tests
{
    /// <summary>
    /// Step 13a (D63, D64): nobody calls while on a call, at most a few calls to a busy recipient (only one who is talking, D83), the busy-line flag,
    /// and callers who hang up when their patience runs out.
    /// </summary>
    public sealed class PressureTests
    {
        private static readonly BoardSize Small = new BoardSize(3, 3);

        private static GameSession Session(IReadOnlyList<CallScript> pool, int slots, double patience = 0, int maxBusy = 1,
            bool tutorial = false) =>
            new GameSession(new FakeContent
            {
                StartingBoard = Small, StartingClientSlots = slots, StartPool = pool, DefaultPatienceSeconds = patience,
                MaxBusyLineCards = maxBusy
            }, new FakeSwitchboard(), tutorial: tutorial);

        private static string[] LiveIds(GameSession session) => session.Calls.Live.Select(c => c.Request.Script.Id).ToArray();

        private static void Connect(GameSession session, Call call)
        {
            session.Calls.Take(call.Id);
            session.Calls.Connect(call.Id);
        }

        // --- The queue rule ---

        [Test]
        public void ACallerWhoIsBeingCalledDoesNotRingUntilTheirCallEnds()
        {
            // Agatha (A1) is on the line with the doctor (B1); her call to the police (C1) waits.
            var toDoctor = Scripts.Call("agatha_doctor", "A1", "B1");
            var toPolice = Scripts.Call("agatha_police", "B1", "C1");
            var other = Scripts.Call("other", "C2", "C3");
            GameSession session = Session(new[] { toDoctor, other, toPolice }, 3);
            session.Tick(.01);
            Assert.That(LiveIds(session), Is.EqualTo(new[] { "agatha_doctor", "other" }), "B1 is being called, so B1 doesn't ring");

            session.Calls.End(session.Calls.Live[0].Id, CallEndReason.Finished);
            session.Tick(.01);
            Assert.That(LiveIds(session), Does.Contain("agatha_police"), "once the call ends, the skipped call comes round again");
        }

        [Test]
        public void ACallerWhoIsCallingDoesNotRingAgain()
        {
            var first = Scripts.Call("first", "A1", "B1");
            var second = Scripts.Call("second", "A1", "C1");
            GameSession session = Session(new[] { first, second }, 3);
            session.Tick(.01);
            Assert.That(LiveIds(session), Is.EqualTo(new[] { "first" }), "A1 is calling already; the slot stays empty");
        }

        [Test]
        public void ACallToABusyRecipientIsDealtButOnlyOneAtATime()
        {
            var talk = Scripts.Call("talk", "A1", "B1");
            var free = Scripts.Call("free", "C3", "A3");
            var toBusy1 = Scripts.Call("to_busy_1", "C1", "B1");
            var toBusy2 = Scripts.Call("to_busy_2", "C2", "A1");
            GameSession session = Session(new[] { talk, free, toBusy1, toBusy2 }, 4);
            session.Tick(.01);
            Connect(session, session.Calls.Live[0]);
            session.Tick(.01);
            Assert.That(LiveIds(session), Is.EqualTo(new[] { "talk", "free", "to_busy_1" }),
                "one busy-recipient card; the second waits");
            Assert.That(session.Queue.BusyLineCards(), Is.EqualTo(1));

            session.Calls.End(session.Calls.Live[2].Id, CallEndReason.Finished);
            session.Tick(.01);
            Assert.That(LiveIds(session), Does.Contain("to_busy_2"), "the first busy-recipient card left, so the next may come");
        }

        [TestCase(CallState.Incoming)]
        [TestCase(CallState.Taken)]
        [TestCase(CallState.Connecting)]
        public void NobodyIsCalledWhileTheirOwnCardStillWaits(CallState state)
        {
            // D83: a busy recipient is someone who is talking, never someone whose own card still waits.
            var waiting = Scripts.Call("waiting", "A1", "B1");
            var toCaller = Scripts.Call("to_caller", "C1", "A1");
            var toRecipient = Scripts.Call("to_recipient", "C2", "B1");
            GameSession session = Session(new[] { waiting, toCaller, toRecipient }, 4, maxBusy: 1);
            session.Tick(.01);
            Call call = session.Calls.Live[0];
            if (state != CallState.Incoming) session.Calls.Take(call.Id);
            if (state == CallState.Connecting) session.Calls.BeginConnecting(call.Id);
            session.Tick(.01);
            Assert.That(call.State, Is.EqualTo(state));
            Assert.That(LiveIds(session), Is.EqualTo(new[] { "waiting" }), "both jacks belong to a call still waiting");

            if (state == CallState.Incoming) session.Calls.Take(call.Id);
            session.Calls.Connect(call.Id);
            session.Tick(.01);
            Assert.That(LiveIds(session).Length, Is.EqualTo(2), "once it is talking, one call to a busy line may ring");
        }

        [Test]
        public void NoBusyRecipientCardsWhenTheFileAllowsNone()
        {
            var talk = Scripts.Call("talk", "A1", "B1");
            var toBusy = Scripts.Call("to_busy", "C1", "B1");
            GameSession session = Session(new[] { talk, toBusy }, 3, maxBusy: 0);
            session.Tick(.01);
            Assert.That(LiveIds(session), Is.EqualTo(new[] { "talk" }));
        }

        // --- Line busy ---

        [Test]
        public void TheLineIsBusyOnlyWhileTheRecipientsJackCarriesALiveCall()
        {
            var calls = new CallService();
            Call talk = calls.Add(new CallRequest(Scripts.Call("talk", "A1", "B1")));
            Call waiting = calls.Add(new CallRequest(Scripts.Call("waiting", "C1", "B1")));
            calls.Tick(.01);
            Assert.That(waiting.LineBusy, Is.False, "B1 is only asked for, not on the line");

            calls.Take(talk.Id);
            calls.Connect(talk.Id);
            calls.Tick(.01);
            Assert.That(waiting.LineBusy, Is.True, "B1 is on the line");

            calls.Hold(talk.Id);
            calls.Tick(.01);
            Assert.That(waiting.LineBusy, Is.True, "a call on hold still holds the line");

            calls.End(talk.Id, CallEndReason.DisconnectedEarly);
            calls.Tick(.01);
            Assert.That(waiting.LineBusy, Is.False, "the jack is free again");
        }

        [Test]
        public void ANewCallToALineAlreadyInUseShowsBusyAtOnce()
        {
            var calls = new CallService();
            Call talk = calls.Add(new CallRequest(Scripts.Call("talk", "A1", "B1")));
            calls.Take(talk.Id);
            calls.Connect(talk.Id);
            Call waiting = calls.Add(new CallRequest(Scripts.Call("waiting", "C1", "A1")));
            Assert.That(waiting.LineBusy, Is.True);
        }

        [Test]
        public void AHangUpOverABusyLineIsFlagged()
        {
            var calls = new CallService(10);
            Call talk = calls.Add(new CallRequest(Scripts.Call("talk", "A1", "B1", durationSeconds: 100)));
            Call free = calls.Add(new CallRequest(Scripts.Call("free", "C2", "C3")));
            calls.Take(talk.Id);
            calls.Connect(talk.Id);
            Call busy = calls.Add(new CallRequest(Scripts.Call("busy", "C1", "B1")));
            calls.Tick(5);
            calls.Tick(5);
            calls.Tick(.01);
            Assert.That(busy.EndReason, Is.EqualTo(CallEndReason.HungUp));
            Assert.That(busy.HungUpOnBusyLine, Is.True);
            Assert.That(free.EndReason, Is.EqualTo(CallEndReason.HungUp));
            Assert.That(free.HungUpOnBusyLine, Is.False, "a free line: the caller just waited too long");
        }

        // --- Patience ---

        [Test]
        public void PatienceCountsOnlyWhileTheCallWaitsAndStopsOnceConnected()
        {
            var calls = new CallService(45);
            Call call = calls.Add(new CallRequest(Scripts.Call("c", "A1", "B1", durationSeconds: 100)));
            Assert.That(call.HasPatience, Is.True);
            Assert.That(call.PatienceSecondsLeft, Is.EqualTo(45));
            calls.Tick(10);
            Assert.That(call.PatienceSecondsLeft, Is.EqualTo(35).Within(1e-9), "counts while incoming");
            calls.Take(call.Id);
            calls.Tick(5);
            calls.BeginConnecting(call.Id);
            calls.Tick(5);
            Assert.That(call.PatienceSecondsLeft, Is.EqualTo(25).Within(1e-9), "and while taken or connecting");
            calls.Connect(call.Id);
            calls.Tick(60);
            Assert.That(call.PatienceSecondsLeft, Is.EqualTo(25).Within(1e-9), "stopped for good once connected");
            Assert.That(call.State, Is.EqualTo(CallState.Connected));
            calls.Hold(call.Id);
            calls.Tick(60);
            Assert.That(call.State, Is.EqualTo(CallState.OnHold), "a call on hold never hangs up");
        }

        [Test]
        public void ACallerHangsUpWithNoPayAndNoFine()
        {
            var calls = new CallService(45);
            var wallet = new Wallet(500);
            var earnings = new EarningsService(calls, wallet);
            var penalties = new PenaltyService(calls, wallet, 1.5);
            var fines = 0;
            penalties.FineApplied += _ => fines++;
            CallEndReason ended = CallEndReason.None;
            calls.CallEnded += e => ended = e.Reason;
            Call call = calls.Add(new CallRequest(Scripts.Call("c", "A1", "B1", 5, 20, 10)));
            calls.Tick(44.9);
            Assert.That(call.State, Is.EqualTo(CallState.Incoming));
            calls.Tick(.2);
            Assert.That(call.EndReason, Is.EqualTo(CallEndReason.HungUp));
            Assert.That(ended, Is.EqualTo(CallEndReason.HungUp));
            Assert.That(wallet.Cents, Is.EqualTo(500));
            Assert.That(earnings.TotalPaidCents, Is.Zero);
            Assert.That(fines, Is.Zero);
            Assert.That(calls.Live, Is.Empty);
        }

        [Test]
        public void ACallsOwnPatienceWinsAndZeroWaitsForever()
        {
            var calls = new CallService(45);
            Call quick = calls.Add(new CallRequest(Scripts.Call("quick", "A1", "B1", patienceSeconds: 10)));
            Call forever = calls.Add(new CallRequest(Scripts.Call("forever", "A2", "B2", patienceSeconds: 0)));
            Assert.That(quick.PatienceSeconds, Is.EqualTo(10));
            Assert.That(forever.HasPatience, Is.False);
            calls.Tick(500);
            Assert.That(quick.EndReason, Is.EqualTo(CallEndReason.HungUp));
            Assert.That(forever.State, Is.EqualTo(CallState.Incoming));
        }

        [Test]
        public void NoDefaultPatienceMeansCallersWaitForever()
        {
            var calls = new CallService();
            Call call = calls.Add(new CallRequest(Scripts.Call("c", "A1", "B1")));
            calls.Tick(1000);
            Assert.That(call.State, Is.EqualTo(CallState.Incoming));
        }

        [Test]
        public void AnEmergencyUsesItsResponseTimeAndIgnoresPatience()
        {
            var calls = new CallService(5);
            Call emergency = calls.Add(new CallRequest(Scripts.Emergency("sos", "A1", "F4", responseTimeSeconds: 30)));
            Assert.That(emergency.HasPatience, Is.False);
            calls.Tick(10);
            Assert.That(emergency.State, Is.EqualTo(CallState.Incoming), "no hang-up after the default patience");
            calls.Tick(21);
            Assert.That(emergency.EndReason, Is.EqualTo(CallEndReason.Missed));
        }

        [Test]
        public void TheTutorialsCallKeepsItsPatienceUntilTheGuideEnds()
        {
            GameSession session = Session(new[] { Scripts.Call("guided", "A1", "B1"), Scripts.Call("next", "C1", "C2") }, 3,
                patience: 10, tutorial: true);
            session.Tick(.01);
            Call guided = session.Calls.Live[0];
            Assert.That(guided.WaitingHeld, Is.True);
            session.Tick(60);
            Assert.That(guided.State, Is.EqualTo(CallState.Incoming), "no hang-up while the guide runs");
            Assert.That(guided.PatienceSecondsLeft, Is.EqualTo(10));

            session.EndTutorial();
            session.Tick(10.1);
            Assert.That(guided.EndReason, Is.EqualTo(CallEndReason.HungUp), "a skipped tutorial leaves an ordinary caller");
        }

        [Test]
        public void AHungUpCallComesRoundAgainLater()
        {
            GameSession session = Session(new[] { Scripts.Call("only", "A1", "B1") }, 1, patience: 10);
            session.Tick(.01);
            Call first = session.Calls.Live[0];
            session.Tick(10.1);
            Assert.That(first.EndReason, Is.EqualTo(CallEndReason.HungUp));
            Assert.That(LiveIds(session), Is.EqualTo(new[] { "only" }), "the queue loops back to it as a new call");
            Assert.That(session.Calls.Live[0], Is.Not.SameAs(first));
        }

        [Test]
        public void CareerStatsCountHangUpsAndSaveThem()
        {
            GameSession session = Session(new[] { Scripts.Call("a", "A1", "B1"), Scripts.Call("b", "A2", "B2") }, 2, patience: 10);
            session.Tick(.01);
            session.Tick(10.1);
            Assert.That(session.Career.CallsHungUp, Is.EqualTo(2));
            CareerData saved = session.CaptureSave().career;
            Assert.That(saved.callsHungUp, Is.EqualTo(2));

            var loaded = new GameSession(new FakeContent { StartingBoard = Small, StartPool = new[] { Scripts.Call("a", "A1", "B1") } },
                new FakeSwitchboard(), new SaveData { version = SaveData.CurrentVersion, career = saved });
            Assert.That(loaded.Career.CallsHungUp, Is.EqualTo(2));
        }

        [Test]
        public void AFineInTheSessionFollowsTheBoughtIncomeMultiplier()
        {
            var shop = new IUpgrade[] { FakeUpgrade.Income("income_1", 100, 2) };
            var session = new GameSession(new FakeContent
            {
                StartingBoard = Small, StartingClientSlots = 1, StartingCents = 1100, Upgrades = shop,
                StartPool = new[] { Scripts.Emergency("sos", "A1", "B1", responseTimeSeconds: 10) }
            }, new FakeSwitchboard());
            Assert.That(session.Upgrades.TryBuy("income_1"), Is.True);
            session.Tick(.01);
            session.Tick(10);
            Assert.That(session.Career.FinesPaidCents, Is.EqualTo(405), "135¢ x 2 x 1.5");
            Assert.That(session.Wallet.Cents, Is.EqualTo(1000 - 405));
        }

        // --- The files ---

        [Test]
        public void TheRealEconomyFileSetsThePressureValues()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/Content/GameConfig.asset");
            EconomyCatalog economy = config.LoadEconomy();
            Assert.That(economy.Errors, Is.Empty, string.Join("\n", economy.Errors));
            Assert.That(economy.DefaultPatienceSeconds, Is.GreaterThan(0));
            Assert.That(economy.EmergencyFineFactor, Is.GreaterThan(0));
            Assert.That(economy.MaxBusyLineCards, Is.GreaterThanOrEqualTo(0));
            var content = new GameContent(economy);
            Assert.That(content.DefaultPatienceSeconds, Is.EqualTo(economy.DefaultPatienceSeconds));
            Assert.That(content.EmergencyFineFactor, Is.EqualTo(economy.EmergencyFineFactor));
            Assert.That(content.MaxBusyLineCards, Is.EqualTo(economy.MaxBusyLineCards));
        }

        private const string MinimalEconomy = "{ \"startingCords\": 1, \"startingBoard\": {\"cols\": 4, \"rows\": 4}, " +
            "\"startingClientSlots\": 3, \"quickConnectSeconds\": 5, \"quickConnectTipCents\": 5, %PRESSURE% " +
            "\"upgrades\": [ {\"id\": \"secret\", \"type\": \"secret\", \"name\": \"?\", \"costCents\": 100} ] }";

        private static EconomyCatalog LoadEconomy(string pressure) => EconomyFileLoader.Load(MinimalEconomy.Replace("%PRESSURE%", pressure));

        [Test]
        public void ThePressureValuesAreRead()
        {
            EconomyCatalog economy = LoadEconomy("\"defaultPatienceSeconds\": 40, \"emergencyFineFactor\": 2, \"maxBusyLineCards\": 2,");
            Assert.That(economy.Errors, Is.Empty, string.Join("\n", economy.Errors));
            Assert.That(economy.DefaultPatienceSeconds, Is.EqualTo(40));
            Assert.That(economy.EmergencyFineFactor, Is.EqualTo(2));
            Assert.That(economy.MaxBusyLineCards, Is.EqualTo(2));
        }

        [Test]
        public void MissingPressureValuesTurnTheirRulesOffWithAMessage()
        {
            EconomyCatalog economy = LoadEconomy(string.Empty);
            Assert.That(economy.Errors.Count(e => e.StartsWith("defaultPatienceSeconds is missing")), Is.EqualTo(1));
            Assert.That(economy.Errors.Count(e => e.StartsWith("emergencyFineFactor is missing")), Is.EqualTo(1));
            Assert.That(economy.Errors.Count(e => e.StartsWith("maxBusyLineCards is missing")), Is.EqualTo(1));
            Assert.That(economy.DefaultPatienceSeconds, Is.Zero);
            Assert.That(economy.EmergencyFineFactor, Is.Zero);
            Assert.That(economy.MaxBusyLineCards, Is.Zero);
        }

        [Test]
        public void WrongPressureValuesAreReported()
        {
            EconomyCatalog economy = LoadEconomy("\"defaultPatienceSeconds\": 0, \"emergencyFineFactor\": -1, \"maxBusyLineCards\": -2,");
            Assert.That(economy.Errors.Any(e => e.StartsWith("defaultPatienceSeconds 0 must be above 0")), Is.True);
            Assert.That(economy.Errors.Any(e => e.StartsWith("emergencyFineFactor -1 is negative")), Is.True);
            Assert.That(economy.Errors.Any(e => e.StartsWith("maxBusyLineCards -2 is negative")), Is.True);
        }
    
        // --- Windows hold waiting calls only: a talking call's card is a tab (D83, 13a pass 2b) ---

        private static GameSession FullBoardSession(int calls, int slots) =>
            new GameSession(new FakeContent
            {
                StartingBoard = BoardSize.Full, StartingClientSlots = slots,
                StartPool = Scripts.Pool(calls, BoardSize.Full), MaxBusyLineCards = 0
            }, new FakeSwitchboard());

        [Test]
        public void AConnectFreesItsWindowForTheNextCaller()
        {
            GameSession session = FullBoardSession(6, 3);
            session.Tick(.01);
            Assert.That(session.Calls.Live.Count, Is.EqualTo(3), "three windows, three callers");

            Connect(session, session.Calls.Live[0]);
            session.Tick(.01);
            Assert.That(session.Calls.Live.Count, Is.EqualTo(4), "the talking call left its window, and the next caller took it");
            Assert.That(session.Queue.WaitingCalls(), Is.EqualTo(3));
            Assert.That(session.Calls.Live[0].IsTalking && !session.Calls.Live[0].IsWaiting, Is.True);
        }

        [Test]
        public void FourTalkingCallsAndThreeWaitingCards()
        {
            GameSession session = FullBoardSession(12, 3);
            session.Tick(.01);
            for (int i = 0; i < 4; i++)
            {
                Connect(session, session.Calls.Live.First(c => c.IsWaiting));
                session.Tick(.01);
            }
            Assert.That(session.Calls.Live.Count(c => c.IsTalking), Is.EqualTo(4));
            Assert.That(session.Calls.Live.Count(c => c.IsWaiting), Is.EqualTo(3), "the windows stay full beside four talking calls");
        }

        [Test]
        public void AHeldCallHoldsNoWindowAndTakenOrConnectingCallsDo()
        {
            GameSession session = FullBoardSession(8, 3);
            session.Tick(.01);
            Call talking = session.Calls.Live[0];
            Connect(session, talking);
            session.Calls.Hold(talking.Id);
            Assert.That(talking.State, Is.EqualTo(CallState.OnHold), "setup");
            Call taken = session.Calls.Live[1];
            Call connecting = session.Calls.Live[2];
            session.Calls.Take(taken.Id);
            session.Calls.Take(connecting.Id);
            session.Calls.BeginConnecting(connecting.Id);
            session.Tick(.01);
            Assert.That(session.Queue.WaitingCalls(), Is.EqualTo(3), "a call in hand or half-patched still holds its window");
            Assert.That(session.Calls.Live.Count, Is.EqualTo(4), "only the held call's window was refilled");
        }
    }
}
