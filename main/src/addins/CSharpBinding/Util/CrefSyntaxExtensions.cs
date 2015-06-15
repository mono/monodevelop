//
// CrefSyntaxExtensions.cs
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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Simplification;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Rename.ConflictEngine;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System;
using System.Reflection;

namespace ICSharpCode.NRefactory6.CSharp
{
	public static class CrefSyntaxExtensions
	{
		public static bool TryReduceOrSimplifyExplicitName(
			this CrefSyntax crefSyntax,
			SemanticModel semanticModel,
			out CrefSyntax replacementNode,
			out TextSpan issueSpan,
			OptionSet optionSet,
			CancellationToken cancellationToken)
		{
			replacementNode = null;
			issueSpan = default(TextSpan);

			// Currently Qualified Cref is the only CrefSyntax We are handling separately
			if (crefSyntax.Kind() != SyntaxKind.QualifiedCref)
			{
				return false;
			}

			var qualifiedCrefSyntax = (QualifiedCrefSyntax)crefSyntax;
			var memberCref = qualifiedCrefSyntax.Member;

			// Currently we are dealing with only the NameMemberCrefs
			if (optionSet.GetOption(SimplificationOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, LanguageNames.CSharp) &&
				(memberCref.Kind() == SyntaxKind.NameMemberCref))
			{
				var nameMemberCref = ((NameMemberCrefSyntax)memberCref).Name;
				var symbolInfo = semanticModel.GetSymbolInfo(nameMemberCref, cancellationToken);
				var symbol = symbolInfo.Symbol;

				if (symbol == null)
				{
					return false;
				}

				if (symbol is INamespaceOrTypeSymbol)
				{
					var namespaceOrTypeSymbol = (INamespaceOrTypeSymbol)symbol;

					// 1. Check for Predefined Types
					if (symbol is INamedTypeSymbol)
					{
						var namedSymbol = (INamedTypeSymbol)symbol;
						var keywordKind = ExpressionSyntaxExtensions.GetPredefinedKeywordKind(namedSymbol.SpecialType);

						if (keywordKind != SyntaxKind.None)
						{
							replacementNode = SyntaxFactory.TypeCref(
								SyntaxFactory.PredefinedType(
									SyntaxFactory.Token(crefSyntax.GetLeadingTrivia(), keywordKind, crefSyntax.GetTrailingTrivia())));
							replacementNode = crefSyntax.CopyAnnotationsTo(replacementNode);

							// we want to show the whole name expression as unnecessary
							issueSpan = crefSyntax.Span;

							return true;
						}
					}
				}
			}

			var oldSymbol = semanticModel.GetSymbolInfo(crefSyntax, cancellationToken).Symbol;
			if (oldSymbol != null)
			{
				var speculativeBindingOption = SpeculativeBindingOption.BindAsExpression;
				if (oldSymbol is INamespaceOrTypeSymbol)
				{
					speculativeBindingOption = SpeculativeBindingOption.BindAsTypeOrNamespace;
				}

				var newSymbol = semanticModel.GetSpeculativeSymbolInfo(crefSyntax.SpanStart, memberCref, speculativeBindingOption).Symbol;

				if (newSymbol == oldSymbol)
				{
					// Copy Trivia and Annotations
					memberCref = memberCref.WithLeadingTrivia(crefSyntax.GetLeadingTrivia());
					memberCref = crefSyntax.CopyAnnotationsTo(memberCref);
					issueSpan = qualifiedCrefSyntax.Container.Span;
					replacementNode = memberCref;
					return true;
				}
			}

			return false;
		}
	}
	
}
