using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [Header("Open/Close")]
    [SerializeField] private KeyCode toggleKey = KeyCode.I;
    [SerializeField] private GameObject inventoryOverlay;
    [SerializeField] private bool pauseGameWhileOpen = true;

    [Header("Tabs")]
    [SerializeField] private GameObject pokemonsPanel;
    [SerializeField] private GameObject itemsPanel;

    [Header("Pokemon List")]
    [SerializeField] private Transform pokemonListContent;
    [SerializeField] private TMP_Text pokemonDetailTitle;
    [SerializeField] private TMP_Text pokemonDetailBody;

    [Header("Items List")]
    [SerializeField] private Transform itemsListContent;
    [SerializeField] private TMP_Text itemDetailTitle;
    [SerializeField] private TMP_Text itemDetailBody;

    [Header("List Item Prefab")]
    [SerializeField] private InventoryListItemUI listItemPrefab;

    private bool isOpen;

    // Datos placeholder
    private List<PokemonEntry> pokemons;
    private List<ItemEntry> healingItems;
    private List<ItemEntry> battleItems;

    private enum ItemsMode { Healing, Battle }
    private ItemsMode currentItemsMode = ItemsMode.Healing;

    private void Awake()
    {
        // Estado inicial UI
        if (inventoryOverlay != null) inventoryOverlay.SetActive(false);
        isOpen = false;

        BuildDummyData();
        ShowPokemonsTab(); // deja la pestaña lista (aunque overlay esté cerrado)
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleInventory();
        }
    }

    public void ToggleInventory()
    {
        SetOpen(!isOpen);
    }

    public void CloseInventory()
    {
        SetOpen(false);
    }

    private void SetOpen(bool open)
    {
        isOpen = open;

        if (inventoryOverlay != null)
            inventoryOverlay.SetActive(isOpen);

        if (pauseGameWhileOpen)
            Time.timeScale = isOpen ? 0f : 1f;

        // Cuando se abre, refrescamos la pestaña actual
        if (isOpen)
        {
            if (pokemonsPanel.activeSelf) PopulatePokemonList();
            if (itemsPanel.activeSelf) PopulateItemsList();
        }
    }

    // --- TABS ---
    public void ShowPokemonsTab()
    {
        if (pokemonsPanel != null) pokemonsPanel.SetActive(true);
        if (itemsPanel != null) itemsPanel.SetActive(false);

        PopulatePokemonList();

        if (pokemonDetailTitle != null) pokemonDetailTitle.text = "Selecciona un pokemon";
        if (pokemonDetailBody != null) pokemonDetailBody.text = "Aquí aparecerán stats (placeholder).";
    }

    public void ShowItemsTab()
    {
        if (pokemonsPanel != null) pokemonsPanel.SetActive(false);
        if (itemsPanel != null) itemsPanel.SetActive(true);

        currentItemsMode = ItemsMode.Healing;
        PopulateItemsList();

        if (itemDetailTitle != null) itemDetailTitle.text = "Selecciona un objeto";
        if (itemDetailBody != null) itemDetailBody.text = "Aquí aparecerá su descripción (placeholder).";
    }

    // --- ITEMS FILTERS ---
    public void ShowHealingItems()
    {
        currentItemsMode = ItemsMode.Healing;
        PopulateItemsList();
    }

    public void ShowBattleItems()
    {
        currentItemsMode = ItemsMode.Battle;
        PopulateItemsList();
    }

    // --- POPULATE LISTS ---
    private void PopulatePokemonList()
    {
        if (pokemonListContent == null || listItemPrefab == null) return;

        ClearChildren(pokemonListContent);

        foreach (var p in pokemons)
        {
            var item = Instantiate(listItemPrefab, pokemonListContent);
            item.Setup($"{p.name}  Lv.{p.level}", () =>
            {
                if (pokemonDetailTitle != null) pokemonDetailTitle.text = p.name;
                if (pokemonDetailBody != null)
                {
                    pokemonDetailBody.text =
                        $"Nivel: {p.level}\n" +
                        $"HP: {p.hp}/{p.maxHp}\n" +
                        $"Tipo: {p.type}\n\n" +
                        $"(Placeholder)";
                }
            });
        }
    }

    private void PopulateItemsList()
    {
        if (itemsListContent == null || listItemPrefab == null) return;

        ClearChildren(itemsListContent);

        var list = (currentItemsMode == ItemsMode.Healing) ? healingItems : battleItems;

        foreach (var it in list)
        {
            var item = Instantiate(listItemPrefab, itemsListContent);
            item.Setup($"{it.name} x{it.qty}", () =>
            {
                if (itemDetailTitle != null) itemDetailTitle.text = it.name;
                if (itemDetailBody != null)
                {
                    itemDetailBody.text =
                        $"Cantidad: {it.qty}\n" +
                        $"Uso: {it.usage}\n\n" +
                        $"{it.description}";
                }
            });
        }
    }

    private void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }

    // --- DUMMY DATA ---
    private void BuildDummyData()
    {
        pokemons = new List<PokemonEntry>
        {
            new PokemonEntry("FuegoMon", 12, 30, 40, "Fuego"),
            new PokemonEntry("AguaMon", 9, 22, 30, "Agua"),
            new PokemonEntry("PlantaMon", 15, 45, 45, "Planta"),
        };

        healingItems = new List<ItemEntry>
        {
            new ItemEntry("Poción", 5, "Fuera o en combate", "Recupera 20 HP."),
            new ItemEntry("Súper Poción", 2, "Fuera o en combate", "Recupera 50 HP."),
            new ItemEntry("Antídoto", 3, "Fuera o en combate", "Cura veneno (placeholder)."),
        };

        battleItems = new List<ItemEntry>
        {
            new ItemEntry("Bomba de Humo", 1, "Solo en combate", "Aumenta evasión (placeholder)."),
            new ItemEntry("X-Ataque", 2, "Solo en combate", "Sube ataque por 3 turnos (placeholder)."),
        };
    }

    // --- DATA CLASSES ---
    private class PokemonEntry
    {
        public string name;
        public int level;
        public int hp;
        public int maxHp;
        public string type;

        public PokemonEntry(string name, int level, int hp, int maxHp, string type)
        {
            this.name = name;
            this.level = level;
            this.hp = hp;
            this.maxHp = maxHp;
            this.type = type;
        }
    }

    private class ItemEntry
    {
        public string name;
        public int qty;
        public string usage;
        public string description;

        public ItemEntry(string name, int qty, string usage, string description)
        {
            this.name = name;
            this.qty = qty;
            this.usage = usage;
            this.description = description;
        }
    }
}