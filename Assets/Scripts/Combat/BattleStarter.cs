using UnityEngine;

public class BattleStarter : MonoBehaviour
{
    private EnemyTrainer enemyTrainer;
    private DialogueTrigger dialogueTrigger;

    private void Awake()
    {
        enemyTrainer = GetComponent<EnemyTrainer>();
        dialogueTrigger = GetComponent<DialogueTrigger>();
    }

    public void Interact()
    {
        if (dialogueTrigger == null)
        {
            StartBattle();
            return;
        }

        dialogueTrigger.TriggerDialogue();

        DialogueManager.Instance.OnDialogueEnded += HandleDialogueEnded;
    }

    private void HandleDialogueEnded()
    {
        DialogueManager.Instance.OnDialogueEnded -= HandleDialogueEnded;
        StartBattle();
    }

    private void StartBattle()
    {
        if (CombatManager.Instance == null) return;
        if (PlayerParty.Instance == null) return;
        if (enemyTrainer == null) return;

        CombatManager.Instance.StartBattle(
            PlayerParty.Instance,
            enemyTrainer
        );
    }
}