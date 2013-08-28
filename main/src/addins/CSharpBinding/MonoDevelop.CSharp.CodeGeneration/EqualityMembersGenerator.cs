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
using ICSharpCode.NRefactory.CSharp;
using MonoDevelop.Core;
using MonoDevelop.Refactoring;
using ICSharpCode.NRefactory.TypeSystem;

namespace MonoDevelop.CodeGeneration
{
	class EqualityMembersGenerator : ICodeGenerator
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
			
			protected override IEnumerable<object> GetValidMembers ()
			{
				if (Options.EnclosingType == null || Options.EnclosingMember != null)
					yield break;
				foreach (IField field in Options.EnclosingType.Fields) {
					if (field.IsSynthetic)
						continue;
					yield return field;
				}

				foreach (IProperty property in Options.EnclosingType.Properties) {
					if (property.IsSynthetic)
						continue;
					if (property.CanGet)
						yield return property;
				}
			}
			
			protected override IEnumerable<string> GenerateCode (List<object> includedMembers)
			{
				// Genereate Equals
				var methodDeclaration = new MethodDeclaration ();
				methodDeclaration.Name = "Equals";

				methodDeclaration.ReturnType = new PrimitiveType ("bool");
				methodDeclaration.Modifiers = ICSharpCode.NRefactory.CSharp.Modifiers.Public | ICSharpCode.NRefactory.CSharp.Modifiers.Override;
				methodDeclaration.Body = new BlockStatement ();
				methodDeclaration.Parameters.Add (new ParameterDeclaration (new PrimitiveType ("object"), "obj"));
				var paramId = new IdentifierExpression ("obj");
				IfElseStatement ifStatement = new IfElseStatement ();
				ifStatement.Condition = new BinaryOperatorExpression (paramId, BinaryOperatorType.Equality, new PrimitiveExpression (null));
				ifStatement.TrueStatement = new ReturnStatement (new PrimitiveExpression (false));
				methodDeclaration.Body.Statements.Add (ifStatement);

				ifStatement = new IfElseStatement ();
				var arguments = new List<Expression> ();
				arguments.Add (new ThisReferenceExpression ());
				arguments.Add (paramId.Clone ());
				ifStatement.Condition = new InvocationExpression (new IdentifierExpression ("ReferenceEquals"), arguments);
				ifStatement.TrueStatement = new ReturnStatement (new PrimitiveExpression (true));
				methodDeclaration.Body.Statements.Add (ifStatement);

				ifStatement = new IfElseStatement ();
				ifStatement.Condition = new BinaryOperatorExpression (new InvocationExpression (new MemberReferenceExpression (paramId.Clone (), "GetType")), BinaryOperatorType.InEquality, new TypeOfExpression (new SimpleType (Options.EnclosingType.Name)));
				ifStatement.TrueStatement = new ReturnStatement (new PrimitiveExpression (false));
				methodDeclaration.Body.Statements.Add (ifStatement);

				var varType = new SimpleType (Options.EnclosingType.Name);
				var varDecl = new VariableDeclarationStatement (varType, "other", new CastExpression (varType.Clone (), paramId.Clone ()));
				methodDeclaration.Body.Statements.Add (varDecl);
				
				var otherId = new IdentifierExpression ("other");
				Expression binOp = null;
				foreach (IMember member in includedMembers) {
					Expression right = new BinaryOperatorExpression (new IdentifierExpression (member.Name), BinaryOperatorType.Equality, new MemberReferenceExpression (otherId.Clone (), member.Name));
					if (binOp == null) {
						binOp = right;
					} else {
						binOp = new BinaryOperatorExpression (binOp, BinaryOperatorType.ConditionalAnd, right);
					}
				}

				methodDeclaration.Body.Statements.Add (new ReturnStatement (binOp));
				yield return methodDeclaration.ToString (Options.FormattingOptions);

				methodDeclaration = new MethodDeclaration ();
				methodDeclaration.Name = "GetHashCode";

				methodDeclaration.ReturnType = new PrimitiveType ("int");
				methodDeclaration.Modifiers = ICSharpCode.NRefactory.CSharp.Modifiers.Public | ICSharpCode.NRefactory.CSharp.Modifiers.Override;
				methodDeclaration.Body = new BlockStatement ();

				binOp = null;
				foreach (IMember member in includedMembers) {
					Expression right;
					right = new InvocationExpression (new MemberReferenceExpression (new IdentifierExpression (member.Name), "GetHashCode"));

					IType type = member.ReturnType;
					if (type != null && type.Kind != TypeKind.Struct && type.Kind != TypeKind.Enum)
						right = new ParenthesizedExpression (new ConditionalExpression (new BinaryOperatorExpression (new IdentifierExpression (member.Name), BinaryOperatorType.InEquality, new PrimitiveExpression (null)), right, new PrimitiveExpression (0)));

					if (binOp == null) {
						binOp = right;
					} else {
						binOp = new BinaryOperatorExpression (binOp, BinaryOperatorType.ExclusiveOr, right);
					}
				}
				var uncheckedBlock = new BlockStatement ();
				uncheckedBlock.Statements.Add (new ReturnStatement (binOp));

				methodDeclaration.Body.Statements.Add (new UncheckedStatement (uncheckedBlock));
				yield return methodDeclaration.ToString (Options.FormattingOptions);
			}
		}
	}
}
