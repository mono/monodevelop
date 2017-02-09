using System.Windows;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.Text.Differencing
{
    /// <summary>
    /// A WPF-specific version of an <see cref="IDifferenceViewer"/>, which provides access to the
    /// <see cref="VisualElement" /> used to host the viewer and the various text view hosts as <see cref="IWpfTextViewHost" />.
    /// </summary>
    public interface IWpfDifferenceViewer : IDifferenceViewer
    {
        /// <summary>
        /// Initialize the DifferenceViewer, hooking it to the specified buffer and using the callback to create the text view hosts.
        /// </summary>
        /// <param name="differenceBuffer"></param>
        /// <param name="createTextViewHost"></param>
        /// <param name="parentOptions"></param>
        /// <remarks>
        /// <para>This method should only be called if the CreateUninitializedDifferenceView method on the <see cref="IWpfDifferenceViewerFactoryService"/> is used. Otherwise, it is
        /// called by the factory.</para>
        /// <para>The viewer does not have to be initialized immediately. You can wait until the Loaded event on the VisualElement.</para>
        /// </remarks>
        void Initialize(IDifferenceBuffer differenceBuffer,
                        CreateTextViewHostCallback createTextViewHost,
                        IEditorOptions parentOptions = null);

        /// <summary>
        /// Has this viewer been initialized?
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// The view for displaying <see cref="DifferenceViewMode.Inline"/> differences.
        /// </summary>
        /// <remarks>Will never be <c>null</c>, but will only be visible when <see cref="IDifferenceViewer.ViewMode"/>
        /// is set to <see cref="DifferenceViewMode.Inline"/>.</remarks>
        new IWpfTextView InlineView { get; }

        /// <summary>
        /// The view for displaying the left buffer for <see cref="DifferenceViewMode.SideBySide"/> differences.
        /// </summary>
        /// <remarks>Will never be <c>null</c>, but will only be visible when <see cref="IDifferenceViewer.ViewMode"/>
        /// is set to <see cref="DifferenceViewMode.SideBySide"/>.</remarks>
        new IWpfTextView LeftView { get; }

        /// <summary>
        /// The view for displaying the right buffer for <see cref="DifferenceViewMode.SideBySide"/> differences.
        /// </summary>
        /// <remarks>Will never be <c>null</c>, but will only be visible when <see cref="IDifferenceViewer.ViewMode"/>
        /// is set to <see cref="DifferenceViewMode.SideBySide"/>.</remarks>
        new IWpfTextView RightView { get; }

        /// <summary>
        /// The host for displaying <see cref="DifferenceViewMode.Inline"/> differences.
        /// </summary>
        /// <remarks>Will never be <c>null</c>, but will only be visible when <see cref="IDifferenceViewer.ViewMode"/>
        /// is set to <see cref="DifferenceViewMode.Inline"/>.</remarks>
        IWpfTextViewHost InlineHost { get; }

        /// <summary>
        /// The host for displaying the left buffer for <see cref="DifferenceViewMode.SideBySide"/> differences.
        /// </summary>
        /// <remarks>Will never be <c>null</c>, but will only be visible when <see cref="IDifferenceViewer.ViewMode"/>
        /// is set to <see cref="DifferenceViewMode.SideBySide"/>.</remarks>
        IWpfTextViewHost LeftHost { get; }

        /// <summary>
        /// The host for displaying the right buffer for <see cref="DifferenceViewMode.SideBySide"/> differences.
        /// </summary>
        /// <remarks>Will never be <c>null</c>, but will only be visible when <see cref="IDifferenceViewer.ViewMode"/>
        /// is set to <see cref="DifferenceViewMode.SideBySide"/>.</remarks>
        IWpfTextViewHost RightHost { get; }

        /// <summary>
        /// The visual element of this viewer.
        /// </summary>
        FrameworkElement VisualElement { get; }
    }
}
