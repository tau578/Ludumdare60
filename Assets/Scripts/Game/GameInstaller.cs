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
        [SerializeField, Tooltip("0 picks a new seed every session.")] private int randomSeed;
        [SerializeField] private CordBridge cordBridge;
        [SerializeField] private GameLoopRunner loopRunner;

        private NotepadBookLink bookLink;
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
            MoneyCounterView moneyCounter = FindSingle<MoneyCounterView>(scene);
            CallQueueView callQueue = FindSingle<CallQueueView>(scene);
            ShopView shop = FindSingle<ShopView>(scene);
            GoalPanelView goalPanel = FindSingle<GoalPanelView>(scene);
            if (moneyCounter == null || callQueue == null || shop == null || goalPanel == null) return;

            // People reference the economy file's call types by id, so the economy file loads first.
            EconomyCatalog economy = config.LoadEconomy();
            foreach (string error in economy.Errors) Debug.LogError($"Economy file: {error}", config);
            List<SocketView> sockets = LevelScene.FindAll<SocketView>(scene);
            PeopleCatalog people = config.LoadPeople(economy, BoardJacks(sockets));
            foreach (string error in people.Errors) Debug.LogError($"People file: {error}", config);
            foreach (string error in ContentValidator.ValidateStartingBoard(people.Directory, economy.StartingBoard))
                Debug.LogError($"People file: {error}", config);

            // Fewer cords than startingCords are added below like bought ones; more can't be taken away.
            cordBridge.Attach(scene, config.ExtraCordColors);
            if (cordBridge.CordCount > economy.StartingCords)
                Debug.LogWarning($"The scene has {cordBridge.CordCount} cords but Economy.json starts with {economy.StartingCords}.", config);
            int seed = randomSeed != 0 ? randomSeed : Random.Range(1, int.MaxValue);
            Session = new GameSession(new GameContent(people, economy), new SeededRandom(seed), cordBridge);
            cordBridge.Bind(Session.Desk, Session.Calls);
            cordBridge.EnsureCords(Session.Stats.Cords);
            Session.Stats.Changed += OnStatsChanged;
            moneyCounter.Bind(Session.Wallet, LevelScene.FindShopHeader(scene), Session.Penalties, Session.Upgrades);
            RectTransform[] callerSlots = LevelScene.FindCallerSlots(scene);
            if (callerSlots.Length < economy.MostClientSlots)
                Debug.LogWarning($"The caller panel has {callerSlots.Length} slots, but Economy.json can open {economy.MostClientSlots} " +
                    "(startingClientSlots or an extra_clients clientSlots). Lower the file's values or add slots to the panel.", config);
            callQueue.Bind(Session.Calls, Session.Desk, Session.Earnings, Session.Penalties, callerSlots, PortraitOf,
                cordBridge.ColorOf, CardStyleOf, notes: Session.Notepad,
                syncBoard: cordBridge.Synchronize, counter: moneyCounter);
            PhoneBookView book = FindSingle<PhoneBookView>(scene);
            if (book != null) bookLink = new NotepadBookLink(book, Session.Stats, Session.Notepad);
            boardLink = new OpenBoardLink(sockets, book, callQueue, Session.Stats, Session.Board);
            shop.Bind(Session.Upgrades, Session.Shelf, Session.Goal, Session.Wallet, LevelScene.FindAll<UpgradeSlotView>(scene));
            goalPanel.Bind(Session.Goal);
            loopRunner.Bind(Session);
            if (TryGetComponent(out CallAudio audio)) audio.Bind(Session.Calls);
            var guide = GetComponent<FirstOrderGuide>();
            if (guide == null) guide = gameObject.AddComponent<FirstOrderGuide>();
            guide.Bind(callQueue, Session.Calls, book,
                LevelScene.FindAll<CableController>(scene).ToArray(), cordBridge);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Added at runtime so no scene or prefab references a type that release builds leave out.
            gameObject.AddComponent<DevPanel>().Bind(Session, callQueue);
#endif
        }

        private void OnDestroy()
        {
            if (Session != null) Session.Stats.Changed -= OnStatsChanged;
            bookLink?.Dispose();
            bookLink = null;
            boardLink?.Dispose();
            boardLink = null;
        }

        // Cords are only ever added: one can't be taken away safely mid-call.
        private void OnStatsChanged() => cordBridge.EnsureCords(Session.Stats.Cords);

        private Sprite PortraitOf(IPerson person) => person is PersonRecord record ? config.Portrait(record.Portrait) : null;

        private CallCardStyle CardStyleOf(ICallerType type) => type != null ? config.CardStyleOf(type.Id) : default;

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
