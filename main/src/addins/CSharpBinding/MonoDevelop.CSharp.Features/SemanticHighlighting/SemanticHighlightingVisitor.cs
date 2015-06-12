// Copyright (c) 2010-2013 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.NRefactory6.CSharp.Analysis
{
	/// <summary>
	/// C# Semantic highlighter.
	/// </summary>
	public abstract class SemanticHighlightingVisitor<TColor> : CSharpSyntaxWalker
	{
		protected CancellationToken cancellationToken = default(CancellationToken);

		protected TColor defaultTextColor;
		protected TColor referenceTypeColor;
		protected TColor valueTypeColor;
		protected TColor interfaceTypeColor;
		protected TColor enumerationTypeColor;
		protected TColor typeParameterTypeColor;
		protected TColor delegateTypeColor;

		protected TColor methodCallColor;
		protected TColor methodDeclarationColor;

		protected TColor eventDeclarationColor;
		protected TColor eventAccessColor;

		protected TColor propertyDeclarationColor;
		protected TColor propertyAccessColor;

		protected TColor fieldDeclarationColor;
		protected TColor fieldAccessColor;

		protected TColor variableDeclarationColor;
		protected TColor variableAccessColor;

		protected TColor parameterDeclarationColor;
		protected TColor parameterAccessColor;

		protected TColor valueKeywordColor;
		protected TColor externAliasKeywordColor;
		protected TColor varKeywordTypeColor;
		protected TColor nameofKeywordColor;
		protected TColor whenKeywordColor;

		/// <summary>
		/// Used for 'in' modifiers on type parameters.
		/// </summary>
		/// <remarks>
		/// 'in' may have a different color when used with 'foreach'.
		/// 'out' is not colored by semantic highlighting, as syntax highlighting can already detect it as a parameter modifier.
		/// </remarks>
		protected TColor parameterModifierColor;

		/// <summary>
		/// Used for inactive code (excluded by preprocessor or ConditionalAttribute)
		/// </summary>
		protected TColor inactiveCodeColor;

		protected TColor stringFormatItemColor;

		protected TextSpan region;

		protected SemanticModel semanticModel;
		// bool isInAccessorContainingValueParameter;

		protected abstract void Colorize (TextSpan span, TColor color);

		protected SemanticHighlightingVisitor (SemanticModel semanticModel)
		{
			this.semanticModel = semanticModel;
		}

		#region Colorize helper methods
		protected void Colorize (SyntaxNode node, TColor color)
		{
			if (node == null)
				return;
			Colorize (node.Span, color);
		}

		protected void Colorize (SyntaxToken node, TColor color)
		{
			Colorize (node.Span, color);
		}

		#endregion
		public override void VisitRefTypeExpression (Microsoft.CodeAnalysis.CSharp.Syntax.RefTypeExpressionSyntax node)
		{
			base.VisitRefTypeExpression (node);
			Colorize (node.Expression, referenceTypeColor);
		}

		public override void VisitCompilationUnit (CompilationUnitSyntax node)
		{
			var startNode = node.DescendantNodesAndSelf (n => region.Start <= n.SpanStart).FirstOrDefault ();
			if (startNode == node || startNode == null) {
				base.VisitCompilationUnit (node);
			} else {
				this.Visit (startNode);
			}
		}

		public override void Visit (SyntaxNode node)
		{
			if (node.Span.End < region.Start)
				return;
			if (node.Span.Start > region.End)
				return;
			base.Visit(node);
		}

		void HighlightStringFormatItems(LiteralExpressionSyntax expr)
		{
			if (!expr.Token.IsKind(SyntaxKind.StringLiteralToken))
				return;
			var text = expr.Token.Text;
			int start = -1;
			for (int i = 0; i < text.Length; i++) {
				char ch = text [i];

				if (NewLine.GetDelimiterType(ch, i + 1 < text.Length ? text [i + 1] : '\0') != UnicodeNewline.Unknown) {
					continue;
				}

				if (ch == '{' && start < 0) {
					char next = i + 1 < text.Length ? text [i + 1] : '\0';
					if (next == '{') {
						i++;
						continue;
					}
					start = i;
				}
				
				if (ch == '}' && start >= 0) {
					Colorize(new TextSpan (expr.SpanStart + start, i - start + 1), stringFormatItemColor);
					start = -1;
				}
			}
		}

		public override void VisitInvocationExpression(InvocationExpressionSyntax node)
		{
			var symbolInfo = semanticModel.GetSymbolInfo(node.Expression, cancellationToken);
			
			if (IsInactiveConditional (symbolInfo.Symbol) || IsEmptyPartialMethod(symbolInfo.Symbol, cancellationToken)) {
				// mark the whole invocation statement as inactive code
				Colorize(node.Span, inactiveCodeColor);
				return;
			}
			if (node.Expression.IsKind (SyntaxKind.IdentifierName) && symbolInfo.Symbol == null) {
				var id = (IdentifierNameSyntax)node.Expression;
				if (id.Identifier.ValueText == "nameof") {
					Colorize(id.Span, nameofKeywordColor);
				}
			}

			ExpressionSyntax fmtArgumets;
			IList<ExpressionSyntax> args;
			if (node.ArgumentList.Arguments.Count > 1 && FormatStringHelper.TryGetFormattingParameters(semanticModel, node, out fmtArgumets, out args, null, cancellationToken)) {
				var expr = node.ArgumentList.Arguments.First(); 
				if (expr != null) {
					var literalExpressionSyntax = expr.Expression as LiteralExpressionSyntax;
					if (literalExpressionSyntax != null)
						HighlightStringFormatItems(literalExpressionSyntax);
				}
			}

			base.VisitInvocationExpression(node);
		}
		
		bool IsInactiveConditional(ISymbol member)
		{
			if (member == null || member.Kind != SymbolKind.Method)
				return false;
			var method = member as IMethodSymbol;
			if (method.ReturnType.SpecialType != SpecialType.System_Void)
				return false;
			
			var om = method.OverriddenMethod;
			while (om != null) {
				if (IsInactiveConditional (om.GetAttributes()))
					return true;
				om = om.OverriddenMethod;
			}
			
			return IsInactiveConditional(member.GetAttributes());
		}

		bool IsInactiveConditional(System.Collections.Immutable.ImmutableArray<AttributeData> attributes)
		{
			foreach (var attr in attributes) {
				if (attr.AttributeClass.Name == "ConditionalAttribute" && attr.AttributeClass.ContainingNamespace.ToString() == "System.Diagnostics" && attr.ConstructorArguments.Length == 1) {
					string symbol = attr.ConstructorArguments[0].Value as string;
					if (symbol != null) {
						var options = (CSharpParseOptions)semanticModel.SyntaxTree.Options;
						if (!options.PreprocessorSymbolNames.Contains(symbol))
							return true;
					}
				}
			}
			return false;
		}

		static bool IsEmptyPartialMethod(ISymbol member, CancellationToken cancellationToken = default(CancellationToken))
		{
			var method = member as IMethodSymbol;
			if (method == null || method.IsDefinedInMetadata ())
				return false;
			foreach (var r in method.DeclaringSyntaxReferences) {
				var node = r.GetSyntax (cancellationToken) as MethodDeclarationSyntax;
				if (node == null)
					continue;
				if (node.Body != null || !node.Modifiers.Any(m => m.IsKind (SyntaxKind.PartialKeyword)))
					return false;
			}

			return true;
		}

		public override void VisitExternAliasDirective(ExternAliasDirectiveSyntax node)
		{
			base.VisitExternAliasDirective(node);
			Colorize (node.AliasKeyword.Span, externAliasKeywordColor);
		}
		
		public override void VisitGenericName(GenericNameSyntax node)
		{
			base.VisitGenericName(node);
			var info = semanticModel.GetSymbolInfo(node, cancellationToken);
			TColor color;
			if (TryGetSymbolColor(info, out color)) {
				Colorize(node.Identifier.Span, color);
			}
		}
		
		public override void VisitStructDeclaration(Microsoft.CodeAnalysis.CSharp.Syntax.StructDeclarationSyntax node)
		{
			base.VisitStructDeclaration(node);
			Colorize(node.Identifier, valueTypeColor);
		}
		
		public override void VisitInterfaceDeclaration(Microsoft.CodeAnalysis.CSharp.Syntax.InterfaceDeclarationSyntax node)
		{
			base.VisitInterfaceDeclaration(node);
			Colorize(node.Identifier, interfaceTypeColor);
		}
		
		public override void VisitClassDeclaration(Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax node)
		{
			base.VisitClassDeclaration(node);
			Colorize(node.Identifier, referenceTypeColor);
		}
		
		public override void VisitEnumDeclaration(Microsoft.CodeAnalysis.CSharp.Syntax.EnumDeclarationSyntax node)
		{
			base.VisitEnumDeclaration(node);
			Colorize(node.Identifier, enumerationTypeColor);
		}
		
		public override void VisitDelegateDeclaration(Microsoft.CodeAnalysis.CSharp.Syntax.DelegateDeclarationSyntax node)
		{
			base.VisitDelegateDeclaration(node);
			Colorize(node.Identifier, delegateTypeColor);
		}
		
		public override void VisitTypeParameter(Microsoft.CodeAnalysis.CSharp.Syntax.TypeParameterSyntax node)
		{
			base.VisitTypeParameter(node);
			Colorize(node.Identifier, typeParameterTypeColor);
		}
		
		public override void VisitEventDeclaration(Microsoft.CodeAnalysis.CSharp.Syntax.EventDeclarationSyntax node)
		{
			base.VisitEventDeclaration(node);
			Colorize(node.Identifier, eventDeclarationColor);
		}
		
		public override void VisitMethodDeclaration(Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax node)
		{
			base.VisitMethodDeclaration(node);
			Colorize(node.Identifier, methodDeclarationColor);
		}
		
		public override void VisitPropertyDeclaration(Microsoft.CodeAnalysis.CSharp.Syntax.PropertyDeclarationSyntax node)
		{
			base.VisitPropertyDeclaration(node);
			Colorize(node.Identifier, propertyDeclarationColor);
		}

		public override void VisitParameter(Microsoft.CodeAnalysis.CSharp.Syntax.ParameterSyntax node)
		{
			base.VisitParameter(node);
			Colorize(node.Identifier, parameterDeclarationColor);
		}

		public override void VisitIdentifierName(Microsoft.CodeAnalysis.CSharp.Syntax.IdentifierNameSyntax node)
		{
			base.VisitIdentifierName(node);
			if (node.IsVar) {
				var symbolInfo = semanticModel.GetSymbolInfo(node, cancellationToken);
				if (node.Parent is ForEachStatementSyntax) {
					var sym = semanticModel.GetDeclaredSymbol(node.Parent, cancellationToken);
					if (sym != null) {
						Colorize(node.Span, varKeywordTypeColor);
						return;
					}
				}
				var vds = node.Parent as VariableDeclarationSyntax;
				if (vds != null && vds.Variables.Count == 1) {
					// var sym = vds.Variables[0].Initializer != null ? vds.Variables[0].Initializer.Value as LiteralExpressionSyntax : null;
					if (symbolInfo.Symbol == null || symbolInfo.Symbol.Name != "var") {
						Colorize(node.Span, varKeywordTypeColor);
						return;
					}
				}
			}
			
			switch (node.Identifier.Text) {
				case "add":
				case "async":
				case "await":
				case "get":
				case "partial":
				case "remove":
				case "set":
				case "where":
				case "yield":
				case "from":
				case "select":
				case "group":
				case "into":
				case "orderby":
				case "join":
				case "let":
				case "on":
				case "equals":
				case "by":
				case "ascending":
				case "descending":
				case "dynamic":
					// Reset color of contextual keyword to default if it's used as an identifier.
					// Note that this method does not get called when 'var' or 'dynamic' is used as a type,
					// because types get highlighted with valueTypeColor/referenceTypeColor instead.
					Colorize(node.Span, defaultTextColor);
					break;
				case "global":
//					// Reset color of 'global' keyword to default unless its used as part of 'global::'.
//					MemberType parentMemberType = identifier.Parent as MemberType;
//					if (parentMemberType == null || !parentMemberType.IsDoubleColon)
						Colorize(node.Span, defaultTextColor);
					break;
			}
			// "value" is handled in VisitIdentifierExpression()
			// "alias" is handled in VisitExternAliasDeclaration()

			TColor color;
			if (TryGetSymbolColor (semanticModel.GetSymbolInfo (node, cancellationToken), out color)) {
				if (node.Parent is AttributeSyntax || node.Parent is QualifiedNameSyntax && node.Parent.Parent is AttributeSyntax)
					color = referenceTypeColor;
				Colorize (node.Span, color);
			}
		}
		
		bool TryGetSymbolColor(SymbolInfo info, out TColor color)
		{
			var symbol = info.Symbol;

			if (symbol == null) {
				color = default(TColor);
			return false;
			}
			
			switch (symbol.Kind) {
				case SymbolKind.Field:
					color = fieldAccessColor;
					return true;
				case SymbolKind.Event:
					color = eventAccessColor;
					return true;
				case SymbolKind.Parameter:
					var param = (IParameterSymbol)symbol;
					var method = param.ContainingSymbol as IMethodSymbol;
					if (param.Name == "value" && method != null && (
						method.MethodKind == MethodKind.EventAdd || 
						method.MethodKind == MethodKind.EventRaise ||
						method.MethodKind == MethodKind.EventRemove ||
						method.MethodKind == MethodKind.PropertySet)) {
						color = valueKeywordColor;
					} else {
						color = parameterAccessColor;
					}
					return true;
				case SymbolKind.RangeVariable:
					color = variableAccessColor;
					return true;
				case SymbolKind.Method:
					color = methodCallColor;
					return true;
				case SymbolKind.Property:
					color = propertyAccessColor;
					return true;
				case SymbolKind.TypeParameter:
					color = typeParameterTypeColor;
					return true;
				case SymbolKind.Local:
					color = variableAccessColor;
					return true;
				case SymbolKind.NamedType:
					var type = (INamedTypeSymbol)symbol;
					switch (type.TypeKind) {
						case TypeKind.Class:
							color = referenceTypeColor;
							break;
						case TypeKind.Delegate:
							color = delegateTypeColor;
							break;
						case TypeKind.Enum:
							color = enumerationTypeColor;
							break;
						case TypeKind.Error:
							color = default(TColor);
							return false;
						case TypeKind.Interface:
							color = interfaceTypeColor;
							break;
						case TypeKind.Struct:
							color = valueTypeColor;
							break;
						case TypeKind.TypeParameter:
							color = typeParameterTypeColor;
							break;
						default:
							color = referenceTypeColor;
							break;
					}
					return true;
			}
			color = default(TColor);
			return false;
		}
		
		public override void VisitVariableDeclaration(Microsoft.CodeAnalysis.CSharp.Syntax.VariableDeclarationSyntax node)
		{
			base.VisitVariableDeclaration(node);
			TColor color;
			if (node.Parent.IsKind(SyntaxKind.EventFieldDeclaration))
				color = eventDeclarationColor;
			else if (node.Parent.IsKind(SyntaxKind.FieldDeclaration))
				color = fieldDeclarationColor;
			else
				color = variableDeclarationColor;

			foreach (var declarations in node.Variables) {
				// var info = semanticModel.GetTypeInfo(declarations, cancellationToken); 
				Colorize(declarations.Identifier, color);
			}
		}
		
//		public override void VisitComment(Comment comment)
//		{
//			if (comment.CommentType == CommentType.InactiveCode) {
//				Colorize(comment, inactiveCodeColor);
//			}
//		}
//
//		public override void VisitPreProcessorDirective(PreProcessorDirective preProcessorDirective)
//		{
//		}
		
//		public override void VisitAttribute(Microsoft.CodeAnalysis.CSharp.Syntax.AttributeSyntax node)
//		{
//			var symbol = semanticModel.GetDeclaredSymbol(node) as INamedTypeSymbol;
//			if (symbol != null && IsInactiveConditional(symbol)) {
//				Colorize(attribute, inactiveCodeColor);
//			} else {
//				base.VisitAttribute(node);
//			}
//		}
		
		public override void VisitInitializerExpression(Microsoft.CodeAnalysis.CSharp.Syntax.InitializerExpressionSyntax node)
		{
			base.VisitInitializerExpression(node);
			
			foreach (var a in node.Expressions) {
				// TODO
//				var namedElement = a as NamedExpression;
//				if (namedElement != null) {
//					var result = resolver.Resolve (namedElement, cancellationToken);
//					if (result.IsError)
//						Colorize (namedElement.NameToken, syntaxErrorColor);
//					namedElement.Expression.AcceptVisitor (this);
//				} 
			}
		}

		int blockDepth;
		
		public override void VisitBlock(Microsoft.CodeAnalysis.CSharp.Syntax.BlockSyntax node)
		{
			blockDepth++;
			cancellationToken.ThrowIfCancellationRequested ();
			base.VisitBlock(node);
			blockDepth--;
		}

		public override void VisitCatchFilterClause (CatchFilterClauseSyntax node)
		{
			if (!node.WhenKeyword.IsMissing) {
				Colorize(node.WhenKeyword, whenKeywordColor);
			}
			base.VisitCatchFilterClause (node);
		}
	}
}
