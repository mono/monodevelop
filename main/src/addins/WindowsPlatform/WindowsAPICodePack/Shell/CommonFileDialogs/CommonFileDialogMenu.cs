//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Markup;

namespace Microsoft.WindowsAPICodePack.Dialogs.Controls
{
    /// <summary>
    /// Defines the menu controls for the Common File Dialog.
    /// </summary>
    [ContentProperty("Items")]
    public class CommonFileDialogMenu : CommonFileDialogProminentControl
    {
        private Collection<CommonFileDialogMenuItem> items = new Collection<CommonFileDialogMenuItem>();
        /// <summary>
        /// Gets the collection of CommonFileDialogMenuItem objects.
        /// </summary>
        public Collection<CommonFileDialogMenuItem> Items
        {
            get { return items; }
        }

        /// <summary>
        /// Creates a new instance of this class.
        /// </summary>
        public CommonFileDialogMenu() : base() { }

        /// <summary>
        /// Creates a new instance of this class with the specified text.
        /// </summary>
        /// <param name="text">The text to display for this control.</param>
        public CommonFileDialogMenu(string text) : base(text) { }

        /// <summary>
        /// Creates a new instance of this class with the specified name and text.
        /// </summary>
        /// <param name="name">The name of this control.</param>
        /// <param name="text">The text to display for this control.</param>
        public CommonFileDialogMenu(string name, string text) : base(name, text) { }

        /// <summary>
        /// Attach the Menu control to the dialog object.
        /// </summary>
        /// <param name="dialog">the target dialog</param>
        internal override void Attach(IFileDialogCustomize dialog)
        {
            Debug.Assert(dialog != null, "CommonFileDialogMenu.Attach: dialog parameter can not be null");

            // Add the menu control
            dialog.AddMenu(this.Id, this.Text);

            // Add the menu items
            foreach (CommonFileDialogMenuItem item in this.items)
                dialog.AddControlItem(this.Id, item.Id, item.Text);

            // Make prominent as needed
            if (IsProminent)
                dialog.MakeProminent(this.Id);

            // Sync unmanaged properties with managed properties
            SyncUnmanagedProperties();
        }
    }

    /// <summary>
    /// Creates the CommonFileDialogMenuItem items for the Common File Dialog.
    /// </summary>
    public class CommonFileDialogMenuItem : CommonFileDialogControl
    {
        /// <summary>
        /// Creates a new instance of this class.
        /// </summary>
        public CommonFileDialogMenuItem() : base(string.Empty) { }

        /// <summary>
        /// Creates a new instance of this class with the specified text.
        /// </summary>
        /// <param name="text">The text to display for this control.</param>
        public CommonFileDialogMenuItem(string text) : base(text) { }

        /// <summary>
        /// Occurs when a user clicks a menu item.
        /// </summary>
        public event EventHandler Click = delegate { };
        internal void RaiseClickEvent()
        {
            // Make sure that this control is enabled and has a specified delegate
            if (Enabled) { Click(this, EventArgs.Empty); }
        }

        /// <summary>
        /// Attach this control to the dialog object
        /// </summary>
        /// <param name="dialog">Target dialog</param>
        internal override void Attach(IFileDialogCustomize dialog)
        {
            // Items are added via the menu itself
        }
    }
}
