using System;
using TeamSpeakOverlay.Domain.Entities;
using TeamSpeakOverlay.Domain.Interfaces;
using TeamSpeakOverlay.Infrastructure.Localization;

namespace TeamSpeakOverlay.Application.UseCases
{
    public class UpdateLanguageUseCase
    {
        private readonly ISettingsRepository _settingsRepository;

        public UpdateLanguageUseCase(ISettingsRepository settingsRepository)
        {
            _settingsRepository = settingsRepository;
        }

        public void UpdateLanguage(AppLanguage language)
        {
            _settingsRepository.Settings.Language = language;
            _settingsRepository.Save();
            LocalizationManager.ApplyLanguage(language);
        }
    }
}
