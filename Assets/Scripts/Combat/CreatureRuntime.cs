using UnityEngine;
public class CreatureRuntime
{
    public CreatureData data;

    public int CurrentHP { get; private set; }

    public int Attack => data.attack;
    public int Defense => data.defense;

    public CreatureRuntime(CreatureData data)
    {
        this.data = data;
        CurrentHP = data.maxHP;
    }

    public void TakeDamage(int amount)
    {
        CurrentHP -= amount;
    }

    public void Heal(int amount)
    {
        CurrentHP = Mathf.Min(CurrentHP + amount, data.maxHP);
    }
}