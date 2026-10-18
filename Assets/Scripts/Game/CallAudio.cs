using Switchboard.Domain;
using UnityEngine;

namespace Switchboard.Game
{
    public sealed class CallAudio : MonoBehaviour
    {
        public event System.Action ConnectionSucceeded;
        [SerializeField] private AudioSource ringSource;
        [SerializeField] private AudioSource feedbackSource;
        [SerializeField] private AudioClip incoming;
        [SerializeField] private AudioClip connected;
        [SerializeField] private AudioClip cashRegister;
        [SerializeField] private AudioClip hold;
        [SerializeField] private AudioClip finished;
        [SerializeField] private AudioClip wrongConnection;
        private CallService calls;
        private EarningsService earnings;
        private float nextFeedback;

        public void Bind(CallService service, EarningsService earningsService = null)
        {
            Unbind();
            calls = service;
            calls.CallAdded += OnAdded;
            calls.CallStateChanged += OnChanged;
            calls.CallEnded += OnEnded;
            earnings = earningsService;
            if (earnings != null) earnings.ConnectionFeePaid += OnFeePaid;
        }

        private void OnAdded(CallAddedArgs e)
        {
            if (ringSource == null || incoming == null) return;
            // One bell for this new order. No reminders while its card stays in the queue.
            ringSource.loop = false;
            ringSource.pitch = e.Call.HasResponseTime ? 1.18f : 1f;
            ringSource.PlayOneShot(incoming);
        }

        private void OnFeePaid(ConnectionFeePaidArgs e)
        {
            // Independent of the relay's debounce: each actual connection fee gets its own cue.
            if (feedbackSource != null && cashRegister != null) feedbackSource.PlayOneShot(cashRegister);
        }

        private void OnChanged(CallStateChangedArgs e)
        {
            if (e.To == CallState.Connected)
            {
                Play(connected);
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
            if (earnings != null) earnings.ConnectionFeePaid -= OnFeePaid;
            earnings = null;
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
