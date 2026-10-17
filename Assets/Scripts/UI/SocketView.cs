using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Switchboard.UI
{
    /// <summary>Socket identity and attachment point for a future cable plug.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SocketGraphic))]
    public sealed class SocketView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField, Min(0)] private int row;
        [SerializeField, Min(0)] private int column;
        [SerializeField] private RectTransform plugAnchor;
        [SerializeField] private SocketGraphic socketGraphic;
        private CableController occupyingCable;
        private bool pointerHovered;
        private bool holdingPlug;

        public event Action<SocketView> Clicked;
        public event Action<SocketView, bool> HoverChanged;

        public int Row => row;
        public int Column => column;
        public string Address => $"{(char)('A' + row)}{column + 1}";
        public RectTransform PlugAnchor => plugAnchor;
        public bool IsOccupied => occupyingCable != null;
        public CableController OccupyingCable => occupyingCable;

        public bool TryOccupy(CableController cable)
        {
            if (cable == null || IsOccupied) return false;
            occupyingCable = cable;
            RefreshHighlight();
            return true;
        }

        public void Release(CableController cable)
        {
            if (occupyingCable == cable) occupyingCable = null;
            RefreshHighlight();
        }

        public void Configure(int rowIndex, int columnIndex, RectTransform anchor)
        {
            row = Mathf.Clamp(rowIndex, 0, 25);
            column = Mathf.Max(0, columnIndex);
            plugAnchor = anchor;
            socketGraphic = GetComponent<SocketGraphic>();
            gameObject.name = $"Socket_{Address}";
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left) Clicked?.Invoke(this);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            pointerHovered = true;
            RefreshHighlight();
            HoverChanged?.Invoke(this, true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            pointerHovered = false;
            RefreshHighlight();
            HoverChanged?.Invoke(this, false);
        }

        private SocketGraphic Graphic => socketGraphic != null
            ? socketGraphic : socketGraphic = GetComponent<SocketGraphic>();

        public void SetPointerState(bool hovered, bool plugOnCursor)
        {
            pointerHovered = hovered;
            holdingPlug = plugOnCursor;
            RefreshHighlight();
        }

        private void RefreshHighlight() => Graphic.SetHighlighted(pointerHovered, holdingPlug && IsOccupied);

        private void OnDisable()
        {
            pointerHovered = false;
            holdingPlug = false;
            if (socketGraphic != null) socketGraphic.SetHighlighted(false);
        }
    }
}
