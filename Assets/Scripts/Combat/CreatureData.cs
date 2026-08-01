using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Combat/Creature")]
public class CreatureData : ScriptableObject
{
    [Header("Type")]
    public CreatureType type;

    public string creatureName;

    [Header("Visuals")]
    public Sprite frontSprite;
    public Sprite backSprite;

    [Header("Stats")]
    public int maxHP;
    public int attack;
    public int defense;
    public int speed;

    [Header("Moves")]
    public List<MoveData> moves;

    public int xpYield = 20;

    [Header("Captura")]
    [Tooltip("Si esta activo, este UVGmon puede intentarse capturar al derrotarlo en batalla.")]
    public bool isCapturable = false;

    [Tooltip("Dificultad manual del desafio de captura (0-100). No es probabilidad: alto = circulo inicial mas grande y reduccion mas lenta (mas facil); bajo = circulo mas chico y reduccion mas rapida (mas dificil).")]
    [Range(0f, 100f)]
    public float porcentajeCaptura = 50f;

    [Header("Extremidades (opcional)")]
    [Tooltip("Si tiene elementos, esta criatura puede recibir ataques dirigidos a una extremidad especifica (seleccionable con el mouse en batalla) en vez de solo al cuerpo entero. Vacio = comportamiento normal (DamageEffect de siempre).")]
    public System.Collections.Generic.List<BodyPartDefinition> bodyParts = new System.Collections.Generic.List<BodyPartDefinition>();
}