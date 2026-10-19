using UnityEngine;
using UnityEngine.UI;

namespace Switchboard.UI
{
    /// <summary>PNG cursor selection; keeps existing scene bindings without drawing a UI mesh.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HandCursorGraphic : MaskableGraphic
    {
        [SerializeField] private Texture2D pointingTexture;
        [SerializeField] private Texture2D grabTexture;
        [SerializeField] private Texture2D pageLeftTexture;
        [SerializeField] private Texture2D pageRightTexture;
        private bool applied;
        public bool Grabbing { get; set; }
        public int PageTurnDirection { get; set; }
        public Texture2D CursorTexture => PageTurnDirection < 0 ? pageLeftTexture :
            PageTurnDirection > 0 ? pageRightTexture : Grabbing ? grabTexture : pointingTexture;
        public Vector2 Hotspot => PageTurnDirection < 0 ? new Vector2(42, 23) :
            PageTurnDirection > 0 ? new Vector2(22, 23) : Grabbing ? new Vector2(29, 28) : new Vector2(20, 2);

        public void ApplyCursor()
        {
#if UNITY_EDITOR
            // Also supports already-open scenes after the scripts reload.
            if (CursorTexture == null) AssignMissingTextures();
#endif
            var texture = CursorTexture;
            Cursor.SetCursor(texture, texture != null ? Hotspot : Vector2.zero, CursorMode.Auto);
            Cursor.visible = true;
            applied = true;
        }

        // Yield without restoring when another controller already owns the pointer (volume options).
        public void ReleaseCursor(bool restore = true)
        {
            if (!applied) return;
            applied = false;
            if (restore) RestoreDefaultCursor();
        }

        public void RestoreDefaultCursor()
        {
            Cursor.SetCursor(pointingTexture, pointingTexture != null ? new Vector2(20, 2) : Vector2.zero, CursorMode.Auto);
            Cursor.visible = true;
        }

        protected override void OnPopulateMesh(VertexHelper mesh) => mesh.Clear();

        protected override void OnDisable()
        {
            ReleaseCursor();
            base.OnDisable();
        }

#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();
            raycastTarget = false;
            AssignMissingTextures();
        }

        private void AssignMissingTextures()
        {
            if (pointingTexture == null) pointingTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Sprites/Cursors/cursor_pointing.png");
            if (grabTexture == null) grabTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Sprites/Cursors/cursor_grab.png");
            if (pageLeftTexture == null) pageLeftTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Sprites/Cursors/cursor_page_left.png");
            if (pageRightTexture == null) pageRightTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Sprites/Cursors/cursor_page_right.png");
        }
#endif
    }
}
