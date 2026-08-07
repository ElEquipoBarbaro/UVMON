using UnityEngine;

public class BattleStarter : MonoBehaviour
{
    private EnemyTrainer enemyTrainer;
    private DialogueTrigger dialogueTrigger;

    // Evita que una interaccion repetida (p.ej. el jugador mashea la tecla de interactuar
    // mientras el dialogo previo a la pelea todavia se esta abriendo) suscriba
    // HandleDialogueEnded mas de una vez — eso hacia que, cuando el dialogo terminaba, se
    // llamara StartBattle() dos veces y quedaran dos BattleLoop corriendo a la vez
    // compartiendo el mismo estado de turno (ver COMBAT_SYSTEM_ANALYSIS.md).
    private bool interactionPending;

    private void Awake()
    {
        enemyTrainer = GetComponent<EnemyTrainer>();
        dialogueTrigger = GetComponent<DialogueTrigger>();
    }

    public void Interact()
    {
        if (interactionPending)
            return;

        if (dialogueTrigger == null)
        {
            StartBattle();
            return;
        }

        interactionPending = true;

        dialogueTrigger.TriggerDialogue();

        DialogueManager.Instance.OnDialogueEnded += HandleDialogueEnded;
    }

    private void HandleDialogueEnded()
    {
        DialogueManager.Instance.OnDialogueEnded -= HandleDialogueEnded;
        interactionPending = false;
        StartBattle();
    }

    private void StartBattle()
    {
        if (CombatManager.Instance == null) return;
        if (PlayerParty.Instance == null) return;
        if (enemyTrainer == null) return;

        // Ademas del guard de arriba (dialogo repetido), CombatManager.StartBattle
        // tambien ignora una segunda llamada si ya hay una batalla en curso — doble
        // defensa por si este mismo BattleStarter se dispara por otra via.
        CombatManager.Instance.StartBattle(
            PlayerParty.Instance,
            enemyTrainer
        );
    }
}