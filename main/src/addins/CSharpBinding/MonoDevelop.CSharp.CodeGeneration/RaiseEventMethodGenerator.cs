// 
// RaiseEventMethodGenerator.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin <http://xamarin.com>
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
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.NRefactory6.CSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MonoDevelop.Core;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.CodeGeneration
{
	class RaiseEventMethodGenerator : ICodeGenerator
	{
		public string Icon {
			get {
				return "md-event";
			}
		}

		public string Text {
			get {
				return GettextCatalog.GetString ("Event OnXXX method");
			}
		}

		public string GenerateDescription {
			get {
				return GettextCatalog.GetString ("Select event to generate the method for.");
			}
		}

		public bool IsValid (CodeGenerationOptions options)
		{
			return new CreateEventMethod (options).IsValid ();
		}

		public IGenerateAction InitalizeSelection (CodeGenerationOptions options, Gtk.TreeView treeView)
		{
			var createEventMethod = new CreateEventMethod (options);
			createEventMethod.Initialize (treeView);
			return createEventMethod;
		}

		class CreateEventMethod : AbstractGenerateAction
		{
			const string handlerName = "handler";

			public CreateEventMethod (CodeGenerationOptions options) : base (options)
			{
			}

			static string GetEventMethodName (ISymbol member)
			{
				return "On" + member.Name;
			}

			protected override IEnumerable<object> GetValidMembers ()
			{
				if (Options.EnclosingType == null || Options.EnclosingMember != null)
					yield break;
				foreach (IEventSymbol e in Options.EnclosingType.GetMembers ().OfType<IEventSymbol> ()) {
					if (e.IsImplicitlyDeclared)
						continue;
					var invokeMethod = e.GetReturnType ().GetDelegateInvokeMethod ();
					if (invokeMethod == null)
						continue;
					if (Options.EnclosingType.GetMembers ().OfType<IMethodSymbol> ().Any (m => m.Name == GetEventMethodName (e)))
						continue;
					yield return e;
				}
			}

			protected override IEnumerable<string> GenerateCode (List<object> includedMembers)
			{
				foreach (IEventSymbol member in includedMembers) {
					var invokeMethod = member.GetReturnType ().GetDelegateInvokeMethod ();
					if (invokeMethod == null)
						continue;

					var node = SyntaxFactory.MethodDeclaration (
						SyntaxFactory.PredefinedType (SyntaxFactory.Token (SyntaxKind.VoidKeyword)),
						SyntaxFactory.Identifier (GetEventMethodName (member))
					);

					node = node.WithModifiers (SyntaxFactory.TokenList (SyntaxFactory.Token (SyntaxKind.ProtectedKeyword), SyntaxFactory.Token (SyntaxKind.VirtualKeyword)));
					node = node.WithParameterList (SyntaxFactory.ParameterList (SyntaxFactory.SeparatedList<ParameterSyntax> (new [] {
						SyntaxFactory.Parameter (SyntaxFactory.Identifier (invokeMethod.Parameters [1].Name)).WithType (SyntaxFactory.ParseTypeName (Options.CreateShortType (invokeMethod.Parameters [1].Type)))
					})));

					bool csharp6Style = true;
					;
					if (csharp6Style) {
						var expressionSyntax = SyntaxFactory.ParseExpression ("foo?.bar") as ConditionalAccessExpressionSyntax;
						Console.WriteLine (expressionSyntax.OperatorToken.Kind ());
						Console.WriteLine (expressionSyntax.Expression.GetType ());
						Console.WriteLine (expressionSyntax.WhenNotNull.GetType ());
						node = node.WithBody (SyntaxFactory.Block (
							SyntaxFactory.ExpressionStatement (
								SyntaxFactory.InvocationExpression (
									SyntaxFactory.ConditionalAccessExpression (
										SyntaxFactory.MemberAccessExpression (
											SyntaxKind.SimpleMemberAccessExpression,
											SyntaxFactory.ThisExpression (),
											SyntaxFactory.IdentifierName (member.Name)
										),
										SyntaxFactory.MemberBindingExpression (SyntaxFactory.IdentifierName ("Invoke"))
									),
									SyntaxFactory.ArgumentList (
										SyntaxFactory.SeparatedList<ArgumentSyntax> (new [] {
											SyntaxFactory.Argument (SyntaxFactory.ThisExpression ()),
											SyntaxFactory.Argument (SyntaxFactory.IdentifierName (invokeMethod.Parameters [1].Name))
										})
									)
								)
							)
						));
					} else {
						node = node.WithBody (SyntaxFactory.Block (
							SyntaxFactory.LocalDeclarationStatement (
								SyntaxFactory.VariableDeclaration (
									SyntaxFactory.ParseTypeName ("var"),
									SyntaxFactory.SeparatedList<VariableDeclaratorSyntax> (new [] {
										SyntaxFactory.VariableDeclarator (SyntaxFactory.Identifier (handlerName)).WithInitializer (
											SyntaxFactory.EqualsValueClause (
												SyntaxFactory.MemberAccessExpression (
													SyntaxKind.SimpleMemberAccessExpression,
													SyntaxFactory.ThisExpression (),
													SyntaxFactory.IdentifierName (member.Name)
												)
											)
										)
									})
								)
							),
							SyntaxFactory.IfStatement (
								SyntaxFactory.BinaryExpression (
									SyntaxKind.NotEqualsExpression,
									SyntaxFactory.IdentifierName (handlerName),
									SyntaxFactory.ParseExpression ("null")
								),
								SyntaxFactory.ExpressionStatement (
									SyntaxFactory.InvocationExpression (
										SyntaxFactory.IdentifierName (handlerName), 
										SyntaxFactory.ArgumentList (
											SyntaxFactory.SeparatedList<ArgumentSyntax> (new [] {
												SyntaxFactory.Argument (SyntaxFactory.ThisExpression ()),
												SyntaxFactory.Argument (SyntaxFactory.IdentifierName (invokeMethod.Parameters [1].Name))
											})
										)
									)
								)
							)
						));
					}
					yield return Options.OutputNode (node).Result;
				}
			}
		}
	}
}