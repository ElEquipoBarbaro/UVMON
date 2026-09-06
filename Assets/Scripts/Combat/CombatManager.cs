using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }

    private CreatureRuntime playerRuntime;
    private CreatureRuntime enemyRuntime;
    private PlayerParty currentPlayerParty;
    private EnemyTrainer currentEnemyTrainer;

    [Header("Battle Systems")]
    [SerializeField] private BattleUIManager battleUI;
    [SerializeField] private BattleAnimationPlayer battleAnimationPlayer;

    [Header("Creature Switch")]
    [SerializeField, Min(0f)] private float switchOutDuration = 0.3f;
    [SerializeField, Min(0f)] private float switchInDuration = 0.35f;

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

    /// <summary>Se pone en true cuando una accion que NO es un ataque (cambio de UVGmon,
    /// Prompt 6; usar un objeto de inventario, Prompt 7) resuelve el turno del jugador --
    /// PlayerTurn() lo usa para saltarse por completo el camino de ataque (QTE/dano) y
    /// terminar el turno directamente, sin imprimir "Nothing happened."</summary>
    private bool nonAttackActionResolved;
    private int pendingSwitchPartyIndex = -1;

    private enum CreatureSwitchReason
    {
        Voluntary,
        Fainted
    }

    private bool HasEnemyBodyParts => enemyBodyPartsRuntime != null && enemyBodyPartsRuntime.Count > 0;

    /// <summary>
    /// Prompt 7: guardia unica ("playerActionInProgress") que bloquea el inicio de
    /// cualquier accion del jugador (ataque, objeto, cambio de UVGmon) fuera de la
    /// ventana valida. Es true tanto cuando NO es el turno del jugador (QTE en curso,
    /// turno enemigo, batalla resolviendo) como cuando ya eligio una accion este turno
    /// pero todavia no termino de resolverse -- exactamente la misma condicion que ya
    /// usaban por separado HandleBodyPartClicked/IsValidMoveIndex/HandleTeamMemberClicked,
    /// ahora centralizada en un solo lugar en vez de repetir "!isPlayerTurn || playerHasChosen"
    /// en cada handler (y con eso, sin crear una maquina de turnos paralela: sigue leyendo
    /// los mismos dos campos que ya gobiernan PlayerTurn()).
    /// </summary>
    private bool IsPlayerActionLocked => !isPlayerTurn || playerHasChosen;

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
            battleUI.OnTeamMemberClicked += HandleTeamMemberClicked;
            battleUI.OnInventoryItemClicked += HandleInventoryItemClicked;
            battleUI.OnInventoryItemDropped += HandleInventoryItemClicked;
        }
    }

    private void OnDestroy()
    {
        if (battleUI != null)
        {
            battleUI.OnMoveHovered -= HandleMoveHovered;
            battleUI.OnMoveClicked -= HandleMoveClicked;
            battleUI.OnBodyPartClicked -= HandleBodyPartClicked;
            battleUI.OnTeamMemberClicked -= HandleTeamMemberClicked;
            battleUI.OnInventoryItemClicked -= HandleInventoryItemClicked;
            battleUI.OnInventoryItemDropped -= HandleInventoryItemClicked;
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
        if (IsPlayerActionLocked)
            return;

        if (enemyBodyPartsRuntime == null || index < 0 || index >= enemyBodyPartsRuntime.Count)
            return;

        SelectBodyPartTarget(index);

        bodyPartConfirmedThisTurn = true;

        if (battleUI != null)
            battleUI.SetMoveSelectionLocked(false);
    }

    /// <summary>
    /// Rotacion de UVGmon activo (Prompt 6). Solo valido durante el turno del jugador,
    /// antes de haber elegido ya una accion (mismo guard que HandleBodyPartClicked/
    /// IsValidMoveIndex -- bloquea automaticamente durante QTE y durante el turno
    /// enemigo, porque isPlayerTurn ya es false en ambos casos). Un cambio exitoso
    /// consume el turno completo: no ataca, no usa inventario, no permite una segunda
    /// accion -- ver PlayerTurn().
    /// </summary>
    private void HandleTeamMemberClicked(int partyIndex)
    {
        if (IsPlayerActionLocked)
            return;

        if (currentPlayerParty == null)
        {
            RejectTeamSwitch("No se encontro el equipo del jugador.");
            return;
        }

        IReadOnlyList<CreatureRuntime> party = currentPlayerParty.Party;

        if (partyIndex < 0 || partyIndex >= party.Count)
        {
            RejectTeamSwitch("La seleccion ya no es valida.");
            return;
        }

        if (partyIndex == 0)
        {
            RejectTeamSwitch("Ese UVGmon ya esta en combate.");
            return;
        }

        CreatureRuntime target = party[partyIndex];

        if (target == null || target.CurrentHP <= 0)
        {
            RejectTeamSwitch("Ese UVGmon esta derrotado y no puede combatir.");
            return;
        }

        pendingSwitchPartyIndex = partyIndex;
        playerHasChosen = true;

        if (battleUI != null)
            battleUI.SetPlayerInputEnabled(false);
    }

    private void RejectTeamSwitch(string message)
    {
        if (battleUI == null)
            return;

        battleUI.ShowBattleMessage(message);
        battleUI.RefreshTeamTab();
        battleUI.SetPlayerInputEnabled(isPlayerTurn && !playerHasChosen);
    }

    /// <summary>
    /// Usar un objeto de inventario durante el turno del jugador (Prompt 7). Misma
    /// guardia que ataque/cambio de UVGmon (IsPlayerActionLocked). Solo objetos
    /// Healing con un ItemEffect asignado son usables aqui -- exactamente la misma regla
    /// que ya aplica InventoryController.HandleItemActionRequest fuera de combate (no se
    /// modifica esa logica, solo se replica la condicion en este nuevo punto de entrada).
    /// El objetivo es siempre el UVGmon activo: a diferencia del menu fuera de combate,
    /// en batalla no hay una segunda pestana de seleccion de objetivo -- se asume que
    /// "usar un objeto" en pleno combate cura a quien esta peleando.
    /// </summary>
    private void HandleInventoryItemClicked(int inventoryIndex)
    {
        if (IsPlayerActionLocked)
            return;

        if (inventoryData == null || playerRuntime == null)
            return;

        if (!inventoryData.TryGetItemAt(inventoryIndex, out InventoryItem slot))
        {
            RejectInventoryItem("Ese objeto ya no está disponible.");
            return;
        }

        string itemName = slot.item.Name;
        string creatureName = playerRuntime.data.creatureName;

        if (slot.item.Category != ItemCategory.Healing || slot.item.Effect == null)
        {
            RejectInventoryItem($"{itemName} no puede utilizarse durante el combate.");
            return;
        }

        if (playerRuntime.CurrentHP <= 0)
        {
            RejectInventoryItem($"{creatureName} está debilitado; {itemName} no puede reanimarlo.");
            return;
        }

        if (playerRuntime.CurrentHP >= playerRuntime.MaxHP)
        {
            RejectInventoryItem($"{creatureName} ya tiene todos sus PS.");
            return;
        }

        int hpBefore = playerRuntime.CurrentHP;

        if (battleUI != null)
            battleUI.SetPlayerInputEnabled(false);

        try
        {
            slot.item.Effect.Apply(playerRuntime);
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            RejectInventoryItem($"No se pudo usar {itemName}. Inténtalo de nuevo.");
            return;
        }

        int healedAmount = playerRuntime.CurrentHP - hpBefore;
        if (healedAmount <= 0)
        {
            RejectInventoryItem($"{itemName} no produjo ningún efecto.");
            return;
        }

        inventoryData.RemoveItem(inventoryIndex, 1);

        if (battleUI != null)
        {
            battleUI.UpdateHP(playerRuntime, enemyRuntime);
            battleUI.ShowBattleMessage($"¡{creatureName} usó {itemName}! Recuperó {healedAmount} PS.");
            battleUI.ShowAttacksTab();
        }

        nonAttackActionResolved = true;
        playerHasChosen = true;
    }

    private void RejectInventoryItem(string message)
    {
        if (battleUI == null)
            return;

        battleUI.ShowBattleMessage(message);
        battleUI.SetPlayerInputEnabled(isPlayerTurn && !playerHasChosen);
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
        // IsPlayerActionLocked (en vez de solo "!isPlayerTurn") tambien bloquea un
        // segundo clic sobre otra opcion de movimiento en el mismo frame en que ya se
        // eligio uno (playerHasChosen=true pero isPlayerTurn todavia no bajo a false --
        // PlayerTurn() recien lo hace al reanudar la corutina) -- sin esto, un doble clic
        // muy rapido podia pisar selectedMove con otro movimiento (Prompt 7, prueba 7).
        if (IsPlayerActionLocked || playerRuntime == null || playerRuntime.Moves == null)
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

        currentPlayerParty = playerParty;

        int firstUsableIndex = currentPlayerParty.FindFirstUsableCreatureIndex();
        if (firstUsableIndex > 0)
            currentPlayerParty.SetLeadCreature(firstUsableIndex);

        playerRuntime = currentPlayerParty.GetLeadCreature();
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
        pendingSwitchPartyIndex = -1;
        nonAttackActionResolved = false;

        enemyBodyPartsRuntime = BuildBodyPartsRuntime(enemyRuntime.data);
        selectedBodyPartIndex = 0;
        bodyPartConfirmedThisTurn = false;

        if (battleUI != null)
        {
            battleUI.ShowBattleUI();
            battleUI.BindCreatures(playerRuntime, enemyRuntime);
            battleUI.RenderMoveSelection(playerRuntime.Moves, selectedMoveIndex);
            battleUI.ShowBattleMessage($"¡Un {enemyRuntime.data.creatureName} salvaje apareció!");

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

        if (battleUI != null)
            battleUI.SetPlayerInputEnabled(false);
    }

    private IEnumerator PlayerTurn()
    {
        while (true)
        {
            isPlayerTurn = true;
            playerHasChosen = false;
            bodyPartConfirmedThisTurn = false;
            nonAttackActionResolved = false;
            pendingSwitchPartyIndex = -1;
            selectedMove = null;

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

                battleUI.SetPlayerInputEnabled(true);
            }

            yield return new WaitUntil(() => playerHasChosen);

            isPlayerTurn = false;

            if (battleUI != null)
                battleUI.SetPlayerInputEnabled(false);

            if (pendingSwitchPartyIndex >= 0)
            {
                int targetIndex = pendingSwitchPartyIndex;
                pendingSwitchPartyIndex = -1;

                bool switchCompleted = false;
                yield return SwitchPlayerCreature(
                    targetIndex,
                    CreatureSwitchReason.Voluntary,
                    playTransition: true,
                    onComplete: result => switchCompleted = result
                );

                if (!switchCompleted)
                {
                    if (battleUI != null)
                        battleUI.ShowBattleMessage("No se pudo realizar el cambio.");

                    continue;
                }

                yield return new WaitForSeconds(0.35f);
                yield break;
            }

            // El objeto ya fue aplicado por el handler. Termina el turno sin pasar por
            // QTE/ataque, igual que antes, pero con la entrada bloqueada visualmente.
            if (nonAttackActionResolved)
            {
                nonAttackActionResolved = false;
                yield return new WaitForSeconds(0.6f);
                yield break;
            }

            if (selectedMove == null || selectedMove.effect == null)
            {
                if (battleUI != null)
                    battleUI.ShowBattleMessage("La acción no produjo ningún efecto.");

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
                    battleUI.ShowBattleMessage($"¡{playerRuntime.data.creatureName} falló! El ataque no alcanzó al rival.");

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
            yield break;
        }
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
            battleUI.ShowBattleMessage($"¡{playerRuntime.data.creatureName} usó {move.moveName} contra {target.NombreParte}!");
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
                battleUI.ShowBattleMessage($"¡El ataque no logró impactar en {target.NombreParte}!");
                yield return new WaitForSeconds(0.8f);
            }

            yield break;
        }

        if (result.esCritico && battleUI != null)
        {
            battleUI.ShowBattleMessage("¡Golpe crítico! El impacto fue devastador.");
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
            battleUI.ShowBattleMessage($"¡{target.NombreParte} recibió {result.danoFinalEntero} de daño! Resistencia: {target.VidaActual}/{target.VidaMaxima}.");
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

    private IEnumerator SwitchPlayerCreature(
        int partyIndex,
        CreatureSwitchReason reason,
        bool playTransition,
        System.Action<bool> onComplete)
    {
        if (currentPlayerParty == null ||
            partyIndex <= 0 ||
            partyIndex >= currentPlayerParty.Party.Count ||
            !currentPlayerParty.IsUsableCreatureIndex(partyIndex))
        {
            onComplete?.Invoke(false);
            yield break;
        }

        CreatureRuntime outgoing = playerRuntime;
        CreatureRuntime incoming = currentPlayerParty.Party[partyIndex];

        if (incoming == null || ReferenceEquals(incoming, outgoing))
        {
            onComplete?.Invoke(false);
            yield break;
        }

        if (battleUI != null)
        {
            battleUI.SetPlayerInputEnabled(false);

            if (reason == CreatureSwitchReason.Voluntary && outgoing != null)
                battleUI.ShowBattleMessage($"{outgoing.data.creatureName}, regresa!");
        }

        if (playTransition && battleUI != null && battleUI.PlayerView != null)
            yield return battleUI.PlayerView.PlaySwitchOut(switchOutDuration);

        // Revalidar justo antes de mutar el orden. Si algo externo cambio el equipo o
        // la vida durante la animacion, el turno no se consume y la vista anterior se
        // restaura en lugar de dejar el HUD bloqueado.
        if (partyIndex >= currentPlayerParty.Party.Count ||
            !ReferenceEquals(currentPlayerParty.Party[partyIndex], incoming) ||
            !currentPlayerParty.IsUsableCreatureIndex(partyIndex) ||
            !currentPlayerParty.SetLeadCreature(partyIndex))
        {
            if (battleUI != null && outgoing != null)
            {
                battleUI.BindCreatures(outgoing, enemyRuntime);

                if (playTransition && battleUI.PlayerView != null)
                    yield return battleUI.PlayerView.PlaySwitchIn(switchInDuration);
            }

            onComplete?.Invoke(false);
            yield break;
        }

        playerRuntime = currentPlayerParty.GetLeadCreature();
        selectedMove = null;
        selectedMoveIndex = 0;

        if (battleUI != null)
        {
            battleUI.BindCreatures(playerRuntime, enemyRuntime);
            battleUI.RenderMoveSelection(playerRuntime.Moves, selectedMoveIndex);
            battleUI.RefreshTeamTab();
            battleUI.ShowBattleMessage($"Adelante, {playerRuntime.data.creatureName}!");

            if (playTransition && battleUI.PlayerView != null)
                yield return battleUI.PlayerView.PlaySwitchIn(switchInDuration);

            battleUI.ShowAttacksTab();
        }

        onComplete?.Invoke(true);
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
        while (enemyRuntime != null &&
               enemyRuntime.CurrentHP > 0 &&
               currentPlayerParty != null &&
               currentPlayerParty.HasUsableCreature())
        {
            if (playerRuntime == null || playerRuntime.CurrentHP <= 0)
            {
                bool replacedBeforeTurn = false;
                yield return ReplaceFaintedPlayer(result => replacedBeforeTurn = result);

                if (!replacedBeforeTurn)
                    break;
            }

            yield return PlayerTurn();

            if (enemyRuntime.CurrentHP <= 0)
                break;

            // Cubre efectos futuros de retroceso/estado. La sustitucion forzada no
            // concede un ataque enemigo extra: tras cambiar se inicia una ronda nueva.
            if (playerRuntime == null || playerRuntime.CurrentHP <= 0)
            {
                bool replacedAfterPlayerAction = false;
                yield return ReplaceFaintedPlayer(result => replacedAfterPlayerAction = result);

                if (!replacedAfterPlayerAction)
                    break;

                continue;
            }

            yield return EnemyTurn();

            if (playerRuntime == null || playerRuntime.CurrentHP <= 0)
            {
                bool replacedAfterEnemyAction = false;
                yield return ReplaceFaintedPlayer(result => replacedAfterEnemyAction = result);

                if (!replacedAfterEnemyAction)
                    break;
            }
        }

        yield return EndBattleSequence();

        if (battleUI != null)
            battleUI.SetPlayerInputEnabled(false);

        battleLoopCoroutine = null;
    }

    private IEnumerator ReplaceFaintedPlayer(System.Action<bool> onComplete)
    {
        if (currentPlayerParty == null)
        {
            onComplete?.Invoke(false);
            yield break;
        }

        int replacementIndex = currentPlayerParty.FindFirstUsableCreatureIndex(1);
        if (replacementIndex <= 0)
        {
            onComplete?.Invoke(false);
            yield break;
        }

        bool switched = false;
        yield return SwitchPlayerCreature(
            replacementIndex,
            CreatureSwitchReason.Fainted,
            playTransition: true,
            onComplete: result => switched = result
        );

        onComplete?.Invoke(switched);
    }

    private IEnumerator EndBattleSequence()
    {
        if (battleUI != null)
        {
            if (enemyRuntime.CurrentHP <= 0 && playerRuntime.CurrentHP > 0)
            {
                battleUI.ShowBattleMessage($"¡{enemyRuntime.data.creatureName} quedó debilitado!");
                yield return new WaitForSeconds(1f);

                playerRuntime.GainXP(enemyRuntime.data.xpYield);

                if (enemyRuntime.data.isCapturable)
                {
                    yield return battleUI.FadeOutEnemyVisual(0.8f);
                    yield return RunCaptureSequence();
                }

                battleUI.ShowBattleMessage("¡El combate ha terminado!");
                yield return new WaitForSeconds(0.8f);

                if (currentEnemyTrainer != null)
                {
                    Destroy(currentEnemyTrainer.gameObject);
                    currentEnemyTrainer = null;
                }
            }
            else if (currentPlayerParty == null || !currentPlayerParty.HasUsableCreature())
            {
                if (playerRuntime != null)
                {
                    battleUI.ShowBattleMessage($"¡{playerRuntime.data.creatureName} quedó debilitado!");
                    yield return new WaitForSeconds(1f);
                }

                battleUI.ShowBattleMessage("Tu equipo ya no puede continuar. Has perdido el combate.");
                yield return new WaitForSeconds(1f);
            }
            else
            {
                battleUI.ShowBattleMessage("¡El combate ha terminado!");
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
                battleUI.ShowBattleMessage($"¡{enemyRuntime.data.creatureName} fue capturado!");
            else if (result.failureReason == CaptureFailReason.NoJar)
                battleUI.ShowBattleMessage("No tienes frascos de captura disponibles.");
            else
                battleUI.ShowBattleMessage($"¡{enemyRuntime.data.creatureName} escapó del frasco!");

            yield return new WaitForSeconds(1f);
        }

        if (result.success && currentPlayerParty != null)
            currentPlayerParty.AddCreature(enemyRuntime.data, enemyRuntime.Level);
    }
}
