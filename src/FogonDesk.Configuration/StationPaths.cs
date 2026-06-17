using System;
using System.IO;

namespace FogonDesk.Configuration
{
    public sealed class StationPaths
    {
        public StationPaths(string rootPath)
        {
            this.RootPath = rootPath;
            this.DataPath = Path.Combine(rootPath, "data");
            this.BackupPath = Path.Combine(rootPath, "backups");
            this.LogPath = Path.Combine(rootPath, "logs");
            this.DatabaseFilePath = Path.Combine(this.DataPath, "fogondesk.db");
        }

        public string RootPath { get; private set; }
        public string DataPath { get; private set; }
        public string BackupPath { get; private set; }
        public string LogPath { get; private set; }
        public string DatabaseFilePath { get; private set; }
    }

    public static class StationPathsFactory
    {
        public static StationPaths CreateDefault()
        {
            var rootPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FogonDesk");

            return new StationPaths(rootPath);
        }

        public static void EnsureCreated(StationPaths paths)
        {
            if (paths == null)
            {
                throw new ArgumentNullException(nameof(paths));
            }

            Directory.CreateDirectory(paths.RootPath);
            Directory.CreateDirectory(paths.DataPath);
            Directory.CreateDirectory(paths.BackupPath);
            Directory.CreateDirectory(paths.LogPath);
        }
    }
}
