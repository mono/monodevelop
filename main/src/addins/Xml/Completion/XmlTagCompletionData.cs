// 
// XmlTagCompletionData.cs
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

using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Core;
using MonoDevelop.Ide.Editor.Extension;

namespace MonoDevelop.Xml.Completion
{
	
	
	public class XmlTagCompletionData : BaseXmlCompletionData
	{
		string element;
		int cursorOffset;
		bool closing;
		
		public XmlTagCompletionData (string element, int cursorOffset)
			: this (element, cursorOffset, false)
		{
		}
		
		public XmlTagCompletionData (string element, int cursorOffset, bool closing)
		{
			this.cursorOffset = cursorOffset;
			this.element = element;
			this.closing = closing;
		}
		
		public override IconId Icon {
			get { return closing? Gtk.Stock.GoBack : Gtk.Stock.GoForward; }
		}

		public override string DisplayText {
			get { return element; }
		}
		
		public override string CompletionText {
			get { return element; }
		}
		
		public override void InsertCompletionText (CompletionListWindow window, ref KeyActions ka, KeyDescriptor descriptor)
		{
			var buf = window.CompletionWidget;
			if (buf != null) {
				//completion context gets nulled from window as soon as we alter the buffer
				var codeCompletionContext = window.CodeCompletionContext;

				buf.Replace (buf.CaretOffset, 0, element);
					
				// Move caret into the middle of the tags
				buf.CaretOffset = codeCompletionContext.TriggerOffset + cursorOffset;
			}
		}
	}
}
