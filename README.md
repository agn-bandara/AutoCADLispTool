# AutoCAD Lisp Tool

AutoCAD Lisp Tool is a small Windows Forms utility (AutoCAD .NET plugin) that automates running AutoLISP routines and AutoCAD commands across multiple DWG files.

It was created to help batch-process drawings by loading a .lsp file or executing a command for each selected drawing, saving results and producing a per-session log.

## Features
- Load a .lsp file and execute a LISP function per drawing
- Execute raw AutoCAD commands
- Select multiple .dwg files (multi-select or append more files)
- Optionally save (QSAVE) and close drawings after processing
- Progress UI and per-drawing result status
- Per-session log files written to a Logs folder next to the executable

## Prerequisites
- AutoCAD installed (ensure the referenced AutoCAD .NET assemblies match the installed version)
- .NET Framework 4.7.2
- Visual Studio (recommended) or MSBuild for building

## Building
1. Open the solution or the project in Visual Studio.
2. Make sure the project references the Autodesk AutoCAD .NET assemblies (Autodesk.AutoCAD.ApplicationServices, Autodesk.AutoCAD.DatabaseServices, Autodesk.AutoCAD.EditorInput) and point them to your AutoCAD installation folder.
3. Build the project targeting .NET Framework 4.7.2.

Note: If you plan to load the tool inside AutoCAD, compile as a Class Library (DLL) and use NETLOAD in AutoCAD. If building an EXE, ensure the runtime environment has access to the AutoCAD .NET assemblies.

## Usage
1. Start AutoCAD and use NETLOAD to load the compiled plugin (recommended) or run the compiled EXE when appropriate for your environment.
2. In the tool UI:
   - Click "Load" to select a .lsp file (optional if you will only run a command string).
   - Enter a LISP function call or AutoCAD command in the command textbox. Examples:
     - LISP: (my-function arg1 arg2)
     - Command: -PURGE A
   - Click "Select Drawings" or "Append" to pick one or more DWG files.
   - Optionally check "Close" to save and close each drawing after processing.
   - Click "Process" to run the LISP/command for every selected drawing.
3. Monitor progress and per-drawing results in the UI. Logs are written to the Logs folder next to the executable.

## Logging
- Each session creates a log file named like `LispTool_Log_YYYYMMDD_HHMMSS.txt` inside a `Logs` folder next to the executable.
- Logs include a timestamped session header and incremental per-drawing entries with STATUS (SUCCESS/ERROR).

## Troubleshooting
- Missing AutoCAD assemblies or version mismatch: verify references and AutoCAD installation path.
- No result from LISP: ensure the LISP routine returns a value or writes to the editor for debugging.
- Permission issues saving DWG: ensure the user account has write access to the files; the tool attempts to clear read-only attributes, but elevated permissions may be required.
- UI responsiveness: running the tool inside AutoCAD (NETLOAD) is recommended to avoid cross-process issues. The tool uses document locks and asynchronous patterns to minimize UI blocking.

## Extending the tool / Ideas
- Add parallel processing (requires careful handling of the AutoCAD API and document locks).
- Provide options for different save behaviors (SaveAs, backup folder).
- Export log in CSV or JSON for easier parsing and reporting.
- Add test harness and sample LISP routines for validation.

## Contributing
- Fork the repository, add changes on a topic branch and open a Pull Request with a clear description and testing notes.

## License
- No license file is included. Add a LICENSE file to make terms explicit before publishing.

## Disclaimer
This tool automates changes in AutoCAD drawings. Always back up drawings before running batch operations.