// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Editor
{
    using System;
    using System.Windows;
    using System.Windows.Input;

    /// <summary>
    /// Provides extensions for mouse bindings.
    /// </summary>
    public interface IMouseProcessor
    {
        /// <summary>
        /// Handles a mouse left button down event before the default handler.
        /// </summary>
        /// <param name="e">
        /// Event arguments that describe the event.
        /// </param>
        void PreprocessMouseLeftButtonDown(MouseButtonEventArgs e);

        /// <summary>
        /// Handles a mouse left button down event after the default handler.
        /// </summary>
        /// <param name="e">
        /// Event arguments that describe the event.
        /// </param>
        void PostprocessMouseLeftButtonDown(MouseButtonEventArgs e);

        /// <summary>
        /// Handles a mouse right button down event before the default handler.
        /// </summary>
        /// <param name="e">
        /// Event arguments that describe the event.
        /// </param>
        void PreprocessMouseRightButtonDown(MouseButtonEventArgs e);

        /// <summary>
        /// Handles a mouse right button down event after the default handler.
        /// </summary>
        /// <param name="e">
        /// Event arguments that describe the event.
        /// </param>
        void PostprocessMouseRightButtonDown(MouseButtonEventArgs e);

        /// <summary>
        /// Handles a mouse left button up event before the default handler.
        /// </summary>
        /// <param name="e">
        /// Event arguments that describe the event.
        /// </param>
        void PreprocessMouseLeftButtonUp(MouseButtonEventArgs e);

        /// <summary>
        /// Handles a mouse left button up event after the default handler.
        /// </summary>
        /// <param name="e">
        /// Event arguments that describe the event.
        /// </param>
        void PostprocessMouseLeftButtonUp(MouseButtonEventArgs e);

        /// <summary>
        /// Handles a mouse right button up event before the default handler.
        /// </summary>
        /// <param name="e">
        /// Event arguments that describe the event.
        /// </param>
        void PreprocessMouseRightButtonUp(MouseButtonEventArgs e);

        /// <summary>
        /// Handles a mouse right button up event after the default handler.
        /// </summary>
        /// <param name="e">
        /// Event arguments that describe the event.
        /// </param>
        void PostprocessMouseRightButtonUp(MouseButtonEventArgs e);

        /// <summary>
        /// Handles a mouse up event before the default handler.
        /// </summary>
        /// <param name="e">
        /// Event arguments that describe the event.
        /// </param>
        void PreprocessMouseUp(MouseButtonEventArgs e);

        /// <summary>
        /// Handles a mouse up event after the default handler.
        /// </summary>
        /// <param name="e">
        /// Event arguments that describe the event.
        /// </param>
        void PostprocessMouseUp(MouseButtonEventArgs e);

        /// <summary>
        /// Handles a mouse down event before the default handler.
        /// </summary>
        /// <param name="e">
        /// Event arguments that describe the event.
        /// </param>
        void PreprocessMouseDown(MouseButtonEventArgs e);

        /// <summary>
        /// Handles a mouse down event after the default handler.
        /// </summary>
        /// <param name="e">
        /// Event arguments that describe the event.
        /// </param>
        void PostprocessMouseDown(MouseButtonEventArgs e);

        /// <summary>
        /// Handles a mouse move event before the default handler.
        /// </summary>
        /// <param name="e">
        /// Event arguments that describe the event.
        /// </param>
        void PreprocessMouseMove(MouseEventArgs e);

        /// <summary>
        /// Handles a mouse move event after the default handler.
        /// </summary>
        /// <param name="e">
        /// Event arguments that describe the event.
        /// </param>
        void PostprocessMouseMove(MouseEventArgs e);

        /// <summary>
        /// Handles a mouse wheel event before the default handler.
        /// </summary>
        /// <param name="e">
        /// Event arguments that describe the event.
        /// </param>
        void PreprocessMouseWheel(MouseWheelEventArgs e);

        /// <summary>
        /// Handles a mouse wheel event after the default handler.
        /// </summary>
        /// <param name="e">
        /// Event arguments that describe the event.
        /// </param>
        void PostprocessMouseWheel(MouseWheelEventArgs e);

        /// <summary>
        /// Handles a mouse enter event before the default handler.
        /// </summary>
        /// <param name="e">
        /// Event arguments that describe the event.
        /// </param>
        void PreprocessMouseEnter(MouseEventArgs e);

        /// <summary>
        /// Handles a mouse enter event after the default handler.
        /// </summary>
        /// <param name="e">
        /// Event arguments that describe the event.
        /// </param>
        void PostprocessMouseEnter(MouseEventArgs e);

        /// <summary>
        /// Handles a mouse leave event before the default handler.
        /// </summary>
        /// <param name="e">
        /// Event arguments that describe the event.
        /// </param>
        void PreprocessMouseLeave(MouseEventArgs e);

        /// <summary>
        /// Handles a mouse leave event after the default handler.
        /// </summary>
        /// <param name="e">
        /// Event arguments that describe the event.
        /// </param>
        void PostprocessMouseLeave(MouseEventArgs e);

        /// <summary>
        /// Handles a drag leave event before the default handler.
        /// </summary>
        /// <param name="e">
        /// A <see cref="DragEventArgs"/> describing the drag operation.
        /// </param>
        void PreprocessDragLeave(DragEventArgs e);

        /// <summary>
        /// Handles a drag leave event after the default handler.
        /// </summary>
        /// <param name="e">
        /// A <see cref="DragEventArgs"/> describing the drag operation.
        /// </param>
        void PostprocessDragLeave(DragEventArgs e);

        /// <summary>
        /// Handles a drag over event before the default handler.
        /// </summary>
        /// <param name="e">
        /// A <see cref="DragEventArgs"/> describing the drag operation.
        /// </param>
        void PreprocessDragOver(DragEventArgs e);

        /// <summary>
        /// Handles a drag over event after the default handler.
        /// </summary>
        /// <param name="e">
        /// A <see cref="DragEventArgs"/> describing the drag operation.
        /// </param>
        void PostprocessDragOver(DragEventArgs e);

        /// <summary>
        /// Handles a drag enter event before the default handler.
        /// </summary>
        /// <param name="e">
        /// A <see cref="DragEventArgs"/> describing the drag operation.
        /// </param>
        void PreprocessDragEnter(DragEventArgs e);

        /// <summary>
        /// Handles a drag enter event after the default handler.
        /// </summary>
        /// <param name="e">
        /// A <see cref="DragEventArgs"/> describing the drag operation.
        /// </param>
        void PostprocessDragEnter(DragEventArgs e);

        /// <summary>
        /// Handles a drop event before the default handler.
        /// </summary>
        /// <param name="e">
        /// <see cref="DragEventArgs"/> describing the drag operation.
        /// </param>
        void PreprocessDrop(DragEventArgs e);

        /// <summary>
        /// Handles a drop event after the default handler.
        /// </summary>
        /// <param name="e">
        /// A <see cref="DragEventArgs"/> describing the drag operation.
        /// </param>
        void PostprocessDrop(DragEventArgs e);

        /// <summary>
        /// Handles a QueryContinueDrag event before the default handler. 
        /// </summary>
        /// <param name="e">
        /// A <see cref="DragEventArgs"/> describing the drag operation.
        /// </param>
        void PreprocessQueryContinueDrag(QueryContinueDragEventArgs e);

        /// <summary>
        /// Handles a QueryContinueDrag event after the default handler. 
        /// </summary>
        /// <param name="e">
        /// A <see cref="DragEventArgs"/> describing the drag operation.
        /// </param>
        void PostprocessQueryContinueDrag(QueryContinueDragEventArgs e);

        /// <summary>
        /// Handles a GiveFeedback event before the default handler. 
        /// </summary>
        /// <param name="e">
        /// A <see cref="DragEventArgs"/> describing the drag operation.
        /// </param>
        void PreprocessGiveFeedback(GiveFeedbackEventArgs e);

        /// <summary>
        /// Handles a GiveFeedback event after the default handler. 
        /// </summary>
        /// <param name="e">
        /// A <see cref="DragEventArgs"/> describing the drag operation.
        /// </param>
        void PostprocessGiveFeedback(GiveFeedbackEventArgs e);
        

    }
}
        