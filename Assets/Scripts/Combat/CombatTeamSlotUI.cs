using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Fila de un integrante del equipo en la pestana EQUIPO del menu de combate (Prompt 5).
/// Solo pinta lo que CombatTeamUI le pasa -- mismo patron "dumb relay" que
/// PokemonSlotUI/UIInventoryItem. El click (Prompt 6, rotacion de UVGmon activo) solo
/// reemite el evento; la validacion real (¿puede combatir? ¿es turno del jugador?) vive en
/// CombatManager, igual que BodyPartOptionUI/MoveOptionUI.
/// </summary>
public class CombatTeamSlotUI : MonoBehaviour, IPointerClickHandler
{
    public event Action<CombatTeamSlotUI> OnClicked;

    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private Image hpFillImage;
    [SerializeField] private GameObject activeMarker;
    [SerializeField] private Image faintedOverlay;

    public CreatureRuntime Creature { get; private set; }
    public int PartyIndex { get; private set; }

    public void SetData(CreatureRuntime creature, int partyIndex, bool isActive)
    {
        Creature = creature;
        PartyIndex = partyIndex;

        if (icon != null)
        {
            icon.sprite = creature.data.frontSprite;
            icon.enabled = creature.data.frontSprite != null;
        }

        if (nameText != null)
            nameText.text = creature.data.creatureName;

        if (hpText != null)
            hpText.text = $"{creature.CurrentHP}/{creature.MaxHP}";

        if (hpFillImage != null)
        {
            float hpNormalizado = creature.MaxHP > 0
                ? Mathf.Clamp01((float)creature.CurrentHP / creature.MaxHP)
                : 0f;
            hpFillImage.fillAmount = hpNormalizado;
        }

        if (activeMarker != null)
            activeMarker.SetActive(isActive);

        if (faintedOverlay != null)
            faintedOverlay.enabled = creature.CurrentHP <= 0;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnClicked?.Invoke(this);
    }
}
