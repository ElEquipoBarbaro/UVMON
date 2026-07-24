# Systems — Curative Items & Pokémon Selection

This folder documents two features that sit on top of the inventory and combat
codebases (`Assets/Scripts/MenuInventary` and `Assets/Scripts/Combat`) but aren't
covered by the existing per-folder analyses:

- **[ItemUsageSystem.md](ItemUsageSystem.md)** — how a curative item (Soda, Shuko,
  Purina...) goes from "right-click in the inventory grid" to actually healing a
  creature and being consumed.
- **[PokemonSelectionSystem.md](PokemonSelectionSystem.md)** — how the "Pokémon" tab
  of the in-game menu lists the party, shows details, and doubles as a target picker
  when a healing item needs a creature to be chosen.

Both features were built **after** `Assets/Scripts/MenuInventary/MENU_INVENTORY_ANALYSIS.md`
and `Assets/Scripts/Combat/COMBAT_SYSTEM_ANALYSIS.md` were written — those two docs
still describe an item-less, single-tab inventory. The docs here describe the current
(as of this writing) state: two tabs (`Items` / `Pokemon`), a right-click "Use" context
menu, and target selection against `PlayerParty`.

## Where things live

| Concern | Location |
|---|---|
| Item data (name/sprite/category/effect) | `Assets/ScriptableObjects/Items/*.asset` (`ItemSO`) |
| Heal effect data (amount / full-heal) | `Assets/ScriptableObjects/Items/Effects/*.asset` (`HealItemEffect`) |
| Item/effect scripts | `Assets/Scripts/MenuInventary/Model/` |
| Inventory UI + "Use" flow glue | `Assets/Scripts/MenuInventary/InventoryController.cs`, `UIInventoryPage.cs`, `UIInventoryItem.cs`, `ItemContextMenu.cs` |
| Pokémon tab UI + target picker | `Assets/Scripts/MenuInventary/PokemonTabController.cs`, `PokemonSlotUI.cs`, `PokemonDetailsPanel.cs` |
| Party / creature runtime state | `Assets/Scripts/Combat/PlayerParty.cs`, `CreatureRuntime.cs` |
| Scene wiring | `Assets/Scenes/jardinconocimiento.unity` (see each doc's "Scene wiring" section) |

## One-paragraph summary

The overworld inventory menu (`I` key) has two tabs driven by `InventoryController`:
an **Items** tab (`UIInventoryPage`, grid of `ItemSO` stacks) and a **Pokemon** tab
(`PokemonTabController`, list of the player's `CreatureRuntime`s from `PlayerParty`).
Right-clicking an item slot opens a small `ItemContextMenu` with a single **Use**
button; clicking it bubbles up to `InventoryController.HandleItemActionRequest`,
which only acts on items whose `ItemSO.Category == ItemCategory.Healing` and that
have an `ItemEffect` assigned. If the party has more than one creature, the request
switches the menu to the Pokemon tab and puts `PokemonTabController` into a
one-shot "target selection" mode — the next slot clicked is fed back as the heal
target instead of just opening its details panel. Once a target is resolved, the
item's `ItemEffect.Apply(target)` runs (currently only `HealItemEffect`, which either
heals a fixed amount or fully heals), the item is decremented from `InventorySO`,
and the Pokemon tab is refreshed to show the new HP. **This entire flow is
overworld-only** — `CombatManager`'s battle loop has no equivalent "use item"
input, so items cannot be used mid-battle (see the Gaps section in
`ItemUsageSystem.md`).
