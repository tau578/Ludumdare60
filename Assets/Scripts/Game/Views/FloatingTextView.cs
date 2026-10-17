using TMPro;
using UnityEngine;

namespace Switchboard.Game
{
    /// <summary>A reused popup, such as "+10¢": it rises from where it sits in the prefab and fades out.</summary>
    public sealed class FloatingTextView : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private CanvasGroup group;
        [SerializeField, Min(0.1f)] private float seconds = 1f;
        [SerializeField] private float rise = 48f;
        [SerializeField, Range(0, 1), Tooltip("The share of the time it stays fully visible before fading.")]
        private float holdShare = 0.4f;

        private RectTransform rect;
        private Vector2 start;
        private float elapsed;

        public void Show(string text)
        {
            if (rect == null)
            {
                rect = (RectTransform)transform;
                start = rect.anchoredPosition;
            }
            label.text = text;
            elapsed = 0;
            gameObject.SetActive(true);
            Apply(0);
        }

        public void Hide() => gameObject.SetActive(false);

        private void Update()
        {
            elapsed += Time.deltaTime;
            if (elapsed >= seconds) Hide();
            else Apply(elapsed / seconds);
        }

        private void Apply(float t)
        {
            rect.anchoredPosition = start + Vector2.up * (rise * t);
            group.alpha = t <= holdShare ? 1f : 1f - (t - holdShare) / (1f - holdShare);
        }
    }
}
