using System;
using System.Diagnostics;
using TeamSpeakOverlay.Infrastructure.Logging;

namespace TeamSpeakOverlay.Application.UseCases
{
    public class OpenTelegramLinkUseCase
    {
        public void Execute(string handle)
        {
            try
            {
                string cleanHandle = handle.Replace("@", "").Trim();
                if (string.IsNullOrEmpty(cleanHandle)) cleanHandle = "SergeyIvanovPro";

                string url = $"https://t.me/{cleanHandle}";
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
                Logger.Info($"Opened Telegram link: {url}", "OpenTelegramLinkUseCase");
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to open Telegram link for handle: {handle}", ex, "OpenTelegramLinkUseCase");
            }
        }
    }
}
