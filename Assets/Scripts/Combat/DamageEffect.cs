using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Effects/Damage")]
public class DamageEffect : MoveEffect
{
    public override void Execute(
        CreatureRuntime user,
        CreatureRuntime target,
        MoveData move
    )
    {

        int baseDamage = (user.Attack + move.power) - target.Defense;
        baseDamage = Mathf.Max(1, baseDamage);

        float variance = Random.Range(0.85f, 1f);

        bool isCrit = Random.value < 0.1f;
        float critMultiplier = isCrit ? 1.5f : 1f;

        int damage = Mathf.RoundToInt(baseDamage * variance * critMultiplier);

        Debug.Log($"{user.data.creatureName} used {move.moveName} for {damage} damage!" +
                  (isCrit ? " CRITICAL HIT!" : ""));

        target.TakeDamage(damage);
    }
}