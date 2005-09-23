// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Drawing;

using MonoDevelop.TextEditor.Document;

namespace MonoDevelop.TextEditor.Actions 
{
	public class Home : AbstractEditAction
	{
		public override void Execute(TextArea textArea)
		{
			LineSegment curLine = textArea.Document.GetLineSegment(textArea.Caret.Line);
			
			if (TextUtilities.IsEmptyLine(textArea.Document, curLine)) {
				if (textArea.Caret.Column != 0) {
					textArea.Caret.Column = 0;
				} else  {
					textArea.Caret.Column = curLine.Length;
				}
			} else {
				int firstCharOffset = TextUtilities.GetFirstNonWSChar(textArea.Document, curLine.Offset);
				int firstCharColumn = firstCharOffset - curLine.Offset;
				
				if (textArea.Caret.Column == firstCharColumn) {
					textArea.Caret.Column  = 0;
				} else {
					textArea.Caret.Column  = firstCharColumn;
				}
			}
			
			textArea.SetDesiredColumn();
		}
	}
	
	public class End : AbstractEditAction
	{
		public override void Execute(TextArea textArea)
		{
			LineSegment curLine = textArea.Document.GetLineSegment(textArea.Caret.Line);
			if (textArea.Caret.Column != curLine.Length) {
				textArea.Caret.Column = curLine.Length;
				textArea.SetDesiredColumn();
			}
		}
	}
	
	
	public class MoveToStart : AbstractEditAction
	{
		public override void Execute(TextArea textArea)
		{
			if (textArea.Caret.Line != 0 || textArea.Caret.Column != 0) {
				textArea.Caret.Position = new Point(0, 0);
				textArea.SetDesiredColumn();
			}
		}
	}
	
	
	public class MoveToEnd : AbstractEditAction
	{
		public override void Execute(TextArea textArea)
		{
			Point endPos = textArea.Document.OffsetToPosition(textArea.Document.TextLength);
			if (textArea.Caret.Position != endPos) {
				textArea.Caret.Position = endPos;
				textArea.SetDesiredColumn();
			}
		}
	}
}
