using System;
using System.Reflection;

namespace TeamSpeakOverlay.Domain.Entities
{
    public static class AppVersion
    {
        private static readonly Version _asmVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 1, 1);

        public static string Version => $"{_asmVersion.Major}.{_asmVersion.Minor}.{_asmVersion.Build}";
        public static string DisplayVersion => $"v{Version}";
        public static string FullName => $"TeamSpeak Overlay Pro {DisplayVersion}";
        public const string ReleaseDate = "2026-08-09";
        public const string Author = "@SergeyIvanovPro";
    }
}
