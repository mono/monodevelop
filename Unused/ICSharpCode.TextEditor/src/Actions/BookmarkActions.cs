// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System.Drawing;
using System;

using MonoDevelop.TextEditor.Document;

namespace MonoDevelop.TextEditor.Actions 
{
	public class ToggleBookmark : AbstractEditAction
	{
		public override void Execute(TextArea textArea)
		{
			textArea.Document.BookmarkManager.ToggleMarkAt(textArea.Caret.Line);
			textArea.Document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.SingleLine, textArea.Caret.Line));
			textArea.Document.CommitUpdate();
		}
	}
	
	public class GotoPrevBookmark : AbstractEditAction
	{
		public override void Execute(TextArea textArea)
		{
			int lineNumber = textArea.Document.BookmarkManager.GetPrevMark(textArea.Caret.Line);
			if (lineNumber >= 0 && lineNumber < textArea.Document.TotalNumberOfLines) {
				textArea.Caret.Line = lineNumber;
			}
		}
	}
	
	public class GotoNextBookmark : AbstractEditAction
	{
		public override void Execute(TextArea textArea)
		{
			int lineNumber = textArea.Document.BookmarkManager.GetNextMark(textArea.Caret.Line);
			if (lineNumber >= 0 && lineNumber < textArea.Document.TotalNumberOfLines) {
				textArea.Caret.Line = lineNumber;
			}
		}
	}
	
	public class ClearAllBookmarks : AbstractEditAction
	{
		public override void Execute(TextArea textArea)
		{
			textArea.Document.BookmarkManager.Clear();
			textArea.Document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.WholeTextArea));
			textArea.Document.CommitUpdate();
		}
	}
}
