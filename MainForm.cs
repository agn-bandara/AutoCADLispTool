using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using AutoCADLispTool.Models;
using AutoCADLispTool.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace AutoCADLispTool
{
    public partial class MainForm : Form
    {
        private readonly List<string> _drawingPaths = new List<string>();
        private readonly List<DrawingResult> _results = new List<DrawingResult>();
        private readonly ToolTip _toolTip = new ToolTip();
        
        private CancellationTokenSource _cancellationTokenSource;
        private BufferedLogger _logger;
        private ProcessingConfig _config;
        private string _selectedLispPath = string.Empty;
        private int _executionCounter = 0;

        public MainForm()
        {
            InitializeComponent();
            SetupListView();
            _config = new ProcessingConfig();
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
            txtLspFile.Text = string.Empty;
            txtCommand.Text = string.Empty;
            _selectedLispPath = string.Empty;

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "LISP Files (*.lsp)|*.lsp|All Files (*.*)|*.*";
                openFileDialog.FilterIndex = 1;
                openFileDialog.Title = "Select LISP File";
                openFileDialog.CheckFileExists = true;
                openFileDialog.CheckPathExists = true;
                openFileDialog.Multiselect = false;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        _selectedLispPath = openFileDialog.FileName;
                        txtLspFile.Text = Path.GetFileName(_selectedLispPath);
                        _toolTip.SetToolTip(txtLspFile, _selectedLispPath);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error loading file: {ex.Message}", "Error", 
                                      MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        public string SelectedLispFilePath
        {
            get { return _selectedLispPath; }
        }

        private void btnDwgs_Click(object sender, EventArgs e)
        {
            lstDwgList.Items.Clear();
            _drawingPaths.Clear();
            _results.Clear();

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "AutoCAD Drawing Files (*.dwg)|*.dwg|All Files (*.*)|*.*";
                openFileDialog.FilterIndex = 1;
                openFileDialog.Title = "Select Drawing Files";
                openFileDialog.CheckFileExists = true;
                openFileDialog.CheckPathExists = true;
                openFileDialog.Multiselect = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        lstDwgList.BeginUpdate();
                        foreach (string fileName in openFileDialog.FileNames)
                        {
                            _drawingPaths.Add(fileName);
                            
                            var result = new DrawingResult
                            {
                                DrawingName = Path.GetFileName(fileName),
                                DrawingPath = fileName,
                                ResultStatus = "",
                                ResultMessage = "",
                                IsProcessed = false,
                                HasError = false
                            };
                            _results.Add(result);
                            
                            var item = new ListViewItem(result.DrawingName);
                            item.SubItems.Add(result.ResultStatus);
                            item.SubItems.Add(result.ResultMessage);
                            lstDwgList.Items.Add(item);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error loading drawing files: {ex.Message}", "Error", 
                                      MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        lstDwgList.EndUpdate();
                    }
                }
            }
        }

        public List<string> SelectedDrawingPaths
        {
            get { return new List<string>(_drawingPaths); }
        }

        public void ClearDrawingList()
        {
            lstDwgList.Items.Clear();
            _drawingPaths.Clear();
            _results.Clear();
        }

        public void RemoveSelectedDrawing()
        {
            if (lstDwgList.SelectedIndices.Count > 0)
            {
                int selectedIndex = lstDwgList.SelectedIndices[0];
                lstDwgList.Items.RemoveAt(selectedIndex);
                _drawingPaths.RemoveAt(selectedIndex);
                _results.RemoveAt(selectedIndex);
            }
        }

        private async void btnProcess_Click(object sender, EventArgs e)
        {
            if (_drawingPaths.Count == 0)
            {
                MessageBox.Show("Please select drawing files first.", "No Drawings Selected", 
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!string.IsNullOrWhiteSpace(txtCommand.Text) && string.IsNullOrWhiteSpace(_selectedLispPath))
            {
                MessageBox.Show("Please select a LISP file first when using commands.", "LISP File Required", 
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string logFilePath = CreateLogFilePath();
            _executionCounter = 0;
            
            _logger?.Dispose();
            _logger = new BufferedLogger(logFilePath);
            
            _logger.Log("=== LISP Tool Processing Session Started ===");
            _logger.Log($"LISP File: {(string.IsNullOrEmpty(_selectedLispPath) ? "None" : Path.GetFileName(_selectedLispPath))}");
            _logger.Log($"Command: {(string.IsNullOrEmpty(txtCommand.Text) ? "None" : txtCommand.Text)}");
            _logger.Log($"Total Drawings: {_drawingPaths.Count}");
            _logger.Log("================================================");
            
            SetControlsEnabled(false);
            prgDrawingProgress.Minimum = 0;
            prgDrawingProgress.Maximum = _drawingPaths.Count;
            prgDrawingProgress.Value = 0;
            lblProgress.Text = "0%";
            
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;
            var pathsToProcess = new List<string>(_drawingPaths);
            
            try
            {
                for (int i = 0; i < pathsToProcess.Count; i++)
                {
                    token.ThrowIfCancellationRequested();
                    
                    string dwgPath = pathsToProcess[i];
                    string commandOutput = "Drawing processed successfully";
                    
                    await UpdateProgressAsync(i + 1, pathsToProcess.Count, Path.GetFileName(dwgPath));
                    
                    try
                    {
                        var docMgr = AcadApp.DocumentManager;
                        Document openDoc = null;
                        
                        try
                        {
                            _logger.Log($"Attempting to open: {Path.GetFileName(dwgPath)}");
                            openDoc = docMgr.Open(dwgPath, false);
                            
                            if (openDoc != null)
                            {
                                _logger.Log($"Successfully opened: {Path.GetFileName(dwgPath)}");
                                
                                docMgr.MdiActiveDocument = openDoc;
                                await Task.Delay(_config.DocumentActivationDelayMs, token);
                                
                                if (docMgr.MdiActiveDocument != openDoc)
                                {
                                    commandOutput = "Failed to make document current";
                                    _logger.Log($"Failed to make document current: {Path.GetFileName(dwgPath)}");
                                    continue;
                                }

                                commandOutput = "Document opened successfully";

                                using (DocumentLock docLock = openDoc.LockDocument())
                                {
                                    _logger.Log($"Document locked for processing: {Path.GetFileName(dwgPath)}");
                                    
                                    Editor ed = openDoc.Editor;
                                    if (!string.IsNullOrEmpty(_selectedLispPath) && !string.IsNullOrEmpty(txtCommand.Text))
                                    {
                                        try
                                        {
                                            string loadCommand = $"(load \"{_selectedLispPath.Replace("\\", "\\\\")}\")";
                                            openDoc.SendStringToExecute(loadCommand + "\n", true, false, false);
                                          
                                            await Task.Delay(_config.LispLoadDelayMs, token);

                                            string lispFunction = txtCommand.Text;
                                            string resultVarName = "resultVar";
                                            string commandToExecute = $"(setq {resultVarName} {lispFunction})";
                                            openDoc.SendStringToExecute(commandToExecute + "\n", true, false, true);
                                            
                                            await Task.Delay(_config.CommandExecutionDelayMs, token);

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
                                            _logger.Log($"LISP command executed successfully for: {Path.GetFileName(dwgPath)}");
                                        }
                                        catch (Exception cmdEx)
                                        {
                                            commandOutput = $"Command execution error: {cmdEx.Message}";
                                            _logger.Log($"Command execution error for {Path.GetFileName(dwgPath)}: {cmdEx.Message}");
                                        }
                                    }
                                    else if (!string.IsNullOrEmpty(txtCommand.Text))
                                    {
                                        try
                                        {
                                            openDoc.SendStringToExecute(txtCommand.Text + "\n", true, false, false);
                                            await Task.Delay(_config.CommandExecutionDelayMs, token);

                                            commandOutput = $"LISP executed";
                                            _logger.Log($"Command executed successfully for: {Path.GetFileName(dwgPath)}");
                                        }
                                        catch (Exception cmdEx)
                                        {
                                            commandOutput = $"Command execution error: {cmdEx.Message}";
                                            _logger.Log($"Command execution error for {Path.GetFileName(dwgPath)}: {cmdEx.Message}");
                                        }
                                    }
                                    
                                    try
                                    {
                                        FileInfo fileInfo = new FileInfo(dwgPath);
                                        if (fileInfo.IsReadOnly)
                                        {
                                            fileInfo.IsReadOnly = false;
                                            _logger.Log($"Removed read-only attribute from: {Path.GetFileName(dwgPath)}");
                                        }
                                        
                                        openDoc.SendStringToExecute("QSAVE\n", true, false, false);
                                        await Task.Delay(_config.SaveDelayMs, token);
                                        _logger.Log($"Document saved successfully: {Path.GetFileName(dwgPath)}");
                                    }
                                    catch (Exception saveEx)
                                    {
                                        commandOutput = $"Save failed: {saveEx.Message}";
                                        _logger.Log($"QSAVE failed for {Path.GetFileName(dwgPath)}: {saveEx.Message}");
                                    }
                                }
                                
                                _logger.Log($"Document lock released for: {Path.GetFileName(dwgPath)}");
                                
                                if (chkClose.Checked)
                                {
                                    try
                                    {
                                        openDoc.CloseAndDiscard();
                                        _logger.Log($"Document closed: {Path.GetFileName(dwgPath)}");
                                    }
                                    catch (Exception closeEx)
                                    {
                                        commandOutput += $" (Close warning: {closeEx.Message})";
                                        _logger.Log($"Close warning for {Path.GetFileName(dwgPath)}: {closeEx.Message}");
                                    }
                                }
                                else
                                {
                                    _logger.Log($"Document left open: {Path.GetFileName(dwgPath)}");
                                }
                                
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
                                
                                if (i < _results.Count)
                                {
                                    _results[i].ResultStatus = firstWord;
                                    _results[i].ResultMessage = restOfMessage;
                                    _results[i].IsProcessed = true;
                                }
                                
                                await UpdateListViewItemAsync(i, firstWord, restOfMessage, Color.LightGreen);
                                
                                if (i < _results.Count)
                                {
                                    LogLispExecution(_results[i].DrawingName, _selectedLispPath, txtCommand.Text, commandOutput, false);
                                }
                            }
                            else
                            {
                                commandOutput = "Failed to open document";
                                _logger.Log($"Failed to open document: {Path.GetFileName(dwgPath)}");
                                
                                if (i < _results.Count)
                                {
                                    _results[i].ResultStatus = "ERROR";
                                    _results[i].ResultMessage = "Failed to open document";
                                    _results[i].HasError = true;
                                    
                                    await UpdateListViewItemAsync(i, "ERROR", "Failed to open document", Color.LightCoral);
                                    LogLispExecution(_results[i].DrawingName, _selectedLispPath, txtCommand.Text, "Failed to open document", true);
                                }
                            }
                        }
                        catch (Exception docEx)
                        {
                            _logger.Log($"Document operation error for {Path.GetFileName(dwgPath)}: {docEx.Message}");
                            
                            if (openDoc != null && chkClose.Checked)
                            {
                                try
                                {
                                    openDoc.CloseAndDiscard();
                                    _logger.Log($"Document closed after error: {Path.GetFileName(dwgPath)}");
                                }
                                catch
                                {
                                    _logger.Log($"Failed to close document after error: {Path.GetFileName(dwgPath)}");
                                }
                            }
                            
                            commandOutput = $"Document error: {docEx.Message}";
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Log($"General processing error for {Path.GetFileName(dwgPath)}: {ex.Message}");
                        
                        _results[i].HasError = true;
                        _results[i].ResultStatus = "ERROR";
                        _results[i].ResultMessage = ex.Message;
                        
                        await UpdateListViewItemAsync(i, "ERROR", ex.Message, Color.LightCoral);
                        LogLispExecution(_results[i].DrawingName, _selectedLispPath, txtCommand.Text, ex.Message, true);
                        
                        continue;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.Log("Processing cancelled by user");
                MessageBox.Show("Processing was cancelled.", "Cancelled", 
                              MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception generalEx)
            {
                _logger.Log($"Critical error during processing: {generalEx.Message}");
                MessageBox.Show($"A critical error occurred during processing: {generalEx.Message}", "Critical Error", 
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetControlsEnabled(true);
                lblProgress.Text = "Complete";
                
                _logger.Log("=== LISP Tool Processing Session Completed ===");
                _logger.Log($"Total drawings processed: {pathsToProcess.Count}");
                _logger.Log($"Log file location: {logFilePath}");
                _logger?.Flush();
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
            this.Text = $"Run Lisp - Processing: {currentFileName} ({currentIndex}/{totalCount})";
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

        private string CreateLogFilePath()
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string logFileName = $"LispTool_Log_{timestamp}.txt";
            string logsDir = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Logs");
            if (!Directory.Exists(logsDir))
            {
                Directory.CreateDirectory(logsDir);
            }
            return Path.Combine(logsDir, logFileName);
        }

        private void LogLispExecution(string drawingName, string lispFile, string command, string output, bool hasError)
        {
            _executionCounter++;
            string status = hasError ? "ERROR" : "SUCCESS";
            string logEntry = $"[{_executionCounter:D4}] {drawingName}\t | {status} |\t {output}";
            
            if (_logger != null)
            {
                _logger.Log(logEntry.Substring(logEntry.IndexOf(']') + 2));
            }
        }

        private void btnAppend_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "AutoCAD Drawing Files (*.dwg)|*.dwg|All Files (*.*)|*.*";
                openFileDialog.FilterIndex = 1;
                openFileDialog.Title = "Append Drawing Files";
                openFileDialog.CheckFileExists = true;
                openFileDialog.CheckPathExists = true;
                openFileDialog.Multiselect = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        lstDwgList.BeginUpdate();
                        foreach (string fileName in openFileDialog.FileNames)
                        {
                            if (!_drawingPaths.Contains(fileName))
                            {
                                _drawingPaths.Add(fileName);
                                
                                var result = new DrawingResult
                                {
                                    DrawingName = Path.GetFileName(fileName),
                                    DrawingPath = fileName,
                                    ResultStatus = "",
                                    ResultMessage = "",
                                    IsProcessed = false,
                                    HasError = false
                                };
                                _results.Add(result);
                                
                                var item = new ListViewItem(result.DrawingName);
                                item.SubItems.Add(result.ResultStatus);
                                item.SubItems.Add(result.ResultMessage);
                                lstDwgList.Items.Add(item);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error appending drawing files: {ex.Message}", "Error", 
                                      MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        lstDwgList.EndUpdate();
                    }
                }
            }
        }

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
            
            if (enabled)
            {
                prgDrawingProgress.Value = 0;
                lblProgress.Text = "Ready";
                this.Text = "Run Lisp";
                
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            lstDwgList.Items.Clear();
            _drawingPaths.Clear();
            _results.Clear();
            
            MessageBox.Show("Drawing list cleared successfully.", "List Cleared", 
                          MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
