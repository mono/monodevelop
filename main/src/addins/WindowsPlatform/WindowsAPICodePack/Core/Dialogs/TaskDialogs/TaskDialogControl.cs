//Copyright (c) Microsoft Corporation.  All rights reserved.

namespace Microsoft.WindowsAPICodePack.Dialogs
{
    /// <summary>
    /// Declares the abstract base class for all custom task dialog controls.
    /// </summary>
    public abstract class TaskDialogControl : DialogControl
    {
        /// <summary>
        /// Creates a new instance of a task dialog control.
        /// </summary>
        protected TaskDialogControl() { }
        /// <summary>
        /// Creates a new instance of a task dialog control with the specified name.
        /// </summary>
        /// <param name="name">The name for this control.</param>
        protected TaskDialogControl(string name) : base(name) { }
    }
}