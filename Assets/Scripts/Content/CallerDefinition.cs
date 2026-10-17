using Switchboard.Domain;
using UnityEngine;

namespace Switchboard.Content
{
    [CreateAssetMenu(menuName = "Switchboard/CallerDefinition")]
    public sealed class CallerDefinition : ScriptableObject, ICaller
    {
        [SerializeField] private string id;
        [SerializeField] private PersonDefinition person;
        [SerializeField] private CallerTypeDefinition type;
        [SerializeField, Min(0)] private float weight = 1;
        [SerializeField, Min(0), Tooltip("0 rolls the rate from the caller type's range.")]
        private float centsPerSecondOverride;

        public string Id => id;
        public IPerson Person => person != null ? person : null;
        public ICallerType Type => type != null ? type : null;
        public float Weight => weight;
        public float CentsPerSecondOverride => centsPerSecondOverride;
    }
}
