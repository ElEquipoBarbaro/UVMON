using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "New Dialogue", menuName = "ScriptableObjects/DialogueRound")]

public class DialogueRound : ScriptableObject
{
    [SerializeField] private List<DialogueTurn> dialogueTurnsList;

    public List<DialogueTurn> DialogueTurnsList => dialogueTurnsList;

}
