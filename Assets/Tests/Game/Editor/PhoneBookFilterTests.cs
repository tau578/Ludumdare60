using NUnit.Framework;
using Switchboard.Domain;
using Switchboard.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Switchboard.Game.Tests
{
    // The book shows only the people on open jacks (D42), with the real people file.
    public sealed class PhoneBookFilterTests
    {
        private const string PeopleFilePath = "Assets/Configs/PhoneBook.json";
        private GameObject root;

        [TearDown]
        public void TearDown()
        {
            if (root != null) Object.DestroyImmediate(root);
        }

        private PhoneBookView Book()
        {
            var file = AssetDatabase.LoadAssetAtPath<TextAsset>(PeopleFilePath);
            Assert.That(file, Is.Not.Null, PeopleFilePath);
            root = new GameObject("PhoneBook", typeof(RectTransform));
            var view = root.AddComponent<PhoneBookView>();
            view.ConfigureDirectory(file, Page("Left"), Page("Right"), null, null);
            view.ReloadContacts();
            return view;
        }

        private PhoneBookPageView Page(string name)
        {
            var page = new GameObject(name, typeof(RectTransform));
            page.transform.SetParent(root.transform, false);
            var entries = new GameObject("Entries", typeof(RectTransform)).GetComponent<RectTransform>();
            entries.SetParent(page.transform, false);
            entries.sizeDelta = new Vector2(326, 326);
            var template = new GameObject("ContactTemplate", typeof(RectTransform));
            template.transform.SetParent(entries, false);
            var row = template.AddComponent<PhoneBookContactRow>();
            row.Configure(Label("Name", template.transform), Label("Socket", template.transform), template.AddComponent<Image>());
            template.SetActive(false);
            var view = page.AddComponent<PhoneBookPageView>();
            view.Configure(entries, row, Label("PageNumber", page.transform), Label("EmptyMessage", page.transform));
            return view;
        }

        private static Text Label(string name, Transform parent)
        {
            var label = new GameObject(name, typeof(RectTransform)).AddComponent<Text>();
            label.transform.SetParent(parent, false);
            return label;
        }

        private static System.Predicate<string> Open(int side) => socket => new BoardSize(side, side).Contains(socket);

        // D50: the book lists the real file exactly as written, so the designer's surname order is what players see.
        [Test]
        public void TheBookKeepsTheRealFileNamesAndOrder()
        {
            var file = AssetDatabase.LoadAssetAtPath<TextAsset>(PeopleFilePath);
            Assert.That(file, Is.Not.Null, PeopleFilePath);
            PhoneBookConfig raw = JsonUtility.FromJson<PhoneBookConfig>(file.text);
            PhoneBookView book = Book();

            Assert.That(book.ContactCount, Is.EqualTo(raw.contacts.Length));
            for (int i = 0; i < raw.contacts.Length; i++)
                Assert.That(book.ContactAt(i).name, Is.EqualTo(raw.contacts[i].name.Trim()), $"entry {i + 1}");
            Assert.That(book.ContactAt(raw.contacts.Length - 1).name, Does.Contain(", "), "people stay \"Surname, First\"");
        }

        [Test]
        public void AThreeByThreeBoardShowsTheStartingNineOnTwoPages()
        {
            PhoneBookView book = Book();
            Assert.That(book.ContactCount, Is.EqualTo(36));

            book.SetVisibleSockets(Open(3));

            Assert.That(book.ContactCount, Is.EqualTo(9));
            Assert.That(book.PageCount, Is.EqualTo(2));
            string[] names =
            {
                "Dunmore, Mabel", "Godfrey, Irene", "Hollis, Arthur", "Moss, Ada", "Quigley, Bernard", "Rook, Gertrude",
                "Stanhope, Mildred", "Underhill, Reginald", "Yardley, Lillian"
            };
            for (int i = 0; i < names.Length; i++) Assert.That(book.ContactAt(i).name, Is.EqualTo(names[i]), "surname order, no services");
            Assert.That(book.CanGoForward, Is.False, "two pages make one spread");
            Assert.That(root.transform.Find("Right/Entries/Contact_03").gameObject.activeSelf, Is.True);
            Assert.That(root.transform.Find("Right/Entries/Contact_04").gameObject.activeSelf, Is.False, "the second page has 3 rows");
            Assert.That(book.HasLetter('M'), Is.True);
            Assert.That(book.HasLetter('V'), Is.False, "Vickery is on C4, still closed");
        }

        [Test]
        public void BiggerBoardsAddPeopleWithTheServicesFirst()
        {
            PhoneBookView book = Book();
            book.SetVisibleSockets(Open(4));
            Assert.That(book.ContactCount, Is.EqualTo(16));
            Assert.That(book.ContactAt(0).name, Is.EqualTo("Doctor"));
            Assert.That(book.HasLetter('V'), Is.True);

            book.SetVisibleSockets(Open(5));
            Assert.That(book.ContactCount, Is.EqualTo(25));
            Assert.That(book.ContactAt(0).name, Is.EqualTo("Fire Brigade"));
            Assert.That(book.ContactAt(1).name, Is.EqualTo("Doctor"));

            book.SetVisibleSockets(Open(6));
            Assert.That(book.ContactCount, Is.EqualTo(36));
            Assert.That(book.ContactAt(0).name, Is.EqualTo("Police"));
            book.SetVisibleSockets(null);
            Assert.That(book.ContactCount, Is.EqualTo(36));
        }

        [Test]
        public void ShrinkingTheFilterBringsTheBookBackToAShownSpread()
        {
            PhoneBookView book = Book();
            book.NextSpread();
            book.NextSpread();
            Assert.That(book.CurrentSpreadIndex, Is.EqualTo(2));
            book.SetVisibleSockets(Open(3));
            Assert.That(book.CurrentSpreadIndex, Is.EqualTo(0));
            Assert.That(book.CanGoBack, Is.False);
        }
    }
}
