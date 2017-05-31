//
// CSharpAutoInsertBracketHandler.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using System.Threading;

namespace MonoDevelop.CSharp.Features.AutoInsertBracket
{
	class CSharpAutoInsertBracketHandler : AutoInsertBracketHandler
	{
		public override bool CanHandle (TextEditor editor)
		{

			return editor.MimeType == CSharpFormatter.MimeType;
		}

		public override bool Handle (TextEditor editor, DocumentContext ctx, KeyDescriptor descriptor)
		{
			char closingBrace;
			if (!IsSupportedOpeningBrace (descriptor.KeyChar, out closingBrace) || !CheckCodeContext (editor, ctx, editor.CaretOffset - 1, descriptor.KeyChar, default (CancellationToken)) || ctx.AnalysisDocument == null)
				return false;

			var session = CreateEditorSession (editor, ctx, editor.CaretOffset, descriptor.KeyChar, default (CancellationToken));
			session.SetEditor (editor);
			if (session == null | !((ICheckPointEditSession)session).CheckOpeningPoint (editor, ctx, default (CancellationToken)))
				return false;
			using (var undo = editor.OpenUndoGroup ()) {
				editor.EnsureCaretIsNotVirtual ();
				editor.InsertAtCaret (closingBrace.ToString ());
				editor.CaretOffset--;
				editor.StartSession (session);
			}
			return true;
		}

		protected virtual bool CheckCodeContext (TextEditor editor, DocumentContext ctx, int position, char openingBrace, CancellationToken cancellationToken)
		{
			// SPECIAL CASE: Allow in curly braces in string literals to support interpolated strings.
			if (openingBrace == CurlyBrace.OpenCharacter &&
				InterpolationCompletionSession.IsContext (editor, ctx, position, cancellationToken)) {
				return true;
			}

			if (openingBrace == DoubleQuote.OpenCharacter &&
				InterpolatedStringCompletionSession.IsContext (editor, ctx, position, cancellationToken)) {
				return true;
			}

			var analysisDoc = ctx.AnalysisDocument;
			if (analysisDoc == null)
				return false;
			
			// check that the user is not typing in a string literal or comment
			var tree = analysisDoc.GetSyntaxTreeAsync (cancellationToken).Result;

			return !tree.IsInNonUserCode (position, cancellationToken);
		}

		EditSession CreateEditorSession (TextEditor editor, DocumentContext ctx, int openingPosition, char openingBrace, CancellationToken cancellationToken)
		{
			switch (openingBrace) {
			case CurlyBrace.OpenCharacter:
				return InterpolationCompletionSession.IsContext (editor, ctx, openingPosition, cancellationToken)
					? (EditSession)new InterpolationCompletionSession ()
						: new CurlyBraceCompletionSession (ctx);

			case DoubleQuote.OpenCharacter:
				return InterpolatedStringCompletionSession.IsContext (editor, ctx, openingPosition, cancellationToken)
					? (EditSession)new InterpolatedStringCompletionSession ()
						: new StringLiteralCompletionSession (ctx);

			case Bracket.OpenCharacter: return new BracketCompletionSession (ctx);
			case Parenthesis.OpenCharacter: return new ParenthesisCompletionSession (ctx);
			case SingleQuote.OpenCharacter: return new CharLiteralCompletionSession (ctx);
			case LessAndGreaterThan.OpenCharacter: return new LessAndGreaterThanCompletionSession (ctx);
			}

			return null;
		}


		protected bool IsSupportedOpeningBrace (char openingBrace, out char closingBrace)
		{
			switch (openingBrace) {
			case Bracket.OpenCharacter:
				closingBrace = Bracket.CloseCharacter;
				return true;
			case CurlyBrace.OpenCharacter:
				closingBrace = CurlyBrace.CloseCharacter;
				return true;
			case Parenthesis.OpenCharacter:
				closingBrace = Parenthesis.CloseCharacter;
				return true;
			case SingleQuote.OpenCharacter:
				closingBrace = SingleQuote.CloseCharacter;
				return true;
			case DoubleQuote.OpenCharacter:
				closingBrace = DoubleQuote.CloseCharacter;
				return true;
			case LessAndGreaterThan.OpenCharacter:
				closingBrace = LessAndGreaterThan.CloseCharacter;
				return true;
			}
			closingBrace = openingBrace;
			return false;
		}

		static class CurlyBrace
		{
			public const char OpenCharacter = '{';
			public const char CloseCharacter = '}';
		}

		static class Parenthesis
		{
			public const char OpenCharacter = '(';
			public const char CloseCharacter = ')';
		}

		static class Bracket
		{
			public const char OpenCharacter = '[';
			public const char CloseCharacter = ']';
		}

		static class LessAndGreaterThan
		{
			public const char OpenCharacter = '<';
			public const char CloseCharacter = '>';
		}

		static class DoubleQuote
		{
			public const char OpenCharacter = '"';
			public const char CloseCharacter = '"';
		}

		static class SingleQuote
		{
			public const char OpenCharacter = '\'';
			public const char CloseCharacter = '\'';
		}
	}
}