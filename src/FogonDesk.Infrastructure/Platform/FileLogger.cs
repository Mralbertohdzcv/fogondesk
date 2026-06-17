using System;
using System.IO;
using System.Text;
using FogonDesk.Application.Contracts;
using FogonDesk.Configuration;

namespace FogonDesk.Infrastructure.Platform
{
    public sealed class FileLogger : IAppLogger
    {
        private static readonly object SyncRoot = new object();
        private readonly StationPaths paths;

        public FileLogger(StationPaths paths)
        {
            this.paths = paths;
            StationPathsFactory.EnsureCreated(paths);
        }

        public void Info(string message)
        {
            Write("INFO", message, null);
        }

        public void Warn(string message)
        {
            Write("WARN", message, null);
        }

        public void Error(string message, Exception exception = null)
        {
            Write("ERROR", message, exception);
        }

        private void Write(string level, string message, Exception exception)
        {
            var filePath = Path.Combine(this.paths.LogPath, "fogondesk-" + DateTime.Now.ToString("yyyyMMdd") + ".log");
            var builder = new StringBuilder();
            builder.Append('[').Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")).Append("] ");
            builder.Append(level).Append(" ");
            builder.Append(message ?? string.Empty);

            if (exception != null)
            {
                builder.AppendLine();
                builder.Append(exception);
            }

            builder.AppendLine();

            lock (SyncRoot)
            {
                File.AppendAllText(filePath, builder.ToString(), Encoding.UTF8);
            }
        }
    }
}
