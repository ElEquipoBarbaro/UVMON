using System.Collections.Generic;
using UnityEngine;

public class PlayerParty : MonoBehaviour
{
    public static PlayerParty Instance { get; private set; }

    [Header("Starting Team")]
    [SerializeField] private List<CreatureData> startingCreatures = new List<CreatureData>();

    [Header("Starting Levels")]
    [SerializeField] private List<int> startingLevels = new List<int>();

    private List<CreatureRuntime> party = new List<CreatureRuntime>();

    public IReadOnlyList<CreatureRuntime> Party => party;

    private void Awake()
    {
        // Keep singleton alive even if player gets disabled
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        BuildStartingParty();
    }

    private void BuildStartingParty()
    {
        party.Clear();

        for (int i = 0; i < startingCreatures.Count; i++)
        {
            CreatureData creature = startingCreatures[i];

            if (creature == null)
                continue;

            int level = 1;

            if (i < startingLevels.Count)
                level = Mathf.Max(1, startingLevels[i]);

            party.Add(new CreatureRuntime(creature, level));
        }
    }

    public CreatureRuntime GetLeadCreature()
    {
        if (party.Count == 0)
            return null;

        return party[0];
    }

    public void SetLeadCreature(int index)
    {
        if (index < 0 || index >= party.Count)
            return;

        CreatureRuntime selected = party[index];
        party.RemoveAt(index);
        party.Insert(0, selected);
    }

    public void AddCreature(CreatureData creatureData, int level = 1)
    {
        if (creatureData == null)
            return;

        party.Add(new CreatureRuntime(creatureData, Mathf.Max(1, level)));
    }

    public void RemoveCreature(int index)
    {
        if (index < 0 || index >= party.Count)
            return;

        party.RemoveAt(index);
    }

    public bool HasUsableCreature()
    {
        foreach (CreatureRuntime creature in party)
        {
            if (creature.CurrentHP > 0)
                return true;
        }

        return false;
    }

    public void HealAll()
    {
        foreach (CreatureRuntime creature in party)
        {
            creature.Heal(creature.MaxHP);
        }
    }
}