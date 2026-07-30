using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }

    private CreatureRuntime playerRuntime;
    private CreatureRuntime enemyRuntime;
    private EnemyTrainer currentEnemyTrainer;

    [Header("Battle Systems")]
    [SerializeField] private BattleUIManager battleUI;
    [SerializeField] private BattleAnimationPlayer battleAnimationPlayer;

    [Header("QTE")]
    [SerializeField] private QTEController qteController;
    [SerializeField] private QTEData qteData;

    [Header("Capture")]
    [SerializeField] private CaptureController captureController;
    [SerializeField] private CaptureData captureData;
    [SerializeField] private InventorySO inventoryData;

    private bool playerHasChosen = false;
    private bool isPlayerTurn = false;

    private MoveData selectedMove;
    private int selectedMoveIndex = 0;

    public BattleUIManager BattleUI => battleUI;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (!isPlayerTurn || playerRuntime == null || battleUI == null)
            return;

        if (playerRuntime.Moves == null || playerRuntime.Moves.Count == 0)
            return;

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            selectedMoveIndex--;
            ClampMoveIndex();
            battleUI.RenderMoveSelection(playerRuntime.Moves, selectedMoveIndex);
        }

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            selectedMoveIndex++;
            ClampMoveIndex();
            battleUI.RenderMoveSelection(playerRuntime.Moves, selectedMoveIndex);
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (playerRuntime.Moves.Count > 0)
                SelectMove(playerRuntime.Moves[selectedMoveIndex]);
        }
    }

    private void ClampMoveIndex()
    {
        if (playerRuntime == null || playerRuntime.Moves == null)
            return;

        selectedMoveIndex = Mathf.Clamp(
            selectedMoveIndex,
            0,
            Mathf.Max(0, playerRuntime.Moves.Count - 1)
        );
    }

    public void StartBattle(PlayerParty playerParty, EnemyTrainer enemyTrainer)
    {
        if (playerParty == null || enemyTrainer == null)
            return;

        playerRuntime = playerParty.GetLeadCreature();
        enemyRuntime = enemyTrainer.GetLeadCreature();
        currentEnemyTrainer = enemyTrainer;

        if (playerRuntime == null || enemyRuntime == null)
        {
            Debug.LogError("Battle could not start because one side has no usable creature.");
            return;
        }

        selectedMoveIndex = 0;
        playerHasChosen = false;
        isPlayerTurn = false;
        selectedMove = null;

        if (battleUI != null)
        {
            battleUI.ShowBattleUI();
            battleUI.BindCreatures(playerRuntime, enemyRuntime);
            battleUI.RenderMoveSelection(playerRuntime.Moves, selectedMoveIndex);
            battleUI.ShowBattleMessage($"A wild {enemyRuntime.data.creatureName} appeared!");
        }

        StartCoroutine(BattleLoop());
    }

    public void RefreshBattleUI()
    {
        if (battleUI != null)
            battleUI.UpdateHP(playerRuntime, enemyRuntime);
    }

    public void SelectMove(MoveData move)
    {
        selectedMove = move;
        playerHasChosen = true;
    }

    private IEnumerator PlayerTurn()
    {
        isPlayerTurn = true;
        playerHasChosen = false;

        if (battleUI != null)
            battleUI.RenderMoveSelection(playerRuntime.Moves, selectedMoveIndex);

        yield return new WaitUntil(() => playerHasChosen);

        isPlayerTurn = false;

        if (selectedMove == null || selectedMove.effect == null)
        {
            if (battleUI != null)
                battleUI.ShowBattleMessage("Nothing happened.");

            yield return new WaitForSeconds(0.8f);
            yield break;
        }

        bool attackSucceeds = true;

        if (qteController != null)
        {
            bool qteResult = true;

            if (selectedMove.qteParallel != null && selectedMove.qteParallel.Length > 0)
            {
                yield return qteController.RunQTEParallel(selectedMove.qteParallel, result => qteResult = result);
            }
            else
            {
                IReadOnlyList<QTEData> qteChain = (selectedMove.qteSequence != null && selectedMove.qteSequence.Length > 0)
                    ? selectedMove.qteSequence
                    : (qteData != null ? new QTEData[] { qteData } : null);

                if (qteChain != null)
                    yield return qteController.RunQTEChain(qteChain, result => qteResult = result);
            }

            attackSucceeds = qteResult;
        }

        if (!attackSucceeds)
        {
            if (battleUI != null)
                battleUI.ShowBattleMessage($"{playerRuntime.data.creatureName}'s attack missed!");

            yield return new WaitForSeconds(0.8f);
            yield break;
        }

        if (battleAnimationPlayer != null && battleUI != null)
        {
            yield return battleAnimationPlayer.PlayMoveAnimation(
                selectedMove.animationData,
                battleUI.PlayerView,
                battleUI.EnemyView
            );
        }

        yield return selectedMove.effect.Execute(playerRuntime, enemyRuntime, selectedMove);

        if (battleUI != null)
            battleUI.UpdateHP(playerRuntime, enemyRuntime);

        yield return new WaitForSeconds(0.35f);
    }

    private IEnumerator EnemyTurn()
    {
        if (enemyRuntime.Moves == null || enemyRuntime.Moves.Count == 0)
            yield break;

        MoveData move = enemyRuntime.Moves[0];

        if (battleAnimationPlayer != null && battleUI != null)
        {
            yield return battleAnimationPlayer.PlayMoveAnimation(
                move.animationData,
                battleUI.EnemyView,
                battleUI.PlayerView
            );
        }

        yield return move.effect.Execute(enemyRuntime, playerRuntime, move);

        if (battleUI != null)
            battleUI.UpdateHP(playerRuntime, enemyRuntime);

        yield return new WaitForSeconds(0.35f);
    }

    private IEnumerator BattleLoop()
    {
        while (playerRuntime.CurrentHP > 0 && enemyRuntime.CurrentHP > 0)
        {
            yield return PlayerTurn();
            if (enemyRuntime.CurrentHP <= 0 || playerRuntime.CurrentHP <= 0)
                break;

            yield return EnemyTurn();
        }

        yield return EndBattleSequence();
    }

    private IEnumerator EndBattleSequence()
    {
        if (battleUI != null)
        {
            if (enemyRuntime.CurrentHP <= 0 && playerRuntime.CurrentHP > 0)
            {
                battleUI.ShowBattleMessage($"{enemyRuntime.data.creatureName} fainted!");
                yield return new WaitForSeconds(1f);

                playerRuntime.GainXP(enemyRuntime.data.xpYield);

                if (enemyRuntime.data.isCapturable)
                    yield return RunCaptureSequence();

                battleUI.ShowBattleMessage("Battle Ended!");
                yield return new WaitForSeconds(0.8f);

                if (currentEnemyTrainer != null)
                {
                    Destroy(currentEnemyTrainer.gameObject);
                    currentEnemyTrainer = null;
                }
            }
            else if (playerRuntime.CurrentHP <= 0)
            {
                battleUI.ShowBattleMessage($"{playerRuntime.data.creatureName} fainted!");
                yield return new WaitForSeconds(1f);

                battleUI.ShowBattleMessage("You lost the battle!");
                yield return new WaitForSeconds(1f);
            }
            else
            {
                battleUI.ShowBattleMessage("Battle Ended!");
                yield return new WaitForSeconds(0.8f);
            }

            battleUI.HideBattleUI();
        }
    }

    private IEnumerator RunCaptureSequence()
    {
        if (captureController == null)
            yield break;

        CaptureResult result = default;
        yield return captureController.RunCapture(enemyRuntime.data, inventoryData, captureData, r => result = r);

        if (battleUI != null)
        {
            if (result.success)
                battleUI.ShowBattleMessage($"{enemyRuntime.data.creatureName} was captured!");
            else if (result.failureReason == CaptureFailReason.NoJar)
                battleUI.ShowBattleMessage("You don't have any capture jars!");
            else
                battleUI.ShowBattleMessage($"{enemyRuntime.data.creatureName} broke free!");

            yield return new WaitForSeconds(1f);
        }

        if (result.success && PlayerParty.Instance != null)
            PlayerParty.Instance.AddCreature(enemyRuntime.data, enemyRuntime.Level);
    }
}