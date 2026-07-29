using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using AutoCADLispTool.Models;
using System;
using System.Diagnostics;
using System.IO;
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
        public async Task ProcessAsync(DrawingResult result, LispJob job, CancellationToken token)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (job == null) throw new ArgumentNullException(nameof(job));

            Stopwatch stopwatch = Stopwatch.StartNew();
            Document document = null;

            try
            {
                token.ThrowIfCancellationRequested();

                _logger.Log($"Attempting to open: {result.DrawingName}");
                document = AcadApp.DocumentManager.Open(result.DrawingPath, false);

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

                string output = "Document opened successfully";
                bool hasError = false;

                using (document.LockDocument())
                {
                    _logger.Log($"Document locked for processing: {result.DrawingName}");

                    if (job.HasCommand)
                    {
                        CommandOutcome outcome = await ExecuteCommandAsync(document, job, result.DrawingName, token);
                        output = outcome.Output;
                        hasError = outcome.HasError;
                    }

                    string saveError = await SaveAsync(document, result, token);
                    if (saveError != null)
                    {
                        output = saveError;
                        hasError = true;
                    }
                }

                _logger.Log($"Document lock released for: {result.DrawingName}");

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

        private async Task<CommandOutcome> ExecuteCommandAsync(Document document, LispJob job, string drawingName, CancellationToken token)
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
                    document.SendStringToExecute($"(setq {ResultVariableName} nil)\n", true, false, false);
                    document.SendStringToExecute($"(setq {ResultVariableName} {job.Command})\n", true, false, true);
                    await Task.Delay(_config.CommandExecutionDelayMs, token);

                    object value = document.GetLispSymbol(ResultVariableName);
                    string output = value != null ? value.ToString() : "LISP no result";

                    Editor editor = document.Editor;
                    editor.WriteMessage($"\n{output}");
                    _logger.Log($"LISP command executed for: {drawingName}");
                    return new CommandOutcome(output, value == null);
                }

                document.SendStringToExecute(job.Command + "\n", true, false, false);
                await Task.Delay(_config.CommandExecutionDelayMs, token);
                _logger.Log($"Command executed for: {drawingName}");
                return new CommandOutcome("LISP executed", false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.Log($"Command execution error for {drawingName}: {ex}");
                return new CommandOutcome($"Command execution error: {ex.Message}", true);
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

        private async Task<string> SaveAsync(Document document, DrawingResult result, CancellationToken token)
        {
            try
            {
                FileInfo fileInfo = new FileInfo(result.DrawingPath);
                if (fileInfo.IsReadOnly)
                {
                    fileInfo.IsReadOnly = false;
                    _logger.Log($"Removed read-only attribute from: {result.DrawingName}");
                }

                document.SendStringToExecute("QSAVE\n", true, false, false);
                await Task.Delay(_config.SaveDelayMs, token);
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
                document.CloseAndDiscard();
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
    }
}
