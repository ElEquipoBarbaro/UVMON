# Sistema de Captura — `Assets/Scripts/Combat/Capture`

Implementado en la escena `jardinconocimiento`, integrado con el sistema descrito en
`../COMBAT_SYSTEM_ANALYSIS.md` y con el inventario (`../../MenuInventary/MENU_INVENTORY_ANALYSIS.md`).
Se activa automáticamente al derrotar a un UVGmon enemigo configurado como capturable:
el jugador debe hacer clic para soltar un frasco sobre un círculo indicador que se mueve
y se encoge; si el punto de impacto queda dentro del círculo, la captura es exitosa.

Especificación completa (mecánica, fórmulas, estados y motivos de fallo) en
`claudeInstructions.md/CAPTURE_SYSTEM_INSTRUCCIONS.md`.

## 1. Archivos

| Archivo | Rol |
|---|---|
| `CaptureData.cs` | `ScriptableObject` con los parámetros globales del desafío (radios, velocidades, tiempo, tolerancia). |
| `CaptureController.cs` | `MonoBehaviour` que corre la máquina de estados: mueve/encoge el indicador, sigue el cursor con el frasco, valida el impacto por geometría. |
| `CaptureFailReason.cs` | Enum de motivos de fallo (`NoJar`, `Timeout`, `IndicatorTooSmall`, `MissedIndicator`, etc). |
| `CaptureResult.cs` | Struct de resultado (`success`, `failureReason`, `capturedUVGmon`, `jarConsumed`, `impactDistance`, `indicatorRadiusAtImpact`). |
| `Assets/DATA/Capture/CaptureData_Default.asset` | Instancia de `CaptureData` usada por defecto en batalla. |
| `Assets/ScriptableObjects/Items/Frasco.asset` | `ItemSO` del frasco de captura (`Category = Capture`, usa `Assets/Items/frasco.png`). |

## 2. `porcentajeCaptura` (en `CreatureData`)

Cada `CreatureData` tiene `isCapturable` (bool) y `porcentajeCaptura` (0-100). **No es
una probabilidad** — solo ajusta la dificultad manual del desafío: alto = círculo
inicial más grande y reducción más lenta (más fácil); bajo = círculo más chico y
reducción más rápida (más difícil). Fórmulas exactas en la instrucción original,
sección 5-8.

En el proyecto de ejemplo están marcados como capturables `Jack Malo.asset`
(`porcentajeCaptura = 55`, usado por `Enemy Spider 1`) y `Jack.asset`
(`porcentajeCaptura = 40`, usado por `Enemy Spider 2`), que son los `CreatureData`
que usan los tres NPC de la escena de prueba.

## 3. Consumo del frasco (`InventorySO`)

Se agregó `ItemCategory.Capture` y dos métodos a `InventorySO`:

- `GetCaptureJarCount()` — suma la cantidad de todos los slots con `Category == Capture`.
- `TryConsumeCaptureJar()` — busca el primer slot con un frasco y le resta 1 vía
  `RemoveItem`. Devuelve `false` si no hay ninguno.

El frasco se consume **una sola vez**, en el primer clic válido que inicia la caída
(no al abrir la interfaz, no si el intento termina antes de hacer clic). Si el jugador
no tiene frascos, la captura falla con `NoJar` sin mover el círculo ni consumir nada.

## 4. Flujo (`CaptureController.RunCapture`)

```
CheckingInventory  -> Failure(NoJar) si GetCaptureJarCount() <= 0
Preparing          -> calcula radioInicial/velocidadReduccion a partir de pCaptura;
                       si el área es demasiado chica para el radio, lo recorta;
                       si ni así entra -> Failure(InvalidConfiguration)
Active             -> el indicador se mueve hacia destinos aleatorios (MoveTowards)
                       dentro del área circular (en las dos dimensiones, no solo
                       izquierda/derecha) y se encoge cada frame; el frasco se mueve
                       como si estuviera sobre un riel fijo a la circunferencia del
                       área, siguiendo el ángulo del cursor respecto al centro.
                       - clic izquierdo -> intenta consumir el frasco -> Dropping
                       - radio <= radioMinimoPermitido sin clic -> Failure(IndicatorTooSmall)
                       - tiempoTranscurrido >= tiempoMaximoCaptura -> Failure(Timeout)
Dropping           -> se bloquea el ángulo del frasco (su punto en el riel al momento
                       del clic); viaja en línea recta desde ese punto hacia el centro
                       del área en 'duracionCaidaFrasco' segundos. El círculo indicador
                       sigue moviéndose y encogiéndose durante el trayecto (regla
                       elegida y aplicada de forma consistente). Cada frame se compara
                       Distance(posicionFrasco, posicionIndicador) contra radioActual +
                       toleranciaImpacto: el impacto se resuelve apenas el frasco toca
                       el círculo en cualquier punto del trayecto, no recién al llegar
                       al centro (ver §7).
Resolving          -> si no hubo contacto durante el trayecto, se hace la misma
                       comprobación una última vez contra la posición final (el
                       centro del área). Sin aleatoriedad adicional ni segunda
                       comprobación más allá de esta.
Success / Failure  -> muestra "¡CAPTURADO!" / "¡ESCAPO!" ~0.9s y cierra el overlay.
```

El punto de impacto del frasco es el `anchoredPosition` de su `RectTransform`, cuyo
`pivot` está en `(0.5, 0.5)` (centro) — al moverse radialmente sobre el área en vez de
caer verticalmente, no hay un "borde inferior del objeto" que tenga sentido físico;
se usa el centro del frasco. Indicador, frasco y el anillo delimitador (`AreaBoundary`)
son todos hijos directos de `CaptureArea`, así que sus `anchoredPosition` están en el
mismo espacio de coordenadas (con origen en el centro del área) y son comparables
directamente.

### Geometría del área circular

`CaptureArea` es un `RectTransform` cuadrado (300x300); `areaRadius` se calcula como
`Mathf.Min(rect.width, rect.height) / 2f`. Tanto los destinos aleatorios del indicador
como el riel del frasco usan ese mismo radio:

- **Destinos del indicador** (`GenerateDestino`): se sortean con
  `Random.insideUnitCircle * maxDestinoRadius`, donde
  `maxDestinoRadius = areaRadius - radioActual - margenArea` — así el círculo nunca se
  sale del área delimitada, en cualquier dirección (arriba, abajo, diagonal, etc), no
  solo izquierda/derecha.
- **Riel del frasco** (`UpdateJarRail`): se calcula el ángulo del cursor respecto al
  centro de `CaptureArea` (`Atan2(localPoint.y, localPoint.x)`) y se posiciona el
  frasco a `areaRadius` de distancia en esa dirección — igual que una cuenta
  deslizándose sobre un riel circular. Al hacer clic se congela ese ángulo/posición y
  el frasco viaja en línea recta hacia el centro (`Vector2.zero`).

## 5. Integración con `CombatManager`

`CombatManager` tiene tres campos nuevos: `captureController`, `captureData` e
`inventoryData` (el mismo `InventorySO` que usa `InventoryController` del jugador).
En `EndBattleSequence()`, justo después de otorgar la XP por derrotar al enemigo:

```csharp
if (enemyRuntime.data.isCapturable)
    yield return RunCaptureSequence();
```

`RunCaptureSequence()` llama a `captureController.RunCapture(...)`, muestra un
mensaje de resultado acorde (`NoJar` tiene su propio mensaje) y, si `result.success`,
registra al UVGmon en el equipo del jugador con
`PlayerParty.Instance.AddCreature(enemyRuntime.data, enemyRuntime.Level)` — el mismo
método que ya usaba `PlayerParty` para agregar criaturas.

## 6. Jerarquía en la escena

```
BattleUI (Canvas)
└─ Capture                 (RectTransform stretch full screen, CaptureController)
   └─ Overlay               (Image negro semitransparente, alpha 0.75; oculto hasta RunCapture)
      ├─ CaptureArea         (RectTransform 300x300, define el área circular de captura)
      │  ├─ AreaBoundary     (Image blanca, sprite ring.png, 300x300 — la circunferencia
      │  │                    visible del área; sibling index 0 para quedar detrás)
      │  ├─ Indicator        (Image, mismo sprite ring.png, se mueve/encoge dentro del área)
      │  └─ Jar              (Image, sprite Assets/Items/frasco.png, pivot (0.5, 0.5),
      │                        se desplaza sobre la circunferencia como un riel)
      └─ CaptureResultText   (TMP, "¡CAPTURADO!"/"¡ESCAPO!")
```

Es un árbol hermano de `QTE` dentro de `BattleUI`, siguiendo la misma convención
(overlay que se activa/desactiva, `RingsContainer`-equivalente = `CaptureArea`).

## 7. Bug encontrado y corregido: el impacto solo se evaluaba al llegar al piso

Primera versión: `posicionFinCaida` era siempre `(lockedX, bounds.yMin)` (el borde
inferior fijo del área) y la distancia contra el indicador se calculaba una única vez,
al final de la caída. Como el indicador se mueve libremente en las dos dimensiones del
área (no solo en X), casi nunca está exactamente sobre esa fila del fondo en el
instante en que el frasco terminaba de caer — el jugador podía pasar justo por encima
del círculo durante la caída y aun así fallar, porque ese momento nunca se evaluaba.

**Fix:** el `Dropping` ahora compara la distancia frasco-indicador en **cada frame**
de la caída, no solo al final. El impacto se resuelve en el primer frame en que el
frasco entra en el radio del círculo (+ tolerancia); si nunca entra, se conserva el
chequeo final contra la posición de piso como respaldo. Esto no agrega aleatoriedad
ni una segunda comprobación real — sigue siendo la misma regla geométrica de
`distancia <= radioActual + toleranciaImpacto`, solo que evaluada continuamente
mientras el frasco está en movimiento en vez de una sola vez al final.

## 8. Nota sobre la posición inicial del frasco

La posición del frasco en el riel se recalcula desde cero cada vez que arranca un
intento (`jarRect.anchoredPosition = new Vector2(areaRadius, 0f)` en `Preparing`, y
`UpdateJarRail` la sobrescribe todos los frames en `Active` según el ángulo del
cursor). Nunca se lee la posición dejada por un intento anterior — si se hiciera, una
segunda captura en la misma sesión heredaría la posición final del centro del área
(donde terminó la caída anterior) en vez de reaparecer sobre el riel.

## 9. Bug encontrado y corregido: escala 0 en objetos creados por MCP

Mismo problema que el documentado en `../QTE/QTE_SYSTEM.md` §9: al crear todo el árbol
de `Capture` (`Capture`, `Overlay`, `CaptureArea`, `Indicator`, `Jar`,
`CaptureResultText`) con la herramienta de GameObjects del MCP de Unity mientras
`BattleUI` (su padre) estaba inactivo, los seis quedaron con `m_LocalScale = (0, 0, 0)`
en vez de `(1, 1, 1)`. Como la escala se propaga multiplicativamente, todo el
sistema de captura quedaba invisible en juego aunque toda la lógica y las
referencias estuvieran bien conectadas.

**Síntoma:** el overlay de captura nunca se veía, pese a que `CaptureController`
corría normalmente.

**Fix:** se forzó `localScale = Vector3.one` en los seis objetos vía `execute_code`
y se guardó la escena.

**Lección (reforzada):** al crear GameObjects UI por MCP mientras el Canvas padre
está inactivo, verificar (o forzar) `localScale = (1,1,1)` explícitamente en cada
objeto nuevo del árbol, no solo en la raíz — no asumir que el valor por defecto se
preserva correctamente al reparentar bajo un padre inactivo.
