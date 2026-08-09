using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using TeamSpeakOverlay.Infrastructure.Logging;

namespace TeamSpeakOverlay.Application.UseCases
{
    public class ExportLogsUseCase
    {
        public bool OpenLogFolder()
        {
            try
            {
                string logPath = Logger.AppDataLogPath;
                string dir = Path.GetDirectoryName(logPath) ?? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                Logger.Info($"Opening log folder: {dir}", "ExportLogsUseCase");

                Process.Start(new ProcessStartInfo
                {
                    FileName = dir,
                    UseShellExecute = true,
                    Verb = "open"
                });

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to open log folder", ex, "ExportLogsUseCase");
                return false;
            }
        }

        public bool CopyLogToClipboard()
        {
            try
            {
                string logPath = Logger.AppDataLogPath;
                if (File.Exists(logPath))
                {
                    string content = File.ReadAllText(logPath);
                    System.Windows.Clipboard.SetText(content);
                    Logger.Info("Copied log content to clipboard", "ExportLogsUseCase");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to copy log content to clipboard", ex, "ExportLogsUseCase");
                return false;
            }
        }
    }
}
