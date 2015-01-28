//
// DebuggerExpressionResolver.cs
//
// Author:
//       David Karlaš <david.karlas@xamarin.com>
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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;

namespace MonoDevelop.CSharp.Resolver
{
	public static class DebuggerExpressionResolver
	{
		public static string Resolve (SemanticModel semanticModel, int offset, out int startOffset)
		{
			var walker = new DebuggerWalker (semanticModel, offset);
			walker.Visit (semanticModel.SyntaxTree.GetRoot ());
			startOffset = walker.StartOffset;
			return walker.ResolvedExpression;
		}

		class DebuggerWalker : CSharpSyntaxWalker
		{
			public int StartOffset {
				get;
				private set;
			}

			public string ResolvedExpression { get; private set; }

			readonly int offset;
			readonly SemanticModel semanticModel;

			public DebuggerWalker (SemanticModel semanticModel, int offset)
			{
				this.semanticModel = semanticModel;
				this.offset = offset;
				this.StartOffset = -1;
			}

			static bool AllMemberAccessExpression (SyntaxNode node)
			{
				var memberNode = node as MemberAccessExpressionSyntax;
				if (memberNode != null) {
					if (memberNode.Expression is IdentifierNameSyntax)//We came to end
						return true;
					return AllMemberAccessExpression (memberNode.Expression);
				} else {
					return false;
				}
			}

			public override void VisitMemberAccessExpression (MemberAccessExpressionSyntax node)
			{
				if (node.Expression.Span.Contains (offset)) {
					base.VisitMemberAccessExpression (node);
				} else {
					if (!(node.Parent is InvocationExpressionSyntax) && AllMemberAccessExpression (node)) {//We don't want some method invocation
						StartOffset = node.SpanStart;
						ResolvedExpression = node.ToString ();
					} else {
						return;
					}
				}
			}

			public override void VisitVariableDeclarator (VariableDeclaratorSyntax node)
			{
				if (node.Identifier.Span.Contains (offset)) {
					StartOffset = node.Identifier.SpanStart;
					ResolvedExpression = node.Identifier.Text;
				} else {
					base.VisitVariableDeclarator (node);
				}
			}

			[System.Diagnostics.DebuggerStepThrough]
			public override void VisitCompilationUnit (CompilationUnitSyntax node)
			{
				var startNode = node.DescendantNodesAndSelf (n => offset <= n.SpanStart).FirstOrDefault ();
				if (startNode == node || startNode == null) {
					base.VisitCompilationUnit (node);
				} else {
					this.Visit (startNode);
				}
			}

			[System.Diagnostics.DebuggerStepThrough]
			public override void Visit (SyntaxNode node)
			{
					
				if (node.Span.End < offset)
					return;
				if (node.Span.Start > offset)
					return;
				base.Visit (node);
			}

			public override void VisitForEachStatement (ForEachStatementSyntax node)
			{
				if (node.Identifier.Span.Contains (offset)) {
					StartOffset = node.Identifier.SpanStart;
					ResolvedExpression = node.Identifier.Text;
				} else {
					base.VisitForEachStatement (node);
				}
			}

			public override void VisitUsingDirective (UsingDirectiveSyntax node)
			{
				if (node.Alias.Name.Span.Contains (offset)) {
					ResolveType (node.Name);//Workaround ResolveType(node.Alias.Name); not working
					StartOffset = node.Alias.Name.SpanStart;
				} else if (node.Name.Span.Contains (offset)) {
					ResolveType (node.Name);
				}
			}

			public override void VisitNamespaceDeclaration (NamespaceDeclarationSyntax node)
			{
				if (!node.Name.Span.Contains (offset)) {
					base.VisitNamespaceDeclaration (node);
				}
			}

			public override void VisitVariableDeclaration (VariableDeclarationSyntax node)
			{
				if (node.Type.IsVar && node.Type.Span.Contains(offset)) {
					return;
				}
				base.VisitVariableDeclaration (node);
			}

			public override void VisitIdentifierName (IdentifierNameSyntax node)
			{
				if (node.Parent is InvocationExpressionSyntax)
					return;
				StartOffset = node.SpanStart;
				ResolvedExpression = node.ToString ();
			}

			void ResolveType (SyntaxNode node)
			{
				var symbolInfo = semanticModel.GetSymbolInfo (node);
				if (symbolInfo.Symbol != null) {
					ResolvedExpression = symbolInfo.Symbol.ToDisplayString (new SymbolDisplayFormat (typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces));
					StartOffset = node.SpanStart;
				}
			}

			public override void VisitPredefinedType (PredefinedTypeSyntax node)
			{
				ResolveType (node);
			}

			public override void VisitGenericName (GenericNameSyntax node)
			{
				StartOffset = node.SpanStart;
				ResolvedExpression = node.ToString ();
			}

			public override void VisitParameter (ParameterSyntax node)
			{
				if (node.Identifier.Span.Contains (offset)) {
					StartOffset = node.Identifier.SpanStart;
					ResolvedExpression = node.Identifier.Text;
				} else {
					base.VisitParameter (node);
				}
			}

			public override void VisitPropertyDeclaration (PropertyDeclarationSyntax node)
			{
				if (node.Identifier.Span.Contains (offset)) {
					StartOffset = node.Identifier.SpanStart;
					ResolvedExpression = node.Identifier.Text;
				} else {
					base.VisitPropertyDeclaration (node);
				}
			}

			public override void VisitFieldDeclaration (FieldDeclarationSyntax node)
			{
				foreach (var variable in node.Declaration.Variables) {
					if (variable.Span.Contains (offset)) {
						ResolvedExpression = node.ToString ();
						StartOffset = node.SpanStart;
						return;
					}
				}
				base.VisitFieldDeclaration (node);
			}
		}
	}
}

