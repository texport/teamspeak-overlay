using System;
using System.Globalization;
using System.Windows.Data;

namespace TeamSpeakOverlay.Infrastructure.Converters
{
    public class ToastIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string iconType = value as string ?? "Poke";
            return iconType switch
            {
                "Poke" => "👉",
                "Message" => "💬",
                "Hotkey" or "Info" => "⚡",
                "Warning" => "⚠️",
                _ => "🔔"
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
