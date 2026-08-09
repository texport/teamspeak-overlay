using System;

namespace TeamSpeakOverlay.Domain.Entities
{
    public class GitHubReleaseInfo
    {
        public string TagName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string HtmlUrl { get; set; } = string.Empty;
        public string SetupAssetUrl { get; set; } = string.Empty;
        public bool IsPreRelease { get; set; }
        public DateTime PublishedAt { get; set; }
        public bool IsNewerThanCurrent { get; set; }
    }
}
