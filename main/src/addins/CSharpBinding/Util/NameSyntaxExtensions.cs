// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.NRefactory6.CSharp
{
	public static class NameSyntaxExtensions
	{
		public static IList<NameSyntax> GetNameParts(this NameSyntax nameSyntax)
		{
			return new NameSyntaxIterator(nameSyntax).ToList();
		}

		public static NameSyntax GetLastDottedName(this NameSyntax nameSyntax)
		{
			var parts = nameSyntax.GetNameParts();
			return parts[parts.Count - 1];
		}

		public static SyntaxToken GetNameToken(this NameSyntax nameSyntax)
		{
			while (true)
			{
				if (nameSyntax.Kind() == SyntaxKind.IdentifierName)
				{
					return ((IdentifierNameSyntax)nameSyntax).Identifier;
				}
				else if (nameSyntax.Kind() == SyntaxKind.QualifiedName)
				{
					nameSyntax = ((QualifiedNameSyntax)nameSyntax).Right;
				}
				else if (nameSyntax.Kind() == SyntaxKind.GenericName)
				{
					return ((GenericNameSyntax)nameSyntax).Identifier;
				}
				else if (nameSyntax.Kind() == SyntaxKind.AliasQualifiedName)
				{
					nameSyntax = ((AliasQualifiedNameSyntax)nameSyntax).Name;
				}
				else
				{
					throw new NotSupportedException();
				}
			}
		}

		public static bool CanBeReplacedWithAnyName(this NameSyntax nameSyntax)
		{
			if (nameSyntax.IsParentKind(SyntaxKind.AliasQualifiedName) ||
				nameSyntax.IsParentKind(SyntaxKind.NameColon) ||
				nameSyntax.IsParentKind(SyntaxKind.NameEquals) ||
				nameSyntax.IsParentKind(SyntaxKind.TypeParameterConstraintClause))
			{
				return false;
			}

			if (nameSyntax.CheckParent<QualifiedNameSyntax>(q => q.Right == nameSyntax) ||
				nameSyntax.CheckParent<MemberAccessExpressionSyntax>(m => m.Name == nameSyntax))
			{
				return false;
			}

			// TODO(cyrusn): Add more cases as the language changes.
			return true;
		}
	}

	internal class NameSyntaxIterator : IEnumerable<NameSyntax>
	{
		private readonly NameSyntax _name;

		public NameSyntaxIterator(NameSyntax name)
		{
			if (name == null)
			{
				throw new ArgumentNullException(@"name");
			}

			_name = name;
		}

		public IEnumerator<NameSyntax> GetEnumerator()
		{
			var nodes = new LinkedList<NameSyntax>();

			var currentNode = _name;
			while (true)
			{
				if (currentNode.Kind() == SyntaxKind.QualifiedName)
				{
					var qualifiedName = currentNode as QualifiedNameSyntax;
					nodes.AddFirst(qualifiedName.Right);
					currentNode = qualifiedName.Left;
				}
				else
				{
					nodes.AddFirst(currentNode);
					break;
				}
			}

			return nodes.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
