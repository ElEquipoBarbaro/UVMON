using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractionPlayer : MonoBehaviour
{
    [SerializeField] private KeyCode interactionKey = KeyCode.E;

    private DialogueTrigger currentNPC;

    void Update()
    {
        if (Input.GetKeyDown(interactionKey) && currentNPC != null)
        {
            if (DialogueManager.Instance.IsDialogueActive)
            {
                DialogueManager.Instance.RequestAdvance();
            }
            else
            {
                currentNPC.TriggerDialogue();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("NPC"))
        {
            currentNPC = collision.GetComponent<DialogueTrigger>();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
{
    if (collision.CompareTag("NPC"))
    {
        if (!DialogueManager.Instance.IsDialogueActive)
        {
            currentNPC = null;
        }
    }
}
}