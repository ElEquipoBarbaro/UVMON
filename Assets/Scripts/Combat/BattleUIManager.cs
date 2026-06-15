using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BattleUIManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject battleUI;
    [SerializeField] private GameObject overworldUI;
    [SerializeField] private GameObject playerObject;

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI playerHPText;
    [SerializeField] private TextMeshProUGUI enemyHPText;
    [SerializeField] private TextMeshProUGUI moveSelectionText;
    [SerializeField] private TextMeshProUGUI battleMessageText;

    [Header("Creature Views")]
    [SerializeField] private CreatureBattleView playerCreatureView;
    [SerializeField] private CreatureBattleView enemyCreatureView;

    public CreatureBattleView PlayerView => playerCreatureView;
    public CreatureBattleView EnemyView => enemyCreatureView;

    public void ShowBattleUI()
    {
        if (battleUI != null) battleUI.SetActive(true);
        if (overworldUI != null) overworldUI.SetActive(false);
        if (playerObject != null) playerObject.SetActive(false);
    }

    public void HideBattleUI()
    {
        if (battleUI != null) battleUI.SetActive(false);
        if (overworldUI != null) overworldUI.SetActive(true);
        if (playerObject != null) playerObject.SetActive(true);
    }

    public void BindCreatures(CreatureRuntime playerRuntime, CreatureRuntime enemyRuntime)
    {
        if (playerCreatureView != null && playerRuntime != null)
            playerCreatureView.SetSprite(playerRuntime.data.backSprite);

        if (enemyCreatureView != null && enemyRuntime != null)
            enemyCreatureView.SetSprite(enemyRuntime.data.frontSprite);

        if (playerCreatureView != null)
            playerCreatureView.CacheRestingPosition();

        if (enemyCreatureView != null)
            enemyCreatureView.CacheRestingPosition();

        UpdateHP(playerRuntime, enemyRuntime);
    }

    public void UpdateHP(CreatureRuntime playerRuntime, CreatureRuntime enemyRuntime)
    {
        if (playerRuntime != null && playerHPText != null)
            playerHPText.text = $"HP: {playerRuntime.CurrentHP}/{playerRuntime.MaxHP}";

        if (enemyRuntime != null && enemyHPText != null)
            enemyHPText.text = $"HP: {enemyRuntime.CurrentHP}/{enemyRuntime.MaxHP}";
    }

    public void RenderMoveSelection(IReadOnlyList<MoveData> moves, int selectedMoveIndex)
    {
        if (moveSelectionText == null)
            return;

        if (moves == null || moves.Count == 0)
        {
            moveSelectionText.text = string.Empty;
            return;
        }

        string text = "";

        for (int i = 0; i < moves.Count; i++)
        {
            text += i == selectedMoveIndex ? "> " : "  ";
            text += moves[i].moveName + "\n";
        }

        moveSelectionText.text = text;
    }

    public void ShowBattleMessage(string message)
    {
        if (battleMessageText != null)
            battleMessageText.text = message;
    }

    public void ClearBattleMessage()
    {
        if (battleMessageText != null)
            battleMessageText.text = string.Empty;
    }
}