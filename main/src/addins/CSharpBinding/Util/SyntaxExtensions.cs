//
// SyntaxExtensions.cs
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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Simplification;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Rename.ConflictEngine;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using System;
using System.Reflection;
using System.Collections.Immutable;
using System.Runtime.ExceptionServices;
using MonoDevelop.Ide.TypeSystem;

namespace ICSharpCode.NRefactory6.CSharp
{
	static class SyntaxExtensions
	{
		readonly static MethodInfo canRemoveParenthesesMethod;
//		readonly static MethodInfo isLeftSideOfDotMethod;
//		readonly static MethodInfo isRightSideOfDotMethod;
//		readonly static MethodInfo getEnclosingNamedTypeMethod;
		readonly static MethodInfo getLocalDeclarationMapMethod;
		readonly static PropertyInfo localDeclarationMapIndexer;
		readonly static MethodInfo getAncestorsMethod;

		static SyntaxExtensions()
		{
			var typeInfo = Type.GetType("Microsoft.CodeAnalysis.CSharp.Extensions.ParenthesizedExpressionSyntaxExtensions" + ReflectionNamespaces.CSWorkspacesAsmName, true);
			canRemoveParenthesesMethod = typeInfo.GetMethod("CanRemoveParentheses", new[] { typeof(ParenthesizedExpressionSyntax) });

//			typeInfo = Type.GetType("Microsoft.CodeAnalysis.CSharp.Extensions.ExpressionSyntaxExtensions" + ReflectionNamespaces.CSWorkspacesAsmName, true);
//			isLeftSideOfDotMethod = typeInfo.GetMethod("IsLeftSideOfDot", new[] { typeof(ExpressionSyntax) });
//			isRightSideOfDotMethod = typeInfo.GetMethod("IsRightSideOfDot", new[] { typeof(ExpressionSyntax) });
//
//			typeInfo = Type.GetType("Microsoft.CodeAnalysis.Shared.Extensions.SemanticModelExtensions" + ReflectionNamespaces.WorkspacesAsmName, true);
//			getEnclosingNamedTypeMethod = typeInfo.GetMethod("GetEnclosingNamedType", new[] { typeof(SemanticModel), typeof(int), typeof(CancellationToken) });
//
			typeInfo = Type.GetType("Microsoft.CodeAnalysis.CSharp.Extensions.MemberDeclarationSyntaxExtensions" + ReflectionNamespaces.CSWorkspacesAsmName, true);
			getLocalDeclarationMapMethod = typeInfo.GetMethod("GetLocalDeclarationMap", new[] { typeof(MemberDeclarationSyntax) });

			typeInfo = Type.GetType("Microsoft.CodeAnalysis.CSharp.Extensions.MemberDeclarationSyntaxExtensions+LocalDeclarationMap" + ReflectionNamespaces.CSWorkspacesAsmName, true);
			localDeclarationMapIndexer = typeInfo.GetProperties().Single(p => p.GetIndexParameters().Any());

			typeInfo = Type.GetType("Microsoft.CodeAnalysis.Shared.Extensions.SyntaxTokenExtensions" + ReflectionNamespaces.WorkspacesAsmName, true);
			getAncestorsMethod = typeInfo.GetMethods().Single(m => m.Name == "GetAncestors" && m.IsGenericMethod && m.GetParameters().Length == 1);
		}


		/// <summary>
		/// Look inside a trivia list for a skipped token that contains the given position.
		/// </summary>
		private static readonly Func<SyntaxTriviaList, int, SyntaxToken> s_findSkippedTokenBackward =
			(l, p) => FindTokenHelper.FindSkippedTokenBackward(GetSkippedTokens(l), p);

		/// <summary>
		/// return only skipped tokens
		/// </summary>
		private static IEnumerable<SyntaxToken> GetSkippedTokens(SyntaxTriviaList list)
		{
			return list.Where(trivia => trivia.RawKind == (int)SyntaxKind.SkippedTokensTrivia)
				.SelectMany(t => ((SkippedTokensTriviaSyntax)t.GetStructure()).Tokens);
		}

		/// <summary>
		/// If the position is inside of token, return that token; otherwise, return the token to the left.
		/// </summary>
		public static SyntaxToken FindTokenOnLeftOfPosition(
			this SyntaxNode root,
			int position,
			bool includeSkipped = true,
			bool includeDirectives = false,
			bool includeDocumentationComments = false)
		{
			var skippedTokenFinder = includeSkipped ? s_findSkippedTokenBackward : (Func<SyntaxTriviaList, int, SyntaxToken>)null;

			return FindTokenHelper.FindTokenOnLeftOfPosition<CompilationUnitSyntax>(
				root, position, skippedTokenFinder, includeSkipped, includeDirectives, includeDocumentationComments);
		}



//		public static bool IntersectsWith(this SyntaxToken token, int position)
//		{
//			return token.Span.IntersectsWith(position);
//		}

//		public static bool IsLeftSideOfDot(this ExpressionSyntax syntax)
//		{
//			return (bool)isLeftSideOfDotMethod.Invoke(null, new object[] { syntax });
//		}
//
//		public static bool IsRightSideOfDot(this ExpressionSyntax syntax)
//		{
//			return (bool)isRightSideOfDotMethod.Invoke(null, new object[] { syntax });
//		}

//		public static INamedTypeSymbol GetEnclosingNamedType(this SemanticModel semanticModel, int position, CancellationToken cancellationToken)
//		{
//			return (INamedTypeSymbol)getEnclosingNamedTypeMethod.Invoke(null, new object[] { semanticModel, position, cancellationToken });
//		}
//
		static ImmutableArray<SyntaxToken> GetLocalDeclarationMap(this MemberDeclarationSyntax member, string localName)
		{
			try {
				object map = getLocalDeclarationMapMethod.Invoke(null, new object[] { member });
				return (ImmutableArray<SyntaxToken>)localDeclarationMapIndexer.GetValue(map, new object[] { localName });
			} catch (TargetInvocationException ex) {
				ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
				return ImmutableArray<SyntaxToken>.Empty;
			}
		}

		static IEnumerable<T> GetAncestors<T>(this SyntaxToken token) where T : SyntaxNode
		{
			try {
				return (IEnumerable<T>)getAncestorsMethod.MakeGenericMethod(typeof(T)).Invoke(null, new object[] { token });
			} catch (TargetInvocationException ex) {
				ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
				return null;
			}
		}

		public static ExpressionSyntax SkipParens(this ExpressionSyntax expression)
		{
			if (expression == null)
				return null;
			while (expression != null && expression.IsKind(SyntaxKind.ParenthesizedExpression)) {
				expression = ((ParenthesizedExpressionSyntax)expression).Expression;
			}
			return expression;
		}

		public static SyntaxNode SkipArgument(this SyntaxNode expression)
		{
			if (expression is ArgumentSyntax)
				return ((ArgumentSyntax)expression).Expression;
			return expression;
		}

		public static bool CanRemoveParentheses(this ParenthesizedExpressionSyntax node)
		{
			try {
				return (bool)canRemoveParenthesesMethod.Invoke(null, new object[] { node }); 
			} catch (TargetInvocationException ex) {
				ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
				return false;
			}
		}

		public static bool IsParentKind(this SyntaxNode node, SyntaxKind kind)
		{
			return node != null && node.Parent.IsKind(kind);
		}

		public static bool IsParentKind(this SyntaxToken node, SyntaxKind kind)
		{
			return node.Parent != null && node.Parent.IsKind(kind);
		}

		public static bool CanReplaceWithReducedName(
			this MemberAccessExpressionSyntax memberAccess,
			ExpressionSyntax reducedName,
			SemanticModel semanticModel,
			CancellationToken cancellationToken)
		{
			if (!IsThisOrTypeOrNamespace(memberAccess, semanticModel)) {
				return false;
			}

			var speculationAnalyzer = new SpeculationAnalyzer(memberAccess, reducedName, semanticModel, cancellationToken);
			if (!speculationAnalyzer.SymbolsForOriginalAndReplacedNodesAreCompatible() ||
			    speculationAnalyzer.ReplacementChangesSemantics()) {
				return false;
			}

			if (WillConflictWithExistingLocal(memberAccess, reducedName)) {
				return false;
			}

			if (IsMemberAccessADynamicInvocation(memberAccess, semanticModel)) {
				return false;
			}

			if (memberAccess.AccessMethodWithDynamicArgumentInsideStructConstructor(semanticModel)) {
				return false;
			}

			if (memberAccess.Expression.Kind() == SyntaxKind.BaseExpression) {
				var enclosingNamedType = semanticModel.GetEnclosingNamedType(memberAccess.SpanStart, cancellationToken);
				var symbol = semanticModel.GetSymbolInfo(memberAccess.Name).Symbol;
				if (enclosingNamedType != null &&
				    !enclosingNamedType.IsSealed &&
				    symbol != null &&
					symbol.IsOverridable()) {
					return false;
				}
			}

			var invalidTransformation1 = ParserWouldTreatExpressionAsCast(reducedName, memberAccess);

			return !invalidTransformation1;
		}

		internal static bool IsValidSymbolInfo(ISymbol symbol)
		{
			// name bound to only one symbol is valid
			return symbol != null && !symbol.IsErrorType ();
		}

		private static bool IsThisOrTypeOrNamespace(MemberAccessExpressionSyntax memberAccess, SemanticModel semanticModel)
		{
			if (memberAccess.Expression.Kind() == SyntaxKind.ThisExpression) {
				var previousToken = memberAccess.Expression.GetFirstToken().GetPreviousToken();

				var symbol = semanticModel.GetSymbolInfo(memberAccess.Name).Symbol;

				if (previousToken.Kind() == SyntaxKind.OpenParenToken &&
				    previousToken.IsParentKind(SyntaxKind.ParenthesizedExpression) &&
				    !previousToken.Parent.IsParentKind(SyntaxKind.ParenthesizedExpression) &&
				    ((ParenthesizedExpressionSyntax)previousToken.Parent).Expression.Kind() == SyntaxKind.SimpleMemberAccessExpression &&
				    symbol != null && symbol.Kind == SymbolKind.Method) {
					return false;
				}

				return true;
			}

			var expressionInfo = semanticModel.GetSymbolInfo(memberAccess.Expression);
			if (IsValidSymbolInfo(expressionInfo.Symbol)) {
				if (expressionInfo.Symbol is INamespaceOrTypeSymbol) {
					return true;
				}

				if (expressionInfo.Symbol.IsThisParameter()) {
					return true;
				}
			}

			return false;
		}

		private static bool WillConflictWithExistingLocal(ExpressionSyntax expression, ExpressionSyntax simplifiedNode)
		{
			if (simplifiedNode.Kind() == SyntaxKind.IdentifierName && !SyntaxFacts.IsInNamespaceOrTypeContext(expression)) {
				var identifierName = (IdentifierNameSyntax)simplifiedNode;
				var enclosingDeclarationSpace = FindImmediatelyEnclosingLocalVariableDeclarationSpace(expression);
				var enclosingMemberDeclaration = expression.FirstAncestorOrSelf<MemberDeclarationSyntax>();
				if (enclosingDeclarationSpace != null && enclosingMemberDeclaration != null) {
					var locals = enclosingMemberDeclaration.GetLocalDeclarationMap(identifierName.Identifier.ValueText);
					foreach (var token in locals) {
						if (GetAncestors<SyntaxNode>(token).Contains(enclosingDeclarationSpace)) {
							return true;
						}
					}
				}
			}

			return false;
		}

		private static SyntaxNode FindImmediatelyEnclosingLocalVariableDeclarationSpace(SyntaxNode syntax)
		{
			for (var declSpace = syntax; declSpace != null; declSpace = declSpace.Parent) {
				switch (declSpace.Kind()) {
				// These are declaration-space-defining syntaxes, by the spec:
					case SyntaxKind.MethodDeclaration:
					case SyntaxKind.IndexerDeclaration:
					case SyntaxKind.OperatorDeclaration:
					case SyntaxKind.ConstructorDeclaration:
					case SyntaxKind.Block:
					case SyntaxKind.ParenthesizedLambdaExpression:
					case SyntaxKind.SimpleLambdaExpression:
					case SyntaxKind.AnonymousMethodExpression:
					case SyntaxKind.SwitchStatement:
					case SyntaxKind.ForEachKeyword:
					case SyntaxKind.ForStatement:
					case SyntaxKind.UsingStatement:

						// SPEC VIOLATION: We also want to stop walking out if, say, we are in a field
						// initializer. Technically according to the wording of the spec it should be
						// legal to use a simple name inconsistently inside a field initializer because
						// it does not define a local variable declaration space. In practice of course
						// we want to check for that. (As the native compiler does as well.)

					case SyntaxKind.FieldDeclaration:
						return declSpace;
				}
			}

			return null;
		}

		private static bool ParserWouldTreatExpressionAsCast(ExpressionSyntax reducedNode, MemberAccessExpressionSyntax originalNode)
		{
			SyntaxNode parent = originalNode;
			while (parent != null) {
				if (parent.IsParentKind(SyntaxKind.SimpleMemberAccessExpression)) {
					parent = parent.Parent;
					continue;
				}

				if (!parent.IsParentKind(SyntaxKind.ParenthesizedExpression)) {
					return false;
				}

				break;
			}

			var newExpression = parent.ReplaceNode((SyntaxNode)originalNode, reducedNode);

			// detect cast ambiguities according to C# spec #7.7.6 
			if (IsNameOrMemberAccessButNoExpression(newExpression)) {
				var nextToken = parent.Parent.GetLastToken().GetNextToken();

				return nextToken.Kind() == SyntaxKind.OpenParenToken ||
				nextToken.Kind() == SyntaxKind.TildeToken ||
				nextToken.Kind() == SyntaxKind.ExclamationToken ||
				(SyntaxFacts.IsKeywordKind(nextToken.Kind()) && !(nextToken.Kind() == SyntaxKind.AsKeyword || nextToken.Kind() == SyntaxKind.IsKeyword));
			}

			return false;
		}

		private static bool IsMemberAccessADynamicInvocation(MemberAccessExpressionSyntax memberAccess, SemanticModel semanticModel)
		{
			var ancestorInvocation = memberAccess.FirstAncestorOrSelf<InvocationExpressionSyntax>();

			if (ancestorInvocation != null && ancestorInvocation.SpanStart == memberAccess.SpanStart) {
				var typeInfo = semanticModel.GetTypeInfo(ancestorInvocation);
				if (typeInfo.Type != null &&
				    typeInfo.Type.Kind == SymbolKind.DynamicType) {
					return true;
				}
			}

			return false;
		}

		private static bool IsNameOrMemberAccessButNoExpression(SyntaxNode node)
		{
			if (node.IsKind(SyntaxKind.SimpleMemberAccessExpression)) {
				var memberAccess = (MemberAccessExpressionSyntax)node;

				return memberAccess.Expression.IsKind(SyntaxKind.IdentifierName) ||
				IsNameOrMemberAccessButNoExpression(memberAccess.Expression);
			}

			return node.IsKind(SyntaxKind.IdentifierName);
		}

		private static bool AccessMethodWithDynamicArgumentInsideStructConstructor(this MemberAccessExpressionSyntax memberAccess, SemanticModel semanticModel)
		{
			var constructor = memberAccess.Ancestors().OfType<ConstructorDeclarationSyntax>().SingleOrDefault();

			if (constructor == null || constructor.Parent.Kind() != SyntaxKind.StructDeclaration) {
				return false;
			}

			return semanticModel.GetSymbolInfo(memberAccess.Name).CandidateReason == CandidateReason.LateBound;
		}


		public static bool CanReplaceWithReducedName(this NameSyntax name, TypeSyntax reducedName, SemanticModel semanticModel, CancellationToken cancellationToken)
		{
			var speculationAnalyzer = new SpeculationAnalyzer(name, reducedName, semanticModel, cancellationToken);
			if (speculationAnalyzer.ReplacementChangesSemantics())
			{
				return false;
			}

			return CanReplaceWithReducedNameInContext(name, reducedName, semanticModel, cancellationToken);
		}

		private static bool CanReplaceWithReducedNameInContext(this NameSyntax name, TypeSyntax reducedName, SemanticModel semanticModel, CancellationToken cancellationToken)
		{
			// Special case.  if this new minimal name parses out to a predefined type, then we
			// have to make sure that we're not in a using alias. That's the one place where the
			// language doesn't allow predefined types. You have to use the fully qualified name
			// instead.
			var invalidTransformation1 = IsNonNameSyntaxInUsingDirective(name, reducedName);
			var invalidTransformation2 = WillConflictWithExistingLocal(name, reducedName);
			var invalidTransformation3 = IsAmbiguousCast(name, reducedName);
			var invalidTransformation4 = IsNullableTypeInPointerExpression(name, reducedName);
			var isNotNullableReplacable = name.IsNotNullableReplacable(reducedName);

			if (invalidTransformation1 || invalidTransformation2 || invalidTransformation3 || invalidTransformation4
				|| isNotNullableReplacable)
			{
				return false;
			}

			return true;
		}

		private static bool IsNullableTypeInPointerExpression(ExpressionSyntax expression, ExpressionSyntax simplifiedNode)
		{
			// Note: nullable type syntax is not allowed in pointer type syntax
			if (simplifiedNode.Kind() == SyntaxKind.NullableType &&
				simplifiedNode.DescendantNodes().Any(n => n is PointerTypeSyntax))
			{
				return true;
			}

			return false;
		}

		private static bool IsAmbiguousCast(ExpressionSyntax expression, ExpressionSyntax simplifiedNode)
		{
			// Can't simplify a type name in a cast expression if it would then cause the cast to be
			// parsed differently.  For example:  (Foo::Bar)+1  is a cast.  But if that simplifies to
			// (Bar)+1  then that's an arithmetic expression.
			if (expression.IsParentKind(SyntaxKind.CastExpression))
			{
				var castExpression = (CastExpressionSyntax)expression.Parent;
				if (castExpression.Type == expression)
				{
					var newCastExpression = castExpression.ReplaceNode((SyntaxNode)castExpression.Type, simplifiedNode);
					var reparsedCastExpression = SyntaxFactory.ParseExpression(newCastExpression.ToString());

					if (!reparsedCastExpression.IsKind(SyntaxKind.CastExpression))
					{
						return true;
					}
				}
			}

			return false;
		}
		private static bool IsNonNameSyntaxInUsingDirective(ExpressionSyntax expression, ExpressionSyntax simplifiedNode)
		{
			return
				expression.IsParentKind(SyntaxKind.UsingDirective) &&
				!(simplifiedNode is NameSyntax);
		}

		private static bool IsNotNullableReplacable(this NameSyntax name, TypeSyntax reducedName)
		{
			var isNotNullableReplacable = false;
			// var isLeftSideOfDot = name.IsLeftSideOfDot();
			// var isRightSideOfDot = name.IsRightSideOfDot();

			if (reducedName.Kind() == SyntaxKind.NullableType)
			{
				if (((NullableTypeSyntax)reducedName).ElementType.Kind() == SyntaxKind.OmittedTypeArgument)
				{
					isNotNullableReplacable = true;
				}
				else
				{
					isNotNullableReplacable = name.IsLeftSideOfDot() || name.IsRightSideOfDot();
				}
			}

			return isNotNullableReplacable;
		}

		public static SyntaxTokenList GetModifiers (this MemberDeclarationSyntax member)
		{
			if (member == null)
				throw new ArgumentNullException("member");
			var method = member as BaseMethodDeclarationSyntax;
			if (method != null)
				return method.Modifiers;
			var property = member as BasePropertyDeclarationSyntax;
			if (property != null)
				return property.Modifiers;
			var field = member as BaseFieldDeclarationSyntax;
			if (field != null)
				return field.Modifiers;
			return new SyntaxTokenList ();
		}

		public static ExplicitInterfaceSpecifierSyntax GetExplicitInterfaceSpecifierSyntax (this MemberDeclarationSyntax member)
		{
			if (member == null)
				throw new ArgumentNullException("member");
			var method = member as MethodDeclarationSyntax;
			if (method != null)
				return method.ExplicitInterfaceSpecifier;
			var property = member as BasePropertyDeclarationSyntax;
			if (property != null)
				return property.ExplicitInterfaceSpecifier;
			var evt = member as EventDeclarationSyntax;
			if (evt != null)
				return evt.ExplicitInterfaceSpecifier;
			return null;
		}
//		public static bool IsKind(this SyntaxToken token, SyntaxKind kind)
//		{
//			return token.RawKind == (int)kind;
//		}
//
//		public static bool IsKind(this SyntaxTrivia trivia, SyntaxKind kind)
//		{
//			return trivia.RawKind == (int)kind;
//		}
//
//		public static bool IsKind(this SyntaxNode node, SyntaxKind kind)
//		{
//			return node?.RawKind == (int)kind;
//		}
//
//		public static bool IsKind(this SyntaxNodeOrToken nodeOrToken, SyntaxKind kind)
//		{
//			return nodeOrToken.RawKind == (int)kind;
//		}
//

//		public static SyntaxNode GetParent(this SyntaxNode node)
//		{
//			return node != null ? node.Parent : null;
//		}
	}
}

