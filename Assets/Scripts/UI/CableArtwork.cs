using UnityEngine;

namespace Switchboard.UI
{
    [CreateAssetMenu(menuName = "Switchboard/Cable artwork")]
    public sealed class CableArtwork : ScriptableObject
    {
        [System.Serializable]
        public struct Variant
        {
            public Color color;
            public Sprite stored, held, inserted;
        }

        [SerializeField] private Variant[] variants;
        public void Configure(Variant[] value) => variants = value;

        public Variant ForColor(Color color)
        {
            var best = default(Variant);
            float distance = float.MaxValue;
            if (variants == null) return best;
            Color.RGBToHSV(color, out float hue, out _, out _);
            foreach (var variant in variants)
            {
                Color.RGBToHSV(variant.color, out float candidate, out _, out _);
                float difference = Mathf.Abs(Mathf.DeltaAngle(hue * 360, candidate * 360));
                if (difference >= distance) continue;
                distance = difference; best = variant;
            }
            return best;
        }
    }
}
