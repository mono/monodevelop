namespace Microsoft.VisualStudio.Text.AdornmentLibrary.ToolTip.Implementation
{
    using System;
    using System.ComponentModel.Composition;
    using System.Text;
    using System.Windows;
    using System.Windows.Automation;
    using System.Windows.Controls;
    using Microsoft.VisualStudio.Text.Adornments;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Utilities;

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

            Panel containerControl;

            if (container.Style == ContainerElementStyle.Stacked)
            {
                containerControl = new StackPanel();
            }
            else
            {
                containerControl = new WrapPanel();
            }

            containerControl.HorizontalAlignment = HorizontalAlignment.Left;
            containerControl.VerticalAlignment = VerticalAlignment.Top;

            var automationNameBuffer = new StringBuilder();

            foreach (var element in container.Elements)
            {
                var convertedElement = this.viewElementFactoryService.CreateViewElement<UIElement>(textView, element);

                if (convertedElement != null)
                {
                    containerControl.Children.Add(convertedElement);

                    var elementAutomationName = convertedElement.GetValue(AutomationProperties.NameProperty)?.ToString();
                    if (elementAutomationName?.Length > 0)
                    {
                        automationNameBuffer.Append(elementAutomationName);
                        automationNameBuffer.Append('\r');
                        automationNameBuffer.Append('\n');
                    }
                }
            }

            containerControl.SetValue(AutomationProperties.NameProperty, automationNameBuffer.ToString());

            return containerControl as TView;
        }
    }
}
