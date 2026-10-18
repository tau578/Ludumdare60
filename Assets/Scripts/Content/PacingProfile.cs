using Switchboard.Domain;
using UnityEngine;

namespace Switchboard.Content
{
    /// <summary>When new calls ring (Design 5.2). Progress curves and opening calls come in Step 7.</summary>
    [CreateAssetMenu(menuName = "Switchboard/PacingProfile")]
    public sealed class PacingProfile : ScriptableObject, IPacingProfile
    {
        [SerializeField, Min(0), Tooltip("Seconds before the first call of a session rings.")]
        private float firstCallDelaySeconds = 2;
        [SerializeField, Min(0.1f), Tooltip("Average seconds between new calls while the board keeps up.")]
        private float baseIntervalSeconds = 8;
        [SerializeField, Range(0, 0.95f), Tooltip("0.3 rolls each interval within ±30 % of the base.")]
        private float intervalJitter = 0.3f;
        [SerializeField, Min(0), Tooltip("Each waiting call beyond the free cords stretches the interval by this share.")]
        private float loadFactor = 0.5f;
        [SerializeField, Min(0), Tooltip("Seconds without input before calls that carry fines stop ringing (D16). 0 = never.")]
        private float idleTimeoutSeconds = 60;

        public double FirstCallDelaySeconds => firstCallDelaySeconds;
        public double BaseIntervalSeconds => baseIntervalSeconds;
        public double IntervalJitter => intervalJitter;
        public double LoadFactor => loadFactor;
        public double IdleTimeoutSeconds => idleTimeoutSeconds;
    }
}
