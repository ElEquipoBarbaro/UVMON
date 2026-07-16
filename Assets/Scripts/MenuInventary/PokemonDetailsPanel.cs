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

    public void Awake()
    {
        ResetDetails();
    }

    public void ResetDetails()
    {
        itemImage.gameObject.SetActive(false);
        title.text = "";
        stats.text = "";
    }

    public void SetDetails(CreatureRuntime creature)
    {
        itemImage.gameObject.SetActive(true);
        itemImage.sprite = creature.data.frontSprite;
        title.text = creature.data.creatureName;
        stats.text =
            $"HP: {creature.CurrentHP}/{creature.MaxHP}\n" +
            $"Attack: {creature.Attack}\n" +
            $"Defense: {creature.Defense}\n" +
            $"Speed: {creature.Speed}";
    }
}
