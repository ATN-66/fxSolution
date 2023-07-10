using Common.Entities;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;

namespace Mediator.Converters;

public class ServiceStatusConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is ServiceStatus connectionStatus)
        {
            return connectionStatus switch
            {
                ServiceStatus.Off => new SolidColorBrush(Colors.PaleVioletRed),
                ServiceStatus.Fault => new SolidColorBrush(Colors.Yellow),
                ServiceStatus.On => new SolidColorBrush(Colors.PaleGreen),
                _ => throw new ArgumentOutOfRangeException(nameof(value), @"Value does not match any defined cases.")
            };
        }

        throw new InvalidOperationException("Must be a ConnectionStatus value.");
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new InvalidOperationException("Back conversion is not supported by this converter.");
    }
}