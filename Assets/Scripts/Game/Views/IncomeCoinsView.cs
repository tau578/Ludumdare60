using System.Collections.Generic;
using UnityEngine;

namespace Switchboard.Game
{
    /// <summary>Short UI-only reward flights, independent of a card's lifetime and pool slot.</summary>
    public sealed class IncomeCoinsView : MonoBehaviour
    {
        private sealed class Coin
        {
            public RectTransform Rect;
            public Vector2 StartScreen;
            public float Age;
            public int Index;
        }

        private const float TravelSeconds = .65f;
        private const float StaggerSeconds = .055f;
        private readonly List<Coin> coins = new List<Coin>();
        private RectTransform area;
        private RectTransform destination;
        private Canvas canvas;
        public bool IsFlying => coins.Count > 0;

        public static IncomeCoinsView Create(Transform owner)
        {
            Canvas parent = owner.GetComponentInParent<Canvas>().rootCanvas;
            var root = new GameObject("Connection coins", typeof(RectTransform), typeof(Canvas));
            var rect = (RectTransform)root.transform;
            rect.SetParent(parent.transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            Canvas overlay = root.GetComponent<Canvas>();
            overlay.overrideSorting = true;
            overlay.sortingLayerID = parent.sortingLayerID;
            overlay.sortingOrder = parent.sortingOrder + 30;
            var view = root.AddComponent<IncomeCoinsView>();
            view.area = rect;
            view.canvas = parent;
            return view;
        }

        public void Fly(RectTransform portrait, RectTransform target)
        {
            destination = target;
            Vector2 start = ScreenCentre(portrait);
            for (int i = 0; i < 4; i++)
            {
                var go = new GameObject("Coin", typeof(RectTransform), typeof(IncomeCoinGraphic));
                var rect = (RectTransform)go.transform;
                rect.SetParent(area, false);
                rect.sizeDelta = new Vector2(24, 24);
                rect.localScale = Vector3.zero;
                go.GetComponent<IncomeCoinGraphic>().raycastTarget = false;
                coins.Add(new Coin { Rect = rect, StartScreen = start, Age = -i * StaggerSeconds, Index = i });
            }
        }

        // Called by the counter before deciding whether its number may advance this frame.
        public void Tick(float dt)
        {
            if (destination == null) { Clear(); return; }
            Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(area, ScreenCentre(destination), camera, out Vector2 end);
            for (int i = coins.Count - 1; i >= 0; i--)
            {
                Coin coin = coins[i];
                coin.Age += dt;
                if (coin.Age >= TravelSeconds)
                {
                    Release(coin.Rect.gameObject);
                    coins.RemoveAt(i);
                    continue;
                }
                if (coin.Age < 0) continue;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(area, coin.StartScreen, camera, out Vector2 start);
                float t = coin.Age / TravelSeconds;
                float progress = t * t * (3 - 2 * t);
                float arc = Mathf.Sin(Mathf.PI * t);
                // The first portrait and money plate are close to the top edge: bow into the screen.
                float bend = Mathf.Max(start.y, end.y) + 140 > area.rect.yMax ? -1 : 1;
                coin.Rect.anchoredPosition = Vector2.Lerp(start, end, progress)
                    + new Vector2((coin.Index - 1.5f) * 14, bend * (95 + coin.Index * 12)) * arc;
                float scale = Mathf.Min(t / .12f, 1) * Mathf.Min((1 - t) / .16f, 1);
                float spin = .45f + .55f * Mathf.Abs(Mathf.Cos(t * Mathf.PI * 3 + coin.Index * .5f));
                coin.Rect.localScale = new Vector3(scale * spin, scale, 1);
                coin.Rect.localRotation = Quaternion.Euler(0, 0, 15 * Mathf.Sin(t * Mathf.PI * 2));
            }
        }

        private static Vector2 ScreenCentre(RectTransform rect)
        {
            Canvas parent = rect.GetComponentInParent<Canvas>().rootCanvas;
            Camera camera = parent.renderMode == RenderMode.ScreenSpaceOverlay ? null : parent.worldCamera;
            return RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center));
        }

        public void Clear()
        {
            foreach (Coin coin in coins)
                if (coin.Rect != null) Release(coin.Rect.gameObject);
            coins.Clear();
        }

        private void OnDisable() => Clear();

        private static void Release(GameObject item)
        {
            if (Application.isPlaying) Destroy(item);
            else DestroyImmediate(item);
        }
    }
}
