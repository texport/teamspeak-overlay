using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using TeamSpeakOverlay.Infrastructure.Logging;

namespace TeamSpeakOverlay.Domain.Entities
{
    public enum OverlayPosition
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
        CenterLeft,
        CenterRight,
        Custom
    }

    public enum OverlayDisplayMode
    {
        ShowAll,
        OnlySpeaking
    }

    public enum OverlaySortOrder
    {
        Alphabetical,
        SpeakersFirst
    }

    public enum OverlayCardStyle
    {
        ModernGlass,
        Solid,
        MinimalBorderless
    }

    public enum SpeechAccentColor
    {
        NeonGreen,
        CyanBlue,
        GoldYellow,
        HotPink,
        Purple,
        ElectricOrange,
        LimeYellow,
        DeepSkyBlue,
        WhiteGlow
    }

    public enum SpeakingNicknameColorMode
    {
        UseAccentColor,  // Подсвечивать цвет ника цветом подсветки говорящего
        KeepNormalColor, // Сохранять стандартный выбранный цвет ника
        PureWhite        // Яркий чистый белый цвет
    }

    public enum NicknameTextColor
    {
        White,
        LightGray,
        NeonGreen,
        CyanBlue,
        GoldYellow,
        HotPink,
        SoftAmber
    }

    public enum WhisperAccentColor
    {
        GoldAmber,
        NeonRed,
        ElectricOrange,
        HotPurple,
        CyanBlue
    }

    public enum VisualPreset
    {
        Custom,
        FpsMinimalist,
        MmoRaid,
        StreamerPro
    }

    public enum AppLanguage
    {
        English,
        Russian,
        Ukrainian
    }

    public enum TeamSpeakConnectionMode
    {
        Auto,
        TeamSpeak3,
        TeamSpeak6
    }

    public enum TimeDisplayFormat
    {
        Full24HourWithSeconds, // HH:mm:ss
        Short24Hour,           // HH:mm
        TwelveHour,            // hh:mm tt
        TwelveHourWithSeconds  // hh:mm:ss tt
    }

    public class AppSettings
    {
        public bool AutostartWithWindows { get; set; } = false;
        public List<string> TargetProcesses { get; set; } = new() { "lu4", "lu4.bin", "l2", "lineage2" };
        public string TS3ApiKey { get; set; } = string.Empty;
        public string TS6ApiKey { get; set; } = string.Empty;
        public TeamSpeakConnectionMode TeamSpeakMode { get; set; } = TeamSpeakConnectionMode.Auto;
        public TimeDisplayFormat TimeFormat { get; set; } = TimeDisplayFormat.Full24HourWithSeconds;
        public OverlayPosition Position { get; set; } = OverlayPosition.TopLeft;
        public int MarginX { get; set; } = 20;
        public int MarginY { get; set; } = 60;
        public double? CustomX { get; set; }
        public double? CustomY { get; set; }
        public double OverlayOpacity { get; set; } = 1.0;
        public double OverlayScale { get; set; } = 1.0;
        public double OverlayWidth { get; set; } = 280;
        public OverlayDisplayMode DisplayMode { get; set; } = OverlayDisplayMode.ShowAll;
        public OverlaySortOrder SortOrder { get; set; } = OverlaySortOrder.Alphabetical;
        public OverlayCardStyle CardStyle { get; set; } = OverlayCardStyle.ModernGlass;
        public SpeechAccentColor TalkingAccentColor { get; set; } = SpeechAccentColor.NeonGreen;
        public NicknameTextColor NicknameColor { get; set; } = NicknameTextColor.White;
        public SpeakingNicknameColorMode SpeakingNickMode { get; set; } = SpeakingNicknameColorMode.UseAccentColor;
        public WhisperAccentColor WhisperAccentColor { get; set; } = WhisperAccentColor.GoldAmber;
        public bool ShowHeader { get; set; } = true;
        public int MaxVisibleClients { get; set; } = 0; // 0 = Unlimited
        public AppTheme Theme { get; set; } = AppTheme.Dark;
        public AppLanguage Language { get; set; } = AppLanguage.Russian;

        public bool EnableHotkeys { get; set; } = true;
        public bool ShowClockInHeader { get; set; } = true;
        public bool EnablePokeNotifications { get; set; } = true;
        public bool ShowAuthorBranding { get; set; } = true;
        public bool EnableSoundNotifications { get; set; } = true;
        public bool UseGameCharacterName { get; set; } = false;
        public int SoundVolume { get; set; } = 80;
        public bool EnableClickThrough { get; set; } = false;
        public bool AlwaysShowOnTop { get; set; } = false;
        public bool EnableVoiceEqualizerAnimation { get; set; } = true;
        public string AuthorTelegramHandle => "@SergeyIvanovPro";
        public VisualPreset CurrentPreset { get; set; } = VisualPreset.Custom;

        private static readonly string SettingsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TeamSpeakOverlay"
        );

        private static readonly string SettingsFilePath = Path.Combine(SettingsDir, "config.json");

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    if (settings != null) return settings;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to load AppSettings", ex, "AppSettings");
            }

            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                if (!Directory.Exists(SettingsDir))
                {
                    Directory.CreateDirectory(SettingsDir);
                }

                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFilePath, json);
                Logger.Info("Saved AppSettings", "AppSettings");
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to save AppSettings", ex, "AppSettings");
            }
        }
    }
}


