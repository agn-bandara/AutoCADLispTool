# .NET Version Upgrade

## Preferences
- **Flow Mode**: Guided
- **Target Framework**: `net8.0-windows`

## Source Control
- **Source Branch**: master
- **Working Branch**: upgrade-autocad-2026-net8
- **Commit Strategy**: After Each Task
- **Branch Sync**: Auto (Merge)

## Key Decisions Log
- **2026 target runtime**: AutoCAD 2026 hosts the .NET 8 runtime. Target framework is pinned to
  `net8.0-windows` and MUST NOT be raised to net9.0/net10.0 — a higher-version assembly will not
  load into AutoCAD's .NET 8 host.
- **Version support**: AutoCAD 2026 only. .NET Framework 4.7.2 / AutoCAD 2024 support is dropped
  (user decision). No multi-targeting.
- **Project format**: Legacy `.csproj` must be converted to SDK-style — required for .NET 8.
- **AutoCAD.NET package → `25.1.0` (CORRECTED, verified 2026-XX)**: Upgrade from `24.3.0`
  (AutoCAD 2024) to **`25.1.0`** — NOT `26.0.0`.
  Evidence from the user's installed `C:\Program Files\Autodesk\AutoCAD 2026`:
  - `acdbmgd.dll` / `acmgd.dll` / `accoremgd.dll` FileVersion = `25.1.164.0.0`
  - `acdbmgd.runtimeconfig.json` declares `"tfm": "net8.0"` and requires
    `Microsoft.NETCore.App 8.0.0` + `Microsoft.WindowsDesktop.App 8.0.0`
  - NuGet `AutoCAD.NET 26.0.0` ships `lib/net10.0/` assemblies and is therefore for a
    LATER AutoCAD release — using it would produce a plugin AutoCAD 2026 cannot load.
  This confirms `net8.0-windows` as the correct TFM.

## Execution Constraints
- Enable `<UseWindowsForms>true</UseWindowsForms>` and `<UseWPF>true</UseWPF>` — this resolves
  the bulk of the ~608 `Api.0001` WinForms/WPF binary-incompatibility findings wholesale.
  Do NOT hand-edit those call sites.
- AutoCAD assemblies must NOT be copied to output (they are supplied by the AutoCAD process).
  Use `ExcludeAssets="runtime"` / `<Private>false</Private>` semantics on the AutoCAD package ref.
- Drop legacy artifacts during SDK conversion: `ProjectTypeGuids`, `AllRules.ruleset`
  (`CodeAnalysisRuleSet`), ClickOnce/publish properties, `FileUpgradeFlags`,
  `UpgradeBackupLocation`, explicit framework `<Reference>` items.
- Plugin loads via `NETLOAD`; there is no app host. Output stays a class library.
