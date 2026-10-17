namespace Switchboard.Domain
{
    public static class RandomExtensions
    {
        public static double Between(this IRandom random, double min, double max) =>
            max > min ? min + (max - min) * random.NextDouble() : min;
    }
}
