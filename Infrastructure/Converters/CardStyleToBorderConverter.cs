using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using TeamSpeakOverlay.Domain.Entities;
using MediaColor = System.Windows.Media.Color;
using MediaColorConverter = System.Windows.Media.ColorConverter;

namespace TeamSpeakOverlay.Infrastructure.Converters
{
    public class CardStyleToBorderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isThickness = parameter?.ToString() == "Thickness";
            if (value is OverlayCardStyle style)
            {
                if (isThickness)
                {
                    return style switch
                    {
                        OverlayCardStyle.ModernGlass => new Thickness(1.5),
                        OverlayCardStyle.Solid => new Thickness(1.0),
                        OverlayCardStyle.MinimalBorderless => new Thickness(0),
                        _ => new Thickness(1.5)
                    };
                }
                else
                {
                    return style switch
                    {
                        OverlayCardStyle.ModernGlass => new SolidColorBrush((MediaColor)MediaColorConverter.ConvertFromString("#35FFFFFF")),
                        OverlayCardStyle.Solid => new SolidColorBrush((MediaColor)MediaColorConverter.ConvertFromString("#55555555")),
                        OverlayCardStyle.MinimalBorderless => System.Windows.Media.Brushes.Transparent,
                        _ => new SolidColorBrush((MediaColor)MediaColorConverter.ConvertFromString("#35FFFFFF"))
                    };
                }
            }
            return isThickness ? (object)new Thickness(1.5) : new SolidColorBrush((MediaColor)MediaColorConverter.ConvertFromString("#35FFFFFF"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
