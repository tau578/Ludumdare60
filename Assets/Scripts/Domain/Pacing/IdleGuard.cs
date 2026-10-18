namespace Switchboard.Domain
{
    /// <summary>
    /// Walking away must never drain money (D16): after a while without input, types that carry fines
    /// stop spawning and their due callbacks wait. Regular calls carry on.
    /// </summary>
    public sealed class IdleGuard
    {
        private readonly double timeoutSeconds;

        public IdleGuard(double timeoutSeconds) => this.timeoutSeconds = timeoutSeconds;

        public double SecondsSinceInput { get; private set; }

        public bool IsIdle => timeoutSeconds > 0 && SecondsSinceInput >= timeoutSeconds;

        public void ReportInput() => SecondsSinceInput = 0;

        public void Tick(double dt)
        {
            if (dt > 0) SecondsSinceInput += dt;
        }

        public bool Blocks(ICallerType type) => IsIdle && type.CarriesFines();
    }
}
