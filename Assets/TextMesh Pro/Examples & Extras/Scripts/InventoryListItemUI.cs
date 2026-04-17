using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class InventoryListItemUI : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    public void Setup(string text, Action onClick)
    {
        if (label != null) label.text = text;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke());
        }
    }
}