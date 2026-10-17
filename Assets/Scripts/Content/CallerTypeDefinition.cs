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

        public string Id => id;
        public string DisplayName => displayName;
        public FloatRange CentsPerSecond => centsPerSecond;
        public FloatRange ConnectionFeeCents => connectionFeeCents;
        public FloatRange DurationSeconds => durationSeconds;
    }
}
