using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Platform;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Utilities;
using Microsoft.VisualStudio.Utilities;

namespace MonoDevelop.Ide.Text
{
    [Export]
	class TextViewImports
	{
		[Import]
		public ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

		[Import]
		public ITextBufferFactoryService TextBufferFactoryService { get; set; }

		[Import]
#if WINDOWS
		public ITextEditorFactoryService TextEditorFactoryService { get; set; }
#elif MAC
		public ICocoaTextEditorFactoryService TextEditorFactoryService { get; set; }
#endif
		[Import]
		public IFileToContentTypeService FileToContentTypeService { get; set; }

		[Import]
		public IContentTypeRegistryService ContentTypeRegistryService { get; set; }

		[Import]
		public IMimeToContentTypeRegistryService MimeToContentTypeRegistryService { get; set; }

		[ImportMany]
		public List<Lazy<ITextViewModelProvider, IContentTypeAndTextViewRoleMetadata>> TextViewModelProviders { get; set; }

		[Import]
		public IGuardedOperations GuardedOperations { get; set; }

		[Import]
		internal IEditorOptionsFactoryService EditorOptionsFactoryService { get; set; }
	}
}