using System.Collections.Generic;
using UnityEngine;

public class CreatureRuntime
{
    public CreatureData data;

    public int Level { get; private set; }
    public int CurrentXP { get; private set; }
    public int CurrentHP { get; private set; }

    public List<MoveData> Moves { get; private set; }

    public int MaxHP => data.maxHP + (Level - 1) * 5;
    public int Attack => data.attack + (Level - 1) * 2;
    public int Defense => data.defense + (Level - 1) * 2;
    public int Speed => data.speed + (Level - 1) * 2;

    public CreatureRuntime(CreatureData data, int level = 1, List<MoveData> overrideMoves = null)
    {
        this.data = data;

        Level = Mathf.Max(1, level);
        CurrentXP = 0;
        CurrentHP = MaxHP;

        if (overrideMoves != null && overrideMoves.Count > 0)
            Moves = new List<MoveData>(overrideMoves);
        else
            Moves = data.moves != null ? new List<MoveData>(data.moves) : new List<MoveData>();
    }

    public void TakeDamage(int amount)
    {
        CurrentHP = Mathf.Max(0, CurrentHP - amount);
    }

    public void Heal(int amount)
    {
        CurrentHP = Mathf.Min(MaxHP, CurrentHP + amount);
    }

    public void GainXP(int amount)
    {
        CurrentXP += amount;

        while (CurrentXP >= XPToNextLevel())
        {
            CurrentXP -= XPToNextLevel();
            LevelUp();
        }
    }

    private void LevelUp()
    {
        Level++;
        CurrentHP = MaxHP;

        Debug.Log(data.creatureName + " leveled up to " + Level + "!");
    }

    private int XPToNextLevel()
    {
        return Level * 25;
    }
}