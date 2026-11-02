using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Effects/Damage")]
public class DamageEffect : MoveEffect
{
    public override IEnumerator Execute(
        CreatureRuntime user,
        CreatureRuntime target,
        MoveData move
    )
    {
        BattleUIManager ui = CombatManager.Instance != null
            ? CombatManager.Instance.BattleUI
            : null;

        if (user == null || target == null || move == null)
            yield break;

        float typeMultiplier = TypeChart.GetMultiplier(
            move.moveType,
            target.data.type
        );

        int baseDamage = (user.Attack + move.power) - target.Defense;
        baseDamage = Mathf.Max(1, baseDamage);

        float variance = Random.Range(0.85f, 1f);

        bool isCrit = Random.value < 0.1f;
        float critMultiplier = isCrit ? 1.5f : 1f;

        int damage = Mathf.Max(
            1,
            Mathf.RoundToInt(baseDamage * variance * critMultiplier * typeMultiplier)
        );

        if (ui != null)
        {
            ui.ShowBattleMessage($"¡{user.data.creatureName} usó {move.moveName}!");
            yield return new WaitForSeconds(0.9f);

            if (isCrit)
            {
                ui.ShowBattleMessage("¡Golpe crítico! El impacto fue devastador.");
                yield return new WaitForSeconds(0.8f);
            }

            if (typeMultiplier > 1f)
            {
                ui.ShowBattleMessage("¡Es muy eficaz! El ataque golpeó su punto débil.");
                yield return new WaitForSeconds(0.8f);
            }
            else if (typeMultiplier < 1f)
            {
                ui.ShowBattleMessage("No fue muy eficaz... El rival resistió el impacto.");
                yield return new WaitForSeconds(0.8f);
            }
        }

        target.TakeDamage(damage);

        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.RefreshBattleUI();
            CombatManager.Instance.PlayHitFlashFor(target);
        }

        if (ui != null)
        {
            ui.ShowBattleMessage($"¡{target.data.creatureName} recibió {damage} puntos de daño!");
            yield return new WaitForSeconds(0.9f);

            if (target.CurrentHP <= 0)
            {
                ui.ShowBattleMessage($"¡{target.data.creatureName} quedó debilitado!");
                yield return new WaitForSeconds(1f);
            }
        }
    }
}