# QTE de Ataque — `Assets/Scripts/Combat/QTE`

Implementado en la escena `jardinconocimiento`, integrado con el sistema descrito en
`../COMBAT_SYSTEM_ANALYSIS.md`. Al atacar, el jugador debe acertar un QTE de círculos
concéntricos (inspirado en `QTE.png`): si falla, el ataque no se ejecuta.

## 1. Archivos

| Archivo | Rol |
|---|---|
| `QTEData.cs` | `ScriptableObject` con los parámetros de diseño del QTE. |
| `QTEController.cs` | `MonoBehaviour` que anima el círculo, escucha el clic y resuelve éxito/fallo. |
| `Assets/DATA/QTE/AttackQTE_Default.asset` | Instancia de `QTEData` usada por defecto en batalla. |
| `Assets/Sprites/QTE/ring.png` | Sprite de anillo (círculo hueco) generado proceduralmente, usado tanto para el círculo objetivo como el que se reduce. |

## 2. `QTEData` (parámetros pedidos)

- `innerRadius` — radio del círculo objetivo (fijo, en el centro).
- `outerRadius` — radio inicial del círculo que se reduce.
- `shrinkSpeed` — velocidad (px/seg) a la que se reduce el radio.
- `position` — posición (x, y) del QTE respecto al centro del panel de batalla.

La tolerancia de acierto (zona verde) **no** es un campo del SO — se calcula en
`QTEController` como `max(6, (outerRadius - innerRadius) * 0.12)`, para no añadir
parámetros fuera de los 4 pedidos.

## 3. Flujo (`QTEController.RunQTE`)

1. Se activa `overlayRoot` (fondo negro semitransparente + anillos).
2. Cada frame el radio del anillo que se reduce baja según `shrinkSpeed`.
3. El anillo se pinta verde como aviso de "ya casi llega" cuando está cerca del
   objetivo (`innerRadius <= currentRadius <= innerRadius + tolerancia`) — es
   solo una ayuda visual, no cambia la regla de acierto.
4. **Regla de acierto:** clic izquierdo (`Input.GetMouseButtonDown(0)`, en
   cualquier parte de la pantalla) mientras `currentRadius >= innerRadius`
   (o sea, **antes** de que la circunferencia llegue al radio interno) → éxito,
   sin importar qué tan grande sea todavía el círculo. No hay que esperar a que
   se achique hasta el objetivo ni acertar una ventana angosta.
5. Si el radio queda por debajo de `innerRadius` sin que le hayan hecho clic
   (ya lo "pasó", se te fue el tiempo) → fallo automático inmediato.
6. Se muestra "¡EXITO!"/"¡FALLO!" ~0.5s y se oculta el overlay.

## 4. Varios QTE en un mismo ataque (cadena/combo)

Un movimiento puede exigir acertar **varios círculos seguidos** en vez de uno solo.
Si falla cualquiera de la cadena, el ataque completo falla (se corta ahí, no sigue
con el resto de la cadena).

- `MoveData` tiene un campo nuevo: `qteSequence` (array de `QTEData`).
  - Si tiene 1 o más elementos, `CombatManager` usa esa cadena para ese movimiento.
  - Si está vacío/null, cae al `qteData` único de `CombatManager` (comportamiento
    de antes, un solo círculo).
- `QTEController.RunQTEChain(IReadOnlyList<QTEData> chain, Action<bool> onComplete)`
  corre cada `QTEData` de la lista en orden, sobre el mismo overlay (no lo
  oculta entre pasos, solo hay una pausa corta de 0.15s). Si un paso falla, corta
  la cadena ahí mismo y el resultado final es fallo.
  `RunQTE(data, onComplete)` (el método de un solo círculo) ahora es solo un atajo
  que llama a `RunQTEChain` con un array de un elemento — mismo comportamiento de
  antes, sin duplicar lógica.

**Cómo agregar una cadena a un movimiento (en el Inspector):**
1. Crea los `QTEData` que quieras (`Assets → Create → Combat → QTE → QTE Data`),
   uno por cada círculo de la cadena (por ejemplo, uno fácil y uno difícil).
2. Selecciona el `MoveData` del ataque (ej. `Assets/Scripts/Combat/Flama.asset`).
3. En el campo `Qte Sequence`, agrega los `QTEData` en el orden en que se deben
   acertar.

**Ejemplo ya cargado en el proyecto:** el movimiento `Flama` (uno de los ataques
de Jack, el pokémon inicial del jugador) tiene `qteSequence = [AttackQTE_Default,
AttackQTE_Hard]` — primero un círculo normal, luego uno un poco más rápido y con
zona de acierto más chica (`Assets/DATA/QTE/AttackQTE_Hard.asset`: innerRadius=55,
outerRadius=220, shrinkSpeed=85 → ~1.9s para llegar al objetivo). Hay que acertar
los dos para que el ataque conecte.

## 5. Varios QTE al mismo tiempo en pantalla (paralelo)

Además de la cadena (secuencial), un movimiento puede exigir acertar **varios
círculos a la vez**, cada uno en su propia posición. Hay que acertarlos todos
(clic cerca de cada uno, en cualquier orden); si fallas cualquiera, el ataque
completo falla — misma severidad que la cadena, pero en paralelo.

- `MoveData` tiene el campo `qteParallel` (array de `QTEData`). Si tiene
  elementos, **tiene prioridad sobre `qteSequence`** (un movimiento no puede
  usar los dos modos a la vez).
- `QTEController.RunQTEParallel(IReadOnlyList<QTEData> circles, Action<bool> onComplete)`:
  1. Instancia una copia de `RingsContainer` (con su `TargetRing`/`ShrinkingRing`)
     por cada `QTEData` de la lista, cada una en su propia posición.
  2. Anima todos los círculos en simultáneo.
  3. En cada clic, busca el círculo **más cercano al cursor** (por distancia en
     pantalla) entre los que faltan por resolver, y evalúa el acierto contra
     ese círculo.
  4. Si ese círculo estaba en zona → queda resuelto (ya no se anima ni cuenta).
     Si no → fallo inmediato de todo el ataque.
  5. Si algún círculo se reduce de más sin que le hayan hecho clic → fallo
     inmediato (timeout), igual que en el modo de un solo círculo.
  6. Cuando ya no quedan círculos sin resolver → éxito. Se destruyen las copias
     instanciadas y se reactiva el `RingsContainer` original (para que el modo
     secuencial/único siga funcionando en el próximo ataque).

**Anti-solape:** cuando un círculo tiene `randomizePosition = true`,
`GetSpawnPositionAvoidingOverlap` intenta hasta 30 posiciones al azar y elige la
primera que deja un espacio libre de al menos `OverlapPadding` (24px) entre los
bordes exteriores de este círculo y los ya colocados; si ninguna queda
perfecta, usa la que menos se solapa. Solo aplica a círculos con posición
aleatoria — uno con posición fija se respeta tal cual (se asume que el diseñador
la puso ahí a propósito).

**Cómo agregarlo a un movimiento:** crea los `QTEData` que quieras y asígnalos
al campo `Qte Parallel` del `MoveData` (en vez de `Qte Sequence`).

**Ejemplo ya cargado:** `Tacleada` (el otro ataque de Jack) tiene
`qteParallel = [AttackQTE_ParallelA, AttackQTE_ParallelB]`, ambos con
`randomizePosition = true` (ver sección 6), innerRadius=70/outerRadius=180 y
shrinkSpeed=55 y 65 respectivamente (~2s y ~1.7s para llegar al objetivo) —
dos círculos aparecen a la vez en posiciones al azar, sin solaparse entre sí,
y hay que acertar los dos.

**Nota sobre velocidad:** el valor por defecto de `shrinkSpeed` en un `QTEData`
nuevo es 60 (antes 140, que resultaba demasiado rápido para reaccionar,
especialmente con varios círculos en pantalla). Todos los `QTEData` del
proyecto (`AttackQTE_Default`, `AttackQTE_Hard`, `AttackQTE_ParallelA/B`) se
bajaron a un rango de 55–85 para que el tiempo hasta el objetivo ronde los
1.7–2.4 segundos.

## 6. Posición aleatoria dentro de la pantalla de batalla

`QTEData` tiene el flag `randomizePosition`. Si está activo, se ignora el campo
`position` y `QTEController` sortea un punto dentro del rect del `Overlay`
(pantalla completa), con un margen igual a `outerRadius + 10` para que el
círculo no se salga de la vista:

```csharp
Rect rect = overlayRT.rect;
float margin = data.outerRadius + 10f;
float halfW = Mathf.Max(0f, rect.width / 2f - margin);
float halfH = Mathf.Max(0f, rect.height / 2f - margin);
return new Vector2(Random.Range(-halfW, halfW), Random.Range(-halfH, halfH));
```

Aplica tanto al modo de un solo círculo/cadena como a cada círculo del modo
paralelo (cada uno sortea su propia posición de forma independiente).

## 7. Integración con `CombatManager`

`CombatManager` tiene dos campos nuevos: `qteController` y `qteData`. En
`PlayerTurn()`, después de que el jugador elige movimiento y antes de reproducir la
animación de ataque:

```csharp
bool attackSucceeds = true;

if (qteController != null)
{
    bool qteResult = true;

    if (selectedMove.qteParallel != null && selectedMove.qteParallel.Length > 0)
    {
        yield return qteController.RunQTEParallel(selectedMove.qteParallel, result => qteResult = result);
    }
    else
    {
        IReadOnlyList<QTEData> qteChain = (selectedMove.qteSequence != null && selectedMove.qteSequence.Length > 0)
            ? selectedMove.qteSequence
            : (qteData != null ? new QTEData[] { qteData } : null);

        if (qteChain != null)
            yield return qteController.RunQTEChain(qteChain, result => qteResult = result);
    }

    attackSucceeds = qteResult;
}

if (!attackSucceeds)
{
    battleUI.ShowBattleMessage($"{playerRuntime.data.creatureName}'s attack missed!");
    yield return new WaitForSeconds(0.8f);
    yield break; // el ataque NO se ejecuta
}
```

Si acierta, sigue el flujo normal (animación + `MoveEffect.Execute`). Solo aplica al
turno del jugador — el enemigo no tiene QTE.

## 8. Jerarquía en la escena

```
BattleUI (Canvas)
└─ QTE                     (RectTransform, stretch full screen, siempre activo)
   └─ Overlay              (Image negro semitransparente, alpha 0.75; oculto hasta RunQTE)
      ├─ RingsContainer    (anclado según QTEData.position)
      │  ├─ TargetRing     (Image, tamaño fijo = innerRadius*2)
      │  └─ ShrinkingRing  (Image, tamaño animado = currentRadius*2)
      └─ ResultText        (TMP, "¡EXITO!"/"¡FALLO!")
```

`QTEController` vive en el GameObject `QTE` (que permanece activo) y controla
`overlayRoot` (el hijo `Overlay`) para no auto-desactivarse a sí mismo en medio de
su propia corrutina (Unity detiene las corrutinas de un GameObject apenas se
desactiva ese mismo GameObject).

## 9. Bug encontrado y corregido: escala 0 en objetos creados por MCP

Al crear `QTE` y `Overlay` con la herramienta de GameObjects del MCP de Unity
mientras `BattleUI` (su padre) estaba inactivo, ambos quedaron con
`m_LocalScale = (0, 0, 0)` en el `.unity` serializado (confirmado leyendo el YAML
crudo de la escena), en vez de `(1, 1, 1)`. Como la escala se propaga
multiplicativamente a los hijos, todo lo que colgaba de `QTE` (el overlay, los
anillos, el texto) se renderizaba con tamaño cero — es decir, invisible — aunque
sus propias transformaciones locales fueran correctas.

**Síntoma:** el QTE nunca se veía en juego, pese a que toda la lógica y las
referencias estaban bien conectadas.

**Fix:** se forzó `localScale = Vector3.one` en `QTE` y `Overlay` vía
`execute_code`, y se guardó la escena.

**Lección:** al crear GameObjects UI por MCP mientras el Canvas padre está inactivo,
verificar (o forzar) `localScale = (1,1,1)` explícitamente — no asumir que el valor
por defecto se preserva correctamente al reparentar bajo un padre inactivo.
