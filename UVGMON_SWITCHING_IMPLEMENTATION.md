# Cambio de UVGmon activo durante el combate

## Registro de implementación

- Proyecto: **UVMON / UVGmon**
- Versión de Unity: **2021.3.45f2**
- Fecha de implementación y validación: **2026-08-23**
- Escena modificada: `Assets/Scenes/jardinconocimiento.unity`
- Alcance: cambio voluntario del UVGmon activo, reemplazo automático por derrota, conservación de vida, sincronización del HUD, bloqueo de entradas y consumo correcto del turno.

Este documento registra la arquitectura encontrada, las decisiones tomadas, los cambios realizados y las pruebas ejecutadas. Su propósito es evitar que una futura modificación duplique estados, rompa el ciclo de turnos o reintroduzca errores ya contemplados.

## 1. Auditoría de la arquitectura existente

### Inicio y control del combate

La escena que contiene el flujo real de combate es `Assets/Scenes/jardinconocimiento.unity`.

El inicio de una batalla sigue esta cadena:

1. `InteractionPlayer`
2. `BattleStarter`
3. `DialogueManager`
4. `CombatManager.StartBattle`

`CombatManager` es el equivalente real de un `BattleManager`. Este componente controla:

- Inicio y finalización de la batalla.
- Preparación de las criaturas del jugador y del enemigo.
- Turno del jugador.
- Turno del enemigo.
- Ataques y daño.
- Inventario de combate.
- Captura.
- Derrota individual y derrota total.
- Actualización coordinada de la interfaz.

Existe un archivo `PlayerTurn.cs`, pero no forma parte del flujo activo. El turno real del jugador se ejecuta dentro de `CombatManager` mediante corrutinas y banderas de estado.

### Equipo del jugador y criatura activa

`PlayerParty` conserva el equipo actual en su colección `Party`, formada por instancias de `CreatureRuntime`.

La fuente confiable del UVGmon activo es el elemento de índice `0` de `PlayerParty.Party`, obtenido mediante `GetLeadCreature()`. `CombatManager.playerRuntime` funciona como referencia de trabajo durante la batalla, pero siempre se sincroniza con el líder de `PlayerParty` al iniciar o completar un cambio.

No se creó otra variable global ni una segunda colección para representar el equipo o el UVGmon activo.

### Vida y derrota

Cada UVGmon tiene su propia instancia `CreatureRuntime`.

- Vida actual: `CreatureRuntime.CurrentHP`.
- Vida máxima: se obtiene de las estadísticas de la criatura.
- Datos base: `CreatureData`.
- Derrota: `CurrentHP <= 0`.

La vida no pertenece al HUD ni a la barra visual. El HUD solamente lee y presenta los valores del runtime activo.

Reordenar el equipo no crea una instancia nueva, no cura la criatura y no copia la vida de otro integrante.

### Turnos y prevención de acciones duplicadas

El combate utiliza corrutinas. El ciclo general alterna entre el turno del jugador y el turno enemigo.

Las banderas `isPlayerTurn` y `playerHasChosen`, junto con la espera de una única acción confirmada, impiden que el jugador ataque y cambie durante el mismo turno. Los sistemas QTE, captura y animación también operan mediante corrutinas.

El proyecto utiliza `Time.timeScale` para otros estados, como pausas, pero el cambio de UVGmon se integra directamente al flujo de corrutinas del combate.

### Interfaz existente

`BattleUIManager` controla las pestañas principales de combate:

- Ataques.
- Equipo de UVGmon.
- Inventario.

La pestaña de equipo ya mostraba nombre, icono, vida, barra, estado activo y estado derrotado. Antes de esta implementación, el clic era inmediato, no existía una confirmación/cancelación completa y la derrota del activo podía finalizar toda la batalla aunque quedaran integrantes sanos.

El proyecto usa uGUI, TextMeshPro y `EventSystem` con `StandaloneInputModule`.

## 2. Decisión de diseño

La solución conserva la arquitectura original y establece estas reglas:

1. `PlayerParty` continúa siendo la única fuente del orden del equipo y del UVGmon activo.
2. `CreatureRuntime.CurrentHP` continúa siendo la única fuente de vida actual.
3. Una sola corrutina de `CombatManager` ejecuta tanto el cambio voluntario como el reemplazo por derrota.
4. El cambio voluntario consume el turno únicamente después de completarse correctamente.
5. El cambio forzado no genera una acción enemiga adicional.
6. Las entradas quedan bloqueadas durante transiciones y acciones comprometidas.
7. Las referencias visuales opcionales se validan antes de utilizarlas.

No se reemplazó el sistema de combate, no se migró el sistema de entrada y no se añadieron paquetes externos.

## 3. Archivos modificados

### `Assets/Scripts/Combat/CombatManager.cs`

- Se añadió `currentPlayerParty` como referencia de autoridad para el equipo usado en la batalla.
- Se añadieron duraciones serializadas para salida y entrada visual.
- Se añadió un índice de cambio pendiente y un motivo de cambio: voluntario o por derrota.
- `StartBattle` busca el primer integrante vivo si el líder ya está derrotado.
- `PlayerTurn` espera una única acción comprometida.
- Una confirmación válida de cambio consume el turno completo.
- Una selección inválida o un fallo inesperado devuelve el control sin consumir el turno.
- Se añadió la corrutina central `SwitchPlayerCreature(...)`.
- Antes de reordenar el equipo se vuelve a validar el destino para evitar condiciones de carrera.
- El runtime activo, el HUD, los ataques, el equipo y la representación visual se sincronizan después del cambio.
- Se añadió `ReplaceFaintedPlayer(...)` para buscar y colocar automáticamente el siguiente integrante disponible.
- El ciclo de combate comprueba derrotas antes y después de las acciones relevantes.
- Un reemplazo provocado por el ataque enemigo continúa con el turno del jugador, sin ejecutar un segundo turno enemigo.
- La derrota total usa `PlayerParty.HasUsableCreature()` y conserva el flujo de derrota existente.
- Una captura exitosa agrega la criatura al `currentPlayerParty` de la batalla.

### `Assets/Scripts/Combat/PlayerParty.cs`

- `SetLeadCreature(int)` ahora devuelve `bool` y valida índices y referencias nulas.
- Se añadió `IsUsableCreatureIndex(int)`.
- Se añadió `FindFirstUsableCreatureIndex(int startIndex = 0)` con recorrido y vuelta al inicio.
- `HasUsableCreature()` y `HealAll()` son seguros ante posiciones nulas.
- El cambio de líder reordena las mismas instancias de `CreatureRuntime`; no crea ni reinicia criaturas.

### `Assets/Scripts/Combat/BattleUIManager.cs`

- Se añadió el bloqueo central `SetPlayerInputEnabled(bool)`.
- El bloqueo afecta pestañas, ataques, equipo e inventario.
- Cancelar la selección del equipo regresa a Ataques y no consume el turno.
- El HUD evita refrescos duplicados al activar paneles.
- Al salir de la pestaña de equipo se limpia la selección pendiente.
- Al ocultar o mostrar la batalla se actualiza correctamente el estado de entrada.

### `Assets/Scripts/Combat/CombatTeamUI.cs`

- Se añadió una selección pendiente en lugar de ejecutar el cambio con el primer clic.
- Se añadieron referencias para `confirmButton`, `cancelButton` y `statusText`.
- Se gestionan listeners en `Awake` y `OnDestroy` para evitar suscripciones duplicadas.
- Seleccionar al activo muestra que ya está en combate.
- Seleccionar a un derrotado informa que no puede volver al campo.
- Confirmar solo se habilita para un integrante sano, no activo y perteneciente al equipo actual.
- Cancelar limpia la selección y devuelve el control sin gastar el turno.
- Se puede bloquear toda la entrada del panel durante una acción.
- Si una escena antigua no posee botón de confirmación, se conserva un comportamiento de compatibilidad para selecciones válidas.

### `Assets/Scripts/Combat/CombatTeamSlotUI.cs`

- Se añadieron estados visuales para activo, seleccionado y bloqueado.
- El slot busca una imagen raíz como respaldo si la referencia visual no está asignada.
- Los clics se ignoran mientras el slot está bloqueado.
- Los datos nulos se manejan de forma segura.
- Un integrante derrotado sigue visible aunque no pueda confirmarse.

### `Assets/Scripts/Combat/CombatInventoryUI.cs`

- Se añadió `SetInputEnabled(bool)` para bloquear el inventario mientras se ejecuta otra acción.

### `Assets/Scripts/Combat/CombatInventorySlotUI.cs`

- Se añadió `SetInteractable(bool)`.
- Los clics quedan protegidos durante transiciones.
- El estado visual refleja cuándo un slot está deshabilitado.

### `Assets/Scripts/Combat/CreatureBattleView.cs`

- Se conserva la escala local de reposo de la criatura.
- `SetSprite` restaura alfa y escala para evitar arrastrar estados incompletos de una transición anterior.
- Se añadió `PlaySwitchOut(float)` con reducción de escala y desvanecimiento.
- Se añadió `PlaySwitchIn(float)` con aparición y recuperación de escala.
- Ambas animaciones terminan antes de que continúe la lógica del turno.

### `Assets/Scenes/jardinconocimiento.unity`

Dentro del panel existente de equipo se añadieron:

- `SwitchStatus`.
- `SwitchActions`.
- `ConfirmSwitchButton`, con texto **Cambiar**.
- `CancelSwitchButton`, con texto **Cancelar**.

También se amplió el panel para alojar estos controles y se asignaron todas las referencias serializadas.

## 4. Archivos creados durante la implementación funcional

No se crearon scripts, prefabs, ScriptableObjects, recursos visuales ni archivos `.meta` nuevos.

El único archivo nuevo posterior es este documento de mantenimiento.

## 5. Flujo de cambio voluntario

1. El turno del jugador habilita las entradas.
2. El jugador abre la pestaña de equipo.
3. Puede navegar y seleccionar sin consumir el turno.
4. El UVGmon activo y los derrotados son rechazados con un mensaje claro.
5. Una selección válida habilita **Cambiar**.
6. Al confirmar, se registra una única acción pendiente y se bloquea la interfaz.
7. El UVGmon actual reproduce la transición de salida.
8. El destino se valida nuevamente.
9. `PlayerParty.SetLeadCreature` mueve la misma instancia al índice `0`.
10. `CombatManager.playerRuntime` se sincroniza con el nuevo líder.
11. Se actualizan sprite, nombre, vida, barra, ataques y panel del equipo.
12. El nuevo UVGmon reproduce la transición de entrada.
13. Se vuelve al panel de ataques.
14. El turno del jugador termina y se ejecuta exactamente un turno enemigo.

Abrir el menú, cambiar de selección, pulsar al activo, pulsar a un derrotado o cancelar no altera `playerHasChosen` y no consume el turno.

## 6. Flujo de reemplazo automático

Cuando el activo llega a `CurrentHP <= 0`:

1. `CombatManager` consulta `FindFirstUsableCreatureIndex(1)`.
2. La búsqueda ignora nulos, índices inválidos y criaturas con vida cero.
3. Si encuentra un destino, llama a la misma corrutina `SwitchPlayerCreature(...)` con motivo de derrota.
4. Se actualizan todos los sistemas de la misma forma que en el cambio voluntario.
5. Si la derrota fue causada por el enemigo, el reemplazo no dispara otro ataque enemigo.
6. El combate continúa en el turno del jugador.
7. Si no hay reemplazo, se ejecuta la derrota total existente.

## 7. Conservación de vida y datos

La operación de cambio no crea un `CreatureRuntime` nuevo. Solo cambia el orden de las referencias existentes en `PlayerParty.Party`.

Por esta razón se conservan:

- `CurrentHP` de cada integrante.
- Vida máxima y estadísticas derivadas.
- Nivel.
- Datos y ataques propios.
- Estado derrotado cuando `CurrentHP` es cero.

El HUD se vuelve a enlazar con el runtime entrante y nunca se usa como almacenamiento de la vida.

## 8. Bloqueo de acciones y protección de turnos

Durante una acción confirmada se bloquean:

- Botones de pestañas.
- Botones de ataques.
- Slots y confirmación del equipo.
- Slots del inventario.
- Navegación que pueda cerrar o reabrir paneles de forma inconsistente.

Protecciones adicionales:

- Solo existe un índice de cambio pendiente.
- La acción se confirma una vez antes de liberar la espera del turno.
- El destino se valida antes y después de la transición de salida.
- Si el cambio falla, se restaura la representación anterior y el turno continúa disponible.
- Los listeners se agregan y eliminan de forma simétrica.
- El HUD y los ataques se actualizan antes de reactivar la entrada.

## 9. Casos límite contemplados

1. Intentar cambiar al UVGmon activo.
2. Seleccionar un integrante con vida cero.
3. Abrir el menú y cancelar.
4. Realizar varios cambios en una batalla.
5. Derrota del primer integrante.
6. Varios integrantes consecutivos derrotados.
7. Un solo UVGmon con vida.
8. Todo el equipo derrotado.
9. Posiciones nulas o referencias inválidas.
10. Pulsaciones rápidas repetidas sobre confirmar.
11. Intentar otra acción durante la animación.
12. Actualizar el HUD durante el cambio.
13. Volver a utilizar una criatura dañada previamente.
14. Conservar el mismo `PlayerParty` entre batallas.
15. Finalizar la batalla con un panel abierto.
16. Derrota mediante daño normal.
17. Cualquier efecto existente que deje `CurrentHP <= 0`.
18. Abrir y cerrar repetidamente el panel de equipo.
19. Cambiar a una criatura con otra lista o cantidad de ataques.
20. Referencias visuales opcionales sin asignar.

## 10. Pruebas ejecutadas

La validación se realizó en la instancia real de Unity mediante MCP.

### Compilación y validación estática

- Los ocho scripts funcionales modificados fueron validados individualmente.
- Resultado: **0 errores y 0 advertencias en los scripts modificados**.
- La escena fue validada.
- Resultado: **0 scripts faltantes, 0 prefabs rotos y 0 referencias inválidas detectadas**.

### Pruebas automatizadas existentes

- Modo: EditMode.
- Total: **24**.
- Aprobadas: **24**.
- Fallidas: **0**.
- Omitidas: **0**.

No se añadieron pruebas unitarias nuevas porque el ensamblado de pruebas existente solo referencia el módulo aislado `CombatMath`; los componentes centrales viven en `Assembly-CSharp`. Añadirlos habría requerido una migración de ensamblados fuera del alcance de esta mejora.

### Pruebas controladas en Play Mode

#### Selecciones inválidas y cancelación

- Seleccionar al Jack activo mostró: `Este UVGmon ya esta en combate.`
- Confirmar permaneció deshabilitado.
- Seleccionar Versionmini habilitó confirmar.
- Cancelar mantuvo a Jack como activo y regresó a Ataques.
- Ninguna de estas acciones consumió el turno.

#### Cambio voluntario y vida individual

Estado inicial observado:

- Jack: `170/170`.
- Versionmini: `120/120`.

Secuencia validada:

1. Jack fue reducido a `21 HP`.
2. Se cambió a Versionmini.
3. Versionmini recibió un único ataque enemigo y quedó con `86 HP`.
4. Se esperó sin elegir otra acción y permaneció en `86 HP`, confirmando que no hubo un segundo ataque.
5. Se volvió a seleccionar a Jack.
6. Jack regresó con los mismos `21 HP`.
7. Después del siguiente ataque enemigo quedó en `19 HP`.

Esto confirmó que cada runtime conserva su vida y que el cambio consume exactamente un turno.

#### Reemplazo forzado

1. Versionmini se colocó en `1 HP` para la prueba.
2. Entró al combate mediante un cambio válido.
3. El enemigo lo derrotó y quedó en `0 HP`.
4. Jack entró automáticamente con sus `19 HP` conservados.
5. Jack permaneció en `19 HP` esperando la acción del jugador.

No ocurrió un segundo ataque enemigo después del reemplazo.

#### UVGmon derrotado

- El slot de Versionmini derrotado permaneció visible.
- Al seleccionarlo se indicó que estaba derrotado.
- Confirmar permaneció deshabilitado.
- El turno no fue consumido.

#### Confirmaciones rápidas

Se invocó confirmar cinco veces rápidamente después de curar al equipo.

Resultado:

- Solo se registró un destino pendiente.
- Solo se ejecutó un cambio.
- Solo ocurrió un turno enemigo.
- Los botones permanecieron bloqueados durante la transición.

#### Derrota total

Ambos integrantes fueron colocados en `0 HP` durante un flujo real de acción.

Resultado:

- Se ejecutó la derrota existente.
- La batalla dejó de estar activa.
- La interfaz de batalla se ocultó.
- El enemigo no realizó otra acción.
- No se intentó seleccionar un reemplazo inexistente.

#### Validación visual

En Game View se comprobó:

- Dos slots de equipo visibles.
- Iconos, nombres y barras de vida.
- Estado activo.
- Estado seleccionado.
- Texto informativo.
- Botones **Cambiar** y **Cancelar**.
- Bloqueo visual y funcional durante acciones.

Las capturas temporales utilizadas para la inspección fueron eliminadas después de validar; no quedaron assets ni `.meta` residuales.

## 11. Compatibilidad con sistemas existentes

- Ataques: el panel vuelve a renderizar los movimientos del nuevo `playerRuntime`.
- Inventario: conserva su comportamiento y queda bloqueado durante otras acciones.
- Captura: mantiene su flujo; una captura exitosa se agrega al mismo `PlayerParty` usado por la batalla.
- Finalización: se reutilizan los flujos existentes de victoria y derrota.
- QTE y animaciones de ataque: no se reemplazaron ni duplicaron.
- Estado entre batallas: se reutilizan las mismas instancias de `CreatureRuntime` de `PlayerParty`.

Los caminos de ataque, captura y una segunda batalla completa no recibieron una nueva prueba automatizada end-to-end. Su estructura se conservó y las pruebas existentes continúan aprobando.

## 12. Referencias del Inspector

No se requiere ninguna asignación manual después de esta implementación.

En `jardinconocimiento.unity`, `CombatTeamUI` tiene asignados:

- `confirmButton` → `ConfirmSwitchButton`.
- `cancelButton` → `CancelSwitchButton`.
- `statusText` → `SwitchStatus`.

Las duraciones de transición también quedaron serializadas.

## 13. Advertencias y trabajo futuro

### Advertencia preexistente

Durante una recompilación Unity mostró `CS0162` en `InventorySO.cs` por código inalcanzable. El archivo no fue modificado y la advertencia no fue causada por esta implementación.

### Diseño en resoluciones pequeñas

En una Game View de `960 × 541`, el HUD inferior existente se percibe apretado. Los controles nuevos son visibles y funcionales, pero sería conveniente revisar el layout responsivo si esa resolución es un objetivo final.

### Pruebas futuras recomendadas

Si posteriormente se reorganizan los ensamblados del proyecto, conviene añadir pruebas automatizadas para:

- `PlayerParty.SetLeadCreature`.
- `PlayerParty.FindFirstUsableCreatureIndex`.
- Cambio voluntario sin consumir dos acciones.
- Reemplazo automático sin segundo turno enemigo.
- Derrota total sin reemplazo.
- Persistencia de `CurrentHP` después de varios cambios.

## 14. Reglas para mantenimiento futuro

Al extender esta función:

- Mantener `PlayerParty.Party[0]` como fuente del activo.
- No guardar vida en barras, textos o componentes visuales.
- No crear un segundo método independiente para cambios forzados.
- Pasar cualquier nuevo motivo de cambio por `SwitchPlayerCreature(...)`.
- Mantener bloqueada la interfaz hasta que termine la transición.
- No marcar `playerHasChosen` al abrir, navegar o cancelar un panel.
- Revalidar el destino inmediatamente antes de modificar el equipo.
- Actualizar ataques y HUD desde el nuevo `CreatureRuntime`.
- Comprobar siempre `HasUsableCreature()` antes de continuar el ciclo.
- Conservar las asignaciones serializadas y los GUID existentes.
- Después de modificar scripts: esperar compilación, revisar la consola y ejecutar las pruebas EditMode.

