//
// CSharpCompletionTextEditorExtension.IDebuggerExpressionResolver.cs
//
// Author:
//       David Karlaš <david.karlas@xamarin.com>
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2018 Microsoft Corporation. All rights reserved.
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


using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using MonoDevelop.Debugger;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.CSharp.Completion
{
	partial class CSharpCompletionTextEditorExtension : IDebuggerExpressionResolver
	{
		async Task<DebugDataTipInfo> IDebuggerExpressionResolver.ResolveExpressionAsync (IReadonlyTextDocument editor, DocumentContext doc, int offset, CancellationToken cancellationToken)
		{
			var analysisDocument = doc.AnalysisDocument;
			if (analysisDocument == null)
				return default (DebugDataTipInfo);
			var debugInfoService = analysisDocument.GetLanguageService<Microsoft.CodeAnalysis.Editor.Implementation.Debugging.ILanguageDebugInfoService> ();
			if (debugInfoService == null)
				return default (DebugDataTipInfo);

			var tipInfo = await debugInfoService.GetDataTipInfoAsync (analysisDocument, offset, cancellationToken).ConfigureAwait (false);
			var text = tipInfo.Text;
			if (text == null && !tipInfo.IsDefault)
				text = editor.GetTextAt (tipInfo.Span.Start, tipInfo.Span.Length);

			var semanticModel = await analysisDocument.GetSemanticModelAsync (cancellationToken).ConfigureAwait (false);
			var root = await semanticModel.SyntaxTree.GetRootAsync (cancellationToken).ConfigureAwait (false);
			var syntaxNode = root.FindNode (tipInfo.Span);
			if (syntaxNode == null)
				return new DebugDataTipInfo (tipInfo.Span, text);
			return GetInfo (root, semanticModel, syntaxNode, text, cancellationToken);
		}

		static DebugDataTipInfo GetInfo (SyntaxNode root, SemanticModel semanticModel, SyntaxNode node, string textOpt, CancellationToken cancellationToken)
		{
			var expression = node as ExpressionSyntax;
			if (expression == null) {
				if (node is MethodDeclarationSyntax) {
					return default (DebugDataTipInfo);
				}
				if (semanticModel != null) {
					if (node is PropertyDeclarationSyntax) {
						var propertySymbol = semanticModel.GetDeclaredSymbol ((PropertyDeclarationSyntax)node);
						if (propertySymbol.IsStatic) {
							textOpt = propertySymbol.ContainingType.GetFullName () + "." + propertySymbol.Name;
						}
					} else if (node.GetAncestor<FieldDeclarationSyntax> () != null) {
						var fieldSymbol = semanticModel.GetDeclaredSymbol (node.GetAncestorOrThis<VariableDeclaratorSyntax> ());
						if (fieldSymbol.IsStatic) {
							textOpt = fieldSymbol.ContainingType.GetFullName () + "." + fieldSymbol.Name;
						}
					}
				}

				return new DebugDataTipInfo (node.Span, text: textOpt);
			}

			if (expression.IsAnyLiteralExpression ()) {
				// If the user hovers over a literal, give them a DataTip for the type of the
				// literal they're hovering over.
				// Partial semantics should always be sufficient because the (unconverted) type
				// of a literal can always easily be determined.
				var type = semanticModel?.GetTypeInfo (expression, cancellationToken).Type;
				return type == null
					? default (DebugDataTipInfo)
						: new DebugDataTipInfo (expression.Span, type.GetFullName ());
			}

			// Check if we are invoking method and if we do return null so we don't invoke it
			if (expression.Parent is InvocationExpressionSyntax || semanticModel.GetSymbolInfo (expression).Symbol is IMethodSymbol) 
				return default (DebugDataTipInfo);

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
