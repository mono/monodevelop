namespace Microsoft.VisualStudio.Text.Implementation
{
    using Microsoft.VisualStudio.Utilities;

    /// <summary>
    /// Subset of ITextBufferFactoryService. Used to avoid having to mock the asset system in unit tests.
    /// </summary>
    internal interface IInternalTextBufferFactory
    {
        ITextBuffer CreateTextBuffer(string text, IContentType contentType);

        ITextBuffer CreateTextBuffer(string text, IContentType contentType, bool spurnGroup);

        IContentType TextContentType { get; }
        IContentType InertContentType { get; }
        IContentType ProjectionContentType { get; }
    }
}