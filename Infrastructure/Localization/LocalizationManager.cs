using System;
using System.Linq;
using System.Windows;
using TeamSpeakOverlay.Domain.Entities;
using TeamSpeakOverlay.Infrastructure.Logging;

namespace TeamSpeakOverlay.Infrastructure.Localization
{
    public static class LocalizationManager
    {
        public static void ApplyLanguage(AppLanguage language)
        {
            try
            {
                var appResources = System.Windows.Application.Current?.Resources;
                if (appResources == null) return;

                string fileName = language switch
                {
                    AppLanguage.English => "Strings.en-US.xaml",
                    AppLanguage.Ukrainian => "Strings.uk-UA.xaml",
                    AppLanguage.Russian => "Strings.ru-RU.xaml",
                    _ => "Strings.ru-RU.xaml"
                };

                var packUri = new Uri($"pack://application:,,,/Themes/{fileName}", UriKind.Absolute);
                var newDict = new ResourceDictionary
                {
                    Source = packUri
                };

                // Find and remove any existing string dictionary
                var existingDict = appResources.MergedDictionaries.FirstOrDefault(d => 
                    d.Source != null && d.Source.OriginalString.Contains("Strings.")
                );

                if (existingDict != null)
                {
                    appResources.MergedDictionaries.Remove(existingDict);
                }

                appResources.MergedDictionaries.Add(newDict);
                Logger.Info($"Applied Language: {language} (PackUri: {packUri})", "LocalizationManager");
                LanguageChanged?.Invoke(language);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to apply language {language}", ex, "LocalizationManager");
            }
        }

        public static event Action<AppLanguage>? LanguageChanged;
    }
}
