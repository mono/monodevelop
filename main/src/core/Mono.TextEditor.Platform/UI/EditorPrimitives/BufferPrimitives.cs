// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.EditorPrimitives.Implementation
{
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Editor;

    internal sealed class BufferPrimitives : IBufferPrimitives
    {
        private TextBuffer _textBuffer;

        public BufferPrimitives(ITextBuffer textBuffer, IBufferPrimitivesFactoryService bufferPrimitivesFactory)
        {
            _textBuffer = bufferPrimitivesFactory.CreateTextBuffer(textBuffer);
        }
        #region IBufferPrimitives Members

        public TextBuffer Buffer
        {
            get { return _textBuffer; }
        }

        #endregion
    }
}
