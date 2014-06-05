//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Markup;
using Microsoft.WindowsAPICodePack.Shell.Resources;

namespace Microsoft.WindowsAPICodePack.Dialogs.Controls
{
    /// <summary>
    /// Creates the ComboBox controls in the Common File Dialog.
    /// </summary>
    [ContentProperty("Items")]
    public class CommonFileDialogComboBox : CommonFileDialogProminentControl, ICommonFileDialogIndexedControls
    {
        private readonly Collection<CommonFileDialogComboBoxItem> items = new Collection<CommonFileDialogComboBoxItem>();
        /// <summary>
        /// Gets the collection of CommonFileDialogComboBoxItem objects.
        /// </summary>
        public Collection<CommonFileDialogComboBoxItem> Items
        {
            get { return items; }
        }

        /// <summary>
        /// Creates a new instance of this class.
        /// </summary>
        public CommonFileDialogComboBox()
        {
        }

        /// <summary>
        /// Creates a new instance of this class with the specified name.
        /// </summary>
        /// <param name="name">Text to display for this control</param>
        public CommonFileDialogComboBox(string name)
            : base(name, string.Empty)
        {
        }

        #region ICommonFileDialogIndexedControls Members

        private int selectedIndex = -1;
        /// <summary>
        /// Gets or sets the current index of the selected item.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes")]
        public int SelectedIndex
        {
            get { return selectedIndex; }
            set
            {
                // Don't update property if it hasn't changed
                if (selectedIndex == value)
                    return;

                if (HostingDialog == null)
                {
                    selectedIndex = value;
                    return;
                }

                // Only update this property if it has a valid value
                if (value >= 0 && value < items.Count)
                {
                    selectedIndex = value;
                    ApplyPropertyChange("SelectedIndex");
                }
                else
                {
                    throw new IndexOutOfRangeException(LocalizedMessages.ComboBoxIndexOutsideBounds);
                }
            }
        }

        /// <summary>
        /// Occurs when the SelectedIndex is changed.
        /// </summary>
        /// 
        /// <remarks>
        /// By initializing the SelectedIndexChanged event with an empty
        /// delegate, it is not necessary to check  
        /// if the SelectedIndexChanged is not null.
        /// 
        /// </remarks>
        public event EventHandler SelectedIndexChanged = delegate { };

        /// <summary>
        /// Raises the SelectedIndexChanged event if this control is 
        /// enabled.
        /// </summary>
        /// <remarks>Because this method is defined in an interface, we can either
        /// have it as public, or make it private and explicitly implement (like below).
        /// Making it public doesn't really help as its only internal (but can't have this 
        /// internal because of the interface)
        /// </remarks>
        void ICommonFileDialogIndexedControls.RaiseSelectedIndexChangedEvent()
        {
            // Make sure that this control is enabled and has a specified delegate
            if (Enabled)
                SelectedIndexChanged(this, EventArgs.Empty);
        }

        #endregion

        /// <summary>
        /// Attach the ComboBox control to the dialog object
        /// </summary>
        /// <param name="dialog">The target dialog</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes")]
        internal override void Attach(IFileDialogCustomize dialog)
        {
            Debug.Assert(dialog != null, "CommonFileDialogComboBox.Attach: dialog parameter can not be null");

            // Add the combo box control
            dialog.AddComboBox(this.Id);

            // Add the combo box items
            for (int index = 0; index < items.Count; index++)
                dialog.AddControlItem(this.Id, index, items[index].Text);

            // Set the currently selected item
            if (selectedIndex >= 0 && selectedIndex < items.Count)
            {
                dialog.SetSelectedControlItem(this.Id, this.selectedIndex);
            }
            else if (selectedIndex != -1)
            {
                throw new IndexOutOfRangeException(LocalizedMessages.ComboBoxIndexOutsideBounds);
            }

            // Make this control prominent if needed
            if (IsProminent)
                dialog.MakeProminent(this.Id);

            // Sync additional properties
            SyncUnmanagedProperties();
        }

    }

    /// <summary>
    /// Creates a ComboBoxItem for the Common File Dialog.
    /// </summary>
    public class CommonFileDialogComboBoxItem
    {
        private string text = string.Empty;
        /// <summary>
        /// Gets or sets the string that is displayed for this item.
        /// </summary>
        public string Text
        {
            get { return text; }
            set { text = value; }
        }

        /// <summary>
        /// Creates a new instance of this class.
        /// </summary>
        public CommonFileDialogComboBoxItem()
        {
        }

        /// <summary>
        /// Creates a new instance of this class with the specified text.
        /// </summary>
        /// <param name="text">The text to use for the combo box item.</param>
        public CommonFileDialogComboBoxItem(string text)
        {
            this.text = text;
        }
    }
}
