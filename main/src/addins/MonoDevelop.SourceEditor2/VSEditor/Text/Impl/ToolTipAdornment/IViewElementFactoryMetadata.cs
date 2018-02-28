namespace Microsoft.VisualStudio.Text.AdornmentLibrary.ToolTip.Implementation
{
    using Microsoft.VisualStudio.Utilities;

    public interface IViewElementFactoryMetadata : IOrderable
    {
        string FromFullName { get; }

        string ToFullName { get; }
    }
}
