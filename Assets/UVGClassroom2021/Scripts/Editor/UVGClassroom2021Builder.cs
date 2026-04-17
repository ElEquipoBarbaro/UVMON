using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UVG.Classroom2021.EditorTools
{
    public static class UVGClassroom2021Builder
    {
        private const string RootName = "UVG_Classroom_2021";
        private const string SpritesFolder = "Assets/UVGClassroom2021/Sprites/";
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        [MenuItem("UVG/2021/Build Classroom Map")]
        public static void Build()
        {
            Clear();

            var root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Build UVG Classroom 2021");

            BuildCamera(root.transform);
            BuildBaseFloor(root.transform);
            BuildPanels(root.transform);
            BuildLowerDeskArea(root.transform);
            BuildDoor(root.transform);

            Selection.activeGameObject = root;
            MarkDirty();
            Debug.Log("[UVG Classroom 2021] Aula generada correctamente.");
        }

        [MenuItem("UVG/2021/Clear Classroom Map")]
        public static void Clear()
        {
            var old = GameObject.Find(RootName);
            if (old != null)
                Object.DestroyImmediate(old);
        }

        private static void BuildCamera(Transform parent)
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var camGO = new GameObject("Main Camera");
                camGO.tag = "MainCamera";
                camGO.transform.SetParent(parent);
                cam = camGO.AddComponent<Camera>();
                camGO.AddComponent<AudioListener>();
            }

            cam.orthographic = true;
            cam.orthographicSize = 10f;
            cam.backgroundColor = new Color(0.84f, 0.84f, 0.82f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.transform.position = new Vector3(16f, 10f, -10f);
        }

        private static void BuildBaseFloor(Transform parent)
        {
            var floorRoot = new GameObject("Floor");
            floorRoot.transform.SetParent(parent);

            for (int x = 0; x < 32; x++)
            {
                for (int y = 0; y < 20; y++)
                {
                    string sprite = y >= 10 ? "floor_light" : "floor_light";
                    CreateSprite(sprite, new Vector2(x + 0.5f, y + 0.5f), floorRoot.transform, 0, new Vector2(1f, 1f));
                }
            }
        }

        private static void BuildPanels(Transform parent)
        {
            var panels = new GameObject("Panels");
            panels.transform.SetParent(parent);

            CreateSprite("panel_desks_topleft",      new Vector2(4.796875f, 14.7421875f), panels.transform, 2);
            CreateSprite("panel_front_topmiddle",   new Vector2(15.4375f,   14.7421875f), panels.transform, 3);
            CreateSprite("panel_right_topright",    new Vector2(26.65625f,  14.7421875f), panels.transform, 3);
            CreateSprite("panel_decor_bottomright", new Vector2(26.65625f,   5.40625f),   panels.transform, 2);
        }

        private static void BuildLowerDeskArea(Transform parent)
        {
            var desks = new GameObject("LowerDeskArea");
            desks.transform.SetParent(parent);

            float[] xs = { 3.5f, 8.5f, 13.5f, 18.5f };
            float[] ys = { 7.0f, 4.3f, 1.6f };

            for (int row = 0; row < ys.Length; row++)
            {
                for (int col = 0; col < xs.Length; col++)
                {
                    string sprite = col % 2 == 0 ? "student_desk_double_large" : "student_desk_double_small";
                    var go = CreateSprite(sprite, new Vector2(xs[col], ys[row]), desks.transform, 4);
                    AddBoxCollider(go, 2.8f, 1.4f);
                }
            }

            var bin = CreateSprite("trash_can_small", new Vector2(20.2f, 2.0f), desks.transform, 4, new Vector2(0.9f, 0.9f));
            AddBoxCollider(bin, 0.8f, 0.8f);
        }

        private static void BuildDoor(Transform parent)
        {
            var doorRoot = new GameObject("Door");
            doorRoot.transform.SetParent(parent);
            var go = CreateSprite("door", new Vector2(16f, 1.1f), doorRoot.transform, 5, new Vector2(1.2f, 1.2f));
            AddBoxCollider(go, 1.0f, 1.4f);
        }

        private static GameObject CreateSprite(string spriteName, Vector2 position, Transform parent, int order, Vector2? scale = null)
        {
            var sprite = LoadSprite(spriteName);
            if (sprite == null)
            {
                Debug.LogWarning($"No se encontró sprite: {spriteName}");
                return null;
            }

            var go = new GameObject(spriteName);
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(position.x, position.y, 0f);
            if (scale.HasValue)
                go.transform.localScale = new Vector3(scale.Value.x, scale.Value.y, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return go;
        }

        private static void AddBoxCollider(GameObject go, float width, float height)
        {
            if (go == null) return;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(width, height);
        }

        private static Sprite LoadSprite(string spriteName)
        {
            if (Cache.TryGetValue(spriteName, out var sprite) && sprite != null)
                return sprite;

            sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesFolder + spriteName + ".png");
            Cache[spriteName] = sprite;
            return sprite;
        }

        private static void MarkDirty()
        {
#if UNITY_EDITOR
            var scene = SceneManager.GetActiveScene();
            if (scene.IsValid())
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
#endif
        }
    }
}
