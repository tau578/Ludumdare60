using System;
using UnityEngine;

namespace Switchboard.Content
{
    /// <summary>How a caller type's cards look. Presentation only, so it stays out of the domain interfaces.</summary>
    [Serializable]
    public struct CallCardStyle
    {
        [Tooltip("Shown on the card, e.g. EMERGENCY. Empty = no badge.")]
        public string badgeText;
        [Tooltip("Tints the badge, the ringing halo and the rate badge outline. Alpha 0 = the card's own colours.")]
        public Color accent;

        public bool HasBadge => !string.IsNullOrEmpty(badgeText);
        public bool HasAccent => accent.a > 0;
    }
}
