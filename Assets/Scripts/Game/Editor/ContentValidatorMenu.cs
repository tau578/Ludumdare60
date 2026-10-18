using System.Collections.Generic;
using Switchboard.Content;
using UnityEditor;
using UnityEngine;

namespace Switchboard.Game.Editor
{
    public static class ContentValidatorMenu
    {
        [MenuItem("Tools/Switchboard/Validate Content")]
        public static void Validate()
        {
            string[] guids = AssetDatabase.FindAssets("t:" + nameof(GameConfig));
            if (guids.Length == 0)
            {
                Debug.LogError("Validate Content: no GameConfig asset was found.");
                return;
            }
            int problems = 0;
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var config = AssetDatabase.LoadAssetAtPath<GameConfig>(path);
                List<string> errors = config.Validate();
                foreach (string error in errors) Debug.LogError($"Validate Content ({path}): {error}", config);
                problems += errors.Count;
            }
            if (problems == 0) Debug.Log("Validate Content: no problems found.");
        }
    }
}
