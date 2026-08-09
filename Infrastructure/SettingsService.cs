using System;
using System.IO;
using System.Text.Json;
using TeamSpeakOverlay.Domain.Entities;
using TeamSpeakOverlay.Domain.Interfaces;
using TeamSpeakOverlay.Infrastructure.Win32;

namespace TeamSpeakOverlay.Infrastructure
{
    public class SettingsService : ISettingsRepository
    {
        private readonly string _settingsPath;
        public AppSettings Settings { get; private set; }

        public SettingsService()
        {
            Settings = AppSettings.Load();
            if (!Settings.TargetProcesses.Contains("lu4.bin", StringComparer.OrdinalIgnoreCase))
            {
                Settings.TargetProcesses.Add("lu4.bin");
            }
            if (!Settings.TargetProcesses.Contains("lu4", StringComparer.OrdinalIgnoreCase))
            {
                Settings.TargetProcesses.Add("lu4");
            }
            Save();

            // Sync registry with settings
            if (Settings.AutostartWithWindows != RegistryAutostartManager.IsAutostartEnabled())
            {
                RegistryAutostartManager.SetAutostart(Settings.AutostartWithWindows);
            }
        }

        public void Save()
        {
            Settings.Save();
        }

        public void SetAutostart(bool enable)
        {
            Settings.AutostartWithWindows = enable;
            RegistryAutostartManager.SetAutostart(enable);
            Save();
        }

        public bool IsAutostartEnabled()
        {
            return RegistryAutostartManager.IsAutostartEnabled();
        }
    }
}


