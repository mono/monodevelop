//-----------------------------------------------------------------------
// <copyright company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Interaction logic for CodeLensDetailsControl.xaml
    /// </summary>
    public partial class CodeLensDetailsControl : UserControl
    {
        public CodeLensDetailsControl()
        {
            InitializeComponent();
        }
    }

    #region Converters
    /// <summary>
    /// Converts an error message into a visibility.  A null or empty (or all whitespace) string will be collapsed, any other value will be visible.
    /// </summary>
    public sealed class ErrorMessageVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Convert null or empty string to Visibility.Collapsed, anything else to Visibility.Visible.
        /// </summary>
        /// <param name="value">string</param>
        /// <param name="targetType">Visibility</param>
        /// <param name="parameter">null</param>
        /// <param name="culture">null</param>
        /// <returns>Visible or Collapsed</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = value as string;

            return !string.IsNullOrWhiteSpace(s) ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// not implemented
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// converter that returns Visibility.Visible if all of the other values are Visibility.Collapsed
    /// </summary>
    public sealed class OthersCollapsedVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            Visibility result = Visibility.Collapsed;
            if (values != null && values.OfType<Visibility>().All(x => x == Visibility.Collapsed))
                result = Visibility.Visible;
            return result;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    #endregion
}
