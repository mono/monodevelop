//Copyright (c) Microsoft Corporation.  All rights reserved.

namespace Microsoft.WindowsAPICodePack.Dialogs
{
    /// <summary>
    /// Implements a button that can be hosted in a task dialog.
    /// </summary>
    public class TaskDialogButton : TaskDialogButtonBase
    {
        /// <summary>
        /// Creates a new instance of this class.
        /// </summary>
        public TaskDialogButton() { }

        /// <summary>
        /// Creates a new instance of this class with the specified property settings.
        /// </summary>
        /// <param name="name">The name of the button.</param>
        /// <param name="text">The button label.</param>
        public TaskDialogButton(string name, string text) : base(name, text) { }

        private bool useElevationIcon;
        /// <summary>
        /// Gets or sets a value that controls whether the elevation icon is displayed.
        /// </summary>
        public bool UseElevationIcon
        {
            get { return useElevationIcon; }
            set
            {
                CheckPropertyChangeAllowed("ShowElevationIcon");
                useElevationIcon = value;
                ApplyPropertyChange("ShowElevationIcon");
            }
        }
    }
}
