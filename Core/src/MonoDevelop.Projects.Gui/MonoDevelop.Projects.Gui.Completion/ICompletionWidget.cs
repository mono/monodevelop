
using System;
using MonoDevelop.Projects;
using Gtk;

namespace MonoDevelop.Projects.Gui.Completion
{
	public interface ICompletionWidget
	{
		int TextLength { get; }
		string GetText (int startOffset, int endOffset);
		char GetChar (int offset);
		Gtk.Style GtkStyle { get; }
		void InsertAtCursor (string text);

		ICodeCompletionContext CreateCodeCompletionContext (int triggerOffset);
		string GetCompletionText (ICodeCompletionContext ctx);
		void SetCompletionText (ICodeCompletionContext ctx, string partial_word, string complete_word);
		
		event EventHandler CompletionContextChanged;
	}
	
	public interface ICodeCompletionContext
	{
		int TriggerOffset { get; }
		int TriggerLine { get; }
		int TriggerLineOffset { get; }
		int TriggerXCoord { get; }
		int TriggerYCoord { get; }
		int TriggerTextHeight { get; }
	}
	
	public class CodeCompletionContext: ICodeCompletionContext
	{
		int triggerOffset;
		int triggerLine;
		int triggerLineOffset;
		int triggerXCoord;
		int triggerYCoord;
		int triggerTextHeight;
		
		public int TriggerOffset {
			get { return triggerOffset; }
			set { triggerOffset = value; }
		}
		
		public int TriggerLine {
			get { return triggerLine; }
			set { triggerLine = value; }
		}
		
		public int TriggerLineOffset {
			get { return triggerLineOffset; }
			set { triggerLineOffset = value; }
		}
		
		public int TriggerXCoord {
			get { return triggerXCoord; }
			set { triggerXCoord = value; }
		}
		
		public int TriggerYCoord {
			get { return triggerYCoord; }
			set { triggerYCoord = value; }
		}
		
		public int TriggerTextHeight {
			get { return triggerTextHeight; }
			set { triggerTextHeight = value; }
		}
	}
}
