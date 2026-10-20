using UnityEngine;

namespace Switchboard.UI
{
    /// <summary>
    /// Resting plugs, the wires by recency, book pages (and GameUI at 10), the tutorial spotlight, then the hand or
    /// held plug. GameUI's Goal dialog (15) and pause menu (16) sit between the spotlight and the hand.
    /// </summary>
    public static class SwitchboardUISorting
    {
        public const int Cable = 0;
        // One order per cord above the board's resting plugs (lead, 2026-10-20); the top one stays below the book.
        public const int Wire = 1;
        public const int Book = 10;
        // Over the book and GameUI so it can dim them, under the Goal dialog, the pause menu and the hand (D57).
        public const int Tutorial = 14;
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
