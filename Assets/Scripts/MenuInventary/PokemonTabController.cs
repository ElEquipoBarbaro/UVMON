using System;
using System.Collections.Generic;
using UnityEngine;

public class PokemonTabController : MonoBehaviour
{
    [SerializeField]
    private PokemonSlotUI slotPrefab;

    [SerializeField]
    private RectTransform contentPanel;

    [SerializeField]
    private PokemonDetailsPanel detailsPanel;

    private readonly List<PokemonSlotUI> spawnedSlots = new List<PokemonSlotUI>();

    private Action<CreatureRuntime> targetSelectionCallback;

    public void Show()
    {
        gameObject.SetActive(true);
        Refresh();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        CancelTargetSelection();
    }

    public void Refresh()
    {
        foreach (PokemonSlotUI slot in spawnedSlots)
        {
            slot.OnClicked -= HandleSlotClicked;
            Destroy(slot.gameObject);
        }
        spawnedSlots.Clear();
        detailsPanel.ResetDetails();

        if (PlayerParty.Instance == null)
            return;

        foreach (CreatureRuntime creature in PlayerParty.Instance.Party)
        {
            PokemonSlotUI slot = Instantiate(slotPrefab, contentPanel);
            slot.transform.localScale = Vector3.one;
            slot.transform.localPosition = Vector3.zero;
            slot.SetData(creature);
            slot.OnClicked += HandleSlotClicked;
            spawnedSlots.Add(slot);
        }
    }

    public void BeginTargetSelection(Action<CreatureRuntime> onSelected)
    {
        targetSelectionCallback = onSelected;
    }

    public void CancelTargetSelection()
    {
        targetSelectionCallback = null;
    }

    private void HandleSlotClicked(PokemonSlotUI slot)
    {
        if (targetSelectionCallback != null)
        {
            Action<CreatureRuntime> callback = targetSelectionCallback;
            targetSelectionCallback = null;
            callback(slot.Creature);
            return;
        }

        foreach (PokemonSlotUI s in spawnedSlots)
            s.Deselect();

        slot.Select();
        detailsPanel.SetDetails(slot.Creature);
    }
}
