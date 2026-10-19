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

        [MenuItem("Tools/Switchboard/Build Web")]
        public static void Build()
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
