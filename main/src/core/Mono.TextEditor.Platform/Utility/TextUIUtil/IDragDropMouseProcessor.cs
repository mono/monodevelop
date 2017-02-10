namespace Microsoft.VisualStudio.Text.Utilities
{
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Input;

    /// <summary>
    /// This interface is a used as an abstraction of the DragDropMouseProcessor so that it can be called from the left margin
    /// to handle drag/drop.
    /// </summary>
    public interface IDragDropMouseProcessor
    {
        void DoPreprocessMouseLeftButtonDown(MouseButtonEventArgs e, Point position);
        void DoPreprocessMouseLeftButtonUp(MouseButtonEventArgs e, Point position);
        void DoPostprocessMouseLeftButtonUp(MouseButtonEventArgs e, Point position);
        void DoPreprocessMouseMove(MouseEventArgs e, Point position);
        void DoPreprocessDrop(DragEventArgs e, Point position);
        void DoPreprocessDragEnter(DragEventArgs e, Point position);
        void DoPreprocessDragLeave(DragEventArgs e);
        void DoPreprocessDragOver(DragEventArgs e, Point position);
        void DoPreprocessQueryContinueDrag(QueryContinueDragEventArgs e);
        void DoPostprocessMouseLeave(MouseEventArgs e);
    }
}