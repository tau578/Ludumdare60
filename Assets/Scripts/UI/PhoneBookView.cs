using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Switchboard.UI
{
    [DisallowMultipleComponent, RequireComponent(typeof(Canvas), typeof(GraphicRaycaster))]
    public sealed class PhoneBookView : MonoBehaviour
    {
        [SerializeField] private Button closedBook;
        [SerializeField] private Button openBook;
        [Header("Directory")]
        [SerializeField] private TextAsset contactsConfig;
        [SerializeField] private PhoneBookPageView leftPage;
        [SerializeField] private PhoneBookPageView rightPage;
        [SerializeField] private Button previousSpread;
        [SerializeField] private Button nextSpread;
        [SerializeField] private PhoneBookTab[] tabs = new PhoneBookTab[0];
        private PhoneBookConfig allContacts = new PhoneBookConfig();
        private PhoneBookConfig directory = new PhoneBookConfig();
        private Predicate<string> isVisibleSocket;
        private string configError;
        private bool contactTapsEnabled;

        /// <summary>A tapped entry's name and socket, raised only while taps are on.</summary>
        public event Action<string, string> ContactTapped;

        /// <summary>Off by default: a tap on an entry then falls through to the open book and closes it.</summary>
        public bool ContactTapsEnabled
        {
            get => contactTapsEnabled;
            set
            {
                contactTapsEnabled = value;
                if (leftPage != null) leftPage.SetTappable(value);
                if (rightPage != null) rightPage.SetTappable(value);
            }
        }

        public bool IsOpen { get; private set; }
        public event System.Action<bool> OpenChanged;
        public Graphic ClosedBookGraphic => closedBook != null ? closedBook.targetGraphic : null;
        public int CurrentSpreadIndex { get; private set; }
        /// <summary>The contacts the book shows, after the socket filter.</summary>
        public int ContactCount => directory.contacts.Length;
        public int PageCount => Mathf.Max(1, Mathf.CeilToInt(ContactCount / (float)directory.contactsPerPage));
        public PhoneBookContact ContactAt(int index) => directory.contacts[index];
        public bool CanGoBack => CurrentSpreadIndex > 0;
        public bool CanGoForward => (CurrentSpreadIndex + 1) * 2 * directory.contactsPerPage < ContactCount;
        private int ContactsPerSpread => 2 * directory.contactsPerPage;

        public void ConfigureDirectory(TextAsset config, PhoneBookPageView left, PhoneBookPageView right, Button previous, Button next)
        {
            contactsConfig = config;
            leftPage = left;
            rightPage = right;
            previousSpread = previous;
            nextSpread = next;
            ListenToPages();
        }

        private void ListenToPages()
        {
            foreach (var page in new[] { leftPage, rightPage })
            {
                if (page == null) continue;
                page.ContactTapped -= OnContactTapped;
                page.ContactTapped += OnContactTapped;
                page.SetTappable(contactTapsEnabled);
            }
        }

        private void OnContactTapped(PhoneBookContact contact) => ContactTapped?.Invoke(contact.name, contact.socket);

        public void ConfigureTabs(PhoneBookTab[] letterTabs) => tabs = letterTabs;

        /// <summary>
        /// Shows only the contacts whose socket passes the filter (D42: the open board), in display order, so the
        /// services stay first and the given-name order holds. Null shows everyone. Pages and tabs follow the filter.
        /// </summary>
        public void SetVisibleSockets(Predicate<string> isVisible)
        {
            isVisibleSocket = isVisible;
            ApplyFilter();
            int lastSpread = Mathf.Max(0, (ContactCount - 1) / ContactsPerSpread);
            if (CurrentSpreadIndex > lastSpread) CurrentSpreadIndex = lastSpread;
            RefreshPages();
        }

        private void ApplyFilter()
        {
            if (isVisibleSocket == null)
            {
                directory = allContacts;
                return;
            }
            var visible = new System.Collections.Generic.List<PhoneBookContact>(allContacts.contacts.Length);
            foreach (PhoneBookContact contact in allContacts.contacts)
                if (isVisibleSocket(contact.socket)) visible.Add(contact);
            directory = new PhoneBookConfig { contactsPerPage = allContacts.contactsPerPage, contacts = visible.ToArray() };
        }

        public bool HasLetter(char letter) => FirstContactWithName(letter) >= 0;

        /// <summary>Opens the spread with the first given name starting with the letter; people are sorted by given name.</summary>
        public bool ShowLetter(char letter)
        {
            int index = FirstContactWithName(letter);
            if (index < 0) return false;
            CurrentSpreadIndex = index / ContactsPerSpread;
            RefreshPages();
            return true;
        }

        private int FirstContactWithName(char letter)
        {
            letter = char.ToUpperInvariant(letter);
            for (int i = 0; i < directory.contacts.Length; i++)
            {
                var contact = directory.contacts[i];
                string name = contact.name;
                bool service = contact.tags != null && System.Array.IndexOf(contact.tags, "emergency-service") >= 0;
                if (!service && char.ToUpperInvariant(name[0]) == letter) return i;
            }
            return -1;
        }

        [ContextMenu("Reload Contacts")]
        public void ReloadContacts()
        {
            allContacts = new PhoneBookConfig();
            configError = null;
            try
            {
                if (contactsConfig == null) throw new System.ArgumentException("Phone book config is not assigned.");
                allContacts = PhoneBookConfig.Parse(contactsConfig.text);
            }
            catch (System.ArgumentException ex)
            {
                configError = "Configuration error";
                Debug.LogWarning($"Phone book: {ex.Message}", this);
            }
            ApplyFilter();
            CurrentSpreadIndex = 0;
            RefreshPages();
        }

        public void NextSpread()
        {
            if (!CanGoForward) return;
            CurrentSpreadIndex++;
            RefreshPages();
        }

        public void PreviousSpread()
        {
            if (!CanGoBack) return;
            CurrentSpreadIndex--;
            RefreshPages();
        }

        private void RefreshPages()
        {
            if (leftPage != null) leftPage.Render(directory, CurrentSpreadIndex * 2, configError);
            if (rightPage != null) rightPage.Render(directory, CurrentSpreadIndex * 2 + 1);
            if (previousSpread != null) previousSpread.gameObject.SetActive(CanGoBack);
            if (nextSpread != null) nextSpread.gameObject.SetActive(CanGoForward);
            foreach (var tab in tabs)
            {
                if (tab == null) continue;
                int index = FirstContactWithName(tab.Letter);
                tab.Show(index >= 0, index >= 0 && index / ContactsPerSpread == CurrentSpreadIndex);
            }
        }

        public void Configure(Button closed, Button opened)
        {
            closedBook = closed;
            openBook = opened;
            SetOpen(false);
        }

        private void Awake()
        {
            SwitchboardUISorting.SetLayer(gameObject, SwitchboardUISorting.Book);
            closedBook.onClick.AddListener(Toggle);
            openBook.onClick.AddListener(Toggle);
            if (previousSpread != null) previousSpread.onClick.AddListener(PreviousSpread);
            if (nextSpread != null) nextSpread.onClick.AddListener(NextSpread);
            foreach (var tab in tabs) if (tab != null) tab.Bind(this);
            ListenToPages();
            ReloadContacts();
            SetOpen(false);
        }

        private void OnDestroy()
        {
            if (closedBook != null) closedBook.onClick.RemoveListener(Toggle);
            if (openBook != null) openBook.onClick.RemoveListener(Toggle);
            if (previousSpread != null) previousSpread.onClick.RemoveListener(PreviousSpread);
            if (nextSpread != null) nextSpread.onClick.RemoveListener(NextSpread);
            foreach (var tab in tabs) if (tab != null) tab.Unbind();
            if (leftPage != null) leftPage.ContactTapped -= OnContactTapped;
            if (rightPage != null) rightPage.ContactTapped -= OnContactTapped;
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (!IsOpen || keyboard == null) return;
            if (keyboard.leftArrowKey.wasPressedThisFrame) PreviousSpread();
            else if (keyboard.rightArrowKey.wasPressedThisFrame) NextSpread();
        }

        public void Toggle() => SetOpen(!IsOpen);

        public void SetOpen(bool value)
        {
            bool changed = IsOpen != value;
            IsOpen = value;
            closedBook.gameObject.SetActive(!value);
            openBook.gameObject.SetActive(value);
            if (changed) OpenChanged?.Invoke(value);
        }
    }
}
