using System;
using System.Media;
using System.Threading.Tasks;
using TeamSpeakOverlay.Domain.Entities;
using TeamSpeakOverlay.Infrastructure.Logging;

namespace TeamSpeakOverlay.Application.UseCases
{
    public class PlaySoundNotificationUseCase
    {
        public void Execute(AppSettings settings)
        {
            if (settings == null || !settings.EnableSoundNotifications) return;

            Task.Run(() =>
            {
                try
                {
                    // Plays non-blocking crisp notification chime
                    SystemSounds.Asterisk.Play();
                    Logger.Info("Sound notification played", "PlaySoundNotificationUseCase");
                }
                catch (Exception ex)
                {
                    Logger.Error("Error playing sound notification", ex, "PlaySoundNotificationUseCase");
                }
            });
        }
    }
}
