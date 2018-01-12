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
using Microsoft.CodeAnalysis.CSharp.Extensions;
using MonoDevelop.Core.Text;
using MonoDevelop.CSharp.Completion;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ICSharpCode.NRefactory6.CSharp;

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

		public override async Task<BraceMatchingResult?> GetMatchingBracesAsync (IReadonlyTextDocument editor, DocumentContext context, int offset, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (context.ParsedDocument == null)
				return await fallback.GetMatchingBracesAsync (editor, context, offset, cancellationToken);

			var analysisDocument = context.AnalysisDocument;
			if (analysisDocument == null)
				return null;
			var partialDoc = analysisDocument.WithFrozenPartialSemantics (cancellationToken);
			var root = await partialDoc.GetSyntaxRootAsync (cancellationToken).ConfigureAwait (false);
			if (offset < 0 || root.Span.End <= offset)
				return null;
			var trivia = root.FindTrivia (offset).GetStructure () as DirectiveTriviaSyntax;
			if (trivia != null && (trivia.IsKind (SyntaxKind.RegionDirectiveTrivia) || trivia.IsKind (SyntaxKind.EndRegionDirectiveTrivia))) {
				var matching = trivia.GetMatchingDirective (cancellationToken);
				if (matching == null)
					return null;
				if (trivia.IsKind (SyntaxKind.RegionDirectiveTrivia)) {
					return new BraceMatchingResult (
						new TextSegment (trivia.Span.Start, trivia.Span.Length),
						new TextSegment (matching.Span.Start, matching.Span.Length),
						true,
						BraceMatchingProperties.Hidden);
				} else {
					return new BraceMatchingResult (
						new TextSegment (matching.Span.Start, matching.Span.Length),
						new TextSegment (trivia.Span.Start, trivia.Span.Length),
						false,
						BraceMatchingProperties.Hidden);
				}
			}

			var token = root.FindToken (offset);
			var tokenSpan = token.Span;
			if (offset < tokenSpan.Start || offset >= tokenSpan.End)
				return null;
			for (int i = 0; i < tokenPairs.Length / 2; i++) {
				var open = tokenPairs [i * 2];
				var close = tokenPairs [i * 2 + 1];
				SyntaxToken match;
				if (token.IsKind (open)) {
					if (TryFindMatchingToken (token, out match, open, close)) {
						return new BraceMatchingResult (new TextSegment (tokenSpan.Start, tokenSpan.Length), new TextSegment (match.Span.Start, match.Span.Length), true);
					}
				} else if (token.IsKind (close)) {
					if (TryFindMatchingToken (token, out match, open, close)) {
						return new BraceMatchingResult (new TextSegment (match.Span.Start, match.Span.Length), new TextSegment (tokenSpan.Start, tokenSpan.Length), false);
					}
				}
			}

			if (token.IsKind (SyntaxKind.StringLiteralToken) && token.ToString().EndsWith ("\"", StringComparison.Ordinal)) {
				if (token.IsVerbatimStringLiteral ()) {
					if (offset <= tokenSpan.Start)
						return new BraceMatchingResult (new TextSegment (tokenSpan.Start, 2), new TextSegment (tokenSpan.End - 1, 1), true);
					if (offset >= tokenSpan.End - 1)
						return new BraceMatchingResult (new TextSegment (tokenSpan.Start, 2), new TextSegment (tokenSpan.End - 1, 1), false);
				} else {
					if (offset <= tokenSpan.Start)
						return new BraceMatchingResult (new TextSegment (tokenSpan.Start, 1), new TextSegment (tokenSpan.End - 1, 1), true);
					if (offset >= tokenSpan.End - 1)
						return new BraceMatchingResult (new TextSegment (tokenSpan.Start, 1), new TextSegment (tokenSpan.End - 1, 1), false);
				}
			}

			if (token.IsKind (SyntaxKind.CharacterLiteralToken) && token.ToString().EndsWith ("\'", StringComparison.Ordinal)) {
				if (offset <= tokenSpan.Start)
					return new BraceMatchingResult (new TextSegment (tokenSpan.Start, 1), new TextSegment (tokenSpan.End - 1, 1), true);
				if (offset >= tokenSpan.End - 1)
					return new BraceMatchingResult (new TextSegment (tokenSpan.Start, 1), new TextSegment (tokenSpan.End - 1, 1), false);
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
