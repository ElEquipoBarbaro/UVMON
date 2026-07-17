# Curative Item Usage — Implementation

How a healing item (Soda, Shuko, Purina...) is defined, selected, and consumed to
heal a creature. Covers `Assets/Scripts/MenuInventary/Model/*`,
`ItemContextMenu.cs`, `UIInventoryPage.cs`, `UIInventoryItem.cs`, and
`InventoryController.cs`, plus the item/effect assets in
`Assets/ScriptableObjects/Items/`.

## 1. Data model

```
ItemSO (ScriptableObject, one per item)
   Name, Description, ItemImage, IsStackable, MaxStackSize
   Category : ItemCategory        { Healing, Attack, Defense }
   Effect   : ItemEffect          (nullable)

ItemEffect (abstract ScriptableObject)
   abstract void Apply(CreatureRuntime target)

HealItemEffect : ItemEffect       (the only ItemEffect implementation today)
   fullHeal : bool
   healAmount : int
   Apply(target) => target.Heal(fullHeal ? target.MaxHP : healAmount)
```

`ItemEffect` is deliberately abstract/`ScriptableObject`-based so future effect
types (revive, cure-status, stat-boost...) can be added as new assets/subclasses
without touching `InventoryController` — the same way `Combat/MoveEffect.cs` does
it for battle moves. `HealItemEffect.Apply` calls straight into
`CreatureRuntime.Heal(amount)` (`Assets/Scripts/Combat/CreatureRuntime.cs`), which
clamps to `MaxHP`.

### Authored item assets (`Assets/ScriptableObjects/Items/`)

| Item | Stackable / max | Category | Effect asset | Effect |
|---|---|---|---|---|
| `Soda.asset` | yes / 15 | Healing | `Effects/Heal10.asset` | heal 10 |
| `Shuko.asset` | yes / 5 | Healing | `Effects/Heal20.asset` | heal 20 |
| `Purina.asset` | no / 1 | Healing | `Effects/HealFull.asset` | full heal |
| `HotDog.asset` | yes / 10 | Healing (default) | *(none)* | no-op — see §5 |

`ItemCategory.Healing == 0`, which is also the C# default for an unset enum field,
so any item asset that never explicitly set `Category` in the Inspector silently
reads as `Healing` (this is how `HotDog` ended up categorized `Healing` despite
having no `Effect` assigned).

## 2. UI flow: right-click → Use

```
UIInventoryItem.OnPointerClick (right button)
        │  OnRightMouseBtnClick(this)
        ▼
UIInventoryPage.HandleShowItemActions(itemUI)
        │  contextMenuTargetIndex = slot index
        │  itemContextMenu.Show(itemUI.transform.position)
        ▼
ItemContextMenu (small floating panel, one "Use" button)
        │  user clicks Use → OnUseClicked
        ▼
UIInventoryPage.HandleUseItem()
        │  hides the context menu
        │  OnItemActionRequested?.Invoke(contextMenuTargetIndex)
        ▼
InventoryController.HandleItemActionRequest(itemIndex)
```

- `UIInventoryItem.OnPointerClick` (`UIInventoryItem.cs`) branches on
  `pointerData.button`: left click still fires the existing `OnItemClicked`
  (description panel), right click fires the new `OnRightMouseBtnClick` instead —
  the two are mutually exclusive per click.
- `ItemContextMenu` (`ItemContextMenu.cs`) is intentionally dumb: it just
  shows/hides itself at a world position and exposes one `Action OnUseClicked`
  event wired to a single `Button`. It has no idea *which* slot it's showing for —
  `UIInventoryPage` tracks that separately in `contextMenuTargetIndex`.
- `UIInventoryPage.Awake()` wires `itemContextMenu.OnUseClicked += HandleUseItem`
  once, and `HandleItemSelection` (left click) / `Hide()` both call
  `itemContextMenu.Hide()` so the menu can't linger open over a stale slot after
  the player clicks elsewhere or closes the inventory.

## 3. Resolving the target and applying the effect

`InventoryController.HandleItemActionRequest(itemIndex)`:

1. Reads the slot via `inventoryData.GetItemAt(itemIndex)`. Bails out (no-op) if
   the slot is empty, `Category != ItemCategory.Healing`, `Effect == null`, or
   `PlayerParty.Instance` doesn't exist / has an empty party.
2. **Single creature in the party** → heals it immediately:
   `UseHealingItem(itemIndex, PlayerParty.Instance.Party[0])`.
3. **Multiple creatures** → does *not* guess a target. Instead it:
   - Switches the menu to the Pokemon tab: `ShowPokemonTab()`.
   - Arms one-shot target selection: `pokemonTab.BeginTargetSelection(target =>
     UseHealingItem(itemIndex, target))`.
   - The actual target resolution happens later, inside
     `PokemonTabController.HandleSlotClicked` — see
     [PokemonSelectionSystem.md](PokemonSelectionSystem.md) §3.
4. `UseHealingItem(itemIndex, target)` — the common endpoint reached either
   immediately (step 2) or via the callback (step 3):
   ```csharp
   inventoryItem.item.Effect.Apply(target);   // e.g. HealItemEffect → target.Heal(...)
   inventoryData.RemoveItem(itemIndex, 1);    // consumes exactly one unit of the stack
   pokemonTab.Refresh();                      // repaints HP text on the pokemon list
   ```
   `InventorySO.RemoveItem` (`Model/InventorySO.cs`) decrements the stack's
   `quantity`, or clears the slot entirely (`InventoryItem.GetEmptyItem()`) once
   it hits zero, then fires `OnInventoryUpdated` — which is how the Items tab grid
   repaints itself with the reduced/removed stack without `InventoryController`
   touching the UI directly.

Note `pokemonTab.Refresh()` is called unconditionally at the end of
`UseHealingItem`, even when the Items tab (not the Pokemon tab) is what's
currently visible — cheap since `Refresh()` just rebuilds an inactive/invisible
list, and it guarantees the HP text is already correct the next time the player
switches tabs.

## 4. Sequence — full example (party of 3, use Shuko)

```
Player right-clicks the Shuko slot
  → ItemContextMenu shows over that slot
Player clicks "Use"
  → InventoryController.HandleItemActionRequest(shukoIndex)
      Category == Healing, Effect == Heal20, Party.Count == 3
      → ShowPokemonTab()                      // tab switches, list repaints
      → pokemonTab.BeginTargetSelection(cb)    // armed, no creature picked yet
Player clicks creature slot #2 in the now-visible Pokemon tab
  → PokemonSlotUI.OnPointerClick → PokemonTabController.HandleSlotClicked
      targetSelectionCallback != null → consumes it, calls cb(creature #2)
      → InventoryController.UseHealingItem(shukoIndex, creature #2)
          Heal20.Apply(creature #2)            // CreatureRuntime.Heal(20)
          inventoryData.RemoveItem(shukoIndex, 1)
          pokemonTab.Refresh()                 // creature #2's HP text updates
```

If the player instead closes the menu (`I` key) or clicks a slot with *no* pending
target selection, `PokemonTabController.Hide()` calls `CancelTargetSelection()`,
clearing `targetSelectionCallback` — the armed "use item" request is silently
dropped rather than firing on some later unrelated click.

## 5. Scene wiring (`jardinconocimiento.unity`)

- `InventoryController` GameObject (`fileID 1108876078`, part of the `Inventory`
  prefab instance) has `pokemonTab` wired to the `PokemonTabController` instance
  (`fileID 1476213322`), plus `itemsTabButton` / `pokemonTabButton` for the two-tab
  toggle. Its `initialItems` list (what the player starts the scene holding) is:
  1× HotDog, 2× Soda, 3× Soda *(two separate list entries that both target the
  Soda asset — they simply stack together in `InventorySO.AddItem`, resulting in a
  single 5-Soda slot)*, 10× Shuko, 2× Purina.
- `UIInventoryPage`'s `itemContextMenu` field points at a `ItemContextMenu`
  component (`fileID 1620657215`) whose `useButton` is wired to a child `Button`
  (`fileID 873690363`) — this is the floating "Use" panel described in §2.
- `PlayerParty` does **not** appear anywhere in this scene file — it's a
  `DontDestroyOnLoad`-style singleton (`PlayerParty.Awake()` self-destroys any
  duplicate) that must already exist from an earlier-loaded scene by the time
  `HandleItemActionRequest` runs. If the inventory menu is opened in a scene
  loaded standalone (e.g. in the Editor via "Play from this scene") without first
  passing through whatever scene creates `PlayerParty`, `PlayerParty.Instance`
  will be `null` and every "Use" click on a healing item silently no-ops (step 1
  of §3).

## 6. Gaps / things not implemented

- **No item use during battle.** `CombatManager`'s turn loop (`Assets/Scripts/Combat/CombatManager.cs`)
  only reads Up/Down/Enter to pick a *move*; there is no input path that reaches
  `ItemEffect.Apply` while `CombatManager.Instance` is mid-`BattleLoop()`. Healing
  is strictly a between-fights, overworld-menu action.
- **`HotDog` has `Category == Healing` but no `Effect`.** Right-click → Use on it
  reaches `HandleItemActionRequest`, sees `Effect == null`, and returns — the
  context menu closes and nothing else happens (no message is shown to the
  player explaining why). Whether this is deliberate ("junk food" flavor item) or
  an authoring oversight (forgot to set `Category = Attack`/leave `Effect` unset
  intentionally with a different category) isn't determinable from the assets
  alone.
- **Only `Healing` category items are usable at all.** `ItemCategory.Attack` and
  `ItemCategory.Defense` exist as enum values but `HandleItemActionRequest`
  hard-codes `!= ItemCategory.Healing` as an early return — there's no code path
  that does anything with an Attack/Defense-categorized item yet.
- **No quantity/confirmation prompt.** Clicking "Use" immediately consumes one
  unit and applies the effect — there's no "are you sure" or "select amount" step,
  and no player-facing feedback message (e.g. a "+20 HP" toast) beyond the HP
  number changing on the Pokemon tab.
