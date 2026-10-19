using System.Text;
using System.Linq;
using NUnit.Framework;
using Switchboard.UI;
using UnityEngine;

namespace Switchboard.Game.Tests
{
    // The book lives in Assembly-CSharp, which asmdef test assemblies can't reference,
    // so this test sits in an Editor folder without an asmdef (Assembly-CSharp-Editor).
    public sealed class PhoneBookTabTests
    {
        private GameObject root;
        private TextAsset file;

        [TearDown]
        public void TearDown()
        {
            if (root != null) Object.DestroyImmediate(root);
            if (file != null) Object.DestroyImmediate(file);
        }

        // Six per page, so a spread holds 12 contacts. Jacks repeat once past F6, as households share a line.
        private PhoneBookView Book(params string[] names)
        {
            var jacks = Switchboard.Content.ContentValidator.BoardJacks();
            var json = new StringBuilder("{ \"contactsPerPage\": 6, \"entries\": [");
            for (int i = 0; i < names.Length; i++)
                json.Append(i > 0 ? "," : "").Append($"{{ \"name\": \"{names[i]}\", \"jack\": \"{jacks[i % jacks.Count]}\" }}");
            json.Append("] }");
            return BookFrom(json.ToString());
        }

        private PhoneBookView BookFrom(string json)
        {
            file = new TextAsset(json);
            root = new GameObject("PhoneBook", typeof(RectTransform));
            var view = root.AddComponent<PhoneBookView>();
            view.ConfigureDirectory(file, null, null, null, null);
            view.ReloadContacts();
            return view;
        }

        private static string[] Surnames(int count, char letter, int from = 0)
        {
            var names = new string[count];
            for (int i = 0; i < count; i++) names[i] = $"{letter}{(char)('A' + (from + i) / 26)}{(char)('A' + (from + i) % 26)}, Ann";
            return names;
        }

        private static string[] Concat(params string[][] parts)
        {
            int length = 0;
            foreach (var part in parts) length += part.Length;
            var all = new string[length];
            int at = 0;
            foreach (var part in parts) { part.CopyTo(all, at); at += part.Length; }
            return all;
        }

        private const string MixedFile = @"{ 'contactsPerPage': 6, 'entries': [
                { 'name': 'MOSS, Ada', 'jack': 'C1' },
                { 'name': 'Police', 'emergencyService': true, 'jack': 'B1' },
                { 'name': ' ABBOTT, Zoe ', 'jack': 'a1' },
                { 'name': 'Fire Brigade', 'emergencyService': true, 'jack': 'F1' },
                { 'name': 'brown, Ada', 'jack': 'C1' }
            ] }";

        // D53: the book sorts itself, services first in file order, then names alphabetically ignoring case.
        [Test]
        public void TheBookListsServicesFirstThenNamesAlphabetically()
        {
            var view = BookFrom(MixedFile.Replace('\'', '"'));
            var shown = Enumerable.Range(0, view.ContactCount).Select(view.ContactAt).ToArray();
            Assert.That(shown.Select(c => c.Name), Is.EqualTo(new[]
                { "Police", "Fire Brigade", "ABBOTT, Zoe", "brown, Ada", "MOSS, Ada" }));
            Assert.That(shown.Select(c => c.Jack), Is.EqualTo(new[]
                { "B1", "F1", "A1", "C1", "C1" }));
            Assert.That(shown.Select(c => c.IsEmergencyService), Is.EqualTo(new[]
                { true, true, false, false, false }));
        }

        [Test]
        public void LettersGoByTheFirstLetterOfTheName()
        {
            var view = BookFrom(MixedFile.Replace('\'', '"'));

            Assert.That(view.HasLetter('P'), Is.True, "Police");
            Assert.That(view.HasLetter('F'), Is.True, "Fire Brigade");
            Assert.That(view.HasLetter('A'), Is.True, "ABBOTT");
            Assert.That(view.HasLetter('b'), Is.True, "brown");
            Assert.That(view.HasLetter('M'), Is.True, "MOSS");
            Assert.That(view.HasLetter('Z'), Is.False, "Zoe is a given name");
        }

        [Test]
        public void JumpsToTheSpreadWithTheFirstMatchingSurname()
        {
            // A x10, M x5 (spreads 0 and 1), T x15 (spreads 1 and 2), Z x6 (spread 2).
            var view = Book(Concat(Surnames(10, 'A'), Surnames(5, 'M'), Surnames(15, 'T'), Surnames(6, 'Z')));

            Assert.That(view.ShowLetter('M'), Is.True);
            Assert.That(view.CurrentSpreadIndex, Is.EqualTo(0));
            Assert.That(view.ShowLetter('t'), Is.True);
            Assert.That(view.CurrentSpreadIndex, Is.EqualTo(1));
            Assert.That(view.ShowLetter('Z'), Is.True);
            Assert.That(view.CurrentSpreadIndex, Is.EqualTo(2));
        }

        [Test]
        public void WorksOnTheFirstAndLastSpread()
        {
            var view = Book(Concat(Surnames(30, 'B'), Surnames(1, 'Y')));

            Assert.That(view.ShowLetter('Y'), Is.True);
            Assert.That(view.CurrentSpreadIndex, Is.EqualTo(2));
            Assert.That(view.CanGoForward, Is.False);
            Assert.That(view.ShowLetter('B'), Is.True);
            Assert.That(view.CurrentSpreadIndex, Is.EqualTo(0));
            Assert.That(view.CanGoBack, Is.False);
        }

        [Test]
        public void IgnoresLettersWithNoSurname()
        {
            var view = Book(Concat(Surnames(13, 'C'), Surnames(2, 'D')));
            view.NextSpread();

            Assert.That(view.HasLetter('Q'), Is.False);
            Assert.That(view.ShowLetter('Q'), Is.False);
            Assert.That(view.CurrentSpreadIndex, Is.EqualTo(1));
            Assert.That(view.HasLetter('D'), Is.True);
        }

        [Test]
        public void SurnameFirstNamesUseTheSurnameInsteadOfTheGivenName()
        {
            var view = Book(Concat(Surnames(12, 'A'), new[] { "Smith, Mary Ann" }));
            Assert.That(view.HasLetter('M'), Is.False);
            Assert.That(view.ShowLetter('S'), Is.True);
            Assert.That(view.CurrentSpreadIndex, Is.EqualTo(1));
        }

        [Test]
        public void ANameWithoutACommaGoesByItsFirstLetter()
        {
            var view = Book(Concat(Surnames(12, 'A'), new[] { "Mary Ann Smith" }));

            Assert.That(view.HasLetter('S'), Is.False);
            Assert.That(view.ShowLetter('M'), Is.True);
            Assert.That(view.CurrentSpreadIndex, Is.EqualTo(1));
        }
    }
}
