using System.Collections.Generic;
using Switchboard.Cables;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Switchboard.UI
{
    public enum CableCursorMode { Pointing, Grab, Plug }

    /// <summary>One held cable, explicit pickup from the tray, and one LMB input path.</summary>
    [DisallowMultipleComponent]
    public sealed class CableInputController : MonoBehaviour
    {
        public const int MaxCables = 6;
        private const float TraySlotSpacing = 270;
        private const float TrayUsableWidth = 830;
        private const float TraySlotY = -140;
        private const float TraySlotWidth = 252;
        private const float TraySlotHeight = 94.5f;
        private const float TrayPreviewScale = .45f;
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

        /// <summary>Adds a cable at runtime by copying the last one and its tray slot; null when the tray is full.</summary>
        public CableController AddCable(Color color)
        {
            if (cables.Length == 0 || cables.Length >= MaxCables) return null;
            CableController source = cables[cables.Length - 1];
            StoredCableView sourceSlot = source != null ? source.TrayView : null;
            if (sourceSlot == null) return null;
            if (hoveredStoredCable == sourceSlot) hoveredStoredCable = null;
            sourceSlot.SetHighlighted(false);

            // Copied under an inactive holder, so the copy's OnEnable can't bind the source's tray slot.
            var holder = new GameObject("CableCopy");
            holder.SetActive(false);
            CableController cable = Instantiate(source, holder.transform, false);
            StoredCableView slot = Instantiate(sourceSlot, sourceSlot.transform.parent, false);
            foreach (var outline in slot.GetComponentsInChildren<Outline>(true)) DestroyCopyPart(outline);
            int number = cables.Length + 1;
            cable.name = $"Cable_{number:00}";
            slot.name = $"StoredCable_{number:00}";
            cable.SetTrayCableView(slot);
            cable.SetCableColor(color);
            cable.transform.SetParent(source.transform.parent, false);
            DestroyCopyPart(holder);

            var inventory = new CableController[cables.Length + 1];
            cables.CopyTo(inventory, 0);
            inventory[cables.Length] = cable;
            cables = inventory;
            LayoutTray();
            return cable;
        }

        private static void DestroyCopyPart(Object part)
        {
            if (Application.isPlaying) Destroy(part);
            else DestroyImmediate(part);
        }

        public static float TraySpacing(int count) =>
            count > 0 ? Mathf.Min(TraySlotSpacing, TrayUsableWidth / count) : TraySlotSpacing;

        public static Vector2 TraySlotPosition(int index, int count) =>
            new Vector2((index - (count - 1) * .5f) * TraySpacing(count), TraySlotY);

        public static Vector2 TraySlotSize(int count) =>
            new Vector2(TraySlotWidth * TraySpacing(count) / TraySlotSpacing, TraySlotHeight);

        public static float TrayPreviewScaleFor(int count) => TrayPreviewScale * TraySpacing(count) / TraySlotSpacing;

        private void LayoutTray()
        {
            for (int i = 0; i < cables.Length; i++)
            {
                StoredCableView slot = cables[i] != null ? cables[i].TrayView : null;
                if (slot == null) continue;
                var rect = (RectTransform)slot.transform;
                rect.anchoredPosition = TraySlotPosition(i, cables.Length);
                rect.sizeDelta = TraySlotSize(cables.Length);
                Transform preview = rect.Find("Preview");
                if (preview != null) preview.localScale = Vector3.one * TrayPreviewScaleFor(cables.Length);
            }
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
            SocketView hit = target != null ? target.GetComponentInParent<SocketView>() : null;
            // A closed socket (D42) is no target: it doesn't highlight, and a click there only bounces the held cord.
            SocketView socket = hit != null && hit.IsOpen ? hit : null;
            StoredCableView stored = target != null ? target.GetComponentInParent<StoredCableView>() : null;
            bool overTray = target != null && tray != null && target.transform.IsChildOf(tray);
            bool overCall = target != null && target.GetComponentInParent<Switchboard.Game.CallCardView>() != null;
            if (leftPressed)
            {
                if (socket != null) HandleSocketClick(socket);
                else if (hit != null) ActiveCable?.PlayBlockedFeedback();
                else if (ActiveCable != null && !overCall)
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
            if (!socket.IsOpen)
            {
                ActiveCable?.PlayBlockedFeedback();
                return;
            }
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
            if (hoveredSocket != socket && hoveredSocket != null) hoveredSocket.SetPointerState(false, selected);
            hoveredSocket = socket;
            if (hoveredSocket != null) hoveredSocket.SetPointerState(true, selected);
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
            if (hoveredSocket != null) hoveredSocket.SetPointerState(false, null);
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
