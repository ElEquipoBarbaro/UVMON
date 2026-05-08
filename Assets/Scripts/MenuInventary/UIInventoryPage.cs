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

    public Sprite image;

    [SerializeField]
    public Sprite image2;
    public int quantity;

    public string title, description;

    private int currentlyDraggedItemIndex = -1;


    [SerializeField]
    private UIInventoryDescription itemDescription;

    private void Awake()
    {
        Hide();
        mouseFollower.Toggle(false);
        itemDescription.ResetDescription();
    }
    public void InitializeInventoryUI(int inventorysize)
        {
            for (int i = 0; i < inventorysize; i++)
{
    UIInventoryItem uiItem =
        Instantiate(itemPrefab, contentPanel);
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

      private void HandleShowItemActions(UIInventoryItem inventoryItemUI)
        {
           
        }

        private void HandleEndDrag(UIInventoryItem inventoryItemUI)
        {
           mouseFollower.Toggle(false);
        }

        private void HandleSwap(UIInventoryItem inventoryItemUI)
        {
             int index = listOfUIItems.IndexOf(inventoryItemUI);
            if (index == -1)
            {
                mouseFollower.Toggle(false);
                currentlyDraggedItemIndex = -1;
                return;
            }

            listOfUIItems[currentlyDraggedItemIndex].SetData(index == 0 ? image: image2, quantity);
            listOfUIItems[index].SetData(currentlyDraggedItemIndex == 0 ? image: image2, quantity);
            mouseFollower.Toggle(false);
            currentlyDraggedItemIndex = -1;
           
        }

         private void HandleBeginDrag(UIInventoryItem inventoryItemUI)
        {
            int index = listOfUIItems.IndexOf(inventoryItemUI);
            if (index == -1)
                return;
            currentlyDraggedItemIndex = index;
            mouseFollower.Toggle(true);
            mouseFollower.SetData(index == 0 ? image: image2, quantity);
        }
        private void HandleItemSelection(UIInventoryItem inventoryItemUI)
        {

            itemDescription.SetDescription(image, title, description);
            listOfUIItems[0].Select();

        }

    public void Show()
        {
            gameObject.SetActive(true);
            itemDescription.ResetDescription();
            listOfUIItems[0].SetData(image, quantity);
            listOfUIItems[1].SetData(image2, quantity);
          
        }

    public void Hide()
        {
          
            gameObject.SetActive(false);
        
        }
}
