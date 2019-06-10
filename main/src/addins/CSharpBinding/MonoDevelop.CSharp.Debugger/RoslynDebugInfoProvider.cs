using System.ComponentModel.Composition;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using MonoDevelop.Debugger;
using MonoDevelop.Debugger.VSTextView.QuickInfo;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.CSharp.Debugger
{
	[Export]
	[Export (typeof (IDebugInfoProvider))]
	public class RoslynDebugInfoProvider : IDebugInfoProvider
	{
		public async Task<DataTipInfo> GetDebugInfoAsync (SnapshotPoint snapshotPoint, CancellationToken cancellationToken)
		{
			var analysisDocument = snapshotPoint.Snapshot.AsText ().GetOpenDocumentInCurrentContextWithChanges ();
			if (analysisDocument == null)
				return default (DataTipInfo);
			var debugInfoService = analysisDocument.GetLanguageService<Microsoft.CodeAnalysis.Editor.Implementation.Debugging.ILanguageDebugInfoService> ();
			if (debugInfoService == null)
				return default (DataTipInfo);

			var tipInfo = await debugInfoService.GetDataTipInfoAsync (analysisDocument, snapshotPoint.Position, cancellationToken).ConfigureAwait (false);
			var text = tipInfo.Text;
			if (text == null && !tipInfo.IsDefault)
				text = snapshotPoint.Snapshot.GetText (tipInfo.Span.Start, tipInfo.Span.Length);

			var semanticModel = await analysisDocument.GetSemanticModelAsync (cancellationToken).ConfigureAwait (false);
			var root = await semanticModel.SyntaxTree.GetRootAsync (cancellationToken).ConfigureAwait (false);
			var syntaxNode = root.FindNode (tipInfo.Span);
			DebugDataTipInfo debugDataTipInfo;
			if (syntaxNode == null)
				debugDataTipInfo = new DebugDataTipInfo (tipInfo.Span, text);
			else
				debugDataTipInfo = GetInfo (root, semanticModel, syntaxNode, text, cancellationToken);
			return new DataTipInfo (snapshotPoint.Snapshot.CreateTrackingSpan (debugDataTipInfo.Span.Start, debugDataTipInfo.Span.Length, SpanTrackingMode.EdgeInclusive), debugDataTipInfo.Text);
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
						var type = (INamedTypeSymbol)semanticModel.GetSymbolInfo (expression).Symbol;
						if (type != null)
							textOpt = type.GetFullName ();
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
