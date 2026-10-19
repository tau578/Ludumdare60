using System;
using Switchboard.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Switchboard.Game
{
    /// <summary>
    /// The pause menu (D56): the pause button or Esc opens a modal with Resume, Options (the main menu's volume
    /// patch) and Main Menu. While paused, game time stops, call sounds pause and the cord input is off, so a
    /// held cord is still held after Resume. Esc closes an open book first; the book itself never pauses (D20).
    /// </summary>
    public sealed class GamePauseMenu : MonoBehaviour
    {
        [SerializeField] private Button pauseButton;
        [SerializeField] private GameObject dialog;
        [SerializeField] private GameObject mainPage;
        [SerializeField] private GameObject optionsPage;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button optionsButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button backButton;
        [SerializeField] private UiClickSound clickSound;
        [SerializeField] private int menuSceneIndex;

        private Action save;
        private CableInputController cords;
        private PhoneBookView book;
        private CallAudio callAudio;
        private Action<int> loadScene;
        private float scaleBeforePause = 1f;
        private bool listening;

        public bool IsPaused { get; private set; }
        public bool OptionsOpen => IsPaused && optionsPage != null && optionsPage.activeSelf;

        /// <param name="saveNow">Writes the save at once (<see cref="Autosave.SaveNow"/>); may be null outside Play Mode.</param>
        /// <param name="sceneLoader">Loads a build index; tests pass their own.</param>
        public void Bind(Action saveNow, CableInputController cordInput, PhoneBookView phoneBook, CallAudio audio,
            Action<int> sceneLoader = null)
        {
            Unbind();
            save = saveNow;
            cords = cordInput;
            book = phoneBook;
            callAudio = audio;
            loadScene = sceneLoader ?? SceneManager.LoadScene;
            if (book != null) book.ClosesOnEscape = false;
            pauseButton.onClick.AddListener(OnPauseClicked);
            resumeButton.onClick.AddListener(OnResumeClicked);
            optionsButton.onClick.AddListener(ShowOptions);
            mainMenuButton.onClick.AddListener(GoToMainMenu);
            backButton.onClick.AddListener(ShowMain);
            listening = true;
            dialog.SetActive(false);
        }

        public void Pause()
        {
            if (IsPaused) return;
            IsPaused = true;
            scaleBeforePause = Time.timeScale;
            Time.timeScale = 0f;
            SetGameInput(false);
            ShowPage(false);
            dialog.SetActive(true);
            save?.Invoke();
        }

        public void Resume()
        {
            if (!IsPaused) return;
            IsPaused = false;
            dialog.SetActive(false);
            // The dev panel's x5 survives a pause.
            Time.timeScale = scaleBeforePause;
            SetGameInput(true);
        }

        /// <summary>Esc: resume while paused, else close an open book, else pause.</summary>
        public void HandleEscape()
        {
            if (IsPaused) Resume();
            else if (book != null && book.IsOpen) book.SetOpen(false);
            else Pause();
        }

        public void ShowOptions()
        {
            Click();
            ShowPage(true);
        }

        public void ShowMain()
        {
            Click();
            ShowPage(false);
        }

        /// <summary>Saves first, then loads the main menu, where Continue picks the game up again.</summary>
        public void GoToMainMenu()
        {
            Click();
            save?.Invoke();
            IsPaused = false;
            Time.timeScale = 1f;
            loadScene(menuSceneIndex);
        }

        private void Update()
        {
            if (!listening) return;
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) HandleEscape();
        }

        private void OnPauseClicked()
        {
            Click();
            Pause();
        }

        private void OnResumeClicked()
        {
            Click();
            Resume();
        }

        private void ShowPage(bool options)
        {
            mainPage.SetActive(!options);
            optionsPage.SetActive(options);
        }

        private void SetGameInput(bool enabled)
        {
            if (cords != null) cords.Suspended = !enabled;
            if (book != null) book.InputEnabled = enabled;
            if (callAudio != null) callAudio.SetPaused(!enabled);
        }

        private void Click()
        {
            if (clickSound != null) clickSound.Play();
        }

        private void OnDestroy()
        {
            // Leaving the scene while paused (dev New game) must not leave the clock stopped.
            if (IsPaused) Time.timeScale = 1f;
            IsPaused = false;
            Unbind();
        }

        private void Unbind()
        {
            if (!listening) return;
            listening = false;
            if (pauseButton != null) pauseButton.onClick.RemoveListener(OnPauseClicked);
            if (resumeButton != null) resumeButton.onClick.RemoveListener(OnResumeClicked);
            if (optionsButton != null) optionsButton.onClick.RemoveListener(ShowOptions);
            if (mainMenuButton != null) mainMenuButton.onClick.RemoveListener(GoToMainMenu);
            if (backButton != null) backButton.onClick.RemoveListener(ShowMain);
            if (book != null) book.ClosesOnEscape = true;
        }
    }
}
