using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace MonoDevelop.Ide.Text
{
    [Export]
	class TextViewImports
	{
		[Import]
		public ITextBufferFactoryService TextBufferFactoryService { get; set; }

		[Import]
		public ITextEditorFactoryService TextEditorFactoryService { get; set; }
	}
}