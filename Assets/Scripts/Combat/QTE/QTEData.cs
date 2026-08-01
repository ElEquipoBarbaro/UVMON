using UnityEngine;

[CreateAssetMenu(menuName = "Combat/QTE/QTE Data", fileName = "NewQTEData")]
public class QTEData : ScriptableObject
{
    [Header("Circulos")]
    [Tooltip("Radio del circulo objetivo (el circulo fijo del centro).")]
    public float innerRadius = 90f;

    [Tooltip("Radio inicial del circulo que se va reduciendo.")]
    public float outerRadius = 220f;

    [Header("Dificultad")]
    [Tooltip("Velocidad (en pixeles por segundo) a la que se reduce el radio del circulo exterior.")]
    public float shrinkSpeed = 60f;

    [Header("Posicion en pantalla")]
    [Tooltip("Posicion (x, y) del QTE respecto al centro del panel de batalla. Se ignora si 'randomizePosition' esta activo.")]
    public Vector2 position = Vector2.zero;

    [Tooltip("Si esta activo, la posicion se sortea al azar dentro de la pantalla de batalla en vez de usar 'position'.")]
    public bool randomizePosition = false;
}
