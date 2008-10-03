// 
// TextFileNavigationPoint.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;

namespace MonoDevelop.Ide.Gui
{
	
	public class TextFileNavigationPoint : FileNavigationPoint
	{
		int line;
		
		public TextFileNavigationPoint (MonoDevelop.Ide.Gui.Content.IEditableTextBuffer buffer)
			: base (buffer.Name, null)
		{
			int col;
			buffer.GetLineColumnFromPosition (buffer.CursorPosition, out line, out col);
			UpdateSnippet (buffer);
		}
		
		public TextFileNavigationPoint (string fileName, int line, string fragment)
			: base (fileName, fragment)
		{
			this.line = line;
		}
		
		public int Line {
			get { return line; }
		}
		
		public override string DisplayName {
			get {
				return string.Format ("{0} : {1}", System.IO.Path.GetFileName (FileName), Line);
			}
		}
		
		protected override void DoShow ()
		{
			IdeApp.Workbench.OpenDocument (FileName, Math.Max (line, 1), 1, true);
		}
		
		public void UpdateLine (int line, MonoDevelop.Ide.Gui.Content.IEditableTextBuffer buffer)
		{
			this.line = line;
			UpdateSnippet (buffer);
		}
		
		//gets a snippet for the tooltip
		void UpdateSnippet (MonoDevelop.Ide.Gui.Content.IEditableTextBuffer buffer)
		{
			//get some lines from the file
			int startPos = buffer.GetPositionFromLineColumn (Math.Max (Line - 1, 1), 1);
			int endPos = buffer.GetPositionFromLineColumn (Line + 2, 1);
			if (endPos < 0)
				endPos = buffer.Length; 
			
			string [] lines = buffer.GetText (startPos, endPos).Split ('\n', '\r');
			System.Text.StringBuilder fragment = new System.Text.StringBuilder ();
			
			//calculate the minimum indent of any of these lines, using an approximation that tab == 4 spaces
			int minIndentLength = int.MaxValue;
			foreach (string line in lines) {
				if (line.Length == 0)
					continue;
				int indentLength = GetIndentLength (line);
				if (indentLength < minIndentLength)
					minIndentLength = indentLength;
			}
			
			//strip off the indents and truncate the length
			const int MAX_LINE_LENGTH = 40;
			foreach (string line in lines) {
				if (line.Length == 0)
					continue;
				
				int length = Math.Min (MAX_LINE_LENGTH, line.Length) - minIndentLength;
				if (length > 0)
					fragment.AppendLine (line.Substring (minIndentLength, length));
				else
					fragment.AppendLine ();
			}
			
			Snippet = fragment.ToString ();
		}
		
		int GetIndentLength (string line)
		{
			int indent = 0;
			for (int i = 0; i < line.Length; i++) {
				if (line[i] == ' ')
					indent++;
				else if (line[i] == '\t')
					indent += 4;
				else
					break;
			}
			return indent;
		}
		
		public override bool Equals (object o)
		{
			TextFileNavigationPoint other = o as TextFileNavigationPoint;
			return other != null && other.line == line && base.Equals (other);
		}
	}
}
