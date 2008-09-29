// 
// DocTypeCompletionData.cs
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
using MonoDevelop.Core;
using MonoDevelop.Projects.Gui.Completion;

namespace MonoDevelop.Html
{
	
	
	public class DocTypeCompletionData : IActionCompletionData
	{
		string name;
		string text;
		string description;
		const string docTypeStart = "<!DOCTYPE ";
		
		public DocTypeCompletionData (string name, string text)
			: this (name, text, string.Empty)
		{
		}
		
		public DocTypeCompletionData (string name, string text, string description)
		{
			this.text = text;
			this.name = name;
		}
		
		public string Image {
			get { return "md-literal"; }
		}

		public string[] Text {
			get { return new string[] { name }; }
		}
		
		public string CompletionString {
			get { return name; }
		}

		public string Description {
			get { return description; }
		}

		public void InsertAction (ICompletionWidget widget, ICodeCompletionContext context)
		{
			MonoDevelop.Ide.Gui.Content.IEditableTextBuffer buf = widget as MonoDevelop.Ide.Gui.Content.IEditableTextBuffer;
			if (buf != null) {
				buf.BeginAtomicUndo ();
				
				int deleteStartOffset = context.TriggerOffset;
				if (text.StartsWith (docTypeStart)) {
					int start = context.TriggerOffset - docTypeStart.Length;
					if (start >= 0) {
						string readback = buf.GetText (start, context.TriggerOffset);
						if (string.Compare (readback, docTypeStart, System.StringComparison.InvariantCulture) == 0)
							deleteStartOffset -= docTypeStart.Length;
					}
				}
				
				buf.DeleteText (deleteStartOffset, buf.CursorPosition - deleteStartOffset);
				buf.InsertText (buf.CursorPosition, text);
				buf.EndAtomicUndo ();
			}
		}

		public virtual int CompareTo (ICompletionData x)
		{
			return String.Compare (Text[0], x.Text[0], true);
		}		
	}
}
