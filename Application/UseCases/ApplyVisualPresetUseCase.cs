using System;
using TeamSpeakOverlay.Domain.Entities;
using TeamSpeakOverlay.Domain.Interfaces;

namespace TeamSpeakOverlay.Application.UseCases
{
    public class ApplyVisualPresetUseCase
    {
        private readonly ISettingsRepository _settingsRepository;

        public ApplyVisualPresetUseCase(ISettingsRepository settingsRepository)
        {
            _settingsRepository = settingsRepository;
        }

        public void ApplyPreset(VisualPreset preset)
        {
            var s = _settingsRepository.Settings;
            s.CurrentPreset = preset;

            switch (preset)
            {
                case VisualPreset.FpsMinimalist:
                    s.DisplayMode = OverlayDisplayMode.OnlySpeaking;
                    s.SortOrder = OverlaySortOrder.SpeakersFirst;
                    s.CardStyle = OverlayCardStyle.MinimalBorderless;
                    s.ShowHeader = false;
                    s.OverlayScale = 0.85;
                    s.OverlayWidth = 220;
                    s.TalkingAccentColor = SpeechAccentColor.NeonGreen;
                    break;

                case VisualPreset.MmoRaid:
                    s.DisplayMode = OverlayDisplayMode.ShowAll;
                    s.SortOrder = OverlaySortOrder.SpeakersFirst;
                    s.CardStyle = OverlayCardStyle.ModernGlass;
                    s.ShowHeader = true;
                    s.OverlayScale = 1.0;
                    s.OverlayWidth = 290;
                    s.TalkingAccentColor = SpeechAccentColor.GoldYellow;
                    break;

                case VisualPreset.StreamerPro:
                    s.DisplayMode = OverlayDisplayMode.OnlySpeaking;
                    s.SortOrder = OverlaySortOrder.Alphabetical;
                    s.CardStyle = OverlayCardStyle.ModernGlass;
                    s.ShowHeader = true;
                    s.OverlayScale = 0.9;
                    s.OverlayWidth = 250;
                    s.TalkingAccentColor = SpeechAccentColor.CyanBlue;
                    break;

                case VisualPreset.Custom:
                default:
                    break;
            }

            _settingsRepository.Save();
        }
    }
}
