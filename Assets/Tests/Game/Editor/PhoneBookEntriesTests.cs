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

        [Test]
        public void TheRealBookShowsEveryEntryInTheLoadersOrder()
        {
            BookCatalog expected = AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath).LoadBook();
            PhoneBookView book = RealBook();

            Assert.That(book.ContactCount, Is.EqualTo(expected.Entries.Count));
            Assert.That(book.ContactCount, Is.EqualTo(39));
            for (int i = 0; i < expected.Entries.Count; i++)
            {
                Assert.That(book.ContactAt(i).Name, Is.EqualTo(expected.Entries[i].Name), $"entry {i + 1}");
                Assert.That(book.ContactAt(i).Jack, Is.EqualTo(expected.Entries[i].Jack), $"entry {i + 1}");
            }
            Assert.That(book.ContactAt(0).Name, Is.EqualTo("Police"));
            Assert.That(book.PageCount, Is.EqualTo(7), "39 entries, 6 per page");
        }

        [Test]
        public void ARowShowsTheNameThenProfessionAndAddressAndTheJackLast()
        {
            RealBook();

            Assert.That(Label("Left", 1, "Name").text, Is.EqualTo("Police"));
            Assert.That(Label("Left", 1, "Details").text, Is.EqualTo("Police station, High St. 1"));
            Assert.That(Label("Left", 1, "Jack").text, Is.EqualTo("B6"));
            Assert.That(Label("Left", 4, "Name").text, Is.EqualTo("ABBOTT, Florence"));
            Assert.That(Label("Left", 4, "Jack").text, Is.EqualTo("F6"));
            Assert.That(Label("Right", 1, "Name").text, Is.EqualTo("COBBOLD, Doris"));
            Assert.That(Label("Right", 1, "Details").text, Is.EqualTo("Baker, Bridge St. 5"));
            Assert.That(Label("Right", 1, "Jack").text, Is.EqualTo("D1"));
            Assert.That(Page("Left").Find("JackHeading").GetComponent<Text>().text, Is.EqualTo("Jack"));
            var jack = Label("Left", 1, "Jack").rectTransform;
            var name = Label("Left", 1, "Name").rectTransform;
            Assert.That(jack.anchorMin.x, Is.GreaterThanOrEqualTo(name.anchorMax.x), "the jack is the last column");
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
        public void AFreshGameOnTheThreeByThreeBoardListsEveryone()
        {
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

                Assert.That(installer.Session.Stats.Board, Is.EqualTo(new BoardSize(3, 3)));
                Assert.That(book.ContactCount, Is.EqualTo(39), "closed jacks are still listed");
                Assert.That(book.HasLetter('Z'), Is.True, "ZOUCH, Albert on F5");
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
