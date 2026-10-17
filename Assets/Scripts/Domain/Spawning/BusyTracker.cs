using System;

namespace Switchboard.Domain
{
    /// <summary>A person can't be in two calls at once (D18): not while in any live call, nor with a callback pending.</summary>
    public sealed class BusyTracker
    {
        private readonly CallService calls;
        private readonly CallbackScheduler callbacks;

        public BusyTracker(CallService calls, CallbackScheduler callbacks)
        {
            this.calls = calls ?? throw new ArgumentNullException(nameof(calls));
            this.callbacks = callbacks ?? throw new ArgumentNullException(nameof(callbacks));
        }

        public bool IsBusy(IPerson person) => calls.IsInLiveCall(person) || callbacks.IsPending(person);
    }
}
