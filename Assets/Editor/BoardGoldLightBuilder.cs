using System;
using System.IO;
using Switchboard.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Switchboard.Editor
{
    public static class BoardGoldLightBuilder
    {
        [MenuItem("Tools/Switchboard/Apply Board Gold Light")]
        public static void Apply()
        {
            const string texturePath = "Assets/Sprites/board/board_matte.png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
            if (importer == null) throw new InvalidOperationException("Missing matte board texture.");
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
            BoardArtworkBuilder.Apply();
            const string materialPath = "Assets/Materials/BoardGoldLight.mat";
            Directory.CreateDirectory("Assets/Materials");
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                var shader = Shader.Find("Switchboard/UI/Board Gold Light");
                if (shader == null) throw new InvalidOperationException("Missing board gold shader.");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, materialPath);
            }
            const string path = "Assets/Prefabs/UI/SwitchboardPanel.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                root.GetComponent<Image>().material = material;
                if (root.GetComponent<BoardGoldLight>() == null) root.AddComponent<BoardGoldLight>();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
