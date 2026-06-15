using System.Collections;
using UnityEngine;

public abstract class MoveEffect : ScriptableObject
{
    public abstract IEnumerator Execute(
        CreatureRuntime user,
        CreatureRuntime target,
        MoveData move
    );
}