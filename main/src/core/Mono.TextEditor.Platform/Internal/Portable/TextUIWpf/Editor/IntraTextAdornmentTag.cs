namespace Microsoft.VisualStudio.Text.Editor
{
    using System;
    using System.Windows;
    using Microsoft.VisualStudio.Text.Formatting;
    using Microsoft.VisualStudio.Text.Tagging;

    /// <summary>
    /// Represents a tag that provides adornments to be displayed as interspersed with text.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The tag's span will be elided from the view and that text will be replaced by the adornment provided by this tag.
    /// </para>
    /// <para>
    /// The aggregator for these tags is created on a per-view basis and handles the
    /// production of <see cref="SpaceNegotiatingAdornmentTag"/> objects, text hiding, and
    /// the positioning of adornments on the adornment layer.
    /// </para>
    /// <para>
    /// This will only work for views that have the
    /// <see cref="F:Microsoft.VisualStudio.Text.Editor.PredefinedTextViewRoles.Structured"/> view role.
    /// </para>
    /// </remarks>
    public class IntraTextAdornmentTag : ITag
    {
        /// <summary>
        /// Initializes a new instance of a <see cref="IntraTextAdornmentTag"/>.
        /// </summary>
        /// <param name="adornment">The adornment to be displayed at tag's position. Must not be null.</param>
        /// <param name="removalCallback">Called when adornment is removed from the view. May be null.</param>
        /// <param name="topSpace">The amount of space needed between the top of the text in the <see cref="ITextViewLine"/> and the top of the <see cref="ITextViewLine"/>.</param>
        /// <param name="baseline">The baseline of the space-negotiating adornment.</param>
        /// <param name="textHeight">The height of the text portion of the space-negotiating adornment.</param>
        /// <param name="bottomSpace">The amount of space needed between the bottom of the text in the <see cref="ITextViewLine"/> and the bottom of the <see cref="ITextViewLine"/>.</param>
        /// <param name="affinity">The affinity of the adornment. Should be null iff the adornment has a non-zero-length span at the view's text buffer.</param>
        public IntraTextAdornmentTag(UIElement adornment, AdornmentRemovedCallback removalCallback,
            double? topSpace, double? baseline, double? textHeight, double? bottomSpace, PositionAffinity? affinity)
        {
            if (adornment == null)
                throw new ArgumentNullException("adornment");

            Adornment = adornment;
            RemovalCallback = removalCallback;

            this.TopSpace = topSpace;
            this.Baseline = baseline;
            this.TextHeight = textHeight;
            this.BottomSpace = bottomSpace;

            this.Affinity = affinity;
        }

        /// <summary>
        /// Initializes a new instance of a <see cref="IntraTextAdornmentTag"/>.
        /// </summary>
        /// <param name="adornment">The adornment to be displayed at tag's position. Must not be null.</param>
        /// <param name="removalCallback">Called when adornment is removed from the view. May be null.</param>
        /// <param name="affinity">The affinity of the adornment. Should be null iff the adornment has a zero-length span at the view's text buffer.</param>
        public IntraTextAdornmentTag(UIElement adornment, AdornmentRemovedCallback removalCallback, PositionAffinity? affinity)
            :this(adornment, removalCallback, null, null, null, null, affinity)
        { }

        /// <summary>
        /// Initializes a new instance of a <see cref="IntraTextAdornmentTag"/>.
        /// </summary>
        /// <param name="adornment">The adornment to be displayed at tag's position. Must not be null.</param>
        /// <param name="removalCallback">Called when adornment is removed from the view. May be null.</param>
        /// <remarks>This constructor should only be used for adornments that replace text in the view's text buffer.</remarks>
        public IntraTextAdornmentTag(UIElement adornment, AdornmentRemovedCallback removalCallback)
            : this(adornment, removalCallback, null, null, null, null, null)
        { }

        /// <summary>
        /// Gets the adornment to be displayed at the position of the tag. It must not be null.
        /// </summary>
        /// <remarks>
        /// This adornment will be added to the view. Note that WPF elements can only be parented in a single
        /// place in the visual tree. Therefore these adornment instances should not be added to any other WPF UI.
        /// </remarks>
        public UIElement Adornment { get; private set; }

        /// <summary>
        /// Called when adornment is removed from the view. It may be null.
        /// </summary>
        public AdornmentRemovedCallback RemovalCallback { get; private set; }

        /// <summary>
        /// Gets the amount of space needed between the top of the text in the <see cref="ITextViewLine"/> and the top of the <see cref="ITextViewLine"/>.
        /// </summary>
        public double? TopSpace { get; private set; }

        /// <summary>
        /// Gets the baseline of the space-negotiating adornment.
        /// </summary>
        public double? Baseline { get; private set; }

        /// <summary>
        /// Gets the height of the text portion of the space-negotiating adornment.
        /// </summary>
        public double? TextHeight { get; private set; }

        /// <summary>
        /// Gets the amount of space needed between the bottom of the text in the <see cref="ITextViewLine"/> and the bottom of the <see cref="ITextViewLine"/>.
        /// </summary>
        public double? BottomSpace { get; private set; }

        /// <summary>
        /// Gets the <see cref="PositionAffinity"/> of the space-negotiating adornment.
        /// </summary>
        /// <remarks>
        /// Should be non-null for tags with zero length spans (at the edit buffer level of the view's buffer graph) and only for those tags.
        /// </remarks>
        public PositionAffinity? Affinity { get; private set; }
    }
}
