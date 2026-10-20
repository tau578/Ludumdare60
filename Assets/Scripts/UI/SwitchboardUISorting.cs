using UnityEngine;

namespace Switchboard.UI
{
    /// <summary>Resting plugs, the wires by recency, book pages, then the hand or held plug.</summary>
    public static class SwitchboardUISorting
    {
        public const int Cable = 0;
        // One order per cord above the board's resting plugs (lead, 2026-10-20); the top one stays below the book.
        public const int Wire = 1;
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
