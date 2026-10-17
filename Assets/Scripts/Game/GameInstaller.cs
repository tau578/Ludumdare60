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
            moneyCounter.Bind(Session.Wallet, LevelScene.FindShopHeader(level));
            callQueue.Bind(Session.Calls, Session.Desk, Session.Earnings, LevelScene.FindCallerSlots(level), PortraitOf);
            loopRunner.Bind(Session);
        }

        private Sprite PortraitOf(IPerson person) => person is PersonRecord record ? config.Portrait(record.Portrait) : null;

        private static List<string> BoardJacks(Scene level)
        {
            var jacks = new List<string>();
            foreach (SocketView socket in LevelScene.FindAll<SocketView>(level)) jacks.Add(socket.Address);
            return jacks;
        }
    }
}
