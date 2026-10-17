using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Switchboard.UI
{
    public sealed class VolumeCablePanel : MonoBehaviour
    {
        [SerializeField] private VolumeCableSlider[] rows;
        [SerializeField] private HandCursorGraphic hand;
        [SerializeField] private RectTransform overlay;
        private readonly List<RaycastResult> hits = new List<RaycastResult>();
        private bool originalCursorVisible;
        public VolumeCableSlider[] Rows => rows;
        public bool OwnsPointerAt(Vector2 position)
        {
            if (!isActiveAndEnabled) return false;
            var canvas = overlay.GetComponentInParent<Canvas>();
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            return AudioVolumeSettings.Patch.HeldCable >= 0 || RectTransformUtility.RectangleContainsScreenPoint((RectTransform)transform, position, camera);
        }
        public void Configure(VolumeCableSlider[] sliders, HandCursorGraphic cursor, RectTransform graphics)
        {
            rows = sliders; hand = cursor; overlay = graphics;
            Refresh(Vector3.zero);
        }
        private void OnEnable()
        {
            if (!Application.isPlaying || rows == null) return;
            originalCursorVisible = Cursor.visible;
            Refresh(Vector3.zero);
        }
        private void Update()
        {
            if (Mouse.current != null && Application.isFocused)
                ProcessPointer(Mouse.current.position.ReadValue(), Mouse.current.leftButton.wasPressedThisFrame);
        }
        public void ProcessPointer(Vector2 position, bool leftPressed)
        {
            if (EventSystem.current == null) return;
            var patch = AudioVolumeSettings.Patch;
            hits.Clear();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = position }, hits);
            var target = hits.Count == 0 ? null : hits[0].gameObject;
            int rowIndex = -1, socketIndex = -1, plug = -1;
            for (int r = 0; r < rows.Length; r++)
            {
                if (target == rows[r].MovingPlug.gameObject && patch.HeldCable != r)
                {
                    plug = r; rowIndex = patch.RowOf(r); socketIndex = patch.SocketOf(r);
                }
                for (int s = 0; s < rows[r].Sockets.Length; s++)
                    if (target == rows[r].Sockets[s].gameObject) { rowIndex = r; socketIndex = s; }
            }
            if (plug < 0 && rowIndex >= 0) plug = patch.CableAt(rowIndex, socketIndex);
            if (leftPressed)
            {
                if (patch.HeldCable >= 0)
                {
                    int held = patch.HeldCable;
                    if (rowIndex >= 0 && AudioVolumeSettings.Connect(rowIndex, socketIndex)) rows[held].PlayFeedback(true);
                    else patch.Cancel();
                }
                else if (plug >= 0 && patch.PickUp(plug)) rows[plug].PlayFeedback(false);
            }
            for (int r = 0; r < rows.Length; r++)
            {
                for (int s = 0; s < rows[r].Sockets.Length; s++)
                    rows[r].Sockets[s].SetHighlighted(rowIndex == r && socketIndex == s && (patch.HeldCable >= 0 || plug >= 0));
            }
            var canvas = overlay.GetComponentInParent<Canvas>();
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            RectTransformUtility.ScreenPointToWorldPointInRectangle(overlay, position, camera, out var world);
            Refresh(world);
            bool inside = RectTransformUtility.RectangleContainsScreenPoint((RectTransform)transform, position, camera);
            bool heldNow = patch.HeldCable >= 0;
            hand.gameObject.SetActive(!heldNow && inside);
            if (!heldNow && inside)
            {
                hand.transform.position = world;
                hand.Grabbing = plug >= 0;
                hand.transform.SetAsLastSibling();
            }
            Cursor.visible = heldNow || inside ? false : originalCursorVisible;
        }
        private void Refresh(Vector3 pointer)
        {
            if (rows == null) return;
            var patch = AudioVolumeSettings.Patch;
            for (int i = 0; i < rows.Length; i++)
                rows[i].Render(patch.HeldCable == i ? pointer : rows[patch.RowOf(i)].Sockets[patch.SocketOf(i)].transform.position);
            if (patch.HeldCable >= 0) rows[patch.HeldCable].MovingPlug.transform.SetAsLastSibling();
        }
        private void OnDisable()
        {
            if (!Application.isPlaying || rows == null) return;
            AudioVolumeSettings.Patch.Cancel();
            Refresh(Vector3.zero);
            foreach (var row in rows)
            {
                foreach (var socket in row.Sockets) socket.SetHighlighted(false);
            }
            if (hand != null) hand.gameObject.SetActive(false);
            Cursor.visible = originalCursorVisible;
        }
        private void OnApplicationFocus(bool focused) { if (!focused) OnDisable(); }
    }
}
