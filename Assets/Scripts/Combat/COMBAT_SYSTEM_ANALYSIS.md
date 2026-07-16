# Combat System Analysis — `Assets/Scripts/Combat`

Analysis of how the turn-based battle system works, based on reading all 19 scripts in the folder.

## 1. High-level architecture

```
BattleStarter (on a trainer/NPC)
        │  Interact()
        ▼
DialogueTrigger / DialogueManager  (optional, outside this folder)
        │  OnDialogueEnded
        ▼
CombatManager.StartBattle(PlayerParty, EnemyTrainer)
        │
        ├─ PlayerParty.GetLeadCreature()   → CreatureRuntime (player, persists between fights)
        ├─ EnemyTrainer.GetLeadCreature()  → CreatureRuntime (enemy, freshly created every fight)
        │
        ▼
BattleUIManager (panels, HP text, move list, messages)
        │
        ▼
BattleLoop() coroutine  ── alternates PlayerTurn() / EnemyTurn()
        │
        ├─ BattleAnimationPlayer  (startup motion, projectile/beam, impact VFX, camera shake, hit reaction)
        └─ MoveData.effect (MoveEffect, e.g. DamageEffect)
                 │
                 ├─ TypeChart.GetMultiplier(moveType, targetType)
                 └─ CreatureRuntime.TakeDamage / GainXP
```

Data is modeled with `ScriptableObject`s (`CreatureData`, `MoveData`, `MoveAnimationData`, `MoveEffect`, `TrainerData`) so designers can author creatures/moves/trainers as assets. `CreatureRuntime` is the plain C# (non-Unity-object) class that wraps a `CreatureData` with mutable battle state (HP, level, XP, moves).

## 2. File-by-file

| File | Role |
|---|---|
| `BattleStarter.cs` | Entry point on an NPC/trainer GameObject. Optionally plays a dialogue first, then calls `CombatManager.Instance.StartBattle(...)`. |
| `CombatManager.cs` | The orchestrator/singleton. Owns the turn loop coroutine, reads player input (arrow keys + Enter) to pick a move, drives `BattleUIManager` and `BattleAnimationPlayer`, and calls into the selected `MoveEffect`. |
| `PlayerTurn.cs` | **Dead code** — a `MonoBehaviour` with a single empty coroutine (`yield return null`). Not attached/referenced anywhere; `CombatManager` has its own private `PlayerTurn()` coroutine method that does the real work under the same name. |
| `BattleUIManager.cs` | Pure UI façade: shows/hides the battle panel vs overworld, renders HP text, renders the move list with a `>` cursor, shows battle messages. No logic of its own. |
| `BattleAnimationPlayer.cs` | Plays `MoveAnimationData`: startup motion (shake/lunge/charge/hop) on the attacker, optional projectile/beam travel, optional impact VFX, optional camera shake, then a hit-reaction shake on the target. |
| `CreatureBattleView.cs` | Per-side visual (sprite + `RectTransform`). Caches a "resting" anchored position so animations can offset from it and return. |
| `CreatureData.cs` | ScriptableObject: type, name, sprites, base stats (HP/atk/def/speed), move list, XP yield. |
| `CreatureRuntime.cs` | Plain class: level/XP/current HP + derived stats (`MaxHP/Attack/Defense/Speed` scale linearly with level: `base + (Level-1)*5` for HP, `*2` for the rest). Handles damage, heal, XP gain and level-up (auto full-heal on level up). |
| `CreatureType.cs` | 12-entry enum (Digital, Mecanico, Armonia, Entropia, Organico, Fosil, Estrategia, Estructura, Vector, Mente, Nexo, Conducto) — the game's own type system (not Pokémon's). |
| `TypeChart.cs` | Static nested-switch lookup, attacker type → defender type → multiplier (0.5/1/2). Every enum value has a case in the outer switch, so it's exhaustive; unmatched inner cases fall back to `1f`. |
| `DamageEffect.cs` | The only `MoveEffect` implementation. Damage = `max(1, (Attack+power) - Defense)`, then `* variance(0.85–1) * crit(10% → 1.5x) * typeMultiplier`. Also drives the "used X!", "critical hit!", "super effective/not very effective", "took N damage", "fainted" message sequence. |
| `EnemyTrainer.cs` | Wraps a `TrainerData` asset; `GetLeadCreature()` builds a brand-new `CreatureRuntime` from `team[0]` every time it's called. |
| `MoveAnimationData.cs` | ScriptableObject holding all the presentation knobs consumed by `BattleAnimationPlayer` (durations, prefabs, sound, screen shake). |
| `MoveData.cs` | ScriptableObject tying together name, power, `accuracy`, type, a `MoveEffect`, and `MoveAnimationData`. |
| `MoveEffect.cs` | Abstract ScriptableObject base: `Execute(user, target, move)` coroutine. Designed so new move behaviors (heal, status, buffs...) can be added as new assets/subclasses. |
| `MovePresentationType.cs` | `Instant / Projectile / Beam` — how `BattleAnimationPlayer` moves the attack visual. |
| `BattleMotionType.cs` | `None / Shake / Lunge / Charge / Hop` — attacker startup motion. |
| `PlayerParty.cs` | Singleton holding the player's `List<CreatureRuntime>`. Supports adding/removing creatures, reordering the lead, `HasUsableCreature()`, `HealAll()`. |
| `TrainerCreatureSlot.cs` | Serializable slot in a trainer's team: creature + level + optional custom moveset override. |
| `TrainerData.cs` | ScriptableObject: trainer name + `List<TrainerCreatureSlot>`. |

## 3. Turn loop, in detail (`CombatManager`)

1. `StartBattle` grabs both leads, resets selection state, shows the UI, prints the "A wild X appeared!" message, and starts `BattleLoop()`.
2. `BattleLoop` is a `while (playerRuntime.CurrentHP > 0 && enemyRuntime.CurrentHP > 0)` loop that always runs **`PlayerTurn()` then `EnemyTurn()`**, unconditionally, every round.
3. `PlayerTurn()` sets `isPlayerTurn = true` and awaits `playerHasChosen` (set by `SelectMove`, itself only called from `Update()`'s Enter-key handler). Once chosen: plays the move's startup/attack animation, then `move.effect.Execute(...)`, then refreshes HP UI.
4. `EnemyTurn()` always plays `enemyRuntime.Moves[0]` — no AI decision-making at all.
5. `EndBattleSequence()` prints fainted/win/lose messages and hides the battle UI, awarding XP to the player's lead creature only on a win.

## 4. Notable findings

### Dead code
- **`PlayerTurn.cs`** is an unused `MonoBehaviour` (empty coroutine, not referenced/attached anywhere). Safe to delete; the real per-turn logic lives in `CombatManager.PlayerTurn()`.

### Gaps vs. what the data model supports
- **`MoveData.accuracy` is never read.** Every move always hits — there's no roll against accuracy anywhere in `DamageEffect` or `CombatManager`.
- **`Speed` is computed on `CreatureRuntime` but never used.** Turn order is hardcoded player-first, every round, regardless of either creature's speed.
- **Enemy has no AI.** `EnemyTurn()` unconditionally picks `Moves[0]`.
- **Only one `MoveEffect` exists (`DamageEffect`).** The abstract `MoveEffect`/`MoveData` design clearly anticipates heal/status/buff effects, but none are implemented yet — every move in the game is currently forced to be a damage move.
- **No fainting/switch flow.** `PlayerParty` supports multiple creatures (`HasUsableCreature`, party list, `SetLeadCreature`), but `CombatManager` only ever fetches `GetLeadCreature()` once at battle start. If the lead faints, the battle ends in a loss even if the party has healthy creatures left — the party-switching machinery exists but is wired to nothing.
- **No flee/run and no item use** during battle — only "pick a move" is supported by the UI/input handling.

### Coupling / robustness
- `DamageEffect` (a ScriptableObject asset) reaches back into `CombatManager.Instance` and `CombatManager.Instance.BattleUI` to print messages and refresh HP. This makes `MoveEffect` implementations implicitly depend on a live `CombatManager` singleton rather than receiving a UI/context reference — works, but any future effect run outside a real battle (e.g. a unit test) needs the singleton mocked.
- `CombatManager.Awake()` sets `Instance = this` with no duplicate-guard, unlike `PlayerParty.Awake()` which explicitly destroys a duplicate singleton. Two `CombatManager`s in a scene would silently overwrite the reference.
- `StartBattle` calls `StartCoroutine(BattleLoop())` without stopping any previously running battle loop. Calling `StartBattle` again while a battle is already in progress (e.g. a double-triggered `BattleStarter.Interact()`) would run two overlapping `BattleLoop`s.
- `enemyRuntime` is rebuilt from scratch every battle (fresh `CreatureRuntime`, full HP), while `playerRuntime` is the actual party object and keeps damage between battles — this looks intentional (wild/trainer battles don't persist enemy HP, but the player's creature carries its scars), but is worth confirming it's the intended design rather than an oversight, since there's no full-heal-on-battle-end path other than an explicit call to `PlayerParty.HealAll()`.

### Things that work as expected
- `TypeChart` is exhaustive over all 12 `CreatureType` values as the attacking type, with a safe `1f` fallback.
- Level-scaling formulas in `CreatureRuntime` are simple and consistent (`+5 HP`/`+2 other stats` per level above 1).
- Animation sequencing in `BattleAnimationPlayer` (startup → sound → travel/beam → screen shake → impact VFX → hit reaction) is coherent and each stage degrades gracefully when its data is unset (e.g. no prefab → just waits `attackTravelDuration`).
- UI/logic separation is clean: `BattleUIManager` has no battle rules in it, `CombatManager` has no direct UI-widget references beyond calling into `BattleUIManager`.
