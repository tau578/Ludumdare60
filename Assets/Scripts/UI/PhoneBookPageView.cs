using System;
using System.Collections.Generic;
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
        private bool tappable;

        public event Action<PhoneBookContact> ContactTapped;

        public void Configure(RectTransform list, PhoneBookContactRow template, Text number, Text empty)
        {
            entries = list;
            rowTemplate = template;
            pageNumber = number;
            emptyMessage = empty;
        }

        public void Render(PhoneBookConfig config, int pageIndex, string message = null)
        {
            int start = pageIndex * config.contactsPerPage;
            int count = Mathf.Clamp(config.contacts.Length - start, 0, config.contactsPerPage);
            while (rows.Count < count)
            {
                var row = Instantiate(rowTemplate, entries);
                row.name = $"Contact_{rows.Count + 1:00}";
                row.Tapped += OnRowTapped;
                row.SetTappable(tappable);
                rows.Add(row);
            }
            float spacing = entries.rect.height / config.contactsPerPage;
            int fontSize = config.contactsPerPage > 8 ? 18 : 22;
            for (int i = 0; i < rows.Count; i++)
            {
                rows[i].gameObject.SetActive(i < count);
                if (i >= count) continue;
                var rect = (RectTransform)rows[i].transform;
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1);
                rect.pivot = new Vector2(.5f, 1);
                rect.sizeDelta = new Vector2(entries.rect.width, spacing);
                rect.anchoredPosition = new Vector2(0, -i * spacing);
                rows[i].Show(config.contacts[start + i], fontSize);
            }
            pageNumber.text = (pageIndex + 1).ToString();
            emptyMessage.text = message ?? "No contacts";
            emptyMessage.gameObject.SetActive(pageIndex == 0 && config.contacts.Length == 0);
        }

        public void SetTappable(bool value)
        {
            tappable = value;
            foreach (var row in rows) row.SetTappable(value);
        }

        private void OnRowTapped(PhoneBookContact contact) => ContactTapped?.Invoke(contact);

        private void OnDestroy()
        {
            foreach (var row in rows) if (row != null) row.Tapped -= OnRowTapped;
        }
    }
}
