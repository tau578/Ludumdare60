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
        private void OnEnable() { originalVisible = Cursor.visible; }
        private void LateUpdate()
        {
            if (Mouse.current == null || !Application.isFocused) { Hide(); return; }
            UpdatePointer(Mouse.current.position.ReadValue());
        }
        public void UpdatePointer(Vector2 position)
        {
            if (hand == null) return;
            var area = (RectTransform)transform;
            var canvas = GetComponentInParent<Canvas>();
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            bool inside = RectTransformUtility.RectangleContainsScreenPoint(area, position, camera);
            bool volumeOwnsPointer = volumePanel != null && volumePanel.OwnsPointerAt(position);
            if (volumeOwnsPointer) hand.ReleaseCursor(false);
            hand.gameObject.SetActive(inside && !volumeOwnsPointer);
            if (hand.gameObject.activeSelf)
            {
                hand.Grabbing = false;
                hand.PageTurnDirection = 0;
                hand.ApplyCursor();
            }
        }
        private void Hide()
        {
            bool hadCursor = hand != null && hand.gameObject.activeSelf;
            if (hand != null) hand.gameObject.SetActive(false);
            if (hadCursor) Cursor.visible = originalVisible;
        }
        private void OnDisable() => Hide();
        private void OnApplicationFocus(bool focused) { if (!focused) Hide(); }
    }
}
