using System;

namespace Switchboard.Domain
{
    /// <summary>Fines calls that end badly, by their type's fines (Design 5.5). Money never goes below zero.</summary>
    public sealed class PenaltyService
    {
        private readonly Wallet wallet;

        public PenaltyService(CallService calls, Wallet wallet)
        {
            if (calls == null) throw new ArgumentNullException(nameof(calls));
            this.wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            calls.CallEnded += OnCallEnded;
        }

        public event Action<FineAppliedArgs> FineApplied;

        // Fines are not multiplied by the earnings multiplier (D37).
        private void OnCallEnded(CallEndedArgs e)
        {
            long requested = e.Call.Request.Type.FineFor(e.Reason);
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
