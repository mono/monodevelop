using System;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.Text.Differencing
{
    /// <summary>
    /// A service for creating <see cref="IWpfDifferenceViewer"/>s.
    /// </summary>
    /// <remarks>
    /// This is a MEF service to be imported.
    /// </remarks>
    public interface IWpfDifferenceViewerFactoryService
    {
        /// <summary>
        /// Create an <see cref="IDifferenceViewer"/> over the given <see cref="IDifferenceBuffer"/>.
        /// </summary>
        /// <param name="buffer">The difference buffer to display.</param>
        /// <param name="parentOptions">The parent of the editor options for the difference viewer (if null, the global options are the parent).</param>
        /// <returns>A difference viewer.</returns>
        IWpfDifferenceViewer CreateDifferenceView(IDifferenceBuffer buffer, IEditorOptions parentOptions = null);

        /// <summary>
        /// Create an <see cref="IDifferenceViewer"/> over the given <see cref="IDifferenceBuffer"/> with the given set of roles.
        /// </summary>
        /// <param name="buffer">The difference buffer to display.</param>
        /// <param name="roles">The text view roles to use for the created views.</param>
        /// <param name="parentOptions">The parent of the editor options for the difference viewer (if null, the global options are the parent).</param>
        /// <returns>A difference viewer.</returns>
        IWpfDifferenceViewer CreateDifferenceView(IDifferenceBuffer buffer, ITextViewRoleSet roles, IEditorOptions parentOptions = null);

        /// <summary>
        /// Create an <see cref="IDifferenceViewer"/> over the given <see cref="IDifferenceBuffer"/>, using the given
        /// callback to create the individual views (inline, left, and right).
        /// </summary>
        /// <param name="buffer">The difference buffer to display.</param>
        /// <param name="callback">The callback to use to create individual views.</param>
        /// <param name="parentOptions">The parent of the editor options for the difference viewer (if null, the global options are the parent).</param>
        /// <returns>A difference viewer.</returns>
        IWpfDifferenceViewer CreateDifferenceView(IDifferenceBuffer buffer, CreateTextViewHostCallback callback, IEditorOptions parentOptions = null);

        /// <summary>
        /// Create an <see cref="IDifferenceViewer"/> over the given <see cref="IDifferenceBuffer"/>, without initializing it.
        /// </summary>
        /// <returns>A difference viewer.</returns>
        /// <remarks>
        /// The only legitimate property call on an uninitialized viwer is the VisualElement property.
        /// </remarks>
        IWpfDifferenceViewer CreateUninitializedDifferenceView();

        /// <summary>
        /// If the given text view is owned by a difference viewer, retrieve that difference viewer.
        /// </summary>
        /// <param name="textView">The view to find the difference viewer for.</param>
        /// <returns>A difference viewer, if one exists.  Otherwise, <c>null</c>.</returns>
        IWpfDifferenceViewer TryGetViewerForTextView(ITextView textView);
    }
}
