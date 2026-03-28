using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DialogueTurn
{
    [field: SerializeField]
    public DialogueCharacter Character { get; private set; }
    
    [SerializeField, TextArea(2, 4)]
    private string dialogueLine = string.Empty;

    public string DialogueLine => dialogueLine;


}
