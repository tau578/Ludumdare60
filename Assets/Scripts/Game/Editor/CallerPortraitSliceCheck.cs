using UnityEditor;
using UnityEngine;

namespace Switchboard.Game.Editor
{
    // Re-exported portraits can shrink by a pixel while the .meta keeps the old slice;
    // Unity then drops the sprite and GameConfig loses the portrait. Warn only, never fix.
    public sealed class CallerPortraitSliceCheck : AssetPostprocessor
    {
        private const string CallersFolder = "Assets/Sprites/callers/";

        private void OnPostprocessTexture(Texture2D texture)
        {
            if (!assetPath.StartsWith(CallersFolder)) return;
            if (!(assetImporter is TextureImporter importer)) return;
            if (importer.spriteImportMode != SpriteImportMode.Multiple) return;

            SerializedProperty sprites = new SerializedObject(importer).FindProperty("m_SpriteSheet.m_Sprites");
            if (sprites == null || sprites.arraySize != 1) return;

            SerializedProperty sprite = sprites.GetArrayElementAtIndex(0);
            Rect rect = sprite.FindPropertyRelative("m_Rect").rectValue;
            if (rect.xMax <= texture.width && rect.yMax <= texture.height) return;

            Debug.LogWarning(
                $"Caller portrait '{assetPath}': the slice '{sprite.FindPropertyRelative("m_Name").stringValue}' " +
                $"is {rect.width}x{rect.height} at ({rect.x}, {rect.y}), but the image is {texture.width}x{texture.height}. " +
                "Unity drops the sprite, so its portrait goes missing. Re-slice it in the Sprite Editor or ask the orchestrator.");
        }
    }
}
