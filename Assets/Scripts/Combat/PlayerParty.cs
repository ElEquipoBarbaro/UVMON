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

    public bool SetLeadCreature(int index)
    {
        if (index < 0 || index >= party.Count)
            return false;

        if (party[index] == null)
            return false;

        if (index == 0)
            return true;

        CreatureRuntime selected = party[index];
        party.RemoveAt(index);
        party.Insert(0, selected);

        return true;
    }

    public bool IsUsableCreatureIndex(int index)
    {
        return index >= 0 &&
               index < party.Count &&
               party[index] != null &&
               party[index].CurrentHP > 0;
    }

    public int FindFirstUsableCreatureIndex(int startIndex = 0)
    {
        if (party.Count == 0)
            return -1;

        int firstIndex = Mathf.Clamp(startIndex, 0, party.Count);

        for (int i = firstIndex; i < party.Count; i++)
        {
            if (IsUsableCreatureIndex(i))
                return i;
        }

        for (int i = 0; i < firstIndex; i++)
        {
            if (IsUsableCreatureIndex(i))
                return i;
        }

        return -1;
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
            if (creature != null && creature.CurrentHP > 0)
                return true;
        }

        return false;
    }

    public void HealAll()
    {
        foreach (CreatureRuntime creature in party)
        {
            if (creature != null)
                creature.Heal(creature.MaxHP);
        }
    }
}
