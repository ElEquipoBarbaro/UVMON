# Sistema de extremidades / acertividad / daño crítico — `Assets/Scripts/Combat`

Implementa `PROMPT.md` (Fase 5 y 6, Prompts 12-18) sobre la especificación
`COMBAT_SYSTEM_SPEC.md` (raíz del repo). Ver también `COMBAT_SYSTEM_ANALYSIS.md` para
el flujo de batalla general y `QTE/QTE_SYSTEM.md` para el QTE (sin cambios).

## 1. Archivos nuevos

| Archivo | Rol |
|---|---|
| `CombatMath/RandomProvider.cs` | `IRandomProvider` + `UnityRandomProvider` (sin semilla fija). Abstracción de aleatoriedad para poder inyectar valores deterministas en pruebas (spec sec 33). |
| `CombatMath/AccuracyChecker.cs` | Comprobación de acertividad (spec sec 23). Solo evalúa si `qteExitoso`; genera `valorImpacto` una única vez. |
| `CombatMath/DamageCalculator.cs` | Orden completo QTE→acertividad→crítico→variación→daño (spec sec 24-28), incluye el crítico de derrota instantánea. |
| `CombatMath/BodyPartDefinition.cs` | Datos de diseño de una extremidad (spec sec 2): `idParte`, `nombreParte`, `vidaMaxima`, `porcentajeAtaque`, `porcentajeAcertividad`, sprites normal/dañado, `anchoredPosition`. Se autora en el Inspector de `CreatureData.bodyParts`. |
| `CombatMath/BodyPart.cs` | Runtime de una extremidad (análogo a `CreatureRuntime`). `ApplyDamage(int)` aplica sec 29-30: clamp a 0, marca `EstadoDanado` una única vez al cruzar, devuelve si acaba de cruzar (para disparar el cambio visual una sola vez). |
| `BodyPartOptionUI.cs` | Sprite de extremidad clickeable sobre el enemigo (Prompt 18). Hit test por alpha (atraviesa zonas transparentes), brillo intermitente vía `Shadow` blanco cuya alpha oscila con `Mathf.Sin`, cursor personalizado en hover (mismo patrón que `MoveOptionUI`). |
| `EnemyBodyPartsView.cs` | Instancia un `BodyPartOptionUI` por cada `BodyPart` de la criatura enemiga actual (mismo patrón que `BattleUIManager` usa para `MoveOptionUI`). Si la criatura no tiene `bodyParts`, muestra el sprite único de siempre (`CreatureBattleView`) en su lugar. |
| `Tests/EditMode/*` | Pruebas NUnit (ver §6). |

## 2. Por qué `CombatMath` es un asmdef separado

`AccuracyChecker`, `DamageCalculator`, `BodyPart`, `BodyPartDefinition` y
`RandomProvider` viven en `Assets/Scripts/Combat/CombatMath/` con su propio
`CombatMath.asmdef` (sin referencias a nada fuera de `UnityEngine`). Motivo: Unity
compila los asmdef **antes** que el ensamblado implícito `Assembly-CSharp` (donde
viven `CombatManager`, `CreatureData`, etc., que a su vez dependen de `InventorySO`
en `MenuInventary`) — un asmdef **no puede** referenciar `Assembly-CSharp` porque
compila antes que él. Como las pruebas EditMode (`CombatSystem.EditModeTests.asmdef`)
sí necesitan referenciar el código bajo prueba, esas 5 clases puramente matemáticas
(sin dependencias cruzadas) se aislaron en su propio asmdef, y el asmdef de pruebas
referencia `CombatMath` en vez de `Assembly-CSharp`. El resto de scripts de Combat
(`CombatManager`, `BattleUIManager`, `EnemyBodyPartsView`, `BodyPartOptionUI`, etc.)
se quedan sueltos en `Assembly-CSharp` como siempre — `Assembly-CSharp` referencia
automáticamente todos los asmdef del proyecto, así que pueden usar `BodyPart` /
`DamageCalculator` sin problema.

**No** se le dio su propio asmdef a todo `Assets/Scripts/Combat` porque
`CombatManager.cs` usa `InventorySO` (en `Assets/Scripts/MenuInventary/Model/`), que
vive en `Assembly-CSharp` — un asmdef para todo Combat habría necesitado también un
asmdef para MenuInventary (y posiblemente Dialogue), un refactor mucho más invasivo
que no correspondía a este pedido.

## 3. Fórmula de daño y decisión de diseño: `porcentajeAtaque`/`porcentajeAcertividad` son de la EXTREMIDAD, no del movimiento

La spec (sec 2) lista `porcentajeAtaque` y `porcentajeAcertividad` como campos **de
cada extremidad**, no del `MoveData` ni del atacante. Esto es intencional y se
verificó releyendo sec 28 ("Las partes críticas deben tener acertividad baja o una
ventana de QTE exigente para mantener el balance"): cada parte define qué tan
"crítica" es (`porcentajeAtaque >= 100` habilita el golpe de derrota instantánea) y
qué tan difícil es acertarle (`porcentajeAcertividad` bajo en partes críticas).

`danoBase` (el otro factor de la fórmula) se calcula en
`CombatManager.ExecuteBodyPartAttack` como `playerRuntime.Attack + move.power`
(igual que el `baseDamage` de `DamageEffect`, pero **sin restar Defense** — la spec
no lo menciona en la fórmula de la sec 26, así que no se inventó esa resta).
`MoveData.accuracy` (el campo ya existente, nunca leído — ver hallazgo en
`COMBAT_SYSTEM_ANALYSIS.md`) sigue sin usarse en este camino: la acertividad que
importa aquí es la de la extremidad objetivo, no la del movimiento.

`DamageEffect`/`MoveData.accuracy` **no se tocaron** — `EnemyTurn()` (el enemigo
ataca al jugador) y cualquier criatura sin `bodyParts` siguen exactamente igual que
antes. El nuevo camino (`CombatManager.ExecuteBodyPartAttack`) solo se activa en
`PlayerTurn()` cuando `enemyRuntime.data.bodyParts` tiene elementos.

## 4. Flujo de un ataque con extremidades (`CombatManager`)

```
PlayerTurn()
  ... (selección de movimiento, QTE — sin cambios) ...
  si QTE exitoso:
    si enemyBodyPartsRuntime tiene elementos:
        ExecuteBodyPartAttack(selectedMove)   // <- nuevo
    si no:
        selectedMove.effect.Execute(...)       // DamageEffect de siempre

ExecuteBodyPartAttack(move):
  target = enemyBodyPartsRuntime[selectedBodyPartIndex]
  danoBase = max(1, playerRuntime.Attack + move.power)
  result = DamageCalculator.Calculate(true, danoBase, target.PorcentajeAtaque,
                                       target.PorcentajeAcertividad,
                                       enemyRuntime.CurrentHP, UnityRandomProvider)
  si !result.ataqueImpacta: mensaje "missed", terminar (sin aplicar nada)
  si result.esCritico: mensaje "Critical hit!"
  justCrossedToZero = target.ApplyDamage(result.danoFinalEntero)
  enemyRuntime.TakeDamage(round(result.danoFinalEntero * multiplicadorVidaGlobal))
  RefreshBattleUI()  // HP global
  mensaje con vida restante de la parte
  si justCrossedToZero: battleUI.MarkEnemyBodyPartDamaged(index, target.ReferenciaVisualDanada)
  // La derrota (vidaGlobal <= 0) la emite EndBattleSequence, una sola vez — no se repite aquí.
```

`multiplicadorVidaGlobal` es un campo serializado nuevo en `CombatManager`
(`Header("Body Parts")`, default `1.0`, spec sec 31).

## 5. Selección de objetivo (Prompt 18)

- `BattleUIManager.OnBodyPartClicked(int index)` — evento nuevo, mismo patrón que
  `OnMoveClicked`. `CombatManager.HandleBodyPartClicked` → `SelectBodyPartTarget`
  actualiza `selectedBodyPartIndex` y llama a `battleUI.SelectEnemyBodyPart(part, index)`,
  que a su vez le dice a `EnemyBodyPartsView.SelectIndex` cuál parte debe brillar
  (`BodyPartOptionUI.SetSelected(true)`, único brillo activo a la vez) y actualiza
  `targetIndicatorText` ("Objetivo: Cabeza (¡critico!)" / "Objetivo: Cuerpo").
- Al iniciar la batalla (`CombatManager.StartBattle`), si el enemigo tiene
  `bodyParts`, se selecciona automáticamente el índice `0` (en `Jack Malo.asset` eso
  es "Cuerpo" — el jugador tiene que hacer clic deliberadamente en la cabeza para
  apuntar al golpe crítico).
- El brillo intermitente usa un componente `Shadow` (blanco) en el mismo GameObject
  que el `Image` de la parte — no una imagen de glow nueva. `BodyPartOptionUI.Update()`
  oscila `Shadow.effectColor.a` con `Mathf.Sin(Time.unscaledTime * blinkSpeed)` entre
  `minGlowAlpha` y `maxGlowAlpha` mientras `isSelected`; en 0 cuando no.
- El cursor (`pointerCursor`/`pointerCursorHotspot`) sigue el mismo patrón que
  `MoveOptionUI`: se asigna en `OnPointerEnter`, se limpia en `OnPointerExit`.
- `Image.alphaHitTestMinimumThreshold = 0.1` en `BodyPartOptionUI.Awake()` — permite
  que el clic atraviese las zonas transparentes del sprite (la textura debe tener
  **Read/Write Enabled** en el importer, ver §7).

## 6. Pruebas (`Tests/EditMode`)

`CombatSystem.EditModeTests.asmdef` referencia `CombatMath` + `UnityEngine.TestRunner`
+ `UnityEditor.TestRunner`, con `nunit.framework.dll` precompilado y
`defineConstraints: ["UNITY_INCLUDE_TESTS"]` (patrón estándar de Unity Test
Framework). `FakeRandomProvider` (cola de valores predeterminados + contador de
llamadas) permite comprobar tanto el resultado como que `valorImpacto`/
`variacionAleatoria` se generan **una única vez** por ataque.

- `AccuracyCheckerTests` — acertividad 0/20/80/100 (bordes exactos e inmediatamente
  fuera de rango), QTE fallido no genera `valorImpacto`, clamp 0-100.
- `DamageCalculatorTests` — ejemplo `100 × 0.6 × 0.8 × 1.05 = 50.4` (→ 50 entero),
  QTE fallido, acertividad fallida, daño nunca negativo, crítico (éxito, QTE
  fallido, acertividad fallida), `porcentajeAtaque < 100` nunca es crítico.
- `BodyPartTests` — vida no baja de 0, `EstadoDanado` se marca una única vez al
  cruzar (ataques posteriores no lo repiten), `EsParteCritica` solo con
  `porcentajeAtaque >= 100`.

**Estado confirmado (2026-08-01, tras liberar espacio en disco)**: las 24 pruebas
corren y pasan (`run_tests mode=EditMode` → `24 total, 24 passed, 0 failed`).

## 7. `Jack Malo.asset` (usado por `Enemy Spider 1` / NPC "Enemy Spider")

Se le agregó `bodyParts` a mano (editando el YAML directamente — ver §8 sobre por
qué no se pudo usar la herramienta del MCP para esto) y se actualizó
`frontSprite`/`backSprite` a `body.png` (antes reusaba el sprite de "Jack", el
UVGmon inicial del jugador — un placeholder). `creatureName` pasó de "Jack Malo" a
"Arana" (el nombre interno del archivo/asset no cambió, solo el campo).

| idParte | nombreParte | vidaMaxima | porcentajeAtaque | porcentajeAcertividad | sprite normal | sprite dañado |
|---|---|---|---|---|---|---|
| body | Cuerpo | 70 | 55 | 85 | `body.png` | `body_damaged.png` |
| head | Cabeza | 30 | **100** (crítico) | 30 (difícil de acertar, balance) | `head.png` | — (sin variante, `referenciaVisualDanada` queda `null`; `BodyPartOptionUI`/`EnemyBodyPartsView` lo manejan sin romperse — Prompt 17 "manejar referencias nulas") |

`head.png`, `body.png`, `body_damaged.png` están en `Assets/Enemys/Spider/PARTES/`,
los tres con el **mismo lienzo** (280×172 px) — están pre-alineados para
superponerse directamente en `anchoredPosition (0,0)`, sin offsets manuales (por
eso `anchoredPosition` de ambas partes en el asset es `(0,0)`; el orden de
instanciación en `EnemyBodyPartsView.Setup` — cuerpo primero, cabeza después — deja
la cabeza como sibling más alto, encima del cuerpo).

## 8. Estado de la integración en la escena (`jardinconocimiento`) y bloqueo de entorno

### Lo que sí se hizo por MCP en esta sesión

- `Assets/Enemys/Spider/PARTES/{head,body,body_damaged}.png`: se activó **Read/Write
  Enabled** en el importer (`manage_texture set_import_settings`) — necesario para
  que `Image.alphaHitTestMinimumThreshold` funcione en `BodyPartOptionUI`.
- En la escena, bajo `BattleUI` (mismo nivel que `jackmalo`, `MoveOptionsContainer`,
  `MoveOptionTemplate`):
  - **`EnemyBodyPartsContainer`** — `RectTransform`, `anchoredPosition (468, 205)`,
    `sizeDelta (100, 100)`, `localScale 3.4397` (igual que `jackmalo`, el
    `CreatureBattleView` del enemigo, para que las partes queden en el mismo lugar
    en pantalla). Será el `partsContainer` de `EnemyBodyPartsView`.
  - **`BodyPartOptionTemplate`** — `RectTransform` (`sizeDelta (163, 100)`,
    preserva el aspecto 280:172 del lienzo de las partes), `CanvasRenderer`,
    `Image` (`raycastTarget=true`, `alphaHitTestMinimumThreshold=0.1`), inactivo
    (`SetActive(false)` vía `execute_code`, mismo workaround que QTE/Capture
    documentan porque `BattleUI` está inactivo — ver gotcha en `CLAUDE.md`). Será
    el `partOptionPrefab` de `EnemyBodyPartsView` (clonado una vez por parte, igual
    que `MoveOptionTemplate`/`MoveOptionUI`).
  - **`TargetIndicatorText`** — `TextMeshProUGUI`, encima de `MoveOptionsContainer`
    (`anchoredPosition (41.8, -205)`, mismo `localScale 2.789` que los demás hijos
    directos de `BattleUI`). Será el `targetIndicatorText` de `BattleUIManager`.
  - `localScale` se fijó explícitamente en los tres (vía `set_property`, no se dejó
    el valor por defecto) porque `BattleUI` está inactivo — mismo bug de escala 0
    documentado en `QTE_SYSTEM.md`/`CAPTURE_SYSTEM.md`.
  - Escena guardada (`manage_scene action=save`).
- `Assets/Scripts/Combat/Jack Malo.asset`: `bodyParts` poblado a mano (§7).

### Wiring completado y probado en Play mode (2026-08-01, tras liberar espacio en disco)

Una vez `com.unity.burst` pudo instalarse (`manage_packages action=resolve_packages`
tras liberar espacio — ver §8 abajo) y `Assembly-CSharp` compiló, se terminó:

1. `BodyPartOptionUI` + `Shadow` agregados a `BodyPartOptionTemplate` (vía
   `execute_code` + `Resources.FindObjectsOfTypeAll` porque `BattleUI` sigue
   inactivo — `manage_components action=add` falla igual que `modify`/`delete` en
   ese árbol, ver gotcha en `CLAUDE.md`). `pointerCursor` =
   `Assets/Sprites/UI/cursor_pointer.png`.
2. `EnemyBodyPartsView` agregado a `EnemyBodyPartsContainer` (mismo workaround) y
   wireado: `partsContainer` = sí mismo, `partOptionPrefab` = `BodyPartOptionTemplate`,
   `defaultEnemyView` = `CreatureBattleView` de `jackmalo`.
3. `BattleUIManager.enemyBodyPartsView`/`targetIndicatorText` wireados (esto sí
   funcionó con `manage_components action=set_property` normal — `BattleUIManager`
   no vive dentro del árbol inactivo de `BattleUI`).
4. Compilación limpia (`read_console` sin errores), escena guardada.
5. **Probado en Play mode** llamando `CombatManager.Instance.StartBattle(...)`
   directamente contra el NPC "Enemy Spider 1" vía `execute_code` (evita la
   dependencia del diálogo/click del jugador para la prueba). Confirmado:
   - `EnemyBodyPartsContainer` pasa a `activeInHierarchy=true` con 2 clones
     (`BodyPartOptionTemplate(Clone)`, índices 0="body"/1="head"), `jackmalo`
     (la vista de sprite único) pasa a `active=false` automáticamente.
   - Clon 0: sprite `body.png`, `Shadow.effectColor.a≈1` (brillando — es el
     objetivo por defecto). Clon 1: sprite `head.png`, dibujado como sibling
     posterior (encima), `Shadow.effectColor.a=0` (sin brillo, no seleccionado).
   - `TargetIndicatorText.text` = `"Objetivo: Cuerpo"` al iniciar.
   - Simulando un clic real sobre el clon de la cabeza
     (`ExecuteEvents.Execute(..., pointerClickHandler)`, el mismo pipeline que un
     clic de mouse real): el brillo se movió al clon de la cabeza, el de cuerpo se
     apagó, y `TargetIndicatorText.text` pasó a
     `"Objetivo: Cabeza (¡critico!)"` — confirma selección + detección de parte
     crítica end-to-end.
   - Cero errores en consola durante todo el flujo (entrar a Play, iniciar
     batalla, clic simulado, salir de Play).

### Por qué quedó bloqueado: `Assembly-CSharp` no compiló en ningún momento de esta sesión

Diagnóstico (verificado, no es una suposición):

- `com.unity.burst@1.8.18` no se pudo instalar: `ENOSPC: no space left on device`
  al copiar su `.Runtime\libburst-llvm-10.dylib` — el disco `C:` tenía entre **~190
  MB y ~450 MB libres** durante la sesión (fluctuante; `Get-PSDrive C`).
- Sin Burst, tres paquetes que lo referencian no compilan (`error CS0246:
  BurstCompileAttribute could not be found`): `com.unity.2d.aseprite@1.1.6`
  (Editor), `com.unity.2d.animation@7.1.1` (**Runtime** — este es el que importa:
  `SpriteSkinUtility.cs`), `com.unity.2d.psdimporter@6.0.9` (Editor).
- `Assembly-CSharp` (el ensamblado implícito con **todo** el código suelto del
  proyecto — todo `Assets/Scripts` salvo `CombatMath`/`Tests`) referencia
  implícitamente todos los ensamblados runtime del proyecto, incluido
  `Unity.2D.Animation.Runtime`. Como ese falla, `Assembly-CSharp` nunca llega a
  compilar — **no existe `Assembly-CSharp.dll` en `Library/ScriptAssemblies` en
  ningún momento de la sesión**, ni antes ni después de mis cambios.
- Consecuencia comprobada: `AssetDatabase.LoadAssetAtPath` devuelve `null` para
  **cualquier** asset `CreatureData` (se probó con `Jack.asset`, sin tocar, y con
  `Jack Malo.asset`) — nada que dependa de un tipo definido en `Assembly-CSharp`
  puede cargarse. Por la misma razón `manage_scriptable_object` reporta
  `target_not_found` para esos assets, el Test Runner no descubre ninguna prueba
  (`mcpforunity://tests` devuelve un nodo raíz vacío tanto en EditMode como
  PlayMode), y componentes ya existentes en la escena como `MoveOptionUI` no
  aparecen en el listado de componentes de `MoveOptionTemplate`/`Label` aunque sí
  estén en el `.unity` guardado en disco.
- Esto **no lo causó este trabajo** — es un problema de espacio en disco/resolución
  de paquetes preexistente en el entorno, y afecta a todo el proyecto (no solo a
  Combat): ningún script del juego, nuevo o viejo, está realmente cargado en el
  Editor en este momento.

**Cómo se desbloqueó (confirmado)**: el usuario liberó espacio en `C:` (quedaron
~18 GB libres). Con más espacio, `manage_packages action=resolve_packages` forzó a
Unity a reintentar la resolución de paquetes — **no alcanzó con más espacio libre
solo**; el intento de resolución anterior había quedado en un estado fallido que no
se reintentaba solo con `refresh_unity`, hubo que forzarlo explícitamente.
Después de eso, `Assembly-CSharp.dll` apareció en `Library/ScriptAssemblies` en el
siguiente compile, `AssetDatabase.LoadAssetAtPath` volvió a funcionar para
`CreatureData`, y las 24 pruebas EditMode corrieron y pasaron.

## 9. Correcciones de UX (2026-08-01, sesión posterior)

Cuatro pedidos del usuario tras probar el sistema en juego:

1. **Feedback de golpe (parpadeo de alfa)**: `BodyPartOptionUI.PlayHitFlash()` —
   corrutina que oscila `image.color.a` con `Mathf.Sin` (mismo patrón que el brillo
   de selección del `Shadow`, pero sobre el `Image` en vez del `Shadow`) durante
   `hitFlashDuration` (0.5s por defecto). `EnemyBodyPartsView.PlayHitFlash(index)` →
   `BattleUIManager.PlayEnemyBodyPartHitFlash(index)` → llamado desde
   `CombatManager.ExecuteBodyPartAttack` justo después de `RefreshBattleUI()`, en
   **todo** golpe que impacta (no solo el que destruye la parte).
2. **Orden de dibujo**: `EnemyBodyPartsContainer` estaba en el sibling index 8 de
   `BattleUI`, **por encima** de `QTE` (5) y `Capture` (6) — los sprites de
   extremidades tapaban la pantalla negra del QTE. Se movió a sibling index 3
   (justo después de `jackmalo`, antes de `BattleMessageText`/
   `MoveOptionsContainer`/`QTE`/`Capture`). Sibling order = draw order (ver gotcha
   en `CLAUDE.md`): ahora `EnemyBodyPartsContainer` queda debajo de todo lo demás.
3. **`TargetIndicatorText` tapaba el primer botón de movimiento**: pese a que en
   coordenadas locales el texto (`anchoredPosition.y=-205`) y el contenedor de
   movimientos (`anchoredPosition.y=-265`, `sizeDelta.y=90`) parecían solo tocarse
   en el borde, midiendo con `RectTransform.GetWorldCorners` en Play mode (tras
   `LayoutRebuilder.ForceRebuildLayoutImmediate` sobre `MoveOptionsContainer`, que
   tiene un `VerticalLayoutGroup`) el texto quedaba **encima del botón 0**
   (rango Y mundial del texto `[110, 163]` vs. botón 0 `[108, 171]` — solapamiento
   casi total), bloqueando el click. Se subió `TargetIndicatorText.anchoredPosition.y`
   a `-70` (medido empíricamente con `GetWorldCorners`, no por cálculo analítico —
   la pendiente local→mundo no coincidía entre hermanos por razones no
   diagnosticadas, así que se iteró midiendo directamente hasta confirmar
   `worldMinY` del texto por encima de `worldMaxY` del contenedor de botones con
   margen). Verificado sin overlap contra `MoveOptionsContainer` ni
   `EnemyBodyPartsContainer`.
4. **Extremidad a 0 HP ya no se puede reseleccionar**: `BodyPart.IsAlive` (nueva
   propiedad, `VidaActual > 0`) + `BodyPartOptionUI.SetInteractable(bool)` (ignora
   `OnPointerClick`/cursor cuando `false`, y fuerza `SetSelected(false)`).
   `EnemyBodyPartsView.MarkDamaged` ahora llama `SetInteractable(false)` sobre la
   parte destruida (antes solo cambiaba el sprite). Si la parte que acaba de morir
   era el objetivo seleccionado, `CombatManager.ExecuteBodyPartAttack` reasigna
   automáticamente el objetivo a la siguiente parte viva
   (`FindNextAliveBodyPartIndex`) para no dejar el ataque apuntando a una
   extremidad muerta.

**Verificación (Play mode, sin click real de usuario)**: mismo patrón que el resto
del documento — `ExecuteBodyPartAttack` invocado por reflexión y bombeado con
`MoveNext()` manualmente (evita depender de tiempo real/frames, que no avanzan
cuando el Editor no tiene foco — `Time.frameCount` se quedó en 4 esperando
`WaitForSeconds` real vía `StartCoroutine` normal). Con eso: la cabeza (30 HP) murió
en el primer intento, `selectedBodyPartIndex` pasó de 1 a 0 automáticamente. Clic
simulado (`ExecuteEvents.Execute(..., pointerClickHandler)`) sobre el clon de la
cabeza muerta no cambió `selectedBodyPartIndex` (bloqueado); el mismo clic sobre el
cuerpo (vivo) sí lo cambió (control positivo). `PlayHitFlash()` se invocó sin
excepciones. Cero errores/warnings nuevos en consola. Cambios de escena (sibling
index, `anchoredPosition`) hechos en Edit mode (no en Play mode, que no persiste) y
guardados con `manage_scene action=save`.

## 10. Correcciones de UX (2026-08-02, tras probar el sistema completo en juego)

Tres pedidos del usuario (ver `PROMPT.md` — el texto libre al inicio, no un prompt
numerado — y confirmados con pruebas empíricas en Play mode, no solo lectura de
código):

1. **"No puedo seleccionar la cabeza, solo el cuerpo, en el primer ataque"**: **no
   era un problema de los sprites superpuestos** — se verificó con
   `execute_code` que `head.png`/`body.png` comparten el mismo lienzo 280×172 y que
   los "ojos rojos" caen en el **mismo bounding box exacto** en ambas imágenes
   (`x[87-133] y[63-84]`), es decir que `head.png` sí está perfectamente
   pre-alineado sobre la cabeza real de `body.png` (la sospecha inicial del usuario
   de que los assets se solapaban mal era una impresión visual, no el bug real). El
   verdadero problema era de **flujo**: `CombatManager.StartBattle` auto-seleccionaba
   `selectedBodyPartIndex = 0` (cuerpo) y **nada bloqueaba los botones de
   movimiento**, así que un jugador que atacaba sin pensar en elegir objetivo
   primero (el flujo natural en cualquier RPG) siempre terminaba golpeando el
   cuerpo por default, sin haber elegido conscientemente. Esto coincide
   exactamente con el pedido #2 del usuario (orden explícito: parte → movimiento →
   QTE), así que ambos pedidos se resolvieron con el mismo cambio.
2. **Orden obligatorio parte→movimiento→QTE**: `CombatManager` ahora trackea
   `bodyPartConfirmedThisTurn` (reseteado a `false` al inicio de cada `PlayerTurn`,
   junto con `battleUI.ClearEnemyBodyPartSelection()` que apaga el brillo y pone el
   texto "Selecciona una parte del enemigo para atacar", y
   `battleUI.SetMoveSelectionLocked(true)`). `HandleBodyPartClicked` (guardado con
   `isPlayerTurn && !playerHasChosen`, antes no tenía ningún guard) marca
   `bodyPartConfirmedThisTurn = true` y llama `SetMoveSelectionLocked(false)` recién
   ahí. `IsValidMoveIndex` también lo comprueba como red de seguridad por si el
   guard visual se saltea. `MoveOptionUI` ganó `SetInteractable(bool)` (mismo
   patrón que `BodyPartOptionUI`: ignora hover/click y atenúa el color de fondo
   —`disabledColor`— cuando está bloqueado) y `BattleUIManager.SetMoveSelectionLocked`
   lo aplica a todos los slots (incluyendo los que se creen después vía
   `RebuildMoveOptions`, que ahora respeta el flag `moveSelectionLocked`). Esto se
   re-exige **cada turno**, no solo el primero (una criatura enemiga puede perder
   una extremidad a mitad de combate; forzar una elección consciente cada vez evita
   apuntar por inercia a una parte que ya no es la que el jugador quiere).
3. **Cursor incorrecto en la selección de extremidad**: `BodyPartOptionTemplate`
   apuntaba a `Assets/Sprites/UI/cursor_pointer.png` (guion bajo — un mero contorno
   sin rellenar, un placeholder), mientras que `MoveOptionUI` ya usaba
   `Assets/Sprites/UI/cursor-pointer.png` (guion medio — la mano apuntando rellena
   que pidió el usuario) con hotspot `(9, 4)`. Se igualó `BodyPartOptionUI.pointerCursor`
   / `pointerCursorHotspot` al mismo asset y hotspot que `MoveOptionUI`.
4. **Flash de golpe: filtro blanco en vez de fundido de alfa**: `PlayHitFlash()`
   antes bajaba el alfa del *propio* sprite (parpadeo de transparencia, no un
   "filtro blanco" real). Se agregó un `Image` hijo nuevo, `FlashOverlay`
   (`anchorMin (0,0)`/`anchorMax (1,1)` estirado, `raycastTarget=false`, creado bajo
   `BodyPartOptionTemplate` vía `execute_code` por el mismo motivo de siempre —
   `BattleUI` inactivo, ver gotcha en `CLAUDE.md`), wireado a
   `BodyPartOptionUI.flashOverlayImage`. `SetSprite()` ahora también genera (y
   cachea en un `Dictionary<Sprite,Sprite>` estático) una variante **blanca** del
   sprite actual: mismo canal alfa, RGB en blanco puro
   (`GetOrCreateWhiteSprite`, usa `Texture2D.GetPixels` sobre el rect del sprite —
   requiere Read/Write Enabled, ya activado, ver §8) y se la asigna al overlay.
   `PlayHitFlash()`/`HitFlashRoutine` ahora oscilan el **alfa del overlay** entre 0
   y `hitFlashMaxAlpha` (0.85 por defecto, antes `hitFlashMinAlpha` controlaba el
   piso del sprite original) con el mismo patrón sinusoidal de siempre — el sprite
   base ya no se toca, así que sus colores no se alteran entre parpadeos, solo se
   superpone (y desaparece) una silueta blanca semitransparente.

**Verificación (Play mode, 2026-08-02)**: batalla iniciada por reflexión contra
"Enemy Spider 1" (mismo patrón que el resto del documento). Confirmado con
`execute_code`: (a) ambos `MoveOptionUI` arrancan con `isInteractable=false` y
`targetIndicatorText` en el prompt de selección al iniciar la batalla; (b) clic
simulado sobre el clon de la cabeza cambia `selectedBodyPartIndex` a 1,
`bodyPartConfirmedThisTurn` a `true`, el texto a `"Objetivo: Cabeza (¡critico!)"` y
desbloquea `move[0].isInteractable`; (c) tras varios intentos de
`ExecuteBodyPartAttack` (la cabeza tiene 30% de acertividad, así que hubo fallos
antes del impacto — comportamiento esperado, no un bug) el golpe que impactó dejó
`flashOverlayImage.color` en `RGBA(1, 1, 1, 0.52)` con la corrutina todavía activa,
confirmando el filtro blanco real (antes habría sido el color original del sprite
con canal alfa reducido, nunca blanco puro). Cero errores nuevos en consola durante
todo el flujo. Cambios de escena (`pointerCursor`, `FlashOverlay` +
`flashOverlayImage`) hechos en Edit mode y guardados con `manage_scene
action=save` antes de entrar a Play mode para probar.

## 11. Hover y seleccion usan el mismo filtro blanco que el golpe, mas lento (2026-08-02)

Pedido del usuario tras probar el punto 10: quería que pasar el cursor sobre una
parte (antes de hacer click) y tener una parte seleccionada usaran **el mismo**
filtro blanco que el flash de golpe (`flashOverlayImage`, ver §10.4) — no el
`Shadow` blanco intermitente que existía desde el Prompt 18 original —, solo que
más lento y en bucle mientras dura el estado (a diferencia del golpe, que es un
pulso único y rápido).

- `BodyPartOptionUI.Update()` ahora es el único lugar que escribe
  `flashOverlayImage.color` para hover/selección: si `hitFlashRoutine` está corriendo
  no toca nada (el pulso de golpe tiene prioridad exclusiva, evita que ambos bucles
  se pisen); si no, y `isInteractable && (isSelected || isHovered)`, oscila el alfa
  entre 0 y `ambientMaxAlpha` (0.6 por defecto) con `Mathf.Sin(Time.unscaledTime *
  ambientBlinkSpeed)` (`ambientBlinkSpeed = 4`, notablemente más lento que
  `hitFlashSpeed = 18` del golpe); si no, fuerza el overlay a alfa 0.
- `isHovered` es un campo nuevo, seteado en `OnPointerEnter`/`OnPointerExit` (y
  forzado a `false` en `SetInteractable(false)`, igual que `isSelected`).
- El `Shadow` (`selectionGlow`) que antes hacía el brillo de selección quedó
  **neutralizado** (alfa forzada a 0 una sola vez en `Awake`, sin lógica en
  `Update`) en vez de eliminado del GameObject — así una escena vieja que todavía
  tenga el componente `Shadow` de una sesión anterior no deja un borde fijo sin
  querer, sin tener que editar la escena para borrar el componente. Los campos
  `glowColor`/`blinkSpeed`/`minGlowAlpha`/`maxGlowAlpha`/`glowDistance` se
  eliminaron del script (reemplazados por `ambientBlinkSpeed`/`ambientMaxAlpha`);
  al guardar la escena Unity descarta solo esos valores serializados viejos, sin
  romper nada.

**Verificación (Play mode, 2026-08-02)**: mismo patrón de batalla por reflexión que
el resto del documento. Nota de entorno: en esta sesión de pruebas
`Time.unscaledTime` quedó congelado entre llamadas separadas de `execute_code`
(Editor sin foco → el Player Loop de Play mode no avanza fotogramas reales entre
comandos MCP, mismo fenómeno que ya documentaba §9 con `WaitForSeconds`), así que
no se pudo observar la animación cambiando cuadro a cuadro sondeando en vivo; en
su lugar se invocó `BodyPartOptionUI.Update()` directamente por reflexión (mismo
`Time.unscaledTime` congelado, pero validando la fórmula) con la parte "cuerpo" en
estado hovered-no-seleccionado y la parte "cabeza" en estado seleccionada-no-hovered
al mismo instante: **ambas devolvieron exactamente el mismo alfa**
(`RGBA(1,1,1,0.594)`), confirmando que hover y selección producen el efecto
idéntico. Con ambos estados apagados (`isHovered=false`, `SetSelected(false)`) el
overlay volvió a `RGBA(1,1,1,0)` en los dos. Compilación limpia
(`refresh_unity` sin errores), cambios guardados con `manage_scene action=save`
antes de la prueba.

Efecto colateral no relacionado, encontrado durante esta prueba: `PlayerParty` usa
un singleton estático (`Instance`) que puede quedar apuntando a un objeto viejo si
se entra/sale de Play mode varias veces sin recarga de dominio (Editor con "Reload
Domain" desactivado en Enter Play Mode Settings, o sin foco) — el `Player` activo
más reciente se auto-destruye pensando que es un duplicado, dejando `party` vacío
(`GetLeadCreature()` devuelve `null`, `CombatManager.StartBattle` aborta con
`"Battle could not start because one side has no usable creature."`). Esto **no es
un bug de este trabajo** ni del flujo normal de juego (un jugador real solo entra a
Play mode una vez por sesión); solo aparece automatizando pruebas repetidas por MCP
en la misma sesión de Editor. No se tocó `PlayerParty.cs` — quedó anotado acá por si
vuelve a aparecer en una futura sesión de pruebas.
