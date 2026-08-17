using System;
using System.Collections.Generic;
using UnityEngine;

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

    private readonly List<CombatTeamSlotUI> spawnedSlots = new List<CombatTeamSlotUI>();

    /// <summary>El jugador hizo clic sobre un integrante del equipo (indice real en
    /// PlayerParty.Instance.Party). CombatManager decide si la seleccion es valida
    /// (Prompt 6) -- esta clase no valida nada, solo reemite.</summary>
    public event Action<int> OnCreatureSelected;

    private void OnEnable()
    {
        Refresh();
    }

    /// <summary>Fuerza una relectura del equipo real (abrir la pestana, HP cambio,
    /// UVGmon activo cambio, empezo un combate, etc).</summary>
    public void Refresh()
    {
        foreach (CombatTeamSlotUI slot in spawnedSlots)
        {
            if (slot != null)
            {
                slot.OnClicked -= HandleSlotClicked;
                Destroy(slot.gameObject);
            }
        }
        spawnedSlots.Clear();

        if (contentContainer == null || slotPrefab == null)
            return;

        IReadOnlyList<CreatureRuntime> party = PlayerParty.Instance != null
            ? PlayerParty.Instance.Party
            : null;

        bool hasAny = party != null && party.Count > 0;

        if (hasAny)
        {
            for (int i = 0; i < party.Count; i++)
            {
                CreatureRuntime creature = party[i];
                if (creature == null)
                    continue;

                CombatTeamSlotUI slot = Instantiate(slotPrefab, contentContainer);
                slot.gameObject.SetActive(true);
                slot.SetData(creature, i, isActive: i == 0);
                slot.OnClicked += HandleSlotClicked;
                spawnedSlots.Add(slot);
            }
        }

        if (emptyStateLabel != null)
            emptyStateLabel.SetActive(!hasAny);
    }

    private void HandleSlotClicked(CombatTeamSlotUI slot)
    {
        OnCreatureSelected?.Invoke(slot.PartyIndex);
    }
}
