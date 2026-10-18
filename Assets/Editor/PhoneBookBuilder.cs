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
        private const int TabsPerEdge = 13;
        private const float TabX = 420, TabTop = 216, TabStep = 36, TabNudge = 8;
        private static readonly Vector2 TabSize = new Vector2(52, 34);

        [MenuItem("Tools/Switchboard/Update Phone Book Directory")]
        public static void UpdateDirectory()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode first.");
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                ConfigureDirectory(root.GetComponent<PhoneBookView>());
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        [MenuItem("Tools/Switchboard/Update Phone Book Tabs")]
        public static void UpdateTabs()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode first.");
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                ConfigureTabs(root.GetComponent<PhoneBookView>());
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

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
                var closedSprite = Sprite("table_book");
                var closed = Rect("ClosedBook", root, new Vector2(360, 360f * closedSprite.rect.height / closedSprite.rect.width));
                BottomRight(closed, new Vector2(-50, 12));
                var closedButton = BookButton(closed, closedSprite);
                closedButton.GetComponent<Image>().preserveAspect = true;

                var opened = Rect("OpenBook", root, new Vector2(820, 524));
                BottomRight(opened, new Vector2(-50, 16));
                var openButton = BookButton(opened, Sprite("back"));
                var pageSprite = Sprite("page");
                var left = Page("LeftPage", opened, pageSprite, new Vector2(-200, 0));
                left.localScale = new Vector3(-1, 1, 1);
                Page("RightPage", opened, pageSprite, new Vector2(200, 0));
                var view = root.gameObject.AddComponent<PhoneBookView>();
                view.Configure(closedButton, openButton);
                ConfigureDirectory(view);
                ConfigureTabs(view);
                return PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabPath);
            }
            finally { Object.DestroyImmediate(root.gameObject); }
        }

        private static void ConfigureLayers(GameObject root)
        {
            SwitchboardUISorting.SetLayer(root, SwitchboardUISorting.Book);
            if (root.GetComponent<GraphicRaycaster>() == null) root.AddComponent<GraphicRaycaster>();
        }

        private static void ConfigureDirectory(PhoneBookView view)
        {
            var opened = view.transform.Find("OpenBook");
            if (opened.Find("Directory") != null)
            {
                foreach (var fold in opened.GetComponentsInChildren<PageFoldGraphic>(true))
                    if (fold.GetComponent<CanvasRenderer>() == null) fold.gameObject.AddComponent<CanvasRenderer>();
                return;
            }
            var config = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Settings/PhoneBook.json");
            if (config == null) throw new InvalidOperationException("PhoneBook.json was not found.");
            // Text is separate from the mirrored artwork on LeftPage.
            var content = Rect("Directory", opened, new Vector2(800, 492));
            var left = DirectoryPage("LeftDirectoryPage", content, -200);
            var right = DirectoryPage("RightDirectoryPage", content, 200);
            var previous = PageCorner("PreviousSpread", content, true);
            var next = PageCorner("NextSpread", content, false);
            view.ConfigureDirectory(config, left, right, previous, next);
            previous.gameObject.SetActive(false);
            next.gameObject.SetActive(false);
        }

        // A thumb index: A-M along the left page edge, N-Z along the right, tucked under the pages.
        private static void ConfigureTabs(PhoneBookView view)
        {
            var opened = view.transform.Find("OpenBook");
            var existing = opened.Find("LetterTabs");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);
            var strip = Rect("LetterTabs", opened, Vector2.zero);
            strip.anchorMin = Vector2.zero;
            strip.anchorMax = Vector2.one;
            strip.SetSiblingIndex(0);
            var tabs = new PhoneBookTab[26];
            for (int i = 0; i < tabs.Length; i++)
            {
                bool left = i < TabsPerEdge;
                float y = TabTop - (i % TabsPerEdge) * TabStep;
                tabs[i] = Tab((char)('A' + i), strip, new Vector2(left ? -TabX : TabX, y), left);
            }
            view.ConfigureTabs(tabs);
        }

        private static PhoneBookTab Tab(char letter, Transform parent, Vector2 position, bool left)
        {
            var rect = Rect($"Tab{letter}", parent, TabSize);
            rect.anchoredPosition = position;
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.type = Image.Type.Sliced;
            image.raycastTarget = true;
            var shadow = rect.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, .3f);
            shadow.effectDistance = new Vector2(left ? -2 : 2, -2);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var colors = button.colors;
            colors.normalColor = colors.selectedColor = colors.disabledColor = Color.white;
            colors.highlightedColor = new Color(1.1f, 1.1f, 1.05f, 1);
            colors.pressedColor = new Color(.8f, .8f, .75f, 1);
            colors.fadeDuration = .08f;
            button.colors = colors;
            // The inner edge sits under the page, so the letter is centred on the part that shows.
            var label = Label("Letter", rect, letter.ToString(), new Vector2(left ? -3 : 3, 0), TabSize, 22, FontStyle.Normal, TextAnchor.MiddleCenter);
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            var tab = rect.gameObject.AddComponent<PhoneBookTab>();
            tab.Configure(letter, button, image, label);
            tab.SetRest(position, new Vector2(left ? -TabNudge : TabNudge, 0));
            return tab;
        }

        private static PhoneBookPageView DirectoryPage(string name, Transform parent, float x)
        {
            var root = Rect(name, parent, new Vector2(400, 492));
            root.anchoredPosition = new Vector2(x, 0);
            Label("Heading", root, "CONTACTS", new Vector2(0, 211), new Vector2(330, 34), 24, FontStyle.Bold, TextAnchor.MiddleCenter);
            Label("NameHeading", root, "Name", new Vector2(-42, 162), new Vector2(242, 28), 18, FontStyle.Italic, TextAnchor.MiddleLeft);
            Label("SocketHeading", root, "Socket", new Vector2(125, 162), new Vector2(76, 28), 18, FontStyle.Italic, TextAnchor.MiddleRight);
            var entries = Rect("Entries", root, new Vector2(326, 326));
            entries.anchoredPosition = new Vector2(0, -30);
            var template = Rect("ContactTemplate", entries, new Vector2(326, 50));
            var nameLabel = Label("Name", template, "", Vector2.zero, Vector2.zero, 22, FontStyle.Normal, TextAnchor.MiddleLeft);
            nameLabel.rectTransform.anchorMin = Vector2.zero;
            nameLabel.rectTransform.anchorMax = new Vector2(.76f, 1);
            nameLabel.rectTransform.offsetMin = Vector2.zero;
            nameLabel.rectTransform.offsetMax = new Vector2(-8, 0);
            nameLabel.resizeTextForBestFit = true;
            nameLabel.resizeTextMinSize = 12;
            nameLabel.resizeTextMaxSize = 22;
            var addressLabel = Label("Socket", template, "", Vector2.zero, Vector2.zero, 22, FontStyle.Bold, TextAnchor.MiddleRight);
            addressLabel.rectTransform.anchorMin = new Vector2(.76f, 0);
            addressLabel.rectTransform.anchorMax = Vector2.one;
            addressLabel.rectTransform.offsetMin = addressLabel.rectTransform.offsetMax = Vector2.zero;
            var row = template.gameObject.AddComponent<PhoneBookContactRow>();
            row.Configure(nameLabel, addressLabel);
            template.gameObject.SetActive(false);
            var number = Label("PageNumber", root, "", new Vector2(0, -223), new Vector2(80, 26), 18, FontStyle.Normal, TextAnchor.MiddleCenter);
            var empty = Label("EmptyMessage", root, "No contacts", new Vector2(0, 50), new Vector2(300, 50), 22, FontStyle.Italic, TextAnchor.MiddleCenter);
            empty.gameObject.SetActive(false);
            var page = root.gameObject.AddComponent<PhoneBookPageView>();
            page.Configure(entries, row, number, empty);
            return page;
        }

        private static Button PageCorner(string name, Transform parent, bool left)
        {
            var rect = Rect(name, parent, new Vector2(60, 60));
            rect.anchoredPosition = new Vector2(left ? -370 : 370, -216);
            var fold = rect.gameObject.AddComponent<PageFoldGraphic>();
            fold.LeftCorner = left;
            fold.raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = fold;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var colors = button.colors;
            colors.normalColor = colors.selectedColor = Color.white;
            colors.highlightedColor = new Color(1, .93f, .69f, 1);
            colors.pressedColor = new Color(.78f, .73f, .56f, 1);
            colors.fadeDuration = .08f;
            button.colors = colors;
            return button;
        }

        private static Text Label(string name, Transform parent, string value, Vector2 position,
            Vector2 size, int fontSize, FontStyle style, TextAnchor alignment)
        {
            var rect = Rect(name, parent, size);
            rect.anchoredPosition = position;
            var label = rect.gameObject.AddComponent<Text>();
            label.font = AssetDatabase.LoadAssetAtPath<Font>(name == "Heading" ? MenuTypographyBuilder.HeadingFontPath : MenuTypographyBuilder.BodyFontPath);
            label.text = value;
            label.fontSize = fontSize;
            label.fontStyle = FontStyle.Normal;
            label.alignment = alignment;
            label.color = new Color32(44, 48, 42, 255);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.supportRichText = false;
            label.raycastTarget = false;
            return label;
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
