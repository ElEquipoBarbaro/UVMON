using System;
using System.Collections;
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
    [SerializeField] private TextMeshProUGUI battleMessageText;

    [Header("Move Selection")]
    [SerializeField] private Transform moveOptionsContainer;
    [SerializeField] private MoveOptionUI moveOptionPrefab;

    private readonly List<MoveOptionUI> moveOptionSlots = new List<MoveOptionUI>();
    private bool moveSelectionLocked;

    /// <summary>El jugador paso el cursor sobre una opcion de ataque (indice en playerRuntime.Moves).</summary>
    public event Action<int> OnMoveHovered;

    /// <summary>El jugador hizo clic sobre una opcion de ataque (indice en playerRuntime.Moves).</summary>
    public event Action<int> OnMoveClicked;

    [Header("Creature Views")]
    [SerializeField] private CreatureBattleView playerCreatureView;
    [SerializeField] private CreatureBattleView enemyCreatureView;

    public CreatureBattleView PlayerView => playerCreatureView;
    public CreatureBattleView EnemyView => enemyCreatureView;

    [Header("Enemy Body Parts (Prompt 18)")]
    [SerializeField] private EnemyBodyPartsView enemyBodyPartsView;
    [SerializeField] private TextMeshProUGUI targetIndicatorText;

    /// <summary>El jugador hizo clic sobre una extremidad del enemigo (indice en la lista de BodyPart activa).</summary>
    public event Action<int> OnBodyPartClicked;

    public bool HasEnemyBodyParts => enemyBodyPartsView != null && enemyBodyPartsView.PartCount > 0;

    private void Awake()
    {
        if (enemyBodyPartsView != null)
            enemyBodyPartsView.OnPartClicked += HandleBodyPartClicked;
    }

    private void OnDestroy()
    {
        if (enemyBodyPartsView != null)
            enemyBodyPartsView.OnPartClicked -= HandleBodyPartClicked;
    }

    private void HandleBodyPartClicked(int index)
    {
        OnBodyPartClicked?.Invoke(index);
    }

    public void SetupEnemyBodyParts(IReadOnlyList<BodyPart> parts)
    {
        if (enemyBodyPartsView != null)
            enemyBodyPartsView.Setup(parts);

        if ((parts == null || parts.Count == 0) && targetIndicatorText != null)
            targetIndicatorText.text = string.Empty;
    }

    public void SelectEnemyBodyPart(BodyPart part, int index)
    {
        if (enemyBodyPartsView != null)
            enemyBodyPartsView.SelectIndex(index);

        if (targetIndicatorText != null && part != null)
        {
            targetIndicatorText.text = part.EsParteCritica
                ? $"Objetivo: {part.NombreParte} (¡critico!)"
                : $"Objetivo: {part.NombreParte}";
        }
    }

    /// <summary>Ningun objetivo confirmado todavia este turno: limpia el brillo y pide al
    /// jugador que elija una parte antes de poder atacar (ver PROMPT.md Prompt 18).</summary>
    public void ClearEnemyBodyPartSelection()
    {
        if (enemyBodyPartsView != null)
            enemyBodyPartsView.SelectIndex(-1);

        if (targetIndicatorText != null)
            targetIndicatorText.text = "Selecciona una parte del enemigo para atacar";
    }

    /// <summary>Bloquea/desbloquea las opciones de movimiento (se exige elegir una extremidad
    /// objetivo primero cuando el enemigo actual tiene bodyParts).</summary>
    public void SetMoveSelectionLocked(bool locked)
    {
        moveSelectionLocked = locked;

        foreach (MoveOptionUI slot in moveOptionSlots)
            slot.SetInteractable(!locked);
    }

    public void MarkEnemyBodyPartDamaged(int index, Sprite damagedSprite)
    {
        if (enemyBodyPartsView != null)
            enemyBodyPartsView.MarkDamaged(index, damagedSprite);
    }

    public void PlayEnemyBodyPartHitFlash(int index)
    {
        if (enemyBodyPartsView != null)
            enemyBodyPartsView.PlayHitFlash(index);
    }

    /// <summary>
    /// Desvanece el/los asset(s) visuales del enemigo actual (sus partes si las tiene, o su
    /// sprite unico) — usado al terminar el combate, antes de iniciar el sistema de captura.
    /// </summary>
    public IEnumerator FadeOutEnemyVisual(float duration)
    {
        if (HasEnemyBodyParts)
            yield return enemyBodyPartsView.FadeOut(duration);
        else if (enemyCreatureView != null)
            yield return enemyCreatureView.FadeOut(duration);
    }

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

        // Por si el jugador dejaba el mouse sobre una opcion de ataque justo cuando
        // termino la batalla (el slot se destruye/desactiva sin disparar OnPointerExit).
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

        SetupEnemyBodyParts(null);
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
        if (moveOptionsContainer == null || moveOptionPrefab == null)
            return;

        int moveCount = moves != null ? moves.Count : 0;

        if (moveOptionSlots.Count != moveCount)
            RebuildMoveOptions(moves);

        for (int i = 0; i < moveOptionSlots.Count; i++)
            moveOptionSlots[i].SetHighlighted(i == selectedMoveIndex);
    }

    private void RebuildMoveOptions(IReadOnlyList<MoveData> moves)
    {
        foreach (MoveOptionUI slot in moveOptionSlots)
        {
            slot.OnHoverEnter -= HandleMoveOptionHoverEnter;
            slot.OnClicked -= HandleMoveOptionClicked;
            Destroy(slot.gameObject);
        }

        moveOptionSlots.Clear();

        if (moves == null)
            return;

        for (int i = 0; i < moves.Count; i++)
        {
            MoveOptionUI slot = Instantiate(moveOptionPrefab, moveOptionsContainer);
            slot.gameObject.SetActive(true);
            slot.SetIndex(i);
            slot.SetText(moves[i].moveName);
            slot.SetHighlighted(false);
            slot.SetInteractable(!moveSelectionLocked);
            slot.OnHoverEnter += HandleMoveOptionHoverEnter;
            slot.OnClicked += HandleMoveOptionClicked;
            moveOptionSlots.Add(slot);
        }
    }

    private void HandleMoveOptionHoverEnter(MoveOptionUI slot)
    {
        OnMoveHovered?.Invoke(slot.Index);
    }

    private void HandleMoveOptionClicked(MoveOptionUI slot)
    {
        OnMoveClicked?.Invoke(slot.Index);
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