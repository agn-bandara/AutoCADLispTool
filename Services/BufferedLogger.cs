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
        private const int MaxBufferedEntries = 10000;
        private const int TimerIntervalMs = 5000;

        private readonly string _logFilePath;
        private readonly List<string> _buffer;
        private readonly object _lock = new object();
        private readonly int _flushThreshold;
        private readonly Timer _flushTimer;
        private StreamWriter _writer;
        private bool _disposed;

        public BufferedLogger(string logFilePath, int flushThreshold = 10)
        {
            if (string.IsNullOrWhiteSpace(logFilePath))
            {
                throw new ArgumentException("A log file path is required.", nameof(logFilePath));
            }

            _logFilePath = logFilePath;
            _flushThreshold = Math.Max(1, flushThreshold);
            _buffer = new List<string>(_flushThreshold);

            EnsureDirectoryExists();

            _flushTimer = new Timer(_ => Flush(), null, TimerIntervalMs, TimerIntervalMs);
        }

        /// <summary>
        /// Add a message to the log buffer
        /// </summary>
        public void Log(string message)
        {
            lock (_lock)
            {
                if (_disposed) return;

                _buffer.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");

                if (_buffer.Count >= _flushThreshold)
                {
                    FlushInternal();
                }
                else if (_buffer.Count > MaxBufferedEntries)
                {
                    // Writing is failing persistently; drop the oldest entries rather
                    // than growing the buffer without bound.
                    _buffer.RemoveRange(0, _buffer.Count - MaxBufferedEntries);
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
                if (_writer == null)
                {
                    _writer = new StreamWriter(_logFilePath, true) { AutoFlush = false };
                }

                foreach (string entry in _buffer)
                {
                    _writer.WriteLine(entry);
                }

                _writer.Flush();
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
            lock (_lock)
            {
                if (_disposed) return;
                _disposed = true;
            }

            // Wait for any in-flight timer callback so it cannot race the final flush.
            using (var timerDisposed = new ManualResetEvent(false))
            {
                if (_flushTimer.Dispose(timerDisposed))
                {
                    timerDisposed.WaitOne();
                }
            }

            lock (_lock)
            {
                FlushInternal();

                if (_writer != null)
                {
                    _writer.Dispose();
                    _writer = null;
                }
            }
        }
    }
}
