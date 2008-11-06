//
// CodeTemplate.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using System.Diagnostics;
using System.Text;
using System.Xml;

namespace MonoDevelop.Ide.CodeTemplates
{
	public class CodeTemplate
	{
		string shortcut;
		string description;
		string text;
		
		public string Shortcut {
			get {
				return shortcut;
			}
			set {
				Debug.Assert (!String.IsNullOrEmpty (value));
				shortcut = value;
			}
		}
		
		public string Description {
			get {
				return description;
			}
			set {
				Debug.Assert (!String.IsNullOrEmpty (value));
				description = value;
			}
		}
		
		public string Text {
			get {
				return text;
			}
			set {
				Debug.Assert (!String.IsNullOrEmpty (value));
				text = value;
			}
		}
		
		public CodeTemplate()
		{
		}
		
		public CodeTemplate (string shortcut, string description, string text)
		{
			this.shortcut = shortcut;
			this.description = description;
			this.text = text;
		}
		
		public override string ToString ()
		{
			return String.Format ("[CodeTemplate: Shortcut={0}, Description={1}, Text={2}]", this.shortcut, this.description, this.text);
		}
		
		static int FindPrevWordStart (MonoDevelop.Ide.Gui.TextEditor editor, int offset)
		{
			while (--offset >= 0 && !Char.IsWhiteSpace (editor.GetCharAt (offset))) 
				;
			return ++offset;
		}
		
		static string GetWordBeforeCaret (MonoDevelop.Ide.Gui.TextEditor editor)
		{
			int offset = editor.CursorPosition;
			int start  = FindPrevWordStart (editor, offset);
			return editor.GetText (start, offset);
		}
		
		static int DeleteWordBeforeCaret (MonoDevelop.Ide.Gui.TextEditor editor)
		{
			int offset = editor.CursorPosition;
			int start  = FindPrevWordStart (editor, offset);
			editor.DeleteText (start, offset - start);
			return start;
		}
		static string GetLeadingWhiteSpace (MonoDevelop.Ide.Gui.TextEditor editor, int lineNr)
		{
			string lineText = editor.GetLineText (lineNr);
			int index = 0;
			while (index < lineText.Length && Char.IsWhiteSpace (lineText[index]))
				index++;
			return index > 0 ? lineText.Substring (0, index) : "";
		}
		
		public void InsertTemplate (MonoDevelop.Ide.Gui.TextEditor editor)
		{
			int offset = editor.CursorPosition;
			string word = GetWordBeforeCaret (editor).Trim ();
			if (word.Length > 0)
				offset = DeleteWordBeforeCaret (editor);
			
			string leadingWhiteSpace = GetLeadingWhiteSpace (editor, editor.CursorLine);

			int finalCaretOffset = offset + Text.Length;
			StringBuilder builder = new StringBuilder ();
			for (int i = 0; i < Text.Length; ++i) {
				switch (Text[i]) {
				case '|':
					finalCaretOffset = i + offset;
					break;
				case '\r':
					break;
				case '\n':
					builder.Append (Environment.NewLine);
					builder.Append (leadingWhiteSpace);
					break;
				default:
					builder.Append (Text[i]);
					break;
				}
			}
			
//			if (endLine > beginLine) {
//				IndentLines (beginLine+1, endLine, leadingWhiteSpace);
//			}
			editor.InsertText (offset, builder.ToString ());
			editor.CursorPosition = finalCaretOffset;
		}		

#region I/O
		public const string Node          = "CodeTemplate";
		const string shortcutAttribute    = "template";
		const string descriptionAttribute = "description";
		
		public void Write (XmlWriter writer)
		{
			writer.WriteStartElement (Node);
			writer.WriteAttributeString (shortcutAttribute, this.shortcut);
			writer.WriteAttributeString (descriptionAttribute, this.description);
			writer.WriteString (this.text);
			writer.WriteEndElement (); // Node
		}
		
		public static CodeTemplate Read (XmlReader reader)
		{
			Debug.Assert (reader.LocalName == Node);
			
			CodeTemplate result = new CodeTemplate ();
			result.shortcut    = reader.GetAttribute (shortcutAttribute);
			result.description = reader.GetAttribute (descriptionAttribute);
			result.text        = reader.ReadString ();
			return result;
		}
#endregion
	}
}
