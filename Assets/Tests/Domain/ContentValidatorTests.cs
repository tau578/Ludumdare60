using System;
using System.Collections.Generic;
using NUnit.Framework;
using Switchboard.Content;
using UnityEditor;

namespace Switchboard.Domain.Tests
{
    public sealed class ContentValidatorTests
    {
        private const string GameConfigPath = "Assets/Content/GameConfig.asset";

        private static string Contact(string name, string socket, string portrait = "caller-01", string type = "type.regular") =>
            $"{{ 'name': '{name}', 'socket': '{socket}', 'portrait': '{portrait}', 'type': '{type}', 'rate': 0, 'fee': 0 }}";

        private static string Service(string name, string socket) =>
            $"{{ 'name': '{name}', 'socket': '{socket}', 'tags': ['emergency-service'] }}";

        private static List<string> WithServices(params string[] contacts) => Validate(6, contacts);

        private static List<string> Validate(int perPage, params string[] contacts)
        {
            string json = $"{{ 'contactsPerPage': {perPage}, 'contacts': [ {string.Join(", ", contacts)} ] }}";
            return ContentValidator.Validate(json.Replace('\'', '"'), ContentValidator.BoardJacks());
        }

        private static List<string> ValidPair(params string[] more)
        {
            var contacts = new List<string> { Contact("Ada Moss", "A1"), Contact("Basil Thorne", "B2") };
            contacts.AddRange(more);
            return Validate(6, contacts.ToArray());
        }

        private static void AssertSingleError(List<string> errors, string fragment)
        {
            Assert.That(errors.Count, Is.EqualTo(1), string.Join("\n", errors));
            Assert.That(errors[0], Does.Contain(fragment));
        }

        [Test]
        public void RealContentHasNoProblems()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath);
            Assert.That(config, Is.Not.Null, GameConfigPath);

            Assert.That(config.Validate(), Is.Empty);
        }

        [Test]
        public void BoardHasThirtySixJacks()
        {
            List<string> jacks = ContentValidator.BoardJacks();
            Assert.That(jacks.Count, Is.EqualTo(36));
            Assert.That(jacks, Is.Unique);
            Assert.That(jacks[0], Is.EqualTo("A1"));
            Assert.That(jacks[35], Is.EqualTo("F6"));
        }

        [Test]
        public void AValidFilePasses() => Assert.That(ValidPair(), Is.Empty);

        [Test]
        public void ReportsADuplicateSocket() => AssertSingleError(ValidPair(Contact("Clara Whitlock", "A1")), "already used");

        [Test]
        public void ReportsAnUnknownJack() => AssertSingleError(ValidPair(Contact("Clara Whitlock", "G7")), "not on the board");

        [Test]
        public void PortraitsTypesAndRatesInTheBookAreNoLongerRead() =>
            Assert.That(ValidPair(Contact("Clara Whitlock", "C3", "nobody", "type.vip")), Is.Empty);

        [Test]
        public void ReportsADuplicateSurname() => AssertSingleError(ValidPair(Contact("Ivy Thorne", "C3")), "surname Thorne");

        [Test]
        public void ReportsAFileNotSortedBySurname() =>
            AssertSingleError(Validate(6, Contact("Basil Thorne", "B2"), Contact("Ada Moss", "A1")), "sort the file by surname");

        [Test]
        public void ReportsAPageSizeTheBookRejects() =>
            AssertSingleError(Validate(13, Contact("Ada Moss", "A1"), Contact("Basil Thorne", "B2")), "contactsPerPage");

        [Test]
        public void ServicesAtTheTopPassWithoutPortraitsOrTypes() =>
            Assert.That(WithServices(Service("Police", "B6"), Service("Fire Brigade", "C2"),
                Contact("Ada Moss", "A1"), Contact("Basil Thorne", "B2")), Is.Empty);

        [Test]
        public void ReportsAServiceBelowThePeople()
        {
            List<string> errors = WithServices(Service("Police", "B6"), Contact("Ada Moss", "A1"),
                Contact("Basil Thorne", "B2"), Service("Fire Brigade", "C2"));

            AssertSingleError(errors, "Contact 4 \"Fire Brigade\": emergency services must be listed at the top of the file");
        }

        [Test]
        public void ServiceNamesDoNotCountAsSurnames() =>
            Assert.That(WithServices(Service("Police", "B6"), Service("Doctor", "D4"),
                Contact("Ada Moss", "A1"), Contact("Basil Thorne", "B2")), Is.Empty);

        [Test]
        public void ReportsAPersonOutOfOrderBelowTheServices() =>
            AssertSingleError(WithServices(Service("Police", "B6"), Contact("Basil Thorne", "B2"), Contact("Ada Moss", "A1")),
                "\"Ada Moss\" is listed after \"Basil Thorne\"");

        [Test]
        public void EveryJackACallUsesNeedsSomeoneInTheBook()
        {
            var book = new IDirectoryEntry[]
            {
                new FakeDirectoryEntry(new FakePerson("Ada", "Moss"), "A1"),
                new FakeDirectoryEntry(new FakePerson("Basil", "Thorne"), "B2")
            };
            var calls = new[] { Scripts.Call("call_001", "A1", "B2"), Scripts.Call("call_002", "B2", "E5") };

            List<string> errors = ContentValidator.ValidateCallJacks(calls, book);

            AssertSingleError(errors, "call_002 uses E5, but nobody is listed on E5 in PhoneBook.json");
        }
    }
}
