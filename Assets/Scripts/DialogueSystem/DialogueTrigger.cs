using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    [SerializeField] private DialogueRound dialogue;

    [ContextMenu("Trigger Dialogue")]
    public void TriggerDialogue()
    {
        DialogueManager.Instance.StartDialogue(dialogue);
    }

    // ❌ Elimina o comenta esto:
    // private void OnTriggerEnter2D(Collider2D collision)
    // {
    //     if (collision.CompareTag("Player"))
    //     {
    //         TriggerDialogue();
    //     }
    // }
}