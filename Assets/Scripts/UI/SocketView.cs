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
        private CableController pointerCable;
        private bool open = true;

        public event Action<SocketView> Clicked;
        public event Action<SocketView, bool> HoverChanged;

        public int Row => row;
        public int Column => column;
        public string Address => $"{(char)('A' + row)}{column + 1}";
        public RectTransform PlugAnchor => plugAnchor;
        public bool IsOccupied => occupyingCable != null;

        /// <summary>A closed socket (D42) is hidden, ignores hover and clicks, and takes no plug.</summary>
        public bool IsOpen => open;
        public bool IsNavigationHighlighted => isActiveAndEnabled && open && pointerHovered && pointerCable != null;
        public CableController OccupyingCable => occupyingCable;

        public void SetCallConnected(bool connected) => Graphic.SetCallConnected(connected && IsOccupied);

        /// <summary>The jack's lamp (D58): the call in hand's caller blinks; a marked jack glows; a waiting caller is dark.</summary>
        public void SetLamp(SocketLamp lamp) => Graphic.SetLamp(lamp);
        public void FlareLamp() => Graphic.FlareLamp();
        public SocketLamp Lamp => Graphic.Lamp;

        public bool TryOccupy(CableController cable)
        {
            if (cable == null || IsOccupied || !open) return false;
            occupyingCable = cable;
            RefreshHighlight();
            return true;
        }

        public void Release(CableController cable)
        {
            if (occupyingCable == cable)
            {
                occupyingCable = null;
                Graphic.SetCallConnected(false);
            }
            RefreshHighlight();
        }

        /// <summary>Opens or closes the socket; a plug already in it stays until it is pulled.</summary>
        public void SetOpen(bool value)
        {
            if (open == value) return;
            open = value;
            if (!open)
            {
                pointerHovered = false;
                pointerCable = null;
            }
            Graphic.SetDimmed(!open);
            Graphic.enabled = open;
            Graphic.raycastTarget = open;
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
            if (open && eventData.button == PointerEventData.InputButton.Left) Clicked?.Invoke(this);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!open) return;
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

        public void SetPointerState(bool hovered, CableController heldCable)
        {
            pointerHovered = open && hovered;
            pointerCable = open ? heldCable : null;
            RefreshHighlight();
        }

        private void RefreshHighlight() => Graphic.SetHighlighted(pointerHovered && pointerCable != null,
            IsOccupied && occupyingCable != pointerCable);

        private void OnDisable()
        {
            pointerHovered = false;
            pointerCable = null;
            if (socketGraphic != null) socketGraphic.SetHighlighted(false);
            if (socketGraphic != null) socketGraphic.SetCallConnected(false);
        }
    }
}
