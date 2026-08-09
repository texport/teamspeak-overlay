using TeamSpeakOverlay.Domain.Entities;
using TeamSpeakOverlay.Domain.Interfaces;
using System;

namespace TeamSpeakOverlay.Application.UseCases
{
    public class UpdateSettingsUseCase
    {
        private readonly ISettingsRepository _settingsRepository;

        public UpdateSettingsUseCase(ISettingsRepository settingsRepository)
        {
            _settingsRepository = settingsRepository;
        }

        public AppSettings GetSettings()
        {
            return _settingsRepository.Settings;
        }

        public void SaveSettings()
        {
            _settingsRepository.Save();
        }

        public void UpdateOpacity(double opacity)
        {
            _settingsRepository.Settings.OverlayOpacity = opacity;
            SaveSettings();
        }

        public void UpdatePosition(OverlayPosition position)
        {
            _settingsRepository.Settings.Position = position;
            SaveSettings();
        }

        public void UpdateScale(double scale)
        {
            _settingsRepository.Settings.OverlayScale = scale;
            SaveSettings();
        }

        public void UpdateTheme(AppTheme theme)
        {
            _settingsRepository.Settings.Theme = theme;
            SaveSettings();
        }

        public void UpdateTimeFormat(TimeDisplayFormat timeFormat)
        {
            _settingsRepository.Settings.TimeFormat = timeFormat;
            SaveSettings();
        }

        public void UpdateCustomPosition(double customX, double customY)
        {
            _settingsRepository.Settings.Position = OverlayPosition.Custom;
            _settingsRepository.Settings.CustomX = customX;
            _settingsRepository.Settings.CustomY = customY;
            SaveSettings();
        }

        public void UpdateVisualSettings(
            double width, int marginX, int marginY,
            OverlayDisplayMode displayMode, OverlaySortOrder sortOrder,
            OverlayCardStyle cardStyle, SpeechAccentColor accentColor,
            bool showHeader, int maxVisibleClients)
        {
            var s = _settingsRepository.Settings;
            s.OverlayWidth = width;
            s.MarginX = marginX;
            s.MarginY = marginY;
            s.DisplayMode = displayMode;
            s.SortOrder = sortOrder;
            s.CardStyle = cardStyle;
            s.TalkingAccentColor = accentColor;
            s.ShowHeader = showHeader;
            s.MaxVisibleClients = maxVisibleClients;
            SaveSettings();
        }
    }
}
