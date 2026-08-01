# UVMON — Project Notes

## MCP for Unity

This project has **MCP for Unity** (CoplayDev) configured, giving Claude Code a live
bridge into the running Unity Editor.

- **Package**: `com.coplaydev.unity-mcp`, sourced from
  `https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#main`
  (declared in `Packages/manifest.json`)
- **MCP server registration** (in `~/.claude.json`, scoped to this project path):
  ```json
  "UnityMCP": {
    "type": "http",
    "url": "http://127.0.0.1:8080/mcp"
  }
  ```
- **Transport**: HTTP, served by the Unity Editor itself while it's open (not a
  separate standalone process).
- **Runtime state file**: `Library/MCPForUnity/RunState/mcp_http_8080.pid` holds the
  PID of the Unity Editor process currently hosting the MCP server on port 8080.
  If the tools aren't responding, check whether that PID is still alive and whether
  the Unity Editor is open — the server only exists while the Editor is running.

Do not re-derive this by grepping `Packages/manifest.json` or `~/.claude.json` again —
this file is the source of truth. If the port/PID ever changes, update this section.

## Gotchas when editing scenes via Unity MCP (learned building the Capture/Combat UI systems)

- **`localScale = (0,0,0)` bug on objects created under an inactive parent.** `BattleUI`
  (and everything under it) stays `activeSelf = false` at rest — it's only turned on
  during battle. Any GameObject created with `manage_gameobject action=create` while its
  parent chain is inactive comes out with `m_LocalScale = (0,0,0)` instead of `(1,1,1)`,
  making it (and everything nested under it) invisible even though every reference is
  wired correctly. Two ways to avoid it:
  1. Immediately after creation, explicitly set `localScale` via
     `manage_components action=set_property` (e.g. `{"localScale": {"x":1,"y":1,"z":1}}`
     on the `RectTransform`) — writing the property explicitly overrides the bug, no
     separate fix step needed.
  2. Or fix it after the fact with `execute_code`: find the object via
     `Resources.FindObjectsOfTypeAll<Transform>()` (searching by name, since normal
     `Transform.Find` from an *active* root won't reach into an inactive tree the way
     you'd expect either) and set `transform.localScale = Vector3.one` directly.
  This has bitten the QTE system, the Capture system, and the move-selection buttons —
  always double check `localScale` on new BattleUI children.

- **`manage_gameobject action=modify/delete` fails with `"not found using method
  'default'/'by_id'"` for anything inside the (inactive) `BattleUI` tree**, even though
  `find_gameobjects` and the `mcpforunity://scene/gameobject/{id}` resource find it fine,
  and `manage_components action=set_property` on it works fine too. Workaround: do
  `SetActive`, `Destroy`, `SetSiblingIndex`, etc. via `execute_code`, locating the object
  with `Resources.FindObjectsOfTypeAll<Transform>()` + `Find("child/path")`, then call
  `UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(...)` so the change
  actually persists on next `manage_scene action=save`.

- **`manage_components action=set_property` `target` is always the GameObject's own
  instance ID**, not the specific component's instance ID (`component_type` picks the
  right component on that GameObject). But when the *value* of a property is itself an
  object reference to a component (e.g. wiring a `RectTransform`/`Image`/custom
  component field), pass **that component's own instance ID**, not its GameObject's —
  read it from the `mcpforunity://scene/gameobject/{id}/components` resource first
  (GameObject id and component id are different numbers).

- **`execute_code` compiles with CodeDom (C# 6) in this project**, not Roslyn — no local
  functions (`ReturnType Name(...) { }` inside a method body); use `Func<>`/`Action<>`
  lambdas instead. Also pass real `<`/`>`/`&` characters in code strings, never
  HTML-escaped `&lt;`/`&gt;`/`&amp;` — those get compiled literally and fail.

- **`BattleUI`'s existing children use an inconsistent manual-scale convention**: the
  root itself has `localScale = 2`, and most direct children (background, HP/message
  text) use `localScale ≈ 2.789` on top of that (creature sprite views use yet other
  values, ~3.44/4.84). When adding a *content* element directly under `BattleUI`, match
  that `2.789` sibling convention for position/size to land visually correct. When
  building a *full-screen overlay* system instead (QTE, Capture), skip that entirely —
  use `anchorMin (0,0)` / `anchorMax (1,1)` stretch with `localScale = 1`, which is
  resolution-independent and doesn't care about the parent's ad-hoc scale.

- **Sibling order = draw order = raycast blocking, and this doubles as a free
  show/hide mechanism.** A later sibling draws on top of earlier ones; a full-screen
  `Image` with `raycastTarget = true` (like QTE's/Capture's dim overlay) also blocks
  clicks to everything behind it. The move-selection buttons only needed to disappear
  and stop being clickable while a QTE/Capture overlay is up — moving
  `MoveOptionsContainer` to a sibling index *before* `QTE`/`Capture` did that for free,
  no extra `SetActive` calls needed in `CombatManager`.

- **Toggle color, not `.enabled`, for a highlight `Image` that also needs to stay
  clickable.** `MoveOptionUI` uses one `Image` as both the raycast target and the
  hover/selection highlight — disabling it to hide the highlight would also stop it
  receiving pointer events. `SetHighlighted` swaps its color/alpha instead.

- **`generate_image` needs external provider API keys (fal.ai/OpenRouter) that aren't
  configured here.** For simple icons/textures (e.g. `Assets/Sprites/UI/cursor_pointer.png`,
  same idea as `Assets/Sprites/QTE/ring.png`), generate a `Texture2D` procedurally in
  `execute_code` (`SetPixels`/`Apply` → `EncodeToPNG` → `File.WriteAllBytes` →
  `AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport)`) instead.
  For a custom OS cursor specifically, set `TextureImporterType.Cursor` +
  `alphaIsTransparency = true` on the importer, and remember `Cursor.SetCursor`'s
  hotspot is measured from the texture's **top-left**, while the pixel buffer you filled
  with `SetPixels` is **bottom-left**-origin — flip the Y when computing the hotspot.

- **Every subsystem folder gets its own analysis `.md`** (`COMBAT_SYSTEM_ANALYSIS.md`,
  `MENU_INVENTORY_ANALYSIS.md`, `Combat/QTE/QTE_SYSTEM.md`,
  `Combat/Capture/CAPTURE_SYSTEM.md`). Check for one before re-deriving how a system
  works, and update it (including a dated "bug found and fixed" entry, which has
  repeatedly saved a future session from rediscovering the same MCP quirks above) when
  changing that system.

- **Workflow that avoided repeated pain**: after any script edit,
  `refresh_unity(compile=request, mode=force)` → `read_console(types=[error,warning])`
  *before* touching the scene (a scene wired against a component that failed to compile
  fails silently/confusingly later). End every batch of scene edits with
  `manage_scene action=save`.
