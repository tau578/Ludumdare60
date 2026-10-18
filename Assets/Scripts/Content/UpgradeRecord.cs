using Switchboard.Domain;

namespace Switchboard.Content
{
    public sealed class UpgradeRecord : IUpgrade
    {
        public UpgradeRecord(string id, UpgradeKind kind, string name, string description, long costCents, double incomeMultiplier, int wires = 0,
            BoardSize boardSize = default, int clientSlots = 0)
        {
            Id = id;
            Kind = kind;
            Name = name;
            Description = description;
            CostCents = costCents;
            IncomeMultiplier = incomeMultiplier;
            Wires = wires;
            BoardSize = boardSize;
            ClientSlots = clientSlots;
        }

        public string Id { get; }
        public UpgradeKind Kind { get; }
        public string Name { get; }
        public string Description { get; }
        public long CostCents { get; }
        public double IncomeMultiplier { get; }
        public int Wires { get; }
        public BoardSize BoardSize { get; }
        public int ClientSlots { get; }
    }
}
