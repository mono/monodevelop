using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    internal sealed class CodeLensIndicatorToolTipConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length != 3 || !(values[0] is string))
            {
                return DependencyProperty.UnsetValue;
            }

            return string.Format(culture, values[0].ToString(), values[1], values[2]);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
