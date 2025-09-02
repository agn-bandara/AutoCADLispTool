using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace AutoCADLispTool
{
    public partial class MainFom : Form
    {
        // Store the full path of the selected LISP file for future use
        private string selectedLispFilePath = string.Empty;
        
        // Store the full paths of selected drawing files for command execution
        private List<string> selectedDrawingPaths = new List<string>();

        // Store drawing results for display
        private List<DrawingResult> drawingResults = new List<DrawingResult>();

        // Log file path for current session
        private string currentLogFilePath = string.Empty;

        // Counter for incremental logging
        private int executionCounter = 0;

        // Add a cancellation token source for stopping processing
        private System.Threading.CancellationTokenSource cancellationTokenSource;

        public MainFom()
        {
            InitializeComponent();
            SetupListView();
        }

        // Class to store drawing processing results
        private class DrawingResult
        {
            public string DrawingName { get; set; }
            public string FirstWord { get; set; }
            public string RestOfMessage { get; set; }
            public bool IsProcessed { get; set; }
            public bool HasError { get; set; }
        }

        // Setup ListView with 3 columns
        private void SetupListView()
        {
            lstDwgList.View = View.Details;
            lstDwgList.FullRowSelect = true;
            lstDwgList.GridLines = true;
            lstDwgList.HeaderStyle = ColumnHeaderStyle.Nonclickable;

            // Add columns
            lstDwgList.Columns.Add("Drawing Name", 120);
            lstDwgList.Columns.Add("Result", 80);
            lstDwgList.Columns.Add("Values", 120);
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            // Clear previous LISP file and command when loading new file
            txtLspFile.Text = string.Empty;
            txtCommand.Text = string.Empty;
            selectedLispFilePath = string.Empty;

            // Create and configure OpenFileDialog for .lsp files
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "LISP Files (*.lsp)|*.lsp|All Files (*.*)|*.*";
                openFileDialog.FilterIndex = 1;
                openFileDialog.Title = "Select LISP File";
                openFileDialog.CheckFileExists = true;
                openFileDialog.CheckPathExists = true;
                openFileDialog.Multiselect = false;

                // Show the dialog and check if user selected a file
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // Store the full path for future use
                        selectedLispFilePath = openFileDialog.FileName;
                        
                        // Display only the filename with extension in the textbox
                        txtLspFile.Text = Path.GetFileName(selectedLispFilePath);
                        
                        // Optional: Set tooltip to show full path
                        ToolTip toolTip = new ToolTip();
                        toolTip.SetToolTip(txtLspFile, selectedLispFilePath);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error loading file: {ex.Message}", "Error", 
                                      MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        // Public property to access the selected file path from other parts of the application
        public string SelectedLispFilePath
        {
            get { return selectedLispFilePath; }
        }

        private void btnDwgs_Click(object sender, EventArgs e)
        {
            // Clear previous drawing list and related variables when loading new drawings
            lstDwgList.Items.Clear();
            selectedDrawingPaths.Clear();
            drawingResults.Clear();

            // Create and configure OpenFileDialog for .dwg files with multiselect enabled
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "AutoCAD Drawing Files (*.dwg)|*.dwg|All Files (*.*)|*.*";
                openFileDialog.FilterIndex = 1;
                openFileDialog.Title = "Select Drawing Files";
                openFileDialog.CheckFileExists = true;
                openFileDialog.CheckPathExists = true;
                openFileDialog.Multiselect = true; // Enable multiple file selection

                // Show the dialog and check if user selected files
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // Add each selected file to the list
                        foreach (string fileName in openFileDialog.FileNames)
                        {
                            // Store the full path for command execution
                            selectedDrawingPaths.Add(fileName);
                            
                            // Create a new drawing result entry
                            var result = new DrawingResult
                            {
                                DrawingName = Path.GetFileName(fileName),
                                FirstWord = "",
                                RestOfMessage = "",
                                IsProcessed = false,
                                HasError = false
                            };
                            drawingResults.Add(result);
                            
                            // Add to ListView
                            var item = new ListViewItem(result.DrawingName);
                            item.SubItems.Add(result.FirstWord);
                            item.SubItems.Add(result.RestOfMessage);
                            lstDwgList.Items.Add(item);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error loading drawing files: {ex.Message}", "Error", 
                                      MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        // Public property to access the selected drawing file paths from other parts of the application
        public List<string> SelectedDrawingPaths
        {
            get { return new List<string>(selectedDrawingPaths); } // Return a copy to prevent external modification
        }

        // Method to clear the drawing list
        public void ClearDrawingList()
        {
            lstDwgList.Items.Clear();
            selectedDrawingPaths.Clear();
            drawingResults.Clear();
        }

        // Method to remove selected drawing from the list
        public void RemoveSelectedDrawing()
        {
            if (lstDwgList.SelectedIndices.Count > 0)
            {
                int selectedIndex = lstDwgList.SelectedIndices[0];
                lstDwgList.Items.RemoveAt(selectedIndex);
                selectedDrawingPaths.RemoveAt(selectedIndex);
                drawingResults.RemoveAt(selectedIndex);
            }
        }

        private async void btnProcess_Click(object sender, EventArgs e)
        {
            if (selectedDrawingPaths.Count == 0)
            {
                MessageBox.Show("Please select drawing files first.", "No Drawings Selected", 
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Check if LISP file is selected when command is provided
            if (!string.IsNullOrWhiteSpace(txtCommand.Text) && string.IsNullOrWhiteSpace(selectedLispFilePath))
            {
                MessageBox.Show("Please select a LISP file first when using commands.", "LISP File Required", 
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Initialize log file for this processing session
            currentLogFilePath = CreateLogFilePath();
            executionCounter = 0; // Reset counter for new session
            
            WriteToLog("=== LISP Tool Processing Session Started ===");
            WriteToLog($"LISP File: {(string.IsNullOrEmpty(selectedLispFilePath) ? "None" : Path.GetFileName(selectedLispFilePath))}");
            WriteToLog($"Command: {(string.IsNullOrEmpty(txtCommand.Text) ? "None" : txtCommand.Text)}");
            WriteToLog($"Total Drawings: {selectedDrawingPaths.Count}");
            WriteToLog("================================================");
            
            // Initialize progress controls
            SetControlsEnabled(false); // Disable controls during processing
            prgDrawingProgress.Minimum = 0;
            prgDrawingProgress.Maximum = selectedDrawingPaths.Count;
            prgDrawingProgress.Value = 0;
            lblProgress.Text = "0%";
            
            // Create a copy of the paths to prevent modification during processing
            var pathsToProcess = new List<string>(selectedDrawingPaths);
            
            try
            {
                // Process each drawing file
                for (int i = 0; i < pathsToProcess.Count; i++)
                {
                    string dwgPath = pathsToProcess[i];
                    string commandOutput = "Drawing processed successfully";
                    
                    // Update progress display using thread-safe method
                    await UpdateProgressAsync(i + 1, pathsToProcess.Count, Path.GetFileName(dwgPath));
                    
                    try
                    {
                        // Open the drawing using AutoCAD DocumentManager
                        var docMgr = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager;
                        Document openDoc = null;
                        
                        try
                        {
                            // Try to open the document with better error handling
                            WriteToLog($"Attempting to open: {Path.GetFileName(dwgPath)}");
                            openDoc = docMgr.Open(dwgPath, false);
                            
                            if (openDoc != null)
                            {
                                WriteToLog($"Successfully opened: {Path.GetFileName(dwgPath)}");
                                
                                // Make the document current
                                docMgr.MdiActiveDocument = openDoc;
                                
                                // Allow time for document to become active
                                await Task.Delay(100);
                                
                                if (docMgr.MdiActiveDocument != openDoc)
                                {
                                    commandOutput = "Failed to make document current";
                                    WriteToLog($"Failed to make document current: {Path.GetFileName(dwgPath)}");
                                    continue;
                                }

                                commandOutput = "Document opened successfully";

                                // Execute commands using document lock
                                using (DocumentLock docLock = openDoc.LockDocument())
                                {
                                    WriteToLog($"Document locked for processing: {Path.GetFileName(dwgPath)}");
                                    
                                    // Alternative approach: Use Editor for better command execution
                                    Editor ed = openDoc.Editor;
                                    // Load LISP file first if both LISP file and command are specified
                                    if (!string.IsNullOrEmpty(selectedLispFilePath) && !string.IsNullOrEmpty(txtCommand.Text))
                                    {
                                        try
                                        {
                                            // Load the LISP file
                                            string loadCommand = $"(load \"{selectedLispFilePath.Replace("\\", "\\\\")}\")";
                                            openDoc.SendStringToExecute(loadCommand + "\n", true, false, false);
                                          
                                            // Wait for LISP file to load
                                            await Task.Delay(250);

                                            // Execute the LISP command directly
                                            string lispFunction = txtCommand.Text;
                                            string resultVarName = "resultVar";
                                            string commandToExecute = $"(setq {resultVarName} {lispFunction})";
                                            openDoc.SendStringToExecute(commandToExecute + "\n", true, false, true);
                                            
                                            // Wait for command to complete
                                            await Task.Delay(250);

                                            object result = openDoc.GetLispSymbol(resultVarName);

                                            if (result != null)
                                            {
                                                commandOutput = result.ToString();
                                            }
                                            else
                                            {
                                                commandOutput = $"LISP no result";
                                            }

                                            ed.WriteMessage($"\n{commandOutput}");
                                            WriteToLog($"LISP command executed successfully for: {Path.GetFileName(dwgPath)}");
                                        }
                                        catch (Exception cmdEx)
                                        {
                                            commandOutput = $"Command execution error: {cmdEx.Message}";
                                            WriteToLog($"Command execution error for {Path.GetFileName(dwgPath)}: {cmdEx.Message}");
                                        }
                                    }
                                    else if (!string.IsNullOrEmpty(txtCommand.Text))
                                    {
                                        try
                                        {
                                            // Execute command only (no LISP file)
                                            openDoc.SendStringToExecute(txtCommand.Text + "\n", true, false, false);
                                            
                                            // Wait for command to complete
                                            await Task.Delay(250);

                                            commandOutput = $"LISP executed";
                                            WriteToLog($"Command executed successfully for: {Path.GetFileName(dwgPath)}");
                                        }
                                        catch (Exception cmdEx)
                                        {
                                            commandOutput = $"Command execution error: {cmdEx.Message}";
                                            WriteToLog($"Command execution error for {Path.GetFileName(dwgPath)}: {cmdEx.Message}");
                                        }
                                    }
                                    
                                    // Save the document within the lock using QSAVE command
                                    try
                                    {
                                        // Check if file is read-only and remove read-only attribute if needed
                                        FileInfo fileInfo = new FileInfo(dwgPath);
                                        if (fileInfo.IsReadOnly)
                                        {
                                            fileInfo.IsReadOnly = false;
                                            WriteToLog($"Removed read-only attribute from: {Path.GetFileName(dwgPath)}");
                                        }
                                        
                                        // Use QSAVE command to save the drawing (AutoCAD's native save)
                                        openDoc.SendStringToExecute("QSAVE\n", true, false, false);
                                        
                                        // Wait for save operation to complete
                                        await Task.Delay(500);
                                        WriteToLog($"Document saved successfully: {Path.GetFileName(dwgPath)}");
                                    }
                                    catch (Exception saveEx)
                                    {
                                        // Don't throw - log the error and continue with next file
                                        commandOutput = $"Save failed: {saveEx.Message}";
                                        WriteToLog($"QSAVE failed for {Path.GetFileName(dwgPath)}: {saveEx.Message}");
                                    }
                                } // DocumentLock disposed here
                                
                                WriteToLog($"Document lock released for: {Path.GetFileName(dwgPath)}");
                                
                                // Only close the document if chkClose is checked
                                if (chkClose.Checked)
                                {
                                    try
                                    {
                                        // Use CloseAndSave() to close and save the drawing
                                        openDoc.CloseAndSave(dwgPath);
                                        WriteToLog($"Document closed and saved: {Path.GetFileName(dwgPath)}");
                                    }
                                    catch (Exception closeEx)
                                    {
                                        // Log the close exception but continue
                                        commandOutput += $" (Close warning: {closeEx.Message})";
                                        WriteToLog($"Close warning for {Path.GetFileName(dwgPath)}: {closeEx.Message}");
                                    }
                                }
                                else
                                {
                                    WriteToLog($"Document left open: {Path.GetFileName(dwgPath)}");
                                }
                                
                                // Parse command output
                                string firstWord = "";
                                string restOfMessage = "";
                                
                                if (!string.IsNullOrEmpty(commandOutput))
                                {
                                    string[] words = commandOutput.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                    if (words.Length > 0)
                                    {
                                        firstWord = words[0];
                                        if (words.Length > 1)
                                        {
                                            restOfMessage = string.Join(" ", words.Skip(1));
                                        }
                                    }
                                }
                                
                                // Update the drawing result - ensure we don't go out of bounds
                                if (i < drawingResults.Count)
                                {
                                    drawingResults[i].FirstWord = firstWord;
                                    drawingResults[i].RestOfMessage = restOfMessage;
                                    drawingResults[i].IsProcessed = true;
                                }
                                
                                // Update ListView using thread-safe method
                                await UpdateListViewItemAsync(i, firstWord, restOfMessage, Color.LightGreen);
                                
                                // Log LISP execution details
                                if (i < drawingResults.Count)
                                {
                                    LogLispExecution(drawingResults[i].DrawingName, selectedLispFilePath, txtCommand.Text, commandOutput, false);
                                }
                            }
                            else
                            {
                                commandOutput = "Failed to open document";
                                WriteToLog($"Failed to open document: {Path.GetFileName(dwgPath)}");
                                
                                // Still update the drawing result for failed opens
                                if (i < drawingResults.Count)
                                {
                                    drawingResults[i].FirstWord = "ERROR";
                                    drawingResults[i].RestOfMessage = "Failed to open document";
                                    drawingResults[i].HasError = true;
                                    
                                    // Update ListView for failed opens
                                    await UpdateListViewItemAsync(i, "ERROR", "Failed to open document", Color.LightCoral);
                                    LogLispExecution(drawingResults[i].DrawingName, selectedLispFilePath, txtCommand.Text, "Failed to open document", true);
                                }
                            }
                        }
                        catch (Exception docEx)
                        {
                            WriteToLog($"Document operation error for {Path.GetFileName(dwgPath)}: {docEx.Message}");
                            
                            // Ensure document is closed only if chkClose is checked, even if there's an error
                            if (openDoc != null && chkClose.Checked)
                            {
                                try
                                {
                                    // Use CloseAndSave() to preserve any saved changes
                                    openDoc.CloseAndSave(dwgPath);
                                    WriteToLog($"Document closed after error: {Path.GetFileName(dwgPath)}");
                                }
                                catch
                                {
                                    // If CloseAndSave fails, try CloseAndDiscard as last resort
                                    try
                                    {
                                        openDoc.CloseAndDiscard();
                                        WriteToLog($"Document discarded after error: {Path.GetFileName(dwgPath)}");
                                    }
                                    catch
                                    {
                                        // Ignore close errors during error handling
                                        WriteToLog($"Failed to close document after error: {Path.GetFileName(dwgPath)}");
                                    }
                                }
                            }
                            
                            // Don't rethrow - continue with processing
                            commandOutput = $"Document error: {docEx.Message}";
                        }
                    }
                    catch (Exception ex)
                    {
                        // Mark as failed
                        WriteToLog($"General processing error for {Path.GetFileName(dwgPath)}: {ex.Message}");
                        
                        drawingResults[i].HasError = true;
                        drawingResults[i].FirstWord = "ERROR";
                        drawingResults[i].RestOfMessage = ex.Message;
                        
                        // Update ListView using thread-safe method
                        await UpdateListViewItemAsync(i, "ERROR", ex.Message, Color.LightCoral);
                        
                        // Log error details
                        LogLispExecution(drawingResults[i].DrawingName, selectedLispFilePath, txtCommand.Text, ex.Message, true);
                        
                        // Continue with next file even if this one failed
                        continue;
                    }
                }
            }
            catch (Exception generalEx)
            {
                WriteToLog($"Critical error during processing: {generalEx.Message}");
                MessageBox.Show($"A critical error occurred during processing: {generalEx.Message}", "Critical Error", 
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Reset progress and re-enable controls after processing
                SetControlsEnabled(true);
                lblProgress.Text = "Complete";
                
                // Log session completion
                WriteToLog("=== LISP Tool Processing Session Completed ===");
                WriteToLog($"Total drawings processed: {pathsToProcess.Count}");
                WriteToLog($"Log file location: {currentLogFilePath}");
            }
        }

        // Thread-safe method to update progress
        private async Task UpdateProgressAsync(int currentIndex, int totalCount, string currentFileName)
        {
            if (InvokeRequired)
            {
                await Task.Run(() => 
                {
                    Invoke(new Action(() => UpdateProgressInternal(currentIndex, totalCount, currentFileName)));
                });
            }
            else
            {
                UpdateProgressInternal(currentIndex, totalCount, currentFileName);
            }
        }

        private void UpdateProgressInternal(int currentIndex, int totalCount, string currentFileName)
        {
            prgDrawingProgress.Value = currentIndex;
            int percentage = (int)((double)currentIndex / totalCount * 100);
            lblProgress.Text = $"{percentage}%";
            
            // Optional: Update the form title to show current processing file
            this.Text = $"Run Lisp - Processing: {currentFileName} ({currentIndex}/{totalCount})";
            
            // Removed Application.DoEvents() - this was causing the problem where moving the form stopped processing
        }

        // Thread-safe method to update ListView items
        private async Task UpdateListViewItemAsync(int index, string firstWord, string restOfMessage, Color backColor)
        {
            if (InvokeRequired)
            {
                await Task.Run(() => 
                {
                    Invoke(new Action(() => UpdateListViewItemInternal(index, firstWord, restOfMessage, backColor)));
                });
            }
            else
            {
                UpdateListViewItemInternal(index, firstWord, restOfMessage, backColor);
            }
        }

        private void UpdateListViewItemInternal(int index, string firstWord, string restOfMessage, Color backColor)
        {
            if (index < lstDwgList.Items.Count)
            {
                lstDwgList.Items[index].SubItems[1].Text = firstWord;
                lstDwgList.Items[index].SubItems[2].Text = restOfMessage;
                lstDwgList.Items[index].BackColor = backColor;
            }
        }

        // Method to create log file path with date and time
        private string CreateLogFilePath()
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string logFileName = $"LispTool_Log_{timestamp}.txt";
            
            // Create logs directory if it doesn't exist
            string logsDir = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Logs");
            if (!Directory.Exists(logsDir))
            {
                Directory.CreateDirectory(logsDir);
            }
            
            return Path.Combine(logsDir, logFileName);
        }

        // Method to write to log file
        private void WriteToLog(string message)
        {
            try
            {
                if (string.IsNullOrEmpty(currentLogFilePath))
                    return;

                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
                File.AppendAllText(currentLogFilePath, logEntry + Environment.NewLine);
            }
            catch (Exception ex)
            {
                // Don't show message box for logging errors to avoid interrupting workflow
                System.Diagnostics.Debug.WriteLine($"Logging error: {ex.Message}");
            }
        }

        // Method to log LISP execution details with incremental numbering
        private void LogLispExecution(string drawingName, string lispFile, string command, string output, bool hasError)
        {
            executionCounter++;
            string status = hasError ? "ERROR" : "SUCCESS";
            string logEntry = $"[{executionCounter:D4}] {drawingName}\t | {status} |\t {output}";
            
            // Write directly without timestamp since we use incremental counter
            try
            {
                if (!string.IsNullOrEmpty(currentLogFilePath))
                {
                    File.AppendAllText(currentLogFilePath, logEntry + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Logging error: {ex.Message}");
            }
        }

        private void btnAppend_Click(object sender, EventArgs e)
        {
            // Create and configure OpenFileDialog for .dwg files with multiselect enabled
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "AutoCAD Drawing Files (*.dwg)|*.dwg|All Files (*.*)|*.*";
                openFileDialog.FilterIndex = 1;
                openFileDialog.Title = "Append Drawing Files";
                openFileDialog.CheckFileExists = true;
                openFileDialog.CheckPathExists = true;
                openFileDialog.Multiselect = true; // Enable multiple file selection

                // Show the dialog and check if user selected files
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        int addedCount = 0;
                        
                        // Add each selected file to the existing list
                        foreach (string fileName in openFileDialog.FileNames)
                        {
                            // Check if the file is already in the list to avoid duplicates
                            if (!selectedDrawingPaths.Contains(fileName))
                            {
                                // Store the full path for command execution
                                selectedDrawingPaths.Add(fileName);
                                
                                // Create a new drawing result entry
                                var result = new DrawingResult
                                {
                                    DrawingName = Path.GetFileName(fileName),
                                    FirstWord = "",
                                    RestOfMessage = "",
                                    IsProcessed = false,
                                    HasError = false
                                };
                                drawingResults.Add(result);
                                
                                // Add to ListView
                                var item = new ListViewItem(result.DrawingName);
                                item.SubItems.Add(result.FirstWord);
                                item.SubItems.Add(result.RestOfMessage);
                                lstDwgList.Items.Add(item);
                                
                                addedCount++;
                            }
                        } 
                        
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error appending drawing files: {ex.Message}", "Error", 
                                      MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        // Method to enable/disable controls during processing
        private void SetControlsEnabled(bool enabled)
        {
            btnClear.Enabled = enabled;
            btnDwgs.Enabled = enabled;
            btnAppend.Enabled = enabled;
            btnProcess.Enabled = enabled;
            chkClose.Enabled = enabled;
            txtLspFile.Enabled = enabled;
            txtCommand.Enabled = enabled;
            btnLoad.Enabled = enabled;
            
            // Initialize or reset progress controls
            if (enabled)
            {
                // Reset progress when re-enabling controls
                prgDrawingProgress.Value = 0;
                lblProgress.Text = "Ready";
                this.Text = "Run Lisp";
                
                // Cancel any ongoing processing
                if (cancellationTokenSource != null)
                {
                    cancellationTokenSource.Cancel();
                    cancellationTokenSource = null;
                }
            }
            else
            {
                // Create new cancellation token for processing
                cancellationTokenSource = new System.Threading.CancellationTokenSource();
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            // Clear the drawing list and all related variables
            lstDwgList.Items.Clear();
            selectedDrawingPaths.Clear();
            drawingResults.Clear();
            
            // Show confirmation message
            MessageBox.Show("Drawing list cleared successfully.", "List Cleared", 
                          MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
