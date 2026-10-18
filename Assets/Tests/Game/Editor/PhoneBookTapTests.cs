using NUnit.Framework;
using Switchboard.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Switchboard.Game.Tests
{
    // The Notepad's hook in the team's book (D23, D44): rows raise taps only while taps are on.
    public sealed class PhoneBookTapTests
    {
        private GameObject root;
        private TextAsset file;

        [TearDown]
        public void TearDown()
        {
            if (root != null) Object.DestroyImmediate(root);
            if (file != null) Object.DestroyImmediate(file);
        }

        private PhoneBookView Book(bool tapsBeforeRows = false)
        {
            file = new TextAsset("{ \"contactsPerPage\": 2, \"contacts\": [ " +
                "{ \"name\": \"Ada Moss\", \"socket\": \"C3\" }, { \"name\": \"Basil Thorne\", \"socket\": \"A5\" }, " +
                "{ \"name\": \"Clara Whitlock\", \"socket\": \"f6\" } ] }");
            root = new GameObject("PhoneBook", typeof(RectTransform));
            var view = root.AddComponent<PhoneBookView>();
            view.ConfigureDirectory(file, Page("Left"), Page("Right"), null, null);
            view.ContactTapsEnabled = tapsBeforeRows;
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
            var tapArea = template.AddComponent<Image>();
            var row = template.AddComponent<PhoneBookContactRow>();
            row.Configure(Label("Name", template.transform), Label("Socket", template.transform), tapArea);
            template.SetActive(false);
            var view = page.AddComponent<PhoneBookPageView>();
            view.Configure(entries, row, Label("PageNumber", page.transform), Label("EmptyMessage", page.transform));
            return view;
        }

        private static Text Label(string name, Transform parent)
        {
            var label = new GameObject(name, typeof(RectTransform)).AddComponent<Text>();
            label.transform.SetParent(parent, false);
            label.raycastTarget = false;
            return label;
        }

        private PhoneBookContactRow Row(string page, int index) =>
            root.transform.Find($"{page}/Entries/Contact_{index:00}").GetComponent<PhoneBookContactRow>();

        private static void Click(Component row) =>
            ExecuteEvents.Execute(row.gameObject, new PointerEventData(null) { button = PointerEventData.InputButton.Left },
                ExecuteEvents.pointerClickHandler);

        [Test]
        public void RaisesTheTappedNameAndSocketWhileTapsAreOn()
        {
            PhoneBookView view = Book();
            string name = null, socket = null;
            int taps = 0;
            view.ContactTapped += (n, s) => { name = n; socket = s; taps++; };
            view.ContactTapsEnabled = true;

            Click(Row("Left", 2));
            Assert.That(taps, Is.EqualTo(1));
            Assert.That(name, Is.EqualTo("Basil Thorne"));
            Assert.That(socket, Is.EqualTo("A5"));

            Click(Row("Right", 1));
            Assert.That(name, Is.EqualTo("Clara Whitlock"));
            Assert.That(socket, Is.EqualTo("F6"), "the book's own trimmed, upper-case socket");
        }

        [Test]
        public void WithTapsOffTheHitAreaIsNoRaycastTargetAndClicksRaiseNothing()
        {
            PhoneBookView view = Book();
            int taps = 0;
            view.ContactTapped += (n, s) => taps++;

            PhoneBookContactRow row = Row("Left", 1);
            Assert.That(view.ContactTapsEnabled, Is.False);
            Assert.That(row.GetComponent<Image>().raycastTarget, Is.False);
            Click(row);
            Assert.That(taps, Is.EqualTo(0));

            view.ContactTapsEnabled = true;
            Assert.That(row.GetComponent<Image>().raycastTarget, Is.True);
            view.ContactTapsEnabled = false;
            Assert.That(row.GetComponent<Image>().raycastTarget, Is.False);
        }

        [Test]
        public void RowsMadeAfterTapsAreOnAreTappableToo()
        {
            PhoneBookView view = Book(tapsBeforeRows: true);
            Assert.That(Row("Left", 1).IsTappable, Is.True);
            Assert.That(Row("Left", 2).IsTappable, Is.True);
            Assert.That(Row("Right", 1).IsTappable, Is.True);
        }
    }
}
