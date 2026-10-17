using UnityEngine;

namespace Switchboard.UI
{
    /// <summary>Connected cables, book pages, then the hand or held plug.</summary>
    public static class SwitchboardUISorting
    {
        public const int Cable = 0;
        public const int Book = 10;
        public const int Cursor = 20;

        public static Canvas SetLayer(GameObject target, int order)
        {
            var canvas = target.GetComponent<Canvas>();
            if (canvas == null) canvas = target.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = order;
            return canvas;
        }
    }
}
