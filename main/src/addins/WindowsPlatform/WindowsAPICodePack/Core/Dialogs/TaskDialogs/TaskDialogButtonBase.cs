//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;

namespace Microsoft.WindowsAPICodePack.Dialogs
{
    // ContentProperty allows us to specify the text 
    // of the button as the child text of
    // a button element in XAML, as well as explicitly 
    // set with 'Text="<text>"'
    // Note that this attribute is inherited, so it 
    // applies to command-links and radio buttons as well.
    /// <summary>
    /// Defines the abstract base class for task dialog buttons. 
    /// Classes that inherit from this class will inherit 
    /// the Text property defined in this class.
    /// </summary>
    public abstract class TaskDialogButtonBase : TaskDialogControl
    {
        
        /// <summary>
        /// Creates a new instance on a task dialog button.
        /// </summary>
        protected TaskDialogButtonBase() { }
        /// <summary>
        /// Creates a new instance on a task dialog button with
        /// the specified name and text.
        /// </summary>
        /// <param name="name">The name for this button.</param>
        /// <param name="text">The label for this button.</param>
        protected TaskDialogButtonBase(string name, string text) : base(name)
        {
            this.text = text;
        }

        // Note that we don't need to explicitly 
        // implement the add/remove delegate for the Click event;
        // the hosting dialog only needs the delegate 
        // information when the Click event is 
        // raised (indirectly) by NativeTaskDialog, 
        // so the latest delegate is always available.
        /// <summary>
        /// Raised when the task dialog button is clicked.
        /// </summary>
        public event EventHandler Click;

        internal void RaiseClickEvent()
        {
            // Only perform click if the button is enabled.
            if (!enabled) { return; }
            
            if (Click != null) { Click(this, EventArgs.Empty); }
        }

        private string text;
        /// <summary>
        /// Gets or sets the button text.
        /// </summary>
        public string Text
        {
            get { return text; }
            set 
            {
                CheckPropertyChangeAllowed("Text");
                text = value;
                ApplyPropertyChange("Text");
            }
        }

        private bool enabled = true;
        /// <summary>
        /// Gets or sets a value that determines whether the
        /// button is enabled. The enabled state can cannot be changed
        /// before the dialog is shown.
        /// </summary>
        public bool Enabled
        {
            get { return enabled; }
            set 
            {
                CheckPropertyChangeAllowed("Enabled");
                enabled = value;
                ApplyPropertyChange("Enabled");
            }
        }

        private bool defaultControl;
        /// <summary>
        /// Gets or sets a value that indicates whether
        /// this button is the default button.
        /// </summary>
        public bool Default
        {
            get { return defaultControl; }
            set
            {
                CheckPropertyChangeAllowed("Default");
                defaultControl = value;
                ApplyPropertyChange("Default");
            }
        }

        /// <summary>
        /// Returns the Text property value for this button.
        /// </summary>
        /// <returns>A <see cref="System.String"/>.</returns>
        public override string ToString()
        {
            return text ?? string.Empty;
        }
    }
}
