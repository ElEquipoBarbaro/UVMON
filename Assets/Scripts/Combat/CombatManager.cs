using UnityEngine;
using System.Collections;
using TMPro;

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance;

    private CreatureRuntime playerRuntime;
    private CreatureRuntime enemyRuntime;

    [SerializeField] private GameObject battleUI;
    [SerializeField] private GameObject overworldUI;
    [SerializeField] private GameObject playerObject;

    [SerializeField] private TextMeshProUGUI playerHPText;
    [SerializeField] private TextMeshProUGUI enemyHPText;
    [SerializeField] private TextMeshProUGUI moveSelectionText;

    private bool playerHasChosen = false;
    private bool isPlayerTurn = false;

    private MoveData selectedMove;
    private int selectedMoveIndex = 0;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (!isPlayerTurn || playerRuntime == null) return;

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            selectedMoveIndex--;
            ClampMoveIndex();
            UpdateMoveSelectionUI();
        }

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            selectedMoveIndex++;
            ClampMoveIndex();
            UpdateMoveSelectionUI();
        }

        // Confirm selection
        if (Input.GetKeyDown(KeyCode.Return))
        {
            SelectMove(playerRuntime.data.moves[selectedMoveIndex]);
        }
    }

    private void ClampMoveIndex()
    {
        selectedMoveIndex = Mathf.Clamp(
            selectedMoveIndex,
            0,
            playerRuntime.data.moves.Count - 1
        );
    }

    private void UpdateMoveSelectionUI()
    {
        if (moveSelectionText == null) return;

        string text = "";

        for (int i = 0; i < playerRuntime.data.moves.Count; i++)
        {
            if (i == selectedMoveIndex)
                text += "> "; // highlight
            else
                text += "  ";

            text += playerRuntime.data.moves[i].moveName + "\n";
        }

        moveSelectionText.text = text;
    }

    private void UpdateUI()
    {
        if (playerRuntime == null || enemyRuntime == null) return;

        playerHPText.text = $"HP: {playerRuntime.CurrentHP}/{playerRuntime.data.maxHP}";
        enemyHPText.text = $"HP: {enemyRuntime.CurrentHP}/{enemyRuntime.data.maxHP}";
    }

    public void StartBattle(CreatureData playerData, CreatureData enemyData)
    {
        Debug.Log("Battle Started");

        playerObject.SetActive(false);
        overworldUI.SetActive(false);
        battleUI.SetActive(true);

        playerRuntime = new CreatureRuntime(playerData);
        enemyRuntime = new CreatureRuntime(enemyData);

        selectedMoveIndex = 0;

        UpdateUI();
        UpdateMoveSelectionUI();

        StartCoroutine(BattleLoop());
    }

    public void SelectMove(MoveData move)
    {
        selectedMove = move;
        playerHasChosen = true;
    }

    private IEnumerator PlayerTurn()
    {
        Debug.Log("Player Turn - waiting for input");

        isPlayerTurn = true;
        playerHasChosen = false;

        yield return new WaitUntil(() => playerHasChosen);

        isPlayerTurn = false;

        Debug.Log("Player used: " + selectedMove.moveName);

        selectedMove.effect.Execute(playerRuntime, enemyRuntime, selectedMove);
        UpdateUI();

        yield return new WaitForSeconds(1f);
    }

    private IEnumerator EnemyTurn()
    {
        Debug.Log("Enemy Turn");

        MoveData move = enemyRuntime.data.moves[0];
        Debug.Log("Enemy used: " + move.moveName);

        move.effect.Execute(enemyRuntime, playerRuntime, move);
        UpdateUI();

        yield return new WaitForSeconds(1f);
    }

    private IEnumerator BattleLoop()
    {
        while (playerRuntime.CurrentHP > 0 && enemyRuntime.CurrentHP > 0)
        {
            yield return PlayerTurn();
            if (enemyRuntime.CurrentHP <= 0) break;

            yield return EnemyTurn();
        }

        EndBattle();
    }

    private void EndBattle()
    {
        Debug.Log("Battle Ended");

        battleUI.SetActive(false);
        overworldUI.SetActive(true);
        playerObject.SetActive(true);
    }
}