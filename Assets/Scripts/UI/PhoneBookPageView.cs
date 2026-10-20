using System.Collections.Generic;
using Switchboard.Content;
using UnityEngine;
using UnityEngine.UI;

namespace Switchboard.UI
{
    public sealed class PhoneBookPageView : MonoBehaviour
    {
        [SerializeField] private RectTransform entries;
        [SerializeField] private PhoneBookContactRow rowTemplate;
        [SerializeField] private Text pageNumber;
        [SerializeField] private Text emptyMessage;
        private readonly List<PhoneBookContactRow> rows = new List<PhoneBookContactRow>();
        private RectTransform jackDivider;

        public void Configure(RectTransform list, PhoneBookContactRow template, Text number, Text empty)
        {
            entries = list;
            rowTemplate = template;
            pageNumber = number;
            emptyMessage = empty;
        }

        /// <summary>The shown row with this jack, or null.</summary>
        public RectTransform RowWithJack(string jack)
        {
            foreach (PhoneBookContactRow row in rows)
                if (row != null && row.gameObject.activeSelf && row.Jack == jack) return (RectTransform)row.transform;
            return null;
        }

        /// <summary>The shown row under a screen point, or null.</summary>
        public PhoneBookContactRow RowAt(Vector2 screenPoint, Camera camera)
        {
            foreach (PhoneBookContactRow row in rows)
                if (row != null && row.gameObject.activeInHierarchy && row.Jack.Length > 0 &&
                    RectTransformUtility.RectangleContainsScreenPoint((RectTransform)row.transform, screenPoint, camera))
                    return row;
            return null;
        }

        public void Render(BookCatalog book, int pageIndex, string message = null)
        {
            if (jackDivider == null)
            {
                var line = new GameObject("Jack column red margin", typeof(RectTransform), typeof(CanvasRenderer))
                    .AddComponent<Image>();
                line.transform.SetParent(transform, false);
                line.transform.SetAsFirstSibling();
                line.color = new Color32(210, 112, 112, 100);
                line.raycastTarget = false;
                jackDivider = line.rectTransform;
                jackDivider.anchorMin = new Vector2(.5f, 0);
                jackDivider.anchorMax = new Vector2(.5f, 1);
                jackDivider.sizeDelta = new Vector2(2, -24);
            }
            // Keep the margin just before the Jack column on either page, independently of mirrored paper art.
            jackDivider.anchoredPosition = new Vector2(entries.anchoredPosition.x + entries.rect.width * .32f, 0);
            IReadOnlyList<BookEntry> contacts = book.Entries;
            int perPage = book.ContactsPerPage;
            int start = pageIndex * perPage;
            int count = Mathf.Clamp(contacts.Count - start, 0, perPage);
            while (rows.Count < count)
            {
                var row = Instantiate(rowTemplate, entries);
                row.name = $"Contact_{rows.Count + 1:00}";
                rows.Add(row);
            }
            float spacing = entries.rect.height / perPage;
            // Two lines per row: the name, then profession and address.
            int fontSize = Mathf.Clamp(Mathf.RoundToInt(spacing * .41f), 10, 22);
            for (int i = 0; i < rows.Count; i++)
            {
                rows[i].gameObject.SetActive(i < count);
                if (i >= count) continue;
                var rect = (RectTransform)rows[i].transform;
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1);
                rect.pivot = new Vector2(.5f, 1);
                rect.sizeDelta = new Vector2(entries.rect.width, spacing);
                rect.anchoredPosition = new Vector2(0, -i * spacing);
                rows[i].Show(contacts[start + i], fontSize);
            }
            pageNumber.text = (pageIndex + 1).ToString();
            emptyMessage.text = message ?? "No contacts";
            emptyMessage.gameObject.SetActive(pageIndex == 0 && contacts.Count == 0);
        }
    }
}
