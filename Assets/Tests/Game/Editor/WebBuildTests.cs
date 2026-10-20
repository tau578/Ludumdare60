using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Switchboard.Domain;
using Switchboard.Game;
using Switchboard.Game.Editor;
using Switchboard.UI;
using UnityEditor;

namespace Switchboard.Game.Tests
{
    /// <summary>Step 9b: where saves live, the volume patch's storage, and the committed Web build settings.</summary>
    public sealed class WebBuildTests
    {
        private const string TemplateIndex = "Assets/WebGLTemplates/Operator/index.html";
        private const string Jslib = "Assets/Scripts/Game/Save/BrowserStorage.jslib";
        private const string PointerJslib = "Assets/Scripts/UI/SystemPointer.jslib";
        private const string PointerScript = "Assets/Scripts/UI/SystemPointer.cs";

        private sealed class MemoryStorage : ISaveStorage
        {
            public readonly Dictionary<string, string> Values = new Dictionary<string, string>();
            public string Read(string key) => Values.TryGetValue(key, out string value) ? value : null;
            public void Write(string key, string value) => Values[key] = value;
            public void Delete(string key) => Values.Remove(key);
        }

        [Test]
        public void TheEditorSavesInPlayerPrefs()
        {
            Assert.That(SaveStorage.Create(), Is.InstanceOf<PlayerPrefsStorage>());
            Assert.That(BrowserStorage.IsAvailable(), Is.False, "there is no browser storage outside Web players");
        }

        [Test]
        public void DesktopNeverAsksTheBrowser()
        {
            bool probed = false;
            ISaveStorage storage = SaveStorage.Choose(false, () => probed = true, () => new MemoryStorage(), null);
            Assert.That(storage, Is.InstanceOf<PlayerPrefsStorage>());
            Assert.That(probed, Is.False);
        }

        [Test]
        public void AWebPlayerUsesTheBrowserWhileItsStorageWorks()
        {
            var browser = new MemoryStorage();
            int warnings = 0;
            Assert.That(SaveStorage.Choose(true, () => true, () => browser, () => warnings++), Is.SameAs(browser));
            Assert.That(warnings, Is.Zero);
        }

        [Test]
        public void AWebPlayerWithBlockedStorageFallsBackToPlayerPrefsWithAWarning()
        {
            int warnings = 0;
            ISaveStorage storage = SaveStorage.Choose(true, () => false, () => new MemoryStorage(), () => warnings++);
            Assert.That(storage, Is.InstanceOf<PlayerPrefsStorage>());
            Assert.That(warnings, Is.EqualTo(1));
        }

        [Test]
        public void TheVolumePatchIsStoredUnderItsKeyAndReadBack()
        {
            var storage = new MemoryStorage();
            var patch = new VolumePatchState();
            Assert.That(patch.Restore(new[] { 2, 0, 1 }, new[] { 0, 4, 3 }), Is.True);
            AudioVolumeSettings.Save(patch, storage);

            Assert.That(AudioVolumeSettings.SaveKey, Is.EqualTo("Switchboard.AudioPatch.v1"));
            Assert.That(storage.Values.Keys, Is.EquivalentTo(new[] { AudioVolumeSettings.SaveKey }));
            Assert.That(storage.Values[AudioVolumeSettings.SaveKey],
                Is.EqualTo("{\"version\":1,\"rows\":[2,0,1],\"sockets\":[0,4,3],\"order\":[0,1,2]}"));

            VolumePatchState loaded = AudioVolumeSettings.Load(storage);
            for (int i = 0; i < 3; i++)
            {
                Assert.That(loaded.RowOf(i), Is.EqualTo(patch.RowOf(i)));
                Assert.That(loaded.SocketOf(i), Is.EqualTo(patch.SocketOf(i)));
            }
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("not json")]
        [TestCase("{\"version\":2,\"rows\":[0,1,2],\"sockets\":[3,3,3]}")]
        [TestCase("{\"version\":1,\"rows\":[0,0,2],\"sockets\":[3,3,3]}")]
        public void AMissingOrBrokenPatchGivesTheDefault(string stored)
        {
            var storage = new MemoryStorage();
            if (stored != null) storage.Values[AudioVolumeSettings.SaveKey] = stored;
            VolumePatchState loaded = AudioVolumeSettings.Load(storage);
            for (int i = 0; i < 3; i++)
            {
                Assert.That(loaded.RowOf(i), Is.EqualTo(i));
                Assert.That(loaded.SocketOf(i), Is.EqualTo(1), "the default patch is 50% everywhere");
            }
        }

        [Test]
        public void TheCommittedPlayerSettingsBuildGzipWithFallbackAndOurTemplate()
        {
            Assert.That(PlayerSettings.WebGL.compressionFormat, Is.EqualTo(WebGLCompressionFormat.Gzip));
            Assert.That(PlayerSettings.WebGL.decompressionFallback, Is.True, "itch can't send compression headers");
            Assert.That(PlayerSettings.WebGL.template, Is.EqualTo(WebBuild.Template));
            Assert.That(PlayerSettings.WebGL.threadsSupport, Is.False, "itch's SharedArrayBuffer option stays off");
        }

        [Test]
        public void TheTemplateHasWhatTheLoaderNeeds()
        {
            Assert.That(File.Exists(TemplateIndex), Is.True, TemplateIndex);
            string html = File.ReadAllText(TemplateIndex);
            foreach (string variable in new[] { "LOADER_FILENAME", "DATA_FILENAME", "FRAMEWORK_FILENAME", "CODE_FILENAME" })
                Assert.That(html, Does.Contain("{{{ " + variable + " }}}"), variable);
            Assert.That(html, Does.Contain("createUnityInstance"));
            Assert.That(html, Does.Contain("showBanner: unityShowBanner"), "errors stay visible on a phone");
            Assert.That(html, Does.Contain("name=\"viewport\""), "phones mustn't zoom out");
            Assert.That(html, Does.Contain("<title>Operator!</title>"));
            Assert.That(html, Does.Not.Contain("unity-footer"));
        }

        [Test]
        public void TheBuildListIsMainMenuThenFinal()
        {
            Assert.That(WebBuild.EnabledScenes(),
                Is.EqualTo(new[] { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/Final.unity" }));
        }

        [Test]
        public void TheFilesTheBuildRestoresExist()
        {
            foreach (string path in WebBuild.RestoredFiles)
                Assert.That(File.Exists(path), Is.True, path);
        }

        [Test]
        public void RestoreWritesBackOnlyTheFilesThatChanged()
        {
            string folder = FileUtil.GetUniqueTempPathInProject();
            Directory.CreateDirectory(folder);
            try
            {
                string kept = Path.Combine(folder, "kept.asset");
                string changed = Path.Combine(folder, "changed.asset");
                File.WriteAllText(kept, "same");
                File.WriteAllText(changed, "before");
                Dictionary<string, byte[]> snapshot = WebBuild.Snapshot(new[] { kept, changed, Path.Combine(folder, "missing.asset") });
                Assert.That(snapshot.Keys, Is.EquivalentTo(new[] { kept, changed }), "a missing file isn't created");

                File.WriteAllText(changed, "after the build");
                Assert.That(WebBuild.Restore(snapshot), Is.EqualTo(new[] { changed }));
                Assert.That(File.ReadAllText(changed), Is.EqualTo("before"));
                Assert.That(File.ReadAllText(kept), Is.EqualTo("same"));
            }
            finally
            {
                Directory.Delete(folder, true);
            }
        }

        [TestCase(Jslib)]
        [TestCase(PointerJslib)]
        public void TheBrowserPluginIsForWebPlayersOnly(string path)
        {
            var importer = (PluginImporter)AssetImporter.GetAtPath(path);
            Assert.That(importer, Is.Not.Null, path);
            Assert.That(importer.GetCompatibleWithPlatform(BuildTarget.WebGL), Is.True);
            Assert.That(importer.GetCompatibleWithEditor(), Is.False);
            Assert.That(importer.GetCompatibleWithAnyPlatform(), Is.False);
            Assert.That(importer.GetCompatibleWithPlatform(BuildTarget.StandaloneOSX), Is.False);
            Assert.That(importer.GetCompatibleWithPlatform(BuildTarget.StandaloneWindows64), Is.False);
        }

        // 9b: Chrome on some Mac displays shows a cursor: none pointer again on every mouse move, so the browser hides it
        // with a transparent image, and only SystemPointer may touch Cursor.visible.
        [Test]
        public void OnlySystemPointerTouchesCursorVisible()
        {
            foreach (string file in Directory.GetFiles("Assets", "*.cs", SearchOption.AllDirectories))
            {
                string path = file.Replace('\\', '/');
                if (path == PointerScript || path.StartsWith("Assets/Tests/")) continue;
                Assert.That(File.ReadAllText(path), Does.Not.Contain("Cursor.visible"), path + " must use SystemPointer.Visible");
            }
        }

        [Test]
        public void TheBrowserHidesThePointerWithATransparentImage()
        {
            string js = File.ReadAllText(PointerJslib);
            Assert.That(js, Does.Contain("url(data:image/png;base64,"));
            Assert.That(js, Does.Not.Contain("'none'").And.Not.Contain("\"none\""));
        }
    }
}
