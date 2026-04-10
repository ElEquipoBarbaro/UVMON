using System.Collections.Generic;
using UnityEngine;

public class InventoryTester : MonoBehaviour
{
    [Header("Items iniciales")]
    [SerializeField] private List<ItemData> starterItems = new List<ItemData>();

    [Header("Cantidades")]
    [SerializeField] private List<int> starterAmounts = new List<int>();

    [Header("UVmones iniciales")]
    [SerializeField] private List<UVmonData> starterUVmones = new List<UVmonData>();

    [Header("Niveles")]
    [SerializeField] private List<int> starterLevels = new List<int>();

    private void Start()
    {
        if (InventoryManager.Instance == null) return;

        for (int i = 0; i < starterItems.Count; i++)
        {
            int amount = 1;

            if (i < starterAmounts.Count)
                amount = starterAmounts[i];

            InventoryManager.Instance.AddItem(starterItems[i], amount);
        }

        for (int i = 0; i < starterUVmones.Count; i++)
        {
            int level = 1;

            if (i < starterLevels.Count)
                level = starterLevels[i];

            InventoryManager.Instance.AddUVmon(starterUVmones[i], level);
        }
    }
}