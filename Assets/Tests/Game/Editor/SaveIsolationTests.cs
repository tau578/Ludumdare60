using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Switchboard.Content;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Switchboard.Game.Tests
{
    /// <summary>EditMode tests build the game outside Play Mode; they must never read or write the player's save (9a).</summary>
    public sealed class SaveIsolationTests
    {
        private const string Planted = "{\"version\":1,\"cents\":777,\"upgrades\":[\"board_1\"]}";

        [Test]
        public void AnEditModeGameIgnoresAndKeepsTheRealSave()
        {
            string main = PlayerPrefs.HasKey(SaveFile.MainKey) ? PlayerPrefs.GetString(SaveFile.MainKey) : null;
            Scene scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Final.unity");
            try
            {
                PlayerPrefs.SetString(SaveFile.MainKey, Planted);
                foreach (GameObject root in scene.GetRootGameObjects())
                    if (PrefabUtility.IsOutermostPrefabInstanceRoot(root))
                        PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                GameInstaller installer = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<GameInstaller>(true)).Single();
                typeof(GameInstaller).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(installer, null);

                Assert.That(installer.Session.IsRestored, Is.False);
                Assert.That(installer.Session.Upgrades.OwnsAny, Is.False);
                Assert.That(installer.GetComponent<Autosave>(), Is.Null);
                installer.Session.Wallet.Earn(5000);
                Object.DestroyImmediate(installer.gameObject);
                Assert.That(PlayerPrefs.GetString(SaveFile.MainKey), Is.EqualTo(Planted));
            }
            finally
            {
                if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
                if (main != null) PlayerPrefs.SetString(SaveFile.MainKey, main);
                else PlayerPrefs.DeleteKey(SaveFile.MainKey);
                PlayerPrefs.Save();
            }
        }
    }
}
