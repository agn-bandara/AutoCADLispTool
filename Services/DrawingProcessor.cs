using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using AutoCADLispTool.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace AutoCADLispTool.Services
{
    /// <summary>
    /// Encapsulates all AutoCAD document interop for a batch run: opening a drawing,
    /// loading and evaluating a LISP expression, saving, and closing.
    /// </summary>
    public class DrawingProcessor
    {
        private const string ResultVariableName = "lispToolResult";
        private const int ClearResultTimeoutMs = 10000;
        private const int CloseIdleTimeoutMs = 10000;
        private static readonly object TimeoutMarker = new object();

        private readonly ProcessingConfig _config;
        private readonly BufferedLogger _logger;

        public DrawingProcessor(ProcessingConfig config, BufferedLogger logger)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            _config = config;
            _logger = logger;
        }

        /// <summary>
        /// Processes a single drawing, recording the outcome on the supplied result.
        /// </summary>
        public async Task ProcessAsync(DrawingResult result, LispJob job, IProgress<string> progress, CancellationToken token)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (job == null) throw new ArgumentNullException(nameof(job));

            Stopwatch stopwatch = Stopwatch.StartNew();
            Document document = null;

            try
            {
                token.ThrowIfCancellationRequested();

                _logger.Log($"Attempting to open: {result.DrawingName}");
                using (new EscapeWatchdog(GetMainWindowHandle(), _config.EffectiveLispResultTimeoutMs))
                {
                    document = AcadApp.DocumentManager.Open(result.DrawingPath, false);
                }

                if (document == null)
                {
                    Fail(result, "Failed to open document");
                    return;
                }

                _logger.Log($"Successfully opened: {result.DrawingName}");

                if (!await TryActivateAsync(document, token))
                {
                    Fail(result, "Failed to make document current");
                    return;
                }

                if (!await EnsureHostLoadedAsync(document, progress, token))
                {
                    Fail(result, "ERROR Application was not loaded in this drawing");
                    return;
                }

                string output = "Document opened successfully";
                bool hasError = false;

                // Do not lock the document across this wait. SendStringToExecute
                // cannot run while the lock is held, so a fixed delay would read
                // lispToolResult before the command had started.
                if (job.HasCommand)
                {
                    CommandOutcome outcome = await ExecuteCommandAsync(document, job, result.DrawingName, progress, token);
                    output = outcome.Output;
                    hasError = outcome.HasError;
                }

                string saveError = await SaveAsync(document, result, progress, token);
                if (saveError != null)
                {
                    output = saveError;
                    hasError = true;
                }

                result.SetOutcome(output, hasError);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.Log($"Processing error for {result.DrawingName}: {ex}");
                Fail(result, $"Document error: {ex.Message}");
            }
            finally
            {
                if (document != null && job.CloseAfterProcessing)
                {
                    try
                    {
                        await WaitUntilIdleAsync(document, CloseIdleTimeoutMs, "Waiting to close the drawing", progress, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        _logger.Log($"Close wait warning for {result.DrawingName}: {ex.Message}");
                    }
                }

                CloseIfRequested(document, job, result.DrawingName);

                stopwatch.Stop();
                result.ProcessingTime = stopwatch.Elapsed;
                _logger.Log($"{result.DrawingName} | {(result.HasError ? "ERROR" : "SUCCESS")} | {result.ResultStatus} {result.ResultMessage} ({result.ProcessingTime.TotalSeconds:F2}s)");
            }
        }

        private async Task<bool> TryActivateAsync(Document document, CancellationToken token)
        {
            AcadApp.DocumentManager.MdiActiveDocument = document;
            await Task.Delay(_config.DocumentActivationDelayMs, token);
            return ReferenceEquals(AcadApp.DocumentManager.MdiActiveDocument, document);
        }

        private const int HostLoadTimeoutMs = 15000;

        /// <summary>
        /// AutoCAD will not run the queued LISP until this application has been
        /// loaded into the newly opened drawing. Do that here instead of waiting
        /// for a manual APPLOAD.
        /// </summary>
        private async Task<bool> EnsureHostLoadedAsync(Document document, IProgress<string> progress, CancellationToken token)
        {
            string location = Assembly.GetExecutingAssembly().Location;
            if (string.IsNullOrWhiteSpace(location))
            {
                return true;
            }

            try
            {
                document.Window.Focus();
            }
            catch (Exception ex)
            {
                _logger.Log($"Could not focus the drawing window: {ex.Message}");
            }

            string path = location.Replace('\\', '/');
            _logger.Log($"Loading application into {document.Name}");
            document.SendStringToExecute("_.NETLOAD \"" + path + "\"\n", true, false, false);
            document.SendStringToExecute("LispToolDocInit\n", true, false, false);
            await Task.Delay(Math.Max(50, _config.CommandExecutionDelayMs), token);

            bool idle = await WaitUntilIdleAsync(
                document,
                HostLoadTimeoutMs,
                "Loading the application into this drawing",
                progress,
                token);
            if (!idle)
            {
                _logger.Log($"Application load timed out for {document.Name}.");
            }

            return idle;
        }

        private async Task<CommandOutcome> ExecuteCommandAsync(Document document, LispJob job, string drawingName, IProgress<string> progress, CancellationToken token)
        {
            using (EscapeWatchdog watchdog = new EscapeWatchdog(GetMainWindowHandle(), _config.EffectiveLispResultTimeoutMs))
            {
                try
                {
                    if (job.HasLispFile)
                    {
                        // AutoLISP accepts forward slashes, which avoids fragile backslash escaping.
                        string loadPath = job.LispFilePath.Replace('\\', '/');
                        document.SendStringToExecute($"(load \"{loadPath}\")\n", true, false, false);
                        await Task.Delay(_config.LispLoadDelayMs, token);

                        // Clear any value left over from the previous drawing so a failed
                        // evaluation cannot be reported as the previous drawing's result.
                        await ClearLispResultAsync(document, progress, watchdog, token);
                        document.SendStringToExecute($"(setq {ResultVariableName} {job.Command})\n", true, false, true);

                        object value = await WaitForLispResultAsync(document, progress, watchdog, token);
                        string output = value != null ? value.ToString() : "Timed out waiting for LISP result";

                        Editor editor = document.Editor;
                        editor.WriteMessage($"\n{output}");
                        _logger.Log($"LISP command executed for: {drawingName}");
                        return new CommandOutcome(output, value == null);
                    }

                    document.SendStringToExecute(job.Command + "\n", true, false, false);
                    await Task.Delay(_config.CommandExecutionDelayMs, token);
                    bool finished = await WaitUntilIdleAsync(document, _config.EffectiveLispResultTimeoutMs, "Waiting for the command", progress, token);
                    if (!finished || watchdog.Fired)
                    {
                        _logger.Log($"Timed out after {_config.EffectiveLispResultTimeoutMs}ms waiting for the command on {drawingName}.");
                        return new CommandOutcome("Timed out waiting for the command", true);
                    }

                    _logger.Log($"Command executed for: {drawingName}");
                    return new CommandOutcome("LISP executed", false);
                }
                catch (OperationCanceledException)
                {
                    CancelRunningCommand(document);
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.Log($"Command execution error for {drawingName}: {ex}");
                    return new CommandOutcome($"Command execution error: {ex.Message}", true);
                }
            }
        }

        private async Task ClearLispResultAsync(Document document, IProgress<string> progress, EscapeWatchdog watchdog, CancellationToken token)
        {
            object existing;
            if (TryReadLispSymbol(document, out existing) && existing == null)
            {
                return;
            }

            document.SendStringToExecute($"(setq {ResultVariableName} nil)\n", true, false, false);
            object cleared = await WaitForLispSymbolAsync(
                document,
                value => value == null,
                Math.Min(ClearResultTimeoutMs, _config.EffectiveLispResultTimeoutMs),
                "Clearing the previous LISP result",
                progress,
                watchdog,
                token);

            if (ReferenceEquals(cleared, TimeoutMarker))
            {
                _logger.Log($"Timed out clearing {ResultVariableName}.");
            }
        }

        private async Task<object> WaitForLispResultAsync(Document document, IProgress<string> progress, EscapeWatchdog watchdog, CancellationToken token)
        {
            object value = await WaitForLispSymbolAsync(
                document,
                candidate => candidate != null,
                _config.EffectiveLispResultTimeoutMs,
                "Waiting for LISP result",
                progress,
                watchdog,
                token);

            if (value == null || ReferenceEquals(value, TimeoutMarker))
            {
                _logger.Log($"Timed out after {_config.EffectiveLispResultTimeoutMs}ms waiting for {ResultVariableName}.");
                return null;
            }

            return value;
        }

        private async Task<object> WaitForLispSymbolAsync(
            Document document,
            Func<object, bool> isReady,
            int timeoutMs,
            string activity,
            IProgress<string> progress,
            EscapeWatchdog watchdog,
            CancellationToken token)
        {
            int waitedMs = 0;
            int pollMs = Math.Max(50, _config.CommandExecutionDelayMs);

            while (waitedMs <= timeoutMs)
            {
                token.ThrowIfCancellationRequested();
                if (watchdog != null && watchdog.Fired)
                {
                    break;
                }

                object value;
                if (TryReadLispSymbol(document, out value) && isReady(value))
                {
                    return value;
                }

                int elapsedSeconds = waitedMs / 1000;
                int timeoutSeconds = Math.Max(1, timeoutMs / 1000);
                progress?.Report($"{activity} ({elapsedSeconds}s / {timeoutSeconds}s). Cancel is available.");

                await Task.Delay(pollMs, token);
                waitedMs += pollMs;
            }

            CancelRunningCommand(document);
            return TimeoutMarker;
        }

        private async Task<bool> WaitUntilIdleAsync(Document document, int timeoutMs, string activity, IProgress<string> progress, CancellationToken token)
        {
            int waitedMs = 0;
            int pollMs = Math.Max(50, _config.CommandExecutionDelayMs);

            while (waitedMs <= timeoutMs)
            {
                token.ThrowIfCancellationRequested();
                if (IsCommandIdle())
                {
                    return true;
                }

                int elapsedSeconds = waitedMs / 1000;
                int timeoutSeconds = Math.Max(1, timeoutMs / 1000);
                progress?.Report($"{activity} ({elapsedSeconds}s / {timeoutSeconds}s). Cancel is available.");

                await Task.Delay(pollMs, token);
                waitedMs += pollMs;
            }

            _logger.Log($"Timed out after {timeoutMs}ms during: {activity}");
            CancelRunningCommand(document);
            return false;
        }

        private bool TryReadLispSymbol(Document document, out object value)
        {
            value = null;
            if (!IsCommandIdle())
            {
                return false;
            }

            try
            {
                using (document.LockDocument())
                {
                    value = document.GetLispSymbol(ResultVariableName);
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool IsCommandIdle()
        {
            try
            {
                string names = Convert.ToString(AcadApp.GetSystemVariable("CMDNAMES"));
                return string.IsNullOrWhiteSpace(names) || names.Trim() == ".";
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void CancelRunningCommand(Document document)
        {
            try
            {
                document.SendStringToExecute("\u0003\u0003", true, false, false);
                _logger.Log("Sent Esc so the stuck command can release the drawing.");
            }
            catch (Exception ex)
            {
                _logger.Log($"Could not cancel the active command: {ex.Message}");
            }
        }

        private static IntPtr GetMainWindowHandle()
        {
            try
            {
                return AcadApp.MainWindow.Handle;
            }
            catch (Exception)
            {
                return IntPtr.Zero;
            }
        }

        private sealed class CommandOutcome
        {
            public CommandOutcome(string output, bool hasError)
            {
                Output = output;
                HasError = hasError;
            }

            public string Output { get; private set; }

            public bool HasError { get; private set; }
        }

        private async Task<string> SaveAsync(Document document, DrawingResult result, IProgress<string> progress, CancellationToken token)
        {
            try
            {
                FileInfo fileInfo = new FileInfo(result.DrawingPath);
                if (fileInfo.IsReadOnly)
                {
                    fileInfo.IsReadOnly = false;
                    _logger.Log($"Removed read-only attribute from: {result.DrawingName}");
                }

                using (EscapeWatchdog watchdog = new EscapeWatchdog(GetMainWindowHandle(), _config.EffectiveSaveTimeoutMs))
                {
                    document.SendStringToExecute("QSAVE\n", true, false, false);
                    await Task.Delay(_config.SaveDelayMs, token);

                    bool saved = await WaitUntilIdleAsync(document, _config.EffectiveSaveTimeoutMs, "Waiting for save", progress, token);
                    if (!saved || watchdog.Fired)
                    {
                        return "Save timed out";
                    }
                }

                _logger.Log($"Document saved: {result.DrawingName}");
                return null;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.Log($"QSAVE failed for {result.DrawingName}: {ex}");
                return $"Save failed: {ex.Message}";
            }
        }

        private void CloseIfRequested(Document document, LispJob job, string drawingName)
        {
            if (document == null)
            {
                return;
            }

            if (!job.CloseAfterProcessing)
            {
                _logger.Log($"Document left open: {drawingName}");
                return;
            }

            try
            {
                using (new EscapeWatchdog(GetMainWindowHandle(), CloseIdleTimeoutMs))
                {
                    document.CloseAndDiscard();
                }
                _logger.Log($"Document closed: {drawingName}");
            }
            catch (Exception ex)
            {
                _logger.Log($"Close warning for {drawingName}: {ex.Message}");
            }
        }

        private void Fail(DrawingResult result, string message)
        {
            _logger.Log($"{result.DrawingName}: {message}");
            result.SetOutcome(message, true);
        }

        /// <summary>
        /// Posts Esc if a command is still running when the timeout elapses.
        /// This runs off the AutoCAD thread, so it can interrupt a call that
        /// never returns to the Cancel button.
        /// </summary>
        private sealed class EscapeWatchdog : IDisposable
        {
            private const int VkEscape = 0x1B;
            private const uint WmKeyDown = 0x0100;
            private const uint WmKeyUp = 0x0101;

            private readonly IntPtr _window;
            private readonly Timer _timer;
            private int _state;

            [DllImport("user32.dll")]
            private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

            [DllImport("user32.dll")]
            private static extern bool EnableWindow(IntPtr hWnd, bool bEnable);

            public EscapeWatchdog(IntPtr window, int timeoutMs)
            {
                _window = window;
                if (window == IntPtr.Zero || timeoutMs <= 0)
                {
                    _state = 2;
                    return;
                }

                _timer = new Timer(Fire, null, timeoutMs, Timeout.Infinite);
            }

            public bool Fired
            {
                get { return Volatile.Read(ref _state) == 1; }
            }

            private void Fire(object state)
            {
                if (Interlocked.CompareExchange(ref _state, 1, 0) != 0)
                {
                    return;
                }

                // The drawing frame is disabled during a batch, so turn it on
                // before Esc or the key is discarded.
                EnableWindow(_window, true);
                PostMessage(_window, WmKeyDown, (IntPtr)VkEscape, IntPtr.Zero);
                PostMessage(_window, WmKeyUp, (IntPtr)VkEscape, IntPtr.Zero);
                PostMessage(_window, WmKeyDown, (IntPtr)VkEscape, IntPtr.Zero);
                PostMessage(_window, WmKeyUp, (IntPtr)VkEscape, IntPtr.Zero);
            }

            public void Dispose()
            {
                Interlocked.CompareExchange(ref _state, 2, 0);
                if (_timer != null)
                {
                    _timer.Dispose();
                }
            }
        }
    }

    /// <summary>
    /// Suppresses file and command prompts for one batch, then restores the
    /// previous system variables. A prompt that cannot be clicked would leave
    /// the batch stuck until someone dismisses it.
    /// </summary>
    internal sealed class BatchEnvironment : IDisposable
    {
        private readonly List<KeyValuePair<string, object>> _saved = new List<KeyValuePair<string, object>>();
        private readonly BufferedLogger _logger;

        public BatchEnvironment(string lispFilePath, BufferedLogger logger)
        {
            _logger = logger;
            SetNumeric("FILEDIA", 0);
            SetNumeric("CMDDIA", 0);
            SetNumeric("EXPERT", 5);
            SetNumeric("ATTDIA", 0);
            SetNumeric("PROXYNOTICE", 0);
            SetNumeric("SECURELOAD", 0);
            TrustFolders(
                Path.GetDirectoryName(lispFilePath),
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
        }

        public void Dispose()
        {
            for (int i = _saved.Count - 1; i >= 0; i--)
            {
                try
                {
                    AcadApp.SetSystemVariable(_saved[i].Key, _saved[i].Value);
                }
                catch (Exception ex)
                {
                    _logger?.Log($"Could not restore {_saved[i].Key}: {ex.Message}");
                }
            }
        }

        private void SetNumeric(string name, int value)
        {
            try
            {
                object current = AcadApp.GetSystemVariable(name);
                _saved.Add(new KeyValuePair<string, object>(name, current));
                object boxed = Convert.ChangeType(value, current.GetType());
                AcadApp.SetSystemVariable(name, boxed);
            }
            catch (Exception ex)
            {
                _logger?.Log($"Could not set {name}: {ex.Message}");
            }
        }

        private void TrustFolders(params string[] folders)
        {
            try
            {
                string trusted = Convert.ToString(AcadApp.GetSystemVariable("TRUSTEDPATHS")) ?? string.Empty;
                _saved.Add(new KeyValuePair<string, object>("TRUSTEDPATHS", trusted));

                string updated = trusted;
                foreach (string folder in folders)
                {
                    if (string.IsNullOrWhiteSpace(folder) || ContainsPath(updated, folder))
                    {
                        continue;
                    }

                    updated = string.IsNullOrWhiteSpace(updated) ? folder : updated + ";" + folder;
                }

                if (!string.Equals(updated, trusted, StringComparison.OrdinalIgnoreCase))
                {
                    AcadApp.SetSystemVariable("TRUSTEDPATHS", updated);
                }
            }
            catch (Exception ex)
            {
                _logger?.Log($"Could not trust the application folders: {ex.Message}");
            }
        }

        private static bool ContainsPath(string trustedList, string folder)
        {
            string[] parts = trustedList.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string part in parts)
            {
                if (string.Equals(part.Trim().TrimEnd('\\'), folder.Trim().TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
