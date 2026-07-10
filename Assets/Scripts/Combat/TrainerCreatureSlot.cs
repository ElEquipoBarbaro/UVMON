using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TrainerCreatureSlot
{
    public CreatureData creatureData;
    public int level = 1;

    public bool overrideMoves = false;
    public List<MoveData> customMoves = new List<MoveData>();
}