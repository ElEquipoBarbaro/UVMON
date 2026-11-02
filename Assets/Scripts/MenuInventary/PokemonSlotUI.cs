using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PokemonSlotUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField]
    private Image itemImage;
    [SerializeField]
    private TMP_Text nameTxt;
    [SerializeField]
    private TMP_Text hpTxt;
    [SerializeField]
    private Image hpFill;
    [SerializeField]
    private Image borderImage;

    public event Action<PokemonSlotUI> OnClicked;

    public CreatureRuntime Creature { get; private set; }

public void SetData(CreatureRuntime creature)
    {
        Creature = creature;
        itemImage.gameObject.SetActive(true);
        itemImage.sprite = creature.data.frontSprite;
        itemImage.preserveAspect = true;
        nameTxt.text = creature.data.creatureName;
        hpTxt.text = string.Empty;

        if (hpFill != null)
        {
            float maxHp = Mathf.Max(1f, creature.MaxHP);
            hpFill.fillAmount = Mathf.Clamp01(creature.CurrentHP / maxHp);
        }
    }

    public void Select()
    {
        borderImage.enabled = true;
    }

    public void Deselect()
    {
        borderImage.enabled = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnClicked?.Invoke(this);
    }
}
