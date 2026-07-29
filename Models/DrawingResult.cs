using System;

namespace AutoCADLispTool.Models
{
    /// <summary>
    /// Data model for drawing processing results
    /// </summary>
    public class DrawingResult
    {
        public string DrawingName { get; set; }
        public string DrawingPath { get; set; }
        public string ResultStatus { get; set; }
        public string ResultMessage { get; set; }
        public bool IsProcessed { get; set; }
        public bool HasError { get; set; }
        public TimeSpan ProcessingTime { get; set; }

        /// <summary>
        /// True only when the drawing was processed without any error.
        /// </summary>
        public bool IsSuccess
        {
            get { return IsProcessed && !HasError; }
        }

        public DrawingResult()
        {
            DrawingName = string.Empty;
            DrawingPath = string.Empty;
            ResultStatus = string.Empty;
            ResultMessage = string.Empty;
            IsProcessed = false;
            HasError = false;
            ProcessingTime = TimeSpan.Zero;
        }

        /// <summary>
        /// Records the outcome of a processing attempt, splitting the raw output into
        /// a short status token and the remaining detail message.
        /// </summary>
        public void SetOutcome(string output, bool hasError)
        {
            IsProcessed = true;
            HasError = hasError;

            if (string.IsNullOrWhiteSpace(output))
            {
                ResultStatus = hasError ? "ERROR" : string.Empty;
                ResultMessage = string.Empty;
                return;
            }

            string[] words = output.Trim().Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
            ResultStatus = words[0];
            ResultMessage = words.Length > 1 ? words[1] : string.Empty;
        }
    }
}
