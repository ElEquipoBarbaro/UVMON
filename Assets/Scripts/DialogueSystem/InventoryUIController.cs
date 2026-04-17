using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum InventoryTab
{
    UVmones,
    Curativos,
    Combate,
    Clave,
    Materiales,
    Varios
}

public class InventoryUIController : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject inventoryRoot;
    [SerializeField] private GameObject uvmonView;
    [SerializeField] private GameObject itemsView;

    [Header("Contenedores")]
    [SerializeField] private Transform uvmonContent;
    [SerializeField] private Transform itemContent;
    [SerializeField] private UVmonSlotUI uvmonSlotPrefab;
    [SerializeField] private InventorySlotUI itemSlotPrefab;

    [Header("Botones Tabs")]
    [SerializeField] private Button uvmonTabButton;
    [SerializeField] private Button healingTabButton;
    [SerializeField] private Button combatTabButton;
    [SerializeField] private Button keyItemTabButton;
    [SerializeField] private Button materialsTabButton;
    [SerializeField] private Button miscTabButton;
    [SerializeField] private Button closeButton;

    [Header("Panel de detalle")]
    [SerializeField] private Image detailIcon;
    [SerializeField] private TMP_Text detailName;
    [SerializeField] private TMP_Text detailCategory;
    [SerializeField] private TMP_Text detailDescription;
    [SerializeField] private TMP_Text detailExtra;
    [SerializeField] private Button useButton;
    [SerializeField] private Button discardButton;

    private InventoryTab currentTab = InventoryTab.UVmones;
    private InventoryItemStack selectedItem;
    private UVmonInstance selectedUVmon;

    private void Start()
    {
        inventoryRoot.SetActive(false);

        uvmonTabButton.onClick.AddListener(() => OpenTab(InventoryTab.UVmones));
        healingTabButton.onClick.AddListener(() => OpenTab(InventoryTab.Curativos));
        combatTabButton.onClick.AddListener(() => OpenTab(InventoryTab.Combate));
        keyItemTabButton.onClick.AddListener(() => OpenTab(InventoryTab.Clave));
        materialsTabButton.onClick.AddListener(() => OpenTab(InventoryTab.Materiales));
        miscTabButton.onClick.AddListener(() => OpenTab(InventoryTab.Varios));
        closeButton.onClick.AddListener(Close);

        useButton.onClick.AddListener(UseSelectedItem);
        discardButton.onClick.AddListener(DiscardSelectedItem);

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged += RefreshView;
        }

        ClearDetails();
    }

    private void OnDestroy()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= RefreshView;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            Toggle();
        }
    }

    public void Toggle()
    {
        if (inventoryRoot.activeSelf)
            Close();
        else
            Open();
    }

    public void Open()
    {
        inventoryRoot.SetActive(true);
        RefreshView();
    }

    public void Close()
    {
        inventoryRoot.SetActive(false);
    }

    private void OpenTab(InventoryTab tab)
    {
        currentTab = tab;
        selectedItem = null;
        selectedUVmon = null;
        RefreshView();
    }

    private void RefreshView()
    {
        if (InventoryManager.Instance == null) return;

        ClearChildren(itemContent);
        ClearChildren(uvmonContent);
        ClearDetails();

        bool showUVmones = currentTab == InventoryTab.UVmones;
        uvmonView.SetActive(showUVmones);
        itemsView.SetActive(!showUVmones);

        if (showUVmones)
        {
            PopulateUVmones();
        }
        else
        {
            PopulateItems();
        }
    }

    private void PopulateUVmones()
    {
        List<UVmonInstance> list = InventoryManager.Instance.UVmones;

        for (int i = 0; i < list.Count; i++)
        {
            UVmonSlotUI slot = Instantiate(uvmonSlotPrefab, uvmonContent);
            slot.Setup(list[i], SelectUVmon);
        }
    }

    private void PopulateItems()
    {
        ItemCategory category = ConvertTabToCategory(currentTab);
        List<InventoryItemStack> list = InventoryManager.Instance.GetItemsByCategory(category);

        for (int i = 0; i < list.Count; i++)
        {
            InventorySlotUI slot = Instantiate(itemSlotPrefab, itemContent);
            slot.Setup(list[i], SelectItem);
        }
    }

    private ItemCategory ConvertTabToCategory(InventoryTab tab)
    {
        switch (tab)
        {
            case InventoryTab.Curativos:
                return ItemCategory.Healing;
            case InventoryTab.Combate:
                return ItemCategory.Combat;
            case InventoryTab.Clave:
                return ItemCategory.KeyItem;
            case InventoryTab.Materiales:
                return ItemCategory.Material;
            default:
                return ItemCategory.Misc;
        }
    }

    private void SelectItem(InventoryItemStack stack)
    {
        selectedItem = stack;
        selectedUVmon = null;

        if (stack == null || stack.data == null) return;

        detailIcon.enabled = true;
        detailIcon.sprite = stack.data.icon;
        detailName.text = stack.data.itemName;
        detailCategory.text = stack.data.category.ToString();
        detailDescription.text = stack.data.description;
        detailExtra.text = "Cantidad: x" + stack.amount + "\nRareza: " + stack.data.rarity;

        useButton.interactable = true;
        discardButton.interactable = true;
    }

    private void SelectUVmon(UVmonInstance uvmon)
    {
        selectedUVmon = uvmon;
        selectedItem = null;

        if (uvmon == null || uvmon.data == null) return;

        detailIcon.enabled = true;
        detailIcon.sprite = uvmon.data.portrait;
        detailName.text = uvmon.data.uvmonName;
        detailCategory.text = "UVmon / " + uvmon.data.elementalType;
        detailDescription.text = "Compañero listo para combate.";
        detailExtra.text =
            "Nivel: " + uvmon.level +
            "\nHP: " + uvmon.currentHP + "/" + uvmon.MaxHP +
            "\nATQ Base: " + uvmon.data.baseAttack +
            "\nDEF Base: " + uvmon.data.baseDefense;

        useButton.interactable = false;
        discardButton.interactable = false;
    }

    private void UseSelectedItem()
    {
        if (selectedItem == null) return;

        InventoryManager.Instance.UseItem(selectedItem);
        RefreshView();
    }

    private void DiscardSelectedItem()
    {
        if (selectedItem == null) return;

        InventoryManager.Instance.RemoveItem(selectedItem.data, 1);
        RefreshView();
    }

    private void ClearDetails()
    {
        detailIcon.enabled = false;
        detailIcon.sprite = null;
        detailName.text = "Selecciona algo";
        detailCategory.text = "";
        detailDescription.text = "Aquí aparecerá la información del objeto o UVmon.";
        detailExtra.text = "";

        useButton.interactable = false;
        discardButton.interactable = false;
    }

    private void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Destroy(parent.GetChild(i).gameObject);
        }
    }
}
