// 
// EventMethodGenerator.cs
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

using System;
using System.Linq;

using MonoDevelop.Components;
using Gtk;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects.Dom;
using System.Collections.Generic;
using ICSharpCode.NRefactory.Ast;
using System.Text;
using MonoDevelop.Core;
using MonoDevelop.Refactoring;

namespace MonoDevelop.CodeGeneration
{
	public class EventMethodGenerator : ICodeGenerator
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
			CreateEventMethod createEventMethod = new CreateEventMethod (options);
			createEventMethod.Initialize (treeView);
			return createEventMethod;
		}
		
		class CreateEventMethod : AbstractGenerateAction
		{
			public CreateEventMethod (CodeGenerationOptions options) : base (options)
			{
			}
			
			protected override IEnumerable<IMember> GetValidMembers ()
			{
				if (Options.EnclosingType == null || Options.EnclosingMember != null)
					yield break;
				foreach (IEvent e in Options.EnclosingType.Events) {
					if (e.IsSpecialName)
						continue;
					IType type = Options.Dom.SearchType (new SearchTypeRequest (Options.Document.ParsedDocument.CompilationUnit, e.ReturnType, Options.EnclosingType));
					if (type == null)
						continue;
					IMethod invokeMethod = type.Methods.FirstOrDefault ();
					if (invokeMethod == null)
						continue;
					yield return e;
				}
			}
			
			protected override IEnumerable<INode> GenerateCode (List<IMember> includedMembers)
			{
				foreach (IMember member in includedMembers) {
					MethodDeclaration methodDeclaration = new MethodDeclaration ();
					methodDeclaration.Name = "On" + member.Name;
					methodDeclaration.TypeReference = DomReturnType.Void.ConvertToTypeReference ();
					methodDeclaration.Modifier = ICSharpCode.NRefactory.Ast.Modifiers.Protected | ICSharpCode.NRefactory.Ast.Modifiers.Virtual;
					methodDeclaration.Body = new BlockStatement ();

					IType type = Options.Dom.SearchType (new SearchTypeRequest (Options.Document.ParsedDocument.CompilationUnit, member.ReturnType, Options.EnclosingType));
					IMethod invokeMethod = type.Methods.First ();

					methodDeclaration.Parameters.Add (new ParameterDeclarationExpression (invokeMethod.Parameters[1].ReturnType.ConvertToTypeReference (), invokeMethod.Parameters[1].Name));
					IfElseStatement ifStatement = new IfElseStatement (null);
					ifStatement.Condition = new BinaryOperatorExpression (new IdentifierExpression (member.Name), BinaryOperatorType.InEquality, new PrimitiveExpression (null));
					List<Expression> arguments = new List<Expression> ();
					arguments.Add (new ThisReferenceExpression ());
					arguments.Add (new IdentifierExpression (invokeMethod.Parameters[1].Name));
					ifStatement.TrueStatement.Add (new ExpressionStatement (new InvocationExpression (new IdentifierExpression (member.Name), arguments)));
					methodDeclaration.Body.AddChild (ifStatement);
					yield return methodDeclaration;
				}
			}
		}
	}
}
