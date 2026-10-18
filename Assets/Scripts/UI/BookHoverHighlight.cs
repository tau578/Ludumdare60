using Switchboard.Game;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Switchboard.UI
{
    /// <summary>Steady book silhouette on hover, sharing the tutorial's glow.</summary>
    [DisallowMultipleComponent]
    public sealed class BookHoverHighlight : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private SlowGuidePulse glow;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!isActiveAndEnabled) return;
            if (glow == null) glow = GetComponent<SlowGuidePulse>();
            if (glow == null) glow = gameObject.AddComponent<SlowGuidePulse>();
            glow.UseSilhouetteHalo();
            glow.SetHovered(true);
        }

        public void OnPointerExit(PointerEventData eventData) => ClearHover();
        private void OnDisable() => ClearHover();
        private void ClearHover() { if (glow != null) glow.SetHovered(false); }
    }
}
