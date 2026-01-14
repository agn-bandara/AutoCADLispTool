# AutoCAD LISP Tool

A high-performance Windows Forms utility that automates running AutoLISP routines and AutoCAD commands across multiple DWG files using the AutoCAD .NET API.

## Features

### Core Functionality
- **Batch Processing**: Process multiple DWG files with a single LISP routine
- **LISP File Loading**: Load and execute .lsp files across drawings
- **Command Execution**: Execute raw AutoCAD command strings
- **Flexible Selection**: Batch-select multiple DWG files or append more dynamically
- **Auto-Save**: Automatically save (QSAVE) drawings after processing
- **Optional Close**: Choose whether to close drawings after processing
- **Real-time Progress**: Live progress tracking with percentage and current file display
- **Result Visualization**: Color-coded per-drawing results in the UI (green=success, red=error)

### Performance Optimizations (V2)
- **Buffered Logging**: 80-90% reduction in disk I/O operations through intelligent log buffering
- **Configurable Delays**: Tunable processing delays for optimal performance on different systems
- **Async Processing**: Non-blocking UI with proper async/await patterns
- **Memory Efficient**: Fixed memory leaks and proper resource disposal

### Architecture Improvements (V2)
- **Separation of Concerns**: Clean architecture with Models and Services layers
- **BufferedLogger Service**: High-performance logging with auto-flush capability
- **ProcessingConfig Service**: Centralized, configurable processing parameters
- **DrawingResult Model**: Structured data model for processing results
- **Proper Disposal**: IDisposable implementation for resource cleanup

## Prerequisites

- **AutoCAD 2024** (or compatible version matching your referenced assemblies)
- **.NET Framework 4.7.2**
- **Visual Studio 2019+** or MSBuild to compile

## Installation & Setup

### 1. Clone the Repository
```bash
git clone https://github.com/agn-bandara/AutoCADLispTool.git
cd AutoCADLispTool
```

### 2. Configure AutoCAD References
Open `AutoCADLispTool.csproj` and verify the HintPath entries point to your AutoCAD installation:
```xml
<Reference Include="AcMgd">
  <HintPath>C:\Program Files\Autodesk\AutoCAD 2024\AcMgd.dll</HintPath>
</Reference>
<Reference Include="AcDbMgd">
  <HintPath>C:\Program Files\Autodesk\AutoCAD 2024\AcDbMgd.dll</HintPath>
</Reference>
<Reference Include="AcCoreMgd">
  <HintPath>C:\Program Files\Autodesk\AutoCAD 2024\AcCoreMgd.dll</HintPath>
</Reference>
```

### 3. Build the Project
```bash
# Using MSBuild
msbuild AutoCADLispTool.csproj /p:Configuration=Release

# Or open in Visual Studio and build (Ctrl+Shift+B)
```

### 4. Load into AutoCAD
1. Launch AutoCAD
2. Type `NETLOAD` at the command line
3. Browse to `bin\Release\AutoCADLispTool.dll`
4. Type `LISPTOOL` to launch the UI

## Usage

### Basic Workflow
1. **Load LISP File** (Optional): Click "Load .lsp" to select your LISP file
2. **Enter Command**: Type the LISP function name (e.g., `(MyFunction)`)
3. **Select Drawings**: Click "Dwgs" to select one or more .dwg files
4. **Process**: Click "Process" to run the command across all drawings
5. **Review Results**: Check the color-coded results in the list

### Advanced Features

#### Append More Drawings
Use the "Append" button to add more drawings to the current batch without clearing existing selections.

#### Optional Close
Uncheck "Close drawings after processing" to leave drawings open for manual review.

#### View Logs
Logs are automatically saved to the `Logs` folder with timestamps:
```
Logs/LispTool_Log_20250115_143022.txt
```

Each log includes:
- Session start/end timestamps
- Per-drawing processing status
- Success/error messages
- Full command output

## Project Structure

```
AutoCADLispTool/
├── MainForm.cs                    # Main UI form (~500 lines)
├── MainForm.Designer.cs           # Auto-generated designer code
├── myPlugin.cs                    # AutoCAD plugin entry point
├── myCommands.cs                  # AutoCAD command definitions
├── Models/
│   └── DrawingResult.cs           # Data model for processing results
├── Services/
│   ├── BufferedLogger.cs          # High-performance buffered logging
│   └── ProcessingConfig.cs        # Configurable processing parameters
└── Properties/
    └── AssemblyInfo.cs            # Assembly metadata
```

## Configuration

### Processing Delays
Edit `ProcessingConfig.cs` to tune delays for your system:

```csharp
public class ProcessingConfig
{
    public int DocumentActivationDelayMs { get; set; } = 50;   // Default: 50ms
    public int LispLoadDelayMs { get; set; } = 150;            // Default: 150ms
    public int CommandExecutionDelayMs { get; set; } = 150;    // Default: 150ms
    public int SaveDelayMs { get; set; } = 300;                // Default: 300ms
}
```

### Log Buffering
Adjust the flush threshold in `BufferedLogger`:
```csharp
var logger = new BufferedLogger(logPath, flushThreshold: 10); // Flush every 10 messages
```

## Performance Benchmarks

| Metric | Before V2 | After V2 | Improvement |
|--------|-----------|----------|-------------|
| Log writes per drawing | 5-10 | 1 (batched) | 80-90% reduction |
| Processing delay per drawing | 1100ms | 650ms | 41% faster |
| Memory leak (ToolTip) | Yes | Fixed | Stable memory |

## Troubleshooting

### Build Errors
- **Missing AutoCAD references**: Update HintPath in `.csproj` to match your AutoCAD installation path
- **Wrong .NET version**: Ensure project targets .NET Framework 4.7.2

### Runtime Issues
- **Drawings won't open**: Verify AutoCAD has permissions to the drawing files
- **Commands fail**: Check that LISP file loads successfully before running commands
- **Read-only errors**: Tool automatically removes read-only attributes before saving

### Performance Issues
- **Slow processing**: Reduce delay values in `ProcessingConfig`
- **Memory growth**: Ensure you're using the latest version with proper disposal

## Development

### Building from Source
```bash
# Debug build
msbuild AutoCADLispTool.csproj /p:Configuration=Debug

# Release build
msbuild AutoCADLispTool.csproj /p:Configuration=Release
```

### Running Tests
Currently, the project doesn't include unit tests. Recommended testing approach:
1. Test with 1-2 small test drawings
2. Verify LISP commands execute correctly
3. Check log files for errors
4. Scale up to larger batches

## Version History

### Version 2.0 (2025)
- ✅ Implemented buffered logging for 80%+ I/O reduction
- ✅ Added configurable processing delays
- ✅ Refactored to Models/Services architecture
- ✅ Fixed ToolTip memory leak
- ✅ Removed unused imports and dead code
- ✅ Renamed MainFom → MainForm (typo fix)
- ✅ Improved error handling and logging
- ✅ Added proper IDisposable implementation

### Version 1.0 (2024)
- Initial release with basic batch processing
- LISP file loading and command execution
- Progress tracking and logging

## Contributing

Contributions are welcome! Please:
1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## Important Notes

⚠️ **Always backup drawings before running batch operations**

✅ **Running via NETLOAD in AutoCAD is the recommended approach**

📝 **Check logs for detailed processing information**

## License

This project is provided as-is for educational and productivity purposes.

## Support

For issues, questions, or feature requests:
- Open an issue on GitHub: https://github.com/agn-bandara/AutoCADLispTool/issues
- Check existing issues for solutions

## Acknowledgments

Built with the AutoCAD .NET API and Windows Forms.

---

**Last Updated**: January 2026  
**Version**: 2.0  
**Author**: AGN Bandara
