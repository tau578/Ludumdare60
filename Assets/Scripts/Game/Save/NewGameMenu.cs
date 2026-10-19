using Switchboard.Content;
using UnityEngine;
using UnityEngine.UI;

namespace Switchboard.Game
{
    /// <summary>
    /// The main menu's save-aware buttons (D55, lead's 9a playtest). Without a save Continue is hidden and New Game
    /// starts at once; with a save Continue comes first and New Game asks before it deletes the whole save (career
    /// included). The visible buttons are re-spaced evenly so a hidden one leaves no gap. Sergei's Menu can't see this
    /// layer, so the buttons are wired in MainMenu.unity. Quit shows only where Application.Quit works: not on Web.
    /// </summary>
    public sealed class NewGameMenu : MonoBehaviour
    {
        [SerializeField] private global::Menu menu;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button newGameButton;
        [SerializeField] private GameObject confirmPanel;
        [SerializeField] private Button startOverButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button quitButton;
        [Tooltip("The main menu's buttons, top to bottom; only the active ones get a slot.")]
        [SerializeField] private RectTransform[] menuButtons;
        [SerializeField] private float centreY = -50f;
        [SerializeField] private float spacing = 122f;

        private bool hasSave;

        private void Awake()
        {
            if (newGameButton != null) newGameButton.onClick.AddListener(OnNewGame);
            if (startOverButton != null) startOverButton.onClick.AddListener(StartOver);
            if (cancelButton != null) cancelButton.onClick.AddListener(Cancel);
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        public static bool QuitAvailable => false;
#else
        public static bool QuitAvailable => true;
#endif

        private void Start() => Apply(SaveStorage.OpenFile().HasSave);

        private void OnDestroy()
        {
            if (newGameButton != null) newGameButton.onClick.RemoveListener(OnNewGame);
            if (startOverButton != null) startOverButton.onClick.RemoveListener(StartOver);
            if (cancelButton != null) cancelButton.onClick.RemoveListener(Cancel);
        }

        public void Apply(bool saveExists) => Apply(saveExists, QuitAvailable);

        public void Apply(bool saveExists, bool quitVisible)
        {
            hasSave = saveExists;
            if (continueButton != null) continueButton.gameObject.SetActive(saveExists);
            if (newGameButton != null) newGameButton.gameObject.SetActive(true);
            if (quitButton != null) quitButton.gameObject.SetActive(quitVisible);
            if (confirmPanel != null) confirmPanel.SetActive(false);
            Arrange();
        }

        public void OnNewGame()
        {
            if (hasSave) Ask();
            else StartNew();
        }

        public void Ask()
        {
            if (confirmPanel != null) confirmPanel.SetActive(true);
        }

        public void Cancel()
        {
            if (confirmPanel != null) confirmPanel.SetActive(false);
        }

        public void StartOver()
        {
            SaveStorage.OpenFile().Delete();
            hasSave = false;
            if (confirmPanel != null) confirmPanel.SetActive(false);
            StartNew();
        }

        private void StartNew()
        {
            if (menu != null) menu.onPlayButtonClick();
        }

        private void Arrange()
        {
            if (menuButtons == null) return;
            int count = 0;
            foreach (RectTransform button in menuButtons)
                if (button != null && button.gameObject.activeSelf) count++;
            int slot = 0;
            foreach (RectTransform button in menuButtons)
            {
                if (button == null || !button.gameObject.activeSelf) continue;
                button.anchoredPosition = new Vector2(button.anchoredPosition.x, SlotY(slot++, count, centreY, spacing));
            }
        }

        /// <summary>The y of slot <paramref name="index"/> in an evenly spaced column of <paramref name="count"/> centred on <paramref name="centre"/>.</summary>
        public static float SlotY(int index, int count, float centre, float spacing) =>
            centre + (count - 1) * spacing * 0.5f - index * spacing;
    }
}
