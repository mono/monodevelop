// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Utilities
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Input;
    using Microsoft.VisualStudio.Text.Editor;

    /// <summary>
    /// The is a wrapper for a WPF ScrollBar that correctly exposes an event for when the user LeftShiftClicks on the scrollbar
    /// (which should be move to absolute position).
    /// </summary>
    internal abstract class ShiftClickScrollBarMargin : ScrollBar, IWpfTextViewMargin
    {
        // NOTE!
        // In this derived ScrollBar class, DO NOT overwrite the Style set property.  We will be calling this in the
        // constructor.

        private bool _inShiftLeftClick = false;
        private readonly string _marginName;
        private readonly Orientation _orientation;
        private bool _isDisposed = false;

        public event EventHandler<RoutedPropertyChangedEventArgs<double>> LeftShiftClick;

        public ShiftClickScrollBarMargin(Orientation orientation, string marginName)
        {
            // Make sure that this derived ScrollBar still uses the ScrollBar style.
            this.SetResourceReference(FrameworkElement.StyleProperty, typeof(ScrollBar));

            _marginName = marginName;
            _orientation = orientation;
        }

        protected override void OnValueChanged(double oldValue, double newValue)
        {
            base.OnValueChanged(oldValue, newValue);

            if (_inShiftLeftClick)
            {
                _inShiftLeftClick = false;
                EventHandler<RoutedPropertyChangedEventArgs<double>> leftShiftClick = this.LeftShiftClick;
                if (leftShiftClick != null)
                    leftShiftClick(this, new RoutedPropertyChangedEventArgs<double>(oldValue, newValue));
            }
        }

        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            try
            {
                _inShiftLeftClick = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
                base.OnPreviewMouseLeftButtonDown(e);
            }
            finally
            {
                _inShiftLeftClick = false;
            }
        }

        #region IWpfTextViewMargin Members

        public FrameworkElement VisualElement
        {
            get 
            {
                this.ThrowIfDisposed();
                return this;
            }
        }

        public double MarginSize
        {
            get
            {
                this.ThrowIfDisposed();
                return _orientation == Orientation.Vertical ?
                    this.ActualHeight :
                    this.ActualWidth;
            }
        }

        public abstract bool Enabled
        {
            get;  
        }

        public ITextViewMargin GetTextViewMargin(string marginName)
        {
            if (!_isDisposed && string.Compare(marginName, _marginName, StringComparison.OrdinalIgnoreCase) == 0)
                return this;

            return null;
        }

        public abstract void OnDispose();

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            _isDisposed = true;
            this.OnDispose();
        }

        #endregion //IWpfTextViewMargin Members

        protected void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException("ShiftClickScrollBarMargin");
        }
    }
}
