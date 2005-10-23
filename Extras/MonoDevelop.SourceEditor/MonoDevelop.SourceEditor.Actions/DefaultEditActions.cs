using System;
using MonoDevelop.SourceEditor.Gui;

namespace MonoDevelop.SourceEditor.Actions
{	
	public class ControlSpace : AbstractEditAction
	{
		public override void Execute (SourceEditorView sourceView)
		{
			sourceView.TriggerCodeComplete ();
		}
	}

	public class DeleteLine : AbstractEditAction
	{
		public override void Execute (SourceEditorView sourceView)
		{
			sourceView.DeleteLine ();
		}
	}

	public class Dot : AbstractEditAction
	{
		public override void PreExecute (SourceEditorView sourceView)
		{
			PassToBase = true;
		}
		
		public override void Execute (SourceEditorView sourceView)
		{
			sourceView.ShowCodeCompletion ('.');
			PassToBase = false;
		}
	}

	public class End : AbstractEditAction
	{
		public override void Execute (SourceEditorView sourceView)
		{
			if (!sourceView.GotoSelectionEnd ())
				PassToBase = true;
		}
	}

	public class F1 : AbstractEditAction
	{
		public override void Execute (SourceEditorView sourceView)
		{
			if (!sourceView.MonodocResolver ())
				PassToBase = true;
		}
	}		

	public class Home : AbstractEditAction
	{
		public override void Execute (SourceEditorView sourceView)
		{
			if (!sourceView.GotoSelectionStart ())
				PassToBase = true;
		}
	}

	public class ScrollUp : AbstractEditAction
	{
		public override void Execute (SourceEditorView sourceView)
		{
			sourceView.ScrollUp ();
		}
	}

	public class ScrollDown : AbstractEditAction
	{
		public override void Execute (SourceEditorView sourceView)
		{
			sourceView.ScrollDown ();
		}
	}

	public class ShiftTab : AbstractEditAction
	{
		public override void Execute (SourceEditorView sourceView)
		{
			if (!sourceView.IndentSelection (true))
				PassToBase = true;
		}
	}

	public class Space : AbstractEditAction
	{
		public override void PreExecute (SourceEditorView sourceView)
		{
			PassToBase = true;
		}
		
		public override void Execute (SourceEditorView sourceView)
		{
			sourceView.ShowCodeCompletion (' ');
			PassToBase = false;
		}
	}
		
	public class Tab : AbstractEditAction
	{
		public override void Execute (SourceEditorView sourceView)
		{
			if (!sourceView.IndentSelection (false) && !sourceView.InsertTemplate ())
				PassToBase = true;
		}
	}		
}

