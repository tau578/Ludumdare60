using System;
using System.Collections.Generic;

namespace Switchboard.Domain
{
    public sealed class EarningsService
    {
        private readonly CallService calls;
        private readonly Wallet wallet;
        private readonly Func<double> multiplier;

        // The multiplier is read live, so an upgrade bought mid-call applies at once (D12). Null means 1.
        public EarningsService(CallService calls, Wallet wallet, Func<double> multiplier = null,
            QuickConnectTip quickTip = default)
        {
            this.calls = calls ?? throw new ArgumentNullException(nameof(calls));
            this.wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            this.multiplier = multiplier ?? (() => 1);
            QuickTip = quickTip;
            calls.CallStateChanged += OnCallStateChanged;
        }

        public event Action<ConnectionFeePaidArgs> ConnectionFeePaid;

        /// <summary>Raised after the fee when a call connects quickly enough for the tip (D59).</summary>
        public event Action<QuickConnectTipPaidArgs> QuickConnectTipPaid;

        public QuickConnectTip QuickTip { get; }

        public double CurrentMultiplier => multiplier();

        /// <summary>Every fee and per-second payment of this session, for the career stats.</summary>
        public double TotalPaidCents { get; private set; }

        // Runs before CallService.Tick and pays only the time left, so the last partial second is paid exactly.
        public void Tick(double dt)
        {
            if (!(dt > 0)) return;
            double factor = multiplier();
            IReadOnlyList<Call> live = calls.Live;
            for (int i = 0; i < live.Count; i++)
            {
                Call call = live[i];
                if (call.State != CallState.Connected) continue;
                Pay(call, Math.Min(dt, call.SecondsLeft) * call.Request.CentsPerSecond * factor);
            }
        }

        // The fee is paid the moment a call first connects, so it never waits on the tick order (D32). A resume after
        // hold is not a first connection, so it pays neither the fee nor the tip again.
        private void OnCallStateChanged(CallStateChangedArgs e)
        {
            Call call = e.Call;
            if (e.To != CallState.Connected || call.ConnectionFeePaid) return;
            call.ConnectionFeePaid = true;
            double factor = multiplier();
            double cents = call.Request.ConnectionFeeCents * factor;
            if (Pay(call, cents)) ConnectionFeePaid?.Invoke(new ConnectionFeePaidArgs(call, cents));
            if (!QuickTip.Qualifies(call.SecondsInHand)) return;
            // Multiplied like the fee, so the tip stays worth chasing as rates grow (fines alone are never multiplied).
            double tip = QuickTip.Cents * factor;
            if (!Pay(call, tip)) return;
            call.TipCents = tip;
            QuickConnectTipPaid?.Invoke(new QuickConnectTipPaidArgs(call, tip));
        }

        private bool Pay(Call call, double cents)
        {
            if (!(cents > 0)) return false;
            call.CentsEarned += cents;
            TotalPaidCents += cents;
            wallet.Earn(cents);
            return true;
        }
    }
}
