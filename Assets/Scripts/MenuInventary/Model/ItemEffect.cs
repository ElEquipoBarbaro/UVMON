using UnityEngine;

public abstract class ItemEffect : ScriptableObject
{
    public abstract bool Use(CreatureRuntime target);
}