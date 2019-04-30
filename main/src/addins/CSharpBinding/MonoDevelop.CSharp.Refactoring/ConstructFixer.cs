//
// ConstructFixer.cs
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

using System;
using ICSharpCode.NRefactory.Editor;
using System.Text;
using System.Reflection;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Options;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MonoDevelop.CSharp.Refactoring
{
	abstract class ConstructCompleter
	{
		public abstract bool TryFix (ConstructFixer fixer, SyntaxNode syntaxTree, MonoDevelop.Ide.Gui.Document document, int location, ref int newOffset);

	}

	class InvocationCompleter : ConstructCompleter
	{
		public override bool TryFix (ConstructFixer fixer, SyntaxNode syntaxTree, Ide.Gui.Document document, int location, ref int newOffset)
		{
			foreach (var invocationExpression in syntaxTree.FindToken (location).Parent.AncestorsAndSelf ().OfType<InvocationExpressionSyntax> ()) {
				if (invocationExpression != null) {
					if (!invocationExpression.ArgumentList.OpenParenToken.IsMissing && invocationExpression.ArgumentList.CloseParenToken.IsMissing) {

						var insertionOffset = invocationExpression.Span.End - 1;

						newOffset = insertionOffset;

						var text = ")";
						newOffset++;
						var expressionStatement = invocationExpression.Parent as ExpressionStatementSyntax;
						if (expressionStatement != null) {
							if (expressionStatement.SemicolonToken.IsMissing)
								text = ");";
							newOffset++;
						}
						document.Editor.InsertText (insertionOffset, text);
						return true;
					}

				}
			}
			return false;
		}
	}


	class BreakStatementCompleter : ConstructCompleter
	{
		public override bool TryFix (ConstructFixer fixer, SyntaxNode syntaxTree, Ide.Gui.Document document, int location, ref int newOffset)
		{
			foreach (var breakStatementSyntax in syntaxTree.FindToken (location).Parent.AncestorsAndSelf ().OfType<BreakStatementSyntax> ()) {
				if (breakStatementSyntax.SemicolonToken.IsMissing) {
					var insertionOffset = breakStatementSyntax.Span.End - 1;
					newOffset = insertionOffset;
					newOffset++;
					document.Editor.InsertText (insertionOffset, ";");
					return true;
				}
			}
			foreach (var breakStatementSyntax in syntaxTree.FindToken (location).Parent.AncestorsAndSelf ().OfType<ContinueStatementSyntax> ()) {
				if (breakStatementSyntax.SemicolonToken.IsMissing) {
					var insertionOffset = breakStatementSyntax.Span.End - 1;
					newOffset = insertionOffset;
					newOffset++;
					document.Editor.InsertText (insertionOffset, ";");
					return true;
				}
			}
			return false;
		}
	}

	class ExpressionStatementCompleter : ConstructCompleter
	{
		public override bool TryFix (ConstructFixer fixer, SyntaxNode syntaxTree, Ide.Gui.Document document, int location, ref int newOffset)
		{
			foreach (var expressionStatement in syntaxTree.FindToken (location).Parent.AncestorsAndSelf ().OfType<ExpressionStatementSyntax> ()) {
				if (expressionStatement.SemicolonToken.IsMissing) {
					var insertionOffset = expressionStatement.Span.End - 1;
					newOffset = insertionOffset;
					newOffset++;
					document.Editor.InsertText (insertionOffset, ";");
					return true;
				}
			}
			return false;
		}
	}

	class ReturnStatementCompleter : ConstructCompleter
	{
		public override bool TryFix (ConstructFixer fixer, SyntaxNode syntaxTree, Ide.Gui.Document document, int location, ref int newOffset)
		{
			foreach (var throwStatement in syntaxTree.FindToken (location).Parent.AncestorsAndSelf ().OfType<ReturnStatementSyntax> ()) {
				if (throwStatement.SemicolonToken.IsMissing) {
					var insertionOffset = throwStatement.Span.End - 1;
					newOffset = insertionOffset;
					newOffset++;
					document.Editor.InsertText (insertionOffset, ";");
					return true;
				}
			}
			return false;
		}
	}

	class YieldReturnStatementCompleter : ConstructCompleter
	{
		public override bool TryFix (ConstructFixer fixer, SyntaxNode syntaxTree, Ide.Gui.Document document, int location, ref int newOffset)
		{
			foreach (var yieldStatement in syntaxTree.FindToken (location).Parent.AncestorsAndSelf ().OfType<YieldStatementSyntax> ()) {
				if (yieldStatement.SemicolonToken.IsMissing) {
					var insertionOffset = yieldStatement.Span.End - 1;
					newOffset = insertionOffset;
					newOffset++;
					document.Editor.InsertText (insertionOffset, ";");
					return true;
				}
			}
			return false;
		}
	}

	class ThrowStatementCompleter : ConstructCompleter
	{
		public override bool TryFix (ConstructFixer fixer, SyntaxNode syntaxTree, Ide.Gui.Document document, int location, ref int newOffset)
		{
			foreach (var throwStatement in syntaxTree.FindToken (location).Parent.AncestorsAndSelf ().OfType<ThrowStatementSyntax> ()) {
				if (throwStatement.SemicolonToken.IsMissing) {
					var insertionOffset = throwStatement.Span.End - 1;
					newOffset = insertionOffset;
					newOffset++;
					document.Editor.InsertText (insertionOffset, ";");
					return true;
				}
			}
			return false;
		}
	}


	public class ConstructFixer
	{
		static readonly ConstructCompleter [] completer = {
			new BreakStatementCompleter (),
			new ThrowStatementCompleter (),
			new ReturnStatementCompleter (),
			new YieldReturnStatementCompleter (),

			new InvocationCompleter (),
			new ExpressionStatementCompleter ()
		};

		// readonly OptionSet options;

		public ConstructFixer (OptionSet options)
		{
			// this.options = options;
		}

		public async Task<int> TryFix (MonoDevelop.Ide.Gui.Document document, int offset, CancellationToken token)
		{
			int newOffset = offset;

			var syntaxTree = await document.DocumentContext.AnalysisDocument.GetSyntaxRootAsync (token);

			foreach (var c in completer) {
				if (c.TryFix (this, syntaxTree, document, offset, ref newOffset)) {
					return newOffset;
				}
			}
			return -1;
		}
	}
}

