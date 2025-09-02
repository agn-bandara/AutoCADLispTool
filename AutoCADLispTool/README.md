# AutoCAD Lisp Tool

AutoCAD Lisp Tool is a small Windows Forms utility that automates running AutoLISP routines and AutoCAD commands across multiple DWG files using the AutoCAD .NET API.

Features
- Load a .lsp file and run a LISP function per drawing
- Execute raw AutoCAD command strings
- Batch-select multiple DWG files or append more
- Save (QSAVE) and optionally close drawings after processing
- Per-session logs written to a Logs folder next to the executable
- Progress and per-drawing results shown in the UI

Prerequisites
- AutoCAD (matching version of referenced AutoCAD .NET assemblies)
- .NET Framework 4.7.2
- Visual Studio or MSBuild to compile

Quick start
1. Open the project in Visual Studio and ensure references to Autodesk.AutoCAD.* assemblies point to your AutoCAD installation.
2. Build the project targeting .NET Framework 4.7.2.
3. Load the compiled plugin into AutoCAD using NETLOAD or run the EXE if appropriate.
4. Use the UI to select a LISP file, select drawings, enter a command, and click Process.

Logging
Logs are saved to a Logs directory next to the executable with filenames like LispTool_Log_YYYYMMDD_HHMMSS.txt.

Notes
- Always backup drawings before running batch operations.
- Running the UI from within AutoCAD via NETLOAD is the recommended and most reliable approach.
