using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Effects/Heal")]
public class HealEffect : ItemEffect
{
    [SerializeField] private int healAmount = 20;

    public override bool Use(CreatureRuntime target)
    {
        if (target == null)
            return false;

        if (target.CurrentHP >= target.MaxHP)
            return false;

        target.Heal(healAmount);

        return true;
    }
}