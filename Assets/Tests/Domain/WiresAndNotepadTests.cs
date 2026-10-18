using System.Collections.Generic;
using NUnit.Framework;

namespace Switchboard.Domain.Tests
{
    public sealed class CordStatTests
    {
        private UpgradeService upgrades;
        private StatSheet stats;
        private int changes;

        [SetUp]
        public void SetUp()
        {
            upgrades = new UpgradeService(FakeUpgrade.FullShop(), new Wallet(100000));
            stats = new StatSheet(upgrades, 3);
            changes = 0;
            stats.Changed += () => changes++;
        }

        [Test]
        public void CordsAreTheStartingCordsWithNothingBought()
        {
            Assert.That(stats.Cords, Is.EqualTo(3));
            Assert.That(new StatSheet(new UpgradeService(null, new Wallet(0)), 5).Cords, Is.EqualTo(5));
        }

        [Test]
        public void CordsAreTheLastBoughtValueSetNotStacked()
        {
            upgrades.TryBuy("wires_1");
            Assert.That(stats.Cords, Is.EqualTo(4));
            upgrades.TryBuy("wires_2");
            Assert.That(stats.Cords, Is.EqualTo(5), "4 then 5 gives 5, not 3 + 1 + 2");
            stats.Recompute();
            Assert.That(stats.Cords, Is.EqualTo(5));
        }

        [Test]
        public void HasNotepadOnlyAfterBuyingIt()
        {
            Assert.That(stats.HasNotepad, Is.False);
            upgrades.TryBuy("income_1");
            Assert.That(stats.HasNotepad, Is.False);
            upgrades.TryBuy("notepad");
            Assert.That(stats.HasNotepad, Is.True);
        }

        [Test]
        public void ChangedIsRaisedOnPurchaseAndOnReset()
        {
            upgrades.TryBuy("wires_1");
            Assert.That(changes, Is.EqualTo(1));
            upgrades.ClearOwned();
            stats.Recompute();
            Assert.That(changes, Is.EqualTo(2));
            Assert.That(stats.Cords, Is.EqualTo(3));
        }

        [Test]
        public void SessionResetTurnsTheNotepadOffAndRaisesChanged()
        {
            var world = new TestWorld();
            world.Content.StartingCents = 100000;
            world.Content.Upgrades = FakeUpgrade.FullShop();
            var session = new GameSession(world.Content, new SeededRandom(5), new FakeSwitchboard());
            int sessionChanges = 0;
            session.Stats.Changed += () => sessionChanges++;
            session.Upgrades.TryBuy("notepad");
            session.Upgrades.TryBuy("wires_1");
            Assert.That(session.Stats.HasNotepad, Is.True);
            Assert.That(session.Stats.Cords, Is.EqualTo(4));

            session.ResetUpgrades();
            Assert.That(session.Stats.HasNotepad, Is.False);
            Assert.That(session.Stats.Cords, Is.EqualTo(3));
            Assert.That(sessionChanges, Is.EqualTo(3));
        }
    }

    public sealed class GrowingCordsSpawnTests
    {
        [Test]
        public void TheIntervalStretchesLessOnceTheCordCountGrows()
        {
            var crowd = new Crowd(12);
            crowd.Content.Pacing = new PacingSettings(1000, 8, 0, 0.5);
            var board = new FakeSwitchboard { CordCount = 3 };
            var session = new GameSession(crowd.Content, new SeededRandom(9), board);
            for (int i = 0; i < 5; i++) session.Calls.Add(crowd.Request(i * 2, i * 2 + 1));

            // Five waiting calls and three free cords: 1 + 0.5 x 2.
            Assert.That(session.Spawner.Load(), Is.EqualTo(2));
            board.CordCount = 4;
            Assert.That(session.Spawner.Load(), Is.EqualTo(1.5));
            board.CordCount = 6;
            Assert.That(session.Spawner.Load(), Is.EqualTo(1));
        }
    }

    public sealed class NotepadTests
    {
        private TestWorld world;
        private GameSession session;
        private List<CallId> noted;

        [SetUp]
        public void SetUp()
        {
            world = new TestWorld();
            world.Content.StartingCents = 100000;
            world.Content.Upgrades = FakeUpgrade.FullShop();
            world.Content.Pacing = new PacingSettings(1000, 1000, 0, 0);
            session = new GameSession(world.Content, new SeededRandom(1), new FakeSwitchboard());
            noted = new List<CallId>();
            session.Notepad.Noted += id => noted.Add(id);
        }

        private Call Take(CallRequest request)
        {
            Call call = session.Calls.Add(request);
            session.Desk.TryTake(call.Id);
            return call;
        }

        private Call TakeAdaWantsBasil() => Take(world.AdaWantsBasil());

        private CallRequest ClaraWantsAda() => new CallRequest(world.Callers[2], world.Ada, 5, 20);

        [Test]
        public void DoesNothingWithoutTheUpgrade()
        {
            Call first = TakeAdaWantsBasil();
            Call second = Take(ClaraWantsAda());
            Assert.That(session.Notepad.Note(TestWorld.A1), Is.False);
            Assert.That(first.CallerNote, Is.Null);
            Assert.That(second.RecipientNote, Is.Null);
            Assert.That(noted, Is.Empty);
        }

        [Test]
        public void NotesTheCallerAndTheRecipientOnceEach()
        {
            session.Upgrades.TryBuy("notepad");
            Call call = TakeAdaWantsBasil();

            Assert.That(session.Notepad.Note(TestWorld.A1), Is.True);
            Assert.That(call.CallerNote, Is.EqualTo(TestWorld.A1));
            Assert.That(call.RecipientNote, Is.Null);
            Assert.That(session.Notepad.Note(TestWorld.B2), Is.True);
            Assert.That(call.RecipientNote, Is.EqualTo(TestWorld.B2));
            Assert.That(noted, Is.EqualTo(new[] { call.Id, call.Id }));

            Assert.That(session.Notepad.Note(TestWorld.A1), Is.False, "the same person again changes nothing");
            Assert.That(session.Notepad.Note(TestWorld.B2), Is.False);
            Assert.That(noted.Count, Is.EqualTo(2));
        }

        [Test]
        public void IgnoresAJackNotOnAnyAcceptedCall()
        {
            session.Upgrades.TryBuy("notepad");
            Call call = TakeAdaWantsBasil();
            Assert.That(session.Notepad.Note(TestWorld.C3), Is.False);
            Assert.That(session.Notepad.Note(new JackId("F6")), Is.False);
            Assert.That(call.CallerNote, Is.Null);
            Assert.That(call.RecipientNote, Is.Null);
            Assert.That(noted, Is.Empty);
        }

        [Test]
        public void TwoAcceptedCallsSharingAPersonBothGetTheNote()
        {
            session.Upgrades.TryBuy("notepad");
            Call first = TakeAdaWantsBasil();
            Call second = Take(ClaraWantsAda());

            Assert.That(session.Notepad.Note(TestWorld.A1), Is.True);
            Assert.That(first.CallerNote, Is.EqualTo(TestWorld.A1));
            Assert.That(second.RecipientNote, Is.EqualTo(TestWorld.A1));
            Assert.That(first.RecipientNote, Is.Null);
            Assert.That(second.CallerNote, Is.Null);
            Assert.That(noted, Is.EqualTo(new[] { first.Id, second.Id }), "Noted is raised once per call");
        }

        [Test]
        public void TappingTheSamePersonTwiceAddsNothingNew()
        {
            session.Upgrades.TryBuy("notepad");
            TakeAdaWantsBasil();
            Take(ClaraWantsAda());
            session.Notepad.Note(TestWorld.A1);

            Assert.That(session.Notepad.Note(TestWorld.A1), Is.False);
            Assert.That(noted.Count, Is.EqualTo(2));
        }

        [Test]
        public void ARingingCallGetsNoNote()
        {
            session.Upgrades.TryBuy("notepad");
            Call ringing = session.Calls.Add(world.AdaWantsBasil());
            Assert.That(session.Notepad.Note(TestWorld.A1), Is.False, "nothing accepted yet");

            Call accepted = Take(ClaraWantsAda());
            Assert.That(session.Notepad.Note(TestWorld.A1), Is.True);
            Assert.That(accepted.RecipientNote, Is.EqualTo(TestWorld.A1));
            Assert.That(ringing.State, Is.EqualTo(CallState.Incoming));
            Assert.That(ringing.CallerNote, Is.Null);
            Assert.That(noted, Is.EqualTo(new[] { accepted.Id }));
        }

        [Test]
        public void AConnectingCallGetsNotes()
        {
            session.Upgrades.TryBuy("notepad");
            Call call = TakeAdaWantsBasil();
            session.Desk.OnPlugInserted(new CordId(0), TestWorld.A1);
            Assert.That(call.State, Is.EqualTo(CallState.Connecting));

            Assert.That(session.Notepad.Note(TestWorld.B2), Is.True);
            Assert.That(call.RecipientNote, Is.EqualTo(TestWorld.B2));
        }

        [Test]
        public void ConnectedAndOnHoldCallsGetNoNote()
        {
            session.Upgrades.TryBuy("notepad");
            Call connected = TakeAdaWantsBasil();
            session.Desk.OnCircuitClosed(new CordId(0), TestWorld.A1, TestWorld.B2);
            Call onHold = Take(ClaraWantsAda());
            session.Desk.OnCircuitClosed(new CordId(1), TestWorld.C3, TestWorld.A1);
            session.Desk.OnCircuitOpened(new CordId(1), TestWorld.C3, TestWorld.A1);
            Assert.That(connected.State, Is.EqualTo(CallState.Connected));
            Assert.That(onHold.State, Is.EqualTo(CallState.OnHold));

            Assert.That(session.Notepad.Note(TestWorld.A1), Is.False);
            Assert.That(session.Notepad.Note(TestWorld.B2), Is.False);
            Assert.That(session.Notepad.Note(TestWorld.C3), Is.False);
            Assert.That(connected.CallerNote, Is.Null);
            Assert.That(connected.RecipientNote, Is.Null);
            Assert.That(onHold.CallerNote, Is.Null);
            Assert.That(onHold.RecipientNote, Is.Null);
            Assert.That(noted, Is.Empty);
        }

        [Test]
        public void NotesStayWhenAnotherCardIsAccepted()
        {
            session.Upgrades.TryBuy("notepad");
            Call first = TakeAdaWantsBasil();
            session.Notepad.Note(TestWorld.A1);
            session.Notepad.Note(TestWorld.B2);

            Call second = Take(new CallRequest(world.Callers[2], world.Basil, 5, 20));
            Assert.That(first.State, Is.EqualTo(CallState.Taken), "accepting another card leaves the first accepted");
            Assert.That(first.CallerNote, Is.EqualTo(TestWorld.A1));
            Assert.That(first.RecipientNote, Is.EqualTo(TestWorld.B2));
            Assert.That(second.CallerNote, Is.Null, "a note doesn't leak to a call accepted later");
            Assert.That(second.RecipientNote, Is.Null);

            Assert.That(session.Notepad.Note(TestWorld.B2), Is.True, "Basil is wanted on the second call too");
            Assert.That(second.RecipientNote, Is.EqualTo(TestWorld.B2));
            Assert.That(noted, Is.EqualTo(new[] { first.Id, first.Id, second.Id }));
        }

        [Test]
        public void ACallbackStartsWithNoNotes()
        {
            session.Upgrades.TryBuy("notepad");
            Call call = TakeAdaWantsBasil();
            session.Notepad.Note(TestWorld.A1);
            Call again = session.Calls.Add(call.Request.Again());
            Assert.That(again.CallerNote, Is.Null);
        }

        [Test]
        public void WorksForAnEmergencysServiceRecipient()
        {
            var emergency = new EmergencyWorld();
            emergency.Content.Upgrades = FakeUpgrade.FullShop();
            emergency.Content.Pacing = new PacingSettings(1000, 1000, 0, 0);
            var emergencySession = new GameSession(emergency.Content, new SeededRandom(2), new FakeSwitchboard());
            emergencySession.Upgrades.TryBuy("notepad");
            Call call = emergencySession.Calls.Add(emergency.EmergencyRequest(0, 1));
            emergencySession.Desk.TryTake(call.Id);

            Assert.That(emergencySession.Notepad.Note(new JackId("F5")), Is.True);
            Assert.That(call.RecipientNote, Is.EqualTo(new JackId("F5")));
            Assert.That(emergencySession.Notepad.Note(new JackId("A1")), Is.True);
            Assert.That(call.CallerNote, Is.EqualTo(new JackId("A1")));
        }

        [Test]
        public void ResetTurnsTheNotepadOff()
        {
            session.Upgrades.TryBuy("notepad");
            TakeAdaWantsBasil();
            session.ResetUpgrades();
            Assert.That(session.Notepad.Note(TestWorld.A1), Is.False);
        }
    }
}
