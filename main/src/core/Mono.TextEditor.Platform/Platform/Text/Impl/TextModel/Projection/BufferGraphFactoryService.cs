// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Projection.Implementation
{
    using System;
    using System.ComponentModel.Composition;
    using Microsoft.VisualStudio.Text.Utilities;

    [Export(typeof(IBufferGraphFactoryService))]
    internal sealed class BufferGraphFactoryService : IBufferGraphFactoryService
    {
        [Import]
        internal GuardedOperations GuardedOperations = null;

        public IBufferGraph CreateBufferGraph(ITextBuffer textBuffer)
        {
            if (textBuffer == null)
            {
                throw new ArgumentNullException("textBuffer");
            }
            return textBuffer.Properties.GetOrCreateSingletonProperty<BufferGraph>(() => (new BufferGraph(textBuffer, GuardedOperations)));
        }
    }
}
