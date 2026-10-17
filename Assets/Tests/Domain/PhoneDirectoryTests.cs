using System.Collections.Generic;
using NUnit.Framework;

namespace Switchboard.Domain.Tests
{
    public sealed class PhoneDirectoryTests
    {
        [Test]
        public void ListsEntriesSortedBySurname()
        {
            var world = new TestWorld();
            var surnames = new List<string>();
            foreach (IDirectoryEntry entry in world.Directory.ListedEntries) surnames.Add(entry.Person.LastName);
            Assert.That(surnames, Is.EqualTo(new[] { "Moss", "Thorne", "Whitlock" }));
        }

        [Test]
        public void FindsJacksByPersonAndPeopleByJack()
        {
            var world = new TestWorld();
            Assert.That(world.Directory.JackOf(world.Basil), Is.EqualTo(TestWorld.B2));
            Assert.That(world.Directory.PersonAt(TestWorld.C3), Is.SameAs(world.Clara));
            Assert.That(world.Directory.PersonAt(new JackId("F6")), Is.Null);

            var stranger = new FakePerson("Otis", "Bramble");
            Assert.That(world.Directory.Lists(stranger), Is.False);
            Assert.That(world.Directory.TryGetJack(stranger, out _), Is.False);
            Assert.Throws<KeyNotFoundException>(() => world.Directory.JackOf(stranger));
        }

        [Test]
        public void DuplicatePeopleAndJacksKeepTheFirstEntry()
        {
            var ada = new FakePerson("Ada", "Moss");
            var basil = new FakePerson("Basil", "Thorne");
            var directory = new PhoneDirectory(new IDirectoryEntry[]
            {
                new FakeDirectoryEntry(ada, "A1"),
                new FakeDirectoryEntry(ada, "D4"),
                new FakeDirectoryEntry(basil, "A1"),
                new FakeDirectoryEntry(basil, "B2")
            });
            Assert.That(directory.ListedEntries.Count, Is.EqualTo(2));
            Assert.That(directory.JackOf(ada), Is.EqualTo(TestWorld.A1));
            Assert.That(directory.JackOf(basil), Is.EqualTo(TestWorld.B2));
        }
    }
}
