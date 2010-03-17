// 
// EqualityMembersGenerator.cs
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

using Gtk;
using System.Collections.Generic;
using ICSharpCode.NRefactory.Ast;
using MonoDevelop.Core;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Refactoring;

namespace MonoDevelop.CodeGeneration
{
	public class EqualityMembersGenerator : ICodeGenerator
	{
		public string Icon {
			get {
				return "md-newmethod";
			}
		}
		
		public string Text {
			get {
				return GettextCatalog.GetString ("Equality members");
			}
		}
		
		public string GenerateDescription {
			get {
				return GettextCatalog.GetString ("Select members to include in equality.");
			}
		}
		
		public bool IsValid (CodeGenerationOptions options)
		{
			return new CreateEquality (options).IsValid ();
		}
		
		public IGenerateAction InitalizeSelection (CodeGenerationOptions options, Gtk.TreeView treeView)
		{
			CreateEquality createEventMethod = new CreateEquality (options);
			createEventMethod.Initialize (treeView);
			return createEventMethod;
		}
		
		class CreateEquality : AbstractGenerateAction
		{
			public CreateEquality (CodeGenerationOptions options) : base (options)
			{
			}
			
			protected override IEnumerable<IBaseMember> GetValidMembers ()
			{
				if (Options.EnclosingType == null || Options.EnclosingMember != null)
					yield break;
				foreach (IField field in Options.EnclosingType.Fields) {
					if (field.IsSpecialName)
						continue;
					yield return field;
				}

				foreach (IProperty property in Options.EnclosingType.Properties) {
					if (property.IsSpecialName)
						continue;
					if (property.HasGet)
						yield return property;
				}
			}
			
			protected override IEnumerable<ICSharpCode.NRefactory.Ast.INode> GenerateCode (List<IBaseMember> includedMembers)
			{
				// Genereate Equals
				MethodDeclaration methodDeclaration = new MethodDeclaration ();
				methodDeclaration.Name = "Equals";

				methodDeclaration.TypeReference = DomReturnType.Bool.ConvertToTypeReference ();
				methodDeclaration.Modifier = ICSharpCode.NRefactory.Ast.Modifiers.Public | ICSharpCode.NRefactory.Ast.Modifiers.Override;
				methodDeclaration.Body = new BlockStatement ();
				methodDeclaration.Parameters.Add (new ParameterDeclarationExpression (DomReturnType.Object.ConvertToTypeReference (), "obj"));
				IdentifierExpression paramId = new IdentifierExpression ("obj");
				IfElseStatement ifStatement = new IfElseStatement (null);
				ifStatement.Condition = new BinaryOperatorExpression (paramId, BinaryOperatorType.Equality, new PrimitiveExpression (null));
				ifStatement.TrueStatement.Add (new ReturnStatement (new PrimitiveExpression (false)));
				methodDeclaration.Body.AddChild (ifStatement);

				ifStatement = new IfElseStatement (null);
				List<Expression> arguments = new List<Expression> ();
				arguments.Add (new ThisReferenceExpression ());
				arguments.Add (paramId);
				ifStatement.Condition = new InvocationExpression (new IdentifierExpression ("ReferenceEquals"), arguments);
				ifStatement.TrueStatement.Add (new ReturnStatement (new PrimitiveExpression (true)));
				methodDeclaration.Body.AddChild (ifStatement);

				ifStatement = new IfElseStatement (null);
				ifStatement.Condition = new BinaryOperatorExpression (new InvocationExpression (new MemberReferenceExpression (paramId, "GetType")), BinaryOperatorType.InEquality, new TypeOfExpression (new TypeReference (Options.EnclosingType.Name)));
				ifStatement.TrueStatement.Add (new ReturnStatement (new PrimitiveExpression (false)));
				methodDeclaration.Body.AddChild (ifStatement);

				LocalVariableDeclaration varDecl = new LocalVariableDeclaration (new DomReturnType (Options.EnclosingType).ConvertToTypeReference ());
				varDecl.Variables.Add (new VariableDeclaration ("other", new CastExpression (varDecl.TypeReference, paramId, CastType.Cast)));
				methodDeclaration.Body.AddChild (varDecl);

				IdentifierExpression otherId = new IdentifierExpression ("other");
				Expression binOp = null;
				foreach (IMember member in includedMembers) {
					Expression right = new BinaryOperatorExpression (new IdentifierExpression (member.Name), BinaryOperatorType.Equality, new MemberReferenceExpression (otherId, member.Name));
					if (binOp == null) {
						binOp = right;
					} else {
						binOp = new BinaryOperatorExpression (binOp, BinaryOperatorType.LogicalAnd, right);
					}
				}

				methodDeclaration.Body.AddChild (new ReturnStatement (binOp));
				yield return methodDeclaration;

				methodDeclaration = new MethodDeclaration ();
				methodDeclaration.Name = "GetHashCode";

				methodDeclaration.TypeReference = DomReturnType.Int32.ConvertToTypeReference ();
				methodDeclaration.Modifier = ICSharpCode.NRefactory.Ast.Modifiers.Public | ICSharpCode.NRefactory.Ast.Modifiers.Override;
				methodDeclaration.Body = new BlockStatement ();

				binOp = null;
				foreach (IMember member in includedMembers) {
					Expression right;
					right = new InvocationExpression (new MemberReferenceExpression (new IdentifierExpression (member.Name), "GetHashCode"));

					IType type = Options.Dom.SearchType (member, member.ReturnType);
					if (type != null && type.ClassType != MonoDevelop.Projects.Dom.ClassType.Struct&& type.ClassType != MonoDevelop.Projects.Dom.ClassType.Enum)
						right = new ParenthesizedExpression (new ConditionalExpression (new BinaryOperatorExpression (new IdentifierExpression (member.Name), BinaryOperatorType.InEquality, new PrimitiveExpression (null)), right, new PrimitiveExpression (0)));

					if (binOp == null) {
						binOp = right;
					} else {
						binOp = new BinaryOperatorExpression (binOp, BinaryOperatorType.ExclusiveOr, right);
					}
				}
				BlockStatement uncheckedBlock = new BlockStatement ();
				uncheckedBlock.AddChild (new ReturnStatement (binOp));

				methodDeclaration.Body.AddChild (new UncheckedStatement (uncheckedBlock));
				yield return methodDeclaration;
			}
		}
	}
}
