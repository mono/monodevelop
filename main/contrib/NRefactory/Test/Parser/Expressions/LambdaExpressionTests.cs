// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision: 3660 $</version>
// </file>

using System;
using System.IO;
using NUnit.Framework;
using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Ast;

namespace ICSharpCode.NRefactory.Tests.Ast
{
	[TestFixture]
	public class LambdaExpressionTests
	{
		#region C#
		
		static LambdaExpression ParseCSharp(string program)
		{
			return ParseUtilCSharp.ParseExpression<LambdaExpression>(program);
		}
		
		[Test]
		public void ImplicitlyTypedExpressionBody()
		{
			LambdaExpression e = ParseCSharp("(x) => x + 1");
			Assert.AreEqual("x", e.Parameters[0].ParameterName);
			Assert.IsTrue(e.Parameters[0].TypeReference.IsNull);
			Assert.IsTrue(e.ExpressionBody is BinaryOperatorExpression);
		}
		
		[Test]
		public void ImplicitlyTypedExpressionBodyWithoutParenthesis()
		{
			LambdaExpression e = ParseCSharp("x => x + 1");
			Assert.AreEqual("x", e.Parameters[0].ParameterName);
			Assert.IsTrue(e.Parameters[0].TypeReference.IsNull);
			Assert.IsTrue(e.ExpressionBody is BinaryOperatorExpression);
		}
		
		[Test]
		public void ImplicitlyTypedStatementBody()
		{
			LambdaExpression e = ParseCSharp("(x) => { return x + 1; }");
			Assert.AreEqual("x", e.Parameters[0].ParameterName);
			Assert.IsTrue(e.Parameters[0].TypeReference.IsNull);
			Assert.IsTrue(e.StatementBody.Children[0] is ReturnStatement);
		}
		
		[Test]
		public void ImplicitlyTypedStatementBodyWithoutParenthesis()
		{
			LambdaExpression e = ParseCSharp("x => { return x + 1; }");
			Assert.AreEqual("x", e.Parameters[0].ParameterName);
			Assert.IsTrue(e.Parameters[0].TypeReference.IsNull);
			Assert.IsTrue(e.StatementBody.Children[0] is ReturnStatement);
		}
		
		[Test]
		public void ExplicitlyTypedStatementBody()
		{
			LambdaExpression e = ParseCSharp("(int x) => { return x + 1; }");
			Assert.AreEqual("x", e.Parameters[0].ParameterName);
			Assert.AreEqual("System.Int32", e.Parameters[0].TypeReference.Type);
			Assert.IsTrue(e.StatementBody.Children[0] is ReturnStatement);
		}
		
		[Test]
		public void LambdaExpressionContainingConditionalExpression()
		{
			LambdaExpression e = ParseCSharp("rr => rr != null ? rr.ResolvedType : null");
			Assert.AreEqual("rr", e.Parameters[0].ParameterName);
			Assert.IsTrue(e.ExpressionBody is ConditionalExpression);
		}
		
		#endregion
		
		#region VB.NET
		
		static LambdaExpression ParseVBNet(string program)
		{
			return ParseUtilVBNet.ParseExpression<LambdaExpression>(program);
		}
		
		[Test]
		public void VBNetLambdaWithParameters()
		{
			LambdaExpression e = ParseVBNet("Function(x As Boolean) x Or True");
			Assert.AreEqual(1, e.Parameters.Count);
			Assert.AreEqual("x", e.Parameters[0].ParameterName);
			Assert.AreEqual("System.Boolean", e.Parameters[0].TypeReference.Type);
			Assert.IsTrue(e.ExpressionBody is BinaryOperatorExpression);
		}

		[Test]
		public void VBNetLambdaWithoutParameters()
		{
			LambdaExpression e = ParseVBNet("Function x Or True");
			Assert.AreEqual(0, e.Parameters.Count);
			Assert.IsTrue(e.ExpressionBody is BinaryOperatorExpression);
		}
		
		[Test]
		public void VBNetNestedLambda()
		{
			LambdaExpression e = ParseVBNet("Function(x As Boolean) Function(y As Boolean) x And y");
			Assert.AreEqual(1, e.Parameters.Count);
			Assert.IsTrue(e.ExpressionBody is LambdaExpression);
		}
		
		#endregion
	}
}
