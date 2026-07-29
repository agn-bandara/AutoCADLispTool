using System;
using System.IO;

namespace AutoCADLispTool.Services
{
    /// <summary>
    /// Describes a single batch of work: which LISP file to load, which expression
    /// to evaluate, and what to do with the drawing afterwards.
    /// </summary>
    public class LispJob
    {
        public LispJob(string lispFilePath, string command, bool closeAfterProcessing)
        {
            LispFilePath = lispFilePath ?? string.Empty;
            Command = command ?? string.Empty;
            CloseAfterProcessing = closeAfterProcessing;
        }

        public string LispFilePath { get; private set; }

        public string Command { get; private set; }

        public bool CloseAfterProcessing { get; private set; }

        public bool HasLispFile
        {
            get { return !string.IsNullOrWhiteSpace(LispFilePath); }
        }

        public bool HasCommand
        {
            get { return !string.IsNullOrWhiteSpace(Command); }
        }

        public string LispFileName
        {
            get { return HasLispFile ? Path.GetFileName(LispFilePath) : "None"; }
        }

        /// <summary>
        /// Validates the job, returning false and an explanation when it cannot be run.
        /// </summary>
        public bool TryValidate(out string error)
        {
            if (HasCommand && !HasLispFile)
            {
                error = "Please select a LISP file first when using commands.";
                return false;
            }

            if (HasLispFile && !File.Exists(LispFilePath))
            {
                error = string.Format("The selected LISP file no longer exists:{0}{1}", Environment.NewLine, LispFilePath);
                return false;
            }

            if (HasCommand && Command.IndexOf('"') >= 0)
            {
                error = "The command must not contain double quote characters.";
                return false;
            }

            error = null;
            return true;
        }
    }
}
