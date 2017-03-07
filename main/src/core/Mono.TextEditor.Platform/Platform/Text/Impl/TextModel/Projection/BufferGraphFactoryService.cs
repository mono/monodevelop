//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
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
