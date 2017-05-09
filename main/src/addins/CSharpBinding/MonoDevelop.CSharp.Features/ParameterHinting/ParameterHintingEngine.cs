// 
// CSharpParameterCompletionEngine.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc. (http://xamarin.com)
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
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace ICSharpCode.NRefactory6.CSharp.Completion
{
	class ParameterHintingEngine
	{
		readonly IParameterHintingDataFactory factory;
		readonly Workspace workspace;
	
		public ParameterHintingEngine(Workspace workspace, IParameterHintingDataFactory factory)
		{
			if (workspace == null)
				throw new ArgumentNullException("workspace");
			if (factory == null)
				throw new ArgumentNullException("factory");
			this.workspace = workspace;
			this.factory = factory;
		}

		public Task<ParameterHintingResult> GetParameterDataProviderAsync (Document document, SemanticModel semanticModel, int position, char completionChar, CancellationToken cancellationToken = default (CancellationToken))
		{
			return InternalGetParameterDataProviderAsync (document, semanticModel, position, completionChar, cancellationToken, 0);
		}

		async Task<ParameterHintingResult> InternalGetParameterDataProviderAsync (Document document, SemanticModel semanticModel, int position, char completionChar, CancellationToken cancellationToken, int recCount)
		{
			if (position == 0 || recCount > 1)
				return ParameterHintingResult.Empty;
			var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
			var tokenLeftOfPosition = tree.FindTokenOnLeftOfPosition (position, cancellationToken);
			if (tokenLeftOfPosition.IsKind (SyntaxKind.LessThanToken)) {
				var startToken = tokenLeftOfPosition.GetPreviousToken();
				return HandleTypeParameterCase(semanticModel, startToken.Parent, cancellationToken);
			}
		
			var context = SyntaxContext.Create(workspace, document, semanticModel, position, cancellationToken);
			var targetParent = context.TargetToken.Parent;
			if (targetParent == null)
				return ParameterHintingResult.Empty;

			if (context.TargetToken.IsKind (SyntaxKind.IdentifierName)) {
				targetParent = targetParent.Parent;
			}
			
			if (context.TargetToken.IsKind (SyntaxKind.CloseParenToken) || context.TargetToken.IsKind (SyntaxKind.CloseBracketToken) || context.TargetToken.IsKind (SyntaxKind.GreaterThanToken))
				targetParent = targetParent.Parent;
			if (targetParent == null)
				return ParameterHintingResult.Empty;
			var node = targetParent.Parent;

			// case: identifier<arg1,|
			if (node == null) {
				if (context.LeftToken.Kind() == SyntaxKind.CommaToken) {
					targetParent = context.LeftToken.GetPreviousToken().Parent;
					node = targetParent.Parent;
					if (node.Kind() == SyntaxKind.LessThanExpression) {
						return HandleTypeParameterCase(semanticModel, ((BinaryExpressionSyntax)node).Left, cancellationToken);

					}
				}
				return ParameterHintingResult.Empty;
			}
			if (node.IsKind (SyntaxKind.Argument)) {
				node = node.Parent.Parent;
			} else {
				if (!(targetParent is BaseArgumentListSyntax) && !(targetParent is AttributeArgumentListSyntax) && !(targetParent is InitializerExpressionSyntax)) {
					if (position == targetParent.Span.Start)
						return ParameterHintingResult.Empty;
					return await InternalGetParameterDataProviderAsync (document, semanticModel, targetParent.Span.Start, completionChar, cancellationToken, recCount + 1).ConfigureAwait (false);
				}
			}
			switch (node.Kind()) {
				case SyntaxKind.Attribute:
					return HandleAttribute(semanticModel, node, cancellationToken);					
				case SyntaxKind.ThisConstructorInitializer:
				case SyntaxKind.BaseConstructorInitializer:
					return HandleConstructorInitializer(semanticModel, node, cancellationToken);
				case SyntaxKind.ObjectCreationExpression:
					return HandleObjectCreationExpression(semanticModel, node, cancellationToken);
				case SyntaxKind.InvocationExpression:
					return HandleInvocationExpression(semanticModel, (InvocationExpressionSyntax)node, cancellationToken);
				case SyntaxKind.ElementAccessExpression:
					return HandleElementAccessExpression(semanticModel, (ElementAccessExpressionSyntax)node, cancellationToken);
			}
			return ParameterHintingResult.Empty;
		}

		ParameterHintingResult HandleInvocationExpression(SemanticModel semanticModel, InvocationExpressionSyntax node, CancellationToken cancellationToken)
		{
			var result = new ParameterHintingResult(node.SpanStart);

			var targetTypeInfo = semanticModel.GetTypeInfo (node.Expression);
			if (targetTypeInfo.Type != null && targetTypeInfo.Type.TypeKind == TypeKind.Delegate) {
				result.AddData (factory.CreateMethodDataProvider (targetTypeInfo.Type.GetDelegateInvokeMethod ()));
				return result;
			}

			var within = semanticModel.GetEnclosingNamedTypeOrAssembly (node.SpanStart, cancellationToken);
			if (within == null)
				return result;

			var memberGroup = semanticModel.GetMemberGroup (node.Expression, cancellationToken).OfType<IMethodSymbol> ();
			var matchedMethodSymbol = semanticModel.GetSymbolInfo (node, cancellationToken).Symbol as IMethodSymbol;
			// if the symbol could be bound, replace that item in the symbol list
			if (matchedMethodSymbol != null && matchedMethodSymbol.IsGenericMethod) {
				memberGroup = memberGroup.Select (m => matchedMethodSymbol.OriginalDefinition == m ? matchedMethodSymbol : m);
			}

			ITypeSymbol throughType = null;
			if (node.Expression is MemberAccessExpressionSyntax) {
				var throughExpression = ((MemberAccessExpressionSyntax)node.Expression).Expression;
				var throughSymbol = semanticModel.GetSymbolInfo (throughExpression, cancellationToken).GetAnySymbol ();

				// if it is via a base expression "base.", we know the "throughType" is the base class but
				// we need to be able to tell between "base.M()" and "new Base().M()".
				// currently, Access check methods do not differentiate between them.
				// so handle "base." primary-expression here by nulling out "throughType"
				if (!(throughExpression is BaseExpressionSyntax)) {
					throughType = semanticModel.GetTypeInfo (throughExpression, cancellationToken).Type;
				}

				var includeInstance = !throughExpression.IsKind (SyntaxKind.IdentifierName) ||
					semanticModel.LookupSymbols (throughExpression.SpanStart, name: throughSymbol.Name).Any (s => !(s is INamedTypeSymbol)) ||
					(!(throughSymbol is INamespaceOrTypeSymbol) && semanticModel.LookupSymbols (throughExpression.SpanStart, container: throughSymbol.ContainingType).Any (s => !(s is INamedTypeSymbol)));

				var includeStatic = throughSymbol is INamedTypeSymbol ||
					(throughExpression.IsKind (SyntaxKind.IdentifierName) &&
					semanticModel.LookupNamespacesAndTypes (throughExpression.SpanStart, name: throughSymbol.Name).Any (t => t.GetSymbolType () == throughType));
				
				memberGroup = memberGroup.Where (m => (m.IsStatic && includeStatic) || (!m.IsStatic && includeInstance));
			} else if (node.Expression is SimpleNameSyntax && node.IsInStaticContext ()) {
				memberGroup = memberGroup.Where (m => m.IsStatic);
			}

			var methodList = memberGroup.Where (member => member.IsAccessibleWithin (within, throughType)).ToList();

			memberGroup = methodList.Where (m => !IsHiddenByOtherMethod (m, methodList));
			foreach (var member in memberGroup) {
				result.AddData (factory.CreateMethodDataProvider (member));
			}
			return result;
		}

		bool IsHiddenByOtherMethod (IMethodSymbol method, List<IMethodSymbol> methodSet)
		{
			foreach (var m in methodSet) {
				if (m != method) {
					if (m.IsMoreSpecificThan (method) == true)
						return true;
				}
			}

			return false;
		}

		ParameterHintingResult HandleTypeParameterCase(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
		{
			var result = new ParameterHintingResult(node.SpanStart);
			string typeName;
			var gns = node as GenericNameSyntax;
			if (gns != null) {
				typeName = gns.Identifier.ToString ();
			} else {
				typeName = node.ToString ();
			}

			foreach (var cand in semanticModel.LookupSymbols (node.SpanStart).OfType<INamedTypeSymbol> ()) {
				if (cand.TypeParameters.Length == 0)
					continue;
				if (cand.Name == typeName || MonoDevelop.Ide.TypeSystem.NR5CompatibiltyExtensions.GetFullName (cand) == typeName)
					result.AddData(factory.CreateTypeParameterDataProvider(cand));
			}

			if (result.Count == 0) {
				foreach (var cand in semanticModel.LookupSymbols (node.SpanStart).OfType<IMethodSymbol> ()) {
					if (cand.TypeParameters.Length == 0)
						continue;
					if (cand.Name == typeName)
						result.AddData (factory.CreateTypeParameterDataProvider (cand));
				}
			}
			return result;
		}
			
		ParameterHintingResult HandleAttribute(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
		{
			var info = semanticModel.GetSymbolInfo(node, cancellationToken);
			var result = new ParameterHintingResult(node.SpanStart);
			var resolvedMethod = info.Symbol as IMethodSymbol;
			if (resolvedMethod != null) {
				foreach (var c in resolvedMethod.ContainingType.GetMembers ().OfType<IMethodSymbol> ().Where (m => m.MethodKind == MethodKind.Constructor)) {
					result.AddData (factory.CreateConstructorProvider (c));
				}
			} else {
				result.AddRange (info.CandidateSymbols.OfType<IMethodSymbol> ().Select (factory.CreateConstructorProvider));
			}
			return result;
		}
		
		ParameterHintingResult HandleConstructorInitializer(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
		{
			var info = semanticModel.GetSymbolInfo(node, cancellationToken);
			var result = new ParameterHintingResult(node.SpanStart);
			var resolvedMethod = info.Symbol as IMethodSymbol;
			if (resolvedMethod != null) {
				var type = resolvedMethod.ContainingType;
				var within = semanticModel.GetEnclosingNamedTypeOrAssembly (node.SpanStart, cancellationToken);

				result.AddRange (type.GetMembers ()
				                 .OfType<IMethodSymbol> ()
				                 .Where (m => m.MethodKind == MethodKind.Constructor && m.IsAccessibleWithin (within))
				                 .Select (factory.CreateConstructorProvider));
			} else {
				result.AddRange (info.CandidateSymbols.OfType<IMethodSymbol> ().Select (factory.CreateConstructorProvider));
			}
			return result;
		}
		
		ParameterHintingResult HandleElementAccessExpression(SemanticModel semanticModel, ElementAccessExpressionSyntax node, CancellationToken cancellationToken)
		{
			var within = semanticModel.GetEnclosingNamedTypeOrAssembly(node.SpanStart, cancellationToken);

			var targetTypeInfo = semanticModel.GetTypeInfo (node.Expression);
			ITypeSymbol type = targetTypeInfo.Type;
			if (type == null)
				return ParameterHintingResult.Empty;

			var result = new ParameterHintingResult(node.SpanStart);
			if (type.TypeKind == TypeKind.Array) {
				result.AddData (factory.CreateArrayDataProvider ((IArrayTypeSymbol)type));
				return result;
			}

			var addedProperties = new List<IPropertySymbol> ();
			for (;type != null; type = type.BaseType) {
				foreach (var indexer in type.GetMembers ().OfType<IPropertySymbol> ().Where (p => p.IsIndexer)) {
					if (addedProperties.Any (added => SignatureComparer.Instance.HaveSameSignature (indexer, added, true)))
						continue;

					if (indexer.IsAccessibleWithin (within)) {
						addedProperties.Add (indexer); 
						result.AddData (factory.CreateIndexerParameterDataProvider (indexer, node));
					}
				}
			}
			return result;
		}
		
		ParameterHintingResult HandleObjectCreationExpression (SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
		{
			// var info = semanticModel.GetSymbolInfo(node, cancellationToken);
			var result = new ParameterHintingResult(node.SpanStart);
			var within = semanticModel.GetEnclosingNamedTypeOrAssembly(node.SpanStart, cancellationToken);

			var targetTypeInfo = semanticModel.GetTypeInfo (node);
			if (targetTypeInfo.Type != null) {
				foreach (IMethodSymbol c in targetTypeInfo.Type.GetMembers().OfType<IMethodSymbol>().Where(m => m.MethodKind == MethodKind.Constructor)) {
					if (c.IsAccessibleWithin (within)) {
						result.AddData(factory.CreateConstructorProvider(c));
					}
				}
			}
			return result;
		}
	}
}
