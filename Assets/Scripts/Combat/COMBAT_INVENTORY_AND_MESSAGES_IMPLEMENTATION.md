# Implementación: inventario en combate y mensajes de batalla

## Resumen

Esta implementación hace funcional el uso de objetos dentro del sistema de combate mediante clic o arrastrar y soltar, reutilizando el inventario real del jugador y el componente `MouseFollower`. También corrige el acceso a la primera posición de la pila, evita consumos inválidos, traduce los mensajes de combate al español y ajusta las frases largas para que no se superpongan con los PS del UVGmon aliado.

## Objetivos cubiertos

- Tomar un objeto desde la pestaña **Inventario** durante el combate.
- Mostrar el objeto siguiendo el cursor mediante `MouseFollower`.
- Soltarlo sobre el UVGmon aliado activo.
- Mantener también el uso mediante clic.
- Consumir una unidad únicamente cuando el efecto se aplica correctamente.
- Corregir el fallo de la primera posición o pila del inventario.
- Evitar el uso con PS completos o con el UVGmon debilitado.
- Mostrar retroalimentación cuando un objeto no puede utilizarse.
- Traducir y mejorar los mensajes de ataques y acciones.
- Mantener los mensajes largos en una sola línea sin cubrir los PS.

## Flujo del arrastre

1. `CombatInventorySlotUI` detecta el inicio con `IBeginDragHandler`.
2. El slot entrega su índice real a `CombatInventoryUI`.
3. `CombatInventoryUI` verifica la posición y conserva el índice arrastrado.
4. `MouseFollower` muestra el icono y la cantidad junto al cursor.
5. `BattleUIManager` habilita temporalmente al UVGmon aliado como destino.
6. `CombatItemDropTarget` detecta el objeto soltado.
7. `BattleUIManager` recupera el índice y emite `OnInventoryItemDropped`.
8. `CombatManager` procesa el drop con la misma lógica utilizada por el clic.
9. Si tiene éxito, aplica el efecto, descuenta una unidad y consume el turno.
10. Si se rechaza, no descuenta el objeto y permite intentar otra acción.

```text
Slot del inventario
        ↓
CombatInventoryUI
        ↓
MouseFollower + destino de drop
        ↓
BattleUIManager
        ↓
CombatManager
        ↓
ItemEffect → CreatureRuntime → InventorySO
```

## Corrección de la primera pila

La validación anterior comparaba el índice seleccionado con `InventorySO.Size`. Ese valor representa el tamaño configurado, pero no garantiza que coincida siempre con la cantidad real de posiciones serializadas en `inventoryItems`.

Se añadió `InventorySO.TryGetItemAt`, que comprueba directamente:

- Que la lista exista.
- Que el índice no sea negativo.
- Que el índice sea menor que `inventoryItems.Count`.
- Que la posición no esté vacía.

El clic y el arrastre usan esta consulta segura. Esto procesa correctamente la posición `0` y evita rechazos incorrectos cuando el tamaño configurado y la lista interna no coinciden.

## Reglas de uso de objetos curativos

`CombatManager.HandleInventoryItemClicked` valida:

1. Que sea el turno del jugador.
2. Que todavía no se haya elegido otra acción.
3. Que existan el inventario y el UVGmon activo.
4. Que la posición seleccionada siga disponible.
5. Que el objeto pertenezca a `ItemCategory.Healing`.
6. Que tenga un `ItemEffect` asignado.
7. Que el UVGmon tenga más de `0 PS`.
8. Que tenga menos de sus PS máximos.
9. Que el efecto aumente realmente sus PS.

La unidad se elimina solamente después de confirmar una curación efectiva. Si ocurre una excepción o el efecto no modifica la vida, el objeto no se consume.

### Mensajes de rechazo

- `Ese objeto ya no está disponible.`
- `[Objeto] no puede utilizarse durante el combate.`
- `[UVGmon] está debilitado; [Objeto] no puede reanimarlo.`
- `[UVGmon] ya tiene todos sus PS.`
- `[Objeto] no produjo ningún efecto.`
- `No se pudo usar [Objeto]. Inténtalo de nuevo.`

Un rechazo no consume el turno y vuelve a habilitar la entrada.

## Mensajes de combate en español

Se reemplazaron los mensajes en inglés y se hicieron más descriptivos:

- Aparición de un UVGmon salvaje.
- Uso de ataques y objetos.
- Ataques fallidos.
- Daño general y daño dirigido a partes.
- Golpes críticos.
- Ataques eficaces y poco eficaces.
- UVGmon debilitados.
- Derrota y finalización del combate.
- Captura, falta de frascos y escape.

Ejemplos:

- `¡Versionmini usó Placaje!`
- `¡Versionmini falló! El ataque no alcanzó al rival.`
- `¡Golpe crítico! El impacto fue devastador.`
- `¡Es muy eficaz! El ataque golpeó su punto débil.`
- `¡Brazo izquierdo recibió 46 de daño! Resistencia: 74/120.`
- `¡Versionmini usó Shuko! Recuperó 20 PS.`
- `¡Versionmini quedó debilitado!`

También se corrigió `¡ESCAPO!` por `¡ESCAPÓ!`.

## Prevención de traslapes

`BattleUIManager.ConfigureBattleMessageText` configura cada mensaje antes de mostrarlo:

- `enableWordWrapping = false`: evita una segunda línea.
- `enableAutoSizing = true`: reduce el texto solo cuando no cabe.
- Tamaño máximo: `26`.
- Tamaño mínimo: `14`.
- `maxVisibleLines = 1`: limita el texto a una línea.
- `TextOverflowModes.Ellipsis`: respaldo para textos extraordinariamente largos.

Las frases reales más largas se probaron entre aproximadamente `18` y `20` puntos, permanecieron completas y separadas del bloque de PS aliado.

## Archivos involucrados

### `Assets/Scripts/Combat/CombatInventorySlotUI.cs`

- Implementa clic, inicio de arrastre, arrastre y fin de arrastre.
- Conserva el índice real y emite eventos sin modificar el inventario.

### `Assets/Scripts/Combat/CombatInventoryUI.cs`

- Administra los slots visibles y el índice arrastrado.
- Activa y actualiza `MouseFollower`.
- Cancela el arrastre al cambiar de pestaña, deshabilitar la entrada o repintar.
- Usa `TryGetItemAt` para acceder de forma segura.

### `Assets/Scripts/Combat/CombatItemDropTarget.cs`

- Nuevo componente basado en `IDropHandler`.
- Se habilita únicamente durante un arrastre válido.
- Notifica el drop sin modificar directamente vida o cantidades.

### `Assets/Scripts/Combat/BattleUIManager.cs`

- Conecta inventario, arrastre y drop.
- Habilita el destino sobre el UVGmon aliado.
- Envía clic y drop hacia la misma lógica.
- Configura el mensaje en una sola línea con tamaño automático.

### `Assets/Scripts/Combat/CombatManager.cs`

- Valida turno, objeto y estado del UVGmon.
- Aplica el efecto y calcula la curación real.
- Consume una unidad solo tras una aplicación exitosa.
- Decide si la acción consume el turno.
- Contiene los mensajes generales traducidos.

### `Assets/Scripts/Combat/DamageEffect.cs`

- Traduce los mensajes de ataque normal, crítico, efectividad, daño y debilitamiento.

### `Assets/Scripts/Combat/Capture/CaptureController.cs`

- Corrige la acentuación del resultado de escape.

### `Assets/Scripts/MenuInventary/Model/InventorySO.cs`

- Añade `TryGetItemAt` y usa la lista real como fuente de verdad.

### `Assets/Scenes/jardinconocimiento.unity`

- Contiene las referencias del `MouseFollower` de combate.
- Contiene el destino de drop del UVGmon aliado.
- Conecta los nuevos componentes con la interfaz.

## Consecuencias y decisiones

- Clic y drop comparten las mismas reglas; no existen dos sistemas de consumo.
- Un uso exitoso consume el turno.
- Un intento inválido no consume objeto ni turno.
- El combate usa el mismo `InventorySO` que el inventario fuera de combate.
- `InventorySO.OnInventoryUpdated` repinta la pestaña después del consumo.
- Solo los objetos `Healing` son utilizables durante el combate.
- Los objetos curativos no reviven UVGmon debilitados.
- Los nombres extremadamente largos pueden alcanzar el tamaño mínimo y, si aun así no caben, mostrar puntos suspensivos.
- Los textos permanecen definidos en `CombatManager` y `DamageEffect`; no existe todavía un sistema externo de localización.

## Observación sobre HotDog

El asset `HotDog` mostrado en la captura no tiene un `ItemEffect` curativo configurado. Por eso no cura ni se consume. Ahora el sistema informa que no puede utilizarse en lugar de fallar silenciosamente.

Para hacerlo utilizable habría que asignarle una categoría y un efecto adecuados desde su asset; eso no forma parte de esta corrección.

## Verificación realizada

### Prueba funcional en Play Mode

Se colocó Shuko en la posición `0` de un inventario temporal:

- PS: `50 → 70`.
- Cantidad: `4 → 3`.

También se comprobó:

- PS completos: no consume.
- UVGmon con `0 PS`: no consume.
- Objeto sin efecto válido: no consume.
- Mensajes largos: una sola línea.
- Frases reales: sin truncamiento.
- Texto y PS aliados: áreas separadas.
- Consola de Unity: sin errores.
- Unity Test Framework EditMode: `24/24` pruebas aprobadas.

## Pruebas manuales recomendadas

1. Iniciar un combate y recibir daño.
2. Abrir **Inventario**.
3. Arrastrar la primera pila de Shuko sobre el aliado.
4. Confirmar curación y descuento de una unidad.
5. Repetir mediante clic.
6. Intentar usar Shuko con PS completos.
7. Intentar usarlo con el UVGmon en `0 PS`.
8. Intentar usar HotDog.
9. Confirmar mensajes y cantidades sin cambios en los casos inválidos.
10. Probar ataques normales, críticos, eficaces, poco eficaces y fallidos.
11. Confirmar que los mensajes no cubren los PS.
12. Probar distintas resoluciones y relaciones de aspecto.

## Commit sugerido

### Nombre

```text
fix(combat): corrige uso de objetos y mejora mensajes de batalla
```

### Descripción

```text
- Corrige el uso del primer objeto de la pila del inventario.
- Integra el arrastre mediante MouseFollower sobre el UVGmon activo.
- Evita consumos con PS completos, UVGmon debilitados u objetos inválidos.
- Agrega retroalimentación para intentos rechazados.
- Traduce y mejora las acciones del combate al español.
- Mantiene los mensajes en una línea con tamaño automático.
- Evita traslapes entre mensajes y PS.
```
