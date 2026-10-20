using System;

namespace Switchboard.Domain
{
    /// <summary>
    /// The tutorial's shop hints (D57), each shown once: buy the first cord (step 8), the mystery upgrade (step 9),
    /// More Profit and More Clients. The save remembers which are done, so a reload never shows one twice.
    /// </summary>
    [Flags]
    public enum ShopHints
    {
        None = 0,
        Cord = 1,
        Mystery = 2,
        Profit = 4,
        Clients = 8,
        All = Cord | Mystery | Profit | Clients
    }
}
