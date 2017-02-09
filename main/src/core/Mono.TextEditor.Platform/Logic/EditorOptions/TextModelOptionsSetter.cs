// Copyright (c) Microsoft Corporation
// All rights reserved

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Utilities;

namespace Microsoft.VisualStudio.Text.Editor.Implementation
{
    [Export(typeof(ITextModelOptionsSetter))]
    public class TextModelOptionsSetter : ITextModelOptionsSetter
    {
        /// <summary>
        /// Propagate options from editor options to the text model (editor options are not visible at the text model level).
        /// </summary>
        /// <param name="options"></param>
        public void SetTextModelOptions(IEditorOptions options)
        {
            TextModelOptions.CompressedStorageFileSizeThreshold = options.GetOptionValue(TextModelEditorOptions.CompressedStorageFileSizeThresholdOptionId);
            TextModelOptions.CompressedStoragePageSize = options.GetOptionValue(TextModelEditorOptions.CompressedStoragePageSizeOptionId);
            TextModelOptions.CompressedStorageMaxLoadedPages = options.GetOptionValue(TextModelEditorOptions.CompressedStorageMaxLoadedPagesOptionId);
            TextModelOptions.CompressedStorageRetainWeakReferences = options.GetOptionValue(TextModelEditorOptions.CompressedStorageRetainWeakReferencesOptionId);
            TextModelOptions.DiffSizeThreshold = options.GetOptionValue(TextModelEditorOptions.DiffSizeThresholdOptionId);
        }
    }
}
