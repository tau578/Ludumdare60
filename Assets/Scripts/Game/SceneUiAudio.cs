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
        [SerializeField, Min(0.1f)] private float phoneBabbleSeconds = 2f;
        private CallAudio callAudio;
        private float babbleUntil;
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
                if (callAudio == null) callAudio = root.GetComponentInChildren<CallAudio>(true);
            }
            if (book != null) { wasOpen = book.IsOpen; spread = book.CurrentSpreadIndex; }
            if (callAudio != null) callAudio.ConnectionSucceeded += PlayConnectionBabble;
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
            if (phoneBabble != null && phoneBabble.isPlaying && Time.unscaledTime >= babbleUntil)
                phoneBabble.Stop();
        }

        private void PlayConnectionBabble()
        {
            if (phoneBabble == null || phoneBabble.clip == null) return;
            babbleUntil = Time.unscaledTime + phoneBabbleSeconds;
            phoneBabble.loop = true;
            if (!phoneBabble.isPlaying) phoneBabble.Play();
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
            if (callAudio != null) callAudio.ConnectionSucceeded -= PlayConnectionBabble;
            callAudio = null;
            babbleUntil = 0;
            if (phoneBabble != null) phoneBabble.Stop();
        }
        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Unbind();
        }
    }
}
