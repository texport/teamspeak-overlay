using System;
using System.IO;
using System.Text;

namespace TeamSpeakOverlay.Infrastructure.Logging
{
    public static class Logger
    {
        private static readonly object _lock = new object();
        public static string AppDataLogPath { get; }
        public static string LocalLogPath { get; }

        static Logger()
        {
            string appDataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "TeamSpeakOverlay"
            );
            if (!Directory.Exists(appDataDir))
            {
                Directory.CreateDirectory(appDataDir);
            }

            AppDataLogPath = Path.Combine(appDataDir, "overlay.log");
            LocalLogPath = @"c:\Users\ivano\OneDrive\Documents\develop\teamspeak-overlay\overlay.log";

            Info("=================== LOGGING INITIALIZED ===================", "Logger");
        }

        public static void Log(string level, string message, string component = "General")
        {
            string time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string line = $"[{time}] [{level.PadRight(5)}] [{component}] {message}";

            System.Diagnostics.Debug.WriteLine(line);
            Console.WriteLine(line);

            lock (_lock)
            {
                WriteToFile(LocalLogPath, line);
                WriteToFile(AppDataLogPath, line);
            }
        }

        private static void WriteToFile(string path, string line)
        {
            try
            {
                File.AppendAllText(path, line + Environment.NewLine, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"File write error to {path}: {ex.Message}");
            }
        }

        public static void Info(string message, string component = "General") => Log("INFO", message, component);
        public static void Debug(string message, string component = "General") => Log("DEBUG", message, component);
        public static void Warn(string message, string component = "General") => Log("WARN", message, component);
        public static void Error(string message, Exception? ex = null, string component = "General") =>
            Log("ERROR", $"{message} {(ex != null ? ex.ToString() : "")}", component);
    }
}

