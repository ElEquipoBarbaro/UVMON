using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Asset con la información del mapa de la UVG: la imagen de fondo y la lista de puntos.
/// Se edita desde el Inspector (Create > UVMON > Mapa de la UVG).
/// </summary>
[CreateAssetMenu(fileName = "CampusMapData", menuName = "UVMON/Mapa de la UVG")]
public class CampusMapData : ScriptableObject
{
    [Header("Mapa")]
    [Tooltip("Imagen del mapa de la UVG que se muestra de fondo.")]
    public Sprite mapImage;

    [Header("Puntos")]
    [Tooltip("Puntos colocados sobre el mapa. Cada uno abre su propia imagen y descripción.")]
    public List<MapPoint> points = new List<MapPoint>();

    /// <summary>Devuelve el punto con ese título, o null si no existe.</summary>
    public MapPoint FindPoint(string title)
    {
        for (int i = 0; i < points.Count; i++)
            if (points[i] != null && points[i].title == title)
                return points[i];

        return null;
    }
}
