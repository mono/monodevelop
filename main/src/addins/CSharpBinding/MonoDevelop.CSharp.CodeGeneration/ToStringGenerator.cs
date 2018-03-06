// 
// ToStringGenerator.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software" ), to deal
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
using System.Text;
using MonoDevelop.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Simplification;

namespace MonoDevelop.CodeGeneration
{
	class ToStringGenerator : ICodeGenerator
	{
		public string Icon
		{
			get
			{
				return "md-newmethod";
			}
		}

		public string Text
		{
			get
			{
				return GettextCatalog.GetString ("ToString() implementation");
			}
		}

		public string GenerateDescription
		{
			get
			{
				return GettextCatalog.GetString ("Select members to be outputted.");
			}
		}

		public bool IsValid (CodeGenerationOptions options)
		{
			return new CreateToString (options).IsValid ();
		}

		public IGenerateAction InitalizeSelection (CodeGenerationOptions options, Gtk.TreeView treeView)
		{
			CreateToString createToString = new CreateToString (options);
			createToString.Initialize (treeView);
			return createToString;
		}

		class CreateToString : AbstractGenerateAction
		{
			public CreateToString (CodeGenerationOptions options) : base (options)
			{
			}

			protected override IEnumerable<object> GetValidMembers ()
			{
				if (Options.EnclosingType == null || Options.EnclosingMember != null)
					yield break;

				foreach (var field in Options.EnclosingType.GetMembers ().OfType<IFieldSymbol> ()) {
					if (field.IsImplicitlyDeclared)
						continue;
					yield return field;
				}

				foreach (var property in Options.EnclosingType.GetMembers ().OfType<IPropertySymbol> ()) {
					if (property.IsImplicitlyDeclared)
						continue;
					if (property.GetMethod != null)
						yield return property;
				}
			}

			string GetFormatString (IEnumerable<object> includedMembers)
			{
				var format = StringBuilderCache.Allocate ();
				format.Append ("[");
				format.Append (Options.EnclosingType.Name);
				format.Append (": ");
				int i = 0;
				foreach (ISymbol member in includedMembers) {
					if (i > 0)
						format.Append (", ");
					format.Append (member.Name);
					format.Append ("={");
					format.Append (i++);
					format.Append ("}");
				}
				format.Append ("]");
				return StringBuilderCache.ReturnAndFree (format);
			}

			protected override IEnumerable<string> GenerateCode (List<object> includedMembers)
			{
				List<ArgumentSyntax> arguments = new List<ArgumentSyntax> ();
				arguments.Add (SyntaxFactory.Argument (SyntaxFactory.LiteralExpression (SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal (GetFormatString (includedMembers)))));
				foreach (ISymbol member in includedMembers) {
					arguments.Add (SyntaxFactory.Argument (SyntaxFactory.IdentifierName (member.Name)));
				}
				var node = SyntaxFactory.MethodDeclaration (
					SyntaxFactory.List<AttributeListSyntax>(),
					SyntaxFactory.TokenList (SyntaxFactory.Token (SyntaxKind.PublicKeyword), SyntaxFactory.Token (SyntaxKind.OverrideKeyword)),
					SyntaxFactory.ParseTypeName ("string"),
					null,
					SyntaxFactory.Identifier ("ToString"),
					null,
					SyntaxFactory.ParameterList (),
					SyntaxFactory.List<TypeParameterConstraintClauseSyntax>(),
					SyntaxFactory.Block (
						SyntaxFactory.ReturnStatement (
							SyntaxFactory.InvocationExpression (
								SyntaxFactory.MemberAccessExpression (
									SyntaxKind.SimpleMemberAccessExpression,
									SyntaxFactory.ParseExpression ("string"),
									SyntaxFactory.IdentifierName ("Format")
								),
								SyntaxFactory.ArgumentList (SyntaxFactory.SeparatedList<ArgumentSyntax> (arguments))
							)
						)
					),
					null);
				yield return Options.OutputNode (node).Result;
			}
		}
	}
}
