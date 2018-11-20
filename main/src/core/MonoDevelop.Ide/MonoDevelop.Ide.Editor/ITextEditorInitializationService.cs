namespace Microsoft.VisualStudio.Text.Editor
{
	public interface ITextEditorInitializationService
	{
		ITextView CreateTextView (MonoDevelop.Ide.Editor.TextEditor textEditor);
	}
}