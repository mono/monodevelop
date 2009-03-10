// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
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
	public class InvocationExpressionTests
	{
		void CheckSimpleInvoke(InvocationExpression ie)
		{
			Assert.AreEqual(0, ie.Arguments.Count);
			Assert.IsTrue(ie.TargetObject is IdentifierExpression);
			Assert.AreEqual("myMethod", ((IdentifierExpression)ie.TargetObject).Identifier);
		}
		
		void CheckGenericInvoke(InvocationExpression expr)
		{
			Assert.AreEqual(1, expr.Arguments.Count);
			Assert.IsTrue(expr.TargetObject is IdentifierExpression);
			IdentifierExpression ident = (IdentifierExpression)expr.TargetObject;
			Assert.AreEqual("myMethod", ident.Identifier);
			Assert.AreEqual(1, ident.TypeArguments.Count);
			Assert.AreEqual("System.Char", ident.TypeArguments[0].Type);
		}
		
		void CheckGenericInvoke2(InvocationExpression expr)
		{
			Assert.AreEqual(0, expr.Arguments.Count);
			Assert.IsTrue(expr.TargetObject is IdentifierExpression);
			IdentifierExpression ident = (IdentifierExpression)expr.TargetObject;
			Assert.AreEqual("myMethod", ident.Identifier);
			Assert.AreEqual(2, ident.TypeArguments.Count);
			Assert.AreEqual("T", ident.TypeArguments[0].Type);
			Assert.IsFalse(ident.TypeArguments[0].IsKeyword);
			Assert.AreEqual("System.Boolean", ident.TypeArguments[1].Type);
			Assert.IsTrue(ident.TypeArguments[1].IsKeyword);
		}
		
		
		#region C#
		[Test]
		public void CSharpSimpleInvocationExpressionTest()
		{
			CheckSimpleInvoke(ParseUtilCSharp.ParseExpression<InvocationExpression>("myMethod()"));
		}
		
		[Test]
		public void CSharpGenericInvocationExpressionTest()
		{
			CheckGenericInvoke(ParseUtilCSharp.ParseExpression<InvocationExpression>("myMethod<char>('a')"));
		}
		
		[Test]
		public void CSharpGenericInvocation2ExpressionTest()
		{
			CheckGenericInvoke2(ParseUtilCSharp.ParseExpression<InvocationExpression>("myMethod<T,bool>()"));
		}
		
		[Test]
		public void CSharpAmbiguousGrammarGenericMethodCall()
		{
			InvocationExpression ie = ParseUtilCSharp.ParseExpression<InvocationExpression>("F(G<A,B>(7))");
			Assert.IsTrue(ie.TargetObject is IdentifierExpression);
			Assert.AreEqual(1, ie.Arguments.Count);
			ie = (InvocationExpression)ie.Arguments[0];
			Assert.AreEqual(1, ie.Arguments.Count);
			Assert.IsTrue(ie.Arguments[0] is PrimitiveExpression);
			IdentifierExpression ident = (IdentifierExpression)ie.TargetObject;
			Assert.AreEqual("G", ident.Identifier);
			Assert.AreEqual(2, ident.TypeArguments.Count);
		}
		
		[Test]
		public void CSharpAmbiguousGrammarNotAGenericMethodCall()
		{
			BinaryOperatorExpression boe = ParseUtilCSharp.ParseExpression<BinaryOperatorExpression>("F<A>+y");
			Assert.AreEqual(BinaryOperatorType.GreaterThan, boe.Op);
			Assert.IsTrue(boe.Left is BinaryOperatorExpression);
			Assert.IsTrue(boe.Right is UnaryOperatorExpression);
		}
		
		[Test]
		public void CSharpInvalidNestedInvocationExpressionTest()
		{
			// this test was written because this bug caused the AbstractASTVisitor to crash
			
			InvocationExpression expr = ParseUtilCSharp.ParseExpression<InvocationExpression>("WriteLine(myMethod(,))", true);
			Assert.IsTrue(expr.TargetObject is IdentifierExpression);
			Assert.AreEqual("WriteLine", ((IdentifierExpression)expr.TargetObject).Identifier);
			
			Assert.AreEqual(1, expr.Arguments.Count); // here a second null parameter was added incorrectly
			
			Assert.IsTrue(expr.Arguments[0] is InvocationExpression);
			CheckSimpleInvoke((InvocationExpression)expr.Arguments[0]);
		}
		#endregion
		
		#region VB.NET
		[Test]
		public void VBNetSimpleInvocationExpressionTest()
		{
			CheckSimpleInvoke(ParseUtilVBNet.ParseExpression<InvocationExpression>("myMethod()"));
		}
		
		[Test]
		public void VBNetGenericInvocationExpressionTest()
		{
			CheckGenericInvoke(ParseUtilVBNet.ParseExpression<InvocationExpression>("myMethod(Of Char)(\"a\"c)"));
		}
		
		[Test]
		public void VBNetGenericInvocation2ExpressionTest()
		{
			CheckGenericInvoke2(ParseUtilVBNet.ParseExpression<InvocationExpression>("myMethod(Of T, Boolean)()"));
		}
		
		[Test]
		public void PrimitiveExpression1Test()
		{
			InvocationExpression ie = ParseUtilVBNet.ParseExpression<InvocationExpression>("546.ToString()");
			Assert.AreEqual(0, ie.Arguments.Count);
		}
		
		#endregion
	}
}
