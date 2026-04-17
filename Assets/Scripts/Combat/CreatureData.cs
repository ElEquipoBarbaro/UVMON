using UnityEngine;
using System.Collections.Generic;
[CreateAssetMenu(menuName = "Combat/Creature")]
public class CreatureData : ScriptableObject
{
    public string creatureName;
    public int maxHP;
    public int attack;
    public int defense;
    public int speed;

    public List<MoveData> moves;
}