using Switchboard.Domain;
using UnityEngine;

namespace Switchboard.Game
{
    public sealed class CallAudio : MonoBehaviour
    {
        [SerializeField] private AudioSource ringSource;
        [SerializeField] private AudioSource feedbackSource;
        [SerializeField] private AudioClip incoming;
        [SerializeField] private AudioClip connected;
        [SerializeField] private AudioClip hold;
        [SerializeField] private AudioClip finished;
        [SerializeField] private AudioClip wrongConnection;
        [SerializeField, Min(1f)] private float ringInterval = 4.5f;
        private CallService calls;
        private float nextRing;
        private float nextFeedback;

        public void Bind(CallService service)
        {
            Unbind();
            calls = service;
            calls.CallAdded += OnAdded;
            calls.CallStateChanged += OnChanged;
            calls.CallEnded += OnEnded;
            nextRing = Time.unscaledTime;
            RefreshRinging();
        }

        private bool HasIncoming()
        {
            if (calls == null) return false;
            foreach (Call call in calls.Live)
                if (call.State == CallState.Incoming) return true;
            return false;
        }

        private void Update() => RefreshRinging();
        private void OnAdded(CallAddedArgs e) => RefreshRinging();
        private void RefreshRinging()
        {
            if (ringSource == null) return;
            if (!HasIncoming())
            {
                ringSource.Stop();
                nextRing = Time.unscaledTime;
                return;
            }
            // One gentle reminder for the queue, regardless of how many cards are waiting.
            if (Time.unscaledTime < nextRing || ringSource.isPlaying || incoming == null) return;
            ringSource.clip = incoming;
            ringSource.Play();
            nextRing = Time.unscaledTime + ringInterval;
        }

        private void OnChanged(CallStateChangedArgs e)
        {
            RefreshRinging();
            if (e.To == CallState.Connected) Play(connected);
            else if (e.To == CallState.OnHold) Play(hold);
        }

        private void OnEnded(CallEndedArgs e)
        {
            RefreshRinging();
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
