// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision: 3017 $</version>
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
		static LambdaExpression Parse(string program)
		{
			return ParseUtilCSharp.ParseExpression<LambdaExpression>(program);
		}
		
		[Test]
		public void ImplicitlyTypedExpressionBody()
		{
			LambdaExpression e = Parse("(x) => x + 1");
			Assert.AreEqual("x", e.Parameters[0].ParameterName);
			Assert.IsTrue(e.Parameters[0].TypeReference.IsNull);
			Assert.IsTrue(e.ExpressionBody is BinaryOperatorExpression);
		}
		
		[Test]
		public void ImplicitlyTypedExpressionBodyWithoutParenthesis()
		{
			LambdaExpression e = Parse("x => x + 1");
			Assert.AreEqual("x", e.Parameters[0].ParameterName);
			Assert.IsTrue(e.Parameters[0].TypeReference.IsNull);
			Assert.IsTrue(e.ExpressionBody is BinaryOperatorExpression);
		}
		
		[Test]
		public void ImplicitlyTypedStatementBody()
		{
			LambdaExpression e = Parse("(x) => { return x + 1; }");
			Assert.AreEqual("x", e.Parameters[0].ParameterName);
			Assert.IsTrue(e.Parameters[0].TypeReference.IsNull);
			Assert.IsTrue(e.StatementBody.Children[0] is ReturnStatement);
		}
		
		[Test]
		public void ImplicitlyTypedStatementBodyWithoutParenthesis()
		{
			LambdaExpression e = Parse("x => { return x + 1; }");
			Assert.AreEqual("x", e.Parameters[0].ParameterName);
			Assert.IsTrue(e.Parameters[0].TypeReference.IsNull);
			Assert.IsTrue(e.StatementBody.Children[0] is ReturnStatement);
		}
		
		[Test]
		public void ExplicitlyTypedStatementBody()
		{
			LambdaExpression e = Parse("(int x) => { return x + 1; }");
			Assert.AreEqual("x", e.Parameters[0].ParameterName);
			Assert.AreEqual("int", e.Parameters[0].TypeReference.Type);
			Assert.IsTrue(e.StatementBody.Children[0] is ReturnStatement);
		}
		
		[Test]
		public void LambdaExpressionContainingConditionalExpression()
		{
			LambdaExpression e = Parse("rr => rr != null ? rr.ResolvedType : null");
			Assert.AreEqual("rr", e.Parameters[0].ParameterName);
			Assert.IsTrue(e.ExpressionBody is ConditionalExpression);
		}
	}
}
