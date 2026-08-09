using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using TeamSpeakOverlay.Domain.Entities;
using TeamSpeakOverlay.Infrastructure.Logging;

namespace TeamSpeakOverlay.Infrastructure.Update
{
    public class AutoUpdateService
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private const string RepositoryApiUrl = "https://api.github.com/repos/texport/teamspeak-overlay/releases/latest";

        static AutoUpdateService()
        {
            _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("TeamSpeakOverlay", "1.1.0"));
            _httpClient.Timeout = TimeSpan.FromSeconds(15);
        }

        public async Task<GitHubReleaseInfo?> CheckForLatestReleaseAsync()
        {
            try
            {
                Logger.Info("Checking for latest release from GitHub API...", "AutoUpdateService");
                var response = await _httpClient.GetAsync(RepositoryApiUrl);
                if (!response.IsSuccessStatusCode)
                {
                    Logger.Warn($"GitHub API returned status {response.StatusCode}", "AutoUpdateService");
                    return null;
                }

                string json = await response.Content.ReadAsStringAsync();
                using JsonDocument doc = JsonDocument.Parse(json);
                JsonElement root = doc.RootElement;

                string tagName = root.TryGetProperty("tag_name", out var tagProp) ? tagProp.GetString() ?? "" : "";
                string name = root.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? "" : "";
                string body = root.TryGetProperty("body", out var bodyProp) ? bodyProp.GetString() ?? "" : "";
                string htmlUrl = root.TryGetProperty("html_url", out var htmlProp) ? htmlProp.GetString() ?? "" : "";
                bool isPreRelease = root.TryGetProperty("prerelease", out var preProp) && preProp.GetBoolean();

                string setupAssetUrl = string.Empty;
                if (root.TryGetProperty("assets", out var assetsProp) && assetsProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var asset in assetsProp.EnumerateArray())
                    {
                        string downloadUrl = asset.TryGetProperty("browser_download_url", out var urlProp) ? urlProp.GetString() ?? "" : "";
                        if (downloadUrl.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                        {
                            setupAssetUrl = downloadUrl;
                            break;
                        }
                    }
                }

                bool isNewer = IsVersionNewer(tagName, AppVersion.DisplayVersion);

                Logger.Info($"GitHub latest tag: '{tagName}', current: '{AppVersion.DisplayVersion}', isNewer: {isNewer}", "AutoUpdateService");

                return new GitHubReleaseInfo
                {
                    TagName = tagName,
                    Name = name,
                    Body = body,
                    HtmlUrl = htmlUrl,
                    SetupAssetUrl = setupAssetUrl,
                    IsPreRelease = isPreRelease,
                    PublishedAt = DateTime.UtcNow,
                    IsNewerThanCurrent = isNewer
                };
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to check for updates from GitHub", ex, "AutoUpdateService");
                return null;
            }
        }

        public async Task<bool> DownloadAndInstallUpdateAsync(string assetUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(assetUrl)) return false;

                Logger.Info($"Downloading update installer from {assetUrl}...", "AutoUpdateService");
                string tempSetupPath = Path.Combine(Path.GetTempPath(), "TeamSpeakOverlay-Setup-Update.exe");

                byte[] data = await _httpClient.GetByteArrayAsync(assetUrl);
                await File.WriteAllBytesAsync(tempSetupPath, data);

                Logger.Info($"Update installer downloaded to {tempSetupPath}. Launching installer...", "AutoUpdateService");

                Process.Start(new ProcessStartInfo
                {
                    FileName = tempSetupPath,
                    UseShellExecute = true
                });

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to download or launch update installer", ex, "AutoUpdateService");
                return false;
            }
        }

        private static bool IsVersionNewer(string latestTag, string currentDisplayVersion)
        {
            if (string.IsNullOrWhiteSpace(latestTag)) return false;

            string CleanVer(string v) => v.TrimStart('v', 'V').Split('-')[0];

            if (Version.TryParse(CleanVer(latestTag), out var latest) &&
                Version.TryParse(CleanVer(currentDisplayVersion), out var current))
            {
                return latest > current;
            }

            return !latestTag.Equals(currentDisplayVersion, StringComparison.OrdinalIgnoreCase);
        }
    }
}
