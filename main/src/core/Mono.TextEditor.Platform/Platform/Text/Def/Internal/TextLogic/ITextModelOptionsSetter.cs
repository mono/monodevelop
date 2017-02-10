namespace Microsoft.VisualStudio.Text.Editor
{
    /// <summary>
    /// A service that propagates <see cref="IEditorOptions"/> to the text model component.
    /// This is never intended to be part of the public API -- we already have the 
    /// editor options facilities for that. This is inteded to allow hosting code (e.g. the
    /// Visual Studio editor package) to propagate options down to the text model,
    /// where EditorOptions isn't visible.
    /// </summary>
    /// <remarks>This is a MEF component part, and should be imported as follows:
    /// [Import]
    /// ITextModelOptionsSetter setter = null;
    /// </remarks>
    public interface ITextModelOptionsSetter
    {
        /// <summary>
        /// Extract options useful to the text model layer and expose them in
        /// that layer.
        /// </summary>
        void SetTextModelOptions(IEditorOptions options);
    }
}