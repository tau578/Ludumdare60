using System;

namespace Switchboard.Domain
{
    /// <summary>
    /// Lifetime numbers across sessions (Design 5.10): money earned, calls connected, fines paid and time played.
    /// They start from the save and only ever grow.
    /// </summary>
    public sealed class CareerStats
    {
        private readonly EarningsService earnings;
        private readonly long earnedBefore;

        public CareerStats(CallService calls, EarningsService earnings, PenaltyService penalties, CareerData saved = null)
        {
            if (calls == null) throw new ArgumentNullException(nameof(calls));
            if (penalties == null) throw new ArgumentNullException(nameof(penalties));
            this.earnings = earnings ?? throw new ArgumentNullException(nameof(earnings));
            if (saved != null)
            {
                earnedBefore = Math.Max(0, saved.earnedCents);
                CallsConnected = Math.Max(0, saved.callsConnected);
                FinesPaidCents = Math.Max(0, saved.finesPaidCents);
                SecondsPlayed = saved.secondsPlayed > 0 ? saved.secondsPlayed : 0;
            }
            calls.CallStateChanged += OnCallStateChanged;
            penalties.FineApplied += e => FinesPaidCents += e.TakenCents;
        }

        /// <summary>Fees and per-second pay; dev money and refunds don't count.</summary>
        public long EarnedCents => earnedBefore + (long)Math.Round(earnings.TotalPaidCents);

        public int CallsConnected { get; private set; }

        /// <summary>What fines actually took, which is less than asked when the balance was short.</summary>
        public long FinesPaidCents { get; private set; }

        public double SecondsPlayed { get; private set; }

        public void Tick(double dt)
        {
            if (dt > 0) SecondsPlayed += dt;
        }

        public CareerData Capture() => new CareerData
        {
            earnedCents = EarnedCents,
            callsConnected = CallsConnected,
            finesPaidCents = FinesPaidCents,
            secondsPlayed = SecondsPlayed
        };

        // A call resumed from hold connects again, but it was already counted (D07).
        private void OnCallStateChanged(CallStateChangedArgs e)
        {
            if (e.To == CallState.Connected && e.From != CallState.OnHold) CallsConnected++;
        }
    }
}
