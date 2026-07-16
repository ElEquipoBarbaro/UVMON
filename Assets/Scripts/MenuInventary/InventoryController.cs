using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryController : MonoBehaviour
{


    private void PrepareUI()
        {
            inventoryUI.InitializeInventoryUI(inventoryData.Size);
            inventoryUI.OnDescriptionRequested += HandleDescriptionRequest;
            inventoryUI.OnSwapItems += HandleSwapItems;
            inventoryUI.OnStartDragging += HandleDragging;
            inventoryUI.OnItemActionRequested += HandleItemActionRequest;

            itemsTabButton.onClick.AddListener(ShowItemsTab);
            pokemonTabButton.onClick.AddListener(ShowPokemonTab);
        }




    [SerializeField]
    private UIInventoryPage inventoryUI;

    [SerializeField]
    private InventorySO inventoryData;

    [SerializeField]
    private PokemonTabController pokemonTab;

    [SerializeField]
    private Button itemsTabButton;

    [SerializeField]
    private Button pokemonTabButton;

    private bool isMenuOpen = false;

    public List<InventoryItem> initialItems = new List<InventoryItem>();

     private void Start()
        {
            PrepareUI();
            PrepareInventoryData();

        }

    private void PrepareInventoryData()
    {
        inventoryData.Initialize();
        inventoryData.OnInventoryUpdated+=UpdateInventoryUI;
        foreach ( var item in initialItems)
        {
            if (item.IsEmpty)
            {
                continue;

            }
            inventoryData.AddItem(item.item, item.quantity);

        }
    }
    // para inicializar todo

     private void UpdateInventoryUI(Dictionary<int, InventoryItem> inventoryState)
        {
            inventoryUI.ResetAllItems();
            foreach (var item in inventoryState)
            {
                inventoryUI.UpdateData(item.Key, item.Value.item.ItemImage,
                    item.Value.quantity);
            }
        }
    private void HandleDescriptionRequest(int itemIndex)
    {
        InventoryItem inventoryItem = inventoryData.GetItemAt(itemIndex);
            if (inventoryItem.IsEmpty)
            {
                inventoryUI.ResetSelection();
                return;
            }
            ItemSO item = inventoryItem.item;

            inventoryUI.UpdateDescription(itemIndex, item.ItemImage,
                item.name, item.Description);
    }
    private void HandleSwapItems(int itemIndex_1, int itemIndex_2)
    {
            inventoryData.SwapItems(itemIndex_1, itemIndex_2);
    }

    private void HandleDragging(int itemIndex)
    {
        InventoryItem inventoryitem = inventoryData.GetItemAt(itemIndex);
        if (inventoryitem.IsEmpty)
        {
            return;
        }
        inventoryUI.CreateDraggedItem(inventoryitem.item.ItemImage, inventoryitem.quantity);


    }
    private void HandleItemActionRequest(int itemIndex)
    {
        InventoryItem inventoryItem = inventoryData.GetItemAt(itemIndex);
        if (inventoryItem.IsEmpty || inventoryItem.item.Category != ItemCategory.Healing)
            return;

        if (inventoryItem.item.Effect == null)
            return;

        if (PlayerParty.Instance == null || PlayerParty.Instance.Party.Count == 0)
            return;

        if (PlayerParty.Instance.Party.Count == 1)
        {
            UseHealingItem(itemIndex, PlayerParty.Instance.Party[0]);
            return;
        }

        ShowPokemonTab();
        pokemonTab.BeginTargetSelection(target => UseHealingItem(itemIndex, target));
    }

    private void UseHealingItem(int itemIndex, CreatureRuntime target)
    {
        InventoryItem inventoryItem = inventoryData.GetItemAt(itemIndex);
        if (inventoryItem.IsEmpty)
            return;

        inventoryItem.item.Effect.Apply(target);
        inventoryData.RemoveItem(itemIndex, 1);
        pokemonTab.Refresh();
    }

    private void ShowItemsTab()
    {
        pokemonTab.Hide();
        inventoryUI.Show();
        UpdateInventoryUI(inventoryData.GetCurrentInventoryState());
    }

    private void ShowPokemonTab()
    {
        inventoryUI.Hide();
        pokemonTab.Show();
    }

    private void OpenMenu()
    {
        isMenuOpen = true;
        ShowItemsTab();
    }

    private void CloseMenu()
    {
        isMenuOpen = false;
        inventoryUI.Hide();
        pokemonTab.Hide();
    }

     public void Update()
        {
            if (Input.GetKeyDown(KeyCode.I))
            {
                if (!isMenuOpen)
                {
                    OpenMenu();
                }
                else
                {
                    CloseMenu();
                }

            }
        }
}

