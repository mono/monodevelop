// Copyright (c) Microsoft Corporation
// All rights reserved

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Globalization;

namespace Microsoft.VisualStudio.Text.Editor
{
    /// <summary>
    /// Value converter that allows convertion between <see cref="String"/> and <see cref="Double"/> representations 
    /// of the zoom level.
    /// </summary>
    [ValueConversion(typeof(Double), typeof(String))]
    public sealed class ZoomLevelConverter : IValueConverter
    {
        /// <summary>
        /// Converts the value from <see cref="Double"/> to its <see cref="String"/> representation.
        /// </summary>
        /// <param name="value">The zoom level as <see cref="Double"/></param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>The <see cref="String"/> representation of the zoom level. Returns <see cref="DependencyProperty.UnsetValue"/> if the conversion fails.</returns>
        /// <remarks>
        /// The zoom level is represented as a number %. This converter takes in a double value and formats it with a % symbol.
        /// </remarks>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (culture == null)
            {
                throw new ArgumentNullException("culture");
            }

            if (value != null)
            {
                return String.Format(culture, "{0:F0} " + culture.NumberFormat.PercentSymbol, (double)value);
            }

            return DependencyProperty.UnsetValue;
        }

        /// <summary>
        /// Converts the value from its <see cref="String"/> representation to its <see cref="Double"/> value.
        /// </summary>
        /// <param name="value">The zoom level as <see cref="String"/>.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>The <see cref="Double"/> value of the zoom level. Returns <see cref="DependencyProperty.UnsetValue"/> if the conversion fails. </returns>
        /// <remarks>
        /// The zoom level is represented as a number %. This converter takes in the string representation and converts it 
        /// to its double value.
        /// </remarks>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (culture == null)
            {
                throw new ArgumentNullException("culture");
            }

            if (value != null)
            {
                String strValue = value.ToString();

                double resultDoubleValue;
                if (Double.TryParse(strValue.Replace(culture.NumberFormat.PercentSymbol, String.Empty).Trim(),
                    out resultDoubleValue))
                {
                    return resultDoubleValue; // zoom level
                }
            }

            return DependencyProperty.UnsetValue;
        }
    }
}