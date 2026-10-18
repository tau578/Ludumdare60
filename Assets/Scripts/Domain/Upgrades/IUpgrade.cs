namespace Switchboard.Domain
{
    /// <summary>One one-time entry of Economy.json's upgrades (D40). Its id is stable forever, because saves store it.</summary>
    public interface IUpgrade
    {
        string Id { get; }
        UpgradeKind Kind { get; }
        string Name { get; }
        string Description { get; }
        long CostCents { get; }

        /// <summary>The earnings multiplier an extra_income entry sets; 0 for other kinds.</summary>
        double IncomeMultiplier { get; }

        /// <summary>The total number of cords an extra_wires entry sets; 0 for other kinds.</summary>
        int Wires { get; }

        /// <summary>The open part of the board an extra_clients entry sets (D49); 0x0 for other kinds.</summary>
        BoardSize BoardSize { get; }

        /// <summary>The number of caller slots an extra_clients entry sets (D49); 0 for other kinds.</summary>
        int ClientSlots { get; }
    }
}
