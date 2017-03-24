//
// LessAndGreaterThanCompletionSession.cs
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

using System.Linq;
using System.Threading;
using ICSharpCode.NRefactory6.CSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using MonoDevelop.Ide.Editor;
using Roslyn.Utilities;

namespace MonoDevelop.CSharp.Features.AutoInsertBracket
{
	internal class LessAndGreaterThanCompletionSession : AbstractTokenBraceCompletionSession, ICheckPointEditSession
	{
		public LessAndGreaterThanCompletionSession (DocumentContext ctx)
			: base (ctx, (int)SyntaxKind.LessThanToken, (int)SyntaxKind.GreaterThanToken, '>')
		{
		}

		public override bool CheckOpeningPoint (TextEditor editor, DocumentContext ctx, CancellationToken cancellationToken)
		{
			var snapshot = CurrentSnapshot;
			var position = StartOffset;
			var token = FindToken (snapshot, position, cancellationToken);

			// check what parser thinks about the newly typed "<" and only proceed if parser thinks it is "<" of 
			// type argument or parameter list
			if (!token.CheckParent<TypeParameterListSyntax> (n => n.LessThanToken == token) &&
				!token.CheckParent<TypeArgumentListSyntax> (n => n.LessThanToken == token) &&
				!PossibleTypeArgument (token, cancellationToken)) {
				return false;
			}

			return true;
		}

		private bool PossibleTypeArgument (SyntaxToken token, CancellationToken cancellationToken)
		{
			var node = token.Parent as BinaryExpressionSyntax;

			// type argument can be easily ambiguous with normal < operations
			if (node == null || node.Kind () != SyntaxKind.LessThanExpression || node.OperatorToken != token) {
				return false;
			}

			// use binding to see whether it is actually generic type or method 
			var document = Document;
			if (document == null) {
				return false;
			}

			var model = document.GetSemanticModelAsync (cancellationToken).WaitAndGetResult (cancellationToken);

			// Analyze node on the left of < operator to verify if it is a generic type or method.
			var leftNode = node.Left;
			if (leftNode is ConditionalAccessExpressionSyntax) {
				// If node on the left is a conditional access expression, get the member binding expression 
				// from the innermost conditional access expression, which is the left of < operator. 
				// e.g: Case a?.b?.c< : we need to get the conditional access expression .b?.c and analyze its
				// member binding expression (the .c) to see if it is a generic type/method.
				// Case a?.b?.c.d< : we need to analyze .c.d
				// Case a?.M(x => x?.P)?.M2< : We need to analyze .M2
				leftNode = leftNode.GetInnerMostConditionalAccessExpression ().WhenNotNull;
			}

			var info = model.GetSymbolInfo (leftNode, cancellationToken);
			return info.CandidateSymbols.Any (IsGenericTypeOrMethod);
		}

		private static bool IsGenericTypeOrMethod (ISymbol symbol)
		{
			return symbol.GetArity () > 0;
		}

		public override bool AllowOverType (CancellationToken cancellationToken)
		{
			return CheckCurrentPosition (cancellationToken);
		}
	}
}