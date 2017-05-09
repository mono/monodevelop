//
// CreateConstructorGenerator.cs
//
// Author:
//       Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using Gtk;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.ExtractMethod;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MonoDevelop.Core;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.CodeGeneration
{
	class CreateConstructorGenerator : ICodeGenerator
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
				return GettextCatalog.GetString ("Constructor");
			}
		}

		public string GenerateDescription
		{
			get
			{
				return GettextCatalog.GetString ("Select members to be initialized by the constructor.");
			}
		}

		public bool IsValid (CodeGenerationOptions options)
		{
			var createConstructor = new CreateConstructor (options);
			return createConstructor.IsValid ();
		}

		public IGenerateAction InitalizeSelection (CodeGenerationOptions options, TreeView treeView)
		{
			var createConstructor = new CreateConstructor (options);
			createConstructor.Initialize (treeView);
			return createConstructor;
		}

		internal static TypeSyntax ConvertType (ITypeSymbol symbol)
		{
			// TODO: There needs to be a better way doing that.
			return SyntaxFactory.ParseTypeName (symbol.ToDisplayString (SymbolDisplayFormat.CSharpErrorMessageFormat));
		}

		class CreateConstructor : AbstractGenerateAction
		{
			public CreateConstructor (CodeGenerationOptions options) : base (options)
			{
			}

			protected override IEnumerable<object> GetValidMembers ()
			{
				if (Options.EnclosingType == null || Options.EnclosingMember != null)
					yield break;

				var bt = Options.EnclosingType.BaseType;

				if (bt != null) {
					var ctors = bt.GetMembers ().OfType<IMethodSymbol> ().Where (m => m.MethodKind == MethodKind.Constructor && !m.IsImplicitlyDeclared).ToList ();
					foreach (IMethodSymbol ctor in ctors) {
						if (ctor.Parameters.Length > 0 || ctors.Count > 1) {
							yield return ctor;
						}
					}
				}

				foreach (IFieldSymbol field in Options.EnclosingType.GetMembers ().OfType<IFieldSymbol> ()) {
					if (field.IsImplicitlyDeclared)
						continue;
					yield return field;
				}

				foreach (IPropertySymbol property in Options.EnclosingType.GetMembers ().OfType<IPropertySymbol> ()) {
					if (property.IsImplicitlyDeclared)
						continue;
					if (property.SetMethod == null) {
						if (property.GetMethod == null)
							continue;
						var r = property.GetMethod.DeclaringSyntaxReferences.FirstOrDefault ();
						if (r == null)
							continue;
						var node = r.SyntaxTree.GetRoot ().FindNode (r.Span) as AccessorDeclarationSyntax;
						if (node == null || node.GetBlockBody () != null)
							continue;
					}
					yield return property;
				}
			}

			static string CreateParameterName (ISymbol member)
			{
				if (char.IsUpper (member.Name[0]))
					return char.ToLower (member.Name[0]) + member.Name.Substring (1);
				return member.Name;
			}

			protected override IEnumerable<string> GenerateCode (List<object> includedMembers)
			{
				bool gotConstructorOverrides = false;
				foreach (IMethodSymbol m in includedMembers.OfType<IMethodSymbol> ().Where (m => m.MethodKind == MethodKind.Constructor)) {
					gotConstructorOverrides = true;
					var parameters = new List<ParameterSyntax> ();
					var initArgs = new List<ArgumentSyntax> ();
					var statements = new List<StatementSyntax> ();
					foreach (var par in m.Parameters) {
						parameters.Add (SyntaxFactory.Parameter (SyntaxFactory.Identifier (par.Name)).WithType (ConvertType (par.Type)));
						initArgs.Add (SyntaxFactory.Argument (SyntaxFactory.ParseExpression (par.Name)));
					}

					foreach (ISymbol member in includedMembers) {
						if (member.Kind == SymbolKind.Method)
							continue;
						var paramName = CreateParameterName (member);
						parameters.Add (SyntaxFactory.Parameter (SyntaxFactory.Identifier (paramName)).WithType (ConvertType (member.GetReturnType ())));

						statements.Add (
							SyntaxFactory.ExpressionStatement (
								SyntaxFactory.AssignmentExpression (
									SyntaxKind.SimpleAssignmentExpression,
									SyntaxFactory.MemberAccessExpression (
										SyntaxKind.SimpleMemberAccessExpression,
										SyntaxFactory.ThisExpression (),
										SyntaxFactory.IdentifierName (member.Name)
									),
									SyntaxFactory.IdentifierName (paramName)
								)
							)
						);
					}

					var node = SyntaxFactory.ConstructorDeclaration (
					SyntaxFactory.List<AttributeListSyntax> (),
					SyntaxFactory.TokenList (SyntaxFactory.Token (SyntaxKind.PublicKeyword)),
					SyntaxFactory.Identifier (Options.EnclosingType.Name),
					SyntaxFactory.ParameterList (SyntaxFactory.SeparatedList<ParameterSyntax> (parameters)),
						initArgs.Count > 0 ? SyntaxFactory.ConstructorInitializer (SyntaxKind.BaseConstructorInitializer, SyntaxFactory.ArgumentList (SyntaxFactory.SeparatedList<ArgumentSyntax> (initArgs))) : null,
					SyntaxFactory.Block (statements.ToArray ())
				);
					yield return Options.OutputNode (node).Result;
				}
				if (gotConstructorOverrides)
					yield break;

				var parameters2 = new List<ParameterSyntax> ();
				var statements2 = new List<StatementSyntax> ();
				foreach (ISymbol member in includedMembers) {
					var paramName = CreateParameterName (member);
					parameters2.Add (SyntaxFactory.Parameter (SyntaxFactory.Identifier (paramName)).WithType (ConvertType (member.GetReturnType ())));

					statements2.Add (
						SyntaxFactory.ExpressionStatement (
							SyntaxFactory.AssignmentExpression (
								SyntaxKind.SimpleAssignmentExpression,
								SyntaxFactory.MemberAccessExpression (
									SyntaxKind.SimpleMemberAccessExpression,
									SyntaxFactory.ThisExpression (),
									SyntaxFactory.IdentifierName (member.Name)
								),
								SyntaxFactory.IdentifierName (paramName)
							)
						)
					);
				}
				
				var node2 = SyntaxFactory.ConstructorDeclaration (
					SyntaxFactory.List<AttributeListSyntax> (),
					SyntaxFactory.TokenList (SyntaxFactory.Token (SyntaxKind.PublicKeyword)),
					SyntaxFactory.Identifier (Options.EnclosingType.Name),
					SyntaxFactory.ParameterList (SyntaxFactory.SeparatedList<ParameterSyntax> (parameters2)),
					null,
					SyntaxFactory.Block (statements2.ToArray ())
				);
				yield return Options.OutputNode (node2).Result;
			}
		}
	}
}
