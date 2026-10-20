using System.Collections.Generic;
using Switchboard.Content;
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
        [SerializeField] private Button shortcutButton;
        [Header("Directory")]
        [SerializeField] private TextAsset contactsConfig;
        [SerializeField] private PhoneBookPageView leftPage;
        [SerializeField] private PhoneBookPageView rightPage;
        [SerializeField] private Button previousSpread;
        [SerializeField] private Button nextSpread;
        [SerializeField] private PhoneBookTab[] tabs = new PhoneBookTab[0];
        private BookCatalog directory = BookCatalog.Empty;
        private string pageMessage;
        private PhoneBookContactRow hoveredRow;

        public bool IsOpen { get; private set; }
        /// <summary>Off while the game is paused (D56): keys and clicks leave the book as it is.</summary>
        private bool inputEnabled = true;
        public bool InputEnabled
        {
            get => inputEnabled;
            set
            {
                inputEnabled = value;
                if (shortcutButton != null) shortcutButton.interactable = value;
            }
        }
        /// <summary>Off when the pause menu owns Esc: it closes an open book before it pauses (D56).</summary>
        public bool ClosesOnEscape { get; set; } = true;
        /// <summary>The tutorial's other steps (D57): the player can't open the book (Tab or a click); closing still works.</summary>
        public bool OpenLocked { get; set; }
        public event System.Action<bool> OpenChanged;
        /// <summary>A row was clicked (D58): its jack, to mark on the board. The book closes in the same click.</summary>
        public event System.Action<string> JackPicked;
        /// <summary>Whether a row's jack can be marked (the open board); null: every row. Other rows ignore clicks.</summary>
        public System.Func<string, bool> CanPickJack { get; set; }
        public Graphic ClosedBookGraphic => closedBook != null ? closedBook.targetGraphic : null;
        public RectTransform OpenBookRect => openBook != null ? (RectTransform)openBook.transform : null;
        public int CurrentSpreadIndex { get; private set; }
        /// <summary>Every entry of Book.json the loader kept, whatever the board size (D53).</summary>
        public int ContactCount => directory.Entries.Count;
        public int PageCount => Mathf.Max(1, Mathf.CeilToInt(ContactCount / (float)directory.ContactsPerPage));
        public BookEntry ContactAt(int index) => directory.Entries[index];
        /// <summary>The row with the hover tint: under the pointer, on the open book, and its jack can be marked.</summary>
        public PhoneBookContactRow HoveredRow => hoveredRow;
        public bool CanGoBack => CurrentSpreadIndex > 0;
        public bool CanGoForward => (CurrentSpreadIndex + 1) * 2 * directory.ContactsPerPage < ContactCount;
        private int ContactsPerSpread => 2 * directory.ContactsPerPage;

        public void ConfigureDirectory(TextAsset config, PhoneBookPageView left, PhoneBookPageView right, Button previous, Button next)
        {
            contactsConfig = config;
            leftPage = left;
            rightPage = right;
            previousSpread = previous;
            nextSpread = next;
            ConfigurePageInput();
        }

        public void ConfigureTabs(PhoneBookTab[] letterTabs) => tabs = letterTabs;

        public bool HasLetter(char letter) => FirstContactWithLetter(letter) >= 0;

        /// <summary>
        /// Opens the spread with the first name starting with the letter. Names are "SURNAME, First" and sorted, so this
        /// is the surname; a service goes by its whole name.
        /// </summary>
        public bool ShowLetter(char letter)
        {
            int index = FirstContactWithLetter(letter);
            if (index < 0) return false;
            CurrentSpreadIndex = index / ContactsPerSpread;
            RefreshPages();
            return true;
        }

        private int FirstContactWithLetter(char letter)
        {
            letter = char.ToUpperInvariant(letter);
            IReadOnlyList<BookEntry> entries = directory.Entries;
            for (int i = 0; i < entries.Count; i++)
                if (char.ToUpperInvariant(entries[i].Name[0]) == letter) return i;
            return -1;
        }

        [ContextMenu("Reload Contacts")]
        public void ReloadContacts()
        {
            // GameInstaller logs the file's messages once at start; an empty book shows its first one on the page.
            directory = BookFileLoader.Load(contactsConfig != null ? contactsConfig.text : null);
            pageMessage = directory.Entries.Count == 0 && directory.Errors.Count > 0 ? directory.Errors[0] : null;
            CurrentSpreadIndex = 0;
            RefreshPages();
        }

        /// <summary>The open spread's row for the jack; null while closed or when the entry is on another spread.</summary>
        public RectTransform RowOnSpread(string jack)
        {
            if (!IsOpen) return null;
            RectTransform row = leftPage != null ? leftPage.RowWithJack(jack) : null;
            if (row == null && rightPage != null) row = rightPage.RowWithJack(jack);
            return row;
        }

        /// <summary>Which way to turn to the jack's first entry: -1 back, 1 forward, 0 here or not in the book.</summary>
        public int SpreadDirectionTo(string jack)
        {
            IReadOnlyList<BookEntry> entries = directory.Entries;
            for (int i = 0; i < entries.Count; i++)
                if (entries[i].Jack == jack) return System.Math.Sign(i / ContactsPerSpread - CurrentSpreadIndex);
            return 0;
        }

        /// <summary>The corner that turns the page that way, while it can.</summary>
        public RectTransform PageCorner(int direction)
        {
            Button corner = direction < 0 && CanGoBack ? previousSpread : direction > 0 && CanGoForward ? nextSpread : null;
            return corner != null ? (RectTransform)corner.transform : null;
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
            // A turned page shows other entries in the same rows; the next hover finds the right one.
            ClearHover();
            if (leftPage != null) leftPage.Render(directory, CurrentSpreadIndex * 2, pageMessage);
            if (rightPage != null) rightPage.Render(directory, CurrentSpreadIndex * 2 + 1);
            if (previousSpread != null) previousSpread.gameObject.SetActive(CanGoBack);
            if (nextSpread != null) nextSpread.gameObject.SetActive(CanGoForward);
            foreach (var tab in tabs)
            {
                if (tab == null) continue;
                int index = FirstContactWithLetter(tab.Letter);
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
            // Only the closed cover highlights on hover; the open pages are for reading.
            if (closedBook != null && closedBook.targetGraphic != null &&
                closedBook.targetGraphic.GetComponent<BookHoverHighlight>() == null)
                closedBook.targetGraphic.gameObject.AddComponent<BookHoverHighlight>();
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
            if (!IsOpen || !InputEnabled) return 0;
            if (CanGoBack && previousSpread != null && RectTransformUtility.RectangleContainsScreenPoint(
                    (RectTransform)previousSpread.transform, position, camera)) return -1;
            if (CanGoForward && nextSpread != null && RectTransformUtility.RectangleContainsScreenPoint(
                    (RectTransform)nextSpread.transform, position, camera)) return 1;
            return 0;
        }

        /// <summary>The open spread's row under a screen point (the page corners come first), or null.</summary>
        public PhoneBookContactRow RowAt(Vector2 position, Camera camera)
        {
            if (!IsOpen || PageTurnDirectionAt(position, camera) != 0) return null;
            PhoneBookContactRow row = leftPage != null ? leftPage.RowAt(position, camera) : null;
            return row != null ? row : rightPage != null ? rightPage.RowAt(position, camera) : null;
        }

        /// <summary>
        /// Tints the row under the pointer (10b fix 1), found by position like a click; the cord input calls it with
        /// its top hit's position each frame. Nothing shows while the book is shut or paused, or for a row whose jack
        /// is off the open board (a click there does nothing either).
        /// </summary>
        public void HoverAt(Vector2 position, Camera camera)
        {
            PhoneBookContactRow row = IsOpen && InputEnabled ? RowAt(position, camera) : null;
            if (row != null && CanPickJack != null && !CanPickJack(row.Jack)) row = null;
            SetHoveredRow(row);
        }

        public void ClearHover() => SetHoveredRow(null);

        private void SetHoveredRow(PhoneBookContactRow row)
        {
            if (hoveredRow == row) return;
            if (hoveredRow != null) hoveredRow.SetHovered(false);
            hoveredRow = row;
            if (hoveredRow != null) hoveredRow.SetHovered(true);
        }

        public void OnPointerDown(PointerEventData eventData) { }

        // Like the page corners, rows are found by position: the open book's art is the only raycast target (D58).
        public void OnPointerClick(PointerEventData eventData)
        {
            if (!IsOpen || !InputEnabled || openBook == null || eventData.button != PointerEventData.InputButton.Left) return;
            int direction = PageTurnDirectionAt(eventData.position, eventData.pressEventCamera);
            if (direction < 0) { PreviousSpread(); return; }
            if (direction > 0) { NextSpread(); return; }
            PhoneBookContactRow row = RowAt(eventData.position, eventData.pressEventCamera);
            if (row != null)
            {
                // A jack outside the open board can't be marked: the click does nothing and the book stays open.
                if (CanPickJack != null && !CanPickJack(row.Jack)) return;
                string jack = row.Jack;
                SetOpen(false);
                JackPicked?.Invoke(jack);
                return;
            }
            if (RectTransformUtility.RectangleContainsScreenPoint((RectTransform)openBook.transform,
                    eventData.position, eventData.pressEventCamera)) SetOpen(false);
        }

        private void Awake()
        {
            SwitchboardUISorting.SetLayer(gameObject, SwitchboardUISorting.Book);
            closedBook.onClick.AddListener(Toggle);
            if (shortcutButton != null) shortcutButton.onClick.AddListener(Toggle);
            ConfigurePageInput();
            foreach (var tab in tabs) if (tab != null) tab.Bind(this);
            ReloadContacts();
            SetOpen(false);
        }

        private void OnDestroy()
        {
            if (closedBook != null) closedBook.onClick.RemoveListener(Toggle);
            if (shortcutButton != null) shortcutButton.onClick.RemoveListener(Toggle);
            foreach (var tab in tabs) if (tab != null) tab.Unbind();
        }

        private void Update()
        {
            if (!InputEnabled) return;
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.tabKey.wasPressedThisFrame && !IsEditingText())
            {
                Toggle();
                return;
            }
            if (!IsOpen) return;
            if (keyboard != null)
            {
                if (ClosesOnEscape && keyboard.escapeKey.wasPressedThisFrame) { SetOpen(false); return; }
                if (keyboard.leftArrowKey.wasPressedThisFrame) PreviousSpread();
                else if (keyboard.rightArrowKey.wasPressedThisFrame) NextSpread();
            }
        }

        private static bool IsEditingText()
        {
            var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            return selected != null && (selected.GetComponent<InputField>() != null ||
                selected.GetComponent<TMPro.TMP_InputField>() != null);
        }

        public void Toggle()
        {
            if (InputEnabled && (IsOpen || !OpenLocked)) SetOpen(!IsOpen);
        }

        public void SetOpen(bool value)
        {
            bool changed = IsOpen != value;
            IsOpen = value;
            if (!value) ClearHover();
            closedBook.gameObject.SetActive(!value);
            openBook.gameObject.SetActive(value);
            if (changed) OpenChanged?.Invoke(value);
        }
    }
}
