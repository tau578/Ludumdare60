using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Switchboard.Game
{
    /// <summary>The only lookups allowed into the team's board objects in the Final scene (Design 3 and 7).</summary>
    internal static class LevelScene
    {
        // The caller slots have no component of their own, so they are found under this agreed name.
        private const string CallerPanelName = "CallerQueuePanel";
        // The shop panel has no component of its own either; the money counter sits in its header plaque.
        private const string ShopPanelName = "UpgradeShopPanel";
        private const string ShopHeaderName = "HeaderSlot";

        public static List<T> FindAll<T>(Scene scene) where T : Component
        {
            var found = new List<T>();
            if (!scene.IsValid() || !scene.isLoaded) return found;
            foreach (GameObject root in scene.GetRootGameObjects()) found.AddRange(root.GetComponentsInChildren<T>(true));
            return found;
        }

        public static RectTransform[] FindCallerSlots(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return new RectTransform[0];
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform panel = FindByName(root.transform, CallerPanelName);
                if (panel == null) continue;
                VerticalLayoutGroup column = panel.GetComponentInChildren<VerticalLayoutGroup>(true);
                if (column == null) break;
                var slots = new RectTransform[column.transform.childCount];
                for (int i = 0; i < slots.Length; i++) slots[i] = (RectTransform)column.transform.GetChild(i);
                return slots;
            }
            Debug.LogWarning($"No {CallerPanelName} with a VerticalLayoutGroup was found in {scene.name}.");
            return new RectTransform[0];
        }

        public static RectTransform FindShopHeader(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform panel = FindByName(root.transform, ShopPanelName);
                if (panel == null) continue;
                if (panel.Find(ShopHeaderName) is RectTransform header) return header;
                break;
            }
            Debug.LogWarning($"No {ShopPanelName} with a {ShopHeaderName} was found in {scene.name}.");
            return null;
        }

        private static Transform FindByName(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform match = FindByName(parent.GetChild(i), name);
                if (match != null) return match;
            }
            return null;
        }
    }
}
