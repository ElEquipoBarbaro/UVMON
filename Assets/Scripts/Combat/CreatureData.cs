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
}