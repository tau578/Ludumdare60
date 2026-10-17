using UnityEngine;
using UnityEngine.UI;

namespace Switchboard.UI
{
    [DisallowMultipleComponent, RequireComponent(typeof(Canvas), typeof(GraphicRaycaster))]
    public sealed class PhoneBookView : MonoBehaviour
    {
        [SerializeField] private Button closedBook;
        [SerializeField] private Button openBook;

        public bool IsOpen { get; private set; }

        public void Configure(Button closed, Button opened)
        {
            closedBook = closed;
            openBook = opened;
            SetOpen(false);
        }

        private void Awake()
        {
            SwitchboardUISorting.SetLayer(gameObject, SwitchboardUISorting.Book);
            closedBook.onClick.AddListener(Toggle);
            openBook.onClick.AddListener(Toggle);
            SetOpen(false);
        }

        private void OnDestroy()
        {
            if (closedBook != null) closedBook.onClick.RemoveListener(Toggle);
            if (openBook != null) openBook.onClick.RemoveListener(Toggle);
        }

        public void Toggle() => SetOpen(!IsOpen);

        public void SetOpen(bool value)
        {
            IsOpen = value;
            closedBook.gameObject.SetActive(!value);
            openBook.gameObject.SetActive(value);
        }
    }
}
