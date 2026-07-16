using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Effects/Heal")]
public class HealItemEffect : ItemEffect
{
    [SerializeField] private bool fullHeal = false;
    [SerializeField] private int healAmount = 20;

    public override void Apply(CreatureRuntime target)
    {
        if (target == null)
            return;

        target.Heal(fullHeal ? target.MaxHP : healAmount);
    }
}
