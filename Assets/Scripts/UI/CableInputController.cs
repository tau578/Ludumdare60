using System.Collections.Generic;
using Switchboard.Cables;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Switchboard.UI
{
    public enum CableCursorMode { Pointing, Grab, Plug }

    /// <summary>One held cable, explicit pickup from the tray, and one LMB input path.</summary>
    [DisallowMultipleComponent]
    public sealed class CableInputController : MonoBehaviour
    {
        [SerializeField] private RectTransform tray;
        [SerializeField] private HandCursorGraphic handCursor;
        [SerializeField] private CableController[] cables = new CableController[0];
        private readonly List<RaycastResult> hits = new List<RaycastResult>();
        private PointerEventData pointerEvent;
        private EventSystem eventSystem;
        private CableController heldCable;
        private SocketView hoveredSocket;
        private StoredCableView hoveredStoredCable;
        private bool cursorWasVisible;
        private bool ownsCursor;
        public CableController ActiveCable => Usable(heldCable) ? heldCable : null;
        public CableCursorMode CursorMode { get; private set; }
        public int AvailableCableCount
        {
            get
            {
                int count = 0;
                foreach (var cable in cables) if (Usable(cable) && cable.IsInTray) count++;
                return count;
            }
        }

        public void Configure(RectTransform cableTray, CableController[] inventory, HandCursorGraphic cursor)
        {
            tray = cableTray;
            cables = inventory;
            handCursor = cursor;
        }

        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            heldCable = null;
            foreach (var cable in cables)
                if (Usable(cable) && (cable.Phase == CablePhase.Held || cable.Phase == CablePhase.OneEndConnected)) { heldCable = cable; break; }
            cursorWasVisible = Cursor.visible;
            ownsCursor = true;
        }

        private void Update()
        {
            if (Mouse.current == null || !Application.isFocused) { HidePointers(); return; }
            ProcessPointer(Mouse.current.position.ReadValue(), Mouse.current.leftButton.wasPressedThisFrame);
        }

        // Shared by real input and integration checks, including UI hit testing.
        public void ProcessPointer(Vector2 position, bool leftPressed)
        {
            GameObject target = Raycast(position);
            SocketView socket = target != null ? target.GetComponentInParent<SocketView>() : null;
            StoredCableView stored = target != null ? target.GetComponentInParent<StoredCableView>() : null;
            bool overTray = target != null && tray != null && target.transform.IsChildOf(tray);
            if (leftPressed)
            {
                if (socket != null) HandleSocketClick(socket);
                else if (ActiveCable != null)
                {
                    if (heldCable.Phase == CablePhase.Held || overTray)
                    {
                        if (heldCable.TryReturnToTray()) heldCable = null;
                    }
                    else if (heldCable.TryRestoreConnection()) heldCable = null;
                }
                else if (stored != null && stored.Available && Owns(stored.Cable) && stored.Cable.TryPickUp())
                    heldCable = stored.Cable;
            }
            if (ActiveCable != null && (heldCable.Phase == CablePhase.Ready || heldCable.Phase == CablePhase.Connected)) heldCable = null;
            UpdatePointers(position, socket, stored);
        }

        private GameObject Raycast(Vector2 position)
        {
            if (EventSystem.current == null) return null;
            if (eventSystem != EventSystem.current || pointerEvent == null)
            {
                eventSystem = EventSystem.current;
                pointerEvent = new PointerEventData(eventSystem);
            }
            pointerEvent.Reset();
            pointerEvent.position = position;
            hits.Clear();
            eventSystem.RaycastAll(pointerEvent, hits);
            return hits.Count > 0 ? hits[0].gameObject : null;
        }

        public void HandleSocketClick(SocketView socket)
        {
            if (socket == null || !socket.isActiveAndEnabled) return;
            var owner = socket.OccupyingCable;
            if (owner != null)
            {
                if (!Owns(owner) || (ActiveCable != null && heldCable != owner))
                {
                    ActiveCable?.PlayBlockedFeedback();
                    return;
                }
                if (owner.TryRemove(socket)) heldCable = owner.IsInTray ? null : owner;
            }
            else if (ActiveCable != null && heldCable.TryInsert(socket) && heldCable.Phase == CablePhase.Connected)
                heldCable = null;
        }

        private void UpdatePointers(Vector2 position, SocketView socket, StoredCableView stored)
        {
            var selected = ActiveCable;
            if (hoveredSocket != socket && hoveredSocket != null) hoveredSocket.SetPointerState(false, selected != null);
            hoveredSocket = socket;
            if (hoveredSocket != null) hoveredSocket.SetPointerState(true, selected != null);
            CursorMode = selected != null ? CableCursorMode.Plug :
                stored != null && stored.Available && Owns(stored.Cable) ? CableCursorMode.Grab : CableCursorMode.Pointing;
            var highlightedStock = CursorMode == CableCursorMode.Grab ? stored : null;
            if (hoveredStoredCable != highlightedStock && hoveredStoredCable != null) hoveredStoredCable.SetHighlighted(false);
            hoveredStoredCable = highlightedStock;
            if (hoveredStoredCable != null) hoveredStoredCable.SetHighlighted(true);
            if (selected != null) selected.transform.SetAsLastSibling();
            foreach (var cable in cables)
            {
                if (!Usable(cable)) continue;
                cable.UpdatePointer(position, cable == selected);
            }
            bool handVisible = false;
            if (handCursor != null)
            {
                var area = (RectTransform)handCursor.transform.parent;
                var canvas = area.GetComponentInParent<Canvas>();
                var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
                handVisible = selected == null && RectTransformUtility.RectangleContainsScreenPoint(area, position, camera);
                handCursor.gameObject.SetActive(handVisible);
                if (handVisible)
                {
                    SwitchboardUISorting.SetLayer(handCursor.gameObject, SwitchboardUISorting.Cursor);
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(area, position, camera, out var local);
                    handCursor.rectTransform.anchoredPosition = local;
                    handCursor.Grabbing = CursorMode == CableCursorMode.Grab;
                    handCursor.transform.SetAsLastSibling();
                }
            }
            if (ownsCursor) Cursor.visible = !handVisible && (selected == null || !selected.IsPointerVisible);
        }

        private bool Owns(CableController owner)
        {
            foreach (var cable in cables) if (cable == owner && Usable(cable)) return true;
            return false;
        }
        private static bool Usable(CableController cable) => cable != null && cable.isActiveAndEnabled;

        private void HidePointers()
        {
            if (hoveredSocket != null) hoveredSocket.SetPointerState(false, false);
            hoveredSocket = null;
            if (hoveredStoredCable != null) hoveredStoredCable.SetHighlighted(false);
            hoveredStoredCable = null;
            foreach (var cable in cables) if (cable != null) cable.HidePointer();
            if (handCursor != null) handCursor.gameObject.SetActive(false);
            if (ownsCursor) Cursor.visible = cursorWasVisible;
        }
        private void OnApplicationFocus(bool focused) { if (!focused) HidePointers(); }
        private void OnDisable() { HidePointers(); ownsCursor = false; heldCable = null; }
    }
}
