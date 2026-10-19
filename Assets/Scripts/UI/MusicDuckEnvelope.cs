using UnityEngine;

namespace Switchboard.UI
{
    /// <summary>Temporary music attenuation, evaluated on unscaled time without changing saved volume.</summary>
    public sealed class MusicDuckEnvelope
    {
        private float started, releaseAt, ends;
        private float initialGain = 1f;
        private const float Attack = .12f;
        private const float Release = .45f;
        private const float QuietGain = .3f;

        public void Trigger(float now, float cueSeconds)
        {
            initialGain = Evaluate(now);
            started = now;
            releaseAt = now + Mathf.Max(Attack, cueSeconds - .2f);
            ends = releaseAt + Release;
        }

        public float Evaluate(float now)
        {
            if (now >= ends) return 1f;
            if (now < started + Attack)
                return Mathf.Lerp(initialGain, QuietGain, Mathf.SmoothStep(0, 1, (now - started) / Attack));
            if (now < releaseAt) return QuietGain;
            return Mathf.Lerp(QuietGain, 1f, Mathf.SmoothStep(0, 1, (now - releaseAt) / Release));
        }
    }
}
