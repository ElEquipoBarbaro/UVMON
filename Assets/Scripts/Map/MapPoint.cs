using UnityEngine;

/// <summary>
/// Un punto del mapa de la UVG. Cada punto guarda su posición XY sobre el mapa
/// (normalizada, de 0 a 1) más la imagen y la descripción que se muestran al abrirlo.
/// </summary>
[System.Serializable]
public class MapPoint
{
    [Tooltip("Nombre del lugar. Se usa como título del detalle y para buscarlo por código.")]
    public string title = "Nuevo punto";

    [TextArea(3, 6)]
    [Tooltip("Descripción que aparece junto a la imagen.")]
    public string description = "";

    [Tooltip("Imagen que se abre al hacer clic en el punto.")]
    public Sprite image;

    [Tooltip("Posición XY sobre el mapa. (0,0) = esquina inferior izquierda, (1,1) = superior derecha.")]
    public Vector2 normalizedPosition = new Vector2(0.5f, 0.5f);

    [Tooltip("Icono del marcador. Si se deja vacío se usa el del prefab MapPoint.")]
    public Sprite markerIcon;

    [Tooltip("Color del marcador sobre el mapa.")]
    public Color markerColor = Color.white;

    [Tooltip("Si está apagado, el punto no se dibuja en el mapa.")]
    public bool visible = true;
}
