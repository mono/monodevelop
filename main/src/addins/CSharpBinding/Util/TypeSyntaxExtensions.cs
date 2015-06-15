//
// TypeSyntaxExtensions.cs
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
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Threading;
using System.Linq;

namespace ICSharpCode.NRefactory6.CSharp
{
	public static class TypeSyntaxExtensions
	{
		public static bool IsPartial(this TypeSyntax typeSyntax)
		{
			return typeSyntax is IdentifierNameSyntax &&
				((IdentifierNameSyntax)typeSyntax).Identifier.IsKind(SyntaxKind.PartialKeyword);
		}

		public static bool IsPotentialTypeName(this TypeSyntax typeSyntax, SemanticModel semanticModelOpt, CancellationToken cancellationToken)
		{
			if (typeSyntax == null)
			{
				return false;
			}

			if (typeSyntax is PredefinedTypeSyntax ||
				typeSyntax is ArrayTypeSyntax ||
				typeSyntax is GenericNameSyntax ||
				typeSyntax is PointerTypeSyntax ||
				typeSyntax is NullableTypeSyntax)
			{
				return true;
			}

			if (semanticModelOpt == null)
			{
				return false;
			}

			var nameSyntax = typeSyntax as NameSyntax;
			if (nameSyntax == null)
			{
				return false;
			}

			var nameToken = nameSyntax.GetNameToken();

			var symbols = semanticModelOpt.LookupName(nameToken, namespacesAndTypesOnly: true, cancellationToken: cancellationToken);
			var firstSymbol = symbols.FirstOrDefault();

			var typeSymbol = firstSymbol != null && firstSymbol.Kind == SymbolKind.Alias
				? (firstSymbol as IAliasSymbol).Target
				: firstSymbol as ITypeSymbol;

			return typeSymbol != null
				&& !typeSymbol.IsErrorType();
		}

		/// <summary>
		/// Determines whether the specified TypeSyntax is actually 'var'.
		/// </summary>
		public static bool IsTypeInferred(this TypeSyntax typeSyntax, SemanticModel semanticModel)
		{
			if (!typeSyntax.IsVar)
			{
				return false;
			}

			if (semanticModel.GetAliasInfo(typeSyntax) != null)
			{
				return false;
			}

			var type = semanticModel.GetTypeInfo(typeSyntax).Type;
			if (type == null)
			{
				return false;
			}

			if (type.Name == "var")
			{
				return false;
			}

			return true;
		}
	}

}

