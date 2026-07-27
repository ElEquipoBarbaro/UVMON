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
3. El anillo se pinta verde cuando está dentro de la tolerancia alrededor de `innerRadius`.
4. Clic izquierdo (`Input.GetMouseButtonDown(0)`, en cualquier parte de la pantalla):
   - Dentro de tolerancia → éxito.
   - Fuera de tolerancia → fallo.
5. Si el radio baja de `innerRadius - tolerancia` sin clic → fallo automático (timeout).
6. Se muestra "¡EXITO!"/"¡FALLO!" ~0.5s y se oculta el overlay.

## 4. Integración con `CombatManager`

`CombatManager` tiene dos campos nuevos: `qteController` y `qteData`. En
`PlayerTurn()`, después de que el jugador elige movimiento y antes de reproducir la
animación de ataque:

```csharp
bool attackSucceeds = true;

if (qteController != null && qteData != null)
{
    bool qteResult = false;
    yield return qteController.RunQTE(qteData, result => qteResult = result);
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

## 5. Jerarquía en la escena

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

## 6. Bug encontrado y corregido: escala 0 en objetos creados por MCP

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
