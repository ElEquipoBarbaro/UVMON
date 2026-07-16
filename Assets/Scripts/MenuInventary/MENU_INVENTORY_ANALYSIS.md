# Menu Inventory System Analysis — `Assets/Scripts/MenuInventary`

Analysis of the inventory scripts, the data model, and the actual UI hierarchy/layout as authored in `Assets/Prefabs/Inventory.prefab`, `Assets/Prefabs/ItemUI.prefab`, and the `MouseFollower` object in `jardinconocimiento.unity`.

## 1. High-level architecture

```
InventoryController (MonoBehaviour, scene)
   │  Update(): 'I' key toggles Show()/Hide()
   │
   ├─ InventorySO (ScriptableObject, the data model)
   │     event OnInventoryUpdated ──────────────► UpdateInventoryUI(...)
   │
   └─ UIInventoryPage (MonoBehaviour, "Inventory" prefab root)
          │
          ├─ N × UIInventoryItem  (grid slots, instantiated at runtime)
          │      IPointerClickHandler / IBeginDragHandler / IDragHandler /
          │      IEndDragHandler / IDropHandler
          │
          ├─ UIInventoryDescription (bottom info panel: icon + name + text)
          │
          └─ MouseFollower (sibling under OverworldUI, NOT a child of Inventory)
                 - Update(): follows the cursor every frame
                 - hosts one extra UIInventoryItem instance used purely as
                   the dragged-item "ghost" visual
```

Data model: `ItemSO` (per-item asset: name/description/sprite/stackable/max stack) + `InventorySO` (ScriptableObject holding `List<InventoryItem>` where `InventoryItem` is a `{ItemSO item; int quantity}` struct, plus add/stack/swap logic and an `OnInventoryUpdated` event).

## 2. File-by-file

| File | Role |
|---|---|
| `InventoryController.cs` | Scene-level glue. Calls `inventoryUI.InitializeInventoryUI(inventoryData.Size)` once at `Start`, subscribes to `UIInventoryPage`'s events (`OnDescriptionRequested`, `OnSwapItems`, `OnStartDragging`, `OnItemActionRequested`) and to `InventorySO.OnInventoryUpdated`. `Update()` toggles the panel with the `I` key and re-pushes the full inventory state into the UI on open. |
| `Model/InventorySO.cs` | ScriptableObject holding the slot array (`List<InventoryItem>`, size fixed at `Size`, default 10). `AddItem` handles stackable vs non-stackable items, `SwapItems` swaps two slots, `GetCurrentInventoryState()` returns only non-empty slots as a `Dictionary<int, InventoryItem>`. Fires `OnInventoryUpdated` after every mutation. |
| `Model/ItemSO.cs` | ScriptableObject: `IsStackable`, `MaxStackSize`, `Name`, `Description`, `ItemImage`, and an `ID` derived from `GetInstanceID()`. |
| `UIInventoryPage.cs` | The page controller. Instantiates `itemPrefab` (`ItemUI.prefab`) `Size` times into `contentPanel`, wires each slot's click/drag/drop events to its own handlers, tracks `currentlyDraggedItemIndex`, and drives `mouseFollower` (show/hide the ghost, feed it the dragged sprite/quantity). Re-raises `OnDescriptionRequested`, `OnSwapItems`, `OnStartDragging` up to `InventoryController`. |
| `UIInventoryItem.cs` | Per-slot component. Pure event relay — implements `IPointerClickHandler/IBeginDragHandler/IDragHandler/IEndDragHandler/IDropHandler` and just re-broadcasts C# events (`OnItemClicked`, `OnItemBeginDrag`, `OnItemDroppedOn`, `OnItemEndDrag`, `OnRightMouseBtnClick`) for `UIInventoryPage` to interpret. Holds `itemImage`, `quantityTxt`, `borderImage` (selection highlight). |
| `UIInventoryDescription.cs` | Bottom description panel — just `SetDescription(sprite, name, description)` / `ResetDescription()` on its own `itemImage`/`title`/`description` refs. No logic. |
| `MouseFollower.cs` | Follows the mouse in `Update()` via `RectTransformUtility.ScreenPointToLocalPointInRectangle`, and hosts a child `UIInventoryItem` used only as the drag-ghost visual (`SetData`/`Toggle`). *(A `CanvasGroup.blocksRaycasts = false` guard was added here in the previous fix so this ghost can no longer steal `OnDrop` from the real slot underneath it — see §5.)* |

## 3. Runtime flow

1. `InventoryController.Start()` → `inventoryUI.InitializeInventoryUI(inventoryData.Size)`: instantiates one `UIInventoryItem` per inventory slot under `contentPanel`, subscribes each slot's events.
2. Player presses `I` → `InventoryController.Update()` calls `inventoryUI.Show()` and pushes every non-empty `InventoryItem` from `InventorySO.GetCurrentInventoryState()` into the matching slot via `UpdateData(index, sprite, quantity)`.
3. **Select/describe**: click a slot → `UIInventoryItem.OnPointerClick` → `UIInventoryPage.HandleItemSelection` → `OnDescriptionRequested` → `InventoryController.HandleDescriptionRequest` → reads `InventorySO.GetItemAt(index)` → `UIInventoryPage.UpdateDescription(...)` → `UIInventoryDescription.SetDescription(...)`.
4. **Drag start**: `OnBeginDrag` on a non-empty slot → `UIInventoryPage.HandleBeginDrag` records `currentlyDraggedItemIndex`, fires `OnStartDragging` → `InventoryController.HandleDragging` reads the item from `InventorySO` and calls `inventoryUI.CreateDraggedItem(sprite, qty)` → `mouseFollower.Toggle(true)` + `SetData(...)`. From here `MouseFollower.Update()` keeps the ghost pinned to the cursor.
5. **Drop**: `OnDrop` on the target slot → `UIInventoryPage.HandleSwap` resolves the target's index and fires `OnSwapItems(currentlyDraggedItemIndex, targetIndex)` → `InventoryController.HandleSwapItems` → `InventorySO.SwapItems(...)` → `OnInventoryUpdated` → `InventoryController.UpdateInventoryUI` repaints every slot.
6. **Drag end**: `OnEndDrag` (fires on every slot, hit or miss) → `UIInventoryPage.HandleEndDrag` → `ResetDraggedItem()` hides the ghost and resets `currentlyDraggedItemIndex = -1`.

## 4. Hierarchy and distribution (as authored in the assets)

```
OverworldUI                                  [Canvas, ScreenSpaceOverlay, CanvasScaler 800x600 "Scale With Screen Size", GraphicRaycaster]
├── DialogBox                                (unrelated: dialogue system, sibling index 0)
├── InGameMenu                                sibling index 1
│     RectTransform: anchors (0,0)-(1,1) — stretches to fill the whole screen
│     HorizontalLayoutGroup: padding 50/50/50/50, childAlignment=UpperLeft, ForceExpand W/H
│     Image: white, alpha 0.392, disabled (m_Enabled: 0) — dimmed background, currently off
│     └── Inventory                          [Inventory.prefab instance — the only child]
│           RectTransform: driven by InGameMenu's HorizontalLayoutGroup (fills the padded area)
│           Image: alpha 0.392 · HorizontalLayoutGroup(pad 15, spacing 10, ForceExpand W/H)
│           UIInventoryPage  (itemPrefab=ItemUI, contentPanel→Content, itemDescription→InventoryDescription)
│           ├── InventoryContent              sibling 0
│           │     Image (black, alpha 1) · LayoutElement(minWidth 430)
│           │     └── Scroll View
│           │           Image (alpha 0.392) · ScrollRect(vertical only; content=Content, viewport=Viewport, verticalScrollbar=Scrollbar Vertical)
│           │           ├── Viewport
│           │           │     Image · Mask(ShowMaskGraphic=false)
│           │           │     HorizontalLayoutGroup(padding top 10, ForceExpand W/H)  ⚠ see §5
│           │           │     └── Content                                 ← `contentPanel`
│           │           │           GridLayoutGroup(padding 15/10/20/10, cellSize 200×200,
│           │           │                            spacing 10×10, constraint=Flexible)
│           │           │           └── (runtime) 10 × ItemUI              ← one per InventorySO.Size
│           │           └── Scrollbar Vertical  (anchored to the right edge, width 20)
│           │                 └── Sliding Area
│           │                       └── Handle
│           └── InventoryDescription           sibling 1
│                 Image (transparent) · VerticalLayoutGroup(ChildAlignment=MiddleCenter, ForceExpand W/H)
│                 UIInventoryDescription  (itemImage→Image below, title→TitleTxt, description→DescriptionTxt)
│                 ├── ImagePanel                                          ← `itemImage`'s container
│                 │     Image (red-tinted debug placeholder, disabled) · LayoutElement(minHeight 120)
│                 │     └── ImageBorder
│                 │           Image (dark gray 0.125) · HorizontalLayoutGroup(pad 10, ForceExpand W/H)
│                 │           └── Image                                    ← bound to `itemImage`
│                 └── DescriptionPanel
│                       Image (light gray) · LayoutElement(flexibleHeight 300)
│                       VerticalLayoutGroup(pad 10/10/5/5, ChildAlignment=UpperCenter)
│                       ├── TitleTxt        (TMP, bold, size 30)           ← bound to `title`
│                       └── DescriptionTxt  (TMP, size 20, wraps/truncates) ← bound to `description`
└── MouseFollower                             sibling index 2 (drawn LAST → always on top)
      RectTransform: 100×100, centered on itself; ContentSizeFitter (Horizontal/Vertical "Preferred")
      └── ItemUI                              (a *second*, independent instance of ItemUI.prefab — the drag "ghost")
            anchored top-left inset in MouseFollower's rect, size 110×110
```

### `ItemUI.prefab` (the slot itself, instantiated both for the grid and for the ghost)

```
ItemUI                              [Image: RaycastTarget=1 → the slot's clickable/droppable surface]
                                     UIInventoryItem (itemImage, quantityTxt, borderImage)
├── Border          Image, RaycastTarget=0   (selection highlight frame → `borderImage`)
├── Text (TMP)      RaycastTarget=1          (stack quantity label → `quantityTxt`) — see §5
├── Image           Image, RaycastTarget=0   (item icon → `itemImage`)
└── TxtBackground   Image, RaycastTarget=0   (small backing plate behind the quantity label)
```

`GridLayoutGroup` on `Content` lays these out in a flexible-wrap grid (200×200 cells, 10px spacing); with the default `InventorySO.Size = 10` and a ~430px-wide content column that's roughly a 2-wide grid (matches the `m_ConstraintCount: 2` set on the group, even though `Constraint` is `Flexible` — see §5).

## 5. Notable findings

### `Inventory.prefab`'s `UIInventoryPage` component looks out of sync with the current script — check the Inspector
Reading the prefab's raw serialized `UIInventoryPage` block, its fields are:
```
itemPrefab, contentPanel, image, quantity: 10, title: "Soda", description: "Sirve para tomar", itemDescription
```
`UIInventoryPage.cs` today only declares `[SerializeField]` for `itemPrefab`, `contentPanel`, `mouseFollower`, `itemDescription` — there is **no** `image`/`quantity`/`title`/`description` field in the current script, and (more importantly) **no `mouseFollower:` entry at all appears in the prefab's serialized data.** This is the signature of a prefab that hasn't been re-saved in the Unity Editor since `UIInventoryPage.cs` was refactored (the `image/quantity/title/description` fields read like leftovers from an older "seed one test item" version of the script, before `mouseFollower` existed).

**Please open `Inventory.prefab` in the Editor and check the `UIInventoryPage` component's `Mouse Follower` slot.** If it shows `None`, every call that dereferences it (`Awake()`'s `Hide()` → `ResetDraggedItem()` → `mouseFollower.Toggle(false)`, and later `CreateDraggedItem`) will throw a `NullReferenceException` the moment the panel initializes. If it's actually assigned in the Editor (Unity may have silently migrated/kept the reference through a GUID-based upgrade not reflected as a clean diff), then this is just stale/orphaned serialized data and harmless — but it's worth a save-and-diff to confirm and clean up either way.

### Unexpected `HorizontalLayoutGroup` on `Viewport`
`Viewport` (inside `Scroll View`) has its own `HorizontalLayoutGroup` with `ChildControlHeight=1` and `ChildForceExpandHeight=1`. Since `Content` is `Viewport`'s only child, this layout group will force `Content`'s height to always exactly match the viewport's visible height on every layout pass — which fights with the whole point of a scrollable `Content` (its height should grow with the number of rows of items, driven by `GridLayoutGroup`, so there's something to scroll). Depending on Unity's layout-rebuild order this may or may not currently be visibly breaking scrolling, but it's an unusual/likely-accidental component to have on a `Viewport` and is worth removing if the inventory grid ever grows past what fits on screen.

### `GridLayoutGroup.Constraint` is `Flexible` but `ConstraintCount: 2` is also set
`Constraint = 0` (Flexible) makes Unity auto-wrap columns based on available width — `ConstraintCount` is only read when `Constraint` is `Fixed Column Count` or `Fixed Row Count`. The `2` currently set is vestigial/ignored; the actual column count in practice is whatever `430px content width ÷ (200px cell + 10px spacing)` works out to. Not a bug, just dead configuration that could mislead someone reading the Inspector into thinking columns are pinned at 2.

### Quantity text (`Text (TMP)`) has `RaycastTarget = true`
The other decorative children of `ItemUI` (`Border`, `Image`, `TxtBackground`) all have `RaycastTarget = 0`; only the root's `Image` should need it (it's the thing `UIInventoryItem`'s handlers effectively hang off). The quantity label's raycast target being `true` doesn't break anything today (any hit still bubbles up to the same root `UIInventoryItem`), but it's an unnecessary raycast cost repeated across every slot and inconsistent with how the other child graphics were configured.

### `InventorySO.AddItem`'s non-stackable branch loops over `i` but never uses it
```csharp
if(item.IsStackable == false)
{
    for (int i = 0; i < inventoryItems.Count; i++)
    {
        while(quantity > 0 && IsInventoryFull() == false)
            quantity -= AddItemToFirstFreeSlot(item, 1);
        InformAboutChange();
        return quantity;              // <- always hit on the very first i=0 iteration
    }
}
```
The unconditional `return` inside the loop body means the `for` never actually iterates past `i = 0` — it behaves exactly as if the `for` weren't there. Functionally harmless (the `while` loop already does the real work of filling one free slot at a time until `quantity` is exhausted or the inventory is full), but it reads as leftover/confusing scaffolding from an earlier version of the method.

### Design notes (not bugs)
- `MouseFollower` deliberately lives as a **sibling of `InGameMenu`** under `OverworldUI`, not inside `Inventory` — this is why it needs its own `Canvas`/raycast handling rather than inheriting `Inventory`'s; being last in sibling order is also what makes it draw on top of the inventory grid.
- `UIInventoryItem` itself has no inventory rules — it's a dumb relay. All actual state changes happen in `InventorySO` (data) and are orchestrated by `UIInventoryPage`/`InventoryController` (UI/event glue), which is a clean separation.
- `ItemSO.ID` uses `GetInstanceID()`, which is stable for the lifetime of one Editor/Player session but is **not stable across sessions or builds** — fine for runtime stack-matching (`AddStackableItem` compares `.item.ID`), but should never be persisted to a save file as an item identifier.
