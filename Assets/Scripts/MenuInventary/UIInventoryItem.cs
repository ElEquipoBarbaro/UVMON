using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIInventoryItem : MonoBehaviour, IPointerClickHandler,
        IBeginDragHandler, IEndDragHandler, IDropHandler, IDragHandler
{
    [SerializeField]
    private Image itemImage;
    [SerializeField]
    private TMP_Text quantityTxt;
    [SerializeField]
    private Image borderImage;

    public event Action<UIInventoryItem> OnItemClicked,
        OnItemDroppedOn, OnItemBeginDrag, OnItemEndDrag,
        OnRightMouseBtnClick;

    private bool empty = true;

    public void Awake()
    {
        ResetData();
        Deselect();
    }

public void ResetData()
    {
        empty = true;

        if (itemImage != null)
        {
            itemImage.sprite = null;
            itemImage.gameObject.SetActive(false);
        }

        if (quantityTxt != null)
            quantityTxt.text = string.Empty;
    }

public void Deselect()
    {
        if (borderImage != null)
            borderImage.enabled = false;
    }

public void SetData(Sprite sprite, int quantity)
    {
        if (itemImage == null)
            return;

        itemImage.gameObject.SetActive(true);
        itemImage.sprite = sprite;

        if (quantityTxt != null)
            quantityTxt.text = quantity.ToString();

        empty = false;
    }

public void Select()
    {
        if (borderImage != null)
            borderImage.enabled = true;
    }

    // ✅ IBeginDragHandler
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (empty) return;
        OnItemBeginDrag?.Invoke(this);
    }

    // ✅ IDropHandler
    public void OnDrop(PointerEventData eventData)
    {
        OnItemDroppedOn?.Invoke(this);
    }

    // ✅ IEndDragHandler
    public void OnEndDrag(PointerEventData eventData)
    {
        OnItemEndDrag?.Invoke(this);
    }

    // ✅ IDragHandler (obligatorio si usas IBeginDrag/IEndDrag)
    public void OnDrag(PointerEventData eventData)
    {
        // necesario para que el drag funcione
    }

    // ✅ IPointerClickHandler
    public void OnPointerClick(PointerEventData pointerData)
    {
        

        if (pointerData.button == PointerEventData.InputButton.Right)
        {
            OnRightMouseBtnClick?.Invoke(this);
        }
        else
        {
            OnItemClicked?.Invoke(this);
        }
    }
}