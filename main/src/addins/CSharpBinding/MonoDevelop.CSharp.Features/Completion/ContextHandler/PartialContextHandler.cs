//
// PartialContextHandler.cs
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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using MonoDevelop.Ide.CodeCompletion;

namespace ICSharpCode.NRefactory6.CSharp.Completion
{
	class PartialContextHandler : CompletionContextHandler
	{
		public override bool IsTriggerCharacter (SourceText text, int position)
		{
			return IsTriggerAfterSpaceOrStartOfWordCharacter (text, position);
		}

		public override Task<bool> IsExclusiveAsync (CompletionContext completionContext, SyntaxContext ctx, CompletionTriggerInfo triggerInfo, CancellationToken cancellationToken)
		{
			var document = completionContext.Document;
			var position = completionContext.Position;
			var tree = ctx.SyntaxTree;

			//DeclarationModifiers modifiers;
			SyntaxToken token;

			var semanticModel = ctx.SemanticModel;
			var enclosingSymbol = semanticModel.GetEnclosingSymbol (position, cancellationToken) as INamedTypeSymbol;

			// Only inside classes and structs
			if (enclosingSymbol == null || !(enclosingSymbol.TypeKind == TypeKind.Struct || enclosingSymbol.TypeKind == TypeKind.Class)) {
				return Task.FromResult (false);
			}

			if (!IsPartialCompletionContext (tree, position, cancellationToken/*, out modifiers*/, out token)) {
				return Task.FromResult (false);
			}

			return Task.FromResult (true);

		}

		protected override Task<IEnumerable<CompletionData>> GetItemsWorkerAsync (CompletionResult completionResult, CompletionEngine engine, CompletionContext completionContext, CompletionTriggerInfo info, SyntaxContext ctx, CancellationToken cancellationToken)
		{
			var document = completionContext.Document;
			var position = completionContext.Position;
			var tree = ctx.SyntaxTree;

			//DeclarationModifiers modifiers;
			SyntaxToken token;

			var semanticModel = ctx.SemanticModel;
			var enclosingSymbol = semanticModel.GetEnclosingSymbol (position, cancellationToken) as INamedTypeSymbol;

			// Only inside classes and structs
			if (enclosingSymbol == null || !(enclosingSymbol.TypeKind == TypeKind.Struct || enclosingSymbol.TypeKind == TypeKind.Class)) {
				return Task.FromResult (Enumerable.Empty<CompletionData> ());
			}

			if (!IsPartialCompletionContext (tree, position, cancellationToken/*, out modifiers*/, out token)) {
				if (enclosingSymbol != null && (token.IsKind (SyntaxKind.OpenBraceToken) || token.IsKind (SyntaxKind.CloseBraceToken) || token.IsKind (SyntaxKind.SemicolonToken))) {
					return Task.FromResult (CreateCompletionData (engine, semanticModel, position, enclosingSymbol, token, false, cancellationToken));
				}
				return Task.FromResult (Enumerable.Empty<CompletionData> ());
			}

			return Task.FromResult (CreateCompletionData (engine, semanticModel, position, enclosingSymbol, token, true, cancellationToken));
		}

		protected virtual IEnumerable<CompletionData> CreateCompletionData (CompletionEngine engine, SemanticModel semanticModel, int position, INamedTypeSymbol enclosingType, SyntaxToken token, bool afterPartialKeyword, CancellationToken cancellationToken)
		{
			var symbols = semanticModel.LookupSymbols(position, container: enclosingType)
				.OfType<IMethodSymbol>()
				.Where(m => IsPartial(m) && m.PartialImplementationPart == null);

			var list = new List<CompletionData> ();

			var declarationBegin = afterPartialKeyword ? token.Parent.SpanStart : position - 1;
			foreach (var m in symbols) {
				var data = engine.Factory.CreatePartialCompletionData (
					this,
					declarationBegin,
					enclosingType,
					m,
					afterPartialKeyword
				);
				list.Add (data); 
			}
			return list;
		}

		static bool IsPartial(IMethodSymbol m)
		{
			if (m.DeclaredAccessibility != Accessibility.NotApplicable &&
				m.DeclaredAccessibility != Accessibility.Private)
			{
				return false;
			}

			if (!m.ReturnsVoid)
			{
				return false;
			}

			if (m.IsVirtual)
			{
				return false;
			}

			var declarations = m.DeclaringSyntaxReferences.Select(r => r.GetSyntax()).OfType<MethodDeclarationSyntax>();
			return declarations.Any(d => d.Body == null && d.Modifiers.Any(SyntaxKind.PartialKeyword));
		}

		static bool IsPartialCompletionContext(SyntaxTree tree, int position, CancellationToken cancellationToken, /*out DeclarationModifiers modifiers, */out SyntaxToken token)
		{
			var touchingToken = tree.FindTokenOnLeftOfPosition(position, cancellationToken);
			var targetToken = touchingToken.GetPreviousTokenIfTouchingWord(position);
			var text = tree.GetText(cancellationToken);

			token = targetToken;

			//modifiers = default(DeclarationModifiers);

			if (targetToken.IsKind(SyntaxKind.VoidKeyword, SyntaxKind.PartialKeyword) ||
				(targetToken.Kind() == SyntaxKind.IdentifierToken && targetToken.HasMatchingText(SyntaxKind.PartialKeyword)))
			{
				return !IsOnSameLine (touchingToken.GetNextToken (), touchingToken, text) &&
				VerifyModifiers (tree, position, cancellationToken/*, out modifiers*/);
			}

			return false;
		}

		static bool VerifyModifiers(SyntaxTree tree, int position, CancellationToken cancellationToken/*, out DeclarationModifiers modifiers*/)
		{
			var touchingToken = tree.FindTokenOnLeftOfPosition(position, cancellationToken);
			var token = touchingToken.GetPreviousToken();

			bool foundPartial = touchingToken.IsKindOrHasMatchingText(SyntaxKind.PartialKeyword);
			//bool foundAsync = false;

			while (IsOnSameLine(token, touchingToken, tree.GetText(cancellationToken)))
			{
				if (token.IsKind(SyntaxKind.ExternKeyword, SyntaxKind.PublicKeyword, SyntaxKind.ProtectedKeyword, SyntaxKind.InternalKeyword))
				{
					//modifiers = default(DeclarationModifiers);
					return false;
				}

				//if (token.IsKindOrHasMatchingText(SyntaxKind.AsyncKeyword))
				//{
				//	foundAsync = true;
				//}

				foundPartial = foundPartial || token.IsKindOrHasMatchingText(SyntaxKind.PartialKeyword);

				token = token.GetPreviousToken();
			}

			/*modifiers = new DeclarationModifiers()
				.WithPartial(true)
				.WithAsync (foundAsync);*/
			return foundPartial;
		}

		static bool IsOnSameLine(SyntaxToken syntaxToken, SyntaxToken touchingToken, SourceText text)
		{
			return !syntaxToken.IsKind(SyntaxKind.None)
				&& !touchingToken.IsKind(SyntaxKind.None)
				&& text.Lines.IndexOf(syntaxToken.SpanStart) == text.Lines.IndexOf(touchingToken.SpanStart);
		}
	}
}
