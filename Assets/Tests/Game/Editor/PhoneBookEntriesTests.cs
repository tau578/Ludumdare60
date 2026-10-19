using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Switchboard.Content;
using Switchboard.Domain;
using Switchboard.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Switchboard.Game.Tests
{
    // The lookup-only book (D53): every entry of Book.json shows from the start, in the loader's order.
    public sealed class PhoneBookEntriesTests
    {
        private const string PrefabPath = "Assets/Prefabs/UI/PhoneBook.prefab";
        private const string GameConfigPath = "Assets/Content/GameConfig.asset";
        private GameObject root;
        private TextAsset file;

        [TearDown]
        public void TearDown()
        {
            if (root != null) Object.DestroyImmediate(root);
            if (file != null) Object.DestroyImmediate(file);
        }

        private PhoneBookView RealBook(string json = null)
        {
            root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
            var view = root.GetComponent<PhoneBookView>();
            if (json != null)
            {
                file = new TextAsset(json);
                typeof(PhoneBookView).GetField("contactsConfig", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(view, file);
            }
            typeof(PhoneBookView).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(view, null);
            view.SetOpen(true);
            Canvas.ForceUpdateCanvases();
            return view;
        }

        private Transform Page(string side) => root.transform.Find($"OpenBook/Directory/{side}DirectoryPage");

        private Text Label(string side, int row, string label) =>
            Page(side).Find($"Entries/Contact_{row:00}/{label}").GetComponent<Text>();

        // Book.json is the designer's (he balances it at any time), so the expected rows come from the loader.
        [Test]
        public void TheRealBookShowsEveryEntryInTheLoadersOrder()
        {
            BookCatalog expected = AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath).LoadBook();
            PhoneBookView book = RealBook();

            Assert.That(book.ContactCount, Is.EqualTo(expected.Entries.Count));
            Assert.That(book.ContactCount, Is.GreaterThan(0));
            for (int i = 0; i < expected.Entries.Count; i++)
            {
                Assert.That(book.ContactAt(i).Name, Is.EqualTo(expected.Entries[i].Name), $"entry {i + 1}");
                Assert.That(book.ContactAt(i).Jack, Is.EqualTo(expected.Entries[i].Jack), $"entry {i + 1}");
            }
            Assert.That(book.PageCount, Is.EqualTo((expected.Entries.Count + expected.ContactsPerPage - 1) / expected.ContactsPerPage),
                "contactsPerPage entries per page");
        }

        [Test]
        public void ARowShowsTheNameThenProfessionAndAddressAndTheJackLast()
        {
            BookCatalog expected = AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath).LoadBook();
            RealBook();

            // The first rows of the left page, and the first row of the right page when the book reaches it.
            for (int row = 1; row <= System.Math.Min(4, System.Math.Min(expected.ContactsPerPage, expected.Entries.Count)); row++)
                AssertRow("Left", row, expected.Entries[row - 1]);
            if (expected.Entries.Count > expected.ContactsPerPage)
                AssertRow("Right", 1, expected.Entries[expected.ContactsPerPage]);
            Assert.That(Page("Left").Find("JackHeading").GetComponent<Text>().text, Is.EqualTo("Jack"));
            var jack = Label("Left", 1, "Jack").rectTransform;
            var name = Label("Left", 1, "Name").rectTransform;
            Assert.That(jack.anchorMin.x, Is.GreaterThanOrEqualTo(name.anchorMax.x), "the jack is the last column");
        }

        private void AssertRow(string side, int row, BookEntry entry)
        {
            Assert.That(Label(side, row, "Name").text, Is.EqualTo(entry.Name), $"{side} row {row}");
            Assert.That(Label(side, row, "Details").text, Is.EqualTo(PhoneBookContactRow.Details(entry)), $"{side} row {row}");
            Assert.That(Label(side, row, "Jack").text, Is.EqualTo(entry.Jack), $"{side} row {row}");
        }

        [Test]
        public void TheDetailsLineSkipsAnEmptyPart()
        {
            Assert.That(PhoneBookContactRow.Details(new BookEntry("MOSS, Ada", "Seamstress", "", false, "C3")), Is.EqualTo("Seamstress"));
            Assert.That(PhoneBookContactRow.Details(new BookEntry("MOSS, Ada", "", "Oak St. 4", false, "C3")), Is.EqualTo("Oak St. 4"));
            Assert.That(PhoneBookContactRow.Details(new BookEntry("MOSS, Ada", "", "", false, "C3")), Is.Empty);
        }

        [Test]
        public void ThePageCountFollowsContactsPerPage()
        {
            string entries = string.Join(", ", Enumerable.Range(0, 9).Select(i => $"{{ \"name\": \"N{i}, Ann\", \"jack\": \"A{i % 6 + 1}\" }}"));
            PhoneBookView book = RealBook($"{{ \"contactsPerPage\": 4, \"entries\": [ {entries} ] }}");

            Assert.That(book.PageCount, Is.EqualTo(3));
            Assert.That(Page("Right").Find("Entries/Contact_04").gameObject.activeSelf, Is.True);
            Assert.That(book.CanGoForward, Is.True, "the ninth entry is on the second spread");
            book.NextSpread();
            Assert.That(Page("Left").Find("Entries/Contact_01").gameObject.activeSelf, Is.True);
            Assert.That(Page("Left").Find("Entries/Contact_02").gameObject.activeSelf, Is.False);
        }

        [Test]
        public void ABadEntryIsLeftOutAndTheRestStillShows()
        {
            PhoneBookView book = RealBook("{ \"entries\": [ { \"name\": \"MOSS, Ada\", \"jack\": \"C3\" }, " +
                "{ \"name\": \"BROKEN, Entry\", \"jack\": \"Z9\" }, { \"name\": \"THORNE, Basil\", \"jack\": \"A5\" } ] }");

            Assert.That(book.ContactCount, Is.EqualTo(2));
            Assert.That(Label("Left", 2, "Name").text, Is.EqualTo("THORNE, Basil"));
            Assert.That(Page("Left").Find("EmptyMessage").gameObject.activeSelf, Is.False);
        }

        [TestCase("")]
        [TestCase("{ not json")]
        public void AMissingOrUnreadableFileShowsItsMessageOnTheFirstPage(string json)
        {
            PhoneBookView book = RealBook(json);

            Assert.That(book.ContactCount, Is.Zero);
            var message = Page("Left").Find("EmptyMessage").GetComponent<Text>();
            Assert.That(message.gameObject.activeSelf, Is.True);
            Assert.That(message.text, Does.Contain("book file"));
            Assert.That(Page("Right").Find("EmptyMessage").gameObject.activeSelf, Is.False);
        }

        [Test]
        public void AFreshGameOnTheStartingBoardListsEveryone()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath);
            BoardSize start = config.LoadEconomy().StartingBoard;
            BookCatalog expected = config.LoadBook();
            var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Final.unity");
            try
            {
                var all = scene.GetRootGameObjects();
                foreach (var go in all)
                    if (PrefabUtility.IsOutermostPrefabInstanceRoot(go))
                        PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                var book = all.SelectMany(g => g.GetComponentsInChildren<PhoneBookView>(true)).Single();
                book.ReloadContacts();
                var installer = all.SelectMany(g => g.GetComponentsInChildren<GameInstaller>(true)).Single();
                typeof(GameInstaller).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(installer, null);

                Assert.That(installer.Session.Stats.Board, Is.EqualTo(start));
                Assert.That(book.ContactCount, Is.EqualTo(expected.Entries.Count), "closed jacks are still listed");
                BookEntry closed = expected.Entries.FirstOrDefault(e => !start.Contains(e.Jack));
                if (closed != null)
                    Assert.That(book.HasLetter(closed.Name[0]), Is.True, $"{closed.Name} on the closed jack {closed.Jack}");
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
