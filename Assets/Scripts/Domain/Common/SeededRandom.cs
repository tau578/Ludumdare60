using System;

namespace Switchboard.Domain
{
    public sealed class SeededRandom : IRandom
    {
        private readonly Random random;

        public SeededRandom(int seed) => random = new Random(seed);

        public int Next(int minInclusive, int maxExclusive) => random.Next(minInclusive, maxExclusive);
        public double NextDouble() => random.NextDouble();
    }
}
