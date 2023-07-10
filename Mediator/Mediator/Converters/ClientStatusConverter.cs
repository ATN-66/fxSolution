using Common.Entities;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;

namespace Mediator.Converters;

public class ClientStatusConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is ClientStatus connectionStatus)
        {
            return connectionStatus switch
            {
                ClientStatus.Off => new SolidColorBrush(Colors.Red),
                ClientStatus.On => new SolidColorBrush(Colors.Green),
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