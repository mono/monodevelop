//
// OverrideContextHandler.cs
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
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.TypeSystem;

namespace ICSharpCode.NRefactory6.CSharp.Completion
{
	class OverrideContextHandler : CompletionContextHandler
	{
		public override bool IsTriggerCharacter (SourceText text, int position)
		{
			return IsTriggerAfterSpaceOrStartOfWordCharacter (text, position);
		}

		public override async Task<bool> IsExclusiveAsync (CompletionContext completionContext, SyntaxContext ctx, CompletionTriggerInfo triggerInfo, CancellationToken cancellationToken)
		{
			var document = completionContext.Document;
			var semanticModel = ctx.SemanticModel;
			var tree = ctx.SyntaxTree;
			if (tree.IsInNonUserCode(completionContext.Position, cancellationToken))
				return false;

			// modifiers* override modifiers* type? |
			Accessibility seenAccessibility;
			//DeclarationModifiers modifiers;
			var token = tree.FindTokenOnLeftOfPosition(completionContext.Position, cancellationToken);
			if (token.Parent == null)
				return false;
			
			var parentMember = token.Parent.AncestorsAndSelf ().OfType<MemberDeclarationSyntax> ().FirstOrDefault (m => !m.IsKind (SyntaxKind.IncompleteMember));

			if (!(parentMember is BaseTypeDeclarationSyntax) &&

			    /* May happen in case: 
				 * 
				 * override $
				 * public override string Foo () {} 
				 */
			    !(token.IsKind (SyntaxKind.OverrideKeyword) && token.Span.Start <= parentMember.Span.Start))
				return false;

			var position = completionContext.Position;
			var startToken = token.GetPreviousTokenIfTouchingWord(position);
			ITypeSymbol returnType;
			SyntaxToken tokenBeforeReturnType;
			TryDetermineReturnType (startToken, semanticModel, cancellationToken, out returnType, out tokenBeforeReturnType);
			if (returnType == null) {
				var enclosingType = semanticModel.GetEnclosingSymbol (position, cancellationToken) as INamedTypeSymbol;
				if (enclosingType != null && (startToken.IsKind (SyntaxKind.OpenBraceToken) || startToken.IsKind (SyntaxKind.CloseBraceToken) || startToken.IsKind (SyntaxKind.SemicolonToken))) {
					return false;
				}
			}

			var text = await document.GetTextAsync (cancellationToken).ConfigureAwait (false);

			var startLineNumber = text.Lines.IndexOf (completionContext.Position);

			if (!TryDetermineModifiers(ref tokenBeforeReturnType, text, startLineNumber, out seenAccessibility/*, out modifiers*/) ||
			    !TryCheckForTrailingTokens (tree, text, startLineNumber, position, cancellationToken)) {
				return false;
			}
			return true;
		}

		protected async override Task<IEnumerable<CompletionData>> GetItemsWorkerAsync (CompletionResult completionResult, CompletionEngine engine, CompletionContext completionContext, CompletionTriggerInfo info, SyntaxContext ctx, CancellationToken cancellationToken)
		{
			// var ctx = await completionContext.GetSyntaxContextAsync (engine.Workspace, cancellationToken).ConfigureAwait (false);
			var document = completionContext.Document;
			var semanticModel = ctx.SemanticModel;
			var tree = ctx.SyntaxTree;
			if (tree.IsInNonUserCode(completionContext.Position, cancellationToken))
				return Enumerable.Empty<CompletionData> ();

			var text = await document.GetTextAsync (cancellationToken).ConfigureAwait (false);

			var startLineNumber = text.Lines.IndexOf (completionContext.Position);

			// modifiers* override modifiers* type? |
			Accessibility seenAccessibility;
			//DeclarationModifiers modifiers;
			var token = tree.FindTokenOnLeftOfPosition (completionContext.Position, cancellationToken);
			if (token.Parent == null)
				return Enumerable.Empty<CompletionData> ();

			// don't show up in that case: int { $$
			if (token.Parent.IsKind (SyntaxKind.SkippedTokensTrivia))
				return Enumerable.Empty<CompletionData> ();
			var im = token.Parent.Ancestors ().OfType<IncompleteMemberSyntax> ().FirstOrDefault ();
			if (im != null) {
				var token2 = tree.FindTokenOnLeftOfPosition (im.Span.Start, cancellationToken);
				if (token2.Parent.IsKind (SyntaxKind.SkippedTokensTrivia))
					return Enumerable.Empty<CompletionData> ();
			}

			var parentMember = token.Parent.AncestorsAndSelf ().OfType<MemberDeclarationSyntax> ().FirstOrDefault (m => !m.IsKind (SyntaxKind.IncompleteMember));

			if (!(parentMember is BaseTypeDeclarationSyntax) &&

				/* May happen in case: 
				 * 
				 * override $
				 * public override string Foo () {} 
				 */
				!(token.IsKind (SyntaxKind.OverrideKeyword) && token.Span.Start <= parentMember.Span.Start))
				return Enumerable.Empty<CompletionData> ();

            var position = completionContext.Position;
            var startToken = token.GetPreviousTokenIfTouchingWord(position);
			ITypeSymbol returnType;
			SyntaxToken tokenBeforeReturnType;
			TryDetermineReturnType (startToken, semanticModel, cancellationToken, out returnType, out tokenBeforeReturnType);
			if (returnType == null) {
				var enclosingType = semanticModel.GetEnclosingSymbol (position, cancellationToken) as INamedTypeSymbol;
				if (enclosingType != null && (startToken.IsKind (SyntaxKind.OpenBraceToken) || startToken.IsKind (SyntaxKind.CloseBraceToken) || startToken.IsKind (SyntaxKind.SemicolonToken))) {
					return CreateCompletionData (engine, semanticModel, position, returnType, Accessibility.NotApplicable, startToken, tokenBeforeReturnType, false, cancellationToken);
				}
			}

			if (!TryDetermineModifiers(ref tokenBeforeReturnType, text, startLineNumber, out seenAccessibility/*, out modifiers*/) ||
			    !TryCheckForTrailingTokens (tree, text, startLineNumber, position, cancellationToken)) {
				return Enumerable.Empty<CompletionData> ();
			}

			return CreateCompletionData (engine, semanticModel, position, returnType, seenAccessibility, startToken, tokenBeforeReturnType, true, cancellationToken);
		}

		protected virtual IEnumerable<CompletionData> CreateCompletionData (CompletionEngine engine, SemanticModel semanticModel, int position, ITypeSymbol returnType, Accessibility seenAccessibility, SyntaxToken startToken, SyntaxToken tokenBeforeReturnType, bool afterKeyword, CancellationToken cancellationToken)
		{
			var result = new List<CompletionData> ();
			ISet<ISymbol> overridableMembers;
			if (!TryDetermineOverridableMembers (semanticModel, tokenBeforeReturnType, seenAccessibility, out overridableMembers, cancellationToken)) {
				return result;
			}
			if (returnType != null) {
				overridableMembers = FilterOverrides (overridableMembers, returnType);
			}
			var curType = semanticModel.GetEnclosingSymbol<INamedTypeSymbol> (position, cancellationToken);
			var declarationBegin = afterKeyword ? (startToken.Parent.AncestorsAndSelf ().OfType<IncompleteMemberSyntax> ().FirstOrDefault () ?? startToken.Parent).SpanStart : position - 1;
			foreach (var m in overridableMembers) {
				var data = engine.Factory.CreateNewOverrideCompletionData (this, declarationBegin, curType, m, afterKeyword);
				result.Add (data);
			}
			return result;
		}

		protected static ISet<ISymbol> FilterOverrides(ISet<ISymbol> members, ITypeSymbol returnType)
		{
			var filteredMembers = new HashSet<ISymbol>(
				from m in members
				where m.GetReturnType ().ToString () ==  returnType.ToString ()
				select m);

			// Don't filter by return type if we would then have nothing to show.
			// This way, the user gets completion even if they speculatively typed the wrong return type
			if (filteredMembers.Count > 0)
			{
				members = filteredMembers;
			}

			return members;
		}

		static bool TryDetermineReturnType(SyntaxToken startToken, SemanticModel semanticModel, CancellationToken cancellationToken, out ITypeSymbol returnType, out SyntaxToken nextToken)
		{
			nextToken = startToken;
			returnType = null;
			if (startToken.Parent is TypeSyntax)
			{
				var typeSyntax = (TypeSyntax)startToken.Parent;

				// 'partial' is actually an identifier.  If we see it just bail.  This does mean
				// we won't handle overrides that actually return a type called 'partial'.  And
				// not a single tear was shed.
				if (typeSyntax is IdentifierNameSyntax &&
				    ((IdentifierNameSyntax)typeSyntax).Identifier.IsKindOrHasMatchingText(SyntaxKind.PartialKeyword))
				{
					return false;
				}

				returnType = semanticModel.GetTypeInfo(typeSyntax, cancellationToken).Type;
				nextToken = typeSyntax.GetFirstToken().GetPreviousToken();
			}

			return true;
		}


		static bool HasOverridden (ISymbol original, ISymbol testSymbol)
		{
			if (original.Kind != testSymbol.Kind)
				return false;
			switch (testSymbol.Kind) {
				case SymbolKind.Method:
				return ((IMethodSymbol)testSymbol).OverriddenMethod == original;
				case SymbolKind.Property:
				return ((IPropertySymbol)testSymbol).OverriddenProperty == original;
				case SymbolKind.Event:
				return ((IEventSymbol)testSymbol).OverriddenEvent == original;
			}
			return false;
		}

		public static bool IsOverridable(ISymbol member, INamedTypeSymbol containingType)
		{
			if (member.IsAbstract || member.IsVirtual || member.IsOverride) {
				if (member.IsSealed) {
					return false;
				}

				if (!member.IsAccessibleWithin(containingType)) {
					return false;
				}

				switch (member.Kind) {
					case SymbolKind.Event:
					return true;
					case SymbolKind.Method:
					return ((IMethodSymbol)member).MethodKind == MethodKind.Ordinary;
					case SymbolKind.Property:
					return !((IPropertySymbol)member).IsWithEvents;
				}
			}
			return false;
		}

		internal static bool TryDetermineOverridableMembers(SemanticModel semanticModel, SyntaxToken startToken, Accessibility seenAccessibility, out ISet<ISymbol> overridableMembers, CancellationToken cancellationToken)
		{
			var result = new HashSet<ISymbol>();
			var containingType = semanticModel.GetEnclosingSymbol<INamedTypeSymbol>(startToken.SpanStart, cancellationToken);
			if (containingType != null && !containingType.IsScriptClass && !containingType.IsImplicitClass)
			{
				if (containingType.TypeKind == TypeKind.Class || containingType.TypeKind == TypeKind.Struct)
				{
					var baseTypes = containingType.GetBaseTypes().Reverse();
					foreach (var type in baseTypes)
					{
						cancellationToken.ThrowIfCancellationRequested();

						// Prefer overrides in derived classes
						RemoveOverriddenMembers(result, type, cancellationToken);

						// Retain overridable methods
						AddOverridableMembers(result, containingType, type, cancellationToken);
					}
					// Don't suggest already overridden members
					RemoveOverriddenMembers(result, containingType, cancellationToken);
				}
			}

			// Filter based on accessibility
			if (seenAccessibility != Accessibility.NotApplicable)
			{
				result.RemoveWhere(m => m.DeclaredAccessibility != seenAccessibility);
			}

			overridableMembers = result;
			return overridableMembers.Count > 0;
		}

		static void AddOverridableMembers(HashSet<ISymbol> result, INamedTypeSymbol containingType, INamedTypeSymbol type, CancellationToken cancellationToken)
		{
			foreach (var member in type.GetMembers())
			{
				cancellationToken.ThrowIfCancellationRequested();

				if (IsOverridable(member, containingType))
				{
					result.Add(member);
				}
			}
		}

		protected static void RemoveOverriddenMembers(HashSet<ISymbol> result, INamedTypeSymbol containingType, CancellationToken cancellationToken)
		{
			foreach (var member in containingType.GetMembers())
			{
				cancellationToken.ThrowIfCancellationRequested();
				var overriddenMember = member.OverriddenMember();
				if (overriddenMember != null)
				{
					result.Remove(overriddenMember);
				}
			}
		}


		static bool TryCheckForTrailingTokens (SyntaxTree tree, SourceText text, int startLineNumber, int position, CancellationToken cancellationToken)
		{
			var root = tree.GetRoot (cancellationToken);
			var token = root.FindToken (position);

			// Don't want to offer Override completion if there's a token after the current
			// position.
			if (token.SpanStart > position) {
				return false;
			}

			// If the next token is also on our line then we don't want to offer completion.
			if (IsOnStartLine (text, startLineNumber, token.GetNextToken ().SpanStart)) {
				return false;
			}

			return true;
		}

		static bool IsOnStartLine (SourceText text, int startLineNumber, int position)
		{
			return text.Lines.IndexOf (position) == startLineNumber;
		}

		static bool TryDetermineModifiers(ref SyntaxToken startToken, SourceText text, int startLine, out Accessibility seenAccessibility/*, out DeclarationModifiers modifiers*/)
		{
			var token = startToken;
			//modifiers = new DeclarationModifiers();
			seenAccessibility = Accessibility.NotApplicable;
			var overrideToken = default(SyntaxToken);

			while (IsOnStartLine(token.SpanStart, text, startLine) && !token.IsKind(SyntaxKind.None))
			{
				switch (token.Kind())
				{
					//case SyntaxKind.UnsafeKeyword:
					//	       isUnsafe = true;
					//break;
					case SyntaxKind.OverrideKeyword:
						       overrideToken = token;
					break;
					//case SyntaxKind.SealedKeyword:
					//	       isSealed = true;
					//break;
					//case SyntaxKind.AbstractKeyword:
					//	       isAbstract = true;
					//break;
					case SyntaxKind.ExternKeyword:
					break;

						// Filter on the most recently typed accessibility; keep the first one we see
					case SyntaxKind.PublicKeyword:
						       if (seenAccessibility == Accessibility.NotApplicable)
						{
							seenAccessibility = Accessibility.Public;
						}

					break;
					case SyntaxKind.InternalKeyword:
						       if (seenAccessibility == Accessibility.NotApplicable)
						{
							seenAccessibility = Accessibility.Internal;
						}

						// If we see internal AND protected, filter for protected internal
						if (seenAccessibility == Accessibility.Protected)
						{
							seenAccessibility = Accessibility.ProtectedOrInternal;
						}

					break;
					case SyntaxKind.ProtectedKeyword:
						       if (seenAccessibility == Accessibility.NotApplicable)
						{
							seenAccessibility = Accessibility.Protected;
						}

						// If we see protected AND internal, filter for protected internal
						if (seenAccessibility == Accessibility.Internal)
						{
							seenAccessibility = Accessibility.ProtectedOrInternal;
						}

					break;
					default:
						// Anything else and we bail.
					return false;
				}

				var previousToken = token.GetPreviousToken();

				// We want only want to consume modifiers
				if (previousToken.IsKind(SyntaxKind.None) || !IsOnStartLine(previousToken.SpanStart, text, startLine))
				{
					break;
				}

				token = previousToken;
			}

			startToken = token;
		/*	modifiers = new DeclarationModifiers ()
				.WithIsUnsafe (isUnsafe)
				.WithIsAbstract (isAbstract)
				.WithIsOverride (true)
				.WithIsSealed (isSealed);*/
			return overrideToken.IsKind(SyntaxKind.OverrideKeyword) && IsOnStartLine(overrideToken.Parent.SpanStart, text, startLine);
		}
	}
}