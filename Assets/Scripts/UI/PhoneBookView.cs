using UnityEngine;
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
        private PhoneBookConfig directory = new PhoneBookConfig();
        private string configError;

        public bool IsOpen { get; private set; }
        public int CurrentSpreadIndex { get; private set; }
        public int ContactCount => directory.contacts.Length;
        public bool CanGoBack => CurrentSpreadIndex > 0;
        public bool CanGoForward => (CurrentSpreadIndex + 1) * 2 * directory.contactsPerPage < ContactCount;

        public void ConfigureDirectory(TextAsset config, PhoneBookPageView left, PhoneBookPageView right, Button previous, Button next)
        {
            contactsConfig = config;
            leftPage = left;
            rightPage = right;
            previousSpread = previous;
            nextSpread = next;
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
            ReloadContacts();
            SetOpen(false);
        }

        private void OnDestroy()
        {
            if (closedBook != null) closedBook.onClick.RemoveListener(Toggle);
            if (openBook != null) openBook.onClick.RemoveListener(Toggle);
            if (previousSpread != null) previousSpread.onClick.RemoveListener(PreviousSpread);
            if (nextSpread != null) nextSpread.onClick.RemoveListener(NextSpread);
        }

        public void Toggle() => SetOpen(!IsOpen);

        public void SetOpen(bool value)
        {
            IsOpen = value;
            closedBook.gameObject.SetActive(!value);
            openBook.gameObject.SetActive(value);
        }
    }
}
