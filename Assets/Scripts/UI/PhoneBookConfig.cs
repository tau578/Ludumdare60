using System;
using System.Linq;
using UnityEngine;

namespace Switchboard.UI
{
    [Serializable]
    public sealed class PhoneBookConfig
    {
        public int contactsPerPage = 6;
        public PhoneBookContact[] contacts = Array.Empty<PhoneBookContact>();

        public static PhoneBookConfig Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("Phone book JSON is empty.");
            var config = JsonUtility.FromJson<PhoneBookConfig>(json);
            if (config == null) throw new ArgumentException("Phone book JSON must contain an object.");
            if (config.contactsPerPage < 1 || config.contactsPerPage > 12)
                throw new ArgumentException("contactsPerPage must be between 1 and 12.");
            if (config.contacts == null) config.contacts = Array.Empty<PhoneBookContact>();
            for (int i = 0; i < config.contacts.Length; i++)
            {
                var contact = config.contacts[i];
                if (contact == null || string.IsNullOrWhiteSpace(contact.name) || string.IsNullOrWhiteSpace(contact.socket))
                    throw new ArgumentException($"Contact {i + 1} requires name and socket.");
                contact.name = contact.name.Trim();
                int comma = contact.name.IndexOf(',');
                if (!contact.IsEmergencyService && comma >= 0)
                    contact.name = (contact.name.Substring(comma + 1).Trim() + " " + contact.name.Substring(0, comma).Trim()).Trim();
                contact.socket = contact.socket.Trim().ToUpperInvariant();
                if (!System.Text.RegularExpressions.Regex.IsMatch(contact.socket, @"^[A-Z][1-9][0-9]*$"))
                    throw new ArgumentException($"Contact {i + 1} has an invalid socket coordinate: {contact.socket}.");
            }
            // Keep services in their authored order, followed by people in first-name order.
            config.contacts = config.contacts.OrderBy(contact => contact.IsEmergencyService ? 0 : 1)
                .ThenBy(contact => contact.IsEmergencyService ? string.Empty : contact.name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            return config;
        }
    }

    [Serializable]
    public sealed class PhoneBookContact
    {
        public string name;
        public string socket;
        public string[] tags = Array.Empty<string>();
        public bool IsEmergencyService => tags != null && Array.IndexOf(tags, "emergency-service") >= 0;
    }
}
