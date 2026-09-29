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
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace AutoCADLispTool
{
    public partial class MainForm : Form
    {
        private const string ConfigFileName = "LispTool.config.xml";
        private const string DefaultLispFileName = "3DCheck_V02.lsp";

        [DllImport("user32.dll")]
        private static extern bool EnableWindow(IntPtr hWnd, bool bEnable);

        private readonly List<DrawingResult> _results = new List<DrawingResult>();
        private readonly ToolTip _toolTip = new ToolTip();

        private CancellationTokenSource _cancellationTokenSource;
        private BufferedLogger _logger;
        private ProcessingConfig _config;
        private string _selectedLispPath = string.Empty;
        private bool _isProcessing;

        public MainForm()
        {
            InitializeComponent();
            SetupListView();
            SetupToolTips();
            SetupContextMenu();
            SetupDragAndDrop();
            _config = ProcessingConfig.LoadFromFile(GetConfigFilePath());
            PreloadDefaultLisp();
        }

        private void PreloadDefaultLisp()
        {
            string assemblyDirectory = Path.GetDirectoryName(typeof(MainForm).Assembly.Location);
            if (string.IsNullOrEmpty(assemblyDirectory))
            {
                return;
            }

            string defaultLispPath = Path.Combine(assemblyDirectory, DefaultLispFileName);
            if (!File.Exists(defaultLispPath))
            {
                return;
            }

            SelectLispFile(defaultLispPath);
            statusLabelHelp.Text = "3D check library loaded. Select drawings to start.";
        }

        private void SelectLispFile(string path)
        {
            _selectedLispPath = path;
            txtLspFile.Text = Path.GetFileName(path);
            _toolTip.SetToolTip(txtLspFile, path);
        }

        // Setup ListView with 3 columns
        private void SetupListView()
        {
            lstDwgList.View = View.Details;
            lstDwgList.FullRowSelect = true;
            lstDwgList.GridLines = true;
            lstDwgList.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            lstDwgList.Columns.Clear();

            // Add columns with proportional widths
            lstDwgList.Columns.Add("Drawing Name", 140);
            lstDwgList.Columns.Add("Result", 80);
            lstDwgList.Columns.Add("Values", 120);
            ResizeListViewColumns();
        }

        private void ResizeListViewColumns()
        {
            if (lstDwgList.Columns.Count < 3)
                return;

            int availableWidth = lstDwgList.ClientSize.Width - SystemInformation.VerticalScrollBarWidth;
            if (availableWidth <= 0)
                return;

            lstDwgList.Columns[0].Width = Math.Max(80, (int)(availableWidth * 0.45));
            lstDwgList.Columns[1].Width = Math.Max(60, (int)(availableWidth * 0.25));
            lstDwgList.Columns[2].Width = Math.Max(80, availableWidth - lstDwgList.Columns[0].Width - lstDwgList.Columns[1].Width - 4);
        }

        private void SetupToolTips()
        {
            _toolTip.AutoPopDelay = 5000;
            _toolTip.InitialDelay = 500;
            _toolTip.ReshowDelay = 200;
            _toolTip.ShowAlways = true;

            _toolTip.SetToolTip(btnLoad, "Replace the loaded LISP file. The 3D check library is selected by default.");
            _toolTip.SetToolTip(txtCommand, "Optional AutoCAD command to execute after loading the LISP file.");
            _toolTip.SetToolTip(btnDwgs, "Replace the current drawing list with selected DWG files.");
            _toolTip.SetToolTip(btnAppend, "Add selected DWG files to the existing drawing list.");
            _toolTip.SetToolTip(btnClear, "Remove all drawings from the list.");
            _toolTip.SetToolTip(btnClearSuccess, "Remove drawings that finished successfully and keep unsuccessful ones.");
            _toolTip.SetToolTip(btnProcess, "Start or cancel processing the drawing list.");
            _toolTip.SetToolTip(chkClose, "Close the drawing document after each drawing finishes processing.");
        }

        private void SetupContextMenu()
        {
            _listContextMenu = new ContextMenuStrip();
            ToolStripMenuItem removeItem = new ToolStripMenuItem("Remove");
            removeItem.Click += (sender, e) => RemoveSelectedDrawing();
            _listContextMenu.Items.Add(removeItem);
            ToolStripMenuItem clearItem = new ToolStripMenuItem("Clear List");
            clearItem.Click += (sender, e) => ClearDrawingList();
            _listContextMenu.Items.Add(clearItem);
            ToolStripMenuItem clearSuccessItem = new ToolStripMenuItem("Clear Successful");
            clearSuccessItem.Click += (sender, e) => ClearSuccessfulRuns();
            _listContextMenu.Items.Add(clearSuccessItem);
            lstDwgList.ContextMenuStrip = _listContextMenu;
        }

        private void SetupDragAndDrop()
        {
            lstDwgList.DragEnter += LstDwgList_DragEnter;
            lstDwgList.DragDrop += LstDwgList_DragDrop;
        }

        private void LstDwgList_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void LstDwgList_DragDrop(object sender, DragEventArgs e)
        {
            string[] droppedFiles = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (droppedFiles == null || droppedFiles.Length == 0)
            {
                return;
            }

            string[] dwgFiles = droppedFiles
                .Where(path => string.Equals(Path.GetExtension(path), ".dwg", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (dwgFiles.Length == 0)
            {
                statusLabelHelp.Text = "Only .dwg files can be added by drag-and-drop.";
                return;
            }

            try
            {
                AddDrawings(dwgFiles, replaceExisting: false);
                statusLabelHelp.Text = $"Added {dwgFiles.Length} drawing(s) from drag-and-drop.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding dropped files: {ex.Message}", "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "LISP Files (*.lsp)|*.lsp|All Files (*.*)|*.*";
                openFileDialog.FilterIndex = 1;
                openFileDialog.Title = "Select LISP File";
                openFileDialog.CheckFileExists = true;
                openFileDialog.CheckPathExists = true;
                openFileDialog.Multiselect = false;

                if (!string.IsNullOrEmpty(_selectedLispPath))
                {
                    string currentDirectory = Path.GetDirectoryName(_selectedLispPath);
                    if (!string.IsNullOrEmpty(currentDirectory) && Directory.Exists(currentDirectory))
                    {
                        openFileDialog.InitialDirectory = currentDirectory;
                        openFileDialog.FileName = Path.GetFileName(_selectedLispPath);
                    }
                }

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        SelectLispFile(openFileDialog.FileName);
                        statusLabelHelp.Text = "LISP file selected. Select drawings to start.";
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
            PickDrawings("Select Drawing Files", replaceExisting: true);
        }

        private void btnAppend_Click(object sender, EventArgs e)
        {
            PickDrawings("Append Drawing Files", replaceExisting: false);
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            ClearDrawingList();

            MessageBox.Show("Drawing list cleared successfully.", "List Cleared",
                          MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnClearSuccess_Click(object sender, EventArgs e)
        {
            ClearSuccessfulRuns();
        }

        private void PickDrawings(string title, bool replaceExisting)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "AutoCAD Drawing Files (*.dwg)|*.dwg|All Files (*.*)|*.*";
                openFileDialog.FilterIndex = 1;
                openFileDialog.Title = title;
                openFileDialog.CheckFileExists = true;
                openFileDialog.CheckPathExists = true;
                openFileDialog.Multiselect = true;

                if (openFileDialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    AddDrawings(openFileDialog.FileNames, replaceExisting);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading drawing files: {ex.Message}", "Error",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void AddDrawings(IEnumerable<string> paths, bool replaceExisting)
        {
            lstDwgList.BeginUpdate();
            try
            {
                if (replaceExisting)
                {
                    lstDwgList.Items.Clear();
                    _results.Clear();
                }

                foreach (string path in paths)
                {
                    if (_results.Any(r => string.Equals(r.DrawingPath, path, StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }

                    var result = new DrawingResult
                    {
                        DrawingName = Path.GetFileName(path),
                        DrawingPath = path
                    };
                    _results.Add(result);

                    var item = new ListViewItem(result.DrawingName);
                    item.SubItems.Add(result.ResultStatus);
                    item.SubItems.Add(result.ResultMessage);
                    item.ToolTipText = path;
                    lstDwgList.Items.Add(item);
                }
            }
            finally
            {
                lstDwgList.EndUpdate();
                UpdateDrawingCountStatus();
            }
        }

        public IReadOnlyList<string> SelectedDrawingPaths
        {
            get { return _results.Select(r => r.DrawingPath).ToList(); }
        }

        public void ClearDrawingList()
        {
            lstDwgList.Items.Clear();
            _results.Clear();
            UpdateDrawingCountStatus();
            UpdateRunMetrics(_results);
        }

        /// <summary>
        /// Removes drawings that completed without error. Failed drawings, and drawings
        /// that have not been processed yet, stay in the list so they can be run again.
        /// </summary>
        public void ClearSuccessfulRuns()
        {
            if (_isProcessing)
            {
                return;
            }

            int removed = 0;
            lstDwgList.BeginUpdate();
            try
            {
                for (int i = _results.Count - 1; i >= 0; i--)
                {
                    if (!_results[i].IsSuccess)
                    {
                        continue;
                    }

                    _results.RemoveAt(i);
                    lstDwgList.Items.RemoveAt(i);
                    removed++;
                }
            }
            finally
            {
                lstDwgList.EndUpdate();
                UpdateDrawingCountStatus();
            }

            if (removed == 0)
            {
                statusLabelHelp.Text = "No successful drawings to clear.";
                return;
            }

            statusLabelHelp.Text = $"Removed {removed} successful drawing{(removed == 1 ? "" : "s")}. Unsuccessful drawings remain.";
            MessageBox.Show(
                $"Removed {removed} successful drawing{(removed == 1 ? "" : "s")}. Unsuccessful drawings remain in the list.",
                "Successful Runs Cleared",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        public void RemoveSelectedDrawing()
        {
            if (_isProcessing || lstDwgList.SelectedIndices.Count == 0)
            {
                return;
            }

            int selectedIndex = lstDwgList.SelectedIndices[0];
            lstDwgList.Items.RemoveAt(selectedIndex);
            _results.RemoveAt(selectedIndex);
            UpdateDrawingCountStatus();
        }

        private void UpdateDrawingCountStatus()
        {
            int count = _results.Count;
            statusLabelCount.Text = $"{count} drawing{(count == 1 ? "" : "s")}";
        }

        private void UpdateRunMetrics(IList<DrawingResult> batch)
        {
            int success = 0;
            int finished = 0;
            double totalSeconds = 0;

            foreach (DrawingResult result in batch)
            {
                if (!result.IsProcessed)
                {
                    continue;
                }

                finished++;
                totalSeconds += result.ProcessingTime.TotalSeconds;
                if (result.IsSuccess)
                {
                    success++;
                }
            }

            int remaining = batch.Count - finished;
            string average = finished == 0 ? "--" : FormatDuration(TimeSpan.FromSeconds(totalSeconds / finished));
            string eta;
            if (finished == 0)
            {
                eta = "--";
            }
            else if (remaining == 0)
            {
                eta = "0s";
            }
            else
            {
                eta = FormatDuration(TimeSpan.FromSeconds((totalSeconds / finished) * remaining));
            }

            statusLabelMetrics.Text = $"Success {success}   Avg {average}   ETA {eta}";
        }

        private static string FormatDuration(TimeSpan time)
        {
            if (time.TotalHours >= 1)
            {
                return string.Format("{0}h {1:00}m", (int)time.TotalHours, time.Minutes);
            }

            if (time.TotalMinutes >= 1)
            {
                return string.Format("{0}m {1:00}s", (int)time.TotalMinutes, time.Seconds);
            }

            return string.Format("{0:0.0}s", time.TotalSeconds);
        }

        private void UpdateStatusHelp(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                statusLabelHelp.Text = message;
            }
        }

        private async void btnProcess_Click(object sender, EventArgs e)
        {
            // While a run is in progress the button acts as a Cancel button.
            if (_isProcessing)
            {
                _cancellationTokenSource?.Cancel();
                lblProgress.Text = "Cancelling...";
                return;
            }

            if (_results.Count == 0)
            {
                UpdateStatusHelp("Please select drawing files first.");
                MessageBox.Show("Please select drawing files first.", "No Drawings Selected",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var job = new LispJob(_selectedLispPath, txtCommand.Text, chkClose.Checked);
            string validationError;
            if (!job.TryValidate(out validationError))
            {
                MessageBox.Show(validationError, "Cannot Start Processing",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            await RunProcessingAsync(job);
        }

        private async Task RunProcessingAsync(LispJob job)
        {
            string logFilePath = CreateLogFilePath();

            _logger?.Dispose();
            _logger = new BufferedLogger(logFilePath);

            _logger.Log("=== LISP Tool Processing Session Started ===");
            _logger.Log($"LISP File: {job.LispFileName}");
            _logger.Log($"Command: {(job.HasCommand ? job.Command : "None")}");
            _logger.Log($"Total Drawings: {_results.Count}");
            _logger.Log("================================================");

            _isProcessing = true;
            SetControlsEnabled(false);
            // Keep the form modeless so LISP can run, but ignore clicks in the drawing.
            SetAutoCadInputEnabled(false);

            prgDrawingProgress.Minimum = 0;
            prgDrawingProgress.Maximum = _results.Count;
            prgDrawingProgress.Value = 0;
            lblProgress.Text = "0%";

            _cancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = _cancellationTokenSource.Token;

            var processor = new DrawingProcessor(_config, _logger);
            var batch = _results.ToList();
            int processedCount = 0;
            foreach (DrawingResult pending in batch)
            {
                pending.IsProcessed = false;
                pending.HasError = false;
                pending.ProcessingTime = TimeSpan.Zero;
            }
            UpdateRunMetrics(batch);

            try
            {
                for (int i = 0; i < batch.Count; i++)
                {
                    token.ThrowIfCancellationRequested();

                    DrawingResult result = batch[i];
                    UpdateProgress(i + 1, batch.Count, result.DrawingName);

                    try
                    {
                        await processor.ProcessAsync(result, job, token);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _logger.Log($"Unexpected error for {result.DrawingName}: {ex}");
                        result.SetOutcome($"ERROR {ex.Message}", true);
                    }

                    processedCount++;
                    UpdateListViewItem(result);
                    UpdateRunMetrics(batch);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.Log("Processing cancelled by user");
                MessageBox.Show("Processing was cancelled.", "Cancelled",
                              MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _logger.Log($"Critical error during processing: {ex}");
                MessageBox.Show($"A critical error occurred during processing: {ex.Message}", "Critical Error",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _logger.Log("=== LISP Tool Processing Session Completed ===");
                _logger.Log($"Drawings processed: {processedCount} of {batch.Count}");
                _logger.Log($"Failures: {batch.Count(r => r.HasError)}");
                _logger.Log($"Log file location: {logFilePath}");
                _logger.Flush();

                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;

                _isProcessing = false;
                SetAutoCadInputEnabled(true);
                SetControlsEnabled(true);
                lblProgress.Text = "Complete";
                UpdateStatusHelp("Processing complete.");
            }
        }

        // Runs on the UI thread together with the processing loop, so no marshalling is needed.
        private void UpdateProgress(int currentIndex, int totalCount, string currentFileName)
        {
            prgDrawingProgress.Value = Math.Min(currentIndex, prgDrawingProgress.Maximum);
            int percentage = totalCount == 0 ? 0 : (int)((double)currentIndex / totalCount * 100);
            lblProgress.Text = $"{percentage}%";
            this.Text = $"Run Lisp - Processing: {currentFileName} ({currentIndex}/{totalCount})";
        }

        private void UpdateListViewItem(DrawingResult result)
        {
            int index = _results.IndexOf(result);
            if (index < 0 || index >= lstDwgList.Items.Count)
            {
                return;
            }

            ListViewItem item = lstDwgList.Items[index];
            item.SubItems[1].Text = result.ResultStatus;
            item.SubItems[2].Text = result.ResultMessage;
            item.BackColor = result.IsSuccess ? Color.LightGreen : Color.LightCoral;
        }

        private static string GetAssemblyDirectory()
        {
            return Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        }

        private static string GetConfigFilePath()
        {
            return Path.Combine(GetAssemblyDirectory(), ConfigFileName);
        }

        private string CreateLogFilePath()
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string logsDir = Path.Combine(GetAssemblyDirectory(), "Logs");
            if (!Directory.Exists(logsDir))
            {
                Directory.CreateDirectory(logsDir);
            }
            return Path.Combine(logsDir, $"LispTool_Log_{timestamp}.txt");
        }

        private void SetControlsEnabled(bool enabled)
        {
            btnClear.Enabled = enabled;
            btnClearSuccess.Enabled = enabled;
            btnDwgs.Enabled = enabled;
            btnAppend.Enabled = enabled;
            chkClose.Enabled = enabled;
            txtLspFile.Enabled = enabled;
            txtCommand.Enabled = enabled;
            btnLoad.Enabled = enabled;

            // btnProcess stays enabled and doubles as the Cancel button while running.
            btnProcess.Text = enabled ? "Process" : "Cancel";

            if (enabled)
            {
                prgDrawingProgress.Value = 0;
                lblProgress.Text = "Ready";
                this.Text = "Run Lisp";
            }
        }

        private static void SetAutoCadInputEnabled(bool enabled)
        {
            try
            {
                IntPtr mainWindow = AcadApp.MainWindow.Handle;
                if (mainWindow != IntPtr.Zero)
                {
                    EnableWindow(mainWindow, enabled);
                }
            }
            catch (Exception)
            {
                // Leave AutoCAD as it is if the main window handle cannot be changed.
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            SetAutoCadInputEnabled(true);
            base.OnFormClosed(e);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_isProcessing)
            {
                DialogResult answer = MessageBox.Show(
                    "Processing is still running. Cancel it and close the window?",
                    "Processing In Progress", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (answer != DialogResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }

                _cancellationTokenSource?.Cancel();
            }

            base.OnFormClosing(e);
        }
    }
}
