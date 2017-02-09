// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Editor
{
    /// <summary>
    /// Provides a base implementation for mouse bindings, so that clients can
    /// override only the the methods they need.
    /// </summary>
    public abstract class MouseProcessorBase : IMouseProcessor
    {
        #region IMouseProcessor Members

        /// <summary>
        /// Handles the mouse left button down event before the default handler.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        public virtual void PreprocessMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e) { }

        /// <summary>
        /// Handles the mouse left button down event after the default handler.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        public virtual void PostprocessMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e) { }

        /// <summary>
        /// Handles the mouse right button down event before the default handler.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        public virtual void PreprocessMouseRightButtonDown(System.Windows.Input.MouseButtonEventArgs e) { }

        /// <summary>
        /// Handles the mouse right button down event after the default handler.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        public virtual void PostprocessMouseRightButtonDown(System.Windows.Input.MouseButtonEventArgs e) { }

        /// <summary>
        /// Handles the mouse left button up event before the default handler.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        public virtual void PreprocessMouseLeftButtonUp(System.Windows.Input.MouseButtonEventArgs e) { }

        /// <summary>
        /// Handles the mouse left button up event after the default handler.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        public virtual void PostprocessMouseLeftButtonUp(System.Windows.Input.MouseButtonEventArgs e) { }

        /// <summary>
        /// Handles the mouse right button up event before the default handler.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        public virtual void PreprocessMouseRightButtonUp(System.Windows.Input.MouseButtonEventArgs e) { }

        /// <summary>
        /// Handles the mouse right button up event after the default handler.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        public virtual void PostprocessMouseRightButtonUp(System.Windows.Input.MouseButtonEventArgs e) { }

        /// <summary>
        /// Handles the mouse up event before the default handler.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        public virtual void PreprocessMouseUp(System.Windows.Input.MouseButtonEventArgs e) { }

        /// <summary>
        /// Handles the mouse up event after the default handler.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        public virtual void PostprocessMouseUp(System.Windows.Input.MouseButtonEventArgs e) { }

        /// <summary>
        /// Handles the mouse down event before the default handler.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        public virtual void PreprocessMouseDown(System.Windows.Input.MouseButtonEventArgs e) { }

        /// <summary>
        /// Handles the mouse down event after the default handler.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        public virtual void PostprocessMouseDown(System.Windows.Input.MouseButtonEventArgs e) { }

        /// <summary>
        /// Handles the mouse move event before the default handler.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        public virtual void PreprocessMouseMove(System.Windows.Input.MouseEventArgs e) { }

        /// <summary>
        /// Handles the mouse move event after the default handler.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        public virtual void PostprocessMouseMove(System.Windows.Input.MouseEventArgs e) { }

        /// <summary>
        /// Handles the mouse wheel event before the default handler.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        public virtual void PreprocessMouseWheel(System.Windows.Input.MouseWheelEventArgs e) { }

        /// <summary>
        /// Handles the mouse wheel event after the default handler.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        public virtual void PostprocessMouseWheel(System.Windows.Input.MouseWheelEventArgs e) { }

        /// <summary>
        /// Handles the mouse enter event before the default handler.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        public virtual void PreprocessMouseEnter(System.Windows.Input.MouseEventArgs e) { }

        /// <summary>
        /// Handles the mouse enter event after the default handler.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        public virtual void PostprocessMouseEnter(System.Windows.Input.MouseEventArgs e) { }

        /// <summary>
        /// Handles the mouse leave event before the default handler.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        public virtual void PreprocessMouseLeave(System.Windows.Input.MouseEventArgs e) { }

        /// <summary>
        /// Handles the mouse leave event after the default handler.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        public virtual void PostprocessMouseLeave(System.Windows.Input.MouseEventArgs e) { }

        /// <summary>
        /// Handles the drag leave event before the default handler.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        public virtual void PreprocessDragLeave(System.Windows.DragEventArgs e) { }

        /// <summary>
        /// Handles the drag leave event after the default handler.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        public virtual void PostprocessDragLeave(System.Windows.DragEventArgs e) { }

        /// <summary>
        /// Handles the drag over event before the default handler.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        public virtual void PreprocessDragOver(System.Windows.DragEventArgs e) { }

        /// <summary>
        /// Handles the drag over event after the default handler.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        public virtual void PostprocessDragOver(System.Windows.DragEventArgs e) { }

        /// <summary>
        /// Handles the drag enter event before the default handler.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        public virtual void PreprocessDragEnter(System.Windows.DragEventArgs e) { }

        /// <summary>
        /// Handles the drag enter event after the default handler.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        public virtual void PostprocessDragEnter(System.Windows.DragEventArgs e) { }

        /// <summary>
        /// Handles the drop event before the default handler.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        public virtual void PreprocessDrop(System.Windows.DragEventArgs e) { }

        /// <summary>
        /// Handles the drop event after the default handler.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        public virtual void PostprocessDrop(System.Windows.DragEventArgs e) { }

        /// <summary>
        /// Handles the query continue drag event before the default handler.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        public virtual void PreprocessQueryContinueDrag(System.Windows.QueryContinueDragEventArgs e) { }

        /// <summary>
        /// Handles the query continue drag event after the default handler.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        public virtual void PostprocessQueryContinueDrag(System.Windows.QueryContinueDragEventArgs e) { }

        /// <summary>
        /// Handles the feedback event before the default handler.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        public virtual void PreprocessGiveFeedback(System.Windows.GiveFeedbackEventArgs e) { }

        /// <summary>
        /// Handles the feedback event after the default handler.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        public virtual void PostprocessGiveFeedback(System.Windows.GiveFeedbackEventArgs e) { }

        #endregion
    }
}
