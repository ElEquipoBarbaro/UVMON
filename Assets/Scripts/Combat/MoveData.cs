using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Move")]
public class MoveData : ScriptableObject
{
    [Header("Basic Info")]
    public string moveName;

    [Header("Combat")]
    public int power;
    public int accuracy = 100;
    public CreatureType moveType;

    [Header("Logic")]
    public MoveEffect effect;

    [Header("Presentation")]
    public MoveAnimationData animationData;

    [Header("QTE")]
    [Tooltip("Varios QTE a la vez en pantalla: hay que acertar TODOS (en cualquier orden). Si tiene elementos, tiene prioridad sobre 'qteSequence'.")]
    public QTEData[] qteParallel;

    [Tooltip("Secuencia de QTE que hay que acertar uno tras otro para que el ataque se ejecute. Si esta vacio (y 'qteParallel' tambien), se usa el QTE por defecto del CombatManager.")]
    public QTEData[] qteSequence;
}