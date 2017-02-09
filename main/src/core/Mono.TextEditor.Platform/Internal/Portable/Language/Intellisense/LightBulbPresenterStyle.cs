//
// Copyright (c) Microsoft Corporation.  All rights reserved.
//

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    ///<summary>
    /// Defines a set of properties that will be used to style the default LightBulb presenter.
    ///</summary>
    /// <remarks>
    /// This is a MEF component part, and should be exported with the following attributes:
    /// [Export(typeof(LightBulbPresenterStyle))]
    /// [ContentType]
    /// [Name]
    /// [Order]
    /// All exports of this component part should be ordered after the "default" LightBulb presenter style.  At a minimum,
    /// this means adding [Order(After="default")] to the export metadata.
    /// </remarks>
    public class LightBulbPresenterStyle : INotifyPropertyChanged
    {
        private Brush _actuatorBackgroundBrush;
        private Brush _actuatorBorderBrush;
        private Brush _actuatorHoverBackgroundBrush;
        private Brush _actuatorHoverBorderBrush;
        private Brush _actuatorDropdownChevronBrush;
        private Brush _previewBackgroundBrush;
        private Brush _previewBorderBrush;
        private Brush _previewFocusBackgroundBrush;
        private Color _discoveryModeBackgroundColor;
        private Color _discoveryModeBorderColor;
        private Brush _showQuickFixesLinkBrush;
        private Brush _showQuickFixesKeyBindingBrush;
        private string _showQuickFixesKeyBinding;
        private Brush _displayTextSuffixForegroundBrush;

        /// <summary>
        /// Event raised when a property on this object's value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets a <see cref="Brush"/> that will be used to paint the background of the LightBulb actuator.
        /// </summary>
        public virtual Brush ActuatorBackgroundBrush
        {
            get { return _actuatorBackgroundBrush; }
            set { SetProperty(ref _actuatorBackgroundBrush, value); }
        }

        /// <summary>
        /// Gets a <see cref="Brush"/> that will be used to paint the border of the LightBulb actuator.
        /// </summary>
        public virtual Brush ActuatorBorderBrush
        {
            get { return _actuatorBorderBrush; }
            set { SetProperty(ref _actuatorBorderBrush, value); }
        }

        /// <summary>
        /// Gets a <see cref="Brush"/> that will be used to paint the background of the LightBulb actuator in hover mode.
        /// </summary>
        public virtual Brush ActuatorHoverBackgroundBrush
        {
            get { return _actuatorHoverBackgroundBrush; }
            set { SetProperty(ref _actuatorHoverBackgroundBrush, value); }
        }

        /// <summary>
        /// Gets a <see cref="Brush"/> that will be used to paint the border of the LightBulb actuator.
        /// </summary>
        public virtual Brush ActuatorHoverBorderBrush
        {
            get { return _actuatorHoverBorderBrush; }
            set { SetProperty(ref _actuatorHoverBorderBrush, value); }
        }

        /// <summary>
        /// Gets a <see cref="Brush"/> that will be used to paint the chevron of the LightBulb actuator.
        /// </summary>
        public virtual Brush ActuatorDropdownChevronBrush
        {
            get { return _actuatorDropdownChevronBrush; }
            set { SetProperty(ref _actuatorDropdownChevronBrush, value); }
        }

        /// <summary>
        /// Gets a <see cref="Brush"/> that will be used to paint the background of the LightBulb preview pane.
        /// </summary>
        public virtual Brush PreviewBackgroundBrush
        {
            get { return _previewBackgroundBrush; }
            set { SetProperty(ref _previewBackgroundBrush, value); }
        }

        /// <summary>
        /// Gets a <see cref="Brush"/> that will be used to paint the border of the LightBulb preview pane.
        /// </summary>
        public virtual Brush PreviewBorderBrush
        {
            get { return _previewBorderBrush; }
            set { SetProperty(ref _previewBorderBrush, value); }
        }

        /// <summary>
        /// Gets a <see cref="Brush"/> that will be used to paint the background of the focused LightBulb preview pane.
        /// </summary>
        public virtual Brush PreviewFocusBackgroundBrush
        {
            get { return _previewFocusBackgroundBrush; }
            set { SetProperty(ref _previewFocusBackgroundBrush, value); }
        }

        /// <summary>
        /// Gets a <see cref="Color"/> that will be used to paint the background of the LightBulb in discovery mode.
        /// </summary>
        public virtual Color DiscoveryModeBackgroundColor
        {
            get { return _discoveryModeBackgroundColor; }
            set { SetProperty(ref _discoveryModeBackgroundColor, value); }
        }

        /// <summary>
        /// Gets a <see cref="Color"/> that will be used to paint the border of the LightBulb in discovery mode.
        /// </summary>
        public virtual Color DiscoveryModeBorderColor
        {
            get { return _discoveryModeBorderColor; }
            set { SetProperty(ref _discoveryModeBorderColor, value); }
        }

        /// <summary>
        /// Gets a <see cref="Brush"/> that will be used to paint the hyperlink in QuickInfo that expands QuickInfo-based LightBulb.
        /// </summary>
        public virtual Brush ShowQuickFixesLinkBrush
        {
            get { return _showQuickFixesLinkBrush; }
            set { SetProperty(ref _showQuickFixesLinkBrush, value); }
        }

        /// <summary>
        /// Gets a <see cref="Brush"/> that will be used to paint the shortcut of the command that expands LightBulb.
        /// </summary>
        public virtual Brush ShowQuickFixesKeyBindingBrush
        {
            get { return _showQuickFixesKeyBindingBrush; }
            set { SetProperty(ref _showQuickFixesKeyBindingBrush, value); }
        }

        /// <summary>
        /// Gets a <see cref="Brush"/> that will be used to paint the suffix part of the LightBulb item's display text.
        /// </summary>
        public virtual Brush DisplayTextSuffixForegroundBrush
        {
            get { return _displayTextSuffixForegroundBrush; }
            set { SetProperty(ref _displayTextSuffixForegroundBrush, value); }
        }

        /// <summary>
        /// Gets a shortcut of the command that expands LightBulb.
        /// </summary>
        public virtual string ShowQuickFixesKeyBinding
        {
            get { return _showQuickFixesKeyBinding; }
            set { SetProperty(ref _showQuickFixesKeyBinding, value); }
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
