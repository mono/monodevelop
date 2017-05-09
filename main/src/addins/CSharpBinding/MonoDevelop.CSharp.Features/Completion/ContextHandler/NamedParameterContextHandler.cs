//
// NamedParameterContextHandler.cs
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
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using System.Linq;
using Roslyn.Utilities;
using MonoDevelop.Ide.CodeCompletion;

namespace ICSharpCode.NRefactory6.CSharp.Completion
{
	class NamedParameterContextHandler : CompletionContextHandler, IEqualityComparer<IParameterSymbol>
	{

		protected async override Task<IEnumerable<CompletionData>> GetItemsWorkerAsync (CompletionResult completionResult, CompletionEngine engine, CompletionContext completionContext, CompletionTriggerInfo info, SyntaxContext ctx, CancellationToken cancellationToken)
		{
			var document = completionContext.Document;
			var position = completionContext.Position;
			var syntaxTree = ctx.SyntaxTree;
			if (syntaxTree.IsInNonUserCode(position, cancellationToken))
			{
				return null;
			}

			var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
			token = token.GetPreviousTokenIfTouchingWord(position);

			if (token.Kind() != SyntaxKind.OpenParenToken &&
				token.Kind() != SyntaxKind.OpenBracketToken &&
				token.Kind() != SyntaxKind.CommaToken)
			{
				return null;
			}

			var argumentList = token.Parent as BaseArgumentListSyntax;
			if (argumentList == null)
			{
				return null;
			}

			var semanticModel = await document.GetCSharpSemanticModelForNodeAsync(argumentList, cancellationToken).ConfigureAwait(false);
			var parameterLists = GetParameterLists(semanticModel, position, argumentList.Parent, cancellationToken);
			if (parameterLists == null)
			{
				return null;
			}

			var existingNamedParameters = GetExistingNamedParameters(argumentList, position);
			parameterLists = parameterLists.Where(pl => IsValid(pl, existingNamedParameters));

			var unspecifiedParameters = parameterLists.SelectMany(pl => pl)
				.Where(p => !existingNamedParameters.Contains(p.Name))
				.Distinct(this);

			// var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

			return unspecifiedParameters
				.Select(p => engine.Factory.CreateGenericData(this, p.Name + ":", GenericDataType.NamedParameter));
		}


		private bool IsValid(ImmutableArray<IParameterSymbol> parameterList, ISet<string> existingNamedParameters)
		{
			// A parameter list is valid if it has parameters that match in name all the existing
			// named parameters that have been provided.
			return existingNamedParameters.Except(parameterList.Select(p => p.Name)).IsEmpty();
		}

		private ISet<string> GetExistingNamedParameters(BaseArgumentListSyntax argumentList, int position)
		{
			var existingArguments = argumentList.Arguments.Where(a => a.Span.End <= position && a.NameColon != null)
				.Select(a => a.NameColon.Name.Identifier.ValueText);

			return existingArguments.ToSet();
		}

		private IEnumerable<ImmutableArray<IParameterSymbol>> GetParameterLists(
			SemanticModel semanticModel,
			int position,
			SyntaxNode invocableNode,
			CancellationToken cancellationToken)
		{
			return invocableNode.TypeSwitch(
				(InvocationExpressionSyntax invocationExpression) => GetInvocationExpressionParameterLists(semanticModel, position, invocationExpression, cancellationToken),
				(ConstructorInitializerSyntax constructorInitializer) => GetConstructorInitializerParameterLists(semanticModel, position, constructorInitializer, cancellationToken),
				(ElementAccessExpressionSyntax elementAccessExpression) => GetElementAccessExpressionParameterLists(semanticModel, position, elementAccessExpression, cancellationToken),
				(ObjectCreationExpressionSyntax objectCreationExpression) => GetObjectCreationExpressionParameterLists(semanticModel, position, objectCreationExpression, cancellationToken));
		}

		private IEnumerable<ImmutableArray<IParameterSymbol>> GetObjectCreationExpressionParameterLists(
			SemanticModel semanticModel,
			int position,
			ObjectCreationExpressionSyntax objectCreationExpression,
			CancellationToken cancellationToken)
		{
			var type = semanticModel.GetTypeInfo(objectCreationExpression, cancellationToken).Type as INamedTypeSymbol;
			var within = semanticModel.GetEnclosingNamedType(position, cancellationToken);
			if (type != null && within != null && type.TypeKind != TypeKind.Delegate)
			{
				return type.InstanceConstructors.Where(c => c.IsAccessibleWithin(within))
					.Select(c => c.Parameters);
			}

			return null;
		}

		private IEnumerable<ImmutableArray<IParameterSymbol>> GetElementAccessExpressionParameterLists(
			SemanticModel semanticModel,
			int position,
			ElementAccessExpressionSyntax elementAccessExpression,
			CancellationToken cancellationToken)
		{
			var expressionSymbol = semanticModel.GetSymbolInfo(elementAccessExpression.Expression, cancellationToken).GetAnySymbol();
			var expressionType = semanticModel.GetTypeInfo(elementAccessExpression.Expression, cancellationToken).Type;

			if (expressionSymbol != null && expressionType != null)
			{
				var indexers = semanticModel.LookupSymbols(position, expressionType, WellKnownMemberNames.Indexer).OfType<IPropertySymbol>();
				var within = semanticModel.GetEnclosingNamedTypeOrAssembly(position, cancellationToken);
				if (within != null)
				{
					return indexers.Where(i => i.IsAccessibleWithin(within, throughTypeOpt: expressionType))
						.Select(i => i.Parameters);
				}
			}

			return null;
		}

		private IEnumerable<ImmutableArray<IParameterSymbol>> GetConstructorInitializerParameterLists(
			SemanticModel semanticModel,
			int position,
			ConstructorInitializerSyntax constructorInitializer,
			CancellationToken cancellationToken)
		{
			var within = semanticModel.GetEnclosingNamedType(position, cancellationToken);
			if (within != null &&
				(within.TypeKind == TypeKind.Struct || within.TypeKind == TypeKind.Class))
			{
				var type = constructorInitializer.Kind() == SyntaxKind.BaseConstructorInitializer
					? within.BaseType
					: within;

				if (type != null)
				{
					return type.InstanceConstructors.Where(c => c.IsAccessibleWithin(within))
						.Select(c => c.Parameters);
				}
			}

			return null;
		}

		private IEnumerable<ImmutableArray<IParameterSymbol>> GetInvocationExpressionParameterLists(
			SemanticModel semanticModel,
			int position,
			InvocationExpressionSyntax invocationExpression,
			CancellationToken cancellationToken)
		{
			var within = semanticModel.GetEnclosingNamedTypeOrAssembly(position, cancellationToken);
			if (within != null)
			{
				var methodGroup = semanticModel.GetMemberGroup(invocationExpression.Expression, cancellationToken).OfType<IMethodSymbol>();
				var expressionType = semanticModel.GetTypeInfo(invocationExpression.Expression, cancellationToken).Type as INamedTypeSymbol;

				if (methodGroup.Any())
				{
					return methodGroup.Where(m => m.IsAccessibleWithin(within))
						.Select(m => m.Parameters);
				}
				else if (expressionType.IsDelegateType())
				{
					var delegateType = (INamedTypeSymbol)expressionType;
					return SpecializedCollections.SingletonEnumerable(delegateType.DelegateInvokeMethod.Parameters);
				}
			}

			return null;
		}

		bool IEqualityComparer<IParameterSymbol>.Equals(IParameterSymbol x, IParameterSymbol y)
		{
			return x.Name.Equals(y.Name);
		}

		int IEqualityComparer<IParameterSymbol>.GetHashCode(IParameterSymbol obj)
		{
			return obj.Name.GetHashCode();
		}
	}
}

