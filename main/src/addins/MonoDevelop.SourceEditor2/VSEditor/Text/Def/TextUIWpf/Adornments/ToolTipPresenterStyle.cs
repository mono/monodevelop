namespace Microsoft.VisualStudio.Text.Adornments
{
    using Xwt.Drawing;
    using Microsoft.VisualStudio.Text.Editor;

    ///<summary>
    /// Defines a set of properties that will be used to style the default WPF ToolTip presenter.
    ///</summary>
    /// <remarks>
    /// This is a MEF component part, and should be exported with the following attributes:
    /// [Export(typeof(ToolTipPresenterStyle))]
    /// [Name]
    /// [Order]
    /// All exports of this component part should be ordered before the "default" ToolTip presenter style.  At a minimum, this
    /// means adding [Order(Before="default")] to the export metadata.
    /// </remarks>
    public class ToolTipPresenterStyle
    {
        /// <summary>
        /// Gets a string that identifies the appearance category for the <see cref="ITextView"/>
        /// displayed in the default ToolTip presenter.
        /// </summary>
        public virtual string AppearanceCategory { get; protected set; }

        /// <summary>
        /// Gets a <see cref="Color"/> that will be used to paint the borders in the ToolTip presenter.
        /// </summary>
        public virtual Color BorderBrush { get; protected set; }

        /// <summary>
        /// Gets a <see cref="Color"/> that will be used to paint the background of the ToolTip presenter.
        /// </summary>
        public virtual Color BackgroundBrush { get; protected set; }
    }
}
