using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ImageMove
{
    internal sealed class GridReviewSessionLogger : IDisposable
    {
        private readonly object sync = new object();
        private readonly StreamWriter writer;
        private bool disposed;

        internal GridReviewSessionLogger(string baseDirectory)
        {
            string safeBaseDirectory = string.IsNullOrWhiteSpace(baseDirectory)
                ? AppDomain.CurrentDomain.BaseDirectory
                : baseDirectory;
            string logDirectory = Path.Combine(safeBaseDirectory, "logs");
            Directory.CreateDirectory(logDirectory);

            string fileName = "imagemove_grid_review_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".log";
            string logPath = Path.Combine(logDirectory, fileName);
            writer = new StreamWriter(logPath, false, new UTF8Encoding(false))
            {
                AutoFlush = true
            };

            Log("INFO", "グリッド目検ログを開始しました。", new[] { CreateDetails("WHO", "ImageMove") });
        }

        internal string LogPath
        {
            get
            {
                return writer?.BaseStream is FileStream fileStream
                    ? fileStream.Name
                    : string.Empty;
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            lock (sync)
            {
                writer?.Dispose();
            }
        }

        internal void Info(string message, params KeyValuePair<string, string>[] details)
        {
            Log("INFO", message, details);
        }

        internal void Warn(string message, params KeyValuePair<string, string>[] details)
        {
            Log("WARN", message, details);
        }

        internal void Error(string message, params KeyValuePair<string, string>[] details)
        {
            Log("ERROR", message, details);
        }

        internal static KeyValuePair<string, string> CreateDetails(string key, object value)
        {
            return new KeyValuePair<string, string>(key ?? string.Empty, value?.ToString() ?? string.Empty);
        }

        private void Log(string level, string message, IReadOnlyList<KeyValuePair<string, string>> details)
        {
            if (disposed)
            {
                return;
            }

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var builder = new StringBuilder();
            builder.Append(timestamp);
            builder.Append(" [");
            builder.Append(level);
            builder.Append("] ");
            builder.Append(message ?? string.Empty);

            if (details != null)
            {
                foreach (KeyValuePair<string, string> detail in details)
                {
                    if (string.IsNullOrWhiteSpace(detail.Key))
                    {
                        continue;
                    }

                    builder.Append(" | ");
                    builder.Append(detail.Key);
                    builder.Append('=');
                    builder.Append(detail.Value ?? string.Empty);
                }
            }

            lock (sync)
            {
                writer.WriteLine(builder.ToString());
            }
        }
    }
}
