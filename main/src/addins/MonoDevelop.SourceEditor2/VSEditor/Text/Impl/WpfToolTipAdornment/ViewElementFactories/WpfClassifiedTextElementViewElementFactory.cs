namespace Microsoft.VisualStudio.Text.AdornmentLibrary.ToolTip.Implementation
{
    using System;
    using System.ComponentModel.Composition;
    using System.Text;
    using Microsoft.VisualStudio.Text.Adornments;
    using Microsoft.VisualStudio.Text.Classification;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Utilities;
	using UIElement = Xwt.Widget;

    [Export(typeof(IViewElementFactory))]
    [Name("default ClassifiedTextElement to UIElement")]
    [TypeConversion(from: typeof(ClassifiedTextElement), to: typeof(UIElement))]
    [Order]
    internal sealed class WpfClassifiedTextElementViewElementFactory : IViewElementFactory
    {
        private readonly IClassificationTypeRegistryService classificationTypeRegistryService;
        private readonly ToolTipStyleFactory styleFactory;

        [ImportingConstructor]
        public WpfClassifiedTextElementViewElementFactory(
            IClassificationTypeRegistryService classificationTypeRegistryService,
            ToolTipStyleFactory styleFactory)
        {
            this.classificationTypeRegistryService = classificationTypeRegistryService
                ?? throw new ArgumentNullException(nameof(classificationTypeRegistryService));
            this.styleFactory = styleFactory
                ?? throw new ArgumentNullException(nameof(styleFactory));
        }

        public TView CreateViewElement<TView>(ITextView textView, object model) where TView : class
        {
            // Should never happen if the service's code is correct, but it's good to be paranoid.
            if (typeof(TView) != typeof(UIElement) || !(model is ClassifiedTextElement element))
            {
                throw new ArgumentException($"Invalid type conversion. Unsupported {nameof(model)} or {nameof(TView)} type");
            }
			

            var textBlock = new Xwt.Label();
			StringBuilder markup = new StringBuilder ();
			foreach (var run in element.Runs)
            {
                var textRunClassification = this.classificationTypeRegistryService.GetClassificationType(run.ClassificationTypeName);
				
                //var wpfRun = new Run()
                //{
                //    // Set colors from the specific classification type's text run properties.
                //    Background = viewTextRunProperties.BackgroundBrush,
                //    BaselineAlignment = viewTextRunProperties.BaselineAlignment,
                //    Foreground = viewTextRunProperties.ForegroundBrush,
                //    Text = run.Text,
                //    TextDecorations = viewTextRunProperties.TextDecorations,
                //    TextEffects = viewTextRunProperties.TextEffects,

                //    // Set font properties from Editor Tooltips category so we match other tooltips.
                //    FontSize = tooltipTextRunProperties.FontRenderingEmSize,
                //    FontFamily = tooltipTextRunProperties.Typeface.FontFamily,
                //    FontStretch = tooltipTextRunProperties.Typeface.Stretch,
                //    FontStyle = tooltipTextRunProperties.Typeface.Style,
                //    FontWeight = tooltipTextRunProperties.Typeface.Weight
                //};
				var classy = textRunClassification.Classification;
				var color = classy.GetHashCode ().ToString ("X");
				color = color.Substring (2);
				markup.Append ($"<span foreground=\"#{color}\">{run.Text}</span>");
			}
			textBlock.Markup = markup.ToString ();

			return textBlock as TView;
        }
    }
}
