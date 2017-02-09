namespace Microsoft.VisualStudio.Text.Editor
{
    using System.Windows;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Editor;

    /// <summary>
    /// Defines an element in an adornment layer.
    /// </summary>
    public interface IAdornmentLayerElement
    {
        /// <summary>
        /// Gets the snapshot span that is associated with the adornment.
        /// </summary>
        SnapshotSpan? VisualSpan { get; }
        /// <summary>
        /// Gets the positioning behavior of the adornment.
        /// </summary>
        AdornmentPositioningBehavior Behavior { get; }
        /// <summary>
        /// Gets the adornment.
        /// </summary>
        UIElement Adornment { get; }

        /// <summary>
        /// Gets the tag associated with the adornment.
        /// </summary>
        object Tag { get; }
        /// <summary>
        /// Defines the behavior when an adornment has been removed.
        /// </summary>
        AdornmentRemovedCallback RemovedCallback { get; }
    }
}
