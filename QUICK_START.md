# Quick Start Guide - AutoCAD LISP Tool V2.0

## ?? Get Started in 5 Minutes

### Step 1: Load the Plugin (1 minute)

1. Open **AutoCAD 2024**
2. Type `NETLOAD` at the command line
3. Browse to: `AutoCADLispTool\bin\Release\AutoCADLispTool.dll`
4. Click **Open**

### Step 2: Launch the Tool (30 seconds)

1. Type `LISPTOOL` at the command line
2. The AutoCAD LISP Tool window will appear

### Step 3: Basic Usage (3 minutes)

#### Simple Command Execution (No LISP file needed)
```
1. Click "Dwgs" ? Select 1 or more .dwg files
2. Enter command in "Command" box: (command "ZOOM" "E")
3. Click "Process"
4. Watch progress bar and results
```

#### LISP File Execution
```
1. Click "Load .lsp" ? Select your .lsp file
2. Click "Dwgs" ? Select drawing files
3. Enter LISP function: (MyFunction)
4. Click "Process"
```

### Step 4: Check Results (30 seconds)

**In the UI:**
- ?? Green rows = Success
- ?? Red rows = Error
- Results show in "Result" and "Values" columns

**In the Logs:**
```
Location: AutoCADLispTool\bin\Release\Logs\
Filename: LispTool_Log_YYYYMMDD_HHMMSS.txt
```

---

## ?? Common Scenarios

### Scenario 1: Run a LISP Function on 50 Drawings

```
1. Load .lsp file:        MyTools.lsp
2. Enter command:         (ProcessDrawing)
3. Select 50 drawings:    Click "Dwgs" button
4. Uncheck:               "Close drawings after processing" (to review)
5. Click:                 "Process"
6. Wait:                  ~32 seconds (650ms × 50)
7. Review:                Check green/red status
8. View logs:             Logs folder for details
```

**Expected Performance:**
- Processing time: ~32.5 seconds for 50 drawings
- Log file size: ~5-10 KB
- Memory usage: Stable

### Scenario 2: Add More Drawings Mid-Session

```
1. Already have 20 drawings loaded
2. Click "Append" (not "Dwgs")
3. Select 10 more drawings
4. Total: 30 drawings
5. Click "Process"
```

### Scenario 3: Error Troubleshooting

**If drawings show RED (error):**
```
1. Check log file in Logs folder
2. Look for [ERROR] entries
3. Common issues:
   - Drawing is locked by another user
   - LISP function not found
   - Syntax error in command
4. Fix issue and re-process just the failed drawings
```

---

## ?? Configuration Tips

### Optimize for Your System

Edit `Services\ProcessingConfig.cs`:

**For Fast Computers:**
```csharp
public int DocumentActivationDelayMs { get; set; } = 25;   // Halve it
public int LispLoadDelayMs { get; set; } = 100;            // Reduce
public int CommandExecutionDelayMs { get; set; } = 100;    // Reduce
public int SaveDelayMs { get; set; } = 200;                // Reduce
// Total: ~425ms per drawing (faster!)
```

**For Slow Computers or Network Drives:**
```csharp
public int DocumentActivationDelayMs { get; set; } = 100;  // Double it
public int LispLoadDelayMs { get; set; } = 300;            // Increase
public int CommandExecutionDelayMs { get; set; } = 300;    // Increase
public int SaveDelayMs { get; set; } = 600;                // Increase
// Total: ~1300ms per drawing (more stable)
```

**After editing**, rebuild the project and reload in AutoCAD.

---

## ?? Best Practices

### ? DO:
- ? **Backup drawings first** - Always keep originals
- ? **Test on 1-2 files first** - Verify command works
- ? **Check logs** - Review for errors
- ? **Use NETLOAD** - Most reliable method
- ? **Close AutoCAD drawings** - Before processing in batch

### ? DON'T:
- ? **Process without backup** - Murphy's law applies
- ? **Run on 100s of files untested** - Test first!
- ? **Ignore red errors** - They indicate failures
- ? **Move/close window during processing** - Let it complete

---

## ?? Troubleshooting

### Problem: "Cannot open drawing"
**Solution:**
```
1. Check file path is correct
2. Verify file isn't open in AutoCAD
3. Check file permissions (read/write)
4. Ensure file isn't read-only (tool auto-fixes this)
```

### Problem: "LISP function not found"
**Solution:**
```
1. Verify .lsp file loaded successfully
2. Check function name spelling: (MyFunc) not MyFunc
3. Ensure function is defined in loaded .lsp file
4. Check AutoCAD command line for load errors
```

### Problem: "Processing is slow"
**Solution:**
```
1. Reduce delays in ProcessingConfig.cs
2. Check if drawings are on network drive (slower)
3. Verify AutoCAD isn't running other processes
4. Close unnecessary AutoCAD drawings
```

### Problem: "Memory usage high"
**Solution:**
```
This is fixed in V2.0! Update to latest version.
- BufferedLogger prevents I/O memory buildup
- ToolTip leak fixed
- Proper disposal implemented
```

---

## ?? Performance Reference

### Processing Time Calculator

```
Formula: (Number of Drawings) × (650ms) = Total Time

Examples:
- 10 drawings:  6.5 seconds
- 50 drawings:  32.5 seconds
- 100 drawings: 65 seconds (1 minute)
- 500 drawings: 325 seconds (5.4 minutes)
```

### Batch Size Recommendations

| Drawings | Time | Recommended |
|----------|------|-------------|
| 1-10 | <10 sec | ? Great for testing |
| 11-50 | <1 min | ? Ideal batch size |
| 51-100 | <2 min | ? Good for production |
| 101-500 | <10 min | ?? Monitor progress |
| 500+ | 10+ min | ?? Consider splitting |

---

## ?? Example Commands

### Zoom Extents
```
Command: (command "ZOOM" "E")
No LISP file needed
```

### Purge All
```
Command: (command "-PURGE" "A" "*" "N")
No LISP file needed
```

### Custom LISP Function
```
LISP File: MyTools.lsp
Command: (ProcessAllBlocks)
```

### Get Drawing Properties
```
LISP File: MyTools.lsp  
Command: (GetDrawingInfo)
Result: Returns info in "Values" column
```

---

## ?? Getting Help

1. **Check README.md** - Comprehensive documentation
2. **Check CHANGELOG.md** - Known issues and fixes
3. **Check Logs** - Detailed error messages
4. **GitHub Issues** - Report bugs or ask questions
5. **AutoCAD Forums** - Community support

---

## ?? You're Ready!

You now know how to:
- ? Load and launch the tool
- ? Process drawings in batch
- ? Handle errors
- ? Optimize performance
- ? Troubleshoot issues

**Happy batch processing! ??**

---

**Document Version**: 1.0  
**Last Updated**: January 2025  
**For Version**: AutoCAD LISP Tool V2.0
