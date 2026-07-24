# Tab / pagination system between Inventory and Pokemon panels

How the "pagination" (tab switching) between the **Items** view and the **Pokemon** view
inside the in-game menu works, and the sizing bug that was fixed on `PokemonPanel`.

## 1. Hierarchy (scene `jardinconocimiento`)

```
OverworldUI                                    [Canvas]
└── InGameMenu                                 sibling 1
      HorizontalLayoutGroup: padding 50/50/50/50, ChildControlWidth/Height=1, ForceExpand W/H=1
      ├── TabBar                               ← the two tab buttons
      │     VerticalLayoutGroup + LayoutElement (fixed preferred width, doesn't stretch)
      │     ├── ItemsTabButton
      │     └── PokemonTabButton
      ├── Inventory                            [Inventory.prefab instance]
      │     UIInventoryPage  (Show()/Hide() → gameObject.SetActive)
      └── PokemonPanel
            PokemonTabController  (Show()/Hide() → gameObject.SetActive)
            ├── PokemonContent    (grid of PokemonSlotUI, spawned at runtime)
            └── PokemonDetails    (PokemonDetailsPanel)
```

`InventoryController` (on the `Player` prefab instance) is the glue: it holds references to
`inventoryUI` (`UIInventoryPage`), `pokemonTab` (`PokemonTabController`), `itemsTabButton` and
`pokemonTabButton`.

## 2. How the switching works — no real "pages", just Show/Hide

There's no `PageView`/`Pagination` component. It's the simplest thing that works: **only one of
the two panels is active at a time**, toggled by two buttons.

`InventoryController.cs`:

```csharp
private void PrepareUI()
{
    ...
    itemsTabButton.onClick.AddListener(ShowItemsTab);
    pokemonTabButton.onClick.AddListener(ShowPokemonTab);
}

private void ShowItemsTab()
{
    pokemonTab.Hide();
    inventoryUI.Show();
    UpdateInventoryUI(inventoryData.GetCurrentInventoryState());
}

private void ShowPokemonTab()
{
    inventoryUI.Hide();
    pokemonTab.Show();
}
```

Both `UIInventoryPage.Show()/Hide()` and `PokemonTabController.Show()/Hide()` just do
`gameObject.SetActive(true/false)` (plus their own refresh/reset logic — `PokemonTabController.Show()`
also calls `Refresh()` to rebuild the party slots, `Hide()` cancels any pending target selection).

Opening the whole menu (`I` key) always lands on the Items tab first (`OpenMenu()` → `ShowItemsTab()`).

## 3. Why this makes the "same size" requirement free

`InGameMenu`'s `HorizontalLayoutGroup` has `ChildControlWidth/Height = 1` and
`ForceExpand Width/Height = 1`. Unity's layout groups **skip inactive children** when computing
layout. So:

- When the Items tab is showing: only `TabBar` (fixed width) + `Inventory` are active →
  `Inventory` gets 100% of the remaining width/height.
- When the Pokemon tab is showing: only `TabBar` (fixed width) + `PokemonPanel` are active →
  `PokemonPanel` gets 100% of the remaining width/height.

Since both panels sit in the exact same slot of the same layout group, they automatically end up
the same size — **as long as neither has a stray `localScale` fighting the layout.**

## 4. The actual bug that made `PokemonPanel` look tiny

`HorizontalLayoutGroup` only ever writes to `RectTransform.sizeDelta`/`anchoredPosition` — it never
touches `localScale`. `PokemonPanel` (and `TabBar`) had `m_LocalScale = {0.2083333, 0.2083333, 0.2083333}`
(≈ 1/4.8) left over in the scene file, while `Inventory.prefab`'s root scale is `{1, 1, 1}`. The
layout group was sizing `PokemonPanel`'s rect correctly, but the whole panel then rendered — and hit-tested,
and showed its Scene-view gizmo handles — at ~20% scale, which is why it looked tiny **and** was painful
to select/drag in the Editor.

Fix: reset both `TabBar` and `PokemonPanel`'s `RectTransform.localScale` to `(1, 1, 1)` in the Unity
Editor (via MCP `manage_gameobject`, then `manage_scene` → `save`), matching `Inventory.prefab`. No
prefab, layout, or script changes were needed — it was purely a stray transform value.

*(There's a third, unrelated GameObject — `ItemContextMenu`, the right-click item menu, a sibling of
`InGameMenu` under `OverworldUI` — that still has the same `0.2083333` scale. It's a separate popup,
not part of the Inventory/Pokemon tab pair, so it was left untouched. If it also looks too small, apply
the same fix to it.)*

## 5. Adding a third tab, if you ever need one

1. Give the new panel a controller with `Show()`/`Hide()` methods (`SetActive` + whatever refresh
   logic it needs), same shape as `PokemonTabController`.
2. Add it as a sibling of `Inventory`/`PokemonPanel` under `InGameMenu` (so it shares the same
   `HorizontalLayoutGroup` slot and inherits the "same size" behavior for free). Make sure its root
   `localScale` is `(1, 1, 1)`.
3. Add a button under `TabBar`.
4. In `InventoryController`, add a `ShowXTab()` method that hides the other panels and shows the new
   one, and wire the button's `onClick` to it in `PrepareUI()`.
