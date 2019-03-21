using System;

namespace Microsoft.VisualStudio.Text.Editor
{
	[Obsolete ("Use the Microsoft.VisualStudio.Text.Editor APIs")]
	public interface ITextEditorInitializationService
	{
		ITextView CreateTextView (MonoDevelop.Ide.Editor.TextEditor textEditor);
	}
}