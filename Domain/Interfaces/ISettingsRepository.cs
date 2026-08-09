using TeamSpeakOverlay.Domain.Entities;

namespace TeamSpeakOverlay.Domain.Interfaces
{
    public interface ISettingsRepository
    {
        AppSettings Settings { get; }
        void Save();
    }
}
