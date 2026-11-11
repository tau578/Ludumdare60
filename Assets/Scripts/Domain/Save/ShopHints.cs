using System;

namespace Switchboard.Domain
{
    /// <summary>
    /// The tutorial's hints after the first call (D57), each shown once: buy the first cord (step 9), the mystery upgrade
    /// (step 10), More Profit and More Clients, and since 13a pass 4 (D81) the first emergency. The save remembers which
    /// are done, so a reload never shows one twice.
    /// </summary>
    [Flags]
    public enum ShopHints
    {
        None = 0,
        Cord = 1,
        Mystery = 2,
        Profit = 4,
        Clients = 8,
        Emergency = 16,
        All = Cord | Mystery | Profit | Clients | Emergency
    }
}
