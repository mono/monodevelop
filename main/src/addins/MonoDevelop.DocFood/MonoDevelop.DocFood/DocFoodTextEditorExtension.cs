// 
// TextEditorExtension.cs
//  =
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
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using MonoDevelop.CSharp.Completion;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Extension;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using MonoDevelop.Core;

namespace MonoDevelop.DocFood
{
	class DocFoodTextEditorExtension : TextEditorExtension
	{
		string GenerateDocumentation (ISymbol member, string indent)
		{
			string doc = DocumentBufferHandler.GenerateDocumentation (Editor, member, indent);
			int trimStart = (Math.Min (doc.Length - 1, indent.Length + "//".Length));
			return doc.Substring (trimStart).TrimEnd ('\n', '\r');
		}
		
		string GenerateEmptyDocumentation (ISymbol member, string indent)
		{
			string doc = DocumentBufferHandler.GenerateEmptyDocumentation (Editor, member, indent);
			int trimStart = (Math.Min (doc.Length - 1, indent.Length + "//".Length));
			return doc.Substring (trimStart).TrimEnd ('\n', '\r');
		}

		public override bool KeyPress (KeyDescriptor descriptor)
		{
			if (descriptor.KeyChar != '/')
				return base.KeyPress (descriptor);
			
			var line = Editor.GetLine (Editor.CaretLine);
			string text = Editor.GetTextAt (line.Offset, line.Length);
			
			if (!text.EndsWith ("//", StringComparison.Ordinal))
				return base.KeyPress (descriptor);

			// check if there is doc comment above or below.
			var l = line.PreviousLine;
			while (l != null && l.Length == 0)
				l = l.PreviousLine;
			if (l != null && Editor.GetTextAt (l).TrimStart ().StartsWith ("///", StringComparison.Ordinal))
				return base.KeyPress (descriptor);

			l = line.NextLine;
			while (l != null && l.Length == 0)
				l = l.NextLine;
			if (l != null && Editor.GetTextAt (l).TrimStart ().StartsWith ("///", StringComparison.Ordinal))
				return base.KeyPress (descriptor);

			var memberTask = GetMemberToDocument ();
			if (!memberTask.Wait (250) || memberTask.Result == null)
				return base.KeyPress (descriptor);
			var member = memberTask.Result;
			
			string documentation = GenerateDocumentation (member, Editor.GetLineIndent (line));
			if (string.IsNullOrEmpty (documentation))
				return base.KeyPress (descriptor);
			
			string documentationEmpty = GenerateEmptyDocumentation (member, Editor.GetLineIndent (line));
			
			int offset = Editor.CaretOffset;
			
			int insertedLength;
			
			// Insert key (3rd undo step)
			Editor.InsertText (offset, "/");
			using (var undo = Editor.OpenUndoGroup ()) {
				documentationEmpty = Editor.FormatString (offset, documentationEmpty); 
				insertedLength = documentationEmpty.Length;
				Editor.ReplaceText (offset, 1, documentationEmpty);
				// important to set the caret position here for the undo step
				Editor.CaretOffset = offset + insertedLength;
			}
			
			using (var undo = Editor.OpenUndoGroup ()) {
				documentation = Editor.FormatString (offset, documentation); 
				Editor.ReplaceText (offset, insertedLength, documentation);
				insertedLength = documentation.Length;
				if (SelectSummary (offset, insertedLength, documentation) == false)
					Editor.CaretOffset = offset + insertedLength;
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
			int start = documentation.IndexOf (summaryStart, StringComparison.Ordinal);
			int end = documentation.IndexOf (summaryEnd, StringComparison.Ordinal);
			if (start < 0 || end < 0)
				return false;
			start += summaryStart.Length;
			string summaryText = documentation.Substring (start, end - start).Trim (new char[] {' ', '\t', '\r', '\n', '/'});
			start = documentation.IndexOf (summaryText, start, StringComparison.Ordinal);
			if (start < 0)
				return false;
			Editor.CaretOffset = offset + start;
			Editor.SetSelection (offset + start, offset + start + summaryText.Length);
			return true;
		}

		bool IsEmptyBetweenLines (int start, int end)
		{
			for (int i = start + 1; i < end - 1; i++) {
				var lineSegment = Editor.GetLine (i);
				if (lineSegment == null)
					break;
				if (lineSegment.Length != Editor.GetLineIndent (lineSegment).Length)
					return false;
				
			}
			return true;
		}	
		
		async Task<ISymbol> GetMemberToDocument (CancellationToken cancellationToken = default(CancellationToken))
		{
			var parsedDocument = DocumentContext.ParsedDocument;
			if (parsedDocument == null)
				return null;

			try {
				var analysisDoc = DocumentContext.AnalysisDocument;
				if (analysisDoc == null)
					return null;
				var partialDoc = await CSharpCompletionTextEditorExtension.WithFrozenPartialSemanticsAsync (analysisDoc, cancellationToken).ConfigureAwait (false);
				var semanticModel = await partialDoc.GetSemanticModelAsync ();
				if (semanticModel == null)
					return null;
				var caretOffset = Editor.CaretOffset;
				var offset = caretOffset;
				var root = semanticModel.SyntaxTree.GetRoot ();
				var tokenAtCaret = root.FindTrivia (offset - 1, true);
				if (!tokenAtCaret.IsKind (SyntaxKind.SingleLineCommentTrivia))
					return null;
				while (offset < Editor.Length) {
					var node = root.FindNode (TextSpan.FromBounds (offset, offset));

					if (node == null || node.GetLastToken ().SpanStart < caretOffset) {
						offset++;
						continue;
					}
	                var fieldDeclarationSyntax = node as FieldDeclarationSyntax;
	                if (fieldDeclarationSyntax != null) {
						node = fieldDeclarationSyntax.Declaration.Variables.First ();
					}

					var eventDeclaration = node as EventFieldDeclarationSyntax;
					if (eventDeclaration != null) {
						node = eventDeclaration.Declaration.Variables.First ();
					}

					if (node.Span.Contains (caretOffset))
						return null;

					var declaredSymbol = semanticModel.GetDeclaredSymbol (node); 
					if (declaredSymbol != null)
						return declaredSymbol;
					offset = node.FullSpan.End + 1;
				}
				return null;
			} catch (Exception e) {
				LoggingService.LogError("Error wihle getting member to document.", e);
				return null;
			}
		}
	}
}

