using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Switchboard.Content;
using Switchboard.Domain;
using Switchboard.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Switchboard.Game.Tests
{
    public sealed class MergedSystemsTests
    {
        private Scene scene;
        private GameSession session;
        private CallQueueView queue;
        private CordBridge bridge;
        private PhoneBookView book;

        private T[] All<T>() where T : Component => scene.GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<T>(true)).ToArray();
        private static void Invoke(object target, string method, params object[] args) => target.GetType()
            .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
        private static T Field<T>(object target, string name) => (T)target.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);

        [SetUp]
        public void SetUp()
        {
            // A preview scene keeps the user's open scene and unsaved edits intact.
            scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Final.unity");
            // Startup removes spare tray objects; allow those runtime changes only in the disposable scene.
            foreach (var transform in All<Transform>().Where(t => PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)).ToArray())
                if (PrefabUtility.IsOutermostPrefabInstanceRoot(transform.gameObject))
                    PrefabUtility.UnpackPrefabInstance(transform.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            Canvas.ForceUpdateCanvases();
            foreach (var canvas in All<Canvas>().Where(c => c.isRootCanvas))
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)canvas.transform);
            book = All<PhoneBookView>().Single();
            book.SetOpen(false);
            var installer = All<GameInstaller>().Single();
            Invoke(installer, "Start");
            session = installer.Session;
            Assert.That(session, Is.Not.Null);
            queue = All<CallQueueView>().Single();
            bridge = All<CordBridge>().Single();
        }

        [TearDown]
        public void TearDown()
        {
            if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
        }

        private EconomyCatalog Economy() =>
            AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/Content/GameConfig.asset").LoadEconomy();

        private CableController[] ActiveCables() => All<CableController>().Where(c => c.gameObject.activeInHierarchy).ToArray();

        // Buys the first extra cord and returns the cable it added; Economy.json sets how many cords that is.
        private CableController BuyFirstCord()
        {
            CableController[] before = ActiveCables();
            IUpgrade first = session.Upgrades.ChainOf(UpgradeKind.ExtraWires)[0];
            Assert.That(session.Upgrades.TryBuy(first.Id), Is.True, first.Id);
            Assert.That(bridge.CordCount, Is.EqualTo(first.Wires));
            return ActiveCables().Except(before).Last();
        }

        private CallCardView AcceptOrder()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/Content/GameConfig.asset");
            // The game starts on a small board (D42): the order is the start pool's first call, on open jacks.
            CallScript first = config.LoadEconomy().StartPool[0];
            var script = new CallScript("test_order", first.Name, first.Icon, first.Message, first.From, first.To, 30, 100, 10);
            var call = session.Calls.Add(new CallRequest(script));
            var card = All<CallCardView>().Single(c => c.Call == call);
            Invoke(queue, "OnCardClicked", card);
            return card;
        }

        // Economy.json sets the starting board, slots and cords and every upgrade (the designer balances it at any
        // time), so the expected counts come from the file.
        [Test]
        public void FreshGameHidesLockedPlacesAndShopPurchasesRevealThem()
        {
            EconomyCatalog economy = Economy();
            Assert.That(session.Stats.Cords, Is.EqualTo(economy.StartingCords));
            Assert.That(bridge.CordCount, Is.EqualTo(economy.StartingCords));
            Assert.That(ActiveCables().Length, Is.EqualTo(economy.StartingCords));
            Assert.That(ActiveCables().All(c => c.TrayView.Artwork != null && c.TrayView.Artwork.enabled), Is.True);
            var sockets = All<SocketView>();
            BoardSize start = economy.StartingBoard;
            Assert.That(sockets.Count(s => s.GetComponent<SocketGraphic>().enabled), Is.EqualTo(start.Cols * start.Rows));
            Assert.That(sockets.Where(s => !s.IsOpen).All(s => !s.GetComponent<SocketGraphic>().raycastTarget), Is.True);
            var callerSlots = Field<RectTransform[]>(queue, "slots");
            Assert.That(callerSlots.Count(s => s.GetComponent<CanvasGroup>().alpha > 0), Is.EqualTo(economy.StartingClientSlots),
                "D52: the start's caller slots");
            var shop = All<ShopView>().Single();
            session.Wallet.Earn(session.Upgrades.All.Sum(u => u.CostCents));
            Invoke(shop, "LateUpdate");
            var slots = All<UpgradeSlotView>().OrderByDescending(s => s.transform.position.y).ToArray();
            int cordSlot = Enumerable.Range(0, ShopShelf.SlotCount).Single(i => session.Shelf.OfferAt(i)?.Kind == UpgradeKind.ExtraWires);
            int boardSlot = Enumerable.Range(0, ShopShelf.SlotCount).Single(i => session.Shelf.OfferAt(i)?.Kind == UpgradeKind.ExtraClients);
            Assert.That(Field<TMPro.TMP_Text>(slots[cordSlot], "title").text, Is.EqualTo("More Cables"));
            Assert.That(Field<TMPro.TMP_Text>(slots[boardSlot], "title").text, Is.EqualTo("More Clients"));
            Assert.That(Field<Image>(slots[boardSlot], "background").sprite.name, Does.Contain("buy_btn"));
            IUpgrade board = session.Shelf.OfferAt(boardSlot);
            long before = session.Wallet.Cents;
            Field<Button>(slots[boardSlot], "button").onClick.Invoke();
            Assert.That(session.Wallet.Cents, Is.EqualTo(before - board.CostCents));
            Assert.That(sockets.Count(s => s.GetComponent<SocketGraphic>().enabled), Is.EqualTo(board.BoardSize.Cols * board.BoardSize.Rows));
            Assert.That(callerSlots.Count(s => s.GetComponent<CanvasGroup>().alpha > 0), Is.EqualTo(board.ClientSlots));
            IUpgrade cord = session.Shelf.OfferAt(cordSlot);
            Field<Button>(slots[cordSlot], "button").onClick.Invoke();
            Assert.That(bridge.CordCount, Is.EqualTo(cord.Wires));
            var card = AcceptOrder();
            Assert.That(Field<TMPro.TMP_Text>(card, "speechLabel").text, Is.EqualTo(card.Call.Request.Script.Message),
                "D51: the bubble says the call's own message");
            var name = Field<TMPro.TMP_Text>(card, "nameLabel");
            Assert.That(name.text, Is.EqualTo(card.Call.Request.Script.Name), "the caller's name under the portrait");
            Assert.That(name.raycastTarget, Is.False);
            foreach (IUpgrade more in session.Upgrades.ChainOf(UpgradeKind.ExtraWires).Skip(1))
                Assert.That(session.Upgrades.TryBuy(more.Id), Is.True, more.Id);
            Assert.That(ActiveCables().Length, Is.EqualTo(session.Stats.Cords));
            Assert.That(ActiveCables().Select(c => c.CableColor).Distinct().Count(), Is.EqualTo(session.Stats.Cords),
                "every cord has its own colour: removed tray cords come back with theirs before the extra colours");
        }

        [Test]
        public void NewCordKeepsConnectionHighlightsCoinsAndAudio()
        {
            var card = AcceptOrder();
            session.Wallet.Earn(session.Upgrades.All.Sum(u => u.CostCents));
            var call = card.Call;
            var first = call.Request.From;
            var second = call.Request.To;
            var cable = BuyFirstCord();
            var a = All<SocketView>().Single(s => new JackId(s.Address) == first);
            var b = All<SocketView>().Single(s => new JackId(s.Address) == second);
            int audioCues = 0;
            All<CallAudio>().Single().ConnectionSucceeded += () => audioCues++;
            long before = session.Wallet.Cents;
            Assert.That(cable.TryPickUp() && cable.TryInsert(a), Is.True);
            bridge.Synchronize();
            Assert.That(cable.TryInsert(b), Is.True);
            bridge.Synchronize();
            Assert.That(call.State, Is.EqualTo(CallState.Connected));
            Assert.That(a.GetComponent<SocketGraphic>().IsCallConnected && b.GetComponent<SocketGraphic>().IsCallConnected, Is.True);
            Assert.That(a.GetComponentInChildren<SocketHaloGraphic>(), Is.Not.Null);
            Assert.That(b.GetComponentInChildren<SocketHaloGraphic>(), Is.Not.Null);
            var coins = All<IncomeCoinsView>().Single();
            Assert.That(coins.IsFlying, Is.True);
            Assert.That(session.Wallet.Cents, Is.EqualTo(before + 100));
            Assert.That(audioCues, Is.EqualTo(1));
            coins.Tick(2);
            Assert.That(cable.TryRemove(b), Is.True);
            bridge.Synchronize();
            Assert.That(call.State, Is.EqualTo(CallState.OnHold));
            Assert.That(a.GetComponent<SocketGraphic>().IsCallConnected || b.GetComponent<SocketGraphic>().IsCallConnected, Is.False);
            Assert.That(a.GetComponentInChildren<SocketHaloGraphic>(), Is.Null);
            Assert.That(b.GetComponentInChildren<SocketHaloGraphic>(), Is.Null);
            Assert.That(cable.TryInsert(b), Is.True);
            bridge.Synchronize();
            Assert.That(call.State, Is.EqualTo(CallState.Connected));
            Assert.That(audioCues, Is.EqualTo(2));
            Assert.That(coins.IsFlying, Is.False, "Resume must not pay or replay reward coins");
            Assert.That(session.Wallet.Cents, Is.EqualTo(before + 100));
        }
    }
}
