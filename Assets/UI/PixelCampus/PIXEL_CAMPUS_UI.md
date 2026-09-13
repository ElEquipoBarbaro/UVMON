# Interfaz Liquid Crystal de UVGMon

## Dirección visual

La interfaz usa superficies planas azul marino para mantener legibilidad y concentra el acabado Liquid Crystal exclusivamente en los bordes. Los contornos combinan azul, cian y verde con reflejos puntuales; el centro de cada panel permanece uniforme, sin textura de vidrio, ruido ni halos blancos.

Paleta principal: Ink `#030E19`, Interior `#0A2230`, Crystal Blue `#2FAACE`, Crystal Cyan `#69E1EC`, Crystal Green `#2ACC8B` e Ivory `#F4F0D3`.

## Recursos PNG

Los recursos reutilizables están en `Assets/UI/PixelCampus/Sprites`. Todos son PNG RGBA con transparencia real fuera del contorno, sin mipmaps, sin compresión y con bordes 9-slice cuando corresponde.

- Core: panel, barra de título, botones, pestañas, cierre, insignia, selección y scrollbar.
- Inventory: ranura de objeto y ranura de UVGmon.
- Combat: panel de estado, mensaje, relleno de HP y anillo QTE.

La fuente reproducible es `Assets/UI/PixelCampus/Editor/LiquidCrystalUIRefiner.cs`. Se ejecuta desde `Tools > UVGMon > Apply Liquid Crystal UI Refinement`. El menú anterior `Build Pixel Campus Assets` ahora delega al mismo refinador para evitar regenerar el estilo obsoleto.

## Correcciones de composición

- Textos configurados con autoajuste, márgenes internos, elipsis y salto de línea según el tipo de contenido.
- Slots de inventario y equipo normalizados a escala 1.
- Iconos de objetos y UVGmones contenidos dentro de cajas con `preserveAspect`.
- Cantidades reposicionadas dentro de la esquina inferior derecha de cada ranura.
- Panel de detalles dividido en imagen, título y descripción con áreas independientes.
- Barra de pestañas del menú ampliada y etiquetas limitadas a su propio botón.
- Diálogo reconstruido sin el marco blanco anterior; nombre, texto y botón permanecen dentro del panel.
- Combate reorganizado para separar criaturas, estados, pestañas, comandos y mensaje.
- Plantillas de inventario y equipo de combate reconstruidas para impedir que iconos, HP y botones se traslapen.

## Escenario de combate

El nuevo fondo está en `Assets/Art/Combat/LiquidCrystalBattleArena.png`. Incluye una plataforma despejada inferior izquierda para el aliado, una plataforma superior derecha para el enemigo y una franja inferior oscura de baja complejidad para la interfaz.

Las posiciones de referencia en el Canvas 1920x1080 son:

- UVGmon aliado: `(-535, -40)`, caja `330x280`.
- UVGmon enemigo: `(475, 195)`, caja `300x240`.
- Estado aliado: `(-650, -300)`, caja `390x104`.
- Estado enemigo: `(610, 360)`, caja `390x96`.
- Paneles de comando: `(250, -365)`, caja `800x230`.
- Mensaje: `(0, -503)`, caja `1760x70`.

## Archivos integrados

- `Assets/Scenes/jardinconocimiento.unity`
- `Assets/InventoryScene.unity`
- `Assets/Prefabs/Inventory.prefab`
- `Assets/Prefabs/ItemUI.prefab`
- `Assets/Prefabs/PokemonSlotUI.prefab`
- `Assets/Prefabs/PauseManager.prefab`

La lógica de inventario, arrastrar y soltar, curación, cambio de equipo y combate se conserva. Los sprites de UI mantienen sus rutas para no romper referencias serializadas.

## Verificación

- Compilación de scripts sin errores.
- Escalas de plantillas y paneles normalizadas.
- Fondo de combate importado como Sprite PNG.
- Texto y contenido limitados por sus márgenes.
- Exterior de los recursos PNG completamente transparente y sin píxeles blancos.
