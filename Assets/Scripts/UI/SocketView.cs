using UnityEngine;
using UnityEngine.EventSystems;

namespace Switchboard.UI
{
    /// <summary>Socket identity and attachment point for a future cable plug.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SocketGraphic))]
    public sealed class SocketView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField, Min(0)] private int row;
        [SerializeField, Min(0)] private int column;
        [SerializeField] private RectTransform plugAnchor;
        [SerializeField] private SocketGraphic socketGraphic;

        public int Row => row;
        public int Column => column;
        public string Address => $"{(char)('A' + row)}{column + 1}";
        public RectTransform PlugAnchor => plugAnchor;

        public void Configure(int rowIndex, int columnIndex, RectTransform anchor)
        {
            row = Mathf.Clamp(rowIndex, 0, 25);
            column = Mathf.Max(0, columnIndex);
            plugAnchor = anchor;
            socketGraphic = GetComponent<SocketGraphic>();
            gameObject.name = $"Socket_{Address}";
        }

        public void OnPointerEnter(PointerEventData eventData) => Graphic.SetHighlighted(true);
        public void OnPointerExit(PointerEventData eventData) => Graphic.SetHighlighted(false);

        private SocketGraphic Graphic => socketGraphic != null
            ? socketGraphic : socketGraphic = GetComponent<SocketGraphic>();

        private void OnDisable()
        {
            if (socketGraphic != null) socketGraphic.SetHighlighted(false);
        }
    }
}
