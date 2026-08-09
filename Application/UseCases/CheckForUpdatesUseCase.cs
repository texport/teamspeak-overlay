using System.Threading.Tasks;
using TeamSpeakOverlay.Domain.Entities;
using TeamSpeakOverlay.Infrastructure.Update;

namespace TeamSpeakOverlay.Application.UseCases
{
    public class CheckForUpdatesUseCase
    {
        private readonly AutoUpdateService _updateService;

        public CheckForUpdatesUseCase(AutoUpdateService? updateService = null)
        {
            _updateService = updateService ?? new AutoUpdateService();
        }

        public async Task<GitHubReleaseInfo?> ExecuteCheckAsync()
        {
            return await _updateService.CheckForLatestReleaseAsync();
        }

        public async Task<bool> ExecuteDownloadAndUpdateAsync(string assetUrl)
        {
            return await _updateService.DownloadAndInstallUpdateAsync(assetUrl);
        }
    }
}
