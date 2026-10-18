using UnityEngine;

namespace Switchboard.Content
{
    /// <summary>How cards of one call type look. The type's money and timing live in Economy.json (D47).</summary>
    [CreateAssetMenu(menuName = "Switchboard/CallerTypeDefinition")]
    public sealed class CallerTypeDefinition : ScriptableObject
    {
        [SerializeField, Tooltip("The call type's id in Economy.json, such as type.emergency.")]
        private string id;
        [SerializeField] private CallCardStyle cardStyle;

        public string Id => id;
        public CallCardStyle CardStyle => cardStyle;
    }
}
