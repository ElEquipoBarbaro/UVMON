using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PokemonDetailsPanel : MonoBehaviour
{
    [SerializeField]
    private Image itemImage;
    [SerializeField]
    private TMP_Text title;
    [SerializeField]
    private TMP_Text stats;
    [SerializeField]
    private Image hpFill;

    public void Awake()
    {
        ResetDetails();
    }

public void ResetDetails()
    {
        itemImage.gameObject.SetActive(false);
        title.text = "";
        stats.text = "";

        if (hpFill != null)
            hpFill.fillAmount = 0f;
    }

public void SetDetails(CreatureRuntime creature)
    {
        itemImage.gameObject.SetActive(true);
        itemImage.sprite = creature.data.frontSprite;
        itemImage.preserveAspect = true;
        title.text = creature.data.creatureName;

        if (hpFill != null)
        {
            float maxHp = Mathf.Max(1f, creature.MaxHP);
            hpFill.fillAmount = Mathf.Clamp01(creature.CurrentHP / maxHp);
        }

        stats.text =
            $"Ataque: {creature.Attack}\n" +
            $"Defensa: {creature.Defense}\n" +
            $"Velocidad: {creature.Speed}";
    }
}
