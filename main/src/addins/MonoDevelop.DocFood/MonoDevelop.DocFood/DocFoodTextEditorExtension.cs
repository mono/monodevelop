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
using System.Text;
using MonoDevelop.Ide.TypeSystem;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace MonoDevelop.DocFood
{
	class DocFoodTextEditorExtension : TextEditorExtension
	{
		TextEditorData textEditorData {
			get {
				return Document.Editor;
			}
		}
		
		string GenerateDocumentation (ISymbol member, string indent)
		{
			string doc = DocumentBufferHandler.GenerateDocumentation (textEditorData, member, indent);
			int trimStart = (Math.Min (doc.Length - 1, indent.Length + "//".Length));
			return doc.Substring (trimStart).TrimEnd ('\n', '\r');
		}
		
		string GenerateEmptyDocumentation (ISymbol member, string indent)
		{
			string doc = DocumentBufferHandler.GenerateEmptyDocumentation (textEditorData, member, indent);
			int trimStart = (Math.Min (doc.Length - 1, indent.Length + "//".Length));
			return doc.Substring (trimStart).TrimEnd ('\n', '\r');
		}

		public override bool KeyPress (Gdk.Key key, char keyChar, Gdk.ModifierType modifier)
		{
			if (keyChar != '/')
				return base.KeyPress (key, keyChar, modifier);
			
			var line = textEditorData.Document.GetLine (textEditorData.Caret.Line);
			string text = textEditorData.Document.GetTextAt (line.Offset, line.Length);
			
			if (!text.EndsWith ("//", StringComparison.Ordinal))
				return base.KeyPress (key, keyChar, modifier);

			// check if there is doc comment above or below.
			var l = line.PreviousLine;
			while (l != null && l.Length == 0)
				l = l.PreviousLine;
			if (l != null && textEditorData.GetTextAt (l).TrimStart ().StartsWith ("///"))
				return base.KeyPress (key, keyChar, modifier);

			l = line.NextLine;
			while (l != null && l.Length == 0)
				l = l.NextLine;
			if (l != null && textEditorData.GetTextAt (l).TrimStart ().StartsWith ("///"))
				return base.KeyPress (key, keyChar, modifier);

			var member = GetMemberToDocument ();
			if (member == null)
				return base.KeyPress (key, keyChar, modifier);
			
			string documentation = GenerateDocumentation (member, textEditorData.Document.GetLineIndent (line));
			if (string.IsNullOrEmpty (documentation))
				return base.KeyPress (key, keyChar, modifier);
			
			string documentationEmpty = GenerateEmptyDocumentation (member, textEditorData.Document.GetLineIndent (line));
			
			int offset = textEditorData.Caret.Offset;
			
			int insertedLength;
			
			// Insert key (3rd undo step)
			textEditorData.Insert (offset, "/");
			
			using (var undo = textEditorData.OpenUndoGroup ()) {
				insertedLength = textEditorData.Replace (offset, 1, documentationEmpty);
				// important to set the caret position here for the undo step
				textEditorData.Caret.Offset = offset + insertedLength;
			}
			
			using (var undo = textEditorData.OpenUndoGroup ()) {
				insertedLength = textEditorData.Replace (offset, insertedLength, documentation);
				if (SelectSummary (offset, insertedLength, documentation) == false)
					textEditorData.Caret.Offset = offset + insertedLength;
			}
			return false;
		}

		/// <summary>
		/// Make the summary content selected
		/// </summary>
		/// <returns>
		/// <c>true</c>, if summary was selected, <c>false</c> if summary was not found.
		/// </returns>
		/// <param name='offset'>
		/// Offset in document where the documentation is inserted
		/// </param>
		/// <param name='insertedLength'>
		/// the length of the summary content.
		/// </param>
		/// <param name='documentation'>
		/// Documentation containing the summary
		/// </param>
		bool SelectSummary (int offset, int insertedLength, string documentation)
		{
			//Adjust the line endings to what the document uses to assure correct offset within the documentation
			if (insertedLength > documentation.Length)
				documentation = documentation.Replace ("\n", "\r\n");

			const string summaryStart = "<summary>";
			const string summaryEnd = "</summary>";
			int start = documentation.IndexOf (summaryStart);
			int end = documentation.IndexOf (summaryEnd);
			if (start < 0 || end < 0)
				return false;
			start += summaryStart.Length;
			string summaryText = documentation.Substring (start, end - start).Trim (new char[] {' ', '\t', '\r', '\n', '/'});
			start = documentation.IndexOf (summaryText, start);
			if (start < 0)
				return false;
			textEditorData.Caret.Offset = offset + start;
			textEditorData.SetSelection (offset + start, offset + start + summaryText.Length);
			return true;
		}

		bool IsEmptyBetweenLines (int start, int end)
		{
			for (int i = start + 1; i < end - 1; i++) {
				DocumentLine lineSegment = textEditorData.GetLine (i);
				if (lineSegment == null)
					break;
				if (lineSegment.Length != textEditorData.GetLineIndent (lineSegment).Length)
					return false;
				
			}
			return true;
		}	
		
		ISymbol GetMemberToDocument ()
		{
			var analysisDocument = Document.AnalysisDocument;
			if (analysisDocument == null)
				return null;
			var parsedDocument = analysisDocument.GetSemanticModelAsync ().Result;
			var caretOffset = textEditorData.Caret.Offset;
			var offset = caretOffset;
			var root = parsedDocument.SyntaxTree.GetRoot ();

			while (offset < textEditorData.Length) {
				var node = root.FindNode (TextSpan.FromBounds (offset, offset));
				if (node == null || node.SpanStart < caretOffset) {
					offset++;
					continue;
				}

				var declaredSymbol = parsedDocument.GetDeclaredSymbol (node); 
				if (declaredSymbol != null)
					return declaredSymbol;
				offset = node.FullSpan.End + 1;
			}
			return null;
		}
	}
}

