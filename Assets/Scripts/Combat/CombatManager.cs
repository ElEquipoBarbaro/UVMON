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

    [Header("Body Parts")]
    [Tooltip("Multiplicador aplicado al dano de una extremidad antes de restarlo a la vida global del enemigo (spec sec 31). 1.0 = sin cambio.")]
    [SerializeField] private float multiplicadorVidaGlobal = 1f;

    private bool playerHasChosen = false;
    private bool isPlayerTurn = false;

    private MoveData selectedMove;
    private int selectedMoveIndex = 0;

    private List<BodyPart> enemyBodyPartsRuntime;
    private int selectedBodyPartIndex = 0;
    private bool bodyPartConfirmedThisTurn;

    private bool HasEnemyBodyParts => enemyBodyPartsRuntime != null && enemyBodyPartsRuntime.Count > 0;

    public BattleUIManager BattleUI => battleUI;

    /// <summary>No nulo mientras BattleLoop() esta corriendo. Usado para impedir que una
    /// segunda llamada a StartBattle (p.ej. un doble trigger de interaccion) arranque un
    /// segundo BattleLoop superpuesto compartiendo los mismos campos de turno/seleccion —
    /// eso rompia la alternancia de turnos y dejaba el sistema "sin sentido" (ver hallazgo
    /// en COMBAT_SYSTEM_ANALYSIS.md).</summary>
    private Coroutine battleLoopCoroutine;
    public bool IsBattleActive => battleLoopCoroutine != null;

    private void Awake()
    {
        // Igual que PlayerParty.Awake: si por algun motivo ya hay un CombatManager vivo
        // (duplicado en la escena), este se autodestruye en vez de pisar la referencia
        // singleton existente.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (battleUI != null)
        {
            battleUI.OnMoveHovered += HandleMoveHovered;
            battleUI.OnMoveClicked += HandleMoveClicked;
            battleUI.OnBodyPartClicked += HandleBodyPartClicked;
        }
    }

    private void OnDestroy()
    {
        if (battleUI != null)
        {
            battleUI.OnMoveHovered -= HandleMoveHovered;
            battleUI.OnMoveClicked -= HandleMoveClicked;
            battleUI.OnBodyPartClicked -= HandleBodyPartClicked;
        }

        // Evita que Instance quede apuntando a un objeto destruido si este era el
        // singleton activo (mismo patron que el guard de duplicados en Awake).
        if (Instance == this)
            Instance = null;
    }

    private void HandleBodyPartClicked(int index)
    {
        // Solo se puede (re)elegir objetivo antes de confirmar un movimiento (PROMPT.md
        // Prompt 18: primero se elige la parte, recien despues se habilita el ataque).
        if (!isPlayerTurn || playerHasChosen)
            return;

        if (enemyBodyPartsRuntime == null || index < 0 || index >= enemyBodyPartsRuntime.Count)
            return;

        SelectBodyPartTarget(index);

        bodyPartConfirmedThisTurn = true;

        if (battleUI != null)
            battleUI.SetMoveSelectionLocked(false);
    }

    private void SelectBodyPartTarget(int index)
    {
        selectedBodyPartIndex = index;

        if (battleUI != null)
            battleUI.SelectEnemyBodyPart(enemyBodyPartsRuntime[index], index);
    }

    private void HandleMoveHovered(int index)
    {
        if (!IsValidMoveIndex(index))
            return;

        selectedMoveIndex = index;
        battleUI.RenderMoveSelection(playerRuntime.Moves, selectedMoveIndex);
    }

    private void HandleMoveClicked(int index)
    {
        if (!IsValidMoveIndex(index))
            return;

        SelectMove(playerRuntime.Moves[index]);
    }

    private bool IsValidMoveIndex(int index)
    {
        if (!isPlayerTurn || playerRuntime == null || playerRuntime.Moves == null)
            return false;

        // El enemigo tiene extremidades atacables: hay que confirmar un objetivo
        // (clic sobre una parte) antes de poder elegir el movimiento (Prompt 18).
        if (HasEnemyBodyParts && !bodyPartConfirmedThisTurn)
            return false;

        return index >= 0 && index < playerRuntime.Moves.Count;
    }

    public void StartBattle(PlayerParty playerParty, EnemyTrainer enemyTrainer)
    {
        if (playerParty == null || enemyTrainer == null)
            return;

        // Ya hay un BattleLoop corriendo (p.ej. un doble trigger de interaccion antes de
        // que la primera batalla arrancara del todo): ignorar la llamada en vez de
        // arrancar un segundo BattleLoop superpuesto que comparta isPlayerTurn/
        // playerHasChosen/selectedBodyPartIndex con el primero (eso es lo que rompia la
        // alternancia de turnos — ver COMBAT_SYSTEM_ANALYSIS.md).
        if (battleLoopCoroutine != null)
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

        enemyBodyPartsRuntime = BuildBodyPartsRuntime(enemyRuntime.data);
        selectedBodyPartIndex = 0;
        bodyPartConfirmedThisTurn = false;

        if (battleUI != null)
        {
            battleUI.ShowBattleUI();
            battleUI.BindCreatures(playerRuntime, enemyRuntime);
            battleUI.RenderMoveSelection(playerRuntime.Moves, selectedMoveIndex);
            battleUI.ShowBattleMessage($"A wild {enemyRuntime.data.creatureName} appeared!");

            battleUI.SetupEnemyBodyParts(enemyBodyPartsRuntime);

            if (HasEnemyBodyParts)
            {
                battleUI.ClearEnemyBodyPartSelection();
                battleUI.SetMoveSelectionLocked(true);
            }
        }

        battleLoopCoroutine = StartCoroutine(BattleLoop());
    }

    private static List<BodyPart> BuildBodyPartsRuntime(CreatureData data)
    {
        if (data == null || data.bodyParts == null || data.bodyParts.Count == 0)
            return null;

        List<BodyPart> parts = new List<BodyPart>(data.bodyParts.Count);

        foreach (BodyPartDefinition definition in data.bodyParts)
        {
            if (definition != null)
                parts.Add(new BodyPart(definition));
        }

        return parts.Count > 0 ? parts : null;
    }

    public void RefreshBattleUI()
    {
        if (battleUI != null)
            battleUI.UpdateHP(playerRuntime, enemyRuntime);
    }

    /// <summary>
    /// Dispara el flash de golpe (CreatureBattleView.PlayHitFlash) sobre la vista que
    /// corresponda segun quien recibio el dano — llamado desde DamageEffect justo
    /// despues de confirmar CreatureRuntime.TakeDamage, para que nunca se dispare en un
    /// ataque fallido. Solo afecta al sprite de la criatura golpeada; la otra vista no se
    /// toca. El ataque dirigido a extremidades (ExecuteBodyPartAttack) ya tiene su propio
    /// flash por parte (PlayEnemyBodyPartHitFlash) y no pasa por aca.
    /// </summary>
    public void PlayHitFlashFor(CreatureRuntime creature)
    {
        if (battleUI == null || creature == null)
            return;

        if (creature == playerRuntime)
            battleUI.PlayerView?.PlayHitFlash();
        else if (creature == enemyRuntime)
            battleUI.EnemyView?.PlayHitFlash();
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
        bodyPartConfirmedThisTurn = false;

        if (battleUI != null)
        {
            battleUI.RenderMoveSelection(playerRuntime.Moves, selectedMoveIndex);

            if (HasEnemyBodyParts)
            {
                battleUI.ClearEnemyBodyPartSelection();
                battleUI.SetMoveSelectionLocked(true);
            }
            else
            {
                battleUI.SetMoveSelectionLocked(false);
            }
        }

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
            if (battleAnimationPlayer != null && battleUI != null)
                battleAnimationPlayer.PlayMissIndicator(battleUI.EnemyView);

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

        if (enemyBodyPartsRuntime != null && enemyBodyPartsRuntime.Count > 0)
            yield return ExecuteBodyPartAttack(selectedMove);
        else
            yield return selectedMove.effect.Execute(playerRuntime, enemyRuntime, selectedMove);

        if (battleUI != null)
            battleUI.UpdateHP(playerRuntime, enemyRuntime);

        yield return new WaitForSeconds(0.35f);
    }

    /// <summary>
    /// Ataque dirigido a una extremidad del enemigo (Docs/CombatSystem/COMBAT_SYSTEM_SPEC.md
    /// sec 24-31). El QTE ya se resolvio con exito antes de llegar aqui (ver PlayerTurn),
    /// asi que el orden restante es: acertividad -> critico -> variacion -> dano -> aplicar
    /// a la extremidad -> aplicar a la vida global -> UI -> sprite danado si corresponde.
    /// </summary>
    private IEnumerator ExecuteBodyPartAttack(MoveData move)
    {
        if (enemyBodyPartsRuntime == null ||
            selectedBodyPartIndex < 0 ||
            selectedBodyPartIndex >= enemyBodyPartsRuntime.Count)
            yield break;

        BodyPart target = enemyBodyPartsRuntime[selectedBodyPartIndex];

        if (battleUI != null)
        {
            battleUI.ShowBattleMessage($"{playerRuntime.data.creatureName} used {move.moveName}!");
            yield return new WaitForSeconds(0.6f);
        }

        int danoBase = Mathf.Max(1, playerRuntime.Attack + move.power);

        DamageCalculator.Result result = DamageCalculator.Calculate(
            true, // qteExitoso: ya se comprobo antes de llamar a este metodo
            danoBase,
            target.PorcentajeAtaque,
            target.PorcentajeAcertividad,
            enemyRuntime.CurrentHP,
            UnityRandomProvider.Instance
        );

        if (!result.ataqueImpacta)
        {
            if (battleAnimationPlayer != null && battleUI != null)
                battleAnimationPlayer.PlayMissIndicator(battleUI.EnemyView);

            if (battleUI != null)
            {
                battleUI.ShowBattleMessage($"Attack missed {target.NombreParte}!");
                yield return new WaitForSeconds(0.8f);
            }

            yield break;
        }

        if (result.esCritico && battleUI != null)
        {
            battleUI.ShowBattleMessage("Critical hit!");
            yield return new WaitForSeconds(0.8f);
        }

        bool justCrossedToZero = target.ApplyDamage(result.danoFinalEntero);

        int danoVidaGlobal = Mathf.RoundToInt(result.danoFinalEntero * Mathf.Max(0f, multiplicadorVidaGlobal));
        enemyRuntime.TakeDamage(danoVidaGlobal);

        RefreshBattleUI();

        if (battleUI != null)
            battleUI.PlayEnemyBodyPartHitFlash(selectedBodyPartIndex);

        if (battleUI != null)
        {
            battleUI.ShowBattleMessage($"{target.NombreParte} took {result.danoFinalEntero} damage! ({target.VidaActual}/{target.VidaMaxima} HP)");
            yield return new WaitForSeconds(0.8f);
        }

        if (justCrossedToZero)
        {
            if (battleUI != null)
                battleUI.MarkEnemyBodyPartDamaged(selectedBodyPartIndex, target.ReferenciaVisualDanada);

            // La parte que se acaba de destruir ya no es un objetivo valido (no puede
            // volver a seleccionarse) — si era la seleccionada, se pasa el objetivo a
            // otra parte viva automaticamente.
            int nextAliveIndex = FindNextAliveBodyPartIndex(selectedBodyPartIndex);
            if (nextAliveIndex >= 0)
                SelectBodyPartTarget(nextAliveIndex);
        }

        // El mensaje/derrota del enemigo (vidaGlobal <= 0) lo emite EndBattleSequence
        // una unica vez, cuando BattleLoop detecta que termino la ronda — no se repite aqui.
    }

    private int FindNextAliveBodyPartIndex(int excludeIndex)
    {
        if (enemyBodyPartsRuntime == null)
            return -1;

        for (int i = 0; i < enemyBodyPartsRuntime.Count; i++)
        {
            if (i != excludeIndex && enemyBodyPartsRuntime[i].IsAlive)
                return i;
        }

        return -1;
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

        battleLoopCoroutine = null;
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
                {
                    yield return battleUI.FadeOutEnemyVisual(0.8f);
                    yield return RunCaptureSequence();
                }

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