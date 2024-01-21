using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace ProjectOtter.Converters;
internal class EmptyCollectionToBool : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (parameter is "true")
        {
            if (value is not 0)
                return true;

            return false;
        }

        if (value is 0)
            return true;

        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
