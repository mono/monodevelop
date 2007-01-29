using System;
using MonoDevelop.Projects.Gui.Completion;

namespace MonoDevelop.Ide.Gui.Content
{
	public interface IExtensibleTextEditor: IEditableTextBuffer
	{
		// This method should return the terminator ITextEditorExtension that
		// will execute the default behavior (if any)
		ITextEditorExtension AttachExtension (ITextEditorExtension extension);
		ICompletionWidget GetCompletionWidget ();
	}
}
