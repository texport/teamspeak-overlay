using System;
using System.Windows;
using TeamSpeakOverlay.Domain.Entities;

namespace TeamSpeakOverlay.Infrastructure
{
    public static class ThemeManager
    {
        public static void ApplyTheme(AppTheme theme)
        {
            var app = System.Windows.Application.Current;
            if (app == null) return;

            // Find and remove the existing theme dictionary (Dark or Light ONLY)
            ResourceDictionary? existingTheme = null;
            foreach (var dict in app.Resources.MergedDictionaries)
            {
                if (dict.Source != null && (dict.Source.OriginalString.Contains("Material3Dark.xaml") || dict.Source.OriginalString.Contains("Material3Light.xaml")))
                {
                    existingTheme = dict;
                    break;
                }
            }

            if (existingTheme != null)
            {
                app.Resources.MergedDictionaries.Remove(existingTheme);
            }

            AppTheme effectiveTheme = theme;
            if (theme == AppTheme.System)
            {
                effectiveTheme = GetSystemTheme();
            }

            // Load the new theme dictionary
            string themeFileName = effectiveTheme == AppTheme.Light ? "Material3Light.xaml" : "Material3Dark.xaml";
            var newTheme = new ResourceDictionary
            {
                Source = new Uri($"pack://application:,,,/Themes/{themeFileName}", UriKind.Absolute)
            };

            app.Resources.MergedDictionaries.Add(newTheme);
        }

        private static AppTheme GetSystemTheme()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                if (key?.GetValue("AppsUseLightTheme") is int useLightTheme)
                {
                    return useLightTheme == 1 ? AppTheme.Light : AppTheme.Dark;
                }
            }
            catch (Exception ex)
            {
                TeamSpeakOverlay.Infrastructure.Logging.Logger.Error("Failed to read system theme from registry", ex, "ThemeManager");
            }
            
            return AppTheme.Dark; // Default fallback
        }
    }
}



