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
    /// Represents zoom control in the text view.
    /// </summary>
    public abstract class ZoomControl : ComboBox
    {
        static ZoomControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ZoomControl), new FrameworkPropertyMetadata(typeof(ZoomControl)));
        }

        #region bool SelectedZoomLevel dependency property

        /// <summary>
        /// A <see cref="DependencyProperty"/> that determines the selected zoom level property of the control
        /// </summary>
        public static readonly DependencyProperty SelectedZoomLevelProperty = DependencyProperty.RegisterAttached("SelectedZoomLevel", typeof(double), typeof(ZoomControl));

        /// <summary>
        /// Sets <see cref="SelectedZoomLevelProperty"/>.
        /// </summary>
        public static void SetSelectedZoomLevel(DependencyObject control, double value)
        {
            control.SetValue(SelectedZoomLevelProperty, value);
        }

        /// <summary>
        /// Gets <see cref="SelectedZoomLevelProperty"/>.
        /// </summary>
        public static double GetSelectedZoomLevel(DependencyObject control)
        {
            return (double)control.GetValue(SelectedZoomLevelProperty);
        }

        /// <summary>
        /// Gets or Sets <see cref="SelectedZoomLevelProperty"/>.
        /// </summary>
        public double SelectedZoomLevel
        {
            get
            {
                return GetSelectedZoomLevel(this);
            }
            set
            {
                SetSelectedZoomLevel(this, value);
            }
        }

        #endregion
    }
}
