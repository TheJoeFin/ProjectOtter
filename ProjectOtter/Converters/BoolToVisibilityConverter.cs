using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace ProjectOtter.Converters;

class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool isInverse = parameter?.ToString()?.Equals("Inverse", StringComparison.OrdinalIgnoreCase) ?? false;
        bool boolValue = value is true;

        if (isInverse)
            boolValue = !boolValue;

        return boolValue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        bool isInverse = parameter?.ToString()?.Equals("Inverse", StringComparison.OrdinalIgnoreCase) ?? false;
        bool result = value is Visibility.Visible;

        if (isInverse)
            result = !result;

        return result;
    }
}
