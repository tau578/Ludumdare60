namespace Switchboard.Domain
{
    public readonly struct CallAddedArgs
    {
        public CallAddedArgs(Call call) => Call = call;

        public Call Call { get; }
    }

    public readonly struct CallStateChangedArgs
    {
        public CallStateChangedArgs(Call call, CallState from, CallState to)
        {
            Call = call;
            From = from;
            To = to;
        }

        public Call Call { get; }
        public CallState From { get; }
        public CallState To { get; }
    }

    public readonly struct CallEndedArgs
    {
        public CallEndedArgs(Call call, CallEndReason reason)
        {
            Call = call;
            Reason = reason;
        }

        public Call Call { get; }
        public CallEndReason Reason { get; }
    }

    public readonly struct ConnectionFeePaidArgs
    {
        public ConnectionFeePaidArgs(Call call, double cents)
        {
            Call = call;
            Cents = cents;
        }

        public Call Call { get; }
        public double Cents { get; }
    }

    public readonly struct QuickConnectTipPaidArgs
    {
        public QuickConnectTipPaidArgs(Call call, double cents)
        {
            Call = call;
            Cents = cents;
        }

        public Call Call { get; }
        public double Cents { get; }
    }
}
