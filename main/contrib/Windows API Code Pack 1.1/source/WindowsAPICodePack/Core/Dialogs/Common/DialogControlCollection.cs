//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.WindowsAPICodePack.Resources;

namespace Microsoft.WindowsAPICodePack.Dialogs
{
    /// <summary>
    /// Strongly typed collection for dialog controls.
    /// </summary>
    /// <typeparam name="T">DialogControl</typeparam>
    public sealed class DialogControlCollection<T> : Collection<T> where T : DialogControl
    {
        private IDialogControlHost hostingDialog;

        internal DialogControlCollection(IDialogControlHost host)
        {
            hostingDialog = host;
        }

        /// <summary>
        /// Inserts an dialog control at the specified index.
        /// </summary>
        /// <param name="index">The location to insert the control.</param>
        /// <param name="control">The item to insert.</param>
        /// <permission cref="System.InvalidOperationException">A control with 
        /// the same name already exists in this collection -or- 
        /// the control is being hosted by another dialog -or- the associated dialog is 
        /// showing and cannot be modified.</permission>
        protected override void InsertItem(int index, T control)
        {
            // Check for duplicates, lack of host, 
            // and during-show adds.
            if (Items.Contains(control))
            {
                throw new InvalidOperationException(LocalizedMessages.DialogCollectionCannotHaveDuplicateNames);
            }
            if (control.HostingDialog != null)
            {
                throw new InvalidOperationException(LocalizedMessages.DialogCollectionControlAlreadyHosted);
            }
            if (!hostingDialog.IsCollectionChangeAllowed())
            {
                throw new InvalidOperationException(LocalizedMessages.DialogCollectionModifyShowingDialog);
            }

            // Reparent, add control.
            control.HostingDialog = hostingDialog;
            base.InsertItem(index, control);

            // Notify that we've added a control.
            hostingDialog.ApplyCollectionChanged();
        }

        /// <summary>
        /// Removes the control at the specified index.
        /// </summary>
        /// <param name="index">The location of the control to remove.</param>
        /// <permission cref="System.InvalidOperationException">
        /// The associated dialog is 
        /// showing and cannot be modified.</permission>
        protected override void RemoveItem(int index)
        {
            // Notify that we're about to remove a control.
            // Throw if dialog showing.
            if (!hostingDialog.IsCollectionChangeAllowed())
            {
                throw new InvalidOperationException(LocalizedMessages.DialogCollectionModifyShowingDialog);
            }

            DialogControl control = (DialogControl)Items[index];

            // Unparent and remove.
            control.HostingDialog = null;
            base.RemoveItem(index);

            hostingDialog.ApplyCollectionChanged();
        }

        /// <summary>
        /// Defines the indexer that supports accessing controls by name. 
        /// </summary>
        /// <remarks>
        /// <para>Control names are case sensitive.</para>
        /// <para>This indexer is useful when the dialog is created in XAML
        /// rather than constructed in code.</para></remarks>
        ///<exception cref="System.ArgumentException">
        /// The name cannot be null or a zero-length string.</exception>
        /// <remarks>If there is more than one control with the same name, only the <B>first control</B> will be returned.</remarks>
        public T this[string name]
        {
            get
            {
                if (string.IsNullOrEmpty(name))
                {
                    throw new ArgumentException(LocalizedMessages.DialogCollectionControlNameNull, "name");
                }

                return Items.FirstOrDefault(x => x.Name == name);
            }
        }

        /// <summary>
        /// Searches for the control who's id matches the value
        /// passed in the <paramref name="id"/> parameter.
        /// </summary>
        /// 
        /// <param name="id">An integer containing the identifier of the 
        /// control being searched for.</param>
        /// 
        /// <returns>A DialogControl who's id matches the value of the
        /// <paramref name="id"/> parameter.</returns>        
        internal DialogControl GetControlbyId(int id)
        {
            return Items.FirstOrDefault(x => x.Id == id);
        }
    }
}
