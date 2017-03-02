//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
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
