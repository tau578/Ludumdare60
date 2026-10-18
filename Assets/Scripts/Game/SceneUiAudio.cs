using System.Collections.Generic;
using Switchboard.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Switchboard.Game
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class SceneUiAudio : MonoBehaviour
    {
        [SerializeField] private AudioClip menuClick;
        [SerializeField] private AudioClip bookOpen;
        [SerializeField] private AudioClip bookClose;
        [SerializeField] private AudioClip pageTurn;
        [SerializeField] private AudioSource phoneBabble;
        private CallQueueView callQueue;
        private AudioSource source;
        private readonly List<Button> buttons = new List<Button>();
        private PhoneBookView book;
        private bool wasOpen;
        private int spread;
        private float nextClick;

        private void Awake() => source = GetComponent<AudioSource>();
        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            if (source != null) Bind(SceneManager.GetActiveScene());
        }
        private void Start() => Bind(SceneManager.GetActiveScene());
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Bind(scene);

        private void Bind(Scene scene)
        {
            Unbind();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                // Scope menu clicks to the menu component, including its inactive panels.
                foreach (global::Menu menu in root.GetComponentsInChildren<global::Menu>(true))
                    foreach (Button button in menu.GetComponentsInChildren<Button>(true))
                    {
                        if (buttons.Contains(button)) continue;
                        buttons.Add(button);
                        button.onClick.AddListener(PlayClick);
                    }
                if (book == null) book = root.GetComponentInChildren<PhoneBookView>(true);
                if (callQueue == null) callQueue = root.GetComponentInChildren<CallQueueView>(true);
            }
            if (book != null) { wasOpen = book.IsOpen; spread = book.CurrentSpreadIndex; }
        }

        private void LateUpdate()
        {
            UpdateBabble();
            if (book == null) return;
            if (wasOpen != book.IsOpen) Play(book.IsOpen ? bookOpen : bookClose);
            else if (book.IsOpen && spread != book.CurrentSpreadIndex) Play(pageTurn);
            wasOpen = book.IsOpen;
            spread = book.CurrentSpreadIndex;
        }

        private void UpdateBabble()
        {
            if (phoneBabble == null) return;
            bool talking = callQueue != null && callQueue.isActiveAndEnabled && callQueue.HasSpeakingCaller;
            if (talking && !phoneBabble.isPlaying && phoneBabble.clip != null)
            {
                phoneBabble.loop = true;
                phoneBabble.Play();
            }
            else if (!talking && phoneBabble.isPlaying) phoneBabble.Stop();
        }

        private void PlayClick()
        {
            if (Time.unscaledTime < nextClick) return;
            Play(menuClick);
            nextClick = Time.unscaledTime + .06f;
        }
        private void Play(AudioClip clip) { if (clip != null) source.PlayOneShot(clip); }
        private void Unbind()
        {
            foreach (Button button in buttons) if (button != null) button.onClick.RemoveListener(PlayClick);
            buttons.Clear();
            book = null;
            callQueue = null;
            if (phoneBabble != null) phoneBabble.Stop();
        }
        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Unbind();
        }
    }
}
