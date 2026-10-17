using System.Collections;
using Switchboard.Content;
using Switchboard.Domain;
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
        [SerializeField] private DirectoryListView directoryList;

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
                if (loading == null) yield break;
                yield return loading;
                level = SceneManager.GetSceneByName(levelScene);
            }

            cordBridge.Attach(level);
            int seed = randomSeed != 0 ? randomSeed : Random.Range(1, int.MaxValue);
            Session = new GameSession(config, new SeededRandom(seed), cordBridge);
            cordBridge.Bind(Session.Desk);
            moneyCounter.Bind(Session.Wallet);
            callQueue.Bind(Session.Calls, Session.Desk, LevelScene.FindCallerSlots(level));
            directoryList.Bind(Session.Directory);
            loopRunner.Bind(Session);
        }
    }
}
