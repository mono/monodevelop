// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.NRefactory6.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Debugger;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.CSharp.Resolver
{
	static class DebuggerExpressionResolver
	{
		public static async Task<DebugDataTipInfo> ResolveAsync (IReadonlyTextDocument editor, DocumentContext document, int offset, CancellationToken cancellationToken)
		{
			var analysisDocument = document.AnalysisDocument;
			DebugDataTipInfo result;
			CompilationUnitSyntax compilationUnit = null;
			if (analysisDocument == null) {
				compilationUnit = SyntaxFactory.ParseCompilationUnit (editor.Text);
				result = GetInfo (compilationUnit, null, offset, default(CancellationToken));
			} else {
				compilationUnit = await analysisDocument.GetCSharpSyntaxRootAsync (cancellationToken).ConfigureAwait (false);
				var semantic = document.ParsedDocument?.GetAst<SemanticModel> ();
				if (semantic == null) {
					semantic = await analysisDocument.GetSemanticModelAsync (cancellationToken).ConfigureAwait (false);
				}
				result = GetInfo (compilationUnit, semantic, offset, default(CancellationToken));
			}
			if (result.IsDefault || !result.Span.Contains(offset)) {
				return new DebugDataTipInfo (result.Span, null);
			} else if (result.Text == null) {
				return new DebugDataTipInfo (result.Span, compilationUnit.GetText ().ToString (result.Span));
			} else {
				return result;
			}
		}

		static DebugDataTipInfo GetInfo (CompilationUnitSyntax root, SemanticModel semanticModel, int position, CancellationToken cancellationToken)
		{
			var token = root.FindToken (position);
			string textOpt = null;

			var expression = token.Parent as ExpressionSyntax;
			if (expression == null) {
				if (Microsoft.CodeAnalysis.CSharpExtensions.IsKind (token, SyntaxKind.IdentifierToken)) {
					if (token.Parent is MethodDeclarationSyntax) {
						return default(DebugDataTipInfo);
					}
					if (semanticModel != null) {
						if (token.Parent is PropertyDeclarationSyntax) {
							var propertySymbol = semanticModel.GetDeclaredSymbol ((PropertyDeclarationSyntax)token.Parent);
							if (propertySymbol.IsStatic) {
								textOpt = propertySymbol.ContainingType.GetFullName () + "." + propertySymbol.Name;
							}
						} else if (token.GetAncestor<FieldDeclarationSyntax> () != null) {
							var fieldSymbol = semanticModel.GetDeclaredSymbol (token.GetAncestor<VariableDeclaratorSyntax> ());
							if (fieldSymbol.IsStatic) {
								textOpt = fieldSymbol.ContainingType.GetFullName () + "." + fieldSymbol.Name;
							}
						}
					}

					return new DebugDataTipInfo (token.Span, text: textOpt);
				} else {
					return default(DebugDataTipInfo);
				}
			}

			if (expression.IsAnyLiteralExpression ()) {
				// If the user hovers over a literal, give them a DataTip for the type of the
				// literal they're hovering over.
				// Partial semantics should always be sufficient because the (unconverted) type
				// of a literal can always easily be determined.
				var type = semanticModel?.GetTypeInfo (expression, cancellationToken).Type;
				return type == null
					? default(DebugDataTipInfo)
						: new DebugDataTipInfo (expression.Span, type.GetFullName ());
			}

			// Check if we are invoking method and if we do return null so we don't invoke it
			if (expression.Parent is InvocationExpressionSyntax ||
				(semanticModel != null &&
				  expression.Parent is MemberAccessExpressionSyntax &&
				  expression.Parent.Parent is InvocationExpressionSyntax &&
				  semanticModel.GetSymbolInfo (token).Symbol is IMethodSymbol))
			{
				return default(DebugDataTipInfo);
			}

			if (expression.IsRightSideOfDotOrArrow ()) {
				var curr = expression;
				while (true) {
					var conditionalAccess = curr.GetParentConditionalAccessExpression ();
					if (conditionalAccess == null) {
						break;
					}

					curr = conditionalAccess;
				}

				if (curr == expression) {
					// NB: Parent.Span, not Span as below.
					return new DebugDataTipInfo (expression.Parent.Span, text: null);
				}

				// NOTE: There may not be an ExpressionSyntax corresponding to the range we want.
				// For example, for input a?.$$B?.C, we want span [|a?.B|]?.C.
				return new DebugDataTipInfo (TextSpan.FromBounds (curr.SpanStart, expression.Span.End), text: null);
			}

			var typeSyntax = expression as TypeSyntax;
			if (typeSyntax != null && typeSyntax.IsVar) {
				// If the user is hovering over 'var', then pass back the full type name that 'var'
				// binds to.
				var type = semanticModel?.GetTypeInfo (typeSyntax, cancellationToken).Type;
				if (type != null) {
					textOpt = type.GetFullName ();
				}
			}

			if (semanticModel != null) {
				if (expression is IdentifierNameSyntax) {
					if (expression.Parent is ObjectCreationExpressionSyntax) {
						textOpt = ((INamedTypeSymbol)semanticModel.GetSymbolInfo (expression).Symbol).GetFullName ();
					} else if (expression.Parent is AssignmentExpressionSyntax && expression.Parent.Parent is InitializerExpressionSyntax) {
						var variable = expression.GetAncestor<VariableDeclaratorSyntax> ();
						if (variable != null) {
							textOpt = variable.Identifier.Text + "." + ((IdentifierNameSyntax)expression).Identifier.Text;
						}
					}

				}
			}
			return new DebugDataTipInfo (expression.Span, textOpt);
		}
	}
}

