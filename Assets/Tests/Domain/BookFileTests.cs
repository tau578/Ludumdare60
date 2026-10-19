using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Switchboard.Content;
using UnityEditor;
using UnityEngine;

namespace Switchboard.Domain.Tests
{
    // Book.json (D53): parsing, the checks and the order, in the one loader the book and Validate Content share.
    public sealed class BookFileTests
    {
        private const string BookFilePath = "Assets/Configs/Book.json";
        private const string GameConfigPath = "Assets/Content/GameConfig.asset";

        private static BookCatalog Load(string json) => BookFileLoader.Load(json.Replace('\'', '"'));

        private static string File(params string[] entries) => $"{{ 'entries': [ {string.Join(", ", entries)} ] }}";

        private static string Person(string name, string jack, string profession = "Seamstress", string address = "Oak St. 4") =>
            $"{{ 'name': '{name}', 'profession': '{profession}', 'address': '{address}', 'emergencyService': false, 'jack': '{jack}' }}";

        private static string Service(string name, string jack) =>
            $"{{ 'name': '{name}', 'profession': 'Station', 'address': 'High St. 1', 'emergencyService': true, 'jack': '{jack}' }}";

        private static string[] Names(BookCatalog book) => book.Entries.Select(e => e.Name).ToArray();

        private static void AssertSingleError(BookCatalog book, string fragment)
        {
            Assert.That(book.Errors.Count, Is.EqualTo(1), string.Join("\n", book.Errors));
            Assert.That(book.Errors[0], Does.Contain(fragment));
        }

        [Test]
        public void ReadsTheDesignersFormat()
        {
            BookCatalog book = Load(@"{ 'contactsPerPage': 4, 'entries': [
                { 'name': 'Fire Brigade', 'profession': 'Fire station', 'address': 'Station Rd. 1', 'emergencyService': true, 'jack': 'E5' },
                { 'name': ' MOSS,  Ada ', 'profession': ' Seamstress ', 'address': 'Oak St. 4', 'emergencyService': false, 'jack': ' c3 ' } ] }");

            Assert.That(book.Errors, Is.Empty);
            Assert.That(book.ContactsPerPage, Is.EqualTo(4));
            Assert.That(book.Entries.Count, Is.EqualTo(2));
            BookEntry brigade = book.Entries[0];
            Assert.That(brigade.Name, Is.EqualTo("Fire Brigade"));
            Assert.That(brigade.IsEmergencyService, Is.True);
            Assert.That(brigade.Jack, Is.EqualTo("E5"));
            BookEntry ada = book.Entries[1];
            Assert.That(ada.Name, Is.EqualTo("MOSS, Ada"), "trimmed, single spaces");
            Assert.That(ada.Profession, Is.EqualTo("Seamstress"));
            Assert.That(ada.Address, Is.EqualTo("Oak St. 4"));
            Assert.That(ada.IsEmergencyService, Is.False);
            Assert.That(ada.Jack, Is.EqualTo("C3"), "trimmed and upper-cased");
        }

        [Test]
        public void MissingContactsPerPageMeansSix()
        {
            BookCatalog book = Load(File(Person("MOSS, Ada", "C3")));
            Assert.That(book.Errors, Is.Empty);
            Assert.That(book.ContactsPerPage, Is.EqualTo(6));
        }

        [TestCase(0)]
        [TestCase(13)]
        [TestCase(-2)]
        public void APageSizeOutsideOneToTwelveIsReportedAndSixIsUsed(int perPage)
        {
            BookCatalog book = Load($"{{ 'contactsPerPage': {perPage}, 'entries': [ {Person("MOSS, Ada", "C3")} ] }}");
            AssertSingleError(book, $"contactsPerPage is {perPage}");
            Assert.That(book.ContactsPerPage, Is.EqualTo(6));
            Assert.That(book.Entries.Count, Is.EqualTo(1), "the entries still show");
        }

        [Test]
        public void AMissingEmergencyServiceMeansAPerson()
        {
            BookCatalog book = Load("{ 'entries': [ { 'name': 'MOSS, Ada', 'jack': 'C3' } ] }");
            Assert.That(book.Errors, Is.Empty);
            Assert.That(book.Entries[0].IsEmergencyService, Is.False);
        }

        [Test]
        public void ProfessionAndAddressMayBeEmptyOrMissing()
        {
            BookCatalog book = Load(File("{ 'name': 'MOSS, Ada', 'jack': 'C3' }", Person("THORNE, Basil", "A5", "", "")));
            Assert.That(book.Errors, Is.Empty);
            Assert.That(book.Entries.Count, Is.EqualTo(2));
            Assert.That(book.Entries[0].Profession, Is.Empty);
            Assert.That(book.Entries[0].Address, Is.Empty);
            Assert.That(book.Entries[1].Profession, Is.Empty);
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase(null)]
        public void AMissingOrEmptyFileGivesAnEmptyBookWithAMessage(string json)
        {
            BookCatalog book = BookFileLoader.Load(json);
            AssertSingleError(book, "missing or empty");
            Assert.That(book.Entries, Is.Empty);
            Assert.That(book.ContactsPerPage, Is.EqualTo(6));
        }

        [Test]
        public void AnUnreadableFileGivesAnEmptyBookWithAMessage()
        {
            BookCatalog book = Load("{ 'entries': [ { 'name': 'MOSS, Ada' ");
            AssertSingleError(book, "not valid JSON");
            Assert.That(book.Entries, Is.Empty);
        }

        [Test]
        public void AFileWithNoEntriesIsReported()
        {
            AssertSingleError(Load("{ 'contactsPerPage': 6 }"), "no entries");
            AssertSingleError(Load("{ 'entries': [] }"), "no entries");
        }

        [Test]
        public void AMissingNameIsReportedAndLeftOut()
        {
            BookCatalog book = Load(File(Person("MOSS, Ada", "C3"), "{ 'name': '  ', 'jack': 'A1' }"));
            AssertSingleError(book, "Entry 2: the name is missing");
            Assert.That(Names(book), Is.EqualTo(new[] { "MOSS, Ada" }));
        }

        [Test]
        public void AMissingJackIsReportedAndLeftOut()
        {
            BookCatalog book = Load(File(Person("MOSS, Ada", "C3"), "{ 'name': 'THORNE, Basil', 'profession': 'Butcher' }"));
            AssertSingleError(book, "Entry 2 \"THORNE, Basil\": the jack is missing");
            Assert.That(Names(book), Is.EqualTo(new[] { "MOSS, Ada" }));
        }

        [TestCase("G1")]
        [TestCase("A7")]
        [TestCase("A0")]
        [TestCase("C33")]
        [TestCase("33")]
        public void AJackOffTheBoardIsReportedAndLeftOut(string jack)
        {
            BookCatalog book = Load(File(Person("MOSS, Ada", "C3"), Person("THORNE, Basil", jack)));
            AssertSingleError(book, $"jack {jack} is not on the board");
            Assert.That(Names(book), Is.EqualTo(new[] { "MOSS, Ada" }));
        }

        [Test]
        public void ADuplicateNameIsReportedIgnoringCaseAndSpaces()
        {
            BookCatalog book = Load(File(Person("MOSS, Ada", "C3"), Person("  moss,   ada ", "A1")));
            AssertSingleError(book, "already listed as entry 1");
            Assert.That(book.Entries.Count, Is.EqualTo(1));
            Assert.That(book.Entries[0].Jack, Is.EqualTo("C3"), "the first one stays");
        }

        [Test]
        public void FamiliesShareASurnameAndAJack()
        {
            BookCatalog book = Load(File(Person("MOSS, Ada", "C3"), Person("MOSS, Harriet", "C3"), Person("MOSS, Albert", "F6")));
            Assert.That(book.Errors, Is.Empty);
            Assert.That(book.Entries.Count, Is.EqualTo(3));
        }

        [Test]
        public void ABadEntryLeavesTheRestOfTheBook()
        {
            BookCatalog book = Load(File(Person("ABBOTT, Florence", "F6"), "{ 'name': 'BROKEN, Entry', 'jack': 'Z9' }",
                Person("MOSS, Ada", "C3")));
            Assert.That(book.Errors.Count, Is.EqualTo(1));
            Assert.That(Names(book), Is.EqualTo(new[] { "ABBOTT, Florence", "MOSS, Ada" }));
        }

        [Test]
        public void TheOldFormatGetsTheNewNames()
        {
            AssertSingleError(Load("{ 'contactsPerPage': 6, 'contacts': [ { 'name': 'Moss, Ada', 'socket': 'C3' } ] }"),
                "rename \"contacts\" to \"entries\"");
            BookCatalog book = Load(File("{ 'name': 'MOSS, Ada', 'socket': 'C3' }"));
            AssertSingleError(book, "\"socket\" is the old name. Rename it to \"jack\", such as \"jack\": \"C3\"");
            Assert.That(book.Entries, Is.Empty);
        }

        [Test]
        public void ServicesComeFirstInFileOrderThenNamesAlphabetically()
        {
            BookCatalog book = Load(File(Person("THORNE, Basil", "A5"), Service("Police", "B6"), Person("abbott, Florence", "F6"),
                Person("MOSS, Harriet", "C3"), Service("Fire Brigade", "E5"), Person("MOSS, Ada", "C3"), Person("MOSSOP, Kit", "A1"),
                Service("Doctor", "D4"), Person("Bellamy, Frank", "B1")));

            Assert.That(book.Errors, Is.Empty);
            Assert.That(Names(book), Is.EqualTo(new[]
            {
                "Police", "Fire Brigade", "Doctor",
                "abbott, Florence", "Bellamy, Frank", "MOSS, Ada", "MOSS, Harriet", "MOSSOP, Kit", "THORNE, Basil"
            }));
        }

        [Test]
        public void TheOrderDoesNotDependOnTheFilesOrder()
        {
            string[] people = { Person("ZOUCH, Albert", "F5"), Person("MOSS, Ada", "C3"), Service("Police", "B6"), Person("COBBOLD, Doris", "D1") };
            string[] reversed = (string[])people.Clone();
            System.Array.Reverse(reversed);

            Assert.That(Names(Load(File(reversed))), Is.EqualTo(Names(Load(File(people)))));
            Assert.That(Names(Load(File(people))), Is.EqualTo(new[] { "Police", "COBBOLD, Doris", "MOSS, Ada", "ZOUCH, Albert" }));
        }

        // The designer balances Book.json at any time (lead, 2026-10-19): these check the rules, not the entries.
        [Test]
        public void TheRealFilePassesWithServicesFirstThenNamesInOrder()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath);
            Assert.That(config, Is.Not.Null, GameConfigPath);
            BookCatalog book = config.LoadBook();
            var file = JsonUtility.FromJson<BookFileData>(config.BookFileText);

            Assert.That(book.Errors, Is.Empty, string.Join("\n", book.Errors));
            int perPage = file.contactsPerPage == BookFileData.Unset ? BookFileLoader.DefaultContactsPerPage : file.contactsPerPage;
            Assert.That(book.ContactsPerPage, Is.EqualTo(perPage), "the file sets contactsPerPage");
            Assert.That(book.Entries.Count, Is.EqualTo(file.entries.Length), "every entry is listed");
            Assert.That(book.Entries.Count, Is.GreaterThan(0));
            string[] services = file.entries.Where(e => e.emergencyService).Select(e => e.jack.Trim().ToUpperInvariant()).ToArray();
            Assert.That(book.Entries.Take(services.Length).Select(e => e.Jack), Is.EqualTo(services), "services first, in the file's order");
            for (int i = 0; i < book.Entries.Count; i++)
            {
                BookEntry entry = book.Entries[i];
                Assert.That(entry.Name, Is.Not.Empty, $"entry {i + 1}");
                Assert.That(entry.Jack, Is.Not.Empty, entry.Name);
                Assert.That(entry.IsEmergencyService, Is.EqualTo(i < services.Length), entry.Name);
                if (i <= services.Length) continue;
                Assert.That(string.CompareOrdinal(book.Entries[i - 1].Name.ToUpperInvariant(), entry.Name.ToUpperInvariant()),
                    Is.LessThanOrEqualTo(0), $"{book.Entries[i - 1].Name} before {entry.Name}");
            }
            Assert.That(ContentValidator.ValidateCallJacks(config.LoadCalls().Calls, book.Entries), Is.Empty,
                "every jack a call uses is in the book");
        }

        [Test]
        public void TheRealFileUsesOnlyPlainText()
        {
            var file = AssetDatabase.LoadAssetAtPath<TextAsset>(BookFilePath);
            Assert.That(file, Is.Not.Null, BookFilePath);
            foreach (char c in file.text)
                Assert.That(c == '\n' || c == '\r' || (c >= ' ' && c <= '~'), Is.True, $"'{c}' may be missing from the book's font");
            foreach (BookEntry entry in BookFileLoader.Load(file.text).Entries)
                foreach (string part in new[] { entry.Name, entry.Profession, entry.Address })
                    Assert.That(part.IndexOfAny(new[] { '<', '>' }), Is.EqualTo(-1), $"{entry.Name}: no rich-text tags in the book");
        }
    }
}
