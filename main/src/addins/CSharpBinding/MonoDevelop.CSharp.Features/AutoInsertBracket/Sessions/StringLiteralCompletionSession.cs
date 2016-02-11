//
// StringLiteralCompletionSession.cs
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
using MonoDevelop.Ide.Editor;

namespace MonoDevelop.CSharp.Features.AutoInsertBracket
{
	internal class StringLiteralCompletionSession : AbstractTokenBraceCompletionSession
	{
		private const char VerbatimStringPrefix = '@';

		public StringLiteralCompletionSession(DocumentContext ctx)
			: base(ctx, (int)SyntaxKind.StringLiteralToken, (int)SyntaxKind.StringLiteralToken, '\"')
		{
		}

		public override bool CheckOpeningPoint(TextEditor editor, DocumentContext ctx, CancellationToken cancellationToken)
		{
			var snapshot = CurrentSnapshot;
			var position = StartOffset;
			var token = FindToken(snapshot, position, cancellationToken);

			if (!IsValidToken(token) || token.RawKind != OpeningTokenKind)
			{
				return false;
			}

			if (token.SpanStart == position)
			{
				return true;
			}

			return token.SpanStart + 1 == position && Editor.GetCharAt (token.SpanStart) == VerbatimStringPrefix;
		}

		public override bool AllowOverType(CancellationToken cancellationToken)
		{
			return true;
		}
	}
}