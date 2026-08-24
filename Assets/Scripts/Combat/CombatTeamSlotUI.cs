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
    [SerializeField] private Image background;
    [SerializeField] private Color normalColor = new Color(0f, 0f, 0f, 0.35f);
    [SerializeField] private Color selectedColor = new Color(0.2f, 0.65f, 1f, 0.65f);
    [SerializeField] private Color lockedColor = new Color(0f, 0f, 0f, 0.18f);

    public CreatureRuntime Creature { get; private set; }
    public int PartyIndex { get; private set; }

    private bool isSelected;
    private bool isInteractable = true;

    private void Awake()
    {
        if (background == null)
            background = GetComponent<Image>();

        RefreshBackground();
    }

    public void SetData(CreatureRuntime creature, int partyIndex, bool isActive)
    {
        Creature = creature;
        PartyIndex = partyIndex;

        if (creature == null || creature.data == null)
            return;

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

        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        RefreshBackground();
    }

    public void SetInteractable(bool interactable)
    {
        isInteractable = interactable;
        RefreshBackground();
    }

    private void RefreshBackground()
    {
        if (background == null)
            return;

        background.color = !isInteractable
            ? lockedColor
            : (isSelected ? selectedColor : normalColor);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isInteractable)
            return;

        OnClicked?.Invoke(this);
    }
}
