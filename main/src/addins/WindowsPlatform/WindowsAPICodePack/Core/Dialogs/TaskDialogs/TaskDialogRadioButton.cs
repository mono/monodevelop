//Copyright (c) Microsoft Corporation.  All rights reserved.

namespace Microsoft.WindowsAPICodePack.Dialogs
{
    /// <summary>
    /// Defines a radio button that can be hosted in by a 
    /// <see cref="TaskDialog"/> object.
    /// </summary>
    public class TaskDialogRadioButton : TaskDialogButtonBase
    {
        /// <summary>
        /// Creates a new instance of this class.
        /// </summary>
        public TaskDialogRadioButton() { }

        /// <summary>
        /// Creates a new instance of this class with
        /// the specified name and text.
        /// </summary>
        /// <param name="name">The name for this control.</param>
        /// <param name="text">The value for this controls 
        /// <see cref="P:Microsoft.WindowsAPICodePack.Dialogs.TaskDialogButtonBase.Text"/> property.</param>
        public TaskDialogRadioButton(string name, string text) : base(name, text) { }
    }
}
