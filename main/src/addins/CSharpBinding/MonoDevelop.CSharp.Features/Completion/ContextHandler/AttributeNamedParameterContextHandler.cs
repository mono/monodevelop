//
// AttributeNamedParameterContextHandler.cs
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
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using System.Linq;
using System.Collections.Immutable;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.TypeSystem;
using Roslyn.Utilities;

namespace ICSharpCode.NRefactory6.CSharp.Completion
{
	class AttributeNamedParameterContextHandler : CompletionContextHandler
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

			if (token.Kind() != SyntaxKind.OpenParenToken && token.Kind() != SyntaxKind.CommaToken)
			{
				return null;
			}

			var attributeArgumentList = token.Parent as AttributeArgumentListSyntax;
			var attributeSyntax = token.Parent.Parent as AttributeSyntax;
			if (attributeSyntax == null || attributeArgumentList == null)
			{
				return null;
			}

			// We actually want to collect two sets of named parameters to present the user.  The
			// normal named parameters that come from the attribute constructors.  These will be
			// presented like "foo:".  And also the named parameters that come from the writable
			// fields/properties in the attribute.  These will be presented like "bar =".  

			var existingNamedParameters = GetExistingNamedParameters(attributeArgumentList, position);

			var workspace = document.Project.Solution.Workspace;
			var semanticModel = await document.GetCSharpSemanticModelForNodeAsync(attributeSyntax, cancellationToken).ConfigureAwait(false);
			var nameColonItems = await GetNameColonItemsAsync(engine, workspace, semanticModel, position, token, attributeSyntax, existingNamedParameters, cancellationToken).ConfigureAwait(false);
			var nameEqualsItems = await GetNameEqualsItemsAsync(engine, workspace, semanticModel, position, token, attributeSyntax, existingNamedParameters, cancellationToken).ConfigureAwait(false);

			// If we're after a name= parameter, then we only want to show name= parameters.
			if (IsAfterNameEqualsArgument(token))
			{
				return nameEqualsItems;
			}

			return nameColonItems.Concat(nameEqualsItems);
		}
		public override async Task<bool> IsExclusiveAsync(CompletionContext completionContext, SyntaxContext syntaxContext, CompletionTriggerInfo triggerInfo, CancellationToken cancellationToken)
		{
			var syntaxTree = await completionContext.Document.GetCSharpSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
			var token = syntaxTree.FindTokenOnLeftOfPosition(completionContext.Position, cancellationToken)
			                      .GetPreviousTokenIfTouchingWord(completionContext.Position);

			return IsAfterNameColonArgument(token) || IsAfterNameEqualsArgument(token);
		}

		private bool IsAfterNameColonArgument(SyntaxToken token)
		{
			var argumentList = token.Parent as AttributeArgumentListSyntax;
			if (token.Kind() == SyntaxKind.CommaToken && argumentList != null)
			{
				foreach (var item in argumentList.Arguments.GetWithSeparators())
				{
					if (item.IsToken && item.AsToken() == token)
					{
						return false;
					}

					if (item.IsNode)
					{
						var node = item.AsNode() as AttributeArgumentSyntax;
						if (node.NameColon != null)
						{
							return true;
						}
					}
				}
			}

			return false;
		}

		private bool IsAfterNameEqualsArgument(SyntaxToken token)
		{
			var argumentList = token.Parent as AttributeArgumentListSyntax;
			if (token.Kind() == SyntaxKind.CommaToken && argumentList != null)
			{
				foreach (var item in argumentList.Arguments.GetWithSeparators())
				{
					if (item.IsToken && item.AsToken() == token)
					{
						return false;
					}

					if (item.IsNode)
					{
						var node = item.AsNode() as AttributeArgumentSyntax;
						if (node.NameEquals != null)
						{
							return true;
						}
					}
				}
			}

			return false;
		}

		private Task<IEnumerable<CompletionData>> GetNameEqualsItemsAsync(CompletionEngine engine, Workspace workspace, SemanticModel semanticModel,
			int position, SyntaxToken token, AttributeSyntax attributeSyntax, ISet<string> existingNamedParameters,
			CancellationToken cancellationToken)
		{
			var attributeNamedParameters = GetAttributeNamedParameters(semanticModel, position, attributeSyntax, cancellationToken);
			// var unspecifiedNamedParameters = attributeNamedParameters.Where(p => !existingNamedParameters.Contains(p.Name));

			// var text = await semanticModel.SyntaxTree.GetTextAsync(cancellationToken).ConfigureAwait(false);
			return Task.FromResult ( 
				attributeNamedParameters
					.Where (p => !existingNamedParameters.Contains (p.Name))
					.Select (p => {
						var result = engine.Factory.CreateSymbolCompletionData (this, p);
						result.DisplayFlags |= DisplayFlags.NamedArgument;
						return (CompletionData)result;
				}));


		}

		private Task<IEnumerable<CompletionData>> GetNameColonItemsAsync(
			CompletionEngine engine, Workspace workspace, SemanticModel semanticModel, int position, SyntaxToken token, AttributeSyntax attributeSyntax, ISet<string> existingNamedParameters,
			CancellationToken cancellationToken)
		{
			var parameterLists = GetParameterLists(semanticModel, position, attributeSyntax, cancellationToken);
			parameterLists = parameterLists.Where(pl => IsValid(pl, existingNamedParameters));

			// var text = await semanticModel.SyntaxTree.GetTextAsync(cancellationToken).ConfigureAwait(false);
			return Task.FromResult ( 
				from pl in parameterLists
			 from p in pl
			 where !existingNamedParameters.Contains (p.Name)
				select engine.Factory.CreateGenericData(this, p.Name + ":", GenericDataType.NamedParameter));
		}

		private bool IsValid(ImmutableArray<IParameterSymbol> parameterList, ISet<string> existingNamedParameters)
		{
			return existingNamedParameters.Except(parameterList.Select(p => p.Name)).IsEmpty();
		}

		private ISet<string> GetExistingNamedParameters(AttributeArgumentListSyntax argumentList, int position)
		{
			var existingArguments1 =
				argumentList.Arguments.Where(a => a.Span.End <= position)
					.Where(a => a.NameColon != null)
					.Select(a => a.NameColon.Name.Identifier.ValueText);
			var existingArguments2 =
				argumentList.Arguments.Where(a => a.Span.End <= position)
					.Where(a => a.NameEquals != null)
					.Select(a => a.NameEquals.Name.Identifier.ValueText);

			return existingArguments1.Concat(existingArguments2).ToSet();
		}

		private IEnumerable<ImmutableArray<IParameterSymbol>> GetParameterLists(
			SemanticModel semanticModel,
			int position,
			AttributeSyntax attribute,
			CancellationToken cancellationToken)
		{
			var within = semanticModel.GetEnclosingNamedTypeOrAssembly(position, cancellationToken);
			var attributeType = semanticModel.GetTypeInfo(attribute, cancellationToken).Type as INamedTypeSymbol;
			if (within != null && attributeType != null)
			{
				return attributeType.InstanceConstructors.Where(c => c.IsAccessibleWithin(within))
					.Select(c => c.Parameters);
			}

			return SpecializedCollections.EmptyEnumerable<ImmutableArray<IParameterSymbol>>();
		}

		private IEnumerable<ISymbol> GetAttributeNamedParameters(
			SemanticModel semanticModel,
			int position,
			AttributeSyntax attribute,
			CancellationToken cancellationToken)
		{
			var within = semanticModel.GetEnclosingNamedTypeOrAssembly(position, cancellationToken);
			var attributeType = semanticModel.GetTypeInfo(attribute, cancellationToken).Type as INamedTypeSymbol;
			return attributeType.GetAttributeNamedParameters(semanticModel.Compilation, within);
		}

	}
}

