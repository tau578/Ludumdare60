using Switchboard.Content;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Switchboard.UI
{
    /// <summary>One book entry (D53): the name, a smaller line with profession and address under it, and the jack last.</summary>
    public sealed class PhoneBookContactRow : MonoBehaviour, IPointerDownHandler
    {
        [SerializeField] private Text contactName;
        [SerializeField] private Text details;
        [SerializeField, FormerlySerializedAs("socketAddress")] private Text jack;

        public string Jack => jack != null ? jack.text : string.Empty;

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

        // Taking the press keeps the open book from flashing its pressed tint under the row.
        public void OnPointerDown(PointerEventData eventData) { }
    }
}
