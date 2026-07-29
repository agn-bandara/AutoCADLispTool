# Upgrade Options — AutoCADLispTool

Assessment: 1 project (`AutoCADLispTool.csproj`), currently `.NETFramework,Version=v4.7.2`, legacy non-SDK csproj, WinForms class library plugin. Target `net8.0-windows`. 612 findings: 1 incompatible package (`AutoCAD.NET 24.3.0`), SDK-style conversion required, TFM change required, ~608 binary/source-incompatible API references (overwhelmingly WinForms/WPF surface). No security vulnerabilities.

## Strategy

### Upgrade Strategy
Single-project solution — there is no dependency graph to sequence, so tier-based approaches add overhead with no safety benefit.

| Value | Description |
|-------|-------------|
| **All-at-Once** (selected) | Upgrade the project in a single atomic pass — SDK conversion, TFM change, and package update land together. Fastest; the project will not build until all three complete. |

## Project Structure

### Project Approach
`AutoCADLispTool` is a class library with no `System.Web` dependency and no downstream consumers in the solution. You confirmed AutoCAD 2026 is the only supported host, so no Framework consumer needs to be served.

| Value | Description |
|-------|-------------|
| **In-place** (selected) | Replace the TFM directly with `net8.0-windows`. Clean single-TFM output; drops AutoCAD 2024 / .NET Framework 4.7.2 support. |
| Multi-targeting | Build `net472;net8.0-windows` side by side so one repo serves AutoCAD 2024 and 2026. Requires `#if` guards and two AutoCAD.NET package versions conditioned per TFM. |

## Compatibility

### Unsupported API Handling
~608 `Api.0001` findings are flagged, but nearly all are WinForms/WPF types whose assembly identity moved to `Microsoft.WindowsDesktop.App` — they are resolved wholesale by enabling `UseWindowsForms`/`UseWPF`, not by editing call sites. Only the small `Api.0002` source-incompatible set needs real work.

| Value | Description |
|-------|-------------|
| **Fix Inline** (selected) | Resolve every API change within the upgrade task. No stubs, no deferred cleanup. Appropriate here given the genuine breakage surface is small. |
| Defer Complex Changes | Stub out complex replacements to keep the project compiling and create follow-up subtasks. Intended for large multi-project migrations. |

## Modernization

### Nullable Reference Types
Target `net8.0-windows` supports NRTs and the project does not currently set `<Nullable>`. The codebase is small enough that enabling is feasible, but doing so during the framework migration mixes two unrelated sources of compiler noise.

| Value | Description |
|-------|-------------|
| **Leave Disabled** (selected) | Do not set `<Nullable>`. Keeps the migration diff focused on framework/runtime concerns. Can be enabled afterwards as its own effort. |
| Enable Nullable Reference Types | Add `<Nullable>enable</Nullable>` now and fix resulting warnings as part of the upgrade. |
