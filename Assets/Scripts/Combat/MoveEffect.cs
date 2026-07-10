using UnityEngine;
public abstract class MoveEffect : ScriptableObject
{
    public abstract void Execute(
        CreatureRuntime user,
        CreatureRuntime target,
        MoveData move
    );
}