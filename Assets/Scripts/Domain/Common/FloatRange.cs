using System;

namespace Switchboard.Domain
{
    [Serializable]
    public struct FloatRange
    {
        public float min;
        public float max;

        public FloatRange(float min, float max)
        {
            this.min = min;
            this.max = max;
        }

        public bool Contains(double value) => value >= min && value <= max;
    }
}
