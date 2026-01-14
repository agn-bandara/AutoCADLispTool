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
    }
}
