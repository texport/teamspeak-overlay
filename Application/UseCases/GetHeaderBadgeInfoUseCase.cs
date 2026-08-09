using System;
using TeamSpeakOverlay.Domain.Entities;
using TeamSpeakOverlay.Infrastructure.Logging;

namespace TeamSpeakOverlay.Application.UseCases
{
    public class HeaderBadgeInfo
    {
        public string Text { get; set; } = string.Empty;
        public string Prefix { get; set; } = string.Empty;
        public bool IsVisible { get; set; } = true;
    }

    public class GetHeaderBadgeInfoUseCase
    {
        public HeaderBadgeInfo Execute(AppSettings settings, string gameCharacterName, string rawWindowTitle)
        {
            var info = new HeaderBadgeInfo();

            if (settings.UseGameCharacterName)
            {
                info.IsVisible = true;
                string charName = gameCharacterName;
                if (string.IsNullOrWhiteSpace(charName) && !string.IsNullOrWhiteSpace(rawWindowTitle))
                {
                    charName = ExtractCharacterNameFromTitle(rawWindowTitle);
                }

                if (!string.IsNullOrWhiteSpace(charName))
                {
                    info.Text = $"🎮 {charName}";
                    info.Prefix = string.Empty;
                }
                else
                {
                    info.Text = "🎮 Персонаж L2";
                    info.Prefix = string.Empty;
                }

                Logger.Info($"[BadgeUseCase] UseGameCharacterName=True -> Text='{info.Text}', Prefix='{info.Prefix}', IsVisible={info.IsVisible}", "GetHeaderBadgeInfoUseCase");
                return info;
            }

            info.IsVisible = settings.ShowAuthorBranding;
            info.Text = settings.AuthorTelegramHandle;
            info.Prefix = "Автор: ";

            Logger.Info($"[BadgeUseCase] UseGameCharacterName=False -> IsVisible={info.IsVisible}, Text='{info.Text}'", "GetHeaderBadgeInfoUseCase");
            return info;
        }

        public static string ExtractCharacterNameFromTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title)) return string.Empty;

            var prefixes = new[] { 
                "LU4 - ", "LU4 : ", "LU4 ", "L2 - ", "L2 : ", "L2 ",
                "Lineage II - ", "Lineage 2 - ", "LineageII - ", "Lineage2 - ",
                "Lineage II : ", "Lineage 2 : ", "Lineage II ", "Lineage 2 " 
            };

            foreach (var p in prefixes)
            {
                if (title.StartsWith(p, StringComparison.OrdinalIgnoreCase))
                {
                    var candidate = title.Substring(p.Length).Trim('[', ']', ' ', '-', ':');
                    if (!string.IsNullOrWhiteSpace(candidate) && !IsClientExecutableKeyword(candidate))
                    {
                        Logger.Info($"[ExtractName] Title '{title}' matched prefix '{p}' -> Candidate '{candidate}'", "GetHeaderBadgeInfoUseCase");
                        return candidate;
                    }
                }
            }

            var parts = title.Split(new[] { '-', ':', ']', '[', '|' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 1)
            {
                var last = parts[^1].Trim();
                var first = parts[0].Trim();

                if (!IsClientExecutableKeyword(last))
                {
                    Logger.Info($"[ExtractName] Title '{title}' split -> last part '{last}'", "GetHeaderBadgeInfoUseCase");
                    return last;
                }
                if (!IsClientExecutableKeyword(first))
                {
                    Logger.Info($"[ExtractName] Title '{title}' split -> first part '{first}'", "GetHeaderBadgeInfoUseCase");
                    return first;
                }
            }

            string fallback = IsClientExecutableKeyword(title) ? string.Empty : title.Trim();
            Logger.Info($"[ExtractName] Title '{title}' fallback -> '{fallback}'", "GetHeaderBadgeInfoUseCase");
            return fallback;
        }

        private static bool IsClientExecutableKeyword(string word)
        {
            if (string.IsNullOrWhiteSpace(word)) return true;
            var w = word.Trim().ToLowerInvariant();
            return w is "lu4" or "l2" or "lineage" or "lineage2" or "lineage ii" or "lineageii" or "client" or "game";
        }
    }
}
