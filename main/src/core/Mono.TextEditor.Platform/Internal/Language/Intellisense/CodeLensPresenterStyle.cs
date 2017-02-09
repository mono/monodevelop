using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    ///<summary>
    /// Defines a set of properties that will be used to style the default CodeLens presenter.
    ///</summary>
    /// <remarks>
    /// This is a MEF component part, and should be exported with the following attributes:
    /// [Export(typeof(CodeLensPresenterStyle))]
    /// [Name]
    /// [Order]
    /// Only one CodeLensPresenterStyle should be exported.
    /// </remarks>
    public class CodeLensPresenterStyle : INotifyPropertyChanged
    {
        private TextRunProperties _indicatorTextRunProperties;
        private TextRunProperties _indicatorDisabledTextRunProperties;
        private TextRunProperties _indicatorHoveredTextRunProperties;
        private TextRunProperties _indicatorSelectedTextRunProperties;
        private Brush _indicatorSeparatorBrush;
        private Color _popupBackgroundColor;
        private Brush _popupBackgroundBrush;
        private Brush _popupTextBrush;
        private Brush _popupBorderBrush;
        private Brush _popupPinButtonHoverBackgroundBrush;
        private Brush _popupPinButtonHoverForegroundBrush;
        private Brush _popupPinButtonMouseDownBackgroundBrush;
        private Brush _popupPinButtonMouseDownForegroundBrush;
        private Brush _accessKeyDisabledBackgroundBrush;
        private Brush _accessKeyDisabledTextBrush;
        private Brush _accessKeyDisabledBorderBrush;
        private Brush _accessKeyBackgroundBrush;
        private Brush _accessKeyTextBrush;
        private Brush _accessKeyBorderBrush;
        private bool? _areGradientsAllowed;
        private bool? _areAnimationsAllowed;
        private Color _dropShadowColor;

        /// <summary>
        /// Event raised when a property on this object's value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets a <see cref="TextRunProperties"/> that will be used to format the text of the individual indicators.
        /// </summary>
        public virtual TextRunProperties IndicatorTextRunProperties
        {
            get { return _indicatorTextRunProperties; }
            set { SetProperty(ref _indicatorTextRunProperties, value); }
        }

        /// <summary>
        /// Gets a <see cref="TextRunProperties"/> that will be used to format the text of the individual indicators when the mouse is over them.
        /// </summary>
        public virtual TextRunProperties IndicatorHoveredTextRunProperties
        {
            get { return _indicatorHoveredTextRunProperties; }
            set { SetProperty(ref _indicatorHoveredTextRunProperties, value); }
        }

        /// <summary>
        /// Gets a <see cref="TextRunProperties"/> that will be used to format the text of the individual indicators when the indicator is disabled.
        /// </summary>
        public virtual TextRunProperties IndicatorDisabledTextRunProperties
        {
            get { return _indicatorDisabledTextRunProperties; }
            set { SetProperty(ref _indicatorDisabledTextRunProperties, value); }
        }

        /// <summary>
        /// Gets a <see cref="TextRunProperties"/> that will be used to format the text of the individual indicators when the indicator is selected (and the popup is open).
        /// </summary>
        public virtual TextRunProperties IndicatorSelectedTextRunProperties
        {
            get { return _indicatorSelectedTextRunProperties; }
            set { SetProperty(ref _indicatorSelectedTextRunProperties, value); }
        }

        /// <summary>
        /// Gets a <see cref="Brush"/> that will be used to paint the separators in between indicators.
        /// </summary>
        public virtual Brush IndicatorSeparatorBrush
        {
            get { return _indicatorSeparatorBrush; }
            set { SetProperty(ref _indicatorSeparatorBrush, value); }
        }

        /// <summary>
        /// Gets a <see cref="Color"/> that will be used to paint the background of the details popup.
        /// </summary>
        public virtual Color PopupBackgroundColor
        {
            get { return _popupBackgroundColor; }
            set { SetProperty(ref _popupBackgroundColor, value); }
        }

        /// <summary>
        /// Gets a <see cref="Brush"/> that will be used to paint the background of the details popup.
        /// </summary>
        public virtual Brush PopupBackgroundBrush
        {
            get { return _popupBackgroundBrush; }
            set { SetProperty(ref _popupBackgroundBrush, value); }
        }

        /// <summary>
        /// Gets a <see cref="Brush"/> that will be used to draw text by default in the details popup.
        /// </summary>
        public virtual Brush PopupTextBrush
        {
            get { return _popupTextBrush; }
            set { SetProperty(ref _popupTextBrush, value); }
        }

        /// <summary>
        /// Gets a <see cref="Brush"/> that will be used to paint the border for the details popup.
        /// </summary>
        public virtual Brush PopupBorderBrush
        {
            get { return _popupBorderBrush; }
            set { SetProperty(ref _popupBorderBrush, value); }
        }

        /// <summary>
        /// Gets a <see cref="Brush"/> that will be used for the background of the pin button on mouse hover.
        /// </summary>
        public virtual Brush PopupPinButtonHoverBackgroundBrush
        {
            get { return _popupPinButtonHoverBackgroundBrush; }
            set { SetProperty(ref _popupPinButtonHoverBackgroundBrush, value); }
        }

        /// <summary>
        /// Gets a <see cref="Brush"/> that will be used for the foreground of the pin button on mouse hover.
        /// </summary>
        public virtual Brush PopupPinButtonHoverForegroundBrush
        {
            get { return _popupPinButtonHoverForegroundBrush; }
            set { SetProperty(ref _popupPinButtonHoverForegroundBrush, value); }
        }

        /// <summary>
        /// Gets a <see cref="Brush"/> that will be used for the background of the pin button on mouse down.
        /// </summary>
        public virtual Brush PopupPinButtonMouseDownBackgroundBrush
        {
            get { return _popupPinButtonMouseDownBackgroundBrush; }
            set { SetProperty(ref _popupPinButtonMouseDownBackgroundBrush, value); }
        }

        /// <summary>
        /// Gets a <see cref="Brush"/> that will be used for the foreground of the pin button on mouse down.
        /// </summary>
        public virtual Brush PopupPinButtonMouseDownForegroundBrush
        {
            get { return _popupPinButtonMouseDownForegroundBrush; }
            set { SetProperty(ref _popupPinButtonMouseDownForegroundBrush, value); }
        }

        /// <summary>
        /// Gets a value determining whether or not gradients should be used in the details popup
        /// </summary>
        public virtual bool? AreGradientsAllowed
        {
            get { return _areGradientsAllowed; }
            set { SetProperty(ref _areGradientsAllowed, value); }
        }

        /// <summary>
        /// Gets a value determining whether or not gradients should be used in the details popup
        /// </summary>
        public virtual bool? AreAnimationsAllowed
        {
            get { return _areAnimationsAllowed; }
            set { SetProperty(ref _areAnimationsAllowed, value); }
        }

        /// <summary>
        /// Gets a <see cref="Color"/> that will be used for the drop shadow of the popup
        /// </summary>
        public virtual Color DropShadowColor
        {
            get { return _dropShadowColor; }
            set { SetProperty(ref _dropShadowColor, value); }
        }

        /// <summary>
        /// Gets a <see cref="Brush"/> that will be used to paint the background for the access key popup.
        /// </summary>
        public virtual Brush AccessKeyBackgroundBrush
        {
            get { return _accessKeyBackgroundBrush; }
            set { SetProperty(ref _accessKeyBackgroundBrush, value); }
        }

        /// <summary>
        /// Gets a <see cref="Brush"/> that will be used to paint the border for the access key popup.
        /// </summary>
        public virtual Brush AccessKeyBorderBrush
        {
            get { return _accessKeyBorderBrush; }
            set { SetProperty(ref _accessKeyBorderBrush, value); }
        }

        /// <summary>
        /// Gets a <see cref="Brush"/> that will be used to paint the text for the access key popup.
        /// </summary>
        public virtual Brush AccessKeyTextBrush
        {
            get { return _accessKeyTextBrush; }
            set { SetProperty(ref _accessKeyTextBrush, value); }
        }

        /// <summary>
        /// Gets a <see cref="Brush"/> that will be used to paint the background for the disabled access key popup.
        /// </summary>
        public virtual Brush AccessKeyDisabledBackgroundBrush
        {
            get { return _accessKeyDisabledBackgroundBrush; }
            set { SetProperty(ref _accessKeyDisabledBackgroundBrush, value); }
        }

        /// <summary>
        /// Gets a <see cref="Brush"/> that will be used to paint the border for the disabled access key popup.
        /// </summary>
        public virtual Brush AccessKeyDisabledBorderBrush
        {
            get { return _accessKeyDisabledBorderBrush; }
            set { SetProperty(ref _accessKeyDisabledBorderBrush, value); }
        }

        /// <summary>
        /// Gets a <see cref="Brush"/> that will be used to paint the text for the disabled access key popup.
        /// </summary>
        public virtual Brush AccessKeyDisabledTextBrush
        {
            get { return _accessKeyDisabledTextBrush; }
            set { SetProperty(ref _accessKeyDisabledTextBrush, value); }
        }

        /// <summary>
        /// Gets a <see cref="TextRunProperties"/> that will be used to format the text of the individual indicators when the mouse is over them.
        /// </summary>
        public virtual TextRunProperties ToolTipTextRunProperties
        {
            get { return this.IndicatorTextRunProperties; }
        }

        /// <summary>
        /// Gets a <see cref="Brush"/> that will be used for the background of tooltips.
        /// </summary>
        public virtual Brush ToolTipBackgroundBrush
        {
            get { return SystemColors.InfoBrush; }
        }

        /// <summary>
        /// Gets a <see cref="Brush"/> that will be used for the border of tooltips.
        /// </summary>
        public virtual Brush ToolTipBorderBrush
        {
            get { return SystemColors.InfoBrush; }
        }

        private void SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (!object.Equals(field, value))
            {
                field = value;

                if (propertyName != null)
                {
                    NotifyPropertyChanged(propertyName);
                }
            }
        }

        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
