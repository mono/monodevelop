// 
// TextFileNavigationPoint.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
//   Lluis Sanchez Gual <lluis@novell.com>
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
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Ide.Navigation
{
	public class TextFileNavigationPoint : DocumentNavigationPoint
	{
		int line;
		int column;
		
		public TextFileNavigationPoint (Document doc, IEditableTextBuffer buffer)
			: base (doc)
		{
			buffer.GetLineColumnFromPosition (buffer.CursorPosition, out line, out column);
		}
		
		public TextFileNavigationPoint (FilePath file, int line, int column)
			: base (file)
		{
			this.line = line;
			this.column = column;
		}
		
		public override bool ShouldReplace (NavigationPoint oldPoint)
		{
			TextFileNavigationPoint tf = oldPoint as TextFileNavigationPoint;
			if (tf == null)
				return false;
			return base.Equals (tf) && Math.Abs (line - tf.line) < 5;
		}
		
		
		public int Line {
			get { return line; }
		}
		
		public int Column {
			get { return this.column; }
		}
		
		public override string DisplayName {
			get {
				return string.Format ("{0} : {1}", base.DisplayName, Line);
			}
		}
		
		protected override Document DoShow ()
		{
			Document doc = base.DoShow ();
			if (doc != null) {
				IEditableTextBuffer buf = doc.GetContent<IEditableTextBuffer> ();
				if (buf != null) {
					doc.DisableAutoScroll ();
					buf.RunWhenLoaded (() => {
						buf.SetCaretTo (Math.Max (line, 1), Math.Max (column, 1));
					});
				}
			}
			return doc;
		}
		
		/*
		
		//FIXME: this currently isn't hooked up to any GUI. In addition, it should be done lazily, since it's expensive 
		// and the nav menus are shown much less frequently than nav points are created.
		
		public override string Tooltip {
			get { return snippet; }
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
			
			snippet = fragment.ToString ();
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
		}*/
		
		public override bool Equals (object o)
		{
			TextFileNavigationPoint other = o as TextFileNavigationPoint;
			return other != null && other.line == line && base.Equals (other);
		}
		
		public override int GetHashCode ()
		{
			unchecked {
				return line + base.GetHashCode ();
			}
		}
	}
}
