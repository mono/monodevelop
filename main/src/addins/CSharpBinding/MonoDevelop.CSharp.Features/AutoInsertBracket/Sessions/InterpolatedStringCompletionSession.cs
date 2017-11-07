//
// InterpolatedStringCompletionSession.cs
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
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Shared.Extensions;
using MonoDevelop.Ide.Editor;
using Roslyn.Utilities;

namespace MonoDevelop.CSharp.Features.AutoInsertBracket
{
	internal class InterpolatedStringCompletionSession : SkipCharSession, ICheckPointEditSession
	{
		public InterpolatedStringCompletionSession() : base ('"')
		{
		}

		public bool CheckOpeningPoint(TextEditor editor, DocumentContext ctx, CancellationToken cancellationToken)
		{
			var snapshot = ctx.AnalysisDocument.GetSyntaxTreeAsync (cancellationToken).WaitAndGetResult(cancellationToken);
			var position = editor.CaretOffset - 1;
			var token = AbstractTokenBraceCompletionSession.FindToken(snapshot, position, cancellationToken);

			return token.IsKind(SyntaxKind.InterpolatedStringStartToken, SyntaxKind.InterpolatedVerbatimStringStartToken)
				        && token.Span.End - 1 == position;
		}

		public static bool IsContext(TextEditor editor, DocumentContext ctx, int position, CancellationToken cancellationToken)
		{
			// Check to see if we're to the right of an $ or an @$
			var start = position - 1;
			if (start < 0)
			{
				return false;
			}

			if (editor[start] == '@')
			{
				start--;

				if (start < 0)
				{
					return false;
				}
			}
			if (editor [start] != '$')
			{
				return false;
			}

			var root = ctx.AnalysisDocument.GetSyntaxRootSynchronously (cancellationToken);
			var token = root.FindTokenOnLeftOfPosition (start);
			return root.SyntaxTree.IsExpressionContext (start, token, attributes: false, cancellationToken: cancellationToken)
				|| root.SyntaxTree.IsStatementContext (start, token, cancellationToken);
		}
	}
}