// 
// XmlTextEditorExtension.cs
// 
// Authors:
//   Michael Hutchinson <mhutchinson@novell.com>
//   Matt Ward
// 
// Copyright:
//   (C) 2008 Novell, Inc (http://www.novell.com)
//   (C) 2007 Matt Ward
// 
// License:
//   Derived from LGPL files (Matt Ward)
//   All code since then is MIT/X11
//

using System;

using MonoDevelop.Projects.Gui.Completion;
using MonoDevelop.Ide.Gui.Content;

namespace MonoDevelop.XmlEditor.Completion
{
	
	
	public class ClosingBracketCompletionDataProvider : ICompletionDataProvider
	{
		IEditableTextBuffer buffer;
		
		public string DefaultCompletionString {
			get { return ">"; }
		}

		
		public ClosingBracketCompletionDataProvider (IEditableTextBuffer buffer)
		{
			this.buffer = buffer;
		}

		public void Dispose ()
		{
			buffer = null;
		}

		public ICompletionData[] GenerateCompletionData (ICompletionWidget widget, char charTyped)
		{
			if (charTyped != '>')
				return null;
			
			string autoClose = GetAutoCloseElement (buffer);
			if (autoClose == null)
				return null;
			
			return new ICompletionData[] {
				new ClosingBracketCompletionData (autoClose, 0),
			};
		}
		
		// Greater than key just pressed so try and auto-close the xml element.
		public static string GetAutoCloseElement (IEditableTextBuffer buffer)
		{
			// Move to char before '>'
			int index = buffer.CursorPosition - 2;
			int elementEnd = index;

			// Ignore if is empty element, element has no name or if comment.
			char c = buffer.GetCharAt (index);
			if (c == '/' || c == '<' || c == '-')
				return null;
					
			// Work backwards and try to find the
			// element start.
			while (--index > -1) {
				c = buffer.GetCharAt (index);
				if (c != '<')
					continue;
				
				//have found start of tag, so work out its name
				string elementName = GetElementNameFromStartElement (buffer.GetText (index + 1, elementEnd));
				if (string.IsNullOrEmpty (elementName))
					return null;
				
				return String.Concat ("</", elementName, ">");
			}
			return null;
		}
		
		// Tries to get the element name from an element start tag string.  The element start tag 
		// string in this case means the text inside the start tag.
		static string GetElementNameFromStartElement (string text)
		{
			string name = text.Trim ();
			// A forward slash means we are in an element so ignore it.
			if (name.Length == 0 || name[0] == '/') {
				return null;
			}
			
			// Ignore any attributes.
			int index = name.IndexOf (' ');
			if (index > 0) {
				name = name.Substring (0, index);
			}
			
			return name.Length != 0? name: null;
		}
	}
	
	public class ClosingBracketCompletionData : IActionCompletionData
	{
		string closingElement;
		int cursorOffset;
		
		public ClosingBracketCompletionData (string closingElement, int cursorOffset)
		{
			this.cursorOffset = cursorOffset;
			this.closingElement = closingElement;
		}
		
		public string Image {
			get { return Gtk.Stock.GoForward; }
		}

		public string[] Text {
			get { return new string [] { closingElement }; }
		}

		public string Description {
			get { return null; }
		}

		public string CompletionString {
			get { return closingElement; }
		}

		
		public void InsertAction (ICompletionWidget widget, ICodeCompletionContext context)
		{
			MonoDevelop.Ide.Gui.Content.IEditableTextBuffer buf = widget as MonoDevelop.Ide.Gui.Content.IEditableTextBuffer;
			if (buf != null) {
				buf.BeginAtomicUndo ();
				buf.InsertText (buf.CursorPosition, closingElement);
				// Move caret into the middle of the tags
				buf.CursorPosition = context.TriggerOffset + cursorOffset;
				buf.Select (buf.CursorPosition, buf.CursorPosition);
				buf.EndAtomicUndo ();
			}
		}

		
	}
}
