// 
// XmlMultipleClosingTagCompletionData.cs
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
using System.Text;

using MonoDevelop.Core;
using MonoDevelop.Projects.Gui.Completion;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Xml.StateEngine;

namespace MonoDevelop.XmlEditor.Completion
{
	
	
	public class XmlMultipleClosingTagCompletionData : ICompletionData
	{
		string name, description;
		XElement finalEl;
		XElement[] intEls;
		
		public XmlMultipleClosingTagCompletionData (XElement finalEl, XElement[] intermediateElements)
		{
			name = finalEl.Name.FullName;
			description = GettextCatalog.GetString ("Closing tag for '{0}', also closing all intermediate tags", name);
			name = string.Format ("/{0}>...", name);
			this.intEls = intermediateElements;
			this.finalEl = finalEl;
		}
		
		#region ICompletionData implementation 
				
		public string Icon {
			get { return Gtk.Stock.GoBack; }
		}
		
		public string DisplayText {
			get { return name; }
		}
		
		public string Description {
			get { return description; }
		}
		
		public string CompletionText {
			get {
				StringBuilder sb = new StringBuilder ();
				sb.AppendFormat ("/{0}>", intEls[0].Name.FullName);
				for (int i = 1; i < intEls.Length; i++) {
					sb.AppendFormat ("</{0}>", intEls[i].Name.FullName);
				}
				sb.AppendFormat ("</{0}>", finalEl.Name.FullName);
				return sb.ToString ();
			}
		}
		
		public DisplayFlags DisplayFlags {
			get { return DisplayFlags.None; }
		}
		
		#endregion
		
		#region IActionCompletionData implementation 
		/*
		public void InsertCompletionText (ICompletionWidget widget, ICodeCompletionContext context)
		{
			IEditableTextBuffer buf = widget as IEditableTextBuffer;
			if (buf != null) {
				buf.BeginAtomicUndo ();
				//FIXME: do formatting
				buf.InsertText (buf.CursorPosition, CompletionText);
				buf.EndAtomicUndo ();
			}
		}
		*/
		#endregion
		
	}
}
