//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.BufferUndoManager.Implementation
{
    using System;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Projection;
    using Microsoft.VisualStudio.Text.Operations;
    using System.ComponentModel.Composition;

    [Export(typeof(ITextBufferUndoManagerProvider))]
    internal sealed class TextBufferUndoManagerProvider : ITextBufferUndoManagerProvider
    {
        [Import]
        internal ITextUndoHistoryRegistry _undoHistoryRegistry { get; set; }

        /// <summary>
        /// Provides an <see cref="ITextBufferUndoManager"/> for the given <paramref name="textBuffer"/>.
        /// </summary>
        /// <param name="textBuffer">The <see cref="ITextBuffer"/> to create the <see cref="ITextBufferUndoManager"/> for.</param>
        /// <returns>A cached <see cref="ITextBufferUndoManager"/> for the given <paramref name="textBuffer"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="textBuffer" /> is null.</exception>
        public ITextBufferUndoManager GetTextBufferUndoManager(ITextBuffer textBuffer)
        {
            // Validate
            if (textBuffer == null)
            {
                throw new ArgumentNullException("textBuffer");
            }

            // See if there was already a TextBufferUndoManager created for the given textBuffer, we only ever want to create one
            ITextBufferUndoManager cachedBufferUndoManager;
            if (!textBuffer.Properties.TryGetProperty<ITextBufferUndoManager>(typeof(ITextBufferUndoManager), out cachedBufferUndoManager))
            {
                cachedBufferUndoManager = new TextBufferUndoManager(textBuffer, _undoHistoryRegistry);
                textBuffer.Properties.AddProperty(typeof(ITextBufferUndoManager), cachedBufferUndoManager);
            }

            return cachedBufferUndoManager;
        }

        /// <summary>
        /// If the specified <paramref name="textBuffer" /> has an <see cref="ITextBufferUndoManager" /> associated with it, remove it.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="textBuffer" /> is null.</exception>
        public void RemoveTextBufferUndoManager(ITextBuffer textBuffer)
        {
            // Validate
            if (textBuffer == null)
            {
                throw new ArgumentNullException("textBuffer");
            }

            ITextBufferUndoManager cachedBufferUndoManager;
            if (textBuffer.Properties.TryGetProperty<ITextBufferUndoManager>(typeof(ITextBufferUndoManager), out cachedBufferUndoManager))
            {
                // Dispose() so it stops listening to Changed events on the buffer.
                IDisposable disposableBufferUndoManager = cachedBufferUndoManager as IDisposable;
                if (disposableBufferUndoManager != null)
                {
                    disposableBufferUndoManager.Dispose();
                }

                // Remove from cache.
                textBuffer.Properties.RemoveProperty(typeof(ITextBufferUndoManager));
            }
        }
    }
}
