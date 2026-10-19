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

        public void Configure(RectTransform list, PhoneBookContactRow template, Text number, Text empty)
        {
            entries = list;
            rowTemplate = template;
            pageNumber = number;
            emptyMessage = empty;
        }

        public void Render(BookCatalog book, int pageIndex, string message = null)
        {
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
