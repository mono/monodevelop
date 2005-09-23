
using System;
using MonoDevelop.Internal.Project;
using Gtk;

namespace MonoDevelop.Gui.Completion
{

	public interface ICompletionWidget
	{
		string Text { get; }
		int TextLength { get; }
		string GetText (int startOffset, int endOffset);
		char GetChar (int offset);

		string CompletionText { get; }

		void SetCompletionText (string partial_word, string complete_word);

		void InsertAtCursor (string text);

		int TriggerOffset { get; }
		int TriggerLine { get; }
		int TriggerLineOffset { get; }

		int TriggerXCoord { get; }
		int TriggerYCoord { get; }
		int TriggerTextHeight { get; }

		Gtk.Style GtkStyle { get; }
	}
}
