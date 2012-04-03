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
			
			protected override IEnumerable<string> GenerateCode (INRefactoryASTProvider astProvider, string indent, List<IBaseMember> includedMembers)
			{
				bool isValueType = Options.EnclosingType.ClassType == MonoDevelop.Projects.Dom.ClassType.Struct;	
				//Value types generate Equals method that takes concrete type, this
				//avoids boxing/unboxing when value types are compared directly or called via generated equality operators
				if (isValueType)
					yield return GenerateDirectEqualsMethod (astProvider, indent, includedMembers);
				yield return GenerateEqualsMethod (astProvider, indent, includedMembers, isValueType);
				yield return GenerateGetHashCodeMethod (astProvider, indent, includedMembers);
				yield return GenerateEqualsOperator (astProvider, indent, includedMembers, false, isValueType);
				yield return GenerateEqualsOperator (astProvider, indent, includedMembers, true, isValueType);
			}

			protected string GenerateDirectEqualsMethod (INRefactoryASTProvider astProvider, string indent, List<IBaseMember> includedMembers)
			{
				var type = new DomReturnType (Options.EnclosingType).ConvertToTypeReference ();
				// Genereate Equals
				MethodDeclaration methodDeclaration = new MethodDeclaration ();
				methodDeclaration.Name = "Equals";

				methodDeclaration.ReturnType = DomReturnType.Bool.ConvertToTypeReference ();
				methodDeclaration.Modifiers = ICSharpCode.NRefactory.CSharp.Modifiers.Public;
				methodDeclaration.Body = new BlockStatement ();
				methodDeclaration.Parameters.Add (new ParameterDeclaration (type, "obj"));
				IdentifierExpression expr = new IdentifierExpression("obj");
				methodDeclaration.Body.Add (new ReturnStatement(GenerateMembersEqualityTest (expr, includedMembers)));
				return astProvider.OutputNode (this.Options.Dom, methodDeclaration, indent);
			}
			
			private string GenerateEqualsMethod  (INRefactoryASTProvider astProvider, string indent, List<IBaseMember> includedMembers, bool isValueType)
			{
				// Genereate Equals
				MethodDeclaration methodDeclaration = new MethodDeclaration ();
				methodDeclaration.Name = "Equals";

				methodDeclaration.ReturnType = DomReturnType.Bool.ConvertToTypeReference ();
				methodDeclaration.Modifiers = ICSharpCode.NRefactory.CSharp.Modifiers.Public | ICSharpCode.NRefactory.CSharp.Modifiers.Override;
				methodDeclaration.Body = new BlockStatement ();
				methodDeclaration.Parameters.Add (new ParameterDeclaration (DomReturnType.Object.ConvertToTypeReference (), "obj"));
				IdentifierExpression paramId = new IdentifierExpression ("obj");
				IfElseStatement ifStatement = new IfElseStatement ();
				ifStatement.Condition = new BinaryOperatorExpression (paramId, BinaryOperatorType.Equality, new PrimitiveExpression (null));
				ifStatement.TrueStatement = new ReturnStatement (new PrimitiveExpression (false));
				methodDeclaration.Body.Statements.Add (ifStatement);

				ifStatement = new IfElseStatement ();
				List<Expression> arguments = new List<Expression> ();
				arguments.Add (new ThisReferenceExpression ());
				arguments.Add (paramId.Clone ());
				ifStatement.Condition = new InvocationExpression (new IdentifierExpression ("ReferenceEquals"), arguments);
				ifStatement.TrueStatement = new ReturnStatement (new PrimitiveExpression (true));
				methodDeclaration.Body.Statements.Add (ifStatement);

				ifStatement = new IfElseStatement ();
				ifStatement.Condition = new BinaryOperatorExpression (new InvocationExpression (new MemberReferenceExpression (paramId.Clone (), "GetType")), BinaryOperatorType.InEquality, new TypeOfExpression (new SimpleType (Options.EnclosingType.Name)));
				ifStatement.TrueStatement = new ReturnStatement (new PrimitiveExpression (false));
				methodDeclaration.Body.Statements.Add (ifStatement);

				AstType varType = new DomReturnType (Options.EnclosingType).ConvertToTypeReference ();
				var varDecl = new VariableDeclarationStatement (varType, "other", new CastExpression (varType.Clone (), paramId.Clone ()));
				methodDeclaration.Body.Statements.Add (varDecl);
				
				IdentifierExpression otherId = new IdentifierExpression ("other");
				//when generating Equals body for value type, use the Equals with concrete type
				var returnExpr = isValueType ? new InvocationExpression (new IdentifierExpression ("Equals"), otherId.Clone ()) 
					: GenerateMembersEqualityTest(otherId, includedMembers);
				methodDeclaration.Body.Statements.Add (new ReturnStatement (returnExpr));
				return astProvider.OutputNode (this.Options.Dom, methodDeclaration, indent);
			}
			
			private Expression GenerateMembersEqualityTest (IdentifierExpression otherId, List<IBaseMember> includedMembers)
			{
				Expression binOp = null;
				foreach (IMember member in includedMembers) {
					Expression right = new BinaryOperatorExpression (new IdentifierExpression (member.Name), BinaryOperatorType.Equality, new MemberReferenceExpression (otherId.Clone (), member.Name));
					if (binOp == null) {
						binOp = right;
					} else {
						binOp = new BinaryOperatorExpression (binOp, BinaryOperatorType.ConditionalAnd, right);
					}
				}
				return binOp;
			}
			
			private string GenerateEqualsOperator (INRefactoryASTProvider astProvider, string indent, List<IBaseMember> includedMembers, bool negate, bool isValueType)
			{
				var leftId = new IdentifierExpression ("left");
				var rightId = new IdentifierExpression ("right");
				
				var enclosingType = new DomReturnType (Options.EnclosingType).ConvertToTypeReference ();
				var leftParam = new ParameterDeclaration (enclosingType, leftId.Identifier);
				var rightParam = new ParameterDeclaration (enclosingType.Clone (), rightId.Identifier);
				
				var methodDeclaration = new MethodDeclaration ();
				methodDeclaration.Name = negate ? "operator !=" : "operator ==";
				
				methodDeclaration.ReturnType = DomReturnType.Bool.ConvertToTypeReference ();
				methodDeclaration.Modifiers = ICSharpCode.NRefactory.CSharp.Modifiers.Public | ICSharpCode.NRefactory.CSharp.Modifiers.Static;
				methodDeclaration.Parameters.Add (leftParam);
				methodDeclaration.Parameters.Add (rightParam);
				methodDeclaration.Body = new BlockStatement ();
				
				Expression expr = null;
				//for value type use Equals(..) with concrete type, this avoids pointless boxing/unboxing
				if (isValueType)
					expr = new InvocationExpression (new MemberReferenceExpression(leftId, "Equals"), rightId);
				else
					expr = new InvocationExpression (new IdentifierExpression ("Equals"), leftId, rightId);
				if (negate)
					expr = new UnaryOperatorExpression (UnaryOperatorType.Not, expr);
				methodDeclaration.Body.Statements.Add (new ReturnStatement (expr));
				return astProvider.OutputNode (this.Options.Dom, methodDeclaration, indent);
			}
			
			private string GenerateGetHashCodeMethod (INRefactoryASTProvider astProvider, string indent, List<IBaseMember> includedMembers)
			{
				var methodDeclaration = new MethodDeclaration ();
				methodDeclaration.Name = "GetHashCode";

				methodDeclaration.ReturnType = DomReturnType.Int32.ConvertToTypeReference ();
				methodDeclaration.Modifiers = ICSharpCode.NRefactory.CSharp.Modifiers.Public | ICSharpCode.NRefactory.CSharp.Modifiers.Override;
				methodDeclaration.Body = new BlockStatement ();

				Expression binOp = null;
				foreach (IMember member in includedMembers) {
					Expression right;
					right = new InvocationExpression (new MemberReferenceExpression (new IdentifierExpression (member.Name), "GetHashCode"));

					IType type = Options.Dom.SearchType (Options.Document.ParsedDocument.CompilationUnit, member is IType ? ((IType)member) : member.DeclaringType, member.Location, member.ReturnType);
					if (type != null && type.ClassType != MonoDevelop.Projects.Dom.ClassType.Struct&& type.ClassType != MonoDevelop.Projects.Dom.ClassType.Enum)
						right = new ParenthesizedExpression (new ConditionalExpression (new BinaryOperatorExpression (new IdentifierExpression (member.Name), BinaryOperatorType.InEquality, new PrimitiveExpression (null)), right, new PrimitiveExpression (0)));

					if (binOp == null) {
						binOp = right;
					} else {
						binOp = new BinaryOperatorExpression (binOp, BinaryOperatorType.ExclusiveOr, right);
					}
				}
				BlockStatement uncheckedBlock = new BlockStatement ();
				uncheckedBlock.Statements.Add (new ReturnStatement (binOp));

				methodDeclaration.Body.Statements.Add (new UncheckedStatement (uncheckedBlock));
				return astProvider.OutputNode (this.Options.Dom, methodDeclaration, indent);
			}
		}
	}
}
