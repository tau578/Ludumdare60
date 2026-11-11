using System;

namespace Switchboard.Domain
{
    /// <summary>
    /// Fines a missed emergency, and nothing else (D51). The fine follows pay (D82): emergencyFineFactor times what
    /// connecting the call would have paid (its fee plus its rate for its whole length, times the income multiplier in
    /// force when it is missed; no quick-connect tip), rounded to the nearest cent. Money never goes below zero.
    /// </summary>
    public sealed class PenaltyService
    {
        private readonly Wallet wallet;
        private readonly double fineFactor;
        private readonly Func<double> incomeMultiplier;

        /// <param name="fineFactor">Economy.json's emergencyFineFactor; 0 fines nothing.</param>
        /// <param name="incomeMultiplier">Read when a call is missed; null counts as 1.</param>
        public PenaltyService(CallService calls, Wallet wallet, double fineFactor = 0, Func<double> incomeMultiplier = null)
        {
            if (calls == null) throw new ArgumentNullException(nameof(calls));
            this.wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            this.fineFactor = fineFactor > 0 ? fineFactor : 0;
            this.incomeMultiplier = incomeMultiplier;
            calls.CallEnded += OnCallEnded;
        }

        public event Action<FineAppliedArgs> FineApplied;

        /// <summary>The fine a missed emergency costs now (D82); 0 for a regular call.</summary>
        public long FineFor(CallScript script)
        {
            if (script == null || !script.IsEmergency) return 0;
            double multiplier = incomeMultiplier?.Invoke() ?? 1;
            if (!(multiplier > 0)) multiplier = 1;
            double cents = fineFactor * script.FullPayCents * multiplier;
            return cents > 0 ? (long)Math.Round(cents, MidpointRounding.AwayFromZero) : 0;
        }

        // A wrong patch, a cut-off or a hang-up is never fined (D51, D63).
        private void OnCallEnded(CallEndedArgs e)
        {
            long requested = e.Reason == CallEndReason.Missed ? FineFor(e.Call.Request.Script) : 0;
            if (requested <= 0) return;
            long taken = wallet.ApplyFine(requested);
            e.Call.FineCents = taken;
            FineApplied?.Invoke(new FineAppliedArgs(e.Call, e.Reason, requested, taken));
        }
    }

    public readonly struct FineAppliedArgs
    {
        public FineAppliedArgs(Call call, CallEndReason reason, long requestedCents, long takenCents)
        {
            Call = call;
            Reason = reason;
            RequestedCents = requestedCents;
            TakenCents = takenCents;
        }

        public Call Call { get; }
        public CallEndReason Reason { get; }
        public long RequestedCents { get; }

        /// <summary>What the wallet actually gave up; less than requested when the balance was short.</summary>
        public long TakenCents { get; }
    }
}
