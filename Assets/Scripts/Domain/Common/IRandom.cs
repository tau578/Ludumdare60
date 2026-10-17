namespace Switchboard.Domain
{
    public interface IRandom
    {
        int Next(int minInclusive, int maxExclusive);
        double NextDouble();
    }
}
