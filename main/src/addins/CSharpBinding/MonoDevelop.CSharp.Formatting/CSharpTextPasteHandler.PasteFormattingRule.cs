//
// TextPasteIndentEngine.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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

using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis;

namespace MonoDevelop.CSharp.Formatting
{
	partial class CSharpTextPasteHandler
	{
		class PasteFormattingRule : AbstractFormattingRule
		{
			public override AdjustNewLinesOperation GetAdjustNewLinesOperation (SyntaxToken previousToken, SyntaxToken currentToken, OptionSet optionSet, NextOperation<AdjustNewLinesOperation> nextOperation)
			{
				if (currentToken.Parent != null) {
					var currentTokenParentParent = currentToken.Parent.Parent;
					if (currentToken.Kind () == SyntaxKind.OpenBraceToken && currentTokenParentParent != null &&
						(currentTokenParentParent.Kind () == SyntaxKind.SimpleLambdaExpression ||
						 currentTokenParentParent.Kind () == SyntaxKind.ParenthesizedLambdaExpression ||
						 currentTokenParentParent.Kind () == SyntaxKind.AnonymousMethodExpression)) {
						return FormattingOperations.CreateAdjustNewLinesOperation (0, AdjustNewLinesOption.PreserveLines);
					}
				}

				return nextOperation.Invoke ();
			}
		}

	}
}
