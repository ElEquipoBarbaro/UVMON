# Pokémon Tab & Target Selection — Implementation

How the in-game menu's **Pokemon** tab lists the player's party, shows a details
panel, and doubles as the target picker consumed by
[ItemUsageSystem.md](ItemUsageSystem.md). Covers
`Assets/Scripts/MenuInventary/PokemonTabController.cs`,
`PokemonSlotUI.cs`, `PokemonDetailsPanel.cs`, and their glue in
`InventoryController.cs`, plus the underlying party data in
`Assets/Scripts/Combat/PlayerParty.cs` / `CreatureRuntime.cs`.

## 1. High-level architecture

```
InventoryController                          [Items / Pokemon tab toggle buttons]
   ├─ ShowItemsTab()   → inventoryUI.Show(),  pokemonTab.Hide()
   └─ ShowPokemonTab() → inventoryUI.Hide(),  pokemonTab.Show()

PokemonTabController (MonoBehaviour, "Pokemon" tab root)
   │  Show()/Hide()/Refresh()
   │  Action<CreatureRuntime> targetSelectionCallback   ← armed by ItemUsageSystem
   │
   ├─ N × PokemonSlotUI    (one per PlayerParty.Instance.Party entry, rebuilt every Refresh())
   │        IPointerClickHandler → OnClicked
   │
   └─ PokemonDetailsPanel  (right-hand panel: sprite/name/HP/Atk/Def/Speed)
```

Unlike the Items grid (`UIInventoryPage`), which allocates a fixed number of
slots once in `InitializeInventoryUI` and only updates their contents, the
Pokemon tab **destroys and re-instantiates** its slots every `Refresh()` — simpler
to write since the party size can change (creatures added/removed), at the cost
of GC churn each time the tab is opened.

## 2. File-by-file

| File | Role |
|---|---|
| `PokemonTabController.cs` | Tab controller. `Show()`/`Hide()`/`Refresh()` manage the slot list; also owns the one-shot `targetSelectionCallback` used by the item-use flow (`BeginTargetSelection` / `CancelTargetSelection`). |
| `PokemonSlotUI.cs` | Per-party-member row: sprite, name, `"{CurrentHP}/{MaxHP}"` text, a selection border. Pure `IPointerClickHandler` relay (`OnClicked`) — no branching logic of its own, same "dumb relay" pattern as `UIInventoryItem`. |
| `PokemonDetailsPanel.cs` | Read-only stat readout for whichever creature was last selected (sprite, name, HP/Attack/Defense/Speed). `ResetDetails()` clears it (called on `Refresh()` and via `Awake()`). |

## 3. Runtime flow

### 3a. Normal browsing (no pending item use)

1. Player clicks the **Pokemon** tab button → `InventoryController.ShowPokemonTab()`
   → `pokemonTab.Show()` → `Refresh()`.
2. `Refresh()`:
   - Unsubscribes and `Destroy()`s every previously spawned `PokemonSlotUI`.
   - `detailsPanel.ResetDetails()` (blanks the right-hand panel).
   - Bails out entirely if `PlayerParty.Instance == null`.
   - Otherwise, for every `CreatureRuntime` in `PlayerParty.Instance.Party` (in
     party order — index 0 is always the lead, see `PlayerParty.GetLeadCreature`/
     `SetLeadCreature`), instantiates a `PokemonSlotUI`, calls
     `slot.SetData(creature)`, and subscribes `slot.OnClicked += HandleSlotClicked`.
3. Player clicks a slot → `PokemonSlotUI.OnPointerClick` → `OnClicked(this)` →
   `PokemonTabController.HandleSlotClicked(slot)`.
4. Since `targetSelectionCallback == null` in this branch: deselects every other
   slot's border, `slot.Select()`s the clicked one, and pushes its data into
   `detailsPanel.SetDetails(slot.Creature)` — this is the "browse my party" path,
   independent of item usage.

### 3b. Target-selection mode (armed by "Use" on a healing item)

This is the same `PokemonTabController`/`PokemonSlotUI` machinery, but entered
from `InventoryController.HandleItemActionRequest` when the party has more than
one creature (see [ItemUsageSystem.md](ItemUsageSystem.md) §3):

1. `InventoryController` calls `pokemonTab.BeginTargetSelection(target =>
   UseHealingItem(itemIndex, target))` — this just stores the lambda in
   `targetSelectionCallback`. Nothing on screen changes yet beyond the tab switch
   that already happened in `ShowPokemonTab()`.
2. Player clicks any `PokemonSlotUI` → same `HandleSlotClicked(slot)` entry point
   as §3a, but now `targetSelectionCallback != null`:
   ```csharp
   Action<CreatureRuntime> callback = targetSelectionCallback;
   targetSelectionCallback = null;   // one-shot: cleared before invoking
   callback(slot.Creature);          // → InventoryController.UseHealingItem(...)
   return;                           // note: no Select()/SetDetails() in this branch
   ```
   The `return` means target-selection clicks **skip** the normal
   select-border/details-panel update — clicking a creature to heal it does not
   also open its stat panel or leave it visually selected.
3. Because the callback is nulled out *before* being invoked, `UseHealingItem`'s
   later `pokemonTab.Refresh()` (which destroys/recreates all slots and rewires
   `OnClicked`) cannot re-trigger target selection or double-fire the heal.
4. **Cancellation**: `PokemonTabController.Hide()` unconditionally calls
   `CancelTargetSelection()` (`targetSelectionCallback = null`). So switching back
   to the Items tab, or closing the whole menu with `I`, silently abandons an
   in-flight "use item → pick a target" request — the item is **not** consumed in
   this case (consumption only happens inside `UseHealingItem`, which is never
   reached).

## 4. Data source: `PlayerParty` / `CreatureRuntime`

- `PlayerParty` (`Assets/Scripts/Combat/PlayerParty.cs`) is a scene singleton
  (`Instance`, self-destructs duplicates in `Awake`) holding
  `List<CreatureRuntime> party`, built once from `startingCreatures`/
  `startingLevels` in `BuildStartingParty()`. This is the **same object** the
  battle system reads from (`BattleStarter.Interact → CombatManager.StartBattle(PlayerParty.Instance, ...)`,
  which calls `playerParty.GetLeadCreature()` — always `party[0]`).
- Because `PokemonTabController.Refresh()` reads `PlayerParty.Instance.Party`
  directly (not a copy), healing a creature via the item-use flow immediately
  affects the exact `CreatureRuntime` instance that the next battle's
  `CombatManager` will use as `playerRuntime` — there is no separate
  "overworld roster" vs. "battle roster" to keep in sync.
- `PokemonSlotUI.SetData` / `PokemonDetailsPanel.SetDetails` read
  `CreatureRuntime.CurrentHP` / `MaxHP` / `Attack` / `Defense` / `Speed` — all
  computed properties on `CreatureRuntime` (`MaxHP = data.maxHP + (Level-1)*5`,
  others `+ (Level-1)*2`), so leveling up between battles (`CreatureRuntime.GainXP`)
  is reflected here automatically without the Pokemon tab needing any level-aware
  logic of its own.

## 5. Scene wiring (`jardinconocimiento.unity`)

- `PokemonTabController` instance (`fileID 1476213322`) has `slotPrefab`,
  `contentPanel` (`fileID 1297674325`), and `detailsPanel` (`fileID 546281758`)
  assigned — the standard "prefab + content parent + details panel" trio.
- `InventoryController` (`fileID 1108876085`) holds the reference back to this
  same `PokemonTabController` via its `pokemonTab` field, and to the two tab
  `Button`s (`itemsTabButton` / `pokemonTabButton`) that call
  `ShowItemsTab()` / `ShowPokemonTab()`.
- As noted in `ItemUsageSystem.md` §5, `PlayerParty` itself is not part of this
  scene — it must be provided by a persistent object from an earlier-loaded
  scene. If it's missing, `Refresh()` degrades gracefully (empty list, no
  exception), but target selection armed by `HandleItemActionRequest` can never
  actually be reached since that method already bails out on
  `PlayerParty.Instance == null` before calling `BeginTargetSelection`.

## 6. Gaps / things not implemented

- **No "switch lead creature" UI.** `PlayerParty.SetLeadCreature(index)` exists
  and would let the player reorder who fights first, but nothing in
  `PokemonTabController`/`PokemonSlotUI` calls it — clicking a slot in browse
  mode (§3a) only opens the details panel, it never reorders the party.
- **No fainted-state affordance.** A creature at `CurrentHP == 0` renders exactly
  like any other slot (same sprite, `"0/{MaxHP}"` text, fully clickable/selectable
  as a heal target) — there's no greyed-out/disabled visual, and nothing stops a
  healing item from targeting (and reviving) a fainted creature, which may or may
  not be intended given `HealItemEffect` has no "must be alive" guard either.
- **No feedback if target selection is cancelled.** Switching tabs mid-selection
  silently drops the pending heal (see §3b step 4) with no toast/message telling
  the player their item-use was cancelled.
