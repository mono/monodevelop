// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision: 915 $</version>
// </file>

using System;
using System.Collections.Generic;
using System.CodeDom;
using NUnit.Framework;
using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Parser.AST;

namespace ICSharpCode.NRefactory.Tests.Output.CodeDom.Tests
{
	[TestFixture]
	public class InvocationExpressionsTests
	{
		[Test]
		public void IdentifierOnlyInvocation()
		{
			// InitializeComponents();
			IdentifierExpression identifier = new IdentifierExpression("InitializeComponents");
			InvocationExpression invocation = new InvocationExpression(identifier, new List<Expression>());
			object output = invocation.AcceptVisitor(new CodeDOMVisitor(), null);
			Assert.IsTrue(output is CodeMethodInvokeExpression);
			CodeMethodInvokeExpression mie = (CodeMethodInvokeExpression)output;
			Assert.AreEqual("InitializeComponents", mie.Method.MethodName);
			Assert.IsTrue(mie.Method.TargetObject is CodeThisReferenceExpression);
		}
		
		[Test]
		public void MethodOnThisReferenceInvocation()
		{
			// InitializeComponents();
			FieldReferenceExpression field = new FieldReferenceExpression(new ThisReferenceExpression(), "InitializeComponents");
			InvocationExpression invocation = new InvocationExpression(field, new List<Expression>());
			object output = invocation.AcceptVisitor(new CodeDOMVisitor(), null);
			Assert.IsTrue(output is CodeMethodInvokeExpression);
			CodeMethodInvokeExpression mie = (CodeMethodInvokeExpression)output;
			Assert.AreEqual("InitializeComponents", mie.Method.MethodName);
			Assert.IsTrue(mie.Method.TargetObject is CodeThisReferenceExpression);
		}
		
		[Test]
		public void InvocationOfStaticMethod()
		{
			// System.Drawing.Color.FromArgb();
			FieldReferenceExpression field = new FieldReferenceExpression(new IdentifierExpression("System"), "Drawing");
			field = new FieldReferenceExpression(field, "Color");
			field = new FieldReferenceExpression(field, "FromArgb");
			InvocationExpression invocation = new InvocationExpression(field, new List<Expression>());
			object output = invocation.AcceptVisitor(new CodeDOMVisitor(), null);
			Assert.IsTrue(output is CodeMethodInvokeExpression);
			CodeMethodInvokeExpression mie = (CodeMethodInvokeExpression)output;
			Assert.AreEqual("FromArgb", mie.Method.MethodName);
			Assert.IsTrue(mie.Method.TargetObject is CodeTypeReferenceExpression);
			Assert.AreEqual("System.Drawing.Color", (mie.Method.TargetObject as CodeTypeReferenceExpression).Type.BaseType);
		}
		
		[Test]
		public void ComplexExample()
		{
			string code = @"class A {
	Button closeButton;
	void M() {
		System.Windows.Forms.Panel panel1;
		closeButton = new System.Windows.Forms.Button();
		panel1 = new System.Windows.Forms.Panel();
		panel1.SuspendLayout();
		panel1.Controls.Add(this.closeButton);
		closeButton.BackColor = System.Drawing.Color.FromArgb();
		panel1.BackColor = System.Drawing.SystemColors.Info;
	}
}";
			TypeDeclaration decl = ICSharpCode.NRefactory.Tests.AST.ParseUtilCSharp.ParseGlobal<TypeDeclaration>(code);
			CompilationUnit cu = new CompilationUnit();
			cu.AddChild(decl);
			CodeNamespace ns = (CodeNamespace)cu.AcceptVisitor(new CodeDOMVisitor(), null);
			Assert.AreEqual("A", ns.Types[0].Name);
			Assert.AreEqual("closeButton", ns.Types[0].Members[0].Name);
			Assert.AreEqual("M", ns.Types[0].Members[1].Name);
			CodeMemberMethod m = (CodeMemberMethod)ns.Types[0].Members[1];
			
			CodeVariableDeclarationStatement s0 = (CodeVariableDeclarationStatement)m.Statements[0];
			Assert.AreEqual("panel1", s0.Name);
			Assert.AreEqual("System.Windows.Forms.Panel", s0.Type.BaseType);
			
			CodeAssignStatement cas = (CodeAssignStatement)m.Statements[1];
			Assert.AreEqual("closeButton", ((CodeFieldReferenceExpression)cas.Left).FieldName);
			
			cas = (CodeAssignStatement)m.Statements[2];
			Assert.AreEqual("panel1", ((CodeVariableReferenceExpression)cas.Left).VariableName);
			
			CodeExpressionStatement ces = (CodeExpressionStatement)m.Statements[3];
			CodeMethodInvokeExpression mie = (CodeMethodInvokeExpression)ces.Expression;
			Assert.AreEqual("SuspendLayout", mie.Method.MethodName);
			Assert.AreEqual("panel1", ((CodeVariableReferenceExpression)mie.Method.TargetObject).VariableName);
			
			ces = (CodeExpressionStatement)m.Statements[4];
			mie = (CodeMethodInvokeExpression)ces.Expression;
			Assert.AreEqual("Add", mie.Method.MethodName);
			CodePropertyReferenceExpression pre = (CodePropertyReferenceExpression)mie.Method.TargetObject;
			Assert.AreEqual("Controls", pre.PropertyName);
			Assert.AreEqual("panel1", ((CodeVariableReferenceExpression)pre.TargetObject).VariableName);
			
			cas = (CodeAssignStatement)m.Statements[5];
			pre = (CodePropertyReferenceExpression)cas.Left;
			Assert.AreEqual("BackColor", pre.PropertyName);
			Assert.AreEqual("closeButton", ((CodeFieldReferenceExpression)pre.TargetObject).FieldName);
			mie = (CodeMethodInvokeExpression)cas.Right;
			Assert.AreEqual("FromArgb", mie.Method.MethodName);
			Assert.IsTrue(mie.Method.TargetObject is CodeTypeReferenceExpression);
			Assert.AreEqual("System.Drawing.Color", (mie.Method.TargetObject as CodeTypeReferenceExpression).Type.BaseType);
			
			cas = (CodeAssignStatement)m.Statements[6];
			pre = (CodePropertyReferenceExpression)cas.Left;
			Assert.AreEqual("BackColor", pre.PropertyName);
			Assert.AreEqual("panel1", ((CodeVariableReferenceExpression)pre.TargetObject).VariableName);
			pre = (CodePropertyReferenceExpression)cas.Right;
			Assert.AreEqual("Info", pre.PropertyName);
			Assert.IsTrue(pre.TargetObject is CodeTypeReferenceExpression);
			Assert.AreEqual("System.Drawing.SystemColors", (pre.TargetObject as CodeTypeReferenceExpression).Type.BaseType);
		}
	}
}
