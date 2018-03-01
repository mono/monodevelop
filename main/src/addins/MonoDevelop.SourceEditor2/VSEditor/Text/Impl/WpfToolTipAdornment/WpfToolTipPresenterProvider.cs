namespace Microsoft.VisualStudio.Text.AdornmentLibrary.ToolTip.Implementation
{
    using System.ComponentModel.Composition;
    using Microsoft.VisualStudio.Text.Adornments;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Utilities;

    [Export(typeof(IToolTipPresenterFactory))]
    [Name("default")]
    [ContentType("text")]
    [Order]
    internal sealed class WpfToolTipPresenterProvider : IToolTipPresenterFactory
    {
        [Import]
        internal IViewElementFactoryService viewElementFactoryService;

        [Import]
        internal IObscuringTipManager obscuringTipManager;

        [Import]
        internal ToolTipStyleFactory styleFactory;

        public IToolTipPresenter Create(ITextView textView, ToolTipParameters parameters)
        {
            if (parameters.TrackMouse)
            {
                return new MouseTrackingWpfToolTipPresenter(
                    this.viewElementFactoryService,
                    this.obscuringTipManager,
                    textView,
                    parameters,
                    this.styleFactory.Style);
            }
            else
            {
                return new SpanTrackingWpfToolTipPresenter(
                    this.viewElementFactoryService,
                    this.obscuringTipManager,
                    textView,
                    parameters,
                    this.styleFactory.Style);
            }
        }
    }
}
