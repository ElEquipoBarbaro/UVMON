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

- **`read_console` showing zero errors for your own files does NOT mean
  `Assembly-CSharp` actually compiled.** Unity's default assembly implicitly
  references *every* other precompiled/package assembly in the project. If **any**
  package has a hard compile error — even one totally unrelated to your code, e.g. a
  Burst-dependent package that failed to resolve — `Assembly-CSharp` silently never
  finishes building, and no `Assembly-CSharp.dll` shows up in
  `Library/ScriptAssemblies` at all. Symptoms that reveal this (all observed together
  once, 2026-08-01, root cause: `com.unity.burst` failed to install with `ENOSPC` —
  the machine's `C:` drive had ~190-450 MB free, see `Get-PSDrive C`): `read_console`
  shows only the *package's* CS0246 errors, never anything under `Assets/Scripts`;
  `AssetDatabase.LoadAssetAtPath` (via `execute_code`) returns `null` for **any**
  asset whose type lives in `Assembly-CSharp` (e.g. every `CreatureData` instance,
  even ones untouched this session); `manage_scriptable_object action=modify`
  reports `target_not_found` for those same assets even though `manage_asset
  action=get_info` resolves the guid/path fine; `mcpforunity://tests` returns a
  root node with **zero** children for both EditMode and PlayMode no matter how many
  times you `refresh_unity` or open the Test Runner window; components that
  definitely exist in the saved `.unity` YAML (e.g. `MoveOptionUI` on
  `MoveOptionTemplate`) are silently missing from
  `mcpforunity://scene/gameobject/{id}/components`. None of this is fixable from
  scripts/scene edits — check `Get-PSDrive C` and `read_console` for package
  resolution errors (`"An error occurred while resolving packages"`) *first* whenever
  MCP tools that touch `Assembly-CSharp` types start failing in these specific ways.
  Fix: free disk space (safe to delete:
  `%LOCALAPPDATA%\Unity\cache\packages`, the package download cache — Unity
  re-downloads on demand) and reopen the project / retry package resolution; you
  cannot work around it by editing more scripts.
  - **When this is blocking you**: you can still make real progress without a
    working `Assembly-CSharp` — plain-Unity-type scene edits (`RectTransform`,
    `Image`, `CanvasRenderer`, `TextMeshProUGUI`, `Shadow`, ...) via
    `manage_gameobject`/`manage_components` work fine (those types live in engine/UGUI
    assemblies, not `Assembly-CSharp`), and hand-editing a ScriptableObject `.asset`'s
    YAML directly (`Read`/`Edit`, not the MCP asset tools) is a safe fallback for
    setting fields Unity itself can't currently deserialize through — it'll be picked
    up correctly once compilation is unblocked, since YAML doesn't care about the
    live AppDomain. What you *cannot* do: attach a custom `MonoBehaviour`/assign a
    field that's typed as one of your own classes (the type doesn't exist as a
    loaded `Type` yet), or trust `execute_code` snippets that reference your own
    project types (only engine/editor namespaces resolve).

- **A `List<T>`/array field on a `ScriptableObject`, edited by hand in YAML, uses
  Unity's `Array.size` + `Array.data[i].field` shape** — e.g.
  `bodyParts:\n  - idParte: body\n    vidaMaxima: 70\n    ...` (a plain YAML sequence
  of mappings works directly, no `Array.size`/`data[]` needed in the raw `.asset`
  file — that indexed-path form is only for `SerializedProperty` paths used by tools
  like `manage_scriptable_object`'s `patches`, not for hand-written YAML).

- **Single-sprite-mode texture importers (`spriteMode: 1`) always expose their Sprite
  sub-asset as `{fileID: 21300000, guid: <texture guid>, type: 3}`** — confirmed by
  reading multiple existing `CreatureData` assets' `frontSprite`/`backSprite`
  references. Useful when hand-writing a sprite reference in YAML instead of going
  through `manage_scriptable_object`.

- **`manage_texture action=set_import_settings`'s `import_settings` dict key for
  Read/Write Enabled is `"readable": true`** (it maps internally to the importer's
  `isReadable`, per the tool's own `_debug_params` echo). Needed before
  `Image.alphaHitTestMinimumThreshold` will actually do anything (that feature reads
  pixels via `Texture2D.GetPixelBilinear`, which throws/no-ops on a non-readable
  texture).

- **Fixed 2026-08-01**: freeing disk space alone did *not* get `com.unity.burst` to
  install — the earlier failed resolution attempt stays stuck until you explicitly
  force a retry with `manage_packages action=resolve_packages`. After that,
  `refresh_unity(compile=request, mode=force)` picked it up and `Assembly-CSharp`
  compiled cleanly on the next pass (confirmed via `Library/ScriptAssemblies` and
  `AssetDatabase.LoadAssetAtPath` no longer returning `null`). If you hit the
  broken-compile symptoms described above, check disk space *and* remember to call
  `resolve_packages` once space is free — don't just wait/retry refresh.

- **`manage_components action=add` fails the same way `modify`/`delete` do for
  anything inside the (inactive) `BattleUI` tree** (`"not found using method
  'default'"`) — the earlier-documented workaround (`execute_code` +
  `Resources.FindObjectsOfTypeAll<Transform>()` + `go.AddComponent(typeof(X))`,
  then `EditorSceneManager.MarkSceneDirty`) covers `add` too, not just
  `modify`/`delete`. `set_property` on components already present is still the one
  action that works normally even inside that inactive tree.

- **Scene-object instance IDs are not stable across a domain reload/scene
  reload** (e.g. after `resolve_packages` or any compile that actually completes a
  reload) — an ID captured before the reload (from `find_gameobjects` or a `create`
  call) can 404 afterward even though the GameObject itself is untouched. Re-run
  `find_gameobjects` by name to get fresh IDs before wiring references post-reload;
  don't assume an ID from earlier in the session is still valid after any
  `recovered_from_disconnect`/domain-reload event.

- **Verifying a UI click handler actually works, without a human clicking**: build
  with `execute_code` and `UnityEngine.EventSystems.ExecuteEvents.Execute(gameObject,
  new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler)` —
  this runs the exact same `IPointerClickHandler.OnPointerClick` callback a real
  mouse click would, through the real event pipeline, so it's a faithful way to
  confirm click-to-select logic (e.g. body-part targeting) end-to-end in Play mode
  via MCP instead of just trusting the code compiles. Find the live GameObject by
  name via `Resources.FindObjectsOfTypeAll(typeof(GameObject))` — runtime-instantiated
  clones share the prefab/template's name (`"X(Clone)"`), so also check a
  distinguishing component property (e.g. an `Index` field) to pick the right one
  when several clones exist.
