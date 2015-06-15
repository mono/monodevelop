// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace ICSharpCode.NRefactory6.CSharp
{
	public static class CSharpSemanticFactsService
	{
		public static bool SupportsImplicitInterfaceImplementation
		{
			get
			{
				return true;
			}
		}

		public static bool ExposesAnonymousFunctionParameterNames
		{
			get
			{
				return false;
			}
		}

		public static bool IsExpressionContext(this SemanticModel semanticModel, int position, CancellationToken cancellationToken)
		{
			var csharpModel = (SemanticModel)semanticModel;
			return csharpModel.SyntaxTree.IsExpressionContext(
				position,
				csharpModel.SyntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken),
				attributes: true, cancellationToken: cancellationToken, semanticModelOpt: csharpModel);
		}

		public static bool IsStatementContext(this SemanticModel semanticModel, int position, CancellationToken cancellationToken)
		{
			var csharpModel = (SemanticModel)semanticModel;
			return csharpModel.SyntaxTree.IsStatementContext(
				position, csharpModel.SyntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken), cancellationToken);
		}

		public static bool IsTypeContext(this SemanticModel semanticModel, int position, CancellationToken cancellationToken)
		{
			var csharpModel = (SemanticModel)semanticModel;
			return csharpModel.SyntaxTree.IsTypeContext(position, cancellationToken, csharpModel);
		}

		public static bool IsNamespaceContext(this SemanticModel semanticModel, int position, CancellationToken cancellationToken)
		{
			var csharpModel = (SemanticModel)semanticModel;
			return csharpModel.SyntaxTree.IsNamespaceContext(position, cancellationToken, csharpModel);
		}

		public static bool IsTypeDeclarationContext(this SemanticModel semanticModel, int position, CancellationToken cancellationToken)
		{
			var csharpModel = (SemanticModel)semanticModel;
			return csharpModel.SyntaxTree.IsTypeDeclarationContext(
				position, csharpModel.SyntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken), cancellationToken);
		}

		public static bool IsMemberDeclarationContext(this SemanticModel semanticModel, int position, CancellationToken cancellationToken)
		{
			var csharpModel = (SemanticModel)semanticModel;
			return csharpModel.SyntaxTree.IsMemberDeclarationContext(
				position, csharpModel.SyntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken), cancellationToken);
		}

		public static bool IsPreProcessorDirectiveContext(this SemanticModel semanticModel, int position, CancellationToken cancellationToken)
		{
			var csharpModel = (SemanticModel)semanticModel;
			return csharpModel.SyntaxTree.IsPreProcessorDirectiveContext(
				position, csharpModel.SyntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken, includeDirectives: true), cancellationToken);
		}

		public static bool IsGlobalStatementContext(this SemanticModel semanticModel, int position, CancellationToken cancellationToken)
		{
			var csharpModel = (SemanticModel)semanticModel;
			return csharpModel.SyntaxTree.IsGlobalStatementContext(position, cancellationToken);
		}

		public static bool IsLabelContext(this SemanticModel semanticModel, int position, CancellationToken cancellationToken)
		{
			var csharpModel = (SemanticModel)semanticModel;
			return csharpModel.SyntaxTree.IsLabelContext(position, cancellationToken);
		}

		public static bool IsAttributeNameContext(this SemanticModel semanticModel, int position, CancellationToken cancellationToken)
		{
			var csharpModel = (SemanticModel)semanticModel;
			return csharpModel.SyntaxTree.IsAttributeNameContext(position, cancellationToken);
		}

		public static bool IsWrittenTo(this SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
		{
			return (node as ExpressionSyntax).IsWrittenTo();
		}

		public static bool IsOnlyWrittenTo(this SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
		{
			return (node as ExpressionSyntax).IsOnlyWrittenTo();
		}

		public static bool IsInOutContext(this SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
		{
			return (node as ExpressionSyntax).IsInOutContext();
		}

		public static bool IsInRefContext(this SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
		{
			return (node as ExpressionSyntax).IsInRefContext();
		}

		public static bool CanReplaceWithRValue(this SemanticModel semanticModel, SyntaxNode expression, CancellationToken cancellationToken)
		{
			return (expression as ExpressionSyntax).CanReplaceWithRValue(semanticModel, cancellationToken);
		}

		public static string GenerateNameForExpression(this SemanticModel semanticModel, SyntaxNode expression, bool capitalize = false)
		{
			return semanticModel.GenerateNameForExpression((ExpressionSyntax)expression, capitalize);
		}

		public static ISymbol GetDeclaredSymbol(this SemanticModel semanticModel, SyntaxToken token, CancellationToken cancellationToken)
		{
			var location = token.GetLocation();
			var q = from node in token.GetAncestors<SyntaxNode>()
				let symbol = semanticModel.GetDeclaredSymbol(node, cancellationToken)
					where symbol != null && symbol.Locations.Contains(location)
				select symbol;

			return q.FirstOrDefault();
		}

		public static bool LastEnumValueHasInitializer(INamedTypeSymbol namedTypeSymbol)
		{
			var enumDecl = namedTypeSymbol.DeclaringSyntaxReferences.Select(r => r.GetSyntax()).OfType<EnumDeclarationSyntax>().FirstOrDefault();
			if (enumDecl != null)
			{
				var lastMember = enumDecl.Members.LastOrDefault();
				if (lastMember != null)
				{
					return lastMember.EqualsValue != null;
				}
			}

			return false;
		}

		public static bool SupportsParameterizedProperties
		{
			get
			{
				return false;
			}
		}

		public static bool SupportsParameterizedEvents
		{
			get
			{
				return true;
			}
		}

		public static bool TryGetSpeculativeSemanticModel(this SemanticModel oldSemanticModel, SyntaxNode oldNode, SyntaxNode newNode, out SemanticModel speculativeModel)
		{
			var model = oldSemanticModel;

			// currently we only support method. field support will be added later.
			var oldMethod = oldNode as BaseMethodDeclarationSyntax;
			var newMethod = newNode as BaseMethodDeclarationSyntax;
			if (oldMethod == null || newMethod == null || oldMethod.Body == null)
			{
				speculativeModel = null;
				return false;
			}

			SemanticModel csharpModel;
			bool success = model.TryGetSpeculativeSemanticModelForMethodBody(oldMethod.Body.OpenBraceToken.Span.End, newMethod, out csharpModel);
			speculativeModel = csharpModel;
			return success;
		}

//		public static ImmutableHashSet<string> GetAliasNameSet(this SemanticModel model, CancellationToken cancellationToken)
//		{
//			var original = (SemanticModel)model.GetOriginalSemanticModel();
//			if (!original.SyntaxTree.HasCompilationUnitRoot)
//			{
//				return ImmutableHashSet.Create<string>();
//			}
//
//			var root = original.SyntaxTree.GetCompilationUnitRoot(cancellationToken);
//			var builder = ImmutableHashSet.CreateBuilder<string>(StringComparer.Ordinal);
//
//			AppendAliasNames(root.Usings, builder);
//			AppendAliasNames(root.Members.OfType<NamespaceDeclarationSyntax>(), builder, cancellationToken);
//
//			return builder.ToImmutable();
//		}
//
//		private static void AppendAliasNames(SyntaxList<UsingDirectiveSyntax> usings, ImmutableHashSet<string>.Builder builder)
//		{
//			foreach (var @using in usings)
//			{
//				if (@using.Alias == null || @using.Alias.Name == null)
//				{
//					continue;
//				}
//
//				@using.Alias.Name.Identifier.ValueText.AppendToAliasNameSet(builder);
//			}
//		}
//
//		private void AppendAliasNames(IEnumerable<NamespaceDeclarationSyntax> namespaces, ImmutableHashSet<string>.Builder builder, CancellationToken cancellationToken)
//		{
//			foreach (var @namespace in namespaces)
//			{
//				cancellationToken.ThrowIfCancellationRequested();
//
//				AppendAliasNames(@namespace.Usings, builder);
//				AppendAliasNames(@namespace.Members.OfType<NamespaceDeclarationSyntax>(), builder, cancellationToken);
//			}
//		}

//		public static ForEachSymbols GetForEachSymbols(this SemanticModel semanticModel, SyntaxNode forEachStatement)
//		{
//			var csforEachStatement = forEachStatement as ForEachStatementSyntax;
//			if (csforEachStatement != null)
//			{
//				var info = semanticModel.GetForEachStatementInfo(csforEachStatement);
//				return new ForEachSymbols(
//					info.GetEnumeratorMethod,
//					info.MoveNextMethod,
//					info.CurrentProperty,
//					info.DisposeMethod,
//					info.ElementType);
//			}
//			else
//			{
//				return default(ForEachSymbols);
//			}
//		}

		public static bool IsAssignableTo(ITypeSymbol fromSymbol, ITypeSymbol toSymbol, Compilation compilation)
		{
			return fromSymbol != null &&
				toSymbol != null &&
				((CSharpCompilation)compilation).ClassifyConversion(fromSymbol, toSymbol).IsImplicit;
		}

		public static bool IsNameOfContext(this SemanticModel semanticModel, int position, CancellationToken cancellationToken)
		{
			return semanticModel.SyntaxTree.IsNameOfContext(position, semanticModel, cancellationToken);
		}
	}
}
