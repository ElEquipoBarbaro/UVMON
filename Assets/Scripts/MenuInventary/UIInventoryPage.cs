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

    List<UIInventoryItem> listOfUIItems = new List<UIInventoryItem>();

    public Sprite image;
    public int quantity;

    public string title, description;


    [SerializeField]
    private UIInventoryDescription itemDescription;

    private void Awake()
    {
        Hide();

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
           
        }

        private void HandleSwap(UIInventoryItem inventoryItemUI)
        {
           
        }

         private void HandleBeginDrag(UIInventoryItem inventoryItemUI)
        {
         
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
          
        }

    public void Hide()
        {
          
            gameObject.SetActive(false);
        
        }
}
