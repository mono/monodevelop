// 
// TextEditorExtension.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using MonoDevelop.Ide.Gui.Content;
using Mono.TextEditor;
using MonoDevelop.Projects.Dom;
using System.Text;
using MonoDevelop.Projects.Dom.Parser;

namespace MonoDevelop.DocFood
{
	public class DocFoodTextEditorExtension : TextEditorExtension
	{
		TextEditorData textEditorData;
		
		public override void Initialize ()
		{
			base.Initialize ();
			textEditorData = Document.TextEditorData;
		}
		
		string GenerateDocumentation (IMember member, string indent)
		{
			return DocumentBufferHandler.GenerateDocumentation (textEditorData, member, indent).Substring (indent.Length + "//".Length).TrimEnd ('\n', '\r');
		}
		
		string GenerateEmptyDocumentation (IMember member, string indent)
		{
			return DocumentBufferHandler.GenerateEmptyDocumentation (textEditorData, member, indent).Substring (indent.Length + "//".Length).TrimEnd ('\n', '\r');
		}

		public override bool KeyPress (Gdk.Key key, char keyChar, Gdk.ModifierType modifier)
		{
			if (keyChar != '/')
				return base.KeyPress (key, keyChar, modifier);
			
			LineSegment line = textEditorData.Document.GetLine (textEditorData.Caret.Line);
			string text = textEditorData.Document.GetTextAt (line.Offset, line.EditableLength);
			
			if (!text.EndsWith ("//"))
				return base.KeyPress (key, keyChar, modifier);
			
			IMember member = GetMemberToDocument ();
			if (member == null)
				return base.KeyPress (key, keyChar, modifier);
			
			string documentation = GenerateDocumentation (member, textEditorData.Document.GetLineIndent (line));
			if (string.IsNullOrEmpty (documentation))
				return base.KeyPress (key, keyChar, modifier);
			
			string documentationEmpty = GenerateEmptyDocumentation (member, textEditorData.Document.GetLineIndent (line));
			
			int offset = textEditorData.Caret.Offset;
			textEditorData.Document.EndAtomicUndo ();
			textEditorData.Insert (offset, documentationEmpty);
			textEditorData.Caret.Offset = offset + documentationEmpty.Length;
			textEditorData.Document.BeginAtomicUndo ();
			textEditorData.Replace (offset, documentationEmpty.Length, documentation);
			textEditorData.Caret.Offset = offset + documentation.Length;
			return false;
		}
		
		IMember GetMemberToDocument ()
		{
			var parsedDocument = ProjectDomService.Parse (Document.Project, Document.FileName, Document.TextEditorData.Document.MimeType, Document.TextEditorData.Document.Text);
			IType type = parsedDocument.CompilationUnit.GetTypeAt (textEditorData.Caret.Line, textEditorData.Caret.Column);
			if (type == null)
				return null;
			IMember result = null;
			foreach (IMember member in type.Members) {
				if (member.Location > new DomLocation (textEditorData.Caret.Line + 1, textEditorData.Caret.Column + 1) && (result == null || member.Location < result.Location))
					result = member;
			}
			return result;
		}
	}
}

