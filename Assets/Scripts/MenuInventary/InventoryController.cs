using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryController : MonoBehaviour
{


private void PrepareUI()
    {
        if (inventoryUI == null || inventoryData == null)
        {
            Debug.LogError("InventoryController necesita referencias válidas al inventario y a su UI.", this);
            return;
        }

        inventoryUI.InitializeInventoryUI(inventoryData.Size);

        inventoryUI.OnDescriptionRequested -= HandleDescriptionRequest;
        inventoryUI.OnSwapItems -= HandleSwapItems;
        inventoryUI.OnStartDragging -= HandleDragging;
        inventoryUI.OnItemActionRequested -= HandleItemActionRequest;

        inventoryUI.OnDescriptionRequested += HandleDescriptionRequest;
        inventoryUI.OnSwapItems += HandleSwapItems;
        inventoryUI.OnStartDragging += HandleDragging;
        inventoryUI.OnItemActionRequested += HandleItemActionRequest;

        if (itemsTabButton != null)
        {
            itemsTabButton.onClick.RemoveListener(ShowItemsTab);
            itemsTabButton.onClick.AddListener(ShowItemsTab);
        }

        if (pokemonTabButton != null)
        {
            pokemonTabButton.onClick.RemoveListener(ShowPokemonTab);
            pokemonTabButton.onClick.AddListener(ShowPokemonTab);
        }
    }




    [SerializeField]
    private GameObject inGameMenu;

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

private void OnDestroy()
    {
        if (inventoryData != null)
            inventoryData.OnInventoryUpdated -= UpdateInventoryUI;

        if (inventoryUI != null)
        {
            inventoryUI.OnDescriptionRequested -= HandleDescriptionRequest;
            inventoryUI.OnSwapItems -= HandleSwapItems;
            inventoryUI.OnStartDragging -= HandleDragging;
            inventoryUI.OnItemActionRequested -= HandleItemActionRequest;
        }

        if (itemsTabButton != null)
            itemsTabButton.onClick.RemoveListener(ShowItemsTab);

        if (pokemonTabButton != null)
            pokemonTabButton.onClick.RemoveListener(ShowPokemonTab);
    }


private void PrepareInventoryData()
    {
        if (inventoryData == null)
            return;

        inventoryData.Initialize();

        // InventorySO persiste entre escenas. Eliminar antes de agregar hace que
        // la suscripción sea idempotente y evita callbacks hacia UI destruidas.
        inventoryData.OnInventoryUpdated -= UpdateInventoryUI;
        inventoryData.OnInventoryUpdated += UpdateInventoryUI;

        foreach (InventoryItem item in initialItems)
        {
            if (item.IsEmpty)
                continue;

            inventoryData.AddItem(item.item, item.quantity);
        }
    }
    // para inicializar todo

private void UpdateInventoryUI(Dictionary<int, InventoryItem> inventoryState)
    {
        if (inventoryUI == null)
            return;

        inventoryUI.ResetAllItems();

        if (inventoryState == null)
            return;

        foreach (KeyValuePair<int, InventoryItem> item in inventoryState)
        {
            if (item.Value.IsEmpty || item.Value.item == null)
                continue;

            inventoryUI.UpdateData(
                item.Key,
                item.Value.item.ItemImage,
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
        itemsTabButton.Select();
        UpdateInventoryUI(inventoryData.GetCurrentInventoryState());
    }

private void ShowPokemonTab()
    {
        inventoryUI.Hide();
        pokemonTab.Show();
        pokemonTabButton.Select();
    }

    private void OpenMenu()
    {
        isMenuOpen = true;
        inGameMenu.SetActive(true);
        ShowItemsTab();
    }

    private void CloseMenu()
    {
        isMenuOpen = false;
        inventoryUI.Hide();
        pokemonTab.Hide();
        inGameMenu.SetActive(false);
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

