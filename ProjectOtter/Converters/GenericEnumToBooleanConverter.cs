using Microsoft.UI.Xaml.Data;

namespace ProjectOtter.Converters;

public class GenericEnumToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (parameter is string enumString)
        {
            if (value == null)
                return false;

            var enumType = value.GetType();
            if (!enumType.IsEnum)
                return false;

            var enumValue = Enum.Parse(enumType, enumString);
            return enumValue.Equals(value);
        }

        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (parameter is string enumString && targetType.IsEnum)
        {
            return Enum.Parse(targetType, enumString);
        }

        throw new ArgumentException("Parameter must be a valid enum name and targetType must be an enum");
    }
}
