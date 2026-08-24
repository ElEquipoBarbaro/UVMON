using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Representacion visual, de solo lectura (Prompt 5), del equipo REAL del jugador dentro
/// del menu de combate. No guarda copia propia del equipo: cada Refresh() lee
/// PlayerParty.Instance.Party directamente (el mismo objeto que ya usa PokemonTabController
/// fuera de combate y que CombatManager usa como playerRuntime -- ver
/// MenuInventary/Systems/PokemonSelectionSystem.md). El integrante en el indice 0 es
/// siempre el activo (misma convencion que PlayerParty.GetLeadCreature()/SetLeadCreature).
/// </summary>
public class CombatTeamUI : MonoBehaviour
{
    [SerializeField] private Transform contentContainer;
    [SerializeField] private CombatTeamSlotUI slotPrefab;
    [SerializeField] private GameObject emptyStateLabel;

    [Header("Selection")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private TextMeshProUGUI statusText;

    private readonly List<CombatTeamSlotUI> spawnedSlots = new List<CombatTeamSlotUI>();
    private int selectedPartyIndex = -1;
    private bool inputEnabled;

    /// <summary>El jugador hizo clic sobre un integrante del equipo (indice real en
    /// PlayerParty.Instance.Party). CombatManager decide si la seleccion es valida
    /// (Prompt 6) -- esta clase no valida nada, solo reemite.</summary>
    public event Action<int> OnCreatureSelected;
    public event Action OnCancelled;

    private void Awake()
    {
        if (confirmButton != null)
            confirmButton.onClick.AddListener(ConfirmSelection);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(CancelSelectionAndNotify);

        UpdateActionButtons();
    }

    private void OnDestroy()
    {
        if (confirmButton != null)
            confirmButton.onClick.RemoveListener(ConfirmSelection);

        if (cancelButton != null)
            cancelButton.onClick.RemoveListener(CancelSelectionAndNotify);

        foreach (CombatTeamSlotUI slot in spawnedSlots)
        {
            if (slot != null)
                slot.OnClicked -= HandleSlotClicked;
        }
    }

    private void OnEnable()
    {
        Refresh();
    }

    /// <summary>Fuerza una relectura del equipo real (abrir la pestana, HP cambio,
    /// UVGmon activo cambio, empezo un combate, etc).</summary>
    public void Refresh()
    {
        selectedPartyIndex = -1;

        foreach (CombatTeamSlotUI slot in spawnedSlots)
        {
            if (slot != null)
            {
                slot.OnClicked -= HandleSlotClicked;
                Destroy(slot.gameObject);
            }
        }
        spawnedSlots.Clear();

        SetStatus(string.Empty);
        UpdateActionButtons();

        if (contentContainer == null || slotPrefab == null)
            return;

        IReadOnlyList<CreatureRuntime> party = PlayerParty.Instance != null
            ? PlayerParty.Instance.Party
            : null;

        bool hasAny = false;

        if (party != null)
        {
            for (int i = 0; i < party.Count; i++)
            {
                CreatureRuntime creature = party[i];
                if (creature == null)
                    continue;

                hasAny = true;

                CombatTeamSlotUI slot = Instantiate(slotPrefab, contentContainer);
                slot.gameObject.SetActive(true);
                slot.SetData(creature, i, isActive: i == 0);
                slot.SetInteractable(inputEnabled);
                slot.OnClicked += HandleSlotClicked;
                slot.transform.SetSiblingIndex(spawnedSlots.Count);
                spawnedSlots.Add(slot);
            }
        }

        if (emptyStateLabel != null)
            emptyStateLabel.SetActive(!hasAny);
    }

    private void HandleSlotClicked(CombatTeamSlotUI slot)
    {
        if (!inputEnabled || slot == null)
            return;

        selectedPartyIndex = slot.PartyIndex;

        foreach (CombatTeamSlotUI spawnedSlot in spawnedSlots)
            spawnedSlot.SetSelected(spawnedSlot == slot);

        if (slot.PartyIndex == 0)
            SetStatus("Este UVGmon ya esta en combate.");
        else if (slot.Creature == null || slot.Creature.CurrentHP <= 0)
            SetStatus("Este UVGmon esta derrotado y no puede combatir.");
        else
            SetStatus($"Seleccionado: {slot.Creature.data.creatureName}");

        UpdateActionButtons();

        // Compatibilidad segura si una escena antigua no tiene todavia el boton de
        // confirmacion conectado: conserva el comportamiento de clic directo existente.
        if (confirmButton == null && IsSelectedCreatureUsable())
        {
            SetInputEnabled(false);
            OnCreatureSelected?.Invoke(selectedPartyIndex);
        }
    }

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;

        foreach (CombatTeamSlotUI slot in spawnedSlots)
            slot.SetInteractable(enabled);

        UpdateActionButtons();
    }

    public void CancelSelection(bool clearStatus = true)
    {
        selectedPartyIndex = -1;

        foreach (CombatTeamSlotUI slot in spawnedSlots)
            slot.SetSelected(false);

        if (clearStatus)
            SetStatus(string.Empty);

        UpdateActionButtons();
    }

    private void ConfirmSelection()
    {
        if (!IsSelectedCreatureUsable())
        {
            UpdateActionButtons();
            return;
        }

        int confirmedIndex = selectedPartyIndex;
        SetInputEnabled(false);
        OnCreatureSelected?.Invoke(confirmedIndex);
    }

    private void CancelSelectionAndNotify()
    {
        if (!inputEnabled)
            return;

        CancelSelection();
        OnCancelled?.Invoke();
    }

    private bool IsSelectedCreatureUsable()
    {
        if (!inputEnabled || PlayerParty.Instance == null || selectedPartyIndex <= 0)
            return false;

        return PlayerParty.Instance.IsUsableCreatureIndex(selectedPartyIndex);
    }

    private void UpdateActionButtons()
    {
        if (confirmButton != null)
            confirmButton.interactable = IsSelectedCreatureUsable();

        if (cancelButton != null)
            cancelButton.interactable = inputEnabled;
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }
}
