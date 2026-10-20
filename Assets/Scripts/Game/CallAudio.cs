using Switchboard.Domain;
using UnityEngine;

namespace Switchboard.Game
{
    public sealed class CallAudio : MonoBehaviour
    {
        public event System.Action ConnectionSucceeded;
        /// <summary>Raised by <see cref="SetPaused"/>, so call sounds elsewhere (the connection babble) pause too.</summary>
        public event System.Action<bool> PausedChanged;
        [SerializeField] private AudioSource ringSource;
        [SerializeField] private AudioSource feedbackSource;
        [SerializeField] private AudioClip incoming;
        [SerializeField, Range(0f, 2f)] private float incomingGain = .4875f;
        [SerializeField] private AudioClip connected;
        [SerializeField] private AudioClip cashRegister;
        [SerializeField, Range(0f, 2f)] private float cashRegisterGain = 1f;
        [SerializeField] private AudioClip hold;
        [SerializeField] private AudioClip finished;
        [SerializeField] private AudioClip wrongConnection;
        private CallService calls;
        private float nextFeedback;

        public bool IsPaused { get; private set; }

        /// <summary>The pause menu (D56): ringing and call sounds pause and resume where they were.</summary>
        public void SetPaused(bool paused)
        {
            if (IsPaused == paused) return;
            IsPaused = paused;
            foreach (AudioSource source in new[] { ringSource, feedbackSource })
            {
                if (source == null) continue;
                if (paused) source.Pause();
                else source.UnPause();
            }
            PausedChanged?.Invoke(paused);
        }

        public void Bind(CallService service)
        {
            Unbind();
            calls = service;
            calls.CallAdded += OnAdded;
            calls.CallStateChanged += OnChanged;
            calls.CallEnded += OnEnded;
        }

        private void OnAdded(CallAddedArgs e)
        {
            if (ringSource == null || incoming == null) return;
            // One bell for this new order. No reminders while its card stays in the queue.
            ringSource.loop = false;
            ringSource.pitch = e.Call.HasResponseTime ? 1.18f : 1f;
            ringSource.PlayOneShot(incoming, incomingGain);
        }

        private void OnChanged(CallStateChangedArgs e)
        {
            if (e.To == CallState.Connected)
            {
                // The register confirms the first successful connection, even with a zero fee.
                // It already includes the relay click and must not be swallowed by feedback debounce.
                if (e.From != CallState.OnHold && cashRegister != null && feedbackSource != null)
                    feedbackSource.PlayOneShot(cashRegister, cashRegisterGain);
                else Play(connected);
                ConnectionSucceeded?.Invoke();
            }
            else if (e.To == CallState.OnHold) Play(hold);
        }

        private void OnEnded(CallEndedArgs e)
        {
            if (e.Reason == CallEndReason.Cleared) return;
            Play(e.Reason == CallEndReason.WrongConnection ? wrongConnection : finished);
        }

        private void Play(AudioClip clip)
        {
            if (feedbackSource == null || clip == null || Time.unscaledTime < nextFeedback) return;
            feedbackSource.PlayOneShot(clip);
            nextFeedback = Time.unscaledTime + .12f;
        }

        private void OnDisable() => Unbind();
        private void Unbind()
        {
            if (calls != null)
            {
                calls.CallAdded -= OnAdded;
                calls.CallStateChanged -= OnChanged;
                calls.CallEnded -= OnEnded;
                calls = null;
            }
            if (ringSource != null) ringSource.Stop();
        }
    }
}
