using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Instancia y administra un BodyPartOptionUI por cada BodyPart de la criatura enemiga
/// actual, superpuestos sobre la vista del enemigo (mismo patron que BattleUIManager usa
/// para los slots de movimiento). Si la criatura no tiene extremidades definidas, se
/// muestra el sprite unico de siempre (defaultEnemyImage, en CreatureBattleView) en su lugar.
/// </summary>
public class EnemyBodyPartsView : MonoBehaviour
{
    [SerializeField] private Transform partsContainer;
    [SerializeField] private BodyPartOptionUI partOptionPrefab;
    [SerializeField] private CreatureBattleView defaultEnemyView;

    private readonly List<BodyPartOptionUI> partSlots = new List<BodyPartOptionUI>();

    public event Action<int> OnPartClicked;

    public int PartCount => partSlots.Count;

    /// <summary>Reconstruye las partes clickeables para la criatura enemiga actual.</summary>
    public void Setup(IReadOnlyList<BodyPart> parts)
    {
        foreach (BodyPartOptionUI slot in partSlots)
        {
            slot.OnClicked -= HandleSlotClicked;
            Destroy(slot.gameObject);
        }

        partSlots.Clear();

        bool hasParts = parts != null && parts.Count > 0;

        if (partsContainer != null)
            partsContainer.gameObject.SetActive(hasParts);

        if (defaultEnemyView != null)
            defaultEnemyView.gameObject.SetActive(!hasParts);

        if (!hasParts || partsContainer == null || partOptionPrefab == null)
            return;

        for (int i = 0; i < parts.Count; i++)
        {
            BodyPart part = parts[i];

            BodyPartOptionUI slot = Instantiate(partOptionPrefab, partsContainer);
            slot.gameObject.SetActive(true);
            slot.SetIndex(i);
            slot.SetSprite(part.EstadoDanado && part.ReferenciaVisualDanada != null
                ? part.ReferenciaVisualDanada
                : part.ReferenciaVisualNormal);
            slot.SetAnchoredPosition(part.definition.anchoredPosition);
            slot.SetSelected(false);
            slot.OnClicked += HandleSlotClicked;

            // El primero en la lista queda mas al fondo, el ultimo mas encima
            // (mismo orden que el autor definio en el Inspector).
            slot.transform.SetAsLastSibling();

            partSlots.Add(slot);
        }
    }

    public void SelectIndex(int index)
    {
        for (int i = 0; i < partSlots.Count; i++)
            partSlots[i].SetSelected(i == index);
    }

    /// <summary>Cambia el sprite de la parte a su variante danada (Prompt 17). No-op si ya no hay referencia.</summary>
    public void MarkDamaged(int index, Sprite damagedSprite)
    {
        if (index < 0 || index >= partSlots.Count || damagedSprite == null)
            return;

        partSlots[index].SetSprite(damagedSprite);
    }

    private void HandleSlotClicked(BodyPartOptionUI slot)
    {
        OnPartClicked?.Invoke(slot.Index);
    }
}
