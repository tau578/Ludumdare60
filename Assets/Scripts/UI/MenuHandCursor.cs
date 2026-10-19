using UnityEngine;
using UnityEngine.InputSystem;

namespace Switchboard.UI
{
    public sealed class MenuHandCursor : MonoBehaviour
    {
        [SerializeField] private HandCursorGraphic hand;
        [SerializeField] private VolumeCablePanel volumePanel;
        private bool originalVisible;
        public HandCursorGraphic Hand => hand;
        public void Configure(HandCursorGraphic graphic, VolumeCablePanel panel) { hand = graphic; volumePanel = panel; }
        private void OnEnable() { originalVisible = SystemPointer.Visible; }
        private void LateUpdate()
        {
            // Mouse and touch alike (D28); on a touchscreen the hand shows only while a finger is down.
            var pointer = Pointer.current;
            if (pointer == null || !Application.isFocused) { Hide(); return; }
            if (pointer is Touchscreen && !pointer.press.isPressed && !pointer.press.wasPressedThisFrame) { Hide(); return; }
            UpdatePointer(pointer.position.ReadValue());
        }
        public void UpdatePointer(Vector2 position)
        {
            if (hand == null) return;
            var area = (RectTransform)transform;
            var canvas = GetComponentInParent<Canvas>();
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            bool inside = RectTransformUtility.RectangleContainsScreenPoint(area, position, camera);
            bool volumeOwnsPointer = volumePanel != null && volumePanel.OwnsPointerAt(position);
            hand.gameObject.SetActive(inside && !volumeOwnsPointer);
            if (hand.gameObject.activeSelf)
            {
                RectTransformUtility.ScreenPointToWorldPointInRectangle(area, position, camera, out var world);
                hand.transform.position = world;
                hand.Grabbing = false;
            }
            SystemPointer.Visible = inside || volumeOwnsPointer ? false : originalVisible;
        }
        private void Hide()
        {
            if (hand != null) hand.gameObject.SetActive(false);
            SystemPointer.Visible = originalVisible;
        }
        private void OnDisable() => Hide();
        private void OnApplicationFocus(bool focused) { if (!focused) Hide(); }
    }
}
