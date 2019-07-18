namespace Microsoft.VisualStudio.TextMate.VSWindows
{
    using System;
    using Microsoft.VisualStudio.Editor;
    using Microsoft.VisualStudio.Text;

    internal sealed class CommonEditorAssetService : ICommonEditorAssetService
    {
        private readonly CommonEditorAssetServiceFactory factory;
        private readonly ITextBuffer textBuffer;

        public CommonEditorAssetService(CommonEditorAssetServiceFactory factory, ITextBuffer textBuffer)
        {
            this.factory = factory;
            this.textBuffer = textBuffer;
        }

        public T FindAsset<T>(Predicate<ICommonEditorAssetMetadata> isMatch = null) where T : class
        {
            var candidates = MonoDevelop.Ide.Composition.CompositionManager.Instance.ExportProvider
                .GetExports<T, ICommonEditorAssetMetadata> ();

            foreach (var candidate in candidates)
            {
                if (StringComparer.OrdinalIgnoreCase.Equals (candidate.Metadata.Name, CommonEditorConstants.AssetName)
                    && (isMatch?.Invoke (candidate.Metadata) ?? true))
                {
                    return candidate.Value;
                }
            }

            return null;
        }
    }
}
