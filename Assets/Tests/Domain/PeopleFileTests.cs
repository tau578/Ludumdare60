using System;
using System.Collections.Generic;
using NUnit.Framework;
using Switchboard.Content;
using UnityEditor;
using UnityEngine;

namespace Switchboard.Domain.Tests
{
    public sealed class PeopleFileTests
    {
        private const string PeopleFilePath = "Assets/Configs/PhoneBook.json";
        private const string GameConfigPath = "Assets/Content/GameConfig.asset";

        private static readonly ICallerType Regular = new FakeCallerType(new FloatRange(3, 8), new FloatRange(15, 30));

        // The team's board in Level.unity: rows A to F, columns 1 to 6.
        private static IEnumerable<string> Board()
        {
            for (char row = 'A'; row <= 'F'; row++)
                for (int column = 1; column <= 6; column++)
                    yield return $"{row}{column}";
        }

        private static PeopleCatalog Load(string json) =>
            PeopleFileLoader.Load(json.Replace('\'', '"'), new[] { Regular },
                name => name != null && name.StartsWith("caller-", StringComparison.Ordinal), Board());

        private static PeopleCatalog LoadRealFile()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath);
            Assert.That(config, Is.Not.Null, GameConfigPath);
            return config.LoadPeople(Board());
        }

        [Test]
        public void RealFileHasThirtySixValidPeopleOnEveryJack()
        {
            PeopleCatalog catalog = LoadRealFile();

            Assert.That(catalog.Errors, Is.Empty);
            Assert.That(catalog.People.Count, Is.EqualTo(36));
            Assert.That(catalog.Callers.Count, Is.EqualTo(36));
            Assert.That(catalog.Directory.Count, Is.EqualTo(36));
        }

        [Test]
        public void RealFileIsSortedBySurnameBecauseTheBookShowsFileOrder()
        {
            IReadOnlyList<IDirectoryEntry> entries = LoadRealFile().Directory;
            for (int i = 1; i < entries.Count; i++)
                Assert.That(string.Compare(entries[i - 1].Person.LastName, entries[i].Person.LastName, StringComparison.OrdinalIgnoreCase),
                    Is.LessThan(0), $"{entries[i - 1].Person.LastName} before {entries[i].Person.LastName}");
        }

        [Test]
        public void RealFileKeepsThePhoneBookFormat()
        {
            var file = AssetDatabase.LoadAssetAtPath<TextAsset>(PeopleFilePath);
            Assert.That(file, Is.Not.Null, PeopleFilePath);
            PeopleFileData data = JsonUtility.FromJson<PeopleFileData>(file.text);

            Assert.That(data.contactsPerPage, Is.InRange(1, 12));
            foreach (PeopleFileContact contact in data.contacts)
            {
                Assert.That(contact.name, Is.Not.Empty);
                Assert.That(contact.socket, Does.Match("^[A-Z][1-9][0-9]*$"));
            }
        }

        [Test]
        public void ParsesNamesJacksTypesAndOverrides()
        {
            PeopleCatalog catalog = Load(@"{ 'contactsPerPage': 6, 'contacts': [
                { 'name': 'Ada Moss', 'socket': 'c3', 'portrait': 'caller-01', 'type': 'type.regular', 'rate': 0, 'fee': 0 },
                { 'name': ' Mary  Ann Smith ', 'socket': 'A1', 'portrait': 'caller-02', 'type': 'type.regular', 'rate': 25, 'fee': 40 } ] }");

            Assert.That(catalog.Errors, Is.Empty);
            var ada = (PersonRecord)catalog.People[0];
            Assert.That(ada.Id, Is.EqualTo("person.ada-moss"));
            Assert.That(ada.FirstName, Is.EqualTo("Ada"));
            Assert.That(ada.LastName, Is.EqualTo("Moss"));
            Assert.That(ada.Portrait, Is.EqualTo("caller-01"));
            Assert.That(catalog.Directory[0].JackLabel, Is.EqualTo("C3"));
            Assert.That(catalog.Directory[0].Person, Is.SameAs(ada));

            ICaller adaCaller = catalog.Callers[0];
            Assert.That(adaCaller.Id, Is.EqualTo("caller.ada-moss"));
            Assert.That(adaCaller.Person, Is.SameAs(ada));
            Assert.That(adaCaller.Type, Is.SameAs(Regular));
            Assert.That(adaCaller.Weight, Is.EqualTo(1));
            Assert.That(adaCaller.CentsPerSecondOverride, Is.EqualTo(0));
            Assert.That(adaCaller.ConnectionFeeCentsOverride, Is.EqualTo(0));

            IPerson mary = catalog.People[1];
            Assert.That(mary.FirstName, Is.EqualTo("Mary Ann"));
            Assert.That(mary.LastName, Is.EqualTo("Smith"));
            Assert.That(catalog.Callers[1].CentsPerSecondOverride, Is.EqualTo(25));
            Assert.That(catalog.Callers[1].ConnectionFeeCentsOverride, Is.EqualTo(40));
        }

        [Test]
        public void InvalidEntriesAreReportedAndLeftOut()
        {
            PeopleCatalog catalog = Load(@"{ 'contacts': [
                { 'name': 'Ada Moss', 'socket': 'C3', 'portrait': 'caller-01', 'type': 'type.regular' },
                { 'name': 'Basil Thorne', 'socket': 'G9', 'portrait': 'caller-02', 'type': 'type.regular' },
                { 'name': 'Clara Whitlock', 'socket': 'C3', 'portrait': 'caller-03', 'type': 'type.regular' },
                { 'name': 'Edgar Pennington', 'socket': 'B4', 'portrait': 'nobody', 'type': 'type.regular' },
                { 'name': 'Florence Abbott', 'socket': 'F6', 'portrait': 'caller-05', 'type': 'type.vip' },
                { 'name': '  ', 'socket': 'D2', 'portrait': 'caller-06', 'type': 'type.regular' } ] }");

            Assert.That(catalog.People.Count, Is.EqualTo(1));
            Assert.That(catalog.People[0].LastName, Is.EqualTo("Moss"));
            Assert.That(catalog.Errors.Count, Is.EqualTo(5));
            Assert.That(catalog.Errors[0], Does.Contain("Basil Thorne").And.Contain("G9 is not on the board"));
            Assert.That(catalog.Errors[1], Does.Contain("Clara Whitlock").And.Contain("C3 is already used by Ada Moss"));
            Assert.That(catalog.Errors[2], Does.Contain("Edgar Pennington").And.Contain("portrait 'nobody'"));
            Assert.That(catalog.Errors[3], Does.Contain("Florence Abbott").And.Contain("call type 'type.vip' is unknown"));
            Assert.That(catalog.Errors[4], Does.Contain("Contact 6").And.Contain("name is empty"));
        }

        [TestCase("")]
        [TestCase("{ not json")]
        public void UnreadableFileIsReported(string json)
        {
            PeopleCatalog catalog = Load(json);

            Assert.That(catalog.Errors.Count, Is.EqualTo(1));
            Assert.That(catalog.People, Is.Empty);
        }
    }
}
