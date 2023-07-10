using Common.Entities;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;

namespace Mediator.Converters;

public class ConnectionStatusConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is ConnectionStatus connectionStatus)
        {
            return connectionStatus switch
            {
                ConnectionStatus.Disconnected => new SolidColorBrush(Colors.PaleVioletRed),
                ConnectionStatus.Connecting => new SolidColorBrush(Colors.PaleGoldenrod),
                ConnectionStatus.Connected => new SolidColorBrush(Colors.PaleGreen),
                ConnectionStatus.Disconnecting => new SolidColorBrush(Colors.PaleTurquoise),
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