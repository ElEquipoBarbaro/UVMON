# Combat System Analysis — `Assets/Scripts/Combat`

Analysis of how the turn-based battle system works, based on reading all 19 scripts in the folder.

> **Extremidades / acertividad / daño crítico (Prompts 12-18, `COMBAT_SYSTEM_SPEC.md`
> en la raíz del repo)**: ver `BODY_PARTS_SYSTEM.md` en esta misma carpeta.
> `MoveData.accuracy` sigue sin usarse (ver hallazgo más abajo) — la acertividad del
> sistema nuevo vive en `BodyPartDefinition.porcentajeAcertividad`, no en el
> movimiento.

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
| `CombatManager.cs` | The orchestrator/singleton. Owns the turn loop coroutine, picks a move from mouse hover/click on `BattleUIManager`'s move options (see below), drives `BattleUIManager` and `BattleAnimationPlayer`, and calls into the selected `MoveEffect`. |
| `PlayerTurn.cs` | **Dead code** — a `MonoBehaviour` with a single empty coroutine (`yield return null`). Not attached/referenced anywhere; `CombatManager` has its own private `PlayerTurn()` coroutine method that does the real work under the same name. |
| `BattleUIManager.cs` | UI façade: shows/hides the battle panel vs overworld, renders HP text, shows battle messages, and manages the move-selection slots (instantiates one `MoveOptionUI` per move under `moveOptionsContainer` from `moveOptionPrefab`, re-firing their hover/click as `OnMoveHovered`/`OnMoveClicked` events). |
| `MoveOptionUI.cs` | Per-slot component (`Image` background + `TextMeshProUGUI` label). Implements `IPointerEnterHandler`/`IPointerClickHandler` — hover swaps the background to `highlightColor`, click raises `OnClicked`. The background stays enabled at all times (only its color changes) so it never stops being a valid raycast/click target. |
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
3. `PlayerTurn()` sets `isPlayerTurn = true` and awaits `playerHasChosen` (set by `SelectMove`). Hovering a `MoveOptionUI` slot fires `BattleUIManager.OnMoveHovered` → `CombatManager.HandleMoveHovered` (updates `selectedMoveIndex` and re-highlights); clicking one fires `OnMoveClicked` → `HandleMoveClicked` → `SelectMove(playerRuntime.Moves[index])`. Once chosen: plays the move's startup/attack animation, then `move.effect.Execute(...)`, then refreshes HP UI.
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
- ~~`CombatManager.Awake()` sets `Instance = this` with no duplicate-guard...~~ **Fixed
  2026-08-07** (see section 7): now mirrors `PlayerParty.Awake()`'s duplicate-guard, and
  `OnDestroy()` clears `Instance` if it was the active singleton.
- ~~`StartBattle` calls `StartCoroutine(BattleLoop())` without stopping any previously
  running battle loop...~~ **Fixed 2026-08-07** (see section 7): `StartBattle` now
  tracks `battleLoopCoroutine` and ignores a re-entrant call while one is already
  running; `BattleStarter.Interact()` also guards against double-subscribing
  `HandleDialogueEnded`. This was traced as the likely root cause of two user-reported
  bugs (damaged body-part sprite sometimes not showing, enemy sometimes not retaliating
  on consecutive player attacks) — two overlapping `BattleLoop`s share the same turn-state
  instance fields, corrupting turn order and `selectedBodyPartIndex`.
- `enemyRuntime` is rebuilt from scratch every battle (fresh `CreatureRuntime`, full HP), while `playerRuntime` is the actual party object and keeps damage between battles — this looks intentional (wild/trainer battles don't persist enemy HP, but the player's creature carries its scars), but is worth confirming it's the intended design rather than an oversight, since there's no full-heal-on-battle-end path other than an explicit call to `PlayerParty.HealAll()`.

## 5. Feature agregada (2026-08-07): flash de golpe para el Pokémon del jugador

**Pedido:** el jugador solo tenía feedback visual de golpe (flash) en las extremidades del
enemigo (`BodyPartOptionUI.PlayHitFlash`); el propio Pokémon del jugador no mostraba nada
al recibir daño del enemigo.

**Contexto importante — esta sesión coincidió con un `git pull` a mitad de trabajo:** el
enemigo, mientras tanto, cambió de técnica de flash "por su cuenta" (upstream): pasó de un
simple parpadeo de `alpha` sobre la imagen base a un **filtro blanco superpuesto**
(`flashOverlayImage`) — una variante toda-blanca del sprite (mismo canal alfa) generada en
tiempo real y cacheada, cuyo alfa oscila 0→`hitFlashMaxAlpha` en un pulso sinusoidal único,
dejando el sprite base intocado en todo momento. Esto causó dos conflictos de merge
(`git stash pop`) en `Assets/Scripts/Combat/BodyPartOptionUI.cs` y en
`Assets/Scenes/jardinconocimiento.unity` (un `GameObject` nuevo de cada lado —
`FlashOverlay` del upstream y el `Scroll View` de Pokémon de una tarea anterior de esta
misma sesión— insertados en el mismo punto del árbol; se resolvieron manteniendo ambos
bloques completos, sin perder ninguno).

**Solución:** se extrajo la lógica del filtro blanco (generación del sprite blanco +
animación del pulso) a un helper estático compartido, `HitFlashEffect.cs`
(`GetOrCreateWhiteSprite` + `PlayOverlay`), del cual ahora tiran **tanto**
`BodyPartOptionUI` (extremidades del enemigo, sin cambios de comportamiento) **como**
`CreatureBattleView` (sprite completo — usado por el jugador siempre, y por el enemigo
cuando no tiene extremidades). `CreatureBattleView` ganó un campo `flashOverlayImage`
(mismos valores por defecto que `BodyPartOptionUI`: duración 0.5s, velocidad 18,
alpha máximo 0.85) y un método público `PlayHitFlash()`.

**Conexión al flujo real de daño:** `CombatManager.PlayHitFlashFor(CreatureRuntime)` decide
a qué vista aplicar el flash según si la criatura golpeada es `playerRuntime` o
`enemyRuntime`. Se llama desde `DamageEffect.Execute`, inmediatamente después de
`target.TakeDamage(damage)` — nunca antes, así que un ataque fallido (QTE fallido o
`ataqueImpacta=false`, que cortan el flujo con `yield break` antes de llegar ahí) jamás
dispara el flash. El ataque a extremidades (`ExecuteBodyPartAttack`) sigue usando
`PlayEnemyBodyPartHitFlash` sin pasar por acá — sin cambios.

**Bug preexistente encontrado al probar (bloqueaba el flash del jugador, no relacionado
con el merge):** los sprites `rat.png` (Jack) y `versionmini.png` (Versionmini) — usados
como `frontSprite`/`backSprite` — no tenían **Read/Write Enabled**, y
`GetOrCreateWhiteSprite` necesita leer los píxeles vía `Texture2D.GetPixels`. Sin esto,
`CombatManager.StartBattle` tiraba una `ArgumentException` apenas se intentaba iniciar
cualquier batalla con estas criaturas — no era exclusivo del flash del jugador, rompía el
inicio de combate en sí. Corregido con `manage_texture set_import_settings
{"readable": true}` en ambas texturas (las texturas de las extremidades del enemigo, en
`Assets/Enemys/Spider/PARTES/`, ya lo tenían activado — por eso su flash nunca lo había
expuesto).

**Verificación:** Play Mode vía MCP. `CombatManager.PlayHitFlashFor(playerRuntime)`
invocado directamente (la cadena completa turno→QTE→daño es difícil de automatizar de
forma confiable): el overlay del jugador pasa de alpha 0 a ~0.5–0.6 en el primer frame,
el sprite base (`color`/`sprite`) queda intacto en todo momento, y al terminar el overlay
vuelve a alpha 0 con la corrutina liberada (`hitFlashRoutine = null`). Tres llamadas
seguidas (golpes consecutivos) dejan solo una corrutina activa a la vez (cada llamada
detiene la anterior) y el estado final sigue siendo correcto. Confirmado visualmente con
captura de pantalla (duración extendida a 60s solo en memoria, para poder capturar el
frame — no se persistió). El flash existente del enemigo (`PlayEnemyBodyPartHitFlash`) se
probó sin cambios y sigue funcionando. 0 errores/warnings en consola.

### Corrección posterior (mismo día): el flash del enemigo dejó de verse tras el merge

Después de lo anterior, el usuario reportó que el flash de golpe del enemigo **y** el
parpadeo ambiental de hover/selección (el que indica qué extremidad se va a atacar antes
de confirmar) habían dejado de verse — ninguno de los dos, en ninguna parte.

**Causa:** `BodyPartOptionTemplate` (el `GameObject` en la escena que `EnemyBodyPartsView`
clona como `partOptionPrefab` para cada extremidad) nunca tuvo su campo
`BodyPartOptionUI.flashOverlayImage` conectado al hijo `FlashOverlay` — a pesar de que ese
hijo sí existe correctamente en la jerarquía (confirmado: `BattleUI/BodyPartOptionTemplate
/FlashOverlay`, bien parenteado, con su `Image` en blanco/alpha 0 como corresponde). La
sección conflictiva del merge (ver más arriba) solo contenía la definición del propio
`GameObject` `FlashOverlay` — la asignación del campo en el componente `BodyPartOptionUI`
vive en un bloque totalmente distinto del archivo, que el merge automático de git nunca
marcó como conflicto, así que nadie lo notó hasta probar en Play Mode. Con
`flashOverlayImage == null`, **tanto** `Update()` (el parpadeo ambiental) **como**
`PlayHitFlash()` retornan inmediatamente sin hacer nada (`if (flashOverlayImage ==
null...) return;`) — de ahí que los dos efectos, aparentemente sin relación entre sí,
desaparecieran juntos: comparten el mismo `Image` de overlay.

**Fix:** se conectó `BodyPartOptionTemplate`'s `BodyPartOptionUI.flashOverlayImage` a su
`FlashOverlay/Image` vía MCP (`manage_components set_property`).

**Verificación:** Play Mode vía MCP. Simulando `OnPointerEnter` sobre una extremidad:
`overlay.color.a` oscila con el jugador pasando el mouse (antes de confirmar objetivo).
`PlayEnemyBodyPartHitFlash(index)`: el overlay salta a alpha ~0.5 al instante y, al
terminar el pulso, el control vuelve correctamente al parpadeo ambiental (si seguía en
hover) — confirma que ambos efectos comparten el mismo `Image` sin pisarse mal entre sí,
tal como está diseñado (`hitFlashRoutine != null` le da prioridad exclusiva al pulso de
golpe mientras corre). Sprite base intacto en todo momento. Flash del jugador (agregado
en el fix anterior) confirmado sin regresión. Confirmado también con captura de pantalla.
0 errores en consola.

## 6. Feature agregada (2026-08-07): indicador visual "FALLO" para ataques fallidos

**Pedido:** mostrar un sprite "FALLO" (ya importado en
`Assets/Sprites/Sprite_Fallo_Combate/FALLO_sprite.png`, único archivo en esa carpeta) sobre
la cabeza del enemigo cuando un ataque del jugador falla, con una animación rápida tipo
"drop-up" (sube + escala/alpha 0→1, se mantiene un instante, se desvanece) y sin dejar
`GameObject`s residuales.

**Diseño:** se creó `MissIndicator.cs` (coroutine-based — el proyecto no usa
DOTween/LeanTween en ningún lado) que anima su propio `RectTransform`/`Image` y se
autodestruye (`Destroy(gameObject)`) al terminar. Se empaquetó como prefab
(`Assets/Prefabs/MissIndicator.prefab`, `RectTransform` 160×49.7 con `preserveAspect`,
ancla/pivot centrados) referenciando el sprite existente (`fileID: 21300000, guid:
09f146dda138d2d419e97adfd2d3646a` — la convención de sprite-mode único documentada más
arriba en `CLAUDE.md`); no se tocó ni se duplicó el asset original, y su `.meta` ya estaba
bien configurado (`alphaIsTransparency: 1`, `filterMode: 1` Bilinear — coincide con los
sprites de extremidades del enemigo, sus vecinos visuales reales) así que tampoco se
modificaron sus opciones de importación.

**Integración:** se reutilizó el patrón de `BattleAnimationPlayer.InstantiateImpact`
(instanciar bajo `projectileLayer` + `Destroy(obj, lifetime)` externo como red de
seguridad) en un nuevo método público, `PlayMissIndicator(CreatureBattleView targetView)`,
con dos campos nuevos `[SerializeField]`: `missIndicatorVerticalOffset` (90px por defecto,
el offset configurable pedido) y `missIndicatorLifetime` (1.5s, red de seguridad externa).
Para evitar acumulación en fallos consecutivos rápidos, `BattleAnimationPlayer` guarda la
última instancia (`currentMissIndicator`) y la destruye antes de crear la siguiente en vez
de apilarlas. Se llama desde **los dos** puntos donde `CombatManager` ya detecta un fallo
oficial —`PlayerTurn()` (`if (!attackSucceeds)`) y `ExecuteBodyPartAttack()` (`if
(!result.ataqueImpacta)`)— **antes** del `yield break` que ya cortaba el flujo hacia
daño/flash; no se tocó esa lógica de acierto/fallo ni el camino de ataque exitoso.

**Bug encontrado al verificar (pre-existente, no introducido por esta feature):**
`targetView.CurrentAnchoredPosition` está expresado en las unidades del Canvas de
`BattleUI` (`CanvasScaler` en modo *Scale With Screen Size*, `referenceResolution`
1920×1080, así que 1 unidad ≠ 1 píxel real), pero `projectileLayer` ("ProjectileLayer",
hijo de `AnimationController`) es un Canvas Screen-Space-Overlay **totalmente aparte**, en
modo *Constant Pixel Size* (1 unidad = 1 píxel real). El sistema existente
(`InstantiateImpact`, usado por `attackPrefab`/`impactVfxPrefab`) pasa esa
`anchoredPosition` **tal cual** de un canvas al otro sin convertir unidades — con la
escena de prueba (pantalla 1484×685, `scaleFactor` de `BattleUI` ≈0.773) esto desplaza
cualquier VFX instanciado ahí notablemente lejos de la posición real del objetivo en
pantalla (confirmado: con el offset del indicador daba `anchoredPosition (468, 295)`
dentro de `projectileLayer`, pero la posición real en píxeles del enemigo en pantalla era
`(361.7, 158.5)` — el indicador aparecía ~135px más arriba de lo esperado, casi pegado al
borde superior de pantalla). Se corrigió **solo para el nuevo indicador** (sin tocar
`InstantiateImpact`/`PlayAttackVisual`, que siguen igual que antes) agregando
`BattleAnimationPlayer.ProjectileLayerPosition(Vector3 worldPosition)`, que convierte la
posición real en pantalla (`targetView.transform.position`, válida en cualquier convención
de Canvas) a coordenadas locales de `projectileLayer` vía
`RectTransformUtility.WorldToScreenPoint` + `ScreenPointToLocalPointInRectangle`.

**Segundo bug encontrado al verificar (también pre-existente):** aun con la posición
corregida, el indicador seguía sin verse. Causa: el Canvas de `ProjectileLayer`
(`sortingOrder 0`, `renderOrder 2`) se dibuja **detrás** del Canvas de `BattleUI`
(`sortingOrder 0`, `renderOrder 3`) — con el mismo `sortingOrder`, Unity desempata por
orden de registro, y `BattleUI` (registrado después) gana. Esto significa que
**cualquier VFX de `projectileLayer`** (no solo el indicador nuevo) queda oculto detrás
del fondo/sprites de `BattleUI`. Corregido subiendo `ProjectileLayer.sortingOrder` a `10`
directamente en la escena (cambio de configuración de Canvas, no de lógica de combate) —
arregla el indicador nuevo y, de paso, el sistema de proyectiles/impacto existente, que
compartía el mismo problema.

**Verificación:** Play Mode vía MCP. Con ambos fixes, `PlayMissIndicator` invocado
directamente sobre `battleUI.EnemyView` (la cadena turno→QTE completa es frágil de
automatizar, como en features anteriores de esta sesión) coloca el sprite correctamente
sobre la cabeza del enemigo sin cubrirlo (confirmado con captura de pantalla), con fondo
transparente preservado. Cinco invocaciones seguidas sin esperar (simulando fallos
consecutivos rápidos) dejan como máximo una instancia viva en cualquier momento — nunca
acumulación indefinida — y el conteo de hijos de `ProjectileLayer` vuelve a `0` una vez
transcurre el ciclo completo (entrada+hold+salida), sin dejar `GameObject`s residuales.
0 errores/warnings en consola tras recompilar. El camino de ataque exitoso no se tocó: la
llamada nueva vive estrictamente dentro de las ramas de fallo que ya hacían `yield break`
antes de cualquier código de daño/flash, así que un acierto nunca puede alcanzarla.

## 7. Bugs reportados y corregidos (2026-08-07): selección de cabeza imposible, y combate "sin sentido de turnos"

**Reporte del usuario:** (a) clickear la cabeza del enemigo siempre selecciona el cuerpo,
nunca la cabeza; (b) a veces el sprite dañado (vendajes) del cuerpo no aparece al llegar a
0 de vida; (c) a veces, atacando varias veces seguidas, el enemigo no devuelve daño — "no
tiene sentido que sea por turnos".

### (a) Selección de extremidades: causa raíz encontrada — `spriteMeshType: Tight`

**Diagnóstico:** con `BattleUI` activo y `EnemyBodyPartsView.Setup()` reconstruyendo los
dos slots de Jack Malo (`body` sibling0, `head` sibling1 — `head` queda encima, como
diseñado), se confirmó que `image.alphaHitTestMinimumThreshold = 0.1f` sí se aplicaba
correctamente (el bug NO es la wiring `image`/`flashOverlayImage` de la corrección
anterior). El problema apareció al invocar directamente
`Image.IsRaycastLocationValid(screenPoint, camera)` (reflection) sobre `head`: devolvía
`false` incluso apuntando al **centro exacto** de un píxel confirmado opaco
(`Texture2D.GetPixel(w/2,h/2).a == 1.0`). Un barrido en grilla sobre el rect de `head`
mostró que la región "válida" para el click no coincidía en absoluto con la forma
visible de la cabeza (un solo punto suelto, lejos del centro).

Causa: `head.png`/`body.png`/`body_damaged.png` (`Assets/Enemys/Spider/PARTES/`) tenían
`spriteMeshType: 1` (Tight) en su importer — el valor por defecto de Unity para sprites
recortados con transparencia. Con "Tight", `Sprite.textureRect` (la caja ajustada a los
píxeles opacos) queda más chica/desplazada que `Sprite.rect` (el lienzo completo). El
algoritmo interno de `Image.IsRaycastLocationValid` (`MapCoordinate` + normalización)
mezcla ambos rects al mapear la posición del click a un UV de textura — con `Tight`
mueve el punto de muestreo a una zona completamente distinta de la que realmente se ve
en pantalla (la propia *renderización* de un `Image` Simple sí usa el `rect` completo,
por eso visualmente nada se veía raro — solo el hit-test quedaba mal alineado). Es un
gotcha documentado de UGUI: `alphaHitTestMinimumThreshold` + sprites con mesh "Tight" no
combinan bien.

**Fix:** `spriteMeshType` cambiado de `1` (Tight) a `0` (Full Rect) en los `.meta` de las
tres texturas (edición directa de YAML + `refresh_unity`, ya que
`manage_texture action=set_import_settings` no expone esta clave — solo acepta un set
limitado tipo `readable`/`filterMode`). Con Full Rect, `textureRect == rect` y el mapeo
vuelve a ser 1:1 con lo renderizado. No afecta el aspecto visual (un `Image` Simple ya
renderiza el `rect` completo sin importar el mesh type) — solo corrige el hit-test.

**Verificación:** Play Mode vía MCP, simulando `EventSystem.RaycastAll` en el punto real
de pantalla donde se ve el cráneo de `head` (hallado por prueba visual: capturas con
cada slot oculto/mostrado por separado). Antes del fix: 0 resultados en ese punto (ni
`head` ni `body`). Después del fix: `head` (Index=1, profundidad más alta) aparece
primero en los resultados, con `body` (Index=0) como segunda opción detrás —
exactamente el comportamiento esperado (la extremidad de encima gana donde es opaca; el
click atraviesa a la de abajo donde es transparente). No se modificaron `alphaIsTransparency`,
`isReadable` ni ninguna otra opción ya correcta de estos tres archivos.

### (b) y (c): mismo origen — `CombatManager` sin guardia contra un `BattleLoop` duplicado

Este gap ya estaba anotado (sin corregir) en la sección 4 de este documento:
`CombatManager.Awake()` no protegía contra una instancia duplicada, y `StartBattle()`
llamaba `StartCoroutine(BattleLoop())` sin comprobar si ya había una corutina de batalla
corriendo. `BattleStarter.Interact()` además podía suscribir `HandleDialogueEnded` dos
veces si el jugador interactuaba de nuevo mientras el diálogo previo a la pelea todavía
estaba abierto (antes de que `TriggerDialogue()` bloqueara la tecla de interacción) — y
al terminar el diálogo, ambas suscripciones disparaban `StartBattle()`.

Con dos `BattleLoop()` corriendo a la vez, **ambos comparten los mismos campos de
instancia** (`isPlayerTurn`, `playerHasChosen`, `selectedBodyPartIndex`,
`enemyBodyPartsRuntime`, etc.) — un solo click del jugador satisface el `WaitUntil` de
los dos loops a la vez, cada uno resuelve el ataque por su cuenta (duplicando daño/QTE) y
cada uno decide de forma independiente cuándo llamar a `EnemyTurn()`/tocar
`selectedBodyPartIndex`, así que el orden real de turnos deja de ser predecible: a veces
el turno del enemigo de un loop queda "tapado" por el loop del otro, a veces
`selectedBodyPartIndex` cambia por debajo de un `ExecuteBodyPartAttack` en curso del otro
loop justo antes de que este marque la extremidad dañada (apuntando al índice
equivocado). Esto explica de forma consistente tanto "a veces no aparece el sprite
dañado" como "a veces el enemigo no devuelve daño atacando seguido" — ambos son síntomas
del mismo estado de turno compartido y corrompido, no dos bugs independientes.

**Fix:**
- `CombatManager.Awake()`: guardia de instancia duplicada (mismo patrón que
  `PlayerParty.Awake()` — `Destroy(gameObject)` si ya hay un `Instance` vivo distinto de
  `this`). `OnDestroy()` limpia `Instance` si era el singleton activo.
- `CombatManager.StartBattle()`: nuevo campo `battleLoopCoroutine` que guarda la
  referencia devuelta por `StartCoroutine(BattleLoop())`; si ya hay una batalla en curso
  (`battleLoopCoroutine != null`), la llamada se ignora por completo (no reconstruye
  `playerRuntime`/`enemyRuntime`/`enemyBodyPartsRuntime`, no repinta la UI). Se limpia
  (`= null`) al final de `BattleLoop()`, después de `EndBattleSequence()`, para que una
  batalla *legítima* posterior sí pueda arrancar. Nueva propiedad pública
  `IsBattleActive` por si algo externo necesita consultarlo.
- `BattleStarter.Interact()`: nuevo flag `interactionPending` que evita suscribir
  `HandleDialogueEnded` una segunda vez mientras ya hay una interacción en curso; se
  libera cuando el diálogo termina (justo antes de `StartBattle()`).

**Verificación:** Play Mode vía MCP. Se armó una batalla real (`PlayerParty` +
`EnemyTrainer` apuntando a `Enemy Spider 1.asset` / Jack Malo) y se invocó
`CombatManager.StartBattle(...)` dos veces seguidas sin esperar (reflection, simulando el
doble trigger). Confirmado: `battleLoopCoroutine` es la **misma** instancia de
`Coroutine` antes y después de la segunda llamada, y `enemyRuntime` tampoco se
reconstruyó — la segunda llamada fue ignorada de punta a punta, tal como debía. No se
pudo reproducir de punta a punta el escenario original completo (turno del jugador vía
QTE real + click de UI real, varias rondas) dentro de esta sesión por inestabilidad del
propio arnés de prueba (Play Mode se detuvo solo a mitad de una tanda de pruebas por
reflection, ver advertencia "Destroy may not be called from edit mode" en consola —
artefacto de la sesión de prueba, no del juego), así que la verificación de (b)/(c)
queda al nivel de "la causa raíz identificada y su guardia se probaron directamente y
funcionan", no de una repetición íntegra del flujo reportado por el usuario.

0 errores/warnings en consola tras recompilar (más allá del warning preexistente y no
relacionado de `InventorySO.cs`). Estado de la escena verificado limpio después de cada
tanda de pruebas (sin `GameObject`s de prueba ni cambios de `activeSelf` residuales).

### Things that work as expected
- `TypeChart` is exhaustive over all 12 `CreatureType` values as the attacking type, with a safe `1f` fallback.
- Level-scaling formulas in `CreatureRuntime` are simple and consistent (`+5 HP`/`+2 other stats` per level above 1).
- Animation sequencing in `BattleAnimationPlayer` (startup → sound → travel/beam → screen shake → impact VFX → hit reaction) is coherent and each stage degrades gracefully when its data is unset (e.g. no prefab → just waits `attackTravelDuration`).
- UI/logic separation is clean: `BattleUIManager` has no battle rules in it, `CombatManager` has no direct UI-widget references beyond calling into `BattleUIManager`.
