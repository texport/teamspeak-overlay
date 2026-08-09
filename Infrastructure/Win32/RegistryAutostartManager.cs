using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using TeamSpeakOverlay.Infrastructure.Logging;

namespace TeamSpeakOverlay.Infrastructure.Win32
{
    public static class RegistryAutostartManager
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "TeamSpeakOverlay";

        public static void SetAutostart(bool enable)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
                if (key == null) return;

                if (enable)
                {
                    string exePath = Process.GetCurrentProcess().MainModule?.FileName 
                        ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TeamSpeakOverlay.exe");
                    key.SetValue(AppName, $"\"{exePath}\" --minimized");
                    Logger.Info($"Set HKCU autostart registry entry: {exePath}", "Autostart");
                }
                else
                {
                    if (key.GetValue(AppName) != null)
                    {
                        key.DeleteValue(AppName, false);
                        Logger.Info("Removed HKCU autostart registry entry", "Autostart");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to set autostart registry", ex, "Autostart");
            }
        }

        public static bool IsAutostartEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
                return key?.GetValue(AppName) != null;
            }
            catch
            {
                return false;
            }
        }
    }
}

