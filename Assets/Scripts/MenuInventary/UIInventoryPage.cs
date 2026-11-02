using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


public class UIInventoryPage : MonoBehaviour
{
    [SerializeField]
    private UIInventoryItem itemPrefab;

    [SerializeField]
    private RectTransform contentPanel;

    [SerializeField]
        private MouseFollower mouseFollower;

    List<UIInventoryItem> listOfUIItems = new List<UIInventoryItem>();

    

    private int currentlyDraggedItemIndex = -1;
    private int contextMenuTargetIndex = -1;

    public event Action<int> OnDescriptionRequested,
                OnItemActionRequested,
                OnStartDragging;

    public event Action<int, int> OnSwapItems;

    [SerializeField]
    private UIInventoryDescription itemDescription;

    [SerializeField]
    private ItemContextMenu itemContextMenu;

private void Awake()
    {
        if (mouseFollower != null)
            mouseFollower.Toggle(false);

        if (itemDescription != null)
            itemDescription.ResetDescription();

        if (itemContextMenu != null)
        {
            itemContextMenu.Hide();
            itemContextMenu.OnUseClicked -= HandleUseItem;
            itemContextMenu.OnUseClicked += HandleUseItem;
        }
    }

private void OnDestroy()
    {
        if (itemContextMenu != null)
            itemContextMenu.OnUseClicked -= HandleUseItem;

        ClearInventoryItems();
    }

public void InitializeInventoryUI(int inventorysize)
    {
        ClearInventoryItems();

        if (itemPrefab == null || contentPanel == null)
        {
            Debug.LogError("No se puede inicializar el inventario: faltan el prefab o el panel de contenido.", this);
            return;
        }

        for (int i = 0; i < inventorysize; i++)
        {
            UIInventoryItem uiItem = Instantiate(itemPrefab, contentPanel);
            uiItem.transform.localScale = Vector3.one;
            uiItem.transform.localPosition = Vector3.zero;
            listOfUIItems.Add(uiItem);
            uiItem.OnItemClicked += HandleItemSelection;
            uiItem.OnItemBeginDrag += HandleBeginDrag;
            uiItem.OnItemDroppedOn += HandleSwap;
            uiItem.OnItemEndDrag += HandleEndDrag;
            uiItem.OnRightMouseBtnClick += HandleShowItemActions;
        }
    }

public void UpdateData(int itemIndex, Sprite itemImage, int itemQuantity)
    {
        if (itemIndex < 0 || itemIndex >= listOfUIItems.Count)
            return;

        UIInventoryItem item = listOfUIItems[itemIndex];
        if (item != null)
            item.SetData(itemImage, itemQuantity);
    }

private void ResetDraggedItem()
    {
        if (mouseFollower != null)
            mouseFollower.Toggle(false);

        currentlyDraggedItemIndex = -1;
    }

      private void HandleShowItemActions(UIInventoryItem inventoryItemUI)
        {
            int index = listOfUIItems.IndexOf(inventoryItemUI);
            if (index == -1)
                return;

            contextMenuTargetIndex = index;
            itemContextMenu.Show(inventoryItemUI.transform.position);
        }

        private void HandleUseItem()
        {
            itemContextMenu.Hide();

            if (contextMenuTargetIndex == -1)
                return;

            OnItemActionRequested?.Invoke(contextMenuTargetIndex);
            contextMenuTargetIndex = -1;
        }

        private void HandleEndDrag(UIInventoryItem inventoryItemUI)
        {
           ResetDraggedItem();
        }

        private void HandleSwap(UIInventoryItem inventoryItemUI)
        {
             int index = listOfUIItems.IndexOf(inventoryItemUI);
            if (index == -1)
            {
                
                return;
            }

            OnSwapItems?.Invoke(currentlyDraggedItemIndex, index);
            HandleItemSelection(inventoryItemUI);
           
        }

         private void HandleBeginDrag(UIInventoryItem inventoryItemUI)
        {
            int index = listOfUIItems.IndexOf(inventoryItemUI);
            if (index == -1)
                return;
            currentlyDraggedItemIndex = index;
            HandleItemSelection(inventoryItemUI);
            OnStartDragging?.Invoke(index);
             
        }

        public void CreateDraggedItem(Sprite sprite, int quantity)
        {
            mouseFollower.Toggle(true);
            mouseFollower.SetData(sprite, quantity);
        }
internal void ResetAllItems()
    {
        listOfUIItems.RemoveAll(item => item == null);

        foreach (UIInventoryItem item in listOfUIItems)
        {
            item.ResetData();
            item.Deselect();
        }
    }


        private void HandleItemSelection(UIInventoryItem inventoryItemUI)
        {
            itemContextMenu.Hide();

            int index = listOfUIItems.IndexOf(inventoryItemUI);
            if (index == -1)
                return;
            OnDescriptionRequested?.Invoke(index);

        }

internal void UpdateDescription(int itemIndex, Sprite itemImage, string name, string description)
    {
        if (itemDescription != null)
            itemDescription.SetDescription(itemImage, name, description);

        DeselectAllItems();

        if (itemIndex >= 0 && itemIndex < listOfUIItems.Count && listOfUIItems[itemIndex] != null)
            listOfUIItems[itemIndex].Select();
    }

    public void Show()
        {
            gameObject.SetActive(true);
            ResetSelection();
          
        }
    public void ResetSelection()
        {
            itemDescription.ResetDescription();
            DeselectAllItems();
        }

private void DeselectAllItems()
    {
        listOfUIItems.RemoveAll(item => item == null);

        foreach (UIInventoryItem item in listOfUIItems)
            item.Deselect();
    }

    public void Hide()
        {

            gameObject.SetActive(false);
            ResetDraggedItem();
            itemContextMenu.Hide();

        }


private void ClearInventoryItems()
    {
        foreach (UIInventoryItem item in listOfUIItems)
        {
            if (item == null)
                continue;

            UnsubscribeFromItem(item);
            Destroy(item.gameObject);
        }

        listOfUIItems.Clear();
    }

    private void UnsubscribeFromItem(UIInventoryItem item)
    {
        item.OnItemClicked -= HandleItemSelection;
        item.OnItemBeginDrag -= HandleBeginDrag;
        item.OnItemDroppedOn -= HandleSwap;
        item.OnItemEndDrag -= HandleEndDrag;
        item.OnRightMouseBtnClick -= HandleShowItemActions;
    }
}
