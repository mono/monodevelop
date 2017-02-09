// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Editor.DragDrop
{

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Media;

    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.Operations;

    /// <summary>
    /// This class provides the basic functionality necessary to process drop of data on to the editor. It's provided
    /// as a convenience class to easily allow extenders to provide their own custom drop handlers by extending this class.
    /// </summary>
    public abstract class DropHandlerBase : IDropHandler
    {
        #region Protected Attributes

        /// <summary>
        /// Keeps a reference for the text view for which drap and drop is being implemented
        /// </summary>
        private IWpfTextView _wpfTextView;

        /// <summary>
        /// Keeps a reference to the <see cref="IEditorOperations"/> used to handle tasks such as text insertion.
        /// </summary>
        private IEditorOperations _editorOperations;

        #endregion //Protected Attributes

        #region Construction

        /// <summary>
        /// Constructs a <see cref="DropHandlerBase"/>.
        /// </summary>
        /// <param name="wpfTextView">The <see cref="IWpfTextView"/> over which this drop handler operates on.</param>
        /// <param name="editorOperations">The <see cref="IEditorOperations"/> used to insert text into the editor.</param>
        protected DropHandlerBase(IWpfTextView wpfTextView, IEditorOperations editorOperations)
        {
            if (wpfTextView == null) 
                throw new System.ArgumentNullException("wpfTextView");
            if (editorOperations == null)
                throw new System.ArgumentNullException("editorOperations");
            
            _wpfTextView = wpfTextView;
            _editorOperations = editorOperations;
        }

        #endregion //Construction

        #region IDropHandler Members

        /// <summary>
        /// Indicates the start of a drag and drop operation.
        /// </summary>
        /// <param name="dragDropInfo">
        /// Information about the drag and drop operation in progress.
        /// </param>
        /// <returns>
        /// A <see cref="DragDropPointerEffects"/> object for the current operation. For example, this can be used to
        /// indicate a copy operation when the CTRL key is down.
        /// </returns>
        /// <remarks>
        /// This method is called once a drop operation is in progress and the <see cref="IDropHandler"/>
        /// is the handler for the data format of the drop operation.
        /// It will not be called for dropping data formats not supported by this <see cref="IDropHandler"/>.
        /// A call to <see cref="HandleDragStarted"/> is always followed by a call to either <see cref="HandleDragCanceled"/> or <see cref="HandleDataDropped"/>.
        /// </remarks>
        public virtual DragDropPointerEffects HandleDragStarted(DragDropInfo dragDropInfo)
        {
            return this.GetDragDropEffect(dragDropInfo);
        }

        /// <summary>
        /// Indicates that the drag and drop operation is in progress.
        /// </summary>
        /// <param name="dragDropInfo">
        /// Information about the drag and drop operation in progress.
        /// </param>
        /// <returns>
        /// A <see cref="DragDropPointerEffects"/> for the current operation. For example, this can be used to
        /// indicate a copy operation when the CTRL key is down.
        /// </returns>
        /// <remarks>This method is called continuously while the user is dragging the mouse over the text editor during
        /// a drag and drop operation. It can be used to
        /// draw additional information next to the mouse cursor as a preview of the text after the drop operation.
        /// </remarks>
        public virtual DragDropPointerEffects HandleDraggingOver(DragDropInfo dragDropInfo)
        {
            return this.GetDragDropEffect(dragDropInfo);
        }

        /// <summary>
        /// Indicates that the drag and drop operation has completed, and that the final tasks, if any, should be performed now.
        /// </summary>
        /// <param name="dragDropInfo">
        /// Information about the drag and drop operation in progress.
        /// </param>
        /// <returns>
        /// The drag and drop effects of this drop operation. For example, if the drop operation has moved data,
        /// DragDropPointerEffects.Move should be returned.
        /// </returns>
        /// <remarks>This method is called when the user drops the data onto the editor. 
        /// This marks the end of a drag and drop operation. 
        /// The <see cref="IDropHandler"/> is expected to perform the final tasks of the operation.
        /// </remarks>
        public virtual DragDropPointerEffects HandleDataDropped(DragDropInfo dragDropInfo)
        {
            if (dragDropInfo == null)
                throw new ArgumentNullException("dragDropInfo");

            ITextSelection selection = _wpfTextView.Selection;
            //keeps track of the result of this operation
            DragDropPointerEffects result = DragDropPointerEffects.None;
            //tracks the location at which the data was dropped
            VirtualSnapshotPoint dropLocation = dragDropInfo.VirtualBufferPosition;
            //convert the drag/drop data to text
            string dragDropText = this.ExtractText(dragDropInfo);
            bool isReversed = selection.IsReversed;
            bool copyRequested = (dragDropInfo.KeyStates & DragDropKeyStates.ControlKey) == DragDropKeyStates.ControlKey;
            bool copyAllowed = (dragDropInfo.AllowedEffects & DragDropEffects.Copy) == DragDropEffects.Copy;

            ITextSnapshot preEditSnapshot = _wpfTextView.TextSnapshot;

            // track the point where the data will be inserted
            ITrackingPoint insertionPoint = preEditSnapshot.CreateTrackingPoint(dropLocation.Position, PointTrackingMode.Negative);
            
            // track the currently selected spans before any edits are performed on the buffer
            List<ITrackingSpan> selectionSpans = new List<ITrackingSpan>();
            foreach (SnapshotSpan selectedSpan in selection.SelectedSpans)
            {
                selectionSpans.Add(preEditSnapshot.CreateTrackingSpan(selectedSpan, SpanTrackingMode.EdgeExclusive));
            }

            // perform any necessary pre edit actions
            this.PerformPreEditActions(dragDropInfo);

            // clear selection before data operations
            if (!selection.IsEmpty)
                selection.Clear();

            // a reference to the snapshot resulting from the edits
            bool successfulEdit = false;

            // if the data is being dropped in virtual space, calculate how many whitespace characters will be inserted
            // to fill the gap between the dropped point and the closest buffer position
            int virtualSpaceLength = 0;
            if (dragDropInfo.VirtualBufferPosition.IsInVirtualSpace)
                virtualSpaceLength = _editorOperations.GetWhitespaceForVirtualSpace(dragDropInfo.VirtualBufferPosition).Length;

            if (copyRequested && copyAllowed)
            {
                //copy the data by inserting it in the buffer
                successfulEdit = this.InsertText(dropLocation, dragDropText);
                if (successfulEdit)
                    result = DragDropPointerEffects.Copy;
            }
            else
            {
                //the data needs to be moved
                if (dragDropInfo.IsInternal)
                {
                    //delete the existing selection, and add the data to the new location
                    successfulEdit = this.MoveText(dropLocation, selectionSpans, dragDropText);
                }
                else
                {
                    //the drag is not from this text view, just insert the data at dropLocation
                    successfulEdit = this.InsertText(dropLocation, dragDropText);
                }

                //set the pointer effect to move if the edit was successful since that implies that the data was moved successfully
                if (successfulEdit)
                    result = DragDropPointerEffects.Move;
            }

            // finally select the newly inserted data if the operation was successful
            if (result != DragDropPointerEffects.None)
            {
                SnapshotPoint textInsertionPoint = insertionPoint.GetPoint(_wpfTextView.TextSnapshot);

                // if the data was inserted in virtual space, offset the selection's anchor point by the whitespace that was inserted
                // in virtual space
                if (virtualSpaceLength != 0)
                    textInsertionPoint = textInsertionPoint.Add(virtualSpaceLength);

                this.SelectText(textInsertionPoint, dragDropText.Length, dragDropInfo, isReversed);
            }

            // perform any post edit actions as necessary
            this.PerformPostEditActions(dragDropInfo, successfulEdit);

            return result;
        }

        /// <summary>
        /// Indicates that a drag and drop operation has been Canceled. 
        /// </summary>
        /// <remarks>This method allows the drop handler to update its state after cancellation.</remarks>
        public virtual void HandleDragCanceled()
        {
            //nothing necessary to be done
        }

        /// <summary>
        /// Determines whether the handler can accept data for a drag and drop operation.
        /// </summary>
        /// <param name="dragDropInfo">
        /// Information about the drag and drop operation.
        /// </param>
        /// <returns><c>true</c> if the handler can accept data now, otherwise <c>false</c>.</returns>
        /// <remarks>
        /// <para>This method is used by the editor to check whether the drop handler can accept data 
        /// after it has been designated to handle a drag and drop operation. For example,
        /// the drop handler may be able to handle data only if the view is not read-only. 
        /// The implementation of this method would check the read-only status of the view.</para>
        /// <para>If one drop handler returns <c>false</c>, 
        /// another drop handler might be used to handle the drop operation, even if 
        /// the ordering of <see cref="IDropHandler"/> objects dictates otherwise.</para>
        /// </remarks>
        public virtual bool IsDropEnabled(DragDropInfo dragDropInfo)
        {
            if (dragDropInfo == null)
                throw new ArgumentNullException("dragDropInfo");

            //ensure the source allows either move or copy operation
            if (!(
                (dragDropInfo.AllowedEffects & DragDropEffects.Copy) == DragDropEffects.Copy ||
                (dragDropInfo.AllowedEffects & DragDropEffects.Move) == DragDropEffects.Move))
            {
                return false;
            }

            //only allow dropping when the view is not read-only
            return !_wpfTextView.Options.GetOptionValue<bool>(DefaultTextViewOptions.ViewProhibitUserInputId);
        }

        #endregion //IDropHandler Members

        #region Members

        /// <summary>
        /// Gets the <see cref="IWpfTextView"/> over which this drop handler operates on.
        /// </summary>
        protected IWpfTextView TextView
        {
            get
            {
                return _wpfTextView;
            }
        }

        /// <summary>
        /// Gets the <see cref="IEditorOperations"/> used to handle tasks such as text insertion.
        /// </summary>
        protected IEditorOperations EditorOperations
        {
            get
            {
                return _editorOperations;
            }
        }

        #endregion //Members

        #region Private Helpers

        /// <summary>
        /// This method extracts the text of an <see cref="DragDropInfo"/> object.
        /// </summary>
        protected abstract string ExtractText(DragDropInfo dragDropInfo);

        /// <summary>
        /// This method is called before edits are made to the buffer to perform any necessary pre edit actions.
        /// </summary>
        /// <param name="dragDropInfo">The <see cref="DragDropInfo"/> holding information about the currently ongoing drag/drop operation.</param>
        protected abstract void PerformPreEditActions(DragDropInfo dragDropInfo);

        /// <summary>
        /// This method is called after the edits are made to the buffer to perform any necessary post edit actions.
        /// </summary>
        /// <param name="successfulEdit">If true, the edits performed on the buffer were successful, otherwise, the edits failed.</param>
        /// <param name="dragDropInfo">The <see cref="DragDropInfo"/> holding information about the currently ongoing drag/drop operation.</param>
        protected abstract void PerformPostEditActions(DragDropInfo dragDropInfo, bool successfulEdit);

        /// <summary>
        /// This method selects the text at the end of the drop operation.
        /// </summary>
        /// <remarks>
        /// This method will only be called if the drop of data resulted in an <see cref="DragDropEffects"/> other than DragDropEffects.None.
        /// </remarks>
        /// <param name="insertionPoint">The position at which data was inserted.</param>
        /// <param name="dataLength">The length of the data inserted in the buffer.</param>
        /// <param name="virtualSpaceLength">The length of whitespace inserted in the buffer to fill the gap between the closest buffer position
        ///  and the position at which data was dropped. This value will be non-zero only if data was dropped into virtual space.</param>
        /// <param name="dragDropInfo">The <see cref="DragDropInfo"/> class containing information about the drop.</param>
        /// <param name="reverse">True if the existing selection prior to the drop was reversed.</param>
        protected virtual void SelectText(SnapshotPoint insertionPoint, int dataLength, DragDropInfo dragDropInfo, bool reverse)
        {
            if (insertionPoint == null)
                throw new ArgumentNullException("insertionPoint");
            if (dragDropInfo == null)
                throw new ArgumentNullException("dragDropInfo");

            VirtualSnapshotPoint anchorPoint = new VirtualSnapshotPoint(insertionPoint);
            VirtualSnapshotPoint activePoint = new VirtualSnapshotPoint(insertionPoint.Add(dataLength));

            if (dragDropInfo.IsInternal && reverse)
                _editorOperations.SelectAndMoveCaret(activePoint, anchorPoint, TextSelectionMode.Stream);
            else
                _editorOperations.SelectAndMoveCaret(anchorPoint, activePoint, TextSelectionMode.Stream);
        }

        /// <summary>
        /// Determines what drag &amp; drop effect should be displayed to the user based on the state of the operation.
        /// </summary>
        protected virtual DragDropPointerEffects GetDragDropEffect(DragDropInfo dragDropInfo)
        {
            if (dragDropInfo == null)
                throw new ArgumentNullException("dragDropInfo");

            VirtualSnapshotPoint dropPoint = dragDropInfo.VirtualBufferPosition.TranslateTo(_wpfTextView.TextSnapshot);

            //if an external drop is being performed on a read-only region, then disallow it
            if (_wpfTextView.TextBuffer.IsReadOnly(dropPoint.Position))
                return DragDropPointerEffects.None;

            //determine mode based on user key pressings
            if (((dragDropInfo.AllowedEffects & DragDropEffects.Copy) == DragDropEffects.Copy) && ((dragDropInfo.KeyStates & DragDropKeyStates.ControlKey) == DragDropKeyStates.ControlKey))
                return DragDropPointerEffects.Copy | DragDropPointerEffects.Track;

            if (((dragDropInfo.AllowedEffects & DragDropEffects.Move) == DragDropEffects.Move) && ((dragDropInfo.KeyStates & DragDropKeyStates.ShiftKey) == DragDropKeyStates.ShiftKey))
                return DragDropPointerEffects.Move | DragDropPointerEffects.Track;

            //if control flow gets here, then the user's key pressings must be ignored in order to continue the drag/drop operation because
            //the combination of the user's key pressings with the allowed drag/drop effects is invalid.

            //if move mode is allowed, set move effect
            if ((dragDropInfo.AllowedEffects & DragDropEffects.Move) == DragDropEffects.Move)
                return DragDropPointerEffects.Move | DragDropPointerEffects.Track;

            //if copy mode is allowed, then indicate copy mode
            if ((dragDropInfo.AllowedEffects & DragDropEffects.Copy) == DragDropEffects.Copy)
                return DragDropPointerEffects.Copy | DragDropPointerEffects.Track;

            return DragDropPointerEffects.None;
        }

        /// <summary>
        /// Inserts some textual data at the given position.
        /// </summary>
        /// <param name="position">Position at which the data is to be inserted</param>
        /// <param name="data">Text to be inserted</param>
        /// 
        /// <returns>True if data insertion was successful, false otherwise.</returns>
        protected virtual bool InsertText(VirtualSnapshotPoint position, string data)
        {
            // move the caret to the place where data needs to be inserted
            _wpfTextView.Caret.MoveTo(position.TranslateTo(this.TextView.TextSnapshot));

            return _editorOperations.InsertText(data);
        }

        /// <summary>
        /// Moves the data from one location to another in the buffer by deleting the selection contents and inserting toInsert in insertionPoint.
        /// </summary>
        /// <param name="data">Text to be inserted</param>
        /// <param name="position">Position at which the data is to be inserted</param>
        /// <param name="selectionSpans">A list of <see cref="ITrackingSpan"/> tracking the selection of the user before the drop operation. This span collection should be deleted from the buffer</param>
        /// <returns>True if data insertion and removal was successful, false otherwise.</returns>
        protected virtual bool MoveText(VirtualSnapshotPoint position, IList<ITrackingSpan> selectionSpans, string data)
        {
            ITextSnapshot textSnapshot = _wpfTextView.TextSnapshot;

            // update position to the latest snapshot
            position = position.TranslateTo(textSnapshot);

            // keep track of where the data needs to be inserted
            ITrackingPoint insertionLocation = textSnapshot.CreateTrackingPoint(position.Position, PointTrackingMode.Negative);

            // delete the selection
            if (!this.DeleteSpans(selectionSpans))
                return false;

            // move the caret to the data insertion point
            _wpfTextView.Caret.MoveTo(new VirtualSnapshotPoint(insertionLocation.GetPoint(this.TextView.TextSnapshot), position.VirtualSpaces));

            // finally insert the data
            return _editorOperations.InsertText(data);
        }

        /// <summary>
        /// Given a list of <see cref="ITrackingSpan"/>s, deletes them from the buffer.
        /// </summary>
        protected bool DeleteSpans(IList<ITrackingSpan> spans)
        {
            if (spans == null)
                throw new ArgumentNullException("spans");

            ITextSnapshot mostRecentSnapshot = _wpfTextView.TextSnapshot;
            using (ITextEdit textEdit = _wpfTextView.TextBuffer.CreateEdit())
            {
                foreach(ITrackingSpan span in spans)
                {
                    if (!textEdit.Delete(span.GetSpan(mostRecentSnapshot)))
                        return false;
                }

                textEdit.Apply();

                if (textEdit.Canceled)
                    return false;
            }

            return true;
        }

        #endregion //Private Helpers
    }
}
