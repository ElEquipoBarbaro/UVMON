using UnityEngine;

public class BattleStarter : MonoBehaviour
{
    [SerializeField] private DialogueTrigger dialogueTrigger;
    [SerializeField] private CreatureData playerCreature;
    [SerializeField] private CreatureData enemyCreature;

    private void Start()
    {
        DialogueManager.Instance.OnDialogueEnded += StartBattle;
    }

    private void StartBattle()
    {
        if (!dialogueTrigger.StartsBattle) return;

        CombatManager.Instance.StartBattle(playerCreature, enemyCreature);
    }
}