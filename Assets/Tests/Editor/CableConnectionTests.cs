using NUnit.Framework;
using Switchboard.Cables;

namespace Switchboard.Tests
{
    public sealed class CableConnectionTests
    {
        [TestCase("A1", "B2")]
        [TestCase("B2", "A1")]
        public void RemovedPlugRemembersItsSocketUntilRestored(string removed, string remaining)
        {
            var wire = new CableConnection<string>();
            wire.TryInsert("A1"); wire.TryInsert("B2");
            wire.TryRemove(removed);
            Assert.That(wire.ReconnectSocket, Is.EqualTo(removed));
            Assert.That(wire.TryInsert(remaining), Is.False);
            Assert.That(wire.TryRemove("F6"), Is.False);
            Assert.That(wire.ReconnectSocket, Is.EqualTo(removed));
            Assert.That(wire.TryInsert(wire.ReconnectSocket), Is.True);
            Assert.That(wire.FirstSocket, Is.EqualTo("A1"));
            Assert.That(wire.SecondSocket, Is.EqualTo("B2"));
            Assert.That(wire.Phase, Is.EqualTo(CablePhase.Connected));
            Assert.That(wire.ReconnectSocket, Is.Null);
        }

        [Test]
        public void RepluggingUpdatesTheSocketUsedForTheNextCancellation()
        {
            var wire = new CableConnection<string>();
            wire.TryInsert("A1"); wire.TryInsert("B2"); wire.TryRemove("A1");
            wire.TryInsert("C3");
            Assert.That(wire.ReconnectSocket, Is.Null);
            wire.TryRemove("C3");
            Assert.That(wire.ReconnectSocket, Is.EqualTo("C3"));
            wire.TryInsert(wire.ReconnectSocket);
            Assert.That(wire.FirstSocket, Is.EqualTo("C3"));
            Assert.That(wire.SecondSocket, Is.EqualTo("B2"));
        }

        [Test]
        public void NewConnectionAndFullyReturnedCableHaveNoRestoreTarget()
        {
            var wire = new CableConnection<string>();
            wire.TryPickUp(); wire.TryInsert("A1");
            Assert.That(wire.ReconnectSocket, Is.Null);
            wire.TryInsert("B2"); wire.TryRemove("A1"); wire.TryReturnToTray();
            Assert.That(wire.ReconnectSocket, Is.Null);
            wire.TryPickUp(); wire.TryInsert("C3");
            Assert.That(wire.ReconnectSocket, Is.Null);
            wire.TryInsert("D4"); wire.TryRemove("D4"); wire.TryRemove("C3");
            Assert.That(wire.ReconnectSocket, Is.Null);
        }

        [Test]
        public void PickingUpHidesStockBeforeAnyPlugIsInserted()
        {
            var wire = new CableConnection<string>();
            Assert.That(wire.TryPickUp(), Is.True);
            Assert.That(wire.Phase, Is.EqualTo(CablePhase.Held));
            Assert.That(wire.IsInTray, Is.False);
            Assert.That(wire.FirstSocket, Is.Null);
            Assert.That(wire.SecondSocket, Is.Null);
            Assert.That(wire.TryPickUp(), Is.False);
            Assert.That(wire.TryReturnToTray(), Is.True);
            Assert.That(wire.IsInTray, Is.True);
        }

        [TestCase("A1", "B2")]
        [TestCase("B2", "A1")]
        public void ReturningALooseCableClearsItsRemainingEnd(string removed, string remaining)
        {
            var wire = new CableConnection<string>();
            wire.TryPickUp(); wire.TryInsert("A1"); wire.TryInsert("B2");
            Assert.That(wire.TryReturnToTray(), Is.False);
            Assert.That(wire.TryPickUp(), Is.False);
            Assert.That(wire.Phase, Is.EqualTo(CablePhase.Connected));
            wire.TryRemove(removed);
            Assert.That(wire.Contains(remaining), Is.True);
            Assert.That(wire.TryReturnToTray(), Is.True);
            Assert.That(wire.IsInTray, Is.True);
            Assert.That(wire.FirstSocket, Is.Null);
            Assert.That(wire.SecondSocket, Is.Null);
            Assert.That(wire.TryPickUp(), Is.True);
            Assert.That(wire.TryInsert("C3"), Is.True);
        }

        [Test]
        public void CancelledPickupDoesNotChangeOtherCables()
        {
            var first = new CableConnection<string>();
            var second = new CableConnection<string>();
            var third = new CableConnection<string>();
            first.TryPickUp(); first.TryInsert("A1"); first.TryInsert("B2");
            second.TryPickUp(); third.TryPickUp(); third.TryInsert("C3");
            second.TryReturnToTray();
            Assert.That(first.Phase, Is.EqualTo(CablePhase.Connected));
            Assert.That(second.IsInTray, Is.True);
            Assert.That(third.Phase, Is.EqualTo(CablePhase.OneEndConnected));
        }

        [Test]
        public void TrayHidesAsSoonAsTheFirstEndIsConnected()
        {
            var wire = new CableConnection<string>();
            Assert.That(wire.IsInTray, Is.True);
            wire.TryInsert("A1");
            Assert.That(wire.IsInTray, Is.False);
            Assert.That(wire.TryInsert("A1"), Is.False);
            Assert.That(wire.IsInTray, Is.False);
            wire.TryInsert("B2");
            Assert.That(wire.IsInTray, Is.False);
        }

        [TestCase("A1", "B2")]
        [TestCase("B2", "A1")]
        public void TrayStaysEmptyWhileEitherEndIsBeingMoved(string removed, string remaining)
        {
            var wire = new CableConnection<string>();
            wire.TryInsert("A1");
            wire.TryInsert("B2");
            wire.TryRemove(removed);
            Assert.That(wire.IsInTray, Is.False);
            Assert.That(wire.TryInsert(remaining), Is.False);
            Assert.That(wire.TryRemove("F6"), Is.False);
            Assert.That(wire.IsInTray, Is.False);
            wire.TryInsert("C3");
            Assert.That(wire.IsInTray, Is.False);
            wire.TryRemove("C3");
            Assert.That(wire.IsInTray, Is.False);
            wire.TryRemove(remaining);
            Assert.That(wire.IsInTray, Is.True);
            wire.TryInsert("D4");
            Assert.That(wire.IsInTray, Is.False);
            wire.TryInsert("E5");
            Assert.That(wire.IsInTray, Is.False);
        }

        [Test]
        public void CancellingTheInitialFirstPlugReturnsTheCableToTheTray()
        {
            var wire = new CableConnection<string>();
            wire.TryInsert("A1");
            wire.TryRemove("A1");
            Assert.That(wire.IsInTray, Is.True);
            Assert.That(wire.Phase, Is.EqualTo(CablePhase.Ready));
        }

        [Test]
        public void ResetRestoresTheCableInTheTray()
        {
            var wire = new CableConnection<string>();
            wire.TryInsert("A1");
            wire.TryInsert("B2");
            wire.TryRemove("A1");
            wire.Reset();
            Assert.That(wire.IsInTray, Is.True);
        }

        [Test]
        public void FirstThenSecondPlugConnectsTwoDistinctSockets()
        {
            var wire = new CableConnection<string>();
            Assert.That(wire.Phase, Is.EqualTo(CablePhase.Ready));
            Assert.That(wire.TryInsert("A1"), Is.True);
            Assert.That(wire.Phase, Is.EqualTo(CablePhase.OneEndConnected));
            Assert.That(wire.TryInsert("C3"), Is.True);
            Assert.That(wire.Phase, Is.EqualTo(CablePhase.Connected));
            Assert.That(wire.FirstSocket, Is.EqualTo("A1"));
            Assert.That(wire.SecondSocket, Is.EqualTo("C3"));
        }

        [Test]
        public void SameSocketIsRejectedWithoutLosingTheFirstPlug()
        {
            var wire = new CableConnection<string>();
            Assert.That(wire.PreventSameSocketConnection, Is.True);
            wire.TryInsert("A1");
            Assert.That(wire.TryInsert("A1"), Is.False);
            Assert.That(wire.FirstSocket, Is.EqualTo("A1"));
            Assert.That(wire.SecondSocket, Is.Null);
            Assert.That(wire.Phase, Is.EqualTo(CablePhase.OneEndConnected));
        }

        [TestCase("A1", "B2")]
        [TestCase("B2", "A1")]
        public void EitherEndCanBeRepluggedWithoutMovingTheOtherEnd(string removed, string remaining)
        {
            var wire = new CableConnection<string>();
            wire.TryInsert("A1");
            wire.TryInsert("B2");
            Assert.That(wire.TryRemove(removed), Is.True);
            Assert.That(wire.Phase, Is.EqualTo(CablePhase.OneEndConnected));
            Assert.That(wire.Contains(remaining), Is.True);
            Assert.That(wire.Contains(removed), Is.False);
            Assert.That(wire.TryInsert("C3"), Is.True);
            Assert.That(wire.Phase, Is.EqualTo(CablePhase.Connected));
            Assert.That(wire.FirstSocket, Is.EqualTo(removed == "A1" ? "C3" : "A1"));
            Assert.That(wire.SecondSocket, Is.EqualTo(removed == "B2" ? "C3" : "B2"));
        }

        [TestCase("A1")]
        [TestCase("B2")]
        public void LooseEndCanReturnToItsOriginalSocket(string removed)
        {
            var wire = new CableConnection<string>();
            wire.TryInsert("A1");
            wire.TryInsert("B2");
            wire.TryRemove(removed);
            Assert.That(wire.TryInsert(removed), Is.True);
            Assert.That(wire.FirstSocket, Is.EqualTo("A1"));
            Assert.That(wire.SecondSocket, Is.EqualTo("B2"));
            Assert.That(wire.Phase, Is.EqualTo(CablePhase.Connected));
        }

        [TestCase("A1", "B2")]
        [TestCase("B2", "A1")]
        public void RemovingTheRemainingEndFullyReleasesTheCable(string removed, string remaining)
        {
            var wire = new CableConnection<string>();
            wire.TryInsert("A1");
            wire.TryInsert("B2");
            wire.TryRemove(removed);
            Assert.That(wire.TryRemove(removed), Is.False);
            Assert.That(wire.TryRemove(remaining), Is.True);
            Assert.That(wire.FirstSocket, Is.Null);
            Assert.That(wire.SecondSocket, Is.Null);
            Assert.That(wire.Phase, Is.EqualTo(CablePhase.Ready));
            Assert.That(wire.TryInsert("D4"), Is.True);
        }

        [TestCase("A1", "B2")]
        [TestCase("B2", "A1")]
        public void LooseEndCannotBeInsertedIntoTheRemainingEndsSocket(string removed, string remaining)
        {
            var wire = new CableConnection<string>();
            wire.TryInsert("A1");
            wire.TryInsert("B2");
            wire.TryRemove(removed);
            Assert.That(wire.CanInsert(remaining), Is.False);
            Assert.That(wire.TryInsert(remaining), Is.False);
            Assert.That(wire.Contains(remaining), Is.True);
            Assert.That(wire.Phase, Is.EqualTo(CablePhase.OneEndConnected));
            Assert.That(wire.TryInsert("C3"), Is.True);
        }

        [Test]
        public void EitherEndCanBeMovedAgainAfterReconnection()
        {
            var wire = new CableConnection<string>();
            wire.TryInsert("A1");
            wire.TryInsert("B2");
            wire.TryRemove("A1");
            wire.TryInsert("C3");
            wire.TryRemove("B2");
            Assert.That(wire.TryInsert("D4"), Is.True);
            Assert.That(wire.FirstSocket, Is.EqualTo("C3"));
            Assert.That(wire.SecondSocket, Is.EqualTo("D4"));
            Assert.That(wire.Phase, Is.EqualTo(CablePhase.Connected));
        }

        [Test]
        public void RemovingTheOnlyInsertedEndStartsAFreshCable()
        {
            var wire = new CableConnection<string>();
            wire.TryInsert("A1");
            Assert.That(wire.TryRemove("A1"), Is.True);
            Assert.That(wire.Phase, Is.EqualTo(CablePhase.Ready));
            Assert.That(wire.TryInsert("E5"), Is.True);
            Assert.That(wire.FirstSocket, Is.EqualTo("E5"));
        }

        [Test]
        public void ThirdPlugAndUnrelatedRemovalDoNotChangeAConnection()
        {
            var wire = new CableConnection<string>();
            wire.TryInsert("A1");
            wire.TryInsert("B2");
            Assert.That(wire.TryInsert("C3"), Is.False);
            Assert.That(wire.TryRemove("C3"), Is.False);
            Assert.That(wire.TryInsert(null), Is.False);
            Assert.That(wire.TryRemove(null), Is.False);
            Assert.That(wire.Phase, Is.EqualTo(CablePhase.Connected));
        }

        [Test]
        public void ResetReleasesTheRemainingEnd()
        {
            var wire = new CableConnection<string>();
            wire.TryInsert("A1");
            wire.TryInsert("B2");
            wire.TryRemove("B2");
            wire.Reset();
            Assert.That(wire.FirstSocket, Is.Null);
            Assert.That(wire.SecondSocket, Is.Null);
            Assert.That(wire.Phase, Is.EqualTo(CablePhase.Ready));
        }
    }
}
