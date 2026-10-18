using System.Collections;
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
        [SerializeField] private string levelScene = "Level";
        [SerializeField, Tooltip("0 picks a new seed every session.")] private int randomSeed;
        [SerializeField] private CordBridge cordBridge;
        [SerializeField] private GameLoopRunner loopRunner;
        [SerializeField] private MoneyCounterView moneyCounter;
        [SerializeField] private CallQueueView callQueue;

        public GameSession Session { get; private set; }

        private IEnumerator Start()
        {
            if (config == null)
            {
                Debug.LogError("GameInstaller needs a GameConfig.", this);
                yield break;
            }

            Scene level = SceneManager.GetSceneByName(levelScene);
            if (!level.isLoaded)
            {
                AsyncOperation loading = SceneManager.LoadSceneAsync(levelScene, LoadSceneMode.Additive);
                if (loading != null) yield return loading;
                level = SceneManager.GetSceneByName(levelScene);
            }
            if (!level.isLoaded)
            {
                Debug.LogError($"GameInstaller could not load the scene '{levelScene}'. Is it in the build list?", this);
                yield break;
            }

            PeopleCatalog people = config.LoadPeople(BoardJacks(level));
            foreach (string error in people.Errors) Debug.LogError($"People file: {error}", config);

            cordBridge.Attach(level);
            int seed = randomSeed != 0 ? randomSeed : Random.Range(1, int.MaxValue);
            Session = new GameSession(new GameContent(config, people), new SeededRandom(seed), cordBridge);
            cordBridge.Bind(Session.Desk);
            moneyCounter.Bind(Session.Wallet, LevelScene.FindShopHeader(level), Session.Penalties);
            RectTransform[] callerSlots = LevelScene.FindCallerSlots(level);
            if (callerSlots.Length < config.MaxWaitingCalls)
                Debug.LogWarning($"The caller panel has {callerSlots.Length} slots but GameConfig allows {config.MaxWaitingCalls} calls.", config);
            callQueue.Bind(Session.Calls, Session.Desk, Session.Earnings, Session.Penalties, callerSlots, PortraitOf,
                cordBridge.ColorOf, CardStyleOf);
            loopRunner.Bind(Session);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Added at runtime so no scene or prefab references a type that release builds leave out.
            gameObject.AddComponent<DevPanel>().Bind(Session, callQueue);
#endif
        }

        private Sprite PortraitOf(IPerson person) => person is PersonRecord record ? config.Portrait(record.Portrait) : null;

        private CallCardStyle CardStyleOf(ICallerType type) => type != null ? config.CardStyleOf(type.Id) : default;

        private static List<string> BoardJacks(Scene level)
        {
            var jacks = new List<string>();
            foreach (SocketView socket in LevelScene.FindAll<SocketView>(level)) jacks.Add(socket.Address);
            return jacks;
        }
    }
}
