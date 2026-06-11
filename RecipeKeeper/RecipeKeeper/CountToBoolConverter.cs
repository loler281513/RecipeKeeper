using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace RecipeKeeper
{
    public class CountToBoolConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                // Кнопка активна, если количество фото равно 0
                return count == 0;
            }
            return true;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}