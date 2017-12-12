namespace Microsoft.VisualStudio.Text.AdornmentLibrary.ToolTip.Implementation
{
    using System;
    using System.ComponentModel.Composition;
    using System.Text;
    using System.Windows;
    using System.Windows.Automation;
    using System.Windows.Controls;
    using System.Windows.Documents;
    using System.Windows.Media.TextFormatting;
    using Microsoft.VisualStudio.Text.Adornments;
    using Microsoft.VisualStudio.Text.Classification;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Utilities;

    [Export(typeof(IViewElementFactory))]
    [Name("default ClassifiedTextElement to UIElement")]
    [TypeConversion(from: typeof(ClassifiedTextElement), to: typeof(UIElement))]
    [Order]
    internal sealed class WpfClassifiedTextElementViewElementFactory : IViewElementFactory
    {
        private readonly IClassificationTypeRegistryService classificationTypeRegistryService;
        private readonly IClassificationFormatMapService classificationFormatMapService;
        private readonly ToolTipStyleFactory styleFactory;

        [ImportingConstructor]
        public WpfClassifiedTextElementViewElementFactory(
            IClassificationTypeRegistryService classificationTypeRegistryService,
            IClassificationFormatMapService classificationFormatMapService,
            ToolTipStyleFactory styleFactory)
        {
            this.classificationTypeRegistryService = classificationTypeRegistryService
                ?? throw new ArgumentNullException(nameof(classificationTypeRegistryService));
            this.classificationFormatMapService = classificationFormatMapService
                ?? throw new ArgumentNullException(nameof(classificationFormatMapService));
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

            var viewClassificationFormatMap = this.classificationFormatMapService.GetClassificationFormatMap(textView);
            var tooltipClassificationFormatMap = this.classificationFormatMapService.GetClassificationFormatMap(this.styleFactory.Style.AppearanceCategory);
            var tooltipTextRunProperties = tooltipClassificationFormatMap?.DefaultTextProperties;

            var textBlock = new TextBlock();
            var automationNameBuffer = new StringBuilder();

            foreach (var run in element.Runs)
            {
                var textRunClassification = this.classificationTypeRegistryService.GetClassificationType(run.ClassificationTypeName);

                TextRunProperties viewTextRunProperties = viewClassificationFormatMap.DefaultTextProperties;

                if (textRunClassification != null)
                {
                    viewTextRunProperties = viewClassificationFormatMap.GetTextProperties(textRunClassification);
                }

                var wpfRun = new Run()
                {
                    // Set colors from the specific classification type's text run properties.
                    Background = viewTextRunProperties.BackgroundBrush,
                    BaselineAlignment = viewTextRunProperties.BaselineAlignment,
                    Foreground = viewTextRunProperties.ForegroundBrush,
                    Text = run.Text,
                    TextDecorations = viewTextRunProperties.TextDecorations,
                    TextEffects = viewTextRunProperties.TextEffects,

                    // Set font properties from Editor Tooltips category so we match other tooltips.
                    FontSize = tooltipTextRunProperties.FontRenderingEmSize,
                    FontFamily = tooltipTextRunProperties.Typeface.FontFamily,
                    FontStretch = tooltipTextRunProperties.Typeface.Stretch,
                    FontStyle = tooltipTextRunProperties.Typeface.Style,
                    FontWeight = tooltipTextRunProperties.Typeface.Weight
                };

                textBlock.Inlines.Add(wpfRun);
                automationNameBuffer.Append(run.Text);
            }

            textBlock.SetValue(AutomationProperties.NameProperty, automationNameBuffer.ToString());

            return textBlock as TView;
        }
    }
}
