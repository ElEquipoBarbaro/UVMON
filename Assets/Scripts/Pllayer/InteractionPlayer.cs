using UnityEngine;

public class InteractionPlayer : MonoBehaviour
{
    [SerializeField] private KeyCode interactionKey = KeyCode.E;

    private DialogueTrigger currentNPC;
    private BattleStarter currentBattleStarter;

    private void Update()
    {
        if (Input.GetKeyDown(interactionKey))
        {
            if (currentNPC == null)
                return;

            if (DialogueManager.Instance.IsDialogueActive)
            {
                DialogueManager.Instance.RequestAdvance();
                return;
            }

            if (currentBattleStarter != null)
            {
                currentBattleStarter.Interact();
            }
            else
            {
                currentNPC.TriggerDialogue();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("NPC"))
            return;

        currentNPC = collision.GetComponentInParent<DialogueTrigger>();
        currentBattleStarter = collision.GetComponentInParent<BattleStarter>();
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("NPC"))
            return;

        if (!DialogueManager.Instance.IsDialogueActive)
        {
            currentNPC = null;
            currentBattleStarter = null;
        }
    }
}