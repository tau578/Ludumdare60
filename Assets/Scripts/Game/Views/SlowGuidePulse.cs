using UnityEngine;
using UnityEngine.UI;

namespace Switchboard.Game
{
    /// <summary>A slow yellow silhouette outline, independent of hover feedback and object scale.</summary>
    public sealed class SlowGuidePulse : MonoBehaviour
    {
        private Outline outline;
        private float elapsed;
        public bool IsShowing { get; private set; }

        public void SetShowing(bool value)
        {
            if (IsShowing == value) return;
            IsShowing = value;
            if (outline == null && value)
            {
                outline = gameObject.AddComponent<Outline>();
                outline.effectDistance = new Vector2(4, -4);
                outline.useGraphicAlpha = true;
            }
            if (outline != null) outline.enabled = value;
            if (value) { elapsed = .6f; Refresh(); }
        }

        private void Update()
        {
            if (!IsShowing) return;
            elapsed += Time.unscaledDeltaTime;
            Refresh();
        }

        private void Refresh()
        {
            float wave = .5f - .5f * Mathf.Cos(elapsed * Mathf.PI * 2 / 2.4f);
            outline.effectColor = new Color(1f, .83f, .20f, Mathf.Lerp(.22f, .85f, wave));
        }

        private void OnDisable() => SetShowing(false);
    }
}
