using UnityEditor;
using UnityEngine;

/// <summary>
/// Botones de apoyo para acomodar los puntos del mapa sin entrar a Play:
/// se dibujan los puntos, se arrastran sobre el mapa y se guardan sus posiciones XY.
/// </summary>
[CustomEditor(typeof(UVGMapUI))]
public class UVGMapUIEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        UVGMapUI map = (UVGMapUI)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Edición de puntos", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "1) Dibujar puntos.\n" +
            "2) Arrastrarlos sobre el mapa con la herramienta Rect.\n" +
            "3) Guardar posiciones XY en el asset.",
            MessageType.Info);

        if (GUILayout.Button("Dibujar puntos en el editor"))
        {
            map.RebuildPoints();
            MarkSceneDirty(map);
        }

        if (GUILayout.Button("Guardar posiciones XY en el asset"))
        {
            if (map.MapData == null)
            {
                EditorUtility.DisplayDialog("Mapa UVG", "No hay un CampusMapData asignado.", "OK");
            }
            else
            {
                Undo.RecordObject(map.MapData, "Guardar posiciones de puntos");
                int saved = map.SavePointPositionsToData();
                EditorUtility.SetDirty(map.MapData);
                AssetDatabase.SaveAssets();
                Debug.Log("Mapa UVG: se guardaron " + saved + " posiciones en " + map.MapData.name);
            }
        }

        if (GUILayout.Button("Limpiar puntos"))
        {
            map.ClearPoints();
            MarkSceneDirty(map);
        }
    }

    private static void MarkSceneDirty(UVGMapUI map)
    {
        if (Application.isPlaying) return;

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(map.gameObject.scene);
    }
}
