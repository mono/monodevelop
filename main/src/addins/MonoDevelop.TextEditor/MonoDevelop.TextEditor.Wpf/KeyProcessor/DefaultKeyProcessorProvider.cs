//
// Copyright (c) Microsoft Corp. (https://www.microsoft.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace MonoDevelop.Ide.Text
{
    [Export(typeof(IKeyProcessorProvider))]
    [Name("DefaultKeyProcessor")]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    internal sealed class DefaultKeyProcessorProvider : IKeyProcessorProvider
    {
        [Import]
        private IEditorOperationsFactoryService editorOperationsProvider = null;

		[Import]
		private IEditorCommandHandlerServiceFactory editorCommandHandlerServiceFactory = null;

		[Import]
		private ITextUndoHistoryRegistry textUndoHistoryRegistry = null;

        /// <summary>
        /// Creates a new key processor provider for the given WPF text view host
        /// </summary>
        /// <param name="wpfTextView">WPF-based text view to create key processor for</param>
        /// <returns>A valid key processor</returns>
        public KeyProcessor GetAssociatedProcessor(IWpfTextView wpfTextView)
        {
            if (wpfTextView == null)
            {
                throw new ArgumentNullException("wpfTextView");
            }

            return new DefaultKeyProcessor(
                wpfTextView,
                editorOperationsProvider.GetEditorOperations(wpfTextView),
                textUndoHistoryRegistry,
                editorCommandHandlerServiceFactory.GetService(wpfTextView));
        }
    }
}
