using System;
using System.Linq;
using Switchboard.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Switchboard.Editor
{
    public static class PhoneBookBuilder
    {
        private const string PrefabPath = "Assets/Prefabs/UI/PhoneBook.prefab";

        [MenuItem("Tools/Switchboard/Update Phone Book Layers")]
        public static void UpdateBookLayers()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode first.");
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                ConfigureLayers(root);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            foreach (var book in Object.FindObjectsByType<PhoneBookView>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                ConfigureLayers(book.gameObject);
                PrefabUtility.RecordPrefabInstancePropertyModifications(book.GetComponent<Canvas>());
                EditorSceneManager.MarkSceneDirty(book.gameObject.scene);
            }
        }

        [MenuItem("Tools/Switchboard/Create Phone Book")]
        public static void CreatePhoneBook()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || PrefabStageUtility.GetCurrentPrefabStage() != null)
                throw new InvalidOperationException("Open Level in Edit Mode first.");
            var scene = SceneManager.GetActiveScene();
            var canvas = scene.GetRootGameObjects().FirstOrDefault(go => go.name == "SwitchboardCanvas");
            var area = canvas != null ? canvas.transform.Find("WorkArea") : null;
            if (area == null) throw new InvalidOperationException("Switchboard WorkArea was not found.");
            var existing = area.Find("PhoneBook");
            if (existing != null) { Selection.activeGameObject = existing.gameObject; return; }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null) prefab = CreatePrefab();
            var book = (GameObject)PrefabUtility.InstantiatePrefab(prefab, area);
            ConfigureLayers(book);
            PrefabUtility.RecordPrefabInstancePropertyModifications(book.GetComponent<Canvas>());
            var overlay = area.Find("CableInteraction");
            if (overlay != null) book.transform.SetSiblingIndex(overlay.GetSiblingIndex());
            Undo.RegisterCreatedObjectUndo(book, "Add phone book");
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = book;
        }

        private static GameObject CreatePrefab()
        {
            var root = Rect("PhoneBook", null, Vector2.zero);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            try
            {
                ConfigureLayers(root.gameObject);
                var closed = Rect("ClosedBook", root, new Vector2(360, 360f * 423 / 817));
                BottomRight(closed, new Vector2(-50, 12));
                var closedButton = BookButton(closed, Sprite("table_book"));
                closedButton.GetComponent<Image>().preserveAspect = true;

                var opened = Rect("OpenBook", root, new Vector2(820, 524));
                BottomRight(opened, new Vector2(-50, 16));
                var openButton = BookButton(opened, Sprite("back"));
                var pageSprite = Sprite("page");
                var left = Page("LeftPage", opened, pageSprite, new Vector2(-200, 0));
                left.localScale = new Vector3(-1, 1, 1);
                Page("RightPage", opened, pageSprite, new Vector2(200, 0));
                root.gameObject.AddComponent<PhoneBookView>().Configure(closedButton, openButton);
                return PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabPath);
            }
            finally { Object.DestroyImmediate(root.gameObject); }
        }

        private static void ConfigureLayers(GameObject root)
        {
            SwitchboardUISorting.SetLayer(root, SwitchboardUISorting.Book);
            if (root.GetComponent<GraphicRaycaster>() == null) root.AddComponent<GraphicRaycaster>();
        }

        private static Sprite Sprite(string name)
        {
            var sprite = AssetDatabase.LoadAllAssetsAtPath($"Assets/Sprites/book/{name}.png").OfType<Sprite>().FirstOrDefault();
            if (sprite == null) throw new InvalidOperationException($"Book sprite {name} was not found.");
            return sprite;
        }

        private static Button BookButton(RectTransform rect, Sprite sprite)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = true;
            var shadow = rect.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, .35f);
            shadow.effectDistance = new Vector2(5, -6);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var colors = button.colors;
            colors.normalColor = colors.selectedColor = Color.white;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.1f, 1);
            colors.pressedColor = new Color(.8f, .8f, .75f, 1);
            colors.fadeDuration = .08f;
            button.colors = colors;
            return button;
        }

        private static RectTransform Page(string name, Transform parent, Sprite sprite, Vector2 position)
        {
            var rect = Rect(name, parent, new Vector2(400, 400f * 587 / 477));
            rect.anchoredPosition = position;
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            return rect;
        }

        private static void BottomRight(RectTransform rect, Vector2 offset)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1, 0);
            rect.anchoredPosition = offset;
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 size)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.gameObject.layer = LayerMask.NameToLayer("UI");
            if (parent != null) rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = size;
            return rect;
        }
    }
}
