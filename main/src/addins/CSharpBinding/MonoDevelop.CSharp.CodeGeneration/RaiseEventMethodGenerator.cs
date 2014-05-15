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

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using Gtk;
using System.Collections.Generic;
using MonoDevelop.Refactoring;
using System.Text;
using System.Linq;
using Microsoft.CodeAnalysis;
using ICSharpCode.NRefactory6.CSharp;
using ICSharpCode.NRefactory.CSharp;

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
				foreach (ISymbol member in includedMembers) {
					var invokeMethod = member.GetReturnType ().GetDelegateInvokeMethod ();
					if (invokeMethod == null)
						continue;

					var methodDeclaration = new MethodDeclaration {
						Name = GetEventMethodName (member),
						ReturnType = new PrimitiveType ("void"),
						Modifiers = Modifiers.Protected | Modifiers.Virtual,
						Parameters = {
							new ParameterDeclaration (new SimpleType (Options.CreateShortType (invokeMethod.Parameters [1].Type)), invokeMethod.Parameters [1].Name)
						},
						Body = new BlockStatement () {
							new VariableDeclarationStatement (
								new SimpleType ("var"),//Options.CreateShortType (member.ReturnType), 
								handlerName, 
								new MemberReferenceExpression (new ThisReferenceExpression (), member.Name)
							),
							new IfElseStatement () {
								Condition = new BinaryOperatorExpression (new IdentifierExpression (handlerName), BinaryOperatorType.InEquality, new PrimitiveExpression (null)),
								TrueStatement = new ExpressionStatement (new InvocationExpression (new IdentifierExpression (handlerName)) {
									Arguments = {
										new ThisReferenceExpression (),
										new IdentifierExpression (invokeMethod.Parameters [1].Name)
									}
								})
							}
						}
					};

					yield return methodDeclaration.ToString (Options.FormattingOptions);
				}
			}
		}
	}
}

