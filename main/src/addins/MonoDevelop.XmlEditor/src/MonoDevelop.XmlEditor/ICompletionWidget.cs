//
// Copy of the ICompletionWidget interface that was
// a part of MonoDevelop 0.12
//
// The code completion infrastructure was changed
// ready for MonoDevelop 1.0 and a quick fix to
// get the XML Editor working again is to use the old
// completion code.
//
// Copyright (C) 2004-2006 MonoDevelop Team
//

using System;
using MonoDevelop.Projects;
using Gtk;

namespace MonoDevelop.XmlEditor
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
