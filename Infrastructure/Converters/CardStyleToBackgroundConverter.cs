using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using TeamSpeakOverlay.Domain.Entities;
using MediaColor = System.Windows.Media.Color;
using MediaColorConverter = System.Windows.Media.ColorConverter;

namespace TeamSpeakOverlay.Infrastructure.Converters
{
    public class CardStyleToBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is OverlayCardStyle style)
            {
                return style switch
                {
                    OverlayCardStyle.ModernGlass => new SolidColorBrush((MediaColor)MediaColorConverter.ConvertFromString("#D018181C")),
                    OverlayCardStyle.Solid => new SolidColorBrush((MediaColor)MediaColorConverter.ConvertFromString("#FE18181C")),
                    OverlayCardStyle.MinimalBorderless => new SolidColorBrush((MediaColor)MediaColorConverter.ConvertFromString("#00000000")),
                    _ => new SolidColorBrush((MediaColor)MediaColorConverter.ConvertFromString("#D018181C"))
                };
            }
            return new SolidColorBrush((MediaColor)MediaColorConverter.ConvertFromString("#D018181C"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
