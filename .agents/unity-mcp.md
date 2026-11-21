# Unity MCP

## Status
- CoplayDev MCP for Unity 10.3.0 installed through Unity Package Manager; pinned to `v10.3.0`.
- Unity compiled its editor assembly and ran its startup hook. A working external MCP session has not been verified.

## Entry files
- `Packages/manifest.json`, `Packages/packages-lock.json`.
- Installed editor source: `Library/PackageCache/com.coplaydev.unity-mcp@*/Editor` (generated cache, do not edit).

## Behavior/rules
- Separate integration from Unity Pipeline; keep both packages.
- Computer/screen control remains forbidden. Use Unity CLI/APIs for operations.

## Public interfaces/dependencies
- UPM package URL: `https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#v10.3.0`.
- Upstream requires Python 3.10+ and uv for its external server. uv was not found on this shell's PATH during installation.
- Client registration and server connectivity remain unverified.

## Setup/verification
- Upstream: https://github.com/CoplayDev/unity-mcp.
- Unity menu entries include MCP window and Local Setup Window; inspect `Editor/Constants/ProductInfo.cs` for menu root.
- Verify installation with `unity command "package list"` and compilation with `unity command "script recompile_status"`, targeting this project's absolute path.
- Logs: `Logs/Editor.log` and `Logs/upm.log`.
