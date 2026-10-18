using System;
using System.IO;
using System.Linq;
using Switchboard.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Switchboard.Editor
{
    public static class BoardArtworkBuilder
    {
        [MenuItem("Tools/Switchboard/Apply Board And Socket Artwork")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || PrefabStageUtility.GetCurrentPrefabStage() != null)
                throw new InvalidOperationException("Open the scene in Edit Mode first.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save unsaved scene changes first.");
            Sprite board = SpriteAt(File.Exists("Assets/Sprites/board/board_matte.png") ? "board_matte.png" : "board.png"), node = SpriteAt("node.png");
            const string socketPath = "Assets/Prefabs/UI/Socket.prefab";
            const string boardPath = "Assets/Prefabs/UI/SwitchboardPanel.prefab";
            Directory.CreateDirectory("Temp/BoardArtwork");
            foreach (string path in new[] { socketPath, boardPath })
            {
                string backup = "Temp/BoardArtwork/" + Path.GetFileName(path) + ".before";
                if (!File.Exists(backup)) File.Copy(path, backup);
            }
            var socket = PrefabUtility.LoadPrefabContents(socketPath);
            try
            {
                socket.GetComponent<SocketGraphic>().FaceSprite = node;
                PrefabUtility.SaveAsPrefabAsset(socket, socketPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(socket); }
            var panel = PrefabUtility.LoadPrefabContents(boardPath);
            try
            {
                var image = panel.GetComponent<Image>();
                if (image == null) image = panel.AddComponent<Image>();
                image.sprite = board; image.color = Color.white; image.type = Image.Type.Simple;
                image.raycastTarget = false;
                // These objects contain only the former frame, plate and screw decorations.
                string[] replaced = { "Shadow", "Frame", "FrameHighlight", "Faceplate", "HeaderStrip", "RowStrip", "SocketField", "MountingScrew" };
                foreach (Transform child in panel.transform)
                    if (replaced.Contains(child.name)) child.gameObject.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(panel, boardPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(panel); }
            Debug.Log("Board and socket artwork applied to their prefabs; layout and connection rules preserved.");
        }
        private static Sprite SpriteAt(string name) =>
            AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/board/" + name).OfType<Sprite>().FirstOrDefault()
            ?? throw new InvalidOperationException("Missing sprite: " + name);
    }
}
