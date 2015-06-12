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
using Microsoft.CodeAnalysis;
using System.Threading;
using System.CodeDom;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using System.Threading.Tasks;

namespace ICSharpCode.NRefactory6.CSharp.Completion
{
	public class ParameterHintingEngine
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
		
		public async Task<ParameterHintingResult> GetParameterDataProviderAsync(Document document, SemanticModel semanticModel, int position, CancellationToken cancellationToken = default(CancellationToken))
		{
			var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
			if (tree.IsInNonUserCode(position, cancellationToken))
				return ParameterHintingResult.Empty;
			var tokenLeftOfPosition = tree.FindTokenOnLeftOfPosition (position, cancellationToken);

			if (tokenLeftOfPosition.IsKind (SyntaxKind.LessThanToken)) {
				var startToken = tokenLeftOfPosition.GetPreviousToken();
				return HandleTypeParameterCase(semanticModel, startToken.Parent, cancellationToken);
			}
		
			var context = SyntaxContext.Create(workspace, document, semanticModel, position, cancellationToken);
			var targetParent = context.TargetToken.Parent;
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
			if (node.IsKind (SyntaxKind.Argument))
				node = node.Parent.Parent;
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
			var info = semanticModel.GetSymbolInfo(node, cancellationToken);
			var result = new ParameterHintingResult(node.SpanStart);

			var targetTypeInfo = semanticModel.GetTypeInfo (node.Expression);
			if (targetTypeInfo.Type != null && targetTypeInfo.Type.TypeKind == TypeKind.Delegate) {
				result.AddData (factory.CreateMethodDataProvider (targetTypeInfo.Type.GetDelegateInvokeMethod ()));
				return result;
			}

			var within = semanticModel.GetEnclosingNamedTypeOrAssembly(node.SpanStart, cancellationToken);
			ITypeSymbol type;
			var ma = node.Expression as MemberAccessExpressionSyntax;
			string name = null;
			bool staticLookup = false;
			if (ma != null) {
				staticLookup = semanticModel.GetSymbolInfo (ma.Expression).Symbol is ITypeSymbol;
				type = semanticModel.GetTypeInfo (ma.Expression).Type; 
				name = ma.Name.ToString ();
			} else {
				type = within as ITypeSymbol;
				name = node.Expression.ToString ();
				var sym = semanticModel.GetEnclosingSymbol (node.SpanStart, cancellationToken); 
				staticLookup = sym.IsStatic;
			}
			var addedMethods = new List<IMethodSymbol> ();
			var filterMethod = new HashSet<IMethodSymbol> ();
			for (;type != null; type = type.BaseType) {
				foreach (var method in type.GetMembers ().OfType<IMethodSymbol> ().Where (m => m.Name == name)) {
					if (staticLookup && !method.IsStatic)
						continue;
					if (method.OverriddenMethod != null)
						filterMethod.Add (method.OverriddenMethod);
					if (filterMethod.Contains (method))
						continue;
					if (addedMethods.Any (added => SignatureComparer.HaveSameSignature (method, added, true)))
						continue;
					if (method.IsAccessibleWithin (within)) {
						addedMethods.Add (method); 
						result.AddData (factory.CreateMethodDataProvider (method));
					}
				}
			}
			if (info.Symbol != null && !addedMethods.Contains (info.Symbol)) {
				if (!staticLookup || info.Symbol.IsStatic)
					result.AddData (factory.CreateMethodDataProvider ((IMethodSymbol)info.Symbol));
			}
			foreach (var candidate in info.CandidateSymbols) {
				if (staticLookup && !candidate.IsStatic)
					continue;
				
				if (!addedMethods.Contains (candidate) && candidate.IsAccessibleWithin (within))
					result.AddData (factory.CreateMethodDataProvider ((IMethodSymbol)candidate));
			}
			return result;
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
				if (cand.Name == typeName || cand.GetFullName () == typeName)
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
			if (resolvedMethod != null)
				result.AddData(factory.CreateConstructorProvider(resolvedMethod));
			result.AddRange(info.CandidateSymbols.OfType<IMethodSymbol>().Select (m => factory.CreateConstructorProvider(m)));
			return result;
		}
		
		ParameterHintingResult HandleConstructorInitializer(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
		{
			var info = semanticModel.GetSymbolInfo(node, cancellationToken);
			var result = new ParameterHintingResult(node.SpanStart);
			
			var resolvedMethod = info.Symbol as IMethodSymbol;
			if (resolvedMethod != null)
				result.AddData(factory.CreateConstructorProvider(resolvedMethod));
			result.AddRange(info.CandidateSymbols.OfType<IMethodSymbol>().Select (m => factory.CreateConstructorProvider(m)));
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
					if (addedProperties.Any (added => SignatureComparer.HaveSameSignature (indexer, added, true)))
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
