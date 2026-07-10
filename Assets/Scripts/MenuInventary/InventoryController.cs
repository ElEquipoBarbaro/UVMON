using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryController : MonoBehaviour
{


    private void PrepareUI()
        {
            inventoryUI.InitializeInventoryUI(inventoryData.Size);
            inventoryUI.OnDescriptionRequested += HandleDescriptionRequest;
            inventoryUI.OnSwapItems += HandleSwapItems;
            inventoryUI.OnStartDragging += HandleDragging;
            inventoryUI.OnItemActionRequested += HandleItemActionRequest;
        }




    [SerializeField]
    private UIInventoryPage inventoryUI;

    [SerializeField]
    private InventorySO inventoryData;

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
        
    }
     public void Update()
        {
            if (Input.GetKeyDown(KeyCode.I))
            {
                if (inventoryUI.isActiveAndEnabled == false)
                {
                    inventoryUI.Show();
                    foreach (var item in inventoryData.GetCurrentInventoryState())
                    {
                        inventoryUI.UpdateData(
                            item.Key, 
                            item.Value.item.ItemImage,
                        item.Value.quantity);
                    }
                   
                }
                else
                {
                    inventoryUI.Hide();
                }

            }
        }
}

