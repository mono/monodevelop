using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
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
                textUndoHistoryRegistry);
        }
    }
}
