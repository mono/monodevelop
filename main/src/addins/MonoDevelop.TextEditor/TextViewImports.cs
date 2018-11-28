using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Platform;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
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
		public ITextEditorFactoryService TextEditorFactoryService { get; set; }

		[Import]
		public IFileToContentTypeService FileToContentTypeService { get; set; }

		[Import]
		public IContentTypeRegistryService ContentTypeRegistryService { get; set; }

		[Import]
		public IMimeToContentTypeRegistryService MimeToContentTypeRegistryService { get; set; }
	}
}