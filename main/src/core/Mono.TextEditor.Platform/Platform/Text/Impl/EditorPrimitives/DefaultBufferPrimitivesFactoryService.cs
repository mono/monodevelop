//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.EditorPrimitives.Implementation
{
    using System.ComponentModel.Composition;

    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.Operations;

    [Export(typeof(IBufferPrimitivesFactoryService))]
    internal sealed class DefaultBufferPrimitivesFactoryService : IBufferPrimitivesFactoryService
    {
        [Import]
        internal IEditorOptionsFactoryService EditorOptionsFactoryService { get; set; }

        [Import]
        internal ITextSearchService TextSearchService { get; set; }

        [Import]
        internal ITextStructureNavigatorSelectorService TextStructureNavigatorSelectorService { get; set; }

        #region IBufferPrimitivesFactoryService Members

        public TextBuffer CreateTextBuffer(ITextBuffer textBuffer)
        {
            TextBuffer textBufferPrimitive = null;
            if (!textBuffer.Properties.TryGetProperty<TextBuffer>(EditorPrimitiveIds.BufferPrimitiveId, out textBufferPrimitive))
            {
                textBufferPrimitive = new DefaultBufferPrimitive(textBuffer, this);

                textBuffer.Properties.AddProperty(EditorPrimitiveIds.BufferPrimitiveId, textBufferPrimitive);
            }

            return textBufferPrimitive;
        }

        public TextPoint CreateTextPoint(TextBuffer textBuffer, int position)
        {
            return new DefaultTextPointPrimitive(
                textBuffer,
                position,
                TextSearchService,
                EditorOptionsFactoryService.GetOptions(textBuffer.AdvancedTextBuffer),
                TextStructureNavigatorSelectorService.GetTextStructureNavigator(textBuffer.AdvancedTextBuffer),
                this);
        }

        public TextRange CreateTextRange(TextBuffer textBuffer, TextPoint startPoint, TextPoint endPoint)
        {
            return new DefaultTextRangePrimitive(startPoint, endPoint, EditorOptionsFactoryService);
        }

        #endregion
    }
}
