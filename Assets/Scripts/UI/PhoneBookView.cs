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
        private readonly List<RaycastResult> pointerHits = new List<RaycastResult>();

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
        public event System.Action<bool> OpenChanged;
        public Graphic ClosedBookGraphic => closedBook != null ? closedBook.targetGraphic : null;
        public int CurrentSpreadIndex { get; private set; }
        /// <summary>Every entry of Book.json the loader kept, whatever the board size (D53).</summary>
        public int ContactCount => directory.Entries.Count;
        public int PageCount => Mathf.Max(1, Mathf.CeilToInt(ContactCount / (float)directory.ContactsPerPage));
        public BookEntry ContactAt(int index) => directory.Entries[index];
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
            if (!IsOpen || openBook == null) return 0;
            var rect = (RectTransform)openBook.transform;
            if (!RectTransformUtility.RectangleContainsScreenPoint(rect, position, camera) ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, position, camera, out var local)) return 0;
            return local.x < rect.rect.center.x ? (CanGoBack ? -1 : 0) : (CanGoForward ? 1 : 0);
        }

        public void OnPointerDown(PointerEventData eventData) { }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!InputEnabled) return;
            if (eventData.button != PointerEventData.InputButton.Left) return;
            int direction = PageTurnDirectionAt(eventData.position, eventData.pressEventCamera);
            if (direction < 0) PreviousSpread();
            else if (direction > 0) NextSpread();
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
            var pointer = Pointer.current;
            if (pointer == null || !pointer.press.wasPressedThisFrame || EventSystem.current == null) return;
            pointerHits.Clear();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current)
                { position = pointer.position.ReadValue() }, pointerHits);
            if (pointerHits.Count == 0 || !pointerHits[0].gameObject.transform.IsChildOf(transform)) SetOpen(false);
        }

        private static bool IsEditingText()
        {
            var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            return selected != null && (selected.GetComponent<InputField>() != null ||
                selected.GetComponent<TMPro.TMP_InputField>() != null);
        }

        public void Toggle()
        {
            if (InputEnabled) SetOpen(!IsOpen);
        }

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
