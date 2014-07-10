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
using MonoDevelop.Ide.CodeCompletion;

namespace MonoDevelop.AspNet.Html
{
	public class DocTypeCompletionData : CompletionData
	{
		string name;
		string text;
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
		
		public override IconId Icon {
			get { return "md-literal"; }
		}

		public override string DisplayText {
			get { return name; }
		}
		
		public override string CompletionText {
			get { return name; }
		}
		
		public override void InsertCompletionText (CompletionListWindow window, ref KeyActions ka, Gdk.Key closeChar, char keyChar, Gdk.ModifierType modifier)
		{
			MonoDevelop.Ide.Gui.Content.IEditableTextBuffer buf = window.CompletionWidget as MonoDevelop.Ide.Gui.Content.IEditableTextBuffer;
			if (buf != null) {
				using (var undo = buf.OpenUndoGroup ()) {
					int deleteStartOffset = window.CodeCompletionContext.TriggerOffset;
					if (text.StartsWith (docTypeStart)) {
						int start = window.CodeCompletionContext.TriggerOffset - docTypeStart.Length;
						if (start >= 0) {
							string readback = buf.GetText (start, window.CodeCompletionContext.TriggerOffset);
							if (string.Compare (readback, docTypeStart, StringComparison.OrdinalIgnoreCase) == 0)
								deleteStartOffset -= docTypeStart.Length;
						}
					}
					
					buf.DeleteText (deleteStartOffset, buf.CursorPosition - deleteStartOffset);
					buf.InsertText (buf.CursorPosition, text);
				}
			}
		}
	}
}
