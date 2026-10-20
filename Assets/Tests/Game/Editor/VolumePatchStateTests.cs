using System.Linq;
using NUnit.Framework;
using Switchboard.UI;

namespace Switchboard.Game.Tests
{
    /// <summary>The volume patch (lead, 2026-10-20): any row, the column is the volume, no jack moves on its own.</summary>
    public sealed class VolumePatchStateTests
    {
        // Socket indices as saved (AudioVolumeSettings.Levels): mute is index 4 although drawn leftmost.
        private const int Quarter = 0, Half = 1, ThreeQuarters = 2, Full = 3, Mute = 4;
        private const int Master = 0, Music = 1, Sfx = 2;

        private static VolumePatchState Patch(int[] rows, int[] sockets)
        {
            var patch = new VolumePatchState();
            Assert.That(patch.Restore(rows, sockets), Is.True);
            return patch;
        }

        private static void AssertAt(VolumePatchState patch, int cable, int row, int socket)
        {
            Assert.That(patch.RowOf(cable), Is.EqualTo(row), "row of cable " + cable);
            Assert.That(patch.SocketOf(cable), Is.EqualTo(socket), "socket of cable " + cable);
        }

        [Test]
        public void TheLeadsReproLeavesMusicWhereItWas()
        {
            VolumePatchState patch = AudioVolumeSettings.Patch;
            int[] rows = Enumerable.Range(0, 3).Select(patch.RowOf).ToArray();
            int[] sockets = Enumerable.Range(0, 3).Select(patch.SocketOf).ToArray();
            try
            {
                Assert.That(patch.Restore(new[] { 0, 1, 2 }, new[] { ThreeQuarters, Quarter, Half }), Is.True);
                Assert.That(patch.PickUp(Master), Is.True);
                Assert.That(patch.Connect(1, Half), Is.True);

                AssertAt(patch, Master, 1, Half);
                AssertAt(patch, Music, 1, Quarter);
                AssertAt(patch, Sfx, 2, Half);
                Assert.That(patch.HeldCable, Is.EqualTo(-1));
                Assert.That(AudioVolumeSettings.GetVolume(AudioVolumeChannel.Master), Is.EqualTo(.5f));
                Assert.That(AudioVolumeSettings.GetVolume(AudioVolumeChannel.Music), Is.EqualTo(.25f), "Music was never touched");
                Assert.That(AudioVolumeSettings.GetVolume(AudioVolumeChannel.Sfx), Is.EqualTo(.5f));
            }
            finally { patch.Restore(rows, sockets); }
        }

        [Test]
        public void MovingAWireIntoAnyFreeSocketNeverMovesAnotherWire()
        {
            for (int cable = 0; cable < 3; cable++)
                for (int row = 0; row < 3; row++)
                    for (int socket = 0; socket < 5; socket++)
                    {
                        VolumePatchState patch = Patch(new[] { 0, 1, 2 }, new[] { Full, Mute, Quarter });
                        bool free = patch.CableAt(row, socket) < 0 || patch.CableAt(row, socket) == cable;
                        Assert.That(patch.PickUp(cable), Is.True);
                        Assert.That(patch.Connect(row, socket), Is.EqualTo(free));
                        if (!free) patch.Cancel();
                        int[] startSockets = { Full, Mute, Quarter };
                        for (int other = 0; other < 3; other++)
                        {
                            if (other == cable && free) AssertAt(patch, other, row, socket);
                            else AssertAt(patch, other, other, startSockets[other]);
                        }
                    }
        }

        [Test]
        public void AnOccupiedSocketRefusesAndTheHeldWireStaysWhereItWas()
        {
            VolumePatchState patch = Patch(new[] { 0, 1, 2 }, new[] { ThreeQuarters, Quarter, Half });
            Assert.That(patch.PickUp(Master), Is.True);
            Assert.That(patch.Connect(1, Quarter), Is.False, "Music's socket is taken");
            patch.Cancel();

            AssertAt(patch, Master, 0, ThreeQuarters);
            AssertAt(patch, Music, 1, Quarter);
            AssertAt(patch, Sfx, 2, Half);
            Assert.That(patch.HeldCable, Is.EqualTo(-1));
        }

        [Test]
        public void DroppingAWireBackOnItsOwnSocketIsAllowed()
        {
            VolumePatchState patch = Patch(new[] { 0, 1, 2 }, new[] { ThreeQuarters, Quarter, Half });
            Assert.That(patch.PickUp(Music), Is.True);
            Assert.That(patch.IsFree(1, Quarter), Is.True, "the held wire's own socket counts as free");
            Assert.That(patch.Connect(1, Quarter), Is.True);
            AssertAt(patch, Music, 1, Quarter);
        }

        [Test]
        public void AllThreeWiresCanShareOneRow()
        {
            VolumePatchState patch = Patch(new[] { 0, 1, 2 }, new[] { Half, Half, Half });
            Assert.That(patch.PickUp(Music), Is.True);
            Assert.That(patch.Connect(0, Mute), Is.True);
            Assert.That(patch.PickUp(Sfx), Is.True);
            Assert.That(patch.Connect(0, Full), Is.True);

            AssertAt(patch, Master, 0, Half);
            AssertAt(patch, Music, 0, Mute);
            AssertAt(patch, Sfx, 0, Full);
            Assert.That(patch.CableAt(0, Mute), Is.EqualTo(Music));
            Assert.That(patch.CableAt(0, Full), Is.EqualTo(Sfx));
            Assert.That(patch.CableAt(1, Half), Is.EqualTo(-1), "Music's old row is empty now");
        }

        [Test]
        public void RestoreAcceptsSharedRows()
        {
            VolumePatchState patch = Patch(new[] { 1, 1, 2 }, new[] { Half, Quarter, Mute });
            AssertAt(patch, Master, 1, Half);
            AssertAt(patch, Music, 1, Quarter);
            AssertAt(patch, Sfx, 2, Mute);
        }

        [TestCase(new[] { 1, 1, 2 }, new[] { Half, Half, Mute })]
        [TestCase(new[] { 0, 0, 0 }, new[] { Full, Quarter, Full })]
        [TestCase(new[] { 0, 1, 3 }, new[] { Half, Half, Half })]
        [TestCase(new[] { -1, 1, 2 }, new[] { Half, Half, Half })]
        [TestCase(new[] { 0, 1, 2 }, new[] { Half, 5, Half })]
        [TestCase(new[] { 0, 1, 2 }, new[] { Half, Half, -1 })]
        [TestCase(new[] { 0, 1 }, new[] { Half, Half })]
        public void RestoreRejectsTwoWiresInOneSocketAndOutOfRangeValues(int[] rows, int[] sockets)
        {
            var patch = new VolumePatchState();
            Assert.That(patch.Restore(rows, sockets), Is.False);
            for (int i = 0; i < 3; i++) AssertAt(patch, i, i, Half);
        }

        [Test]
        public void AnOldOneWirePerRowSaveLoadsUnchanged()
        {
            VolumePatchState patch = Patch(new[] { 2, 0, 1 }, new[] { Quarter, Mute, Full });
            AssertAt(patch, Master, 2, Quarter);
            AssertAt(patch, Music, 0, Mute);
            AssertAt(patch, Sfx, 1, Full);
        }
    }
}
