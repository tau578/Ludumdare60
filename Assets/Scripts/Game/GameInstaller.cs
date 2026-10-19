using System.Collections.Generic;
using Switchboard.Content;
using Switchboard.Domain;
using Switchboard.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Switchboard.Game
{
    /// <summary>The composition root and the game's only entry point (Design 4.4).</summary>
    [DefaultExecutionOrder(-100)]
    public sealed class GameInstaller : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [SerializeField] private CordBridge cordBridge;
        [SerializeField] private GameLoopRunner loopRunner;

        private OpenBoardLink boardLink;

        public GameSession Session { get; private set; }

        private void Start()
        {
            if (config == null)
            {
                Debug.LogError("GameInstaller needs a GameConfig.", this);
                return;
            }

            // Board, views and this root share one scene (D38). Views live in other prefabs, so they are found
            // by type at start instead of through serialized links that a scene override could break.
            Scene scene = gameObject.scene;
            foreach (var skin in LevelScene.FindAll<UpgradeShopSkin>(scene)) skin.Apply();
            MoneyCounterView moneyCounter = FindSingle<MoneyCounterView>(scene);
            CallQueueView callQueue = FindSingle<CallQueueView>(scene);
            ShopView shop = FindSingle<ShopView>(scene);
            GoalPanelView goalPanel = FindSingle<GoalPanelView>(scene);
            if (moneyCounter == null || callQueue == null || shop == null || goalPanel == null) return;

            // The economy file's pools refer to Calls.json, so the calls load first.
            CallsCatalog calls = config.LoadCalls();
            foreach (string error in calls.Errors) Debug.LogError($"Calls file: {error}", config);
            EconomyCatalog economy = config.LoadEconomy(calls);
            foreach (string error in economy.Errors) Debug.LogError($"Economy file: {error}", config);
            List<SocketView> sockets = LevelScene.FindAll<SocketView>(scene);
            BookCatalog book = config.LoadBook(BoardJacks(sockets));
            foreach (string error in book.Errors) Debug.LogError($"Book file: {error}", config);
            foreach (string error in ContentValidator.ValidateCallJacks(calls.Calls, book.Entries))
                Debug.LogError($"Calls file: {error}", config);

            // Trim unused prefab cords before binding; missing starting cords are added below.
            cordBridge.Attach(scene, config.ExtraCordColors, economy.StartingCords);
            if (cordBridge.CordCount > economy.StartingCords)
                Debug.LogWarning($"The scene has {cordBridge.CordCount} cords but Economy.json starts with {economy.StartingCords}.", config);
            Session = new GameSession(new GameContent(economy), cordBridge, tutorial: true);
            cordBridge.Bind(Session.Desk, Session.Calls);
            cordBridge.EnsureCords(Session.Stats.Cords);
            Session.Stats.Changed += OnStatsChanged;
            moneyCounter.Bind(Session.Wallet, LevelScene.FindShopHeader(scene), Session.Penalties, Session.Upgrades);
            RectTransform[] callerSlots = LevelScene.FindCallerSlots(scene);
            if (callerSlots.Length < economy.MostClientSlots)
                Debug.LogWarning($"The caller panel has {callerSlots.Length} slots, but Economy.json can open {economy.MostClientSlots} " +
                    "(startingClientSlots or an extra_clients clientSlots). Lower the file's values or add slots to the panel.", config);
            callQueue.Bind(Session.Calls, Session.Desk, Session.Earnings, Session.Penalties, callerSlots, PortraitOf,
                cordBridge.ColorOf, config.CardStyleFor, syncBoard: cordBridge.Synchronize, counter: moneyCounter);
            PhoneBookView bookView = FindSingle<PhoneBookView>(scene);
            boardLink = new OpenBoardLink(sockets, callQueue, Session.Stats, Session.Board);
            shop.Bind(Session.Upgrades, Session.Shelf, Session.Goal, Session.Wallet, LevelScene.FindAll<UpgradeSlotView>(scene), moneyCounter);
            goalPanel.Bind(Session.Goal);
            loopRunner.Bind(Session);
            if (TryGetComponent(out CallAudio audio)) audio.Bind(Session.Calls);
            var guide = GetComponent<FirstOrderGuide>();
            if (guide == null) guide = gameObject.AddComponent<FirstOrderGuide>();
            guide.Bind(callQueue, Session.Calls, bookView,
                LevelScene.FindAll<CableController>(scene).ToArray(), cordBridge);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Added at runtime so no scene or prefab references a type that release builds leave out.
            gameObject.AddComponent<DevPanel>().Bind(Session, callQueue);
#endif
        }

        private void OnDestroy()
        {
            if (Session != null) Session.Stats.Changed -= OnStatsChanged;
            boardLink?.Dispose();
            boardLink = null;
        }

        // Cords are only ever added: one can't be taken away safely mid-call.
        private void OnStatsChanged() => cordBridge.EnsureCords(Session.Stats.Cords);

        private Sprite PortraitOf(CallScript call) => call != null ? config.Portrait(call.Icon) : null;

        private T FindSingle<T>(Scene scene) where T : Component
        {
            List<T> found = LevelScene.FindAll<T>(scene);
            if (found.Count == 1) return found[0];
            Debug.LogError($"GameInstaller needs exactly one {typeof(T).Name} in the scene '{scene.name}', but found {found.Count}.", this);
            return null;
        }

        private static List<string> BoardJacks(List<SocketView> sockets)
        {
            var jacks = new List<string>(sockets.Count);
            foreach (SocketView socket in sockets) jacks.Add(socket.Address);
            return jacks;
        }
    }
}
