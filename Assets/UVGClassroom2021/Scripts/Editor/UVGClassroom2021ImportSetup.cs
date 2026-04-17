using System.IO;
using UnityEditor;
using UnityEngine;

namespace UVG.Classroom2021.EditorTools
{
    public static class UVGClassroom2021ImportSetup
    {
        private const string SpritesFolder = "Assets/UVGClassroom2021/Sprites";

        [MenuItem("UVG/2021/Prepare Imported Sprites")]
        public static void PrepareSprites()
        {
            if (!Directory.Exists(SpritesFolder))
            {
                Debug.LogError($"No se encontró la carpeta: {SpritesFolder}");
                return;
            }

            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { SpritesFolder });
            int count = 0;

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 64;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.isReadable = false;
                importer.SaveAndReimport();
                count++;
            }

            AssetDatabase.Refresh();
            Debug.Log($"[UVG Classroom 2021] Sprites preparados: {count}");
            EditorUtility.DisplayDialog("UVG Classroom 2021",
                $"Sprites preparados correctamente: {count}", "OK");
        }
    }
}
