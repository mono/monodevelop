//
// ParenthesisCompletionSession.cs
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
// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using ICSharpCode.NRefactory6.CSharp;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using MonoDevelop.Ide.Editor;

namespace MonoDevelop.CSharp.Features.AutoInsertBracket
{
	internal class ParenthesisCompletionSession : AbstractTokenBraceCompletionSession
	{
		public ParenthesisCompletionSession(DocumentContext ctx)
			: base(ctx, (int)SyntaxKind.OpenParenToken, (int)SyntaxKind.CloseParenToken, ')')
		{
		}

		public override bool CheckOpeningPoint(TextEditor editor, DocumentContext ctx, CancellationToken cancellationToken)
		{
			var snapshot = CurrentSnapshot;
			var position = StartOffset;
			var token = FindToken(snapshot, position, cancellationToken);

			// check token at the opening point first
			if (!IsValidToken(token) ||
			    token.RawKind != OpeningTokenKind ||
			    token.SpanStart != position || token.Parent == null)
			{
				return false;
			}

			// now check whether parser think whether there is already counterpart closing parenthesis
			var pair = token.Parent.GetParentheses();

			// if pair is on the same line, then the closing parenthesis must belong to other tracker.
			// let it through
			if (Editor.GetLineByOffset (pair.Item1.SpanStart).LineNumber == Editor.GetLineByOffset(pair.Item2.Span.End).LineNumber)
			{
				return true;
			}

			return (int)pair.Item2.Kind() != ClosingTokenKind || pair.Item2.Span.Length == 0;
		}
	}
}