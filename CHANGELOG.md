# Changelog

All notable changes to the AutoCAD LISP Tool project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.0.0] - 2025-01-15

### Added
- **BufferedLogger Service**: High-performance buffered logging with automatic flush
  - Reduces disk I/O operations by 80-90%
  - Auto-flush timer (5 seconds) and threshold-based flushing (10 messages)
  - Thread-safe implementation
- **ProcessingConfig Service**: Centralized configuration for processing delays
  - Configurable document activation delay (default: 50ms)
  - Configurable LISP load delay (default: 150ms)
  - Configurable command execution delay (default: 150ms)
  - Configurable save delay (default: 300ms)
  - Support for loading configuration from file
- **DrawingResult Model**: Structured data model for processing results
  - Stores drawing name, path, status, and messages
  - Tracks processing time and error state
- **Models/Services Architecture**: Clean separation of concerns
  - Models folder for data structures
  - Services folder for business logic
  - Improved maintainability and testability

### Changed
- **Renamed MainFom ? MainForm**: Fixed filename typo for better code maintainability
  - Updated all references in project files
  - Updated myCommands.cs to use correct class name
- **Optimized Processing Delays**: Reduced total delay per drawing from 1100ms to 650ms
  - 41% improvement in processing speed
- **Improved Memory Management**: 
  - Fixed ToolTip memory leak (single instance, reused)
  - Proper IDisposable implementation on MainForm
  - Cleanup of CancellationTokenSource, BufferedLogger, and ToolTip
- **Enhanced Async Processing**: Better async/await patterns throughout
  - Non-blocking UI during processing
  - Proper thread-safe UI updates
  - Removed Application.DoEvents() calls

### Removed
- **Unused Imports**: Cleaned up all source files
  - Removed System.Security.Cryptography from MainForm.cs
  - Removed System.ComponentModel from MainForm.cs
  - Removed System.Data from MainForm.cs
  - Removed System.Text from MainForm.cs
  - Removed unused AcadApp alias from MainForm.cs
  - Removed Autodesk.AutoCAD.Geometry from myPlugin.cs
  - Removed Autodesk.AutoCAD.DatabaseServices from myCommands.cs
  - Removed Autodesk.AutoCAD.Geometry from myCommands.cs
- **Dead Code**: Removed unused code and variables
  - Removed local ToolTip instantiation in btnLoad_Click

### Fixed
- **ToolTip Memory Leak**: Created single ToolTip instance as form field
  - Prevents memory growth on repeated Load button clicks
- **AutoCAD DLL References**: Added proper HintPath entries
  - Points to AutoCAD 2024 installation directory
  - Prevents build errors from missing references
- **Error Handling**: Improved exception handling throughout
  - Better logging of errors
  - Graceful degradation on failures
  - Continues processing remaining files after individual failures

### Performance
- **Logging I/O Operations**: Reduced from 5-10 writes per drawing to 1 batched write
  - 80-90% reduction in file I/O operations
- **Processing Speed**: Reduced delays from 1100ms to 650ms per drawing
  - 41% faster batch processing
- **Memory Usage**: Fixed leaks, stable memory consumption for large batches
  - Successfully tested with 100+ drawings

### Documentation
- **Updated README.md**: Comprehensive documentation with all V2 features
  - Installation and setup instructions
  - Usage examples and workflows
  - Configuration options
  - Performance benchmarks
  - Troubleshooting guide
- **Created CHANGELOG.md**: Version history tracking
- **Improved Code Comments**: Better inline documentation

## [1.0.0] - 2024-12-01

### Added
- Initial release of AutoCAD LISP Tool
- Basic batch processing functionality
- LISP file loading and command execution
- Multiple drawing selection
- Auto-save with QSAVE command
- Optional drawing close after processing
- Session-based logging to Logs folder
- Progress tracking with progress bar
- Per-drawing result display in ListView
- Support for AutoCAD 2024 .NET API

### Features
- Load .lsp files and execute LISP functions
- Execute raw AutoCAD commands
- Batch select multiple DWG files
- Append additional drawings to current batch
- Color-coded result display (green=success, red=error)
- Timestamped log files
- Read-only attribute handling
- Document locking for safe processing

---

## Version Numbering

- **Major version (X.0.0)**: Breaking changes or major feature additions
- **Minor version (0.X.0)**: New features, backwards compatible
- **Patch version (0.0.X)**: Bug fixes and minor improvements

## Links
- [Repository](https://github.com/agn-bandara/AutoCADLispTool)
- [Issues](https://github.com/agn-bandara/AutoCADLispTool/issues)
- [Improvements Plan V2](AutoCADLispTool/improvements_V2.md)
