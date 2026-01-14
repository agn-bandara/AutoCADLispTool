using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace AutoCADLispTool.Services
{
    /// <summary>
    /// High-performance buffered logging service that reduces disk I/O operations
    /// </summary>
    public sealed class BufferedLogger : IDisposable
    {
        private readonly string _logFilePath;
        private readonly List<string> _buffer;
        private readonly object _lock = new object();
        private readonly int _flushThreshold;
        private readonly Timer _flushTimer;
        private bool _disposed;

        public BufferedLogger(string logFilePath, int flushThreshold = 10)
        {
            _logFilePath = logFilePath;
            _buffer = new List<string>();
            _flushThreshold = flushThreshold;
            _flushTimer = new Timer(_ => Flush(), null, 5000, 5000);
            
            EnsureDirectoryExists();
        }

        /// <summary>
        /// Add a message to the log buffer
        /// </summary>
        public void Log(string message)
        {
            if (_disposed) return;
            
            var entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
            lock (_lock)
            {
                _buffer.Add(entry);
                if (_buffer.Count >= _flushThreshold)
                {
                    FlushInternal();
                }
            }
        }

        /// <summary>
        /// Force flush all buffered messages to disk
        /// </summary>
        public void Flush()
        {
            lock (_lock)
            {
                FlushInternal();
            }
        }

        private void FlushInternal()
        {
            if (_buffer.Count == 0) return;
            
            try
            {
                File.AppendAllLines(_logFilePath, _buffer);
                _buffer.Clear();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"BufferedLogger flush error: {ex.Message}");
            }
        }

        private void EnsureDirectoryExists()
        {
            var dir = Path.GetDirectoryName(_logFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            
            _disposed = true;
            _flushTimer?.Dispose();
            Flush();
        }
    }
}
