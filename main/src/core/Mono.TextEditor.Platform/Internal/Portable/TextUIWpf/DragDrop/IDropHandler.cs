// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Editor.DragDrop
{

    using System.Windows;

    /// <summary>
    /// Handles drag and drop operations for different data formats.
    /// </summary>
    /// <remarks>
    /// Any object that implements this interface can act as a drop handler. 
    /// Drop handlers are created via <see cref="IDropHandlerProvider"/>.
    /// To learn more about data formats and their association with
    /// <see cref="IDropHandler"/>s, please see <see cref="IDropHandlerProvider"/>.
    /// </remarks>
    public interface IDropHandler
    {

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
        DragDropPointerEffects HandleDragStarted(DragDropInfo dragDropInfo);

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
        DragDropPointerEffects HandleDraggingOver(DragDropInfo dragDropInfo);

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
        DragDropPointerEffects HandleDataDropped(DragDropInfo dragDropInfo);

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
        bool IsDropEnabled(DragDropInfo dragDropInfo);

        /// <summary>
        /// Indicates that a drag and drop operation has been canceled.
        /// </summary>
        /// <remarks>This method allows the drop handler to update its state after cancellation.</remarks>
        void HandleDragCanceled();
    }
}
