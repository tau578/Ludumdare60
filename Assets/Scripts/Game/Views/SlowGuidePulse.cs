using UnityEngine;
using UnityEngine.UI;
using Switchboard.UI;

namespace Switchboard.Game
{
    /// <summary>A slow yellow silhouette outline, independent of hover feedback and object scale.</summary>
    public sealed class SlowGuidePulse : MonoBehaviour
    {
        private Outline outline;
        private SocketHaloGraphic portraitHalo;
        private Image silhouetteHalo;
        private Material silhouetteMaterial;
        private float elapsed;
        private bool hovered;
        public bool IsShowing { get; private set; }

        public void UseSilhouetteHalo()
        {
            if (silhouetteHalo != null || !TryGetComponent<Image>(out var source)) return;
            var child = new GameObject("Book guide halo", typeof(RectTransform), typeof(CanvasRenderer));
            child.hideFlags = HideFlags.DontSave;
            child.layer = gameObject.layer;
            var rect = (RectTransform)child.transform;
            var original = source.rectTransform;
            // Draw behind the book, keeping its exact sprite silhouette and layout.
            rect.SetParent(original.parent, false);
            rect.SetSiblingIndex(original.GetSiblingIndex());
            rect.anchorMin = original.anchorMin;
            rect.anchorMax = original.anchorMax;
            rect.pivot = original.pivot;
            rect.sizeDelta = original.sizeDelta;
            rect.anchoredPosition3D = original.anchoredPosition3D;
            rect.localRotation = original.localRotation;
            rect.localScale = original.localScale;
            silhouetteHalo = child.AddComponent<Image>();
            silhouetteHalo.sprite = source.sprite;
            silhouetteHalo.preserveAspect = source.preserveAspect;
            silhouetteHalo.raycastTarget = false;
            silhouetteMaterial = new Material(Resources.Load<Shader>("GuideSilhouette")) { hideFlags = HideFlags.HideAndDontSave };
            silhouetteHalo.material = silhouetteMaterial;
            child.AddComponent<SoftGuideOutline>();
            child.SetActive(IsShowing || hovered);
            if (outline != null) outline.enabled = false;
        }

        public void UsePortraitHalo()
        {
            if (portraitHalo != null) return;
            // UI Outline samples the dark portrait RGB; this untextured ring stays bright.
            var child = new GameObject("Portrait guide halo", typeof(RectTransform), typeof(CanvasRenderer));
            child.hideFlags = HideFlags.DontSave;
            child.layer = gameObject.layer;
            var rect = (RectTransform)child.transform;
            rect.SetParent(transform, false);
            rect.SetAsFirstSibling();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = new Vector2(24, 24);
            portraitHalo = child.AddComponent<SocketHaloGraphic>();
            portraitHalo.raycastTarget = false;
            portraitHalo.Intensity = 1.4f;
            child.SetActive(IsShowing);
            if (outline != null) outline.enabled = false;
        }

        public void SetShowing(bool value)
        {
            if (IsShowing == value) return;
            IsShowing = value;
            if (outline == null && portraitHalo == null && silhouetteHalo == null && value)
            {
                outline = gameObject.AddComponent<Outline>();
                outline.effectDistance = new Vector2(4, -4);
                outline.useGraphicAlpha = true;
            }
            if (outline != null) outline.enabled = value && portraitHalo == null && silhouetteHalo == null;
            if (portraitHalo != null) portraitHalo.gameObject.SetActive(value);
            if (silhouetteHalo != null) silhouetteHalo.gameObject.SetActive(value || hovered);
            if (value) { elapsed = portraitHalo != null || silhouetteHalo != null ? 1.2f : .6f; Refresh(); }
        }

        public void SetHovered(bool value)
        {
            hovered = value;
            if (silhouetteHalo == null) return;
            silhouetteHalo.gameObject.SetActive(IsShowing || hovered);
            Refresh();
        }

        private void Update()
        {
            if (!IsShowing || hovered) return;
            elapsed += Time.unscaledDeltaTime;
            Refresh();
        }

        private void Refresh()
        {
            float wave = .5f - .5f * Mathf.Cos(elapsed * Mathf.PI * 2 / 2.4f);
            if (portraitHalo != null)
                portraitHalo.color = new Color(1f, .83f, .20f, Mathf.Lerp(.45f, 1f, wave));
            else if (silhouetteHalo != null)
                silhouetteMaterial.SetColor("_Color", new Color(1f, .83f, .20f, hovered ? 1f : Mathf.Lerp(.45f, 1f, wave)));
            else if (outline != null)
                outline.effectColor = new Color(1f, .83f, .20f, Mathf.Lerp(.22f, .85f, wave));
        }

        private void OnDisable() { SetHovered(false); SetShowing(false); }

        private void OnDestroy()
        {
            if (portraitHalo != null) Release(portraitHalo.gameObject);
            if (silhouetteHalo != null) Release(silhouetteHalo.gameObject);
            if (silhouetteMaterial != null) Release(silhouetteMaterial);
        }

        private static void Release(Object value)
        {
            if (Application.isPlaying) Destroy(value);
            else DestroyImmediate(value);
        }
    }
}
