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
