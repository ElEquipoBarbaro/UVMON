using UnityEngine;

public class EnemyTrainer : MonoBehaviour
{
    [SerializeField] private TrainerData trainerData;

    public TrainerData TrainerData => trainerData;

    public CreatureRuntime GetLeadCreature()
    {
        if (trainerData == null || trainerData.team == null || trainerData.team.Count == 0)
            return null;

        TrainerCreatureSlot slot = trainerData.team[0];

        if (slot == null || slot.creatureData == null)
            return null;

        return new CreatureRuntime(
            slot.creatureData,
            slot.level,
            slot.overrideMoves ? slot.customMoves : null
        );
    }
}