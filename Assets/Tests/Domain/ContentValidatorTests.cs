using System.Collections.Generic;
using NUnit.Framework;
using Switchboard.Content;
using UnityEditor;

namespace Switchboard.Domain.Tests
{
    public sealed class ContentValidatorTests
    {
        private const string GameConfigPath = "Assets/Content/GameConfig.asset";

        private static BookEntry Entry(string name, string jack) => new BookEntry(name, "", "", false, jack);

        private static void AssertSingleError(IReadOnlyList<string> errors, string fragment)
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
        public void ValidateBookUsesTheBookLoadersChecks() =>
            AssertSingleError(ContentValidator.ValidateBook(
                "{ \"entries\": [ { \"name\": \"MOSS, Ada\", \"jack\": \"G9\" } ] }"), "jack G9 is not on the board");

        [Test]
        public void EveryJackACallUsesNeedsAnEntryInTheBook()
        {
            var book = new[] { Entry("MOSS, Ada", "A1"), Entry("THORNE, Basil", "B2") };
            var calls = new[] { Scripts.Call("call_001", "A1", "B2"), Scripts.Call("call_002", "B2", "E5") };

            List<string> errors = ContentValidator.ValidateCallJacks(calls, book);

            AssertSingleError(errors, "call_002 uses E5, but no entry in Book.json is on E5");
        }

        [Test]
        public void ASharedJackCountsForEveryCallOnIt()
        {
            var book = new[] { Entry("MOSS, Ada", "C3"), Entry("MOSS, Harriet", "C3"), Entry("DUNMORE, Mabel", "A2") };
            var calls = new[] { Scripts.Call("call_001", "C3", "A2"), Scripts.Call("call_002", "A2", "C3") };

            Assert.That(ContentValidator.ValidateCallJacks(calls, book), Is.Empty);
        }

        [Test]
        public void AnEmptyBookReportsBothJacksOfACall()
        {
            List<string> errors = ContentValidator.ValidateCallJacks(new[] { Scripts.Call("call_001", "C3", "A2") }, null);

            Assert.That(errors.Count, Is.EqualTo(2));
            Assert.That(errors[0], Does.Contain("C3"));
            Assert.That(errors[1], Does.Contain("A2"));
        }
    }
}
