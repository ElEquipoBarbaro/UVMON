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
}