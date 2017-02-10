// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.EditorPrimitives.Implementation
{
    using System;
    using System.ComponentModel.Composition;

    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Editor;

    [Export(typeof(IEditorPrimitivesFactoryService))]
    internal sealed class EditorPrimitivesFactoryService : IEditorPrimitivesFactoryService
    {
        [Import]
        internal IViewPrimitivesFactoryService ViewPrimitivesFactory { get; set; }

        [Import]
        internal IBufferPrimitivesFactoryService BufferPrimitivesFactory { get; set; }

        #region IEditorPrimitivesFactoryService Members

        public IViewPrimitives GetViewPrimitives(ITextView textView)
        {
            return new ViewPrimitives(textView, ViewPrimitivesFactory); 
        }

        public IBufferPrimitives GetBufferPrimitives(ITextBuffer textBuffer)
        {
            return new BufferPrimitives(textBuffer, BufferPrimitivesFactory);
        }

        #endregion
    }
}
