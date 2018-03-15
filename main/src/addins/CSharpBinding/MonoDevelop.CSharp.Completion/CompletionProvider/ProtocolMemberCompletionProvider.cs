//
// ProtocolMemberCompletionProvider.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2017 Microsoft Corporation
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
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.LanguageServices;
using MonoDevelop.Core;
using MonoDevelop.CSharp.Refactoring;
using MonoDevelop.Ide;
using MonoDevelop.Ide.TypeSystem;
using System.Text;
using Microsoft.CodeAnalysis.DocumentationComments;

namespace MonoDevelop.CSharp.Completion.Provider
{
	[ExportCompletionProvider ("ProtocolMemberCompletionProvider", LanguageNames.CSharp)]
	class ProtocolMemberCompletionProvider : CompletionProvider
	{
		public override async Task ProvideCompletionsAsync (Microsoft.CodeAnalysis.Completion.CompletionContext context)
		{
			var document = context.Document;
			var position = context.Position;
			var cancellationToken = context.CancellationToken;

			var model = await document.GetSemanticModelForSpanAsync (new TextSpan (position, 0), cancellationToken).ConfigureAwait (false);

			var tree = model.SyntaxTree;
			if (tree.IsInNonUserCode (context.Position, cancellationToken))
				return;

			var text = await document.GetTextAsync (cancellationToken).ConfigureAwait (false);

			var startLineNumber = text.Lines.IndexOf (context.Position);

			// modifiers* override modifiers* type? |
			//DeclarationModifiers modifiers;
			var token = tree.FindTokenOnLeftOfPosition (context.Position, cancellationToken);
			if (token.Parent == null)
				return;

			// don't show up in that case: int { $$
			if (token.Parent.IsKind (SyntaxKind.SkippedTokensTrivia))
				return;

			var im = token.Parent.Ancestors ().OfType<IncompleteMemberSyntax> ().FirstOrDefault ();
			if (im != null) {
				var token2 = tree.FindTokenOnLeftOfPosition (im.Span.Start, cancellationToken);
				if (token2.Parent.IsKind (SyntaxKind.SkippedTokensTrivia))
					return;
			}

			var parentMember = token.Parent.AncestorsAndSelf ().OfType<MemberDeclarationSyntax> ().FirstOrDefault (m => !m.IsKind (SyntaxKind.IncompleteMember));

			if (!(parentMember is BaseTypeDeclarationSyntax) &&

				/* May happen in case: 
				 * 
				 * override $
				 * public override string Foo () {} 
				 */
				!(token.IsKind (SyntaxKind.OverrideKeyword) && token.Span.Start <= parentMember.Span.Start))
				return;

			var startToken = token.GetPreviousTokenIfTouchingWord (position);
			TryDetermineReturnType (startToken, model, cancellationToken, out ITypeSymbol returnType, out SyntaxToken tokenBeforeReturnType);
			if (returnType == null) {
				var enclosingType = model.GetEnclosingSymbol (position, cancellationToken) as INamedTypeSymbol;
				if (enclosingType != null && (startToken.IsKind (SyntaxKind.OpenBraceToken) || startToken.IsKind (SyntaxKind.CloseBraceToken) || startToken.IsKind (SyntaxKind.SemicolonToken))) {
					CreateCompletionData (context, model, position, returnType, Accessibility.NotApplicable, startToken, tokenBeforeReturnType, false, cancellationToken);
					return;
				}
			}

			if (!TryDetermineModifiers (ref tokenBeforeReturnType, text, startLineNumber, out Accessibility seenAccessibility/*, out modifiers*/) ||
				!TryCheckForTrailingTokens (tree, text, startLineNumber, position, cancellationToken)) {
				return;
			}

			CreateCompletionData (context, model, position, returnType, seenAccessibility, startToken, tokenBeforeReturnType, true, cancellationToken);

		}

		protected async void CreateCompletionData (CompletionContext context, SemanticModel semanticModel, int position, ITypeSymbol returnType, Accessibility seenAccessibility, SyntaxToken startToken, SyntaxToken tokenBeforeReturnType, bool afterKeyword, CancellationToken cancellationToken)
		{
			if (!TryDetermineOverridableProtocolMembers (semanticModel, tokenBeforeReturnType, seenAccessibility, out ISet<ISymbol> overridableMembers, cancellationToken)) {
				return;
			}
			if (returnType != null) {
				overridableMembers = FilterOverrides (overridableMembers, returnType);
			}
			var currentType = semanticModel.GetEnclosingSymbolMD<INamedTypeSymbol> (startToken.SpanStart, cancellationToken);
			var declarationBegin = afterKeyword ? startToken.SpanStart : position - 1;
			foreach (var m in overridableMembers) {
				var pDict = ImmutableDictionary<string, string>.Empty;
				bool isExplicit = false;
				//			if (member.DeclaringTypeDefinition.Kind == TypeKind.Interface) {
				//				foreach (var m in type.Members) {
				//					if (m.Name == member.Name && !m.ReturnType.Equals (member.ReturnType)) {
				//						isExplicit = true;
				//						break;
				//					}
				//				}
				//			}
				//			var resolvedType = type.Resolve (ext.Project).GetDefinition ();
				//			if (ext.Project != null)
				//				generator.PolicyParent = ext.Project.Policies;

				var result = CSharpCodeGenerator.CreateProtocolMemberImplementation (null, null, currentType, currentType.Locations.First (), m, isExplicit, semanticModel);
				string sb = result.Code.TrimStart ();
				int trimStart = result.Code.Length - sb.Length;
				sb = sb.TrimEnd ();

				var lastRegion = result.BodyRegions.LastOrDefault ();
				var region = lastRegion == null ? null
					: new CodeGeneratorBodyRegion (lastRegion.StartOffset - trimStart, lastRegion.EndOffset - trimStart);

				pDict = pDict.Add ("InsertionText", sb.ToString ());
				pDict = pDict.Add ("DescriptionMarkup", "- <span foreground=\"darkgray\" size='small'>" + GettextCatalog.GetString ("Implement protocol member") + "</span>");
				pDict = pDict.Add ("Description", await GenerateQuickInfo (semanticModel, position, m, cancellationToken));

				var tags = ImmutableArray<string>.Empty.Add ("NewMethod");
				var completionData = CompletionItem.Create (m.Name, properties: pDict, rules: ProtocolCompletionRules, tags: tags);
				context.AddItem (completionData);
			}
		}

		static async Task<string> GenerateQuickInfo (SemanticModel semanticModel, int position, ISymbol m, CancellationToken cancellationToken)
		{
			var ws = IdeApp.Workbench.ActiveDocument.RoslynWorkspace;

			var displayService = ws.Services.GetLanguageServices (LanguageNames.CSharp).GetService<ISymbolDisplayService> ();

			var sections = await displayService.ToDescriptionGroupsAsync (ws, semanticModel, position, new [] { m }.AsImmutable (), default (CancellationToken)).ConfigureAwait (false);
			ImmutableArray<TaggedText> parts;

			var description = new StringBuilder ();
			description.Append ("Text|");
			description.AppendLine (GettextCatalog.GetString ("Creates an implementation for:"));
			description.AppendLine ();
			foreach (var sect in sections) {
				if (sections.TryGetValue (SymbolDescriptionGroups.MainDescription, out parts)) {
					foreach (var part in parts) {
						description.Append ("|");
						description.Append (part.Tag);
						description.Append ("|");
						description.Append (part.Text);
					}
				}
			}
			var formatter = ws.Services.GetLanguageServices (LanguageNames.CSharp).GetService<IDocumentationCommentFormattingService> ();

			var documentation = m.GetDocumentationParts (semanticModel, position, formatter, cancellationToken);

			if (documentation != null && documentation.Any ()) {
				description.Append ("|LineBreak|\n|LineBreak|\n");

				foreach (var part in documentation) {
					description.Append ("|");
					description.Append (part.Tag);
					description.Append ("|");
					description.Append (part.Text);
				}
			}
			return description.ToString ();
		}

		static CompletionItemRules ProtocolCompletionRules = CompletionItemRules.Create (formatOnCommit: true);

		public override async Task<CompletionChange> GetChangeAsync (Document document, CompletionItem item, char? commitKey, CancellationToken cancellationToken)
		{
			var change = (await GetTextChangeAsync (document, item, commitKey, cancellationToken).ConfigureAwait (false))
				?? new TextChange (item.Span, item.DisplayText);
			return CompletionChange.Create (change);
		}

		public virtual Task<TextChange?> GetTextChangeAsync (Document document, CompletionItem selectedItem, char? ch, CancellationToken cancellationToken)
		{
			return GetTextChangeAsync (selectedItem, ch, cancellationToken);
		}

		Task<TextChange?> GetTextChangeAsync (CompletionItem selectedItem, char? ch, CancellationToken cancellationToken)
		{
			var text = Microsoft.CodeAnalysis.Completion.Providers.SymbolCompletionItem.GetInsertionText (selectedItem);
			return Task.FromResult<TextChange?> (new TextChange (new TextSpan (selectedItem.Span.Start, selectedItem.Span.Length), text));
		}

		static bool TryDetermineOverridableProtocolMembers (SemanticModel semanticModel, SyntaxToken startToken, Accessibility seenAccessibility, out ISet<ISymbol> overridableMembers, CancellationToken cancellationToken)
		{
			var result = new HashSet<ISymbol> ();
			var containingType = semanticModel.GetEnclosingSymbolMD<INamedTypeSymbol> (startToken.SpanStart, cancellationToken);
			if (containingType != null && !containingType.IsScriptClass && !containingType.IsImplicitClass) {
				if (containingType.TypeKind == TypeKind.Class || containingType.TypeKind == TypeKind.Struct) {
					var baseTypes = containingType.GetBaseTypesMD ().Reverse ().Concat (containingType.AllInterfaces);
					foreach (var type in baseTypes) {
						if (cancellationToken.IsCancellationRequested) {
							overridableMembers = null;
							return false;
						}


						// Prefer overrides in derived classes
						// RemoveOverriddenMembers (result, type, cancellationToken);

						// Retain overridable methods
						AddProtocolMembers (semanticModel, result, type, cancellationToken);
					}
					// Don't suggest already overridden members
					// RemoveOverriddenMembers (result, containingType, cancellationToken);
				}
			}

			// Filter based on accessibility
			if (seenAccessibility != Accessibility.NotApplicable) {
				result.RemoveWhere (m => m.DeclaredAccessibility != seenAccessibility);
			}


			// Filter members that are already overriden - they're already part of 'override completion'
			//ISet<ISymbol> realOverridableMembers;
			//if (OverrideContextHandler.TryDetermineOverridableMembers (semanticModel, startToken, seenAccessibility, out realOverridableMembers, cancellationToken)) {
			//	result.RemoveWhere (m => realOverridableMembers.Any (m2 => IsEqualMember (m, m2)));
			//}

			overridableMembers = result;
			return overridableMembers.Count > 0;
		}

		static bool IsEqualMember (ISymbol m, ISymbol m2)
		{
			return SignatureComparerMD.HaveSameSignature (m, m2, true);
		}

		static void AddProtocolMembers (SemanticModel semanticModel, HashSet<ISymbol> result, INamedTypeSymbol type, CancellationToken cancellationToken)
		{
			if (!HasProtocolAttribute (type, out string name))
				return;
			var protocolType = semanticModel.Compilation.GlobalNamespace.GetAllTypesMD (cancellationToken).FirstOrDefault (t => string.Equals (t.Name, name, StringComparison.OrdinalIgnoreCase));
			if (protocolType == null)
				return;

			foreach (var member in protocolType.GetMembers ().OfType<IMethodSymbol> ()) {
				if (member.ExplicitInterfaceImplementations.Length > 0 || member.IsAbstract || !member.IsVirtual)
					continue;
				if (member.GetAttributes ().Any (a => a.AttributeClass.Name == "ExportAttribute" && IsFoundationNamespace (a.AttributeClass.ContainingNamespace.GetFullName ()))) {
					result.Add (member);
				}

			}
			foreach (var member in protocolType.GetMembers ().OfType<IPropertySymbol> ()) {
				if (member.ExplicitInterfaceImplementations.Length > 0 || member.IsAbstract || !member.IsVirtual)
					continue;
				if (member.GetMethod != null && member.GetMethod.GetAttributes ().Any (a => a.AttributeClass.Name == "ExportAttribute" && IsFoundationNamespace (a.AttributeClass.ContainingNamespace.GetFullName ())) ||
					member.SetMethod != null && member.SetMethod.GetAttributes ().Any (a => a.AttributeClass.Name == "ExportAttribute" && IsFoundationNamespace (a.AttributeClass.ContainingNamespace.GetFullName ())))
					result.Add (member);
			}
		}
		internal static bool IsFoundationNamespace (string ns)
		{
			return (ns == "MonoTouch.Foundation" || ns == "Foundation");
		}

		internal static bool IsFoundationNamespace (INamespaceSymbol ns)
		{
			return IsFoundationNamespace (ns.GetFullName ());
		}

		internal static bool HasProtocolAttribute (INamedTypeSymbol type, out string name)
		{
			foreach (var baseType in type.GetAllBaseClassesAndInterfaces (true)) {
				foreach (var attrs in baseType.GetAttributes ()) {
					if (attrs.AttributeClass.Name == "ProtocolAttribute" && IsFoundationNamespace (attrs.AttributeClass.ContainingNamespace.GetFullName ())) {
						foreach (var na in attrs.NamedArguments) {
							if (na.Key != "Name")
								continue;
							name = na.Value.Value as string;
							if (name != null)
								return true;
						}
					}
				}
			}
			name = null;
			return false;
		}

		protected static ISet<ISymbol> FilterOverrides (ISet<ISymbol> members, ITypeSymbol returnType)
		{
			var filteredMembers = new HashSet<ISymbol> (
				from m in members
				where m.GetReturnType ().ToString () == returnType.ToString ()
				select m);

			// Don't filter by return type if we would then have nothing to show.
			// This way, the user gets completion even if they speculatively typed the wrong return type
			if (filteredMembers.Count > 0) {
				members = filteredMembers;
			}

			return members;
		}

		static bool TryDetermineReturnType (SyntaxToken startToken, SemanticModel semanticModel, CancellationToken cancellationToken, out ITypeSymbol returnType, out SyntaxToken nextToken)
		{
			nextToken = startToken;
			returnType = null;
			if (startToken.Parent is TypeSyntax typeSyntax) {

				// 'partial' is actually an identifier.  If we see it just bail.  This does mean
				// we won't handle overrides that actually return a type called 'partial'.  And
				// not a single tear was shed.
				if (typeSyntax is IdentifierNameSyntax &&
					((IdentifierNameSyntax)typeSyntax).Identifier.IsKindOrHasMatchingText (SyntaxKind.PartialKeyword)) {
					return false;
				}

				returnType = semanticModel.GetTypeInfo (typeSyntax, cancellationToken).Type;
				nextToken = typeSyntax.GetFirstToken ().GetPreviousToken ();
			}

			return true;
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

		static bool TryDetermineModifiers (ref SyntaxToken startToken, SourceText text, int startLine, out Accessibility seenAccessibility/*, out DeclarationModifiers modifiers*/)
		{
			var token = startToken;
			//modifiers = new DeclarationModifiers();
			seenAccessibility = Accessibility.NotApplicable;
			var overrideToken = default (SyntaxToken);

			while (IsOnStartLine (text, startLine, token.SpanStart) && !token.IsKind (SyntaxKind.None)) {
				switch (token.Kind ()) {
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
					if (seenAccessibility == Accessibility.NotApplicable) {
						seenAccessibility = Accessibility.Public;
					}

					break;
				case SyntaxKind.InternalKeyword:
					if (seenAccessibility == Accessibility.NotApplicable) {
						seenAccessibility = Accessibility.Internal;
					}

					// If we see internal AND protected, filter for protected internal
					if (seenAccessibility == Accessibility.Protected) {
						seenAccessibility = Accessibility.ProtectedOrInternal;
					}

					break;
				case SyntaxKind.ProtectedKeyword:
					if (seenAccessibility == Accessibility.NotApplicable) {
						seenAccessibility = Accessibility.Protected;
					}

					// If we see protected AND internal, filter for protected internal
					if (seenAccessibility == Accessibility.Internal) {
						seenAccessibility = Accessibility.ProtectedOrInternal;
					}

					break;
				default:
					// Anything else and we bail.
					return false;
				}

				var previousToken = token.GetPreviousToken ();

				// We want only want to consume modifiers
				if (previousToken.IsKind (SyntaxKind.None) || !IsOnStartLine (text, startLine, previousToken.SpanStart)) {
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
			return overrideToken.IsKind (SyntaxKind.OverrideKeyword) && IsOnStartLine (text, startLine, overrideToken.Parent.SpanStart);
		}
	}
}