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

    
     private void Start()
        {
            PrepareUI();
            //inventoryData.Initialize();
        }

    // para inicializar todo

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
            
    }

    private void HandleDragging(int itemIndex)
    {
        
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

