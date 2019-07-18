namespace Microsoft.VisualStudio.TextMate.VSWindows
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using Microsoft.VisualStudio.Editor;
    using Microsoft.VisualStudio.Text;

    /// <summary>
    /// Defines a mechanism for publicly sharing common editor infrastructure such
    /// as colorization, match highlighting, word completion, etc.
    /// </summary>
    [Export (typeof (ICommonEditorAssetServiceFactory))]
    [Export (typeof (CommonEditorAssetServiceFactory))]
    public sealed class CommonEditorAssetServiceFactory : ICommonEditorAssetServiceFactory
    {
        public ICommonEditorAssetService GetOrCreate(ITextBuffer textBuffer)
            => new CommonEditorAssetService(this, textBuffer);
    }
}
