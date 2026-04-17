using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    [SerializeField] private DialogueRound dialogue;
    [SerializeField] private bool startsBattle;

    public bool StartsBattle => startsBattle;

    [ContextMenu("Trigger Dialogue")]
    public void TriggerDialogue()
    {
        DialogueManager.Instance.StartDialogue(dialogue);
    }
}