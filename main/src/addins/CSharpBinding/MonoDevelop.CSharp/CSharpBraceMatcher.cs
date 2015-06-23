//
// CSharpBraceMatcher.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Ide.Editor;
using MonoDevelop.CSharp.Formatting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using MonoDevelop.Core.Text;

namespace MonoDevelop.CSharp
{
	sealed class CSharpBraceMatcher : AbstractBraceMatcher
	{
		readonly static AbstractBraceMatcher fallback = new DefaultBraceMatcher ();
		readonly static SyntaxKind [] tokenPairs = {
			SyntaxKind.OpenBraceToken, SyntaxKind.CloseBraceToken,
			SyntaxKind.OpenBracketToken, SyntaxKind.CloseBracketToken,
			SyntaxKind.LessThanToken, SyntaxKind.GreaterThanToken,
			SyntaxKind.OpenParenToken, SyntaxKind.CloseParenToken
		};

		public override bool CanHandle (TextEditor editor)
		{
			return editor.MimeType == CSharpFormatter.MimeType;
		}

		public override async Task<BraceMatchingResult?> GetMatchingBracesAsync (IReadonlyTextDocument editor, DocumentContext context, int offset, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (context.ParsedDocument == null)
				return await fallback.GetMatchingBracesAsync (editor, context, offset, cancellationToken);

			var ast = context.ParsedDocument.GetAst<SemanticModel> ();
			if (ast == null)
				return await fallback.GetMatchingBracesAsync (editor, context, offset, cancellationToken);
			var root = await ast.SyntaxTree.GetRootAsync (cancellationToken).ConfigureAwait (false);
			var token = root.FindToken (offset);
			for (int i = 0; i < tokenPairs.Length / 2; i++) {
				var open = tokenPairs [i * 2];
				var close = tokenPairs [i * 2 + 1];
				SyntaxToken match;
				if (token.IsKind (open)) {
					if (TryFindMatchingToken (token, out match, open, close)) {
						return new BraceMatchingResult (new TextSegment (token.Span.Start, token.Span.Length), new TextSegment (match.Span.Start, match.Span.Length), true);
					}
				} else if (token.IsKind (close)) {
					if (TryFindMatchingToken (token, out match, open, close)) {
						return new BraceMatchingResult (new TextSegment (match.Span.Start, match.Span.Length), new TextSegment (token.Span.Start, token.Span.Length), false);
					}
				}
			}

			if (token.IsKind (SyntaxKind.StringLiteralToken)) {
				if (token.IsVerbatimStringLiteral ()) {
					if (offset <= token.Span.Start)
						return new BraceMatchingResult (new TextSegment (token.Span.Start, 2), new TextSegment (token.Span.End - 1, 1), true);
					if (offset >= token.Span.End - 1)
						return new BraceMatchingResult (new TextSegment (token.Span.Start, 2), new TextSegment (token.Span.End - 1, 1), false);
				} else {
					if (offset <= token.Span.Start)
						return new BraceMatchingResult (new TextSegment (token.Span.Start, 1), new TextSegment (token.Span.End - 1, 1), true);
					if (offset >= token.Span.End - 1)
						return new BraceMatchingResult (new TextSegment (token.Span.Start, 1), new TextSegment (token.Span.End - 1, 1), false);
				}
			}

			if (token.IsKind (SyntaxKind.CharacterLiteralToken)) {
				if (offset <= token.Span.Start)
					return new BraceMatchingResult (new TextSegment (token.Span.Start, 1), new TextSegment (token.Span.End - 1, 1), true);
				if (offset >= token.Span.End - 1)
					return new BraceMatchingResult (new TextSegment (token.Span.Start, 1), new TextSegment (token.Span.End - 1, 1), false);
			}

			return null;
		}

		bool TryFindMatchingToken (SyntaxToken token, out SyntaxToken match, SyntaxKind openKind, SyntaxKind closeKind)
		{
			var parent = token.Parent;

			var braceTokens = parent
				.ChildTokens ()
				.Where (t => t.Span.Length > 0 && (t.IsKind (openKind) || t.IsKind (closeKind)))
				.ToList ();

			if (braceTokens.Count == 2 &&
				braceTokens [0].IsKind (openKind) &&
				braceTokens [1].IsKind (closeKind)) {
				if (braceTokens [0] == token) {
					match = braceTokens [1];
					return true;
				} else if (braceTokens [1] == token) {
					match = braceTokens [0];
					return true;
				}
			}

			match = default(SyntaxToken);
			return false;
		}
	}
}