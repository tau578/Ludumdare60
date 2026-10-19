using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Switchboard.UI
{
    /// <summary>Shares the shop button artwork across prefab and scene-added menu buttons.</summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class MenuButtonSkin : MonoBehaviour
    {
        [SerializeField] private Sprite buttonSprite;

        private void OnEnable() => Apply();

#if UNITY_EDITOR
        private void Update()
        {
            if (!Application.IsPlaying(gameObject)) Apply();
        }
#endif

        public void Apply()
        {
            if (buttonSprite == null) return;
            foreach (var button in GetComponentsInChildren<Button>(true))
            {
                if (!(button.targetGraphic is Image image)) continue;
                image.sprite = buttonSprite;
                image.overrideSprite = null;
                image.type = Image.Type.Simple;
                image.color = Color.white;

                // ColorTint gives the painted green surface a subtle hover without a yellow flash.
                var colors = button.colors;
                colors.normalColor = new Color(.92f, .92f, .92f, 1);
                colors.highlightedColor = colors.selectedColor = Color.white;
                colors.pressedColor = new Color(.72f, .72f, .72f, 1);
                colors.disabledColor = new Color(.55f, .55f, .55f, .7f);
                colors.colorMultiplier = 1;
                colors.fadeDuration = .1f;
                button.colors = colors;
                button.transition = Selectable.Transition.ColorTint;

                foreach (var label in button.GetComponentsInChildren<TMP_Text>(true))
                {
                    label.color = new Color(1, .98f, .91f, 1);
                    // Match the Options Go back label: centred vertically, with no vertical inset.
                    label.verticalAlignment = VerticalAlignmentOptions.Middle;
                    var margin = label.margin;
                    margin.y = margin.w = 0;
                    label.margin = margin;
                    var position = label.rectTransform.anchoredPosition;
                    position.y = 3.3f;
                    label.rectTransform.anchoredPosition = position;
                }
            }
        }
    }
}
