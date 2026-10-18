using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace Switchboard.UI
{
    /// <summary>A soft cursor light confined to the board's gold pigment.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(Image))]
    public sealed class BoardGoldLight : MonoBehaviour
    {
        [SerializeField, Min(1)] private float radius = 180f;
        [SerializeField, Range(0, 1)] private float intensity = .22f;
        [SerializeField, Range(0, 1)] private float shimmer = .1f;
        [SerializeField, Min(.01f)] private float fadeTime = .18f;
        private Image board;
        private Material original, instance;
        private float visibility;
        private Vector2 localPointer;

        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            board = GetComponent<Image>();
            original = board.material;
            if (original == null || original.shader.name != "Switchboard/UI/Board Gold Light") return;
            instance = new Material(original) { name = original.name + " (Instance)", hideFlags = HideFlags.HideAndDontSave };
            visibility = 0;
            board.material = instance;
            Apply(instance);
        }

        private void LateUpdate()
        {
            if (instance == null) return;
            var mouse = Mouse.current;
            bool visible = false;
            if (mouse != null && Application.isFocused)
            {
                Vector2 screen = mouse.position.ReadValue();
                Canvas canvas = board.canvas;
                if (canvas != null && screen.x >= 0 && screen.y >= 0 && screen.x <= Screen.width && screen.y <= Screen.height)
                    visible = RectTransformUtility.ScreenPointToLocalPointInRectangle(board.rectTransform, screen,
                        canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, out localPointer);
            }
            visibility = Mathf.MoveTowards(visibility, visible ? 1 : 0, Time.unscaledDeltaTime / Mathf.Max(.01f, fadeTime));
            Apply(instance);
            Material rendered = board.materialForRendering;
            if (rendered != instance) Apply(rendered);
        }

        private void Apply(Material material)
        {
            Rect rect = board.rectTransform.rect;
            Vector2 uv = (localPointer - rect.min) / rect.size;
            material.SetVector("_Pointer", new Vector4(uv.x, uv.y, visibility, 0));
            material.SetVector("_BoardSize", new Vector4(rect.width, rect.height, 0, 0));
            material.SetVector("_SpriteUV", board.sprite != null ? DataUtility.GetOuterUV(board.sprite) : new Vector4(0, 0, 1, 1));
            material.SetFloat("_LightRadius", radius);
            material.SetFloat("_LightIntensity", intensity);
            material.SetFloat("_Shimmer", shimmer);
            material.SetFloat("_LightTime", Time.unscaledTime);
        }

        private void OnDisable()
        {
            if (instance == null) return;
            if (board != null && board.material == instance) board.material = original;
            Destroy(instance);
            instance = null;
        }
    }
}
