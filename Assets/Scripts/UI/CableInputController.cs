using System.Collections.Generic;
using Switchboard.Cables;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Switchboard.UI
{
    public enum CableCursorMode { Pointing, Grab, Plug, PageTurn }

    /// <summary>One held cable, explicit pickup from the tray, and one LMB input path.</summary>
    [DisallowMultipleComponent]
    public sealed class CableInputController : MonoBehaviour
    {
        public const int MaxCables = 6;
        private const float TraySlotSpacing = 270;
        public const float TrayCentreX = 0;
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
        private CableController topCable;
        private bool stackDirty = true;
        private SocketView hoveredSocket;
        private StoredCableView hoveredStoredCable;
        private bool cursorWasVisible;
        private bool ownsCursor;
        private bool suspended;
        private int resumedFrame = -1;
        public CableController ActiveCable => Usable(heldCable) ? heldCable : null;
        public CableCursorMode CursorMode { get; private set; }

        /// <summary>
        /// While suspended (the pause menu, D56) clicks neither pick, plug nor drop, and a held cord stays held.
        /// The press that resumes is ignored too, so the Resume button can't plug the jack beneath it.
        /// </summary>
        public bool Suspended
        {
            get => suspended;
            set
            {
                if (suspended == value) return;
                suspended = value;
                if (value) HidePointers();
                else resumedFrame = Time.frameCount;
            }
        }
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
            stackDirty = true;
        }

        /// <summary>Called before session binding; only unused tray cords may be removed.</summary>
        public void LimitStartingCables(int count)
        {
            count = Mathf.Clamp(count, 1, MaxCables);
            if (count >= cables.Length) return;
            foreach (var cable in cables)
                if (cable != null && !cable.IsInTray)
                    throw new System.InvalidOperationException("Starting inventory must be configured before using cords.");
            for (int i = count; i < cables.Length; i++)
            {
                var cable = cables[i];
                if (cable == null) continue;
                if (cable.TrayView != null)
                {
                    cable.TrayView.gameObject.SetActive(false);
                    DestroyCopyPart(cable.TrayView.gameObject);
                }
                cable.gameObject.SetActive(false);
                DestroyCopyPart(cable.gameObject);
            }
            System.Array.Resize(ref cables, count);
            LayoutTray();
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
            stackDirty = true;
            LayoutTray();
            return cable;
        }

        private static void DestroyCopyPart(Object part)
        {
            if (Application.isPlaying) Destroy(part);
            else DestroyImmediate(part);
        }

        public static float TraySpacing(int count) => count > 3 ? 190f : TraySlotSpacing;

        public static Vector2 TraySlotPosition(int index, int count)
        {
            int row = index / 3;
            int columns = Mathf.Min(3, count - row * 3);
            float y = count > 3 ? -105f - row * 85f : TraySlotY;
            return new Vector2(TrayCentreX + (index % 3 - (columns - 1) * .5f) * TraySpacing(count), y);
        }

        public static Vector2 TraySlotSize(int count) =>
            new Vector2(TraySlotWidth, TraySlotHeight) * (TraySpacing(count) / TraySlotSpacing);

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
            cursorWasVisible = SystemPointer.Visible;
            ownsCursor = true;
            stackDirty = true;
        }

        private void LateUpdate()
        {
            if (stackDirty) Restack();
        }

        // The cord picked up last is the cord moved last: its wire goes over all the others (lead, 2026-10-20).
        private void RaiseToTop(CableController cable)
        {
            if (cable == topCable) return;
            topCable = cable;
            cable.transform.SetAsLastSibling();
            stackDirty = true;
        }

        private void Restack()
        {
            stackDirty = false;
            foreach (var cable in cables)
            {
                if (cable == null) continue;
                int rank = 0, index = cable.transform.GetSiblingIndex();
                foreach (var other in cables)
                    if (other != null && other != cable && other.transform.GetSiblingIndex() < index) rank++;
                cable.SetStackRank(rank);
            }
        }

        // Mouse, touch and pen alike (D28): a tap is a click, so the click-to-pick, click-to-plug model stays.
        private void Update()
        {
            // Pausing hid the pointers once; while paused the pause panel's volume patch owns the system pointer.
            if (suspended) return;
            Pointer pointer = Pointer.current;
            if (pointer == null || !Application.isFocused) { HidePointers(); return; }
            bool pressed = pointer.press.wasPressedThisFrame && Time.frameCount != resumedFrame;
            // A finger leaves no hover behind: between taps only a held cord stays where it was last put.
            bool hover = !(pointer is Touchscreen) || pressed || pointer.press.isPressed;
            if (!hover && ActiveCable == null) { HidePointers(); return; }
            ProcessPointer(pointer.position.ReadValue(), pressed, hover);
        }

        // Shared by real input and integration checks, including UI hit testing.
        public void ProcessPointer(Vector2 position, bool leftPressed) => ProcessPointer(position, leftPressed, true);

        /// <param name="hover">False for a touchscreen with no finger down: no hover halos, highlights or hand.</param>
        public void ProcessPointer(Vector2 position, bool leftPressed, bool hover)
        {
            if (suspended) { HidePointers(); return; }
            GameObject target = Raycast(position);
            ProcessTarget(target, hits.Count > 0 ? hits[0].module.eventCamera : null, position, leftPressed, hover);
        }

        /// <summary>What a click or hover does on this top raycast hit; integration checks call it directly.</summary>
        public void ProcessTarget(GameObject target, Camera eventCamera, Vector2 position, bool leftPressed, bool hover = true)
        {
            if (suspended) { HidePointers(); return; }
            if (target != null && target.GetComponent<IPointerBlocker>() != null)
            {
                // Blocked (the mandatory tutorial): no pick, plug, drop or return, and no hover on what lies beneath.
                UpdatePointers(position, null, null, 0, false, hover);
                return;
            }
            SocketView hit = target != null ? target.GetComponentInParent<SocketView>() : null;
            // A closed socket (D42) is no target: it doesn't highlight, and a click there only bounces the held cord.
            SocketView socket = hit != null && hit.IsOpen ? hit : null;
            StoredCableView stored = target != null ? target.GetComponentInParent<StoredCableView>() : null;
            bool overTray = target != null && tray != null && target.transform.IsChildOf(tray);
            bool overCall = target != null && target.GetComponentInParent<Switchboard.Game.CallCardView>() != null;
            var hoveredBook = target != null ? target.GetComponentInParent<PhoneBookView>() : null;
            bool overOpenBook = hoveredBook != null && hoveredBook.IsOpen;
            if (leftPressed)
            {
                if (socket != null) HandleSocketClick(socket);
                else if (hit != null) ActiveCable?.PlayBlockedFeedback();
                // A button (the shop, the pause button) does its own thing and keeps a held cord in hand.
                else if (ActiveCable != null && !overCall && hoveredBook == null &&
                    (target == null || target.GetComponentInParent<Selectable>() == null))
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
            var clickHandler = target != null ? ExecuteEvents.GetEventHandler<IPointerClickHandler>(target) : null;
            var book = clickHandler != null ? clickHandler.GetComponent<PhoneBookView>() : null;
            int pageTurn = book != null ? book.PageTurnDirectionAt(position, eventCamera) : 0;
            if (hover) UpdatePointers(position, socket, stored, pageTurn, overOpenBook, true);
            else UpdatePointers(position, null, null, 0, overOpenBook, false);
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
                if (!Owns(owner))
                {
                    ActiveCable?.PlayBlockedFeedback();
                    return;
                }
                if (ActiveCable != null && heldCable != owner)
                {
                    if (socket.PlugAnchor == null) { heldCable.PlayBlockedFeedback(); return; }
                    // Unplug both ends of the displaced cord before occupying its socket.
                    if (!owner.TryRemove(socket) || !owner.TryReturnToTray()) return;
                    if (heldCable.TryInsert(socket) && heldCable.Phase == CablePhase.Connected)
                        heldCable = null;
                    return;
                }
                if (owner.TryRemove(socket)) heldCable = owner.IsInTray ? null : owner;
            }
            else if (ActiveCable != null && heldCable.TryInsert(socket) && heldCable.Phase == CablePhase.Connected)
                heldCable = null;
        }

        private void UpdatePointers(Vector2 position, SocketView socket, StoredCableView stored, int pageTurn, bool overOpenBook,
            bool showHand)
        {
            var selected = ActiveCable;
            if (hoveredSocket != socket && hoveredSocket != null) hoveredSocket.SetPointerState(false, selected);
            hoveredSocket = socket;
            if (hoveredSocket != null) hoveredSocket.SetPointerState(true, selected);
            CursorMode = overOpenBook ? (pageTurn != 0 ? CableCursorMode.PageTurn : CableCursorMode.Pointing) :
                selected != null ? CableCursorMode.Plug :
                pageTurn != 0 ? CableCursorMode.PageTurn :
                stored != null && stored.Available && Owns(stored.Cable) ? CableCursorMode.Grab : CableCursorMode.Pointing;
            var highlightedStock = CursorMode == CableCursorMode.Grab ? stored : null;
            if (hoveredStoredCable != highlightedStock && hoveredStoredCable != null) hoveredStoredCable.SetHighlighted(false);
            hoveredStoredCable = highlightedStock;
            if (hoveredStoredCable != null) hoveredStoredCable.SetHighlighted(true);
            if (selected != null) RaiseToTop(selected);
            if (stackDirty) Restack();
            foreach (var cable in cables)
            {
                if (!Usable(cable)) continue;
                cable.UpdatePointer(position, cable == selected && !overOpenBook);
            }
            bool handVisible = false;
            if (handCursor != null)
            {
                var area = (RectTransform)handCursor.transform.parent;
                var canvas = area.GetComponentInParent<Canvas>();
                var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
                handVisible = showHand && (selected == null || overOpenBook) &&
                    RectTransformUtility.RectangleContainsScreenPoint(area, position, camera);
                handCursor.gameObject.SetActive(handVisible);
                if (handVisible)
                {
                    SwitchboardUISorting.SetLayer(handCursor.gameObject, SwitchboardUISorting.Cursor);
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(area, position, camera, out var local);
                    handCursor.rectTransform.anchoredPosition = local;
                    handCursor.Grabbing = CursorMode == CableCursorMode.Grab;
                    handCursor.PageTurnDirection = CursorMode == CableCursorMode.PageTurn ? pageTurn : 0;
                    handCursor.transform.SetAsLastSibling();
                }
            }
            if (ownsCursor) SystemPointer.Visible = !handVisible && (selected == null || !selected.IsPointerVisible);
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
            if (ownsCursor) SystemPointer.Visible = cursorWasVisible;
        }
        private void OnApplicationFocus(bool focused) { if (!focused) HidePointers(); }
        private void OnDisable() { HidePointers(); ownsCursor = false; heldCable = null; }
    }
}
