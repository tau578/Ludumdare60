using System;
using System.Collections.Generic;

namespace Switchboard.Domain
{
    public sealed class EarningsService
    {
        // StatSheet supplies a live multiplier from Step 5 (D12).
        private const double Multiplier = 1;

        private readonly CallService calls;
        private readonly Wallet wallet;

        public EarningsService(CallService calls, Wallet wallet)
        {
            this.calls = calls ?? throw new ArgumentNullException(nameof(calls));
            this.wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
        }

        // Runs before CallService.Tick and pays only the time left, so the last partial second is paid exactly.
        public void Tick(double dt)
        {
            if (!(dt > 0)) return;
            IReadOnlyList<Call> live = calls.Live;
            for (int i = 0; i < live.Count; i++)
            {
                Call call = live[i];
                if (call.State != CallState.Connected) continue;
                double cents = Math.Min(dt, call.SecondsLeft) * call.Request.CentsPerSecond * Multiplier;
                if (!(cents > 0)) continue;
                call.CentsEarned += cents;
                wallet.Earn(cents);
            }
        }
    }
}
