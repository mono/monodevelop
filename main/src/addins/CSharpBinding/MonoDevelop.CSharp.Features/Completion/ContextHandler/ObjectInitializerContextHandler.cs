//
// ObjectInitializerContextHandler.cs
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

namespace ICSharpCode.NRefactory6.CSharp.Completion
{
	class ObjectInitializerContextHandler : CompletionContextHandler
	{
		public override bool SendEnterThroughToEditor (CompletionData completionItem, string textTypedSoFar)
		{
			return false;
		}
		public override async Task<bool> IsExclusiveAsync (CompletionContext completionContext, SyntaxContext syntaxContext, CompletionTriggerInfo triggerInfo, CancellationToken cancellationToken)
		{
			// We're exclusive if this context could only be an object initializer and not also a
			// collection initializer. If we're initializing something that could be initialized as
			// an object or as a collection, say we're not exclusive. That way the rest of
			// intellisense can be used in the collection intitializer.
			// 
			// Consider this case:

			// class c : IEnumerable<int> 
			// { 
			// public void Add(int addend) { }
			// public int foo; 
			// }

			// void foo()
			// {
			//    var b = new c {|
			// }

			// There we could initialize b using either an object initializer or a collection
			// initializer. Since we don't know which the user will use, we'll be non-exclusive, so
			// the other providers can help the user write the collection initializer, if they want
			// to.
			var document = completionContext.Document;
			var position = completionContext.Position;
			var tree = await document.GetCSharpSyntaxTreeAsync (cancellationToken).ConfigureAwait (false);

			if (tree.IsInNonUserCode (position, cancellationToken)) {
				return false;
			}

			var token = tree.FindTokenOnLeftOfPosition (position, cancellationToken);
			token = token.GetPreviousTokenIfTouchingWord (position);

			if (token.Parent == null) {
				return false;
			}

			var expression = token.Parent.Parent as ExpressionSyntax;
			if (expression == null) {
				return false;
			}

			var semanticModel = await document.GetCSharpSemanticModelForNodeAsync (expression, cancellationToken).ConfigureAwait (false);
			var initializedType = semanticModel.GetTypeInfo (expression, cancellationToken).Type;
			if (initializedType == null) {
				return false;
			}

			var enclosingSymbol = semanticModel.GetEnclosingNamedTypeOrAssembly (position, cancellationToken);
			// Non-exclusive if initializedType can be initialized as a collection.
			if (initializedType.CanSupportCollectionInitializer (enclosingSymbol)) {
				return false;
			}

			// By default, only our member names will show up.
			return true;
		}

		public override bool IsTriggerCharacter (Microsoft.CodeAnalysis.Text.SourceText text, int characterPosition)
		{
			return base.IsTriggerCharacter (text, characterPosition) || text [characterPosition] == ' ';
		}

		protected async override Task<IEnumerable<CompletionData>> GetItemsWorkerAsync (CompletionResult completionResult, CompletionEngine engine, CompletionContext completionContext, CompletionTriggerInfo info, SyntaxContext ctx, CancellationToken cancellationToken)
		{
			var document = completionContext.Document;
			var position = completionContext.Position;
			var workspace = document.Project.Solution.Workspace;
			var semanticModel = await document.GetSemanticModelForSpanAsync (new TextSpan (position, 0), cancellationToken).ConfigureAwait (false);
			var typeAndLocation = GetInitializedType (document, semanticModel, position, cancellationToken);
			if (typeAndLocation == null)
				return Enumerable.Empty<CompletionData> ();

			var initializedType = typeAndLocation.Item1 as INamedTypeSymbol;
			var initializerLocation = typeAndLocation.Item2;
			if (initializedType == null)
				return Enumerable.Empty<CompletionData> ();

			var enclosing = semanticModel.GetEnclosingNamedType (position, cancellationToken);

			// Find the members that can be initialized. If we have a NamedTypeSymbol, also get the overridden members.
			IEnumerable<ISymbol> members = semanticModel.LookupSymbols (position, initializedType);
			members = members.Where (m => IsInitializable (m, initializedType) &&
				m.CanBeReferencedByName &&
				IsLegalFieldOrProperty (m, enclosing) &&
				!m.IsImplicitlyDeclared);

			// Filter out those members that have already been typed
			var alreadyTypedMembers = GetInitializedMembers (semanticModel.SyntaxTree, position, cancellationToken);
			var uninitializedMembers = members.Where (m => !alreadyTypedMembers.Contains (m.Name));

			uninitializedMembers = uninitializedMembers.Where (PortingExtensions.IsEditorBrowsable);

			// var text = await semanticModel.SyntaxTree.GetTextAsync(cancellationToken).ConfigureAwait(false);
			// var changes = GetTextChangeSpan(text, position);
			var list = new List<CompletionData> ();

			// Return the members
			foreach (var member in uninitializedMembers) {
				list.Add (engine.Factory.CreateSymbolCompletionData (this, member));
			}
			return list;
		}

		private bool IsLegalFieldOrProperty (ISymbol symbol, ISymbol within)
		{
			var type = symbol.GetMemberType ();
			if (type != null && type.CanSupportCollectionInitializer (within)) {
				return true;
			}

			return symbol.IsWriteableFieldOrProperty ();
		}


		static bool IsInitializable (ISymbol member, INamedTypeSymbol containingType)
		{
			var propertySymbol = member as IPropertySymbol;
			if (propertySymbol != null) {
				if (propertySymbol.Parameters.Any (p => !p.IsOptional))
					return false;
			}


			return
				!member.IsStatic &&
				member.MatchesKind (SymbolKind.Field, SymbolKind.Property) &&
				member.IsAccessibleWithin (containingType);
		}


		static Tuple<ITypeSymbol, Location> GetInitializedType (Document document, SemanticModel semanticModel, int position, CancellationToken cancellationToken)
		{
			var tree = semanticModel.SyntaxTree;
			if (tree.IsInNonUserCode (position, cancellationToken)) {
				return null;
			}

			var token = tree.FindTokenOnLeftOfPosition (position, cancellationToken);
			token = token.GetPreviousTokenIfTouchingWord (position);

			if (token.Kind () != SyntaxKind.CommaToken && token.Kind () != SyntaxKind.OpenBraceToken) {
				return null;
			}

			if (token.Parent == null || token.Parent.Parent == null) {
				return null;
			}

			// If we got a comma, we can syntactically find out if we're in an ObjectInitializerExpression
			if (token.Kind () == SyntaxKind.CommaToken &&
				token.Parent.Kind () != SyntaxKind.ObjectInitializerExpression) {
				return null;
			}

			// new Foo { bar = $$
			if (token.Parent.Parent.IsKind (SyntaxKind.ObjectCreationExpression)) {
				var objectCreation = token.Parent.Parent as ObjectCreationExpressionSyntax;
				if (objectCreation == null) {
					return null;
				}

				var ctor = semanticModel.GetSymbolInfo (objectCreation, cancellationToken).Symbol;
				var type = ctor != null ? ctor.ContainingType : null;
				if (type == null) {
					type = semanticModel.GetSpeculativeTypeInfo (objectCreation.SpanStart, objectCreation.Type, SpeculativeBindingOption.BindAsTypeOrNamespace).Type as INamedTypeSymbol;
				}

				return Tuple.Create<ITypeSymbol, Location> (type, token.GetLocation ());
			}

			// Nested: new Foo { bar = { $$
			if (token.Parent.Parent.IsKind (SyntaxKind.SimpleAssignmentExpression)) {
				// Use the type inferrer to get the type being initialzied.
				var typeInferenceService = TypeGuessing.typeInferenceService;
				var parentInitializer = token.GetAncestor<InitializerExpressionSyntax> ();

				var expectedType = typeInferenceService.InferType (semanticModel, parentInitializer, objectAsDefault: false, cancellationToken: cancellationToken);
				return Tuple.Create (expectedType, token.GetLocation ());
			}

			return null;
		}

		static HashSet<string> GetInitializedMembers (SyntaxTree tree, int position, CancellationToken cancellationToken)
		{
			var token = tree.FindTokenOnLeftOfPosition (position, cancellationToken)
				.GetPreviousTokenIfTouchingWord (position);

			// We should have gotten back a { or ,
			if (token.Kind () == SyntaxKind.CommaToken || token.Kind () == SyntaxKind.OpenBraceToken) {
				if (token.Parent != null) {
					var initializer = token.Parent as InitializerExpressionSyntax;

					if (initializer != null) {
						return new HashSet<string> (initializer.Expressions.OfType<AssignmentExpressionSyntax> ()
							.Where (b => b.OperatorToken.Kind () == SyntaxKind.EqualsToken)
							.Select (b => b.Left)
							.OfType<IdentifierNameSyntax> ()
							.Select (i => i.Identifier.ValueText));
					}
				}
			}

			return new HashSet<string> ();
		}
	}
}
