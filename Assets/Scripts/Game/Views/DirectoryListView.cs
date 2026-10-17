using System.Text;
using Switchboard.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Switchboard.Game
{
    /// <summary>A temporary directory until the book in Step 3: a sorted list toggled by a button or the D key.</summary>
    public sealed class DirectoryListView : MonoBehaviour
    {
        [SerializeField] private GameObject overlay;
        [SerializeField] private TMP_Text entriesLabel;
        [SerializeField] private Button toggleButton;
        [SerializeField] private Button closeArea;

        public bool IsOpen => overlay.activeSelf;

        public void Bind(PhoneDirectory directory)
        {
            var text = new StringBuilder();
            foreach (IDirectoryEntry entry in directory.ListedEntries)
            {
                if (text.Length > 0) text.Append('\n');
                text.Append(entry.Person.ListingName()).Append(" — ").Append(entry.JackLabel);
            }
            entriesLabel.text = text.ToString();
        }

        public void Toggle() => overlay.SetActive(!overlay.activeSelf);
        public void Close() => overlay.SetActive(false);

        private void OnEnable()
        {
            toggleButton.onClick.AddListener(Toggle);
            closeArea.onClick.AddListener(Close);
        }

        private void OnDisable()
        {
            toggleButton.onClick.RemoveListener(Toggle);
            closeArea.onClick.RemoveListener(Close);
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.dKey.wasPressedThisFrame) Toggle();
        }
    }
}
