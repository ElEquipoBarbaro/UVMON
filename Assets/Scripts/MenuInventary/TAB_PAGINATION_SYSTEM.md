# Tab / pagination system between Inventory and Pokemon panels

How the "pagination" (tab switching) between the **Items** view and the **Pokemon** view
inside the in-game menu works, and the sizing bug that was fixed on `PokemonPanel`.

## 1. Hierarchy (scene `jardinconocimiento`)

```
OverworldUI                                    [Canvas]
└── InGameMenu                                 sibling 1
      HorizontalLayoutGroup: padding 50/50/50/50, ChildControlWidth/Height=1, ForceExpand W/H=1
      ├── TabBar                               ← the two tab buttons
      │     VerticalLayoutGroup + LayoutElement (fixed preferred width, doesn't stretch)
      │     ├── ItemsTabButton
      │     └── PokemonTabButton
      ├── Inventory                            [Inventory.prefab instance]
      │     UIInventoryPage  (Show()/Hide() → gameObject.SetActive)
      └── PokemonPanel
            PokemonTabController  (Show()/Hide() → gameObject.SetActive)
            ├── PokemonContent    (grid of PokemonSlotUI, spawned at runtime)
            └── PokemonDetails    (PokemonDetailsPanel)
```

`InventoryController` (on the `Player` prefab instance) is the glue: it holds references to
`inventoryUI` (`UIInventoryPage`), `pokemonTab` (`PokemonTabController`), `itemsTabButton` and
`pokemonTabButton`.

## 2. How the switching works — no real "pages", just Show/Hide

There's no `PageView`/`Pagination` component. It's the simplest thing that works: **only one of
the two panels is active at a time**, toggled by two buttons.

`InventoryController.cs`:

```csharp
private void PrepareUI()
{
    ...
    itemsTabButton.onClick.AddListener(ShowItemsTab);
    pokemonTabButton.onClick.AddListener(ShowPokemonTab);
}

private void ShowItemsTab()
{
    pokemonTab.Hide();
    inventoryUI.Show();
    UpdateInventoryUI(inventoryData.GetCurrentInventoryState());
}

private void ShowPokemonTab()
{
    inventoryUI.Hide();
    pokemonTab.Show();
}
```

Both `UIInventoryPage.Show()/Hide()` and `PokemonTabController.Show()/Hide()` just do
`gameObject.SetActive(true/false)` (plus their own refresh/reset logic — `PokemonTabController.Show()`
also calls `Refresh()` to rebuild the party slots, `Hide()` cancels any pending target selection).

Opening the whole menu (`I` key) always lands on the Items tab first (`OpenMenu()` → `ShowItemsTab()`).

## 3. Why this makes the "same size" requirement free

`InGameMenu`'s `HorizontalLayoutGroup` has `ChildControlWidth/Height = 1` and
`ForceExpand Width/Height = 1`. Unity's layout groups **skip inactive children** when computing
layout. So:

- When the Items tab is showing: only `TabBar` (fixed width) + `Inventory` are active →
  `Inventory` gets 100% of the remaining width/height.
- When the Pokemon tab is showing: only `TabBar` (fixed width) + `PokemonPanel` are active →
  `PokemonPanel` gets 100% of the remaining width/height.

Since both panels sit in the exact same slot of the same layout group, they automatically end up
the same size — **as long as neither has a stray `localScale` fighting the layout.**

## 4. The actual bug that made `PokemonPanel` look tiny

`HorizontalLayoutGroup` only ever writes to `RectTransform.sizeDelta`/`anchoredPosition` — it never
touches `localScale`. `PokemonPanel` (and `TabBar`) had `m_LocalScale = {0.2083333, 0.2083333, 0.2083333}`
(≈ 1/4.8) left over in the scene file, while `Inventory.prefab`'s root scale is `{1, 1, 1}`. The
layout group was sizing `PokemonPanel`'s rect correctly, but the whole panel then rendered — and hit-tested,
and showed its Scene-view gizmo handles — at ~20% scale, which is why it looked tiny **and** was painful
to select/drag in the Editor.

Fix: reset both `TabBar` and `PokemonPanel`'s `RectTransform.localScale` to `(1, 1, 1)` in the Unity
Editor (via MCP `manage_gameobject`, then `manage_scene` → `save`), matching `Inventory.prefab`. No
prefab, layout, or script changes were needed — it was purely a stray transform value.

*(There's a third, unrelated GameObject — `ItemContextMenu`, the right-click item menu, a sibling of
`InGameMenu` under `OverworldUI` — that still has the same `0.2083333` scale. It's a separate popup,
not part of the Inventory/Pokemon tab pair, so it was left untouched. If it also looks too small, apply
the same fix to it.)*

## 6. Bug encontrado y corregido (2026-08-07): las tarjetas de Pokémon no cabían en el recuadro negro

**Síntoma:** los sprites/tarjetas de Pokémon se veían demasiado grandes, estirados y no
respetaban los límites del fondo negro de `PokemonContent`.

**Causa raíz #1 (por qué el sprite se estiraba):** `PokemonSlotUI.prefab` → `Icon` (el
`Image` que recibe `creature.data.frontSprite`) tenía `preserveAspect = false` y su
`LayoutElement` solo fijaba `minHeight = 120`, sin `preferredHeight`. Sin una restricción
de alto, `Image.preferredHeight` (que un `ILayoutElement` con prioridad 0 aporta cuando
nada de mayor prioridad la pisa) devuelve el **alto nativo en píxeles del sprite** — 300px
para `Jack` (`rat.png`), 500px para `Versionmini` — muy por encima de la celda de 200×200
del `GridLayoutGroup`. El `VerticalLayoutGroup` del slot terminaba dándole al ícono un alto
real de cientos de píxeles, desbordando la celda y, con `preserveAspect=false`, estirando
el sprite sin respetar su proporción.

**Fix (a nivel de prefab compartido, `PokemonSlotUI.prefab` → `Icon`):**
`Image.preserveAspect = true` + `LayoutElement.preferredHeight = 120` (igual al
`minHeight` ya existente, así el alto queda fijo/determinístico). Con esto el sprite se
ajusta ("letterbox") dentro de una caja de ~190×120, sin estirarse y sin salirse — un solo
cambio, en el prefab, se aplica automáticamente a todos los Pokémon (no hace falta tocar
cada instancia).

**Causa raíz #2 (por qué una segunda fila se cortaba con el borde de la pantalla):**
a diferencia del grid de Items (que ya tiene `Scroll View`/`Viewport`/`Mask`/`ScrollRect`),
`PokemonContent` solo tenía el `GridLayoutGroup` puesto directo sobre el fondo negro, sin
ningún mecanismo de recorte/scroll. Con más de ~2 Pokémon (el `PokemonContent` de 320 de
alto solo entra 1 fila completa), las filas siguientes se dibujaban igual, sin clip,
saliéndose por debajo del recuadro negro y hasta del borde de la pantalla.

**Fix (consultado con el usuario — se eligió agregar scroll, igual que en Inventario):**
se replicó el patrón `Scroll View → Viewport(Mask) → Content(GridLayoutGroup) +
Scrollbar Vertical`, leyendo los valores exactos del `ScrollRect`/`Mask`/`Scrollbar` ya
funcionando en `Inventory` (anchors, `movementType=Clamped`, `elasticity=0.1`, sprite del
handle, etc.) y aplicándolos a la nueva jerarquía bajo `PokemonContent`. El
`GridLayoutGroup` (mismo `cellSize`/`spacing`/`padding` que ya tenía) se movió del viejo
`PokemonContent` al nuevo `Content` (hijo de `Viewport`). `PokemonTabController
.contentPanel` se re-apuntó al nuevo `Content`. `PokemonContent` ahora es solo el fondo
negro contenedor (mismo rol que `InventoryContent`).

**Detalle importante que costó encontrar:** copiar los anchors/pivot de `Content` de
Inventory NO alcanza — el `GridLayoutGroup` nunca redimensiona su propio RectTransform (ni
Inventory ni Pokémon lo hacían), así que sin más, el `ScrollRect` no tenía forma de saber
cuánto contenido real había más allá de lo visible (`content.rect.height` se quedaba fijo
en el tamaño inicial). Se agregó un `ContentSizeFitter` (`verticalFit=PreferredSize`,
`horizontalFit=Unconstrained`) al nuevo `Content` de Pokémon para que su alto crezca según
la cantidad de filas que el propio `GridLayoutGroup` calcula. *(Nota: `Inventory`'s
`Content` tampoco tiene `ContentSizeFitter` — puede tener la misma limitación de scroll
con inventarios llenos; no se tocó por estar fuera del alcance de este pedido.)*

**Verificación:** Play Mode vía MCP, party llevado a 8 Pokémon (duplicando los 3
`CreatureData` disponibles en el proyecto — no hay tope de party en el código). Las 4
filas resultantes quedan contenidas y recortadas prolijamente por la máscara del
`Viewport`; haciendo scroll hasta el final se ve la última fila completa, proporción de
sprite conservada, sin superposiciones. Clic en un slot intermedio (`Arana`, requiere
scroll) seleccionó solo ese slot (`borderImage.enabled=true` únicamente ahí) y actualizó
el panel de detalles — selección intacta. 0 errores/warnings en consola.

**Nota aparte (no se tocó, fuera de alcance):** `PokemonDetailsPanel` (el panel a la
derecha del recuadro negro) tiene su propio bug de superposición de texto — el nombre del
Pokémon se dibuja encima del "HP: x/y" — visible en la captura de esta sesión. Es un
problema distinto (layout del panel de detalles, no del recuadro negro de la lista), no
pedido en esta tarea.

## 7. Bug encontrado y corregido (2026-08-07): panel de detalles del Pokémon demasiado angosto

**Síntoma:** el nombre y las estadísticas del Pokémon seleccionado no se leían bien; el
área de descripción se veía diminuta.

**Causa raíz (mismo patrón que la sección 6 del `MENU_INVENTORY_ANALYSIS.md`, "sobras"):**
`PokemonDetails` no tenía `LayoutElement` propio, así que dentro del
`HorizontalLayoutGroup` de `PokemonPanel` (`PokemonContent` + `PokemonDetails`) solo
recibía lo que sobraba después de que `PokemonContent.LayoutElement.minWidth = 430`
(heredado de cuando el grid necesitaba 2 columnas de celdas de 200) se quedaba con casi
todo el presupuesto — dejando apenas **67px de ancho** para todo el panel de detalles.
Dentro de `PokemonDetails`, `DetailsTitleTxt` y `DetailsStatsTxt` tampoco tenían
`LayoutElement` propio, así que el `VerticalLayoutGroup` los apretaba en ~79px de alto
combinados (fontSize 30 + fontSize 20 × 4 líneas) — de ahí la superposición visual.

**Fix (coordinado con el tamaño ya reducido del grid, que desde la sección 6 es
scrolleable y no necesita 2 columnas fijas):**
- `PokemonContent.LayoutElement.minWidth`: 430 → 250 (1 columna cómoda; el resto se
  scrollea, no se pierde nada).
- `PokemonDetails`: nuevo `LayoutElement(minWidth=240, flexibleWidth=0)` — el panel pasa
  de 67px a 240px de ancho (~3.5×).
- `DetailsImagePanel.LayoutElement.minHeight`: 150 → 110 (deja más aire vertical para el
  texto sin perder tamaño de sprite reconocible).
- `DetailsTitleTxt` y `DetailsStatsTxt`: cada uno con `LayoutElement.preferredHeight`
  propio (35 y 84 respectivamente, la suma con la imagen encaja en los ~239px de alto
  disponibles) + `enableAutoSizing=true` (título 16–26pt, stats 12–18pt) +
  `overflowMode=Overflow` — mismo patrón usado en `UIInventoryDescription` (sección 7 del
  otro doc): el texto nunca se corta con "…", el tamaño de letra se autoajusta un poco
  hacia abajo solo si hace falta.

**Bug aparte encontrado de paso (no relacionado con el tamaño): `DetailsImage` (el sprite
del Pokémon dentro de `DetailsImagePanel`) tenía anclajes full-stretch (`(0,0)-(1,1)`)
pero un `anchoredPosition` corrupto — `(-4948, -1188)` en vez de `(0, 0)`.** Como esos
anclajes no dependen de ningún `LayoutGroup` (el padre `DetailsImagePanel` no tiene uno,
solo `LayoutElement`), nada volvía a recalcular esa posición nunca — el sprite se
renderizaba miles de píxeles fuera de la pantalla, dejando el panel de imagen
completamente en blanco. No se sabe cuándo se originó (preexistente, no lo causó ningún
cambio de esta sesión ni de la anterior). Fix: `anchoredPosition = Vector2.zero`. También
se activó `Image.preserveAspect = true` en `DetailsImage` (tenía el mismo problema de
estiramiento que `PokemonSlotUI/Icon`, sección 6).

**Verificación:** Play Mode vía MCP, seleccionando Jack y luego Versionmini (clic
simulado): ambos sprites se ven completos y proporcionados, título sin recortes, las 4
líneas de stats (HP/Attack/Defense/Speed) legibles y sin superponerse, cambia
correctamente al alternar entre Pokémon. 0 errores/warnings en consola.

## 8. Bug encontrado y corregido (2026-08-07): Versionmini quedaba tapado por el borde del recuadro gris

**Síntoma:** al reducir el ancho de `PokemonContent` (sección 7, de 430 a 250 para
cederle espacio a `PokemonDetails`), la segunda columna de la grilla — donde caía
`Versionmini` — quedaba oculta/recortada contra el borde derecho del recuadro gris,
donde está el scrollbar.

**Causa raíz:** al construir el `Scroll View` de Pokémon (sección 6), `Content` (el
`RectTransform` con el `GridLayoutGroup`, dentro de `Viewport`) se creó con
`sizeDelta.x` fijo, copiado del ancho que tenía `PokemonContent` **en ese momento**
(~440). Cuando después se achicó `PokemonContent` a 250 (sección 7), `Content` no se
enteró — `ContentSizeFitter` solo controla el alto (`verticalFit=PreferredSize`), el
ancho se dejó "a mano" (`horizontalFit=Unconstrained`) y nunca se volvió a sincronizar.
Resultado: `Content` seguía siendo ~440 de ancho aunque el `Viewport` visible (con la
máscara que recorta) ya era solo ~233 — el `GridLayoutGroup`, viendo un `Content` de 440,
calculaba que entraban 2 columnas, y la segunda quedaba fuera del área realmente visible.

**Fix:** se cambiaron los anclajes de `Content` de un punto fijo (`(0,1)-(0,1)` con
`sizeDelta.x` manual) a **stretch horizontal** (`anchorMin=(0,1)`, `anchorMax=(1,1)`,
`sizeDelta.x=0`), para que su ancho siga automáticamente al `Viewport` real sin importar
cómo cambie `PokemonContent` en el futuro — ya no depende de un número copiado a mano en
un momento dado. El alto sigue creciendo con `ContentSizeFitter` como antes.

**Verificación:** Play Mode vía MCP — `Content.rect.width` y `Viewport.rect.width` ahora
coinciden siempre (233 en la resolución de prueba); `Jack` y `Versionmini` quedan en la
misma columna (mismo X, distinto Y), scrolleando se ve `Versionmini` completo y
centrado, tanto en la tarjeta de la grilla como en el panel de detalles al
seleccionarlo. 0 errores en consola.

*(Nota: `Inventory`'s `Content` fue creado con el mismo patrón de `sizeDelta.x` fijo —
no se tocó por no ser lo pedido, pero podría tener la misma fragilidad si su ancho
externo vuelve a cambiar más adelante.)*

## 9. Ajuste (2026-08-07): grilla de Pokémon en 2 columnas lado a lado

El usuario pidió explícitamente que los Pokémon queden **uno al lado del otro** (2
columnas) en vez de uno abajo del otro (1 columna, resultado de la sección 7) — con
1 columna, cambiar de Pokémon "se veía raro" al no haber una disposición espacial clara.

**Cambios:**
- `PokemonContent.LayoutElement.minWidth`: 250 → 260; `PokemonDetails.LayoutElement
  .minWidth`: 240 → 190 (se recorta un poco el panel de detalles para darle más ancho
  al grid — sigue siendo ~2.8× más ancho que el original de 67px de la sección 7).
- `GridLayoutGroup.cellSize` (en `Content`): 200×200 → 100×100 (mismo tamaño que usa el
  grid de Items del Inventario, por consistencia). Con el ancho de `Content` ahora
  siguiendo automáticamente al `Viewport` (sección 8), 100×100 dos columnas entran
  cómodas: `2*100 + spacing(10) = 210` contra un `Viewport` de ~243 tras el ajuste.
- `PokemonSlotUI.prefab` (la tarjeta individual) reescalada para la celda más chica:
  `VerticalLayoutGroup` padding 5→3, spacing 4→3; `Icon.LayoutElement` alto 120→48;
  `NameTxt`/`HpTxt` ahora tienen `LayoutElement` propio (alto 20/18) +
  `enableAutoSizing` (9–15pt / 8–13pt) — antes no tenían ninguno de los dos y dependían
  de lo que sobrara del `VerticalLayoutGroup`.

**Verificación:** Play Mode vía MCP, party llevado a 5 Pokémon. Dos columnas visibles
desde el primer render, sin superposición ni recorte; scrolleando hasta el final se ve
la 3ª fila (Versionmini solo) completa. Selección confirmada por código
(`borderImage.enabled` solo en el slot clickeado) y visualmente (panel de detalles
actualiza nombre/sprite/stats al cambiar de Pokémon). 0 errores en consola.

## 10. Adding a third tab, if you ever need one

1. Give the new panel a controller with `Show()`/`Hide()` methods (`SetActive` + whatever refresh
   logic it needs), same shape as `PokemonTabController`.
2. Add it as a sibling of `Inventory`/`PokemonPanel` under `InGameMenu` (so it shares the same
   `HorizontalLayoutGroup` slot and inherits the "same size" behavior for free). Make sure its root
   `localScale` is `(1, 1, 1)`.
3. Add a button under `TabBar`.
4. In `InventoryController`, add a `ShowXTab()` method that hides the other panels and shows the new
   one, and wire the button's `onClick` to it in `PrepareUI()`.
