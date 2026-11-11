using System.Collections.Generic;
using Switchboard.Domain;
using UnityEngine;

namespace Switchboard.Game
{
    /// <summary>
    /// Call sounds. Emergencies (S1, S2; D64, built in 13a): an arriving emergency rings the alarm bell instead of the
    /// pitched-up ring, again every few seconds while it is still waiting to be taken, and stops once it is taken or
    /// ends. In an emergency's last seconds the clock ticks, faster as time runs out, in step with the ring's pulse.
    /// Both run on game time, so the pause stops them, and so does a tutorial step that holds the clock (D81).
    /// </summary>
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

        [Header("Emergencies (S1, S2)")]
        [SerializeField, Tooltip("Plays the alarm bell and the ticks, so the bell can stop once the emergency is taken.")]
        private AudioSource alarmSource;
        [SerializeField] private AudioClip emergencyBell;
        [SerializeField, Range(0f, 2f)] private float bellGain = .8f;
        [SerializeField, Min(1f), Tooltip("Seconds between bells while an emergency waits to be taken.")]
        private float bellRepeatSeconds = 6f;
        [SerializeField] private AudioClip clockTick;
        [SerializeField, Range(0f, 2f)] private float tickGain = .8f;
        private CallService calls;
        private GameStyle paint;
        private System.Func<bool> clockHeld;
        private float bellTimer;
        private float tickTimer;
        private bool bellRinging;
        private float nextFeedback;

        public bool IsPaused { get; private set; }

        /// <summary>Bells rung since binding, for tests.</summary>
        public int BellsRung { get; private set; }

        /// <summary>Clock ticks played since binding, for tests.</summary>
        public int TicksPlayed { get; private set; }

        /// <summary>The pause menu (D56): ringing and call sounds pause and resume where they were.</summary>
        public void SetPaused(bool paused)
        {
            if (IsPaused == paused) return;
            IsPaused = paused;
            foreach (AudioSource source in new[] { ringSource, feedbackSource, alarmSource })
            {
                if (source == null) continue;
                if (paused) source.Pause();
                else source.UnPause();
            }
            PausedChanged?.Invoke(paused);
        }

        /// <param name="style">Its urgent seconds and pulse set when the clock ticks, shared with the card's ring.</param>
        /// <param name="holdClock">While true, the bell's repeats and the ticks wait: a tutorial step is on screen (D81).</param>
        public void Bind(CallService service, GameStyle style = null, System.Func<bool> holdClock = null)
        {
            Unbind();
            calls = service;
            paint = style;
            clockHeld = holdClock;
            BellsRung = TicksPlayed = 0;
            bellTimer = tickTimer = 0;
            calls.CallAdded += OnAdded;
            calls.CallStateChanged += OnChanged;
            calls.CallEnded += OnEnded;
        }

        private void OnAdded(CallAddedArgs e)
        {
            if (e.Call.HasResponseTime && emergencyBell != null)
            {
                RingBell();
                return;
            }
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

        private void Update() => Advance(Time.deltaTime);

        /// <summary>Moves the bell and the ticks on by game seconds (0 while paused or held). Update drives it.</summary>
        public void Advance(float gameSeconds)
        {
            if (calls == null || IsPaused || !(gameSeconds > 0) || (clockHeld != null && clockHeld())) return;
            bool waitingToBeTaken = false;
            double mostUrgent = double.MaxValue;
            IReadOnlyList<Call> live = calls.Live;
            for (int i = 0; i < live.Count; i++)
            {
                Call call = live[i];
                if (!call.HasResponseTime || call.WaitingHeld) continue;
                if (call.State == CallState.Incoming) waitingToBeTaken = true;
                if (call.State == CallState.Incoming || call.State == CallState.Taken || call.State == CallState.Connecting)
                    mostUrgent = System.Math.Min(mostUrgent, call.ResponseSecondsLeft);
            }

            if (waitingToBeTaken)
            {
                if ((bellTimer -= gameSeconds) <= 0) RingBell();
            }
            else if (bellRinging)
            {
                bellRinging = false;
                if (alarmSource != null) alarmSource.Stop();
            }

            float urgent = paint != null ? paint.UrgentSeconds : 10f;
            if (mostUrgent <= urgent)
            {
                if ((tickTimer -= gameSeconds) > 0) return;
                PlayTick();
                tickTimer = TickInterval(mostUrgent, urgent, paint != null ? paint.UrgentPulsesPerSecond : 3f);
            }
            else tickTimer = 0;
        }

        /// <summary>
        /// Seconds to the next tick: every third pulse of the ring at first, every second pulse from 60% of the urgent
        /// seconds, every pulse in the last 30%.
        /// </summary>
        public static float TickInterval(double secondsLeft, float urgentSeconds, float pulsesPerSecond)
        {
            float pulses = secondsLeft > urgentSeconds * .6f ? 3 : secondsLeft > urgentSeconds * .3f ? 2 : 1;
            return pulses / Mathf.Max(.1f, pulsesPerSecond);
        }

        private void RingBell()
        {
            bellTimer = bellRepeatSeconds;
            BellsRung++;
            bellRinging = true;
            if (alarmSource == null || emergencyBell == null) return;
            // Stop also ends a one-shot, so a taken emergency silences its bell at once.
            alarmSource.Stop();
            alarmSource.PlayOneShot(emergencyBell, bellGain);
        }

        private void PlayTick()
        {
            TicksPlayed++;
            AudioSource source = feedbackSource != null ? feedbackSource : alarmSource;
            if (source != null && clockTick != null) source.PlayOneShot(clockTick, tickGain);
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
            if (alarmSource != null) alarmSource.Stop();
            bellRinging = false;
        }
    }
}
