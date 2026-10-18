using Switchboard.Domain;
using UnityEngine;

namespace Switchboard.Content
{
    [CreateAssetMenu(menuName = "Switchboard/CallerTypeDefinition")]
    public sealed class CallerTypeDefinition : ScriptableObject, ICallerType
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private FloatRange centsPerSecond = new FloatRange(3, 8);
        [SerializeField] private FloatRange durationSeconds = new FloatRange(15, 30);
        [SerializeField, Tooltip("Paid once when a call connects correctly (D32).")]
        private FloatRange connectionFeeCents = new FloatRange(5, 15);
        [SerializeField, Tooltip("Seconds until a wrongly connected caller rings again.")]
        private FloatRange callbackDelaySeconds = new FloatRange(20, 40);

        [Header("Response time and fines")]
        [SerializeField, Tooltip("Seconds to connect the call correctly before it is missed. 0 = waits as long as it takes.")]
        private float responseTimeSeconds;
        [SerializeField, Tooltip("Cents taken for a missed call and for a wrong connection.")]
        private long fineMissedCents;
        [SerializeField, Tooltip("Cents taken for cutting a connected call off before it finishes.")]
        private long fineEarlyDisconnectCents;
        [SerializeField, Tooltip("Whether a missed caller rings again later (D10).")]
        private bool callbackAfterMiss;
        [SerializeField, Tooltip("Whether a caller who was cut off rings again later (D10).")]
        private bool callbackAfterDisconnect;

        [Header("Who calls whom")]
        [SerializeField, Tooltip("Only entries with this tag receive the calls. Empty = any person without tags.")]
        private string recipientTag;
        [SerializeField, Tooltip("Every person without tags can place calls of this type, besides their own type.")]
        private bool everyoneCalls;
        [SerializeField, Tooltip("At most this many calls of the type at once, counting pending callbacks. 0 = no limit.")]
        private int maxSimultaneous;
        [SerializeField, Tooltip("How often new calls are of this type compared with the other types (D37, until Step 7).")]
        private float spawnWeight = 1;

        [Header("Card (presentation only)")]
        [SerializeField] private CallCardStyle cardStyle;

        public string Id => id;
        public string DisplayName => displayName;
        public FloatRange CentsPerSecond => centsPerSecond;
        public FloatRange ConnectionFeeCents => connectionFeeCents;
        public FloatRange DurationSeconds => durationSeconds;
        public FloatRange CallbackDelaySeconds => callbackDelaySeconds;
        public double ResponseTimeSeconds => responseTimeSeconds;
        public long FineMissedCents => fineMissedCents;
        public long FineEarlyDisconnectCents => fineEarlyDisconnectCents;
        public bool CallbackAfterMiss => callbackAfterMiss;
        public bool CallbackAfterDisconnect => callbackAfterDisconnect;
        public string RecipientTag => recipientTag ?? string.Empty;
        public int MaxSimultaneous => maxSimultaneous;
        public double SpawnWeight => spawnWeight;
        public bool EveryoneCalls => everyoneCalls;
        public CallCardStyle CardStyle => cardStyle;
    }
}
