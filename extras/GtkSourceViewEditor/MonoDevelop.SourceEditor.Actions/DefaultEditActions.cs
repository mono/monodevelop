using System;
using MonoDevelop.SourceEditor.Gui;

namespace MonoDevelop.SourceEditor.Actions
{
	// indents the next line using the FormattingStrategy
	public class Return : AbstractEditAction
	{
		public override void PreExecute (SourceEditorView sourceView)
		{
			PassToBase = true;
		}
		
		public override void Execute (SourceEditorView sourceView)
		{
			sourceView.FormatLine ();
			PassToBase = false;
		}
	}

	public class Tab : AbstractEditAction
	{
		public override void Execute (SourceEditorView sourceView)
		{
			if (!sourceView.InsertTemplate ())
				PassToBase = true;
		}
	}
}

