using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using TeamSpeakOverlay.Domain.Entities;
using MediaColor = System.Windows.Media.Color;
using MediaColorConverter = System.Windows.Media.ColorConverter;

namespace TeamSpeakOverlay.Infrastructure.Converters
{
    public class TalkingClientStyleConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool isTalking = values.Length > 0 && values[0] is bool b && b;
            SpeechAccentColor accent = values.Length > 1 && values[1] is SpeechAccentColor a ? a : SpeechAccentColor.NeonGreen;
            bool isWhisper = values.Length > 2 && values[2] is bool w && w;
            WhisperAccentColor whisperAccent = values.Length > 3 && values[3] is WhisperAccentColor wa ? wa : WhisperAccentColor.GoldAmber;
            NicknameTextColor nickColor = values.Length > 4 && values[4] is NicknameTextColor nc ? nc : NicknameTextColor.White;

            string target = parameter?.ToString() ?? "Background";

            if (!isTalking)
            {
                if (target == "Background") return new SolidColorBrush((MediaColor)MediaColorConverter.ConvertFromString("#15FFFFFF"));
                if (target == "Foreground")
                {
                    string nickHex = nickColor switch
                    {
                        NicknameTextColor.White => "#FFFFFF",
                        NicknameTextColor.LightGray => "#D1D5DB",
                        NicknameTextColor.NeonGreen => "#00E676",
                        NicknameTextColor.CyanBlue => "#00E5FF",
                        NicknameTextColor.GoldYellow => "#FFD700",
                        NicknameTextColor.HotPink => "#FF4081",
                        NicknameTextColor.SoftAmber => "#FFB74D",
                        _ => "#FFFFFF"
                    };
                    return new SolidColorBrush((MediaColor)MediaColorConverter.ConvertFromString(nickHex));
                }
                if (target == "Dot") return new SolidColorBrush((MediaColor)MediaColorConverter.ConvertFromString("#35FFFFFF"));
                if (target == "BorderBrush") return new SolidColorBrush((MediaColor)MediaColorConverter.ConvertFromString("#1AFFFFFF"));
                if (target == "BorderThickness") return new System.Windows.Thickness(1.0);
                if (target == "FontWeight") return System.Windows.FontWeights.SemiBold;
            }

            if (isWhisper)
            {
                string whisperHex = whisperAccent switch
                {
                    WhisperAccentColor.GoldAmber => "#FFAB40",
                    WhisperAccentColor.NeonRed => "#FF1744",
                    WhisperAccentColor.ElectricOrange => "#FF6D00",
                    WhisperAccentColor.HotPurple => "#D500F9",
                    WhisperAccentColor.CyanBlue => "#00E5FF",
                    _ => "#FFAB40"
                };
                MediaColor whisperColor = (MediaColor)MediaColorConverter.ConvertFromString(whisperHex);

                if (target == "Background")
                {
                    MediaColor bg = MediaColor.FromArgb(0x55, whisperColor.R, whisperColor.G, whisperColor.B);
                    return new SolidColorBrush(bg);
                }
                if (target == "BorderBrush") return new SolidColorBrush(whisperColor);
                if (target == "BorderThickness") return new System.Windows.Thickness(1.8);
                if (target == "FontWeight") return System.Windows.FontWeights.Bold;
                return new SolidColorBrush(whisperColor);
            }

            SpeakingNicknameColorMode nickMode = values.Length > 5 && values[5] is SpeakingNicknameColorMode nm ? nm : SpeakingNicknameColorMode.UseAccentColor;

            string colorHex = accent switch
            {
                SpeechAccentColor.NeonGreen => "#00E676",
                SpeechAccentColor.CyanBlue => "#00E5FF",
                SpeechAccentColor.GoldYellow => "#FFD600",
                SpeechAccentColor.HotPink => "#FF4081",
                SpeechAccentColor.Purple => "#B388FF",
                SpeechAccentColor.ElectricOrange => "#FF6D00",
                SpeechAccentColor.LimeYellow => "#AEEA00",
                SpeechAccentColor.DeepSkyBlue => "#00B0FF",
                SpeechAccentColor.WhiteGlow => "#FFFFFF",
                _ => "#00E676"
            };

            MediaColor mainColor = (MediaColor)MediaColorConverter.ConvertFromString(colorHex);

            if (target == "Background")
            {
                MediaColor bg = MediaColor.FromArgb(0x55, mainColor.R, mainColor.G, mainColor.B);
                return new SolidColorBrush(bg);
            }
            if (target == "BorderBrush") return new SolidColorBrush(mainColor);
            if (target == "BorderThickness") return new System.Windows.Thickness(1.8);
            if (target == "FontWeight") return System.Windows.FontWeights.Bold;

            if (target == "Foreground")
            {
                if (nickMode == SpeakingNicknameColorMode.KeepNormalColor)
                {
                    string normalHex = nickColor switch
                    {
                        NicknameTextColor.White => "#FFFFFF",
                        NicknameTextColor.LightGray => "#D1D5DB",
                        NicknameTextColor.NeonGreen => "#00E676",
                        NicknameTextColor.CyanBlue => "#00E5FF",
                        NicknameTextColor.GoldYellow => "#FFD700",
                        NicknameTextColor.HotPink => "#FF4081",
                        NicknameTextColor.SoftAmber => "#FFB74D",
                        _ => "#FFFFFF"
                    };
                    return new SolidColorBrush((MediaColor)MediaColorConverter.ConvertFromString(normalHex));
                }

                if (nickMode == SpeakingNicknameColorMode.PureWhite)
                {
                    return new SolidColorBrush(Colors.White);
                }

                return new SolidColorBrush(mainColor);
            }

            return new SolidColorBrush(mainColor);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
