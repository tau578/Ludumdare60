using Switchboard.Content;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Switchboard.UI
{
    /// <summary>
    /// One book entry (D53): the name, a smaller line with profession and address under it, and the jack last. It has no
    /// raycast target: a click lands on the open book, which finds the row under it (D58), and so does the hover tint.
    /// </summary>
    public sealed class PhoneBookContactRow : MonoBehaviour
    {
        /// <summary>A soft wash of the jack lamp's warm colour behind the text, so a clickable row reads as one.</summary>
        public static readonly Color HoverTint = new Color32(255, 208, 110, 80);
        [SerializeField] private Text contactName;
        [SerializeField] private Text details;
        [SerializeField, FormerlySerializedAs("socketAddress")] private Text jack;
        private Image hoverTint;

        public string Jack => jack != null ? jack.text : string.Empty;
        public bool IsHovered => hoverTint != null && hoverTint.gameObject.activeSelf;
        public Image HoverTintGraphic => hoverTint;

        public void SetHovered(bool value)
        {
            if (value == IsHovered) return;
            if (hoverTint == null)
            {
                var child = new GameObject("Hover tint", typeof(RectTransform), typeof(CanvasRenderer));
                child.hideFlags = HideFlags.DontSave;
                child.layer = gameObject.layer;
                var rect = (RectTransform)child.transform;
                rect.SetParent(transform, false);
                rect.SetAsFirstSibling();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.sizeDelta = new Vector2(-4, -2);
                hoverTint = child.AddComponent<Image>();
                hoverTint.raycastTarget = false;
                hoverTint.color = HoverTint;
            }
            hoverTint.gameObject.SetActive(value);
        }

        private void OnDestroy()
        {
            if (hoverTint == null) return;
            if (Application.isPlaying) Destroy(hoverTint.gameObject);
            else DestroyImmediate(hoverTint.gameObject);
        }

        public void Configure(Text nameLabel, Text detailsLabel, Text jackLabel)
        {
            contactName = nameLabel;
            details = detailsLabel;
            jack = jackLabel;
        }

        public void Show(BookEntry entry, int fontSize)
        {
            contactName.text = entry.Name;
            jack.text = entry.Jack;
            contactName.resizeTextMaxSize = jack.fontSize = fontSize;
            if (details == null) return;
            details.text = Details(entry);
            details.resizeTextMaxSize = Mathf.Max(8, Mathf.RoundToInt(fontSize * .75f));
        }

        public static string Details(BookEntry entry)
        {
            if (entry.Profession.Length == 0) return entry.Address;
            return entry.Address.Length == 0 ? entry.Profession : entry.Profession + ", " + entry.Address;
        }
    }
}
