using UnityEngine;
using UnityEngine.UI;

namespace Switchboard.UI
{
    public sealed class PhoneBookContactRow : MonoBehaviour
    {
        [SerializeField] private Text contactName;
        [SerializeField] private Text socketAddress;

        public void Configure(Text nameLabel, Text addressLabel)
        {
            contactName = nameLabel;
            socketAddress = addressLabel;
        }

        public void Show(PhoneBookContact contact, int fontSize)
        {
            contactName.text = contact.name;
            socketAddress.text = contact.socket;
            contactName.fontSize = socketAddress.fontSize = fontSize;
        }
    }
}
