using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleUIManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject battleUI;
    [SerializeField] private GameObject overworldUI;
    [SerializeField] private GameObject playerObject;

    [Header("Tabs: Ataques / Inventario / Equipo (Prompt 3-5)")]
    [SerializeField] private Button attacksTabButton;
    [SerializeField] private Button inventoryTabButton;
    [SerializeField] private Button teamTabButton;
    [SerializeField] private GameObject attacksPanel;
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private CombatInventoryUI combatInventoryUI;
    [SerializeField] private GameObject teamPanel;
    [SerializeField] private CombatTeamUI combatTeamUI;
    [SerializeField] private Color tabActiveColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color tabInactiveColor = new Color(0.6f, 0.6f, 0.6f, 1f);

    public enum CombatTab { Attacks, Inventory, Team }
    public CombatTab CurrentTab { get; private set; } = CombatTab.Attacks;

    /// <summary>Se dispara cada vez que la pestana visible cambia (p.ej. para refrescar
    /// Inventario/Equipo con datos actuales al abrirse).</summary>
    public event Action<CombatTab> OnTabChanged;

    /// <summary>El jugador hizo clic sobre un integrante del equipo en la pestana Equipo
    /// (Prompt 6). Indice real en PlayerParty.Instance.Party.</summary>
    public event Action<int> OnTeamMemberClicked;

    /// <summary>El jugador hizo clic sobre un objeto en la pestana Inventario (Prompt 7).
    /// Indice real en InventorySO.</summary>
    public event Action<int> OnInventoryItemClicked;

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI playerHPText;
    [SerializeField] private TextMeshProUGUI enemyHPText;
    [SerializeField] private TextMeshProUGUI battleMessageText;

    [Tooltip("Nombre + nivel del UVGmon activo (Prompt 8: panel 'UVGmon activo' del mockup objetivo). Se actualiza junto con el sprite/HP en BindCreatures.")]
    [SerializeField] private TextMeshProUGUI playerNameLevelText;

    [Header("Move Selection")]
    [SerializeField] private Transform moveOptionsContainer;
    [SerializeField] private MoveOptionUI moveOptionPrefab;

    private readonly List<MoveOptionUI> moveOptionSlots = new List<MoveOptionUI>();
    private bool moveSelectionLocked;
    private IReadOnlyList<MoveData> lastRenderedMoves;

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

        if (combatTeamUI != null)
            combatTeamUI.OnCreatureSelected += HandleTeamMemberClicked;

        if (combatInventoryUI != null)
            combatInventoryUI.OnItemSelected += HandleInventoryItemClicked;

        if (attacksTabButton != null)
            attacksTabButton.onClick.AddListener(ShowAttacksTab);

        if (inventoryTabButton != null)
            inventoryTabButton.onClick.AddListener(ShowInventoryTab);

        if (teamTabButton != null)
            teamTabButton.onClick.AddListener(ShowTeamTab);
    }

    private void OnDestroy()
    {
        if (enemyBodyPartsView != null)
            enemyBodyPartsView.OnPartClicked -= HandleBodyPartClicked;

        if (combatTeamUI != null)
            combatTeamUI.OnCreatureSelected -= HandleTeamMemberClicked;

        if (combatInventoryUI != null)
            combatInventoryUI.OnItemSelected -= HandleInventoryItemClicked;

        if (attacksTabButton != null)
            attacksTabButton.onClick.RemoveListener(ShowAttacksTab);

        if (inventoryTabButton != null)
            inventoryTabButton.onClick.RemoveListener(ShowInventoryTab);

        if (teamTabButton != null)
            teamTabButton.onClick.RemoveListener(ShowTeamTab);
    }

    public void ShowAttacksTab() => SetTab(CombatTab.Attacks);
    public void ShowInventoryTab() => SetTab(CombatTab.Inventory);
    public void ShowTeamTab() => SetTab(CombatTab.Team);

    private void SetTab(CombatTab tab)
    {
        CurrentTab = tab;

        if (attacksPanel != null) attacksPanel.SetActive(tab == CombatTab.Attacks);
        if (inventoryPanel != null) inventoryPanel.SetActive(tab == CombatTab.Inventory);
        if (teamPanel != null) teamPanel.SetActive(tab == CombatTab.Team);

        SetTabButtonColor(attacksTabButton, tab == CombatTab.Attacks);
        SetTabButtonColor(inventoryTabButton, tab == CombatTab.Inventory);
        SetTabButtonColor(teamTabButton, tab == CombatTab.Team);

        OnTabChanged?.Invoke(tab);
    }

    private void SetTabButtonColor(Button button, bool active)
    {
        if (button == null)
            return;

        Image img = button.targetGraphic as Image;
        if (img != null)
            img.color = active ? tabActiveColor : tabInactiveColor;
    }

    private void HandleBodyPartClicked(int index)
    {
        OnBodyPartClicked?.Invoke(index);
    }

    private void HandleTeamMemberClicked(int partyIndex)
    {
        OnTeamMemberClicked?.Invoke(partyIndex);
    }

    private void HandleInventoryItemClicked(int inventoryIndex)
    {
        OnInventoryItemClicked?.Invoke(inventoryIndex);
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

        SetTab(CombatTab.Attacks);
        RefreshTeamTab();
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

        if (playerNameLevelText != null && playerRuntime != null)
            playerNameLevelText.text = $"{playerRuntime.data.creatureName}  Lv.{playerRuntime.Level}";

        UpdateHP(playerRuntime, enemyRuntime);
    }

    public void UpdateHP(CreatureRuntime playerRuntime, CreatureRuntime enemyRuntime)
    {
        if (playerRuntime != null && playerHPText != null)
            playerHPText.text = $"HP: {playerRuntime.CurrentHP}/{playerRuntime.MaxHP}";

        if (enemyRuntime != null && enemyHPText != null)
            enemyHPText.text = $"HP: {enemyRuntime.CurrentHP}/{enemyRuntime.MaxHP}";

        // La pestana Equipo lee PlayerParty en vivo (sin copia propia); cualquier cambio de
        // vida del jugador debe reflejarse ahi tambien, este visible o no en este momento
        // (si esta oculta, OnEnable() la refresca igual al volver a mostrarla).
        RefreshTeamTab();
    }

    /// <summary>Fuerza una relectura del equipo real en la pestana Equipo (Prompt 5/6):
    /// cambio de vida, cambio de UVGmon activo, inicio de combate, etc.</summary>
    public void RefreshTeamTab()
    {
        if (combatTeamUI != null)
            combatTeamUI.Refresh();
    }

    public void RenderMoveSelection(IReadOnlyList<MoveData> moves, int selectedMoveIndex)
    {
        if (moveOptionsContainer == null || moveOptionPrefab == null)
            return;

        // Antes solo comparaba la CANTIDAD de movimientos (moveOptionSlots.Count !=
        // moveCount) para decidir si reconstruir los slots. Eso evitaba el rebuild -- y
        // dejaba nombres obsoletos en pantalla -- cuando el UVGmon activo cambiaba a otro
        // con la misma cantidad de ataques pero distintos (Prompt 4/6: la lista debe
        // reflejar siempre los ataques REALES del activo). Comparar por referencia cada
        // MoveData detecta ese caso sin perder la optimizacion de no reconstruir en cada
        // hover del mismo turno (HandleMoveHovered llama a este metodo constantemente).
        if (!MovesMatch(lastRenderedMoves, moves))
        {
            RebuildMoveOptions(moves);
            lastRenderedMoves = moves;
        }

        for (int i = 0; i < moveOptionSlots.Count; i++)
            moveOptionSlots[i].SetHighlighted(i == selectedMoveIndex);
    }

    private static bool MovesMatch(IReadOnlyList<MoveData> a, IReadOnlyList<MoveData> b)
    {
        int countA = a != null ? a.Count : 0;
        int countB = b != null ? b.Count : 0;

        if (countA != countB)
            return false;

        for (int i = 0; i < countA; i++)
        {
            if (!ReferenceEquals(a[i], b[i]))
                return false;
        }

        return true;
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