using System.Reflection;
using System.Linq;
using Switchboard.Cables;
using Switchboard.Content;
using NUnit.Framework;
using Switchboard.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace Switchboard.Game.Tests
{
    public sealed class PhoneBookPageTurnTests
    {
        private static readonly FieldInfo ContactsConfig =
            typeof(PhoneBookView).GetField("contactsConfig", BindingFlags.NonPublic | BindingFlags.Instance);
        private GameObject root;
        private PhoneBookView book;
        private RectTransform opened;
        private TextAsset realFile;
        private TextAsset sampleFile;

        [SetUp]
        public void SetUp()
        {
            root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/PhoneBook.prefab"));
            book = root.GetComponent<PhoneBookView>();
            // The designer's Book.json may fit on one spread, so the page-turn checks read a sample book of four spreads
            // with the real page size; PageTurningGoesThroughTheWholeBook reads the real file.
            realFile = (TextAsset)ContactsConfig.GetValue(book);
            sampleFile = new TextAsset(SampleBook(BookFileLoader.Load(realFile.text).ContactsPerPage));
            ContactsConfig.SetValue(book, sampleFile);
            typeof(PhoneBookView).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(book, null);
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            book.SetOpen(true);
            Canvas.ForceUpdateCanvases();
            opened = (RectTransform)root.transform.Find("OpenBook");
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null) Object.DestroyImmediate(root);
            if (sampleFile != null) Object.DestroyImmediate(sampleFile);
        }

        private static string SampleBook(int perPage)
        {
            var entries = Enumerable.Range(1, perPage * 2 * 3 + 1)
                .Select(i => $"{{ \"name\": \"Sample {i:00}\", \"jack\": \"{(char)('A' + i % 6)}{i % 6 + 1}\" }}");
            return $"{{ \"contactsPerPage\": {perPage}, \"entries\": [ {string.Join(", ", entries)} ] }}";
        }

        private Vector2 Position(float x, float y = 0)
        {
            if (x == 200 || x == -200)
            {
                var fold = (RectTransform)opened.Find(x > 0 ? "Directory/NextSpread" : "Directory/PreviousSpread");
                return fold.TransformPoint(fold.rect.center);
            }
            return opened.TransformPoint(opened.rect.center + new Vector2(x, y));
        }

        [Test]
        public void TabTogglesBookAndIsIgnoredWhilePaused()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var update = typeof(PhoneBookView).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
            // Outside Play Mode InputSystem.Update() runs an editor update, and wasPressedThisFrame only counts player
            // updates (as the game gets in Play Mode), so the test runs player updates while it presses Tab.
            var flags = BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public;
            object manager = typeof(InputSystem).GetField("s_Manager", flags).GetValue(null);
            PropertyInfo playerUpdates = manager.GetType().GetProperty("runPlayerUpdatesInEditMode", flags);
            object wasRunning = playerUpdates.GetValue(manager);
            try
            {
                playerUpdates.SetValue(manager, true);
                book.SetOpen(false);
                PressTab();
                Assert.That(book.IsOpen, Is.True);
                PressTab();
                Assert.That(book.IsOpen, Is.False);
                book.InputEnabled = false;
                PressTab();
                Assert.That(book.IsOpen, Is.False, "Tab must not open the book behind the pause menu");
                book.SetOpen(true);
                PressTab();
                Assert.That(book.IsOpen, Is.True, "Tab must not close it while paused either");
            }
            finally
            {
                playerUpdates.SetValue(manager, wasRunning);
                InputSystem.RemoveDevice(keyboard);
            }

            void PressTab()
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.Update();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Tab));
                InputSystem.Update();
                update.Invoke(book, null);
            }
        }

        [Test]
        public void ShortcutButtonOpensBookAndRespectsInputLock()
        {
            var shortcut = root.transform.Find("ClosedBook/BookShortcut").GetComponent<Button>();
            book.SetOpen(false);
            book.InputEnabled = false;
            Assert.That(shortcut.interactable, Is.False);
            shortcut.onClick.Invoke();
            Assert.That(book.IsOpen, Is.False);
            book.InputEnabled = true;
            Assert.That(shortcut.interactable, Is.True);
            shortcut.onClick.Invoke();
            Assert.That(book.IsOpen, Is.True);
            Assert.That(shortcut.gameObject.activeInHierarchy, Is.False, "hint hides with the closed cover");
        }
        private void Click(float x, float y = 0, PointerEventData.InputButton button = PointerEventData.InputButton.Left)
        {
            ExecuteEvents.ExecuteHierarchy(opened.gameObject, new PointerEventData(null)
                { position = Position(x, y), button = button }, ExecuteEvents.pointerClickHandler);
        }

        [Test]
        public void CornersTurnPagesAndOutsideClickKeepsBookOpen()
        {
            Assert.That(book.PageTurnDirectionAt(Position(-200), null), Is.Zero);
            Click(900);
            Assert.That(book.CurrentSpreadIndex, Is.Zero);
            Assert.That(book.IsOpen, Is.True);
            Assert.That(book.PageTurnDirectionAt(Position(200), null), Is.EqualTo(1));
            Click(200, 150);
            Assert.That(book.CurrentSpreadIndex, Is.EqualTo(1));
            Assert.That(book.PageTurnDirectionAt(Position(-200), null), Is.EqualTo(-1));
            Click(-200, -100);
            Assert.That(book.CurrentSpreadIndex, Is.Zero);
            while (book.CanGoForward) Click(200);
            int last = book.CurrentSpreadIndex;
            Assert.That(book.PageTurnDirectionAt(Position(200), null), Is.Zero);
            Click(900);
            Assert.That(book.CurrentSpreadIndex, Is.EqualTo(last));
            Assert.That(book.IsOpen, Is.True);
            Assert.That(book.PageTurnDirectionAt(Position(900), null), Is.Zero);
        }

        [Test]
        public void FoldsOnlyIndicateAvailableDirectionWithoutCapturingOrTinting()
        {
            var previous = opened.Find("Directory/PreviousSpread").GetComponent<Button>();
            var next = opened.Find("Directory/NextSpread").GetComponent<Button>();
            Assert.That(previous.gameObject.activeSelf, Is.False);
            Assert.That(next.gameObject.activeSelf, Is.True);
            foreach (var fold in new[] { previous, next })
            {
                Assert.That(fold.transition, Is.EqualTo(Selectable.Transition.None));
                Assert.That(fold.enabled, Is.False);
                Assert.That(fold.targetGraphic.raycastTarget, Is.False);
            }
            Click(200);
            Assert.That(previous.gameObject.activeSelf, Is.True);
            while (book.CanGoForward) Click(200);
            Assert.That(next.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void TabsKeepPriorityAndRowsLetClicksThroughToCloseBook()
        {
            var row = opened.GetComponentInChildren<PhoneBookContactRow>();
            Assert.That(row.GetComponentsInChildren<Graphic>(true).Any(g => g.raycastTarget), Is.False,
                "rows never take clicks since the Notepad went (D54)");
            Assert.That(ExecuteEvents.GetEventHandler<IPointerClickHandler>(row.gameObject), Is.EqualTo(root),
                "a click on a row reaches the book");
            ExecuteEvents.ExecuteHierarchy(row.gameObject, new PointerEventData(null)
                { position = Position(0), button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
            Assert.That(book.IsOpen, Is.False, "a row click closes the book");
            book.SetOpen(true);
            var tab = opened.GetComponentInChildren<PhoneBookTab>(true);
            for (Transform parent = tab.transform; parent != opened; parent = parent.parent) parent.gameObject.SetActive(true);
            Assert.That(ExecuteEvents.GetEventHandler<IPointerClickHandler>(tab.gameObject), Is.EqualTo(tab.gameObject));
            Assert.That(ExecuteEvents.GetEventHandler<IPointerClickHandler>(opened.gameObject), Is.EqualTo(root));
            Click(200);
            Assert.That(book.CurrentSpreadIndex, Is.EqualTo(1));
        }

        // D53: every entry shows from the start, so the pages turn through the whole book.
        [Test]
        public void PageTurningGoesThroughTheWholeBook()
        {
            ContactsConfig.SetValue(book, realFile);
            book.ReloadContacts();
            BookCatalog real = BookFileLoader.Load(realFile.text);
            Assert.That(book.ContactCount, Is.EqualTo(real.Entries.Count), "the whole book, not only the starting board");
            int spreads = (book.PageCount + 1) / 2;
            Assert.That(book.PageTurnDirectionAt(Position(-200), null), Is.Zero);
            for (int spread = 1; spread < spreads; spread++)
            {
                Assert.That(book.PageTurnDirectionAt(Position(200), null), Is.EqualTo(1));
                Click(200);
                Assert.That(book.CurrentSpreadIndex, Is.EqualTo(spread));
            }
            Assert.That(book.CanGoForward, Is.False);
            Assert.That(book.PageTurnDirectionAt(Position(200), null), Is.Zero);
            Click(900);
            Assert.That(book.CurrentSpreadIndex, Is.EqualTo(spreads - 1));
            Assert.That(book.IsOpen, Is.True);
            for (int spread = spreads - 2; spread >= 0; spread--)
            {
                Click(-200);
                Assert.That(book.CurrentSpreadIndex, Is.EqualTo(spread));
            }
            Assert.That(book.PageTurnDirectionAt(Position(-200), null), Is.Zero);
        }

        [Test]
        public void RightClickAndClosedBookDoNotTurnPages()
        {
            Click(200, 0, PointerEventData.InputButton.Right);
            Assert.That(book.CurrentSpreadIndex, Is.Zero);
            book.SetOpen(false);
            Assert.That(book.PageTurnDirectionAt(Position(200), null), Is.Zero);
        }

        [Test]
        public void RaycastSelectsPageCursorOnlyForAnAvailablePageAction() => CheckPageCursor(false, false);

        [Test]
        public void HeldCableYieldsToBookCursorAndSurvivesBookClicks() => CheckPageCursor(true, false);

        [Test]
        public void AttachedCableYieldsToBookCursorAndKeepsItsSocket() => CheckPageCursor(true, true);

        private void CheckPageCursor(bool holding, bool attached)
        {
            var previousEventSystem = EventSystem.current;
            var events = new GameObject("Book cursor events", typeof(EventSystem));
            var eventSystem = events.GetComponent<EventSystem>();
            var flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            typeof(EventSystem).GetMethod("OnEnable", flags).Invoke(eventSystem, null);
            var raycaster = root.GetComponent<GraphicRaycaster>();
            typeof(BaseRaycaster).GetMethod("OnEnable", flags).Invoke(raycaster, null);
            var camera = events.AddComponent<Camera>();
            camera.enabled = false;
            camera.transform.position = new Vector3(960, 540, -10);
            camera.orthographic = true;
            camera.orthographicSize = 540;
            var texture = new RenderTexture(1920, 1080, 24);
            camera.targetTexture = texture;
            // An open game's overlay canvas must not intercept this isolated camera-canvas test.
            var otherRaycasters = RaycasterManager.GetRaycasters().Where(r => r != raycaster && r.isActiveAndEnabled).ToArray();
            try
            {
                foreach (var other in otherRaycasters) other.enabled = false;
                EventSystem.current = eventSystem;
                var canvas = root.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 10;
                canvas.sortingOrder = 1000;
                var cursor = new GameObject("Test hand", typeof(RectTransform)).AddComponent<HandCursorGraphic>();
                cursor.transform.SetParent(root.transform, false);
                cursor.raycastTarget = false;
                var input = root.AddComponent<CableInputController>();
                CableController cable = null;
                SocketView socket = null;
                if (holding)
                {
                    var cableObject = new GameObject("Held test cable", typeof(RectTransform));
                    cableObject.transform.SetParent(root.transform, false);
                    cable = cableObject.AddComponent<CableController>();
                    cable.Configure((RectTransform)root.transform, null, null, null, null, null, null, null);
                    Assert.That(cable.TryPickUp(), Is.True);
                    if (attached)
                    {
                        var socketObject = new GameObject("Test socket", typeof(RectTransform));
                        socketObject.transform.SetParent(root.transform, false);
                        socket = socketObject.AddComponent<SocketView>();
                        socket.Configure(0, 0, (RectTransform)socketObject.transform);
                        socket.GetComponent<SocketGraphic>().raycastTarget = false;
                        Assert.That(cable.TryInsert(socket), Is.True);
                    }
                    typeof(CableInputController).GetField("heldCable", flags).SetValue(input, cable);
                }
                input.Configure(null, holding ? new[] { cable } : new CableController[0], cursor);
                Canvas.ForceUpdateCanvases();
                camera.Render();
                input.ProcessPointer(Position(200), false);
                var hits = new System.Collections.Generic.List<RaycastResult>();
                EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = Position(200) }, hits);
                string details = $"screen={Screen.width}x{Screen.height}, point={Position(200)}, direction={book.PageTurnDirectionAt(Position(200), null)}, depth={opened.GetComponent<Image>().depth}";
                foreach (var hit in hits) details += " hit=" + hit.gameObject.name;
                Assert.That(input.CursorMode, Is.EqualTo(CableCursorMode.PageTurn), details);
                Assert.That(cursor.PageTurnDirection, Is.EqualTo(1));
                Assert.That(cursor.gameObject.activeSelf, Is.True);
                if (holding)
                {
                    Assert.That(cable.IsPointerVisible, Is.False);
                    input.ProcessPointer(Position(200), true);
                    Assert.That(input.ActiveCable, Is.SameAs(cable), "A page click must not drop or reconnect the held cable");
                    Assert.That(cable.Phase, Is.EqualTo(attached ? CablePhase.OneEndConnected : CablePhase.Held));
                }
                input.ProcessPointer(Position(-200), false);
                Assert.That(input.CursorMode, Is.EqualTo(CableCursorMode.Pointing));
                Assert.That(cursor.PageTurnDirection, Is.Zero);
                Assert.That(cursor.gameObject.activeSelf, Is.True);
                if (holding) Assert.That(cable.IsPointerVisible, Is.False);
                book.NextSpread();
                input.ProcessPointer(Position(-200), false);
                Assert.That(cursor.PageTurnDirection, Is.EqualTo(-1));
                Canvas.ForceUpdateCanvases();
                var row = opened.GetComponentInChildren<PhoneBookContactRow>();
                // Only the corner folds turn pages (Sergei, 2c0d99d); over a row the hand points, and a click closes the book.
                input.ProcessPointer(row.transform.position, false);
                Assert.That(input.CursorMode, Is.EqualTo(CableCursorMode.Pointing));
                Assert.That(cursor.PageTurnDirection, Is.Zero);
                if (holding)
                {
                    Assert.That(input.ActiveCable, Is.SameAs(cable));
                    Assert.That(cable.IsPointerVisible, Is.False);
                    input.ProcessPointer(new Vector2(100, 800), false);
                    Assert.That(input.CursorMode, Is.EqualTo(CableCursorMode.Plug));
                    Assert.That(cursor.gameObject.activeSelf, Is.False);
                    Assert.That(cable.IsPointerVisible, Is.True);
                    if (attached) Assert.That(socket.OccupyingCable, Is.SameAs(cable));
                    input.ProcessPointer(row.transform.position, false);
                    Assert.That(cursor.gameObject.activeSelf, Is.True);
                    Assert.That(cable.IsPointerVisible, Is.False);
                }
                book.SetOpen(false);
                input.ProcessPointer(Position(200), false);
                Assert.That(input.CursorMode, Is.EqualTo(holding ? CableCursorMode.Plug : CableCursorMode.Pointing));
            }
            finally
            {
                typeof(EventSystem).GetMethod("OnDisable", flags).Invoke(eventSystem, null);
                typeof(BaseRaycaster).GetMethod("OnDisable", flags).Invoke(raycaster, null);
                camera.targetTexture = null;
                texture.Release();
                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(events);
                if (previousEventSystem != null) EventSystem.current = previousEventSystem;
                foreach (var other in otherRaycasters) if (other != null) other.enabled = true;
            }
        }
    }
}
