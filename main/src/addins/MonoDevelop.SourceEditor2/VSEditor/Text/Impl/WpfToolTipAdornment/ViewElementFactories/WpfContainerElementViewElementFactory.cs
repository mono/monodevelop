namespace Microsoft.VisualStudio.Text.AdornmentLibrary.ToolTip.Implementation
{
    using System;
    using System.ComponentModel.Composition;
    using System.Text;
    using UIElement = Xwt.Widget;
    using Microsoft.VisualStudio.Text.Adornments;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Utilities;
    using Xwt;

    [Export(typeof(IViewElementFactory))]
    [Name("default ContainerElement to UIElement")]
    [TypeConversion(from: typeof(ContainerElement), to: typeof(UIElement))]
    [Order]
    internal sealed class WpfContainerElementViewElementFactory : IViewElementFactory
    {
        [Import]
        internal IViewElementFactoryService viewElementFactoryService;

        public TView CreateViewElement<TView>(ITextView textView, object model) where TView : class
        {
            // Should never happen if the service's code is correct, but it's good to be paranoid.
            if (typeof(TView) != typeof(UIElement) || !(model is ContainerElement container))
            {
                throw new ArgumentException($"Invalid type conversion. Unsupported {nameof(model)} or {nameof(TView)} type");
            }

            Box containerControl;

            if (container.Style == ContainerElementStyle.Stacked)
            {
                containerControl = new VBox();
            }
            else
            {
                containerControl = new HBox();
            }

            containerControl.HorizontalPlacement = WidgetPlacement.Start;
            containerControl.VerticalPlacement = WidgetPlacement.Start;

            foreach (var element in container.Elements)
            {
                var convertedElement = this.viewElementFactoryService.CreateViewElement<UIElement>(textView, element);

                if (convertedElement != null)
                {
                    containerControl.PackStart(convertedElement);
                }
            }

            return containerControl as TView;
        }
    }
}
