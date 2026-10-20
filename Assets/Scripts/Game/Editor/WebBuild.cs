using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Switchboard.Game.Editor
{
    /// <summary>
    /// Tools/Switchboard/Build Web (9b, D26): a release Web build of the build list into Builds/Web, ready for
    /// <c>butler push Builds/Web llazypixell/operator:html5</c>. The editor stays on the Web platform afterwards.
    /// </summary>
    public static class WebBuild
    {
        public const string Template = "PROJECT:Operator";
        public const string OutputFolder = "Builds/Web";
        public const string PushCommand = "butler push Builds/Web llazypixell/operator:html5";

        /// <summary>
        /// Files a build re-saves with no change anyone wants in git (URP shader-stripping flags, glyph caches, format
        /// only). <see cref="Build"/> writes their bytes back afterwards, so nobody has to undo them by hand.
        /// </summary>
        public static readonly string[] RestoredFiles =
        {
            "Assets/Settings/UniversalRP.asset",
            "Assets/UniversalRenderPipelineGlobalSettings.asset",
            "Assets/Fonts/BoecklinsUniverse/Boecklins Universe SDF.asset",
            "Assets/Fonts/DellaRespira/Della Respira SDF.asset",
            "ProjectSettings/TimeManager.asset"
        };

        [MenuItem("Tools/Switchboard/Build Web")]
        public static void Build()
        {
            // Saved first, so an intended change still unsaved in the editor is part of what gets restored.
            AssetDatabase.SaveAssets();
            Dictionary<string, byte[]> files = Snapshot(RestoredFiles);
            // The Input System adds its settings to the preloaded assets for the build and leaves them there.
            UnityEngine.Object[] preloaded = PlayerSettings.GetPreloadedAssets();
            try
            {
                BuildWeb();
            }
            finally
            {
                PlayerSettings.SetPreloadedAssets(preloaded);
                AssetDatabase.SaveAssets();
                foreach (string path in Restore(files))
                    if (path.StartsWith("Assets/")) AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
        }

        public static Dictionary<string, byte[]> Snapshot(IEnumerable<string> paths)
        {
            var files = new Dictionary<string, byte[]>();
            foreach (string path in paths)
                if (File.Exists(path)) files[path] = File.ReadAllBytes(path);
            return files;
        }

        /// <summary>Writes back every file whose bytes changed and returns their paths.</summary>
        public static List<string> Restore(Dictionary<string, byte[]> files)
        {
            var restored = new List<string>();
            foreach (KeyValuePair<string, byte[]> file in files)
            {
                if (File.Exists(file.Key) && SameBytes(File.ReadAllBytes(file.Key), file.Value)) continue;
                File.WriteAllBytes(file.Key, file.Value);
                restored.Add(file.Key);
            }
            return restored;
        }

        private static bool SameBytes(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
                if (a[i] != b[i]) return false;
            return true;
        }

        private static void BuildWeb()
        {
            ApplyWebSettings();
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL &&
                !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            {
                Debug.LogError("Build Web: the editor couldn't switch to the Web platform. Is Web Build Support installed for this Unity version?");
                return;
            }

            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string output = Path.Combine(projectRoot, OutputFolder);
            // butler uploads the whole folder, so nothing from an older build may be left in it.
            if (Directory.Exists(output)) Directory.Delete(output, true);

            string[] scenes = EnabledScenes();
            if (scenes.Length == 0)
            {
                Debug.LogError("Build Web: the build list has no enabled scenes (File > Build Profiles > Scene List).");
                return;
            }

            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = output,
                target = BuildTarget.WebGL,
                targetGroup = BuildTargetGroup.WebGL,
                options = BuildOptions.None
            });

            BuildSummary summary = report.summary;
            if (summary.result == BuildResult.Succeeded)
            {
                double megabytes = FolderBytes(output) / (1024.0 * 1024.0);
                Debug.Log($"Build Web: done in {summary.totalTime.TotalMinutes:0.0} min. The build is in {OutputFolder} " +
                    $"({megabytes:0.0} MB). Upload it with: {PushCommand}");
                return;
            }

            var errors = new StringBuilder();
            foreach (BuildStep step in report.steps)
                foreach (BuildStepMessage message in step.messages)
                    if (message.type == LogType.Error || message.type == LogType.Exception)
                        errors.AppendLine("- " + message.content);
            Debug.LogError($"Build Web: the build {summary.result.ToString().ToLowerInvariant()} with {summary.totalErrors} error(s)." +
                (errors.Length > 0 ? "\n" + errors : " See the console above for details."));
        }

        /// <summary>The D26 settings, set before every build so an accidental change can't ship a Brotli build.</summary>
        public static void ApplyWebSettings()
        {
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.template = Template;
        }

        public static string[] EnabledScenes()
        {
            var scenes = new List<string>();
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
                if (scene.enabled) scenes.Add(scene.path);
            return scenes.ToArray();
        }

        private static long FolderBytes(string folder)
        {
            long total = 0;
            foreach (string file in Directory.GetFiles(folder, "*", SearchOption.AllDirectories))
                total += new FileInfo(file).Length;
            return total;
        }
    }
}
