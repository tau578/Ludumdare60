using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Switchboard.UI
{
    [DisallowMultipleComponent, RequireComponent(typeof(Canvas), typeof(GraphicRaycaster))]
    public sealed class PhoneBookView : MonoBehaviour, IPointerDownHandler, IPointerClickHandler
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
        private PhoneBookConfig directory = new PhoneBookConfig();
        private string configError;
        private bool contactTapsEnabled;
        private readonly List<RaycastResult> pointerHits = new List<RaycastResult>();

        /// <summary>A tapped entry's name and socket, raised only while taps are on.</summary>
        public event Action<string, string> ContactTapped;

        /// <summary>Off by default: entry taps fall through to page turning.</summary>
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
        public int ContactCount => directory.contacts.Length;
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
            ConfigurePageInput();
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

        public bool HasLetter(char letter) => FirstContactWithSurname(letter) >= 0;

        /// <summary>Opens the spread with the first surname starting with the letter; the contacts are sorted by surname.</summary>
        public bool ShowLetter(char letter)
        {
            int index = FirstContactWithSurname(letter);
            if (index < 0) return false;
            CurrentSpreadIndex = index / ContactsPerSpread;
            RefreshPages();
            return true;
        }

        private int FirstContactWithSurname(char letter)
        {
            letter = char.ToUpperInvariant(letter);
            for (int i = 0; i < directory.contacts.Length; i++)
            {
                var contact = directory.contacts[i];
                string name = contact.name;
                bool service = contact.tags != null && System.Array.IndexOf(contact.tags, "emergency-service") >= 0;
                int initial = service || name.IndexOf(',') >= 0 ? 0 : name.LastIndexOf(' ') + 1;
                if (char.ToUpperInvariant(name[initial]) == letter) return i;
            }
            return -1;
        }

        [ContextMenu("Reload Contacts")]
        public void ReloadContacts()
        {
            directory = new PhoneBookConfig();
            configError = null;
            try
            {
                if (contactsConfig == null) throw new System.ArgumentException("Phone book config is not assigned.");
                directory = PhoneBookConfig.Parse(contactsConfig.text);
            }
            catch (System.ArgumentException ex)
            {
                configError = "Configuration error";
                Debug.LogWarning($"Phone book: {ex.Message}", this);
            }
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
                int index = FirstContactWithSurname(tab.Letter);
                tab.Show(index >= 0, index >= 0 && index / ContactsPerSpread == CurrentSpreadIndex);
            }
        }

        public void Configure(Button closed, Button opened)
        {
            closedBook = closed;
            openBook = opened;
            ConfigurePageInput();
            SetOpen(false);
        }

        private void ConfigurePageInput()
        {
            // Keep the artwork as the hit area; clicks bubble to this book instead of its old close button.
            if (openBook != null)
            {
                openBook.transition = Selectable.Transition.None;
                openBook.enabled = false;
                openBook.targetGraphic.canvasRenderer.SetColor(Color.white);
            }
            foreach (var corner in new[] { previousSpread, nextSpread })
            {
                if (corner == null) continue;
                corner.transition = Selectable.Transition.None;
                corner.enabled = false;
                corner.targetGraphic.raycastTarget = false;
                corner.targetGraphic.canvasRenderer.SetColor(Color.white);
            }
        }

        public int PageTurnDirectionAt(Vector2 position, Camera camera)
        {
            if (!IsOpen || openBook == null) return 0;
            var rect = (RectTransform)openBook.transform;
            if (!RectTransformUtility.RectangleContainsScreenPoint(rect, position, camera) ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, position, camera, out var local)) return 0;
            return local.x < rect.rect.center.x ? (CanGoBack ? -1 : 0) : (CanGoForward ? 1 : 0);
        }

        public void OnPointerDown(PointerEventData eventData) { }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            int direction = PageTurnDirectionAt(eventData.position, eventData.pressEventCamera);
            if (direction < 0) PreviousSpread();
            else if (direction > 0) NextSpread();
        }

        private void Awake()
        {
            SwitchboardUISorting.SetLayer(gameObject, SwitchboardUISorting.Book);
            closedBook.onClick.AddListener(Toggle);
            ConfigurePageInput();
            foreach (var tab in tabs) if (tab != null) tab.Bind(this);
            ListenToPages();
            ReloadContacts();
            SetOpen(false);
        }

        private void OnDestroy()
        {
            if (closedBook != null) closedBook.onClick.RemoveListener(Toggle);
            foreach (var tab in tabs) if (tab != null) tab.Unbind();
            if (leftPage != null) leftPage.ContactTapped -= OnContactTapped;
            if (rightPage != null) rightPage.ContactTapped -= OnContactTapped;
        }

        private void Update()
        {
            if (!IsOpen) return;
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.escapeKey.wasPressedThisFrame) { SetOpen(false); return; }
                if (keyboard.leftArrowKey.wasPressedThisFrame) PreviousSpread();
                else if (keyboard.rightArrowKey.wasPressedThisFrame) NextSpread();
            }
            var pointer = Pointer.current;
            if (pointer == null || !pointer.press.wasPressedThisFrame || EventSystem.current == null) return;
            pointerHits.Clear();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current)
                { position = pointer.position.ReadValue() }, pointerHits);
            if (pointerHits.Count == 0 || !pointerHits[0].gameObject.transform.IsChildOf(transform)) SetOpen(false);
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
