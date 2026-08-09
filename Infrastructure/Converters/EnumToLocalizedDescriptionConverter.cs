using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TeamSpeakOverlay.Infrastructure.Converters
{
    public class EnumToLocalizedDescriptionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return string.Empty;

            Type enumType = value.GetType();
            if (!enumType.IsEnum) return value.ToString() ?? string.Empty;

            string key = $"Enum_{enumType.Name}_{value}";
            var resource = System.Windows.Application.Current?.TryFindResource(key);

            if (resource is string localizedString && !string.IsNullOrEmpty(localizedString))
            {
                return localizedString;
            }

            return value.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
