using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Trainer Data", menuName = "Combat/Trainer Data")]
public class TrainerData : ScriptableObject
{
    public string trainerName = "Trainer";
    public List<TrainerCreatureSlot> team = new List<TrainerCreatureSlot>();
}